using System;
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
        var userPublicId = Guid.NewGuid();
        var request = new CreateProfileRequest(
            UserId: 1,
            UserPublicId: userPublicId,
            FullName: "John Doe",
            PhoneNumber: "12345678",
            AvatarUrl: "http://avatar.url",
            Bio: "Software developer",
            Gender: "Male",
            DateOfBirth: new DateOnly(1990, 1, 1));

        // Act
        var response = await client.PostAsJsonAsync("/api/profiles", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedProfileResponse>();
        Assert.NotNull(created);
        Assert.Equal(userPublicId, created.UserPublicId);
        Assert.Equal("John Doe", created.FullName);
    }

    [Fact]
    public async Task CreateProfile_WithEmptyFullName_ReturnsValidationError()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateProfileRequest(
            UserId: 2,
            UserPublicId: Guid.NewGuid(),
            FullName: "",
            PhoneNumber: "12345678",
            AvatarUrl: "http://avatar.url",
            Bio: "Bio",
            Gender: "Male",
            DateOfBirth: new DateOnly(1990, 1, 1));

        // Act
        var response = await client.PostAsJsonAsync("/api/profiles", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ById_ReturnsProfile()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userPublicId = Guid.NewGuid();
        var createRequest = new CreateProfileRequest(
            UserId: 10,
            UserPublicId: userPublicId,
            FullName: "Alice Smith",
            PhoneNumber: "87654321",
            AvatarUrl: "http://avatar2.url",
            Bio: "Alice's Bio",
            Gender: "Female",
            DateOfBirth: new DateOnly(1995, 5, 5));

        var createResponse = await client.PostAsJsonAsync("/api/profiles", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedProfileResponse>();
        Assert.NotNull(created);

        // Act
        var getResponse = await client.GetAsync($"/api/profiles/db/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var profile = await getResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Alice Smith", profile.FullName);
        Assert.Equal(10, profile.UserId);
        Assert.Equal(userPublicId, profile.UserPublicId);
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_ReturnsUpdated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userPublicId = Guid.NewGuid();
        var createRequest = new CreateProfileRequest(
            UserId: 20,
            UserPublicId: userPublicId,
            FullName: "Bob Builder",
            PhoneNumber: "11111111",
            AvatarUrl: "http://avatar3.url",
            Bio: "Can we fix it?",
            Gender: "Male",
            DateOfBirth: new DateOnly(1985, 3, 3));

        var createResponse = await client.PostAsJsonAsync("/api/profiles", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedProfileResponse>();
        Assert.NotNull(created);

        var updateRequest = new UpdateProfileRequest(
            FullName: "Bob The Builder",
            PhoneNumber: "22222222",
            AvatarUrl: "http://newavatar.url",
            Bio: "Yes we can!",
            Gender: "Male",
            DateOfBirth: new DateOnly(1985, 3, 3));

        // Act
        var updateResponse = await client.PutAsJsonAsync($"/api/profiles/{userPublicId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UpdatedProfileResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Bob The Builder", updated.FullName);
        Assert.Equal("Yes we can!", updated.Bio);
        Assert.Equal("22222222", updated.PhoneNumber);
    }

    private record HealthResponse(string Status, string Service);
    private record CreatedProfileResponse(int Id, Guid PublicId, Guid UserPublicId, string FullName);
    private record UpdatedProfileResponse(int Id, Guid PublicId, Guid UserPublicId, string FullName, string? Bio, string? PhoneNumber);
}
