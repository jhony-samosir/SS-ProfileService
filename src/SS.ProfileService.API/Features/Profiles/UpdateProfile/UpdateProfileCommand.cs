using System;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserPublicId,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    DateOnly? DateOfBirth) : IRequest<IResult>;
