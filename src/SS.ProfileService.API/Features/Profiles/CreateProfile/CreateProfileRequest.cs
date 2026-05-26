using System;

namespace SS.ProfileService.API.Features.Profiles.CreateProfile;

public record CreateProfileRequest(
    int UserId,
    Guid UserPublicId,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    DateOnly? DateOfBirth);
