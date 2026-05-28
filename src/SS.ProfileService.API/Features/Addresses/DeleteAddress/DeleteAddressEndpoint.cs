using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SS.ProfileService.API.Features.Addresses.DeleteAddress;

public static class DeleteAddressEndpoint
{
    public static void MapDeleteAddressEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/profiles/{userPublicId:guid}/addresses/{addressPublicId:guid}", async (
            Guid userPublicId,
            Guid addressPublicId,
            ISender sender) =>
        {
            var command = new DeleteAddressCommand(userPublicId, addressPublicId);
            var result = await sender.Send(command);
            return result;
        })
        .WithName("DeleteAddress")
        .WithTags("Addresses")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
