using MediatR;
using Microsoft.AspNetCore.Http;

namespace SS.ProfileService.API.Features.Profiles.CreateProfile;

public record CreateProfileCommand(
    Guid UserId,
    string FullName,
    string? PhoneNumber,
    string? Address,
    string? AvatarUrl) : IRequest<IResult>;
