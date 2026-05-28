using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SS.ProfileService.API.Features.Addresses.SetDefaultAddress;

public static class SetDefaultAddressEndpoint
{
    public static void MapSetDefaultAddressEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/{userPublicId:guid}/addresses/{addressPublicId:guid}/default", async (
            Guid userPublicId,
            Guid addressPublicId,
            ISender sender) =>
        {
            var command = new SetDefaultAddressCommand(userPublicId, addressPublicId);
            var result = await sender.Send(command);
            return result;
        })
        .WithName("SetDefaultAddress")
        .WithTags("Addresses")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
