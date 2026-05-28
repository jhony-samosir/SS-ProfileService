using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;
using SS.ProfileService.API.Infrastructure.Messaging;
using SS.ProfileService.API.Features.Profiles.CreateProfile;
using SS.ProfileService.API.Features.Profiles.GetProfileById;
using SS.ProfileService.API.Features.Profiles.UpdateProfile;
using SS.ProfileService.API.Features.Addresses.CreateAddress;
using SS.ProfileService.API.Features.Addresses.UpdateAddress;
using SS.ProfileService.API.Features.Addresses.DeleteAddress;
using SS.ProfileService.API.Features.Addresses.SetDefaultAddress;
using SS.ProfileService.API.Extensions;
using SS.ProfileService.API.Middleware;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

// Add Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register DbContext with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsEnvironment("Testing"))
{
    // Do not register Npgsql if already configured by the testing host or if we want to let testing host override it.
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(
        options => options.UseNpgsql(connectionString),
        contextLifetime: ServiceLifetime.Scoped,
        optionsLifetime: ServiceLifetime.Singleton);

    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddHostedService<UserEventConsumerWorker>();
}

// Register Observability & OpenTelemetry
builder.Services.AddProfileObservability(builder.Configuration);

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Register FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enforce OWASP security response headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Enforce Zero-Trust HMAC Origin Verification (unless testing)
if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseMiddleware<GatewaySignatureMiddleware>();
}

// Map Vertical Slice Endpoints
app.MapCreateProfileEndpoint();
app.MapGetProfileByIdEndpoint();
app.MapUpdateProfileEndpoint();
app.MapCreateAddressEndpoint();
app.MapUpdateAddressEndpoint();
app.MapDeleteAddressEndpoint();
app.MapSetDefaultAddressEndpoint();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "ProfileService" }));

app.Run();

// Make the implicit Program class public so functional test projects can reference it
public partial class Program { }
