using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;
using SS.ProfileService.API.Features.Profiles.CreateProfile;
using SS.ProfileService.API.Features.Profiles.GetProfileById;
using SS.ProfileService.API.Features.Profiles.UpdateProfile;
using Xunit;

namespace SS.ProfileService.Tests;

public class ProfileEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProfileEndpointsTests(WebApplicationFactory<Program> factory)
    {
        // Setup in-memory database configuration for testing
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Environment", "Testing");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Also remove the DbContext itself if registered in Program
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApplicationDbContext));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });
            });
        });
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(content);
        Assert.Equal("Healthy", content.Status);
        Assert.Equal("ProfileService", content.Service);
    }

    [Fact]
    public async Task CreateProfile_WithValidData_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();
        var request = new CreateProfileRequest(userId, "John Doe", "12345678", "123 Main St", "http://avatar.url");

        // Act
        var response = await client.PostAsJsonAsync("/api/profiles", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedProfileResponse>();
        Assert.NotNull(created);
        Assert.Equal(userId, created.UserId);
        Assert.Equal("John Doe", created.FullName);
    }

    [Fact]
    public async Task CreateProfile_WithEmptyFullName_ReturnsValidationError()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateProfileRequest(Guid.NewGuid(), "", "12345678", "123 St", "");

        // Act
        var response = await client.PostAsJsonAsync("/api/profiles", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record HealthResponse(string Status, string Service);
    private record CreatedProfileResponse(int Id, Guid PublicId, Guid UserId, string FullName);
}
