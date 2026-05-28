using System;
using System.Collections.Generic;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    DateOnly? DateOfBirth);
