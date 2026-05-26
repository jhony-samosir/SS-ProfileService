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
        app.MapGet("/api/profiles/{userId:guid}", async (Guid userId, ISender sender) =>
        {
            var query = new GetProfileQuery(UserId: userId, Id: null);
            return await sender.Send(query);
        })
        .WithName("GetProfileByUserId")
        .WithTags("Profiles");

        app.MapGet("/api/profiles/db/{id:int}", async (int id, ISender sender) =>
        {
            var query = new GetProfileQuery(UserId: null, Id: id);
            return await sender.Send(query);
        })
        .WithName("GetProfileByDbId")
        .WithTags("Profiles");
    }
}
