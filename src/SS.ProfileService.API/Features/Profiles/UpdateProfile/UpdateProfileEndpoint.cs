using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public static class UpdateProfileEndpoint
{
    public static void MapUpdateProfileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/{userId:guid}", async (Guid userId, UpdateProfileRequest request, ISender sender) =>
        {
            var command = new UpdateProfileCommand(
                userId,
                request.FullName,
                request.PhoneNumber,
                request.Address,
                request.AvatarUrl);

            return await sender.Send(command);
        })
        .WithName("UpdateProfile")
        .WithTags("Profiles");
    }
}
