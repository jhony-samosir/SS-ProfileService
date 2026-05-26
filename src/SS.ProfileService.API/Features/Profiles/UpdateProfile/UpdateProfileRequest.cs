namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public record UpdateProfileRequest(string FullName, string? PhoneNumber, string? Address, string? AvatarUrl);
