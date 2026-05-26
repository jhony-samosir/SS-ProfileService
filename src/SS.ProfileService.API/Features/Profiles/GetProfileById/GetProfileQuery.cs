using MediatR;
using Microsoft.AspNetCore.Http;
using System;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public record GetProfileQuery(Guid? UserPublicId, int? UserId, int? ProfileId) : IRequest<IResult>;
