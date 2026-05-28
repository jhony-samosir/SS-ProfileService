using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public static class UpdateProfileEndpoint
{
    public static void MapUpdateProfileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/{userPublicId:guid}", async (
            Guid userPublicId,
            [FromBody] UpdateProfileRequest request,
            ISender sender) =>
        {
            var command = new UpdateProfileCommand(
                userPublicId,
                request.FullName,
                request.PhoneNumber,
                request.AvatarUrl,
                request.Bio,
                request.Gender,
                request.DateOfBirth,
                request.Addresses);

            var result = await sender.Send(command);
            return result;
        })
        .WithName("UpdateProfile")
        .WithTags("Profiles")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
