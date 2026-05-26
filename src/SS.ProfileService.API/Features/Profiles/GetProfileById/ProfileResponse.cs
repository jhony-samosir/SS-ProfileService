using System;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public record ProfileResponse(
    int Id,
    Guid PublicId,
    Guid UserId,
    string FullName,
    string? PhoneNumber,
    string? Address,
    string? AvatarUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
