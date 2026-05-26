namespace SS.ProfileService.API.Features.Profiles.CreateProfile;

public record CreateProfileRequest(Guid UserId, string FullName, string? PhoneNumber, string? Address, string? AvatarUrl);
