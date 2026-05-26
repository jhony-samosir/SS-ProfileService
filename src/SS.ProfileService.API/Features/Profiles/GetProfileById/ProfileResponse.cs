using System;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public record ProfileResponse(
    int Id,
    Guid PublicId,
    int UserId,
    Guid UserPublicId,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    DateOnly? DateOfBirth,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
