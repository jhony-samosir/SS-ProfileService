using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public static class GetProfileByIdEndpoint
{
    public static void MapGetProfileByIdEndpoint(this IEndpointRouteBuilder app)
    {
        // Get by UserPublicId (UUID)
        app.MapGet("/api/profiles/{userPublicId:guid}", async (Guid userPublicId, ISender sender) =>
        {
            var query = new GetProfileQuery(UserPublicId: userPublicId, UserId: null, ProfileId: null);
            return await sender.Send(query);
        })
        .WithName("GetProfileByUserPublicId")
        .WithTags("Profiles");

        // Get by UserId (int)
        app.MapGet("/api/profiles/user/{userId:int}", async (int userId, ISender sender) =>
        {
            var query = new GetProfileQuery(UserPublicId: null, UserId: userId, ProfileId: null);
            return await sender.Send(query);
        })
        .WithName("GetProfileByUserId")
        .WithTags("Profiles");

        // Get by Database ProfileId (int)
        app.MapGet("/api/profiles/db/{id:int}", async (int id, ISender sender) =>
        {
            var query = new GetProfileQuery(UserPublicId: null, UserId: null, ProfileId: id);
            return await sender.Send(query);
        })
        .WithName("GetProfileByDbId")
        .WithTags("Profiles");
    }
}
