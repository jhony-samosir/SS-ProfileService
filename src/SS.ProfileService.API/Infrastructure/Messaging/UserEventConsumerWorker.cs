using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SS.ProfileService.API.Domain.Entities;
using SS.ProfileService.API.Infrastructure.Data;
using System.Text;

namespace SS.ProfileService.API.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes user registration/verification events from RabbitMQ
/// and creates/updates user profiles using an idempotent inbox pattern.
/// </summary>
public class UserEventConsumerWorker : BackgroundService
{
    private readonly ILogger<UserEventConsumerWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private const string ExchangeName = "samstore.events";
    private const string QueueName = "ss-profile-service.user-events";
    private readonly string[] RoutingKeys = { "auth.user.registered", "auth.user.verified" };

    public UserEventConsumerWorker(
        ILogger<UserEventConsumerWorker> logger,
        IConfiguration configuration,
        IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("User Event Consumer Worker starting.");

        await Task.Delay(1000, stoppingToken); // Give time for app to fully start
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InitializeRabbitMQAsync(stoppingToken);
                await ConsumeMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserEventConsumerWorker. Retrying in 5 seconds.");
                await CleanupResourcesAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        await CleanupResourcesAsync();
        _logger.LogInformation("User Event Consumer Worker stopped.");
    }

    private async Task InitializeRabbitMQAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672,
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port}", 
            _configuration["RabbitMQ:Host"] ?? "localhost", 
            _configuration["RabbitMQ:Port"] ?? "5672");

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare exchange
        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Declare queue
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Bind queue to exchange with multiple routing keys
        foreach (var routingKey in RoutingKeys)
        {
            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: ExchangeName,
                routingKey: routingKey,
                cancellationToken: stoppingToken);
            
            _logger.LogInformation("Bound queue {Queue} to exchange {Exchange} with routing key {RoutingKey}", 
                QueueName, ExchangeName, routingKey);
        }

        _logger.LogInformation("RabbitMQ initialized successfully.");
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ channel not initialized");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                // Process the message
                await ProcessMessageAsync(messageJson, ea, stoppingToken);
                
                // Acknowledge the message
                await _channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                // Nack and requeue if processing fails
                await _channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        // Start consuming
        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
        
        _logger.LogInformation("Started consuming messages from queue {Queue}", QueueName);

        // Wait until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(string jsonMessage, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        _logger.LogDebug("Received message: {Message}", jsonMessage);
        
        try
        {
            // Deserialize the message to dynamic to access common properties
            using var doc = JsonDocument.Parse(jsonMessage);
            var root = doc.RootElement;
            
            // Extract common properties
            if (!root.TryGetProperty("userId", out var userIdProp) ||
                !root.TryGetProperty("userPublicId", out var publicIdProp) ||
                !root.TryGetProperty("fullName", out var fullNameProp))
            {
                _logger.LogWarning("Message missing required fields: {Message}", jsonMessage);
                return;
            }

            var userId = userIdProp.GetInt32();
            var userPublicId = Guid.Parse(publicIdProp.GetString()!);
            var fullName = fullNameProp.GetString() ?? "Unknown User";
            var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;

            // Use idempotent inbox pattern - check if we've already processed this message
            // The message ID could be in RabbitMQ headers or we can generate one from the content
            var messageId = GenerateMessageId(ea, jsonMessage);
            
            await using var dbContext = _dbContextFactory.CreateDbContext();
            
            // Begin transaction for atomicity
            await using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);
            try
            {
                // Check if this message was already processed (idempotency)
                var alreadyProcessed = await dbContext.InboxEvents
                    .AnyAsync(ie => ie.MessageId == messageId, stoppingToken);
                
                if (alreadyProcessed)
                {
                    _logger.LogInformation("Message {MessageId} already processed. Skipping.", messageId);
                    await transaction.CommitAsync(stoppingToken);
                    return;
                }

                // Check if profile already exists for this user
                var profileExists = await dbContext.UserProfiles
                    .AnyAsync(up => up.UserId == userId || up.UserPublicId == userPublicId, stoppingToken);

                if (!profileExists)
                {
                    // Create new profile
                    var profile = new UserProfile
                    {
                        UserId = userId,
                        UserPublicId = userPublicId,
                        FullName = fullName,
                        PhoneNumber = null,
                        AvatarUrl = null,
                        Bio = null,
                        Gender = null,
                        DateOfBirth = null,
                        CreatedBy = "UserEventConsumer"
                    };

                    dbContext.UserProfiles.Add(profile);
                    _logger.LogInformation("Created profile for UserId {UserId} from {EventType} event", 
                        userId, ea.RoutingKey);
                }
                else
                {
                    _logger.LogInformation("Profile already exists for UserId {UserId}. Skipping creation.", userId);
                }

                // Record the processed message (idempotency)
                var inboxEvent = new InboxEvent
                {
                    MessageId = messageId,
                    EventType = ea.RoutingKey,
                    AggregateType = "User",
                    Payload = jsonMessage,
                    Status = "PROCESSED",
                    ProcessedAt = DateTime.UtcNow
                };

                dbContext.InboxEvents.Add(inboxEvent);
                await dbContext.SaveChangesAsync(stoppingToken);
                
                await transaction.CommitAsync(stoppingToken);
                _logger.LogInformation("Successfully processed {EventType} event for UserId {UserId}", 
                    ea.RoutingKey, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed for message {MessageId}", messageId);
                await transaction.RollbackAsync(stoppingToken);
                throw;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON message: {Message}", jsonMessage.Substring(0, Math.Min(100, jsonMessage.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message");
            throw;
        }
    }

    private string GenerateMessageId(BasicDeliverEventArgs ea, string jsonMessage)
    {
        // Try to get message ID from headers first
        if (ea.BasicProperties.Headers != null && 
            ea.BasicProperties.Headers.TryGetValue("message-id", out var headerValue) &&
            headerValue is byte[] headerBytes &&
            headerBytes.Length > 0)
        {
            return Encoding.UTF8.GetString(headerBytes);
        }
        
        // Fallback: create a hash from the message content and routing key
        // This ensures idempotency even if RabbitMQ redelivers the same message
        var input = $"{ea.RoutingKey}:{jsonMessage}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }

    private async Task CleanupResourcesAsync()
    {
        if (_channel != null && _channel.IsOpen)
        {
            await _channel.CloseAsync();
        }
        
        if (_connection != null && _connection.IsOpen)
        {
            await _connection.CloseAsync();
        }
        
        _channel = null;
        _connection = null;
    }
}