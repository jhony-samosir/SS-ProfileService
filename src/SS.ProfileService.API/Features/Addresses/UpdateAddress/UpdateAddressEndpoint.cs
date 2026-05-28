using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SS.ProfileService.API.Features.Addresses.Shared;

namespace SS.ProfileService.API.Features.Addresses.UpdateAddress;

public static class UpdateAddressEndpoint
{
    public static void MapUpdateAddressEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/{userPublicId:guid}/addresses/{addressPublicId:guid}", async (
            Guid userPublicId,
            Guid addressPublicId,
            [FromBody] AddressDto request,
            ISender sender) =>
        {
            var command = new UpdateAddressCommand(userPublicId, addressPublicId, request);
            var result = await sender.Send(command);
            return result;
        })
        .WithName("UpdateAddress")
        .WithTags("Addresses")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
