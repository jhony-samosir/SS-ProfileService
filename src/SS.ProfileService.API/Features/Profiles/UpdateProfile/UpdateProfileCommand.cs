using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string FullName,
    string? PhoneNumber,
    string? Address,
    string? AvatarUrl) : IRequest<IResult>;
