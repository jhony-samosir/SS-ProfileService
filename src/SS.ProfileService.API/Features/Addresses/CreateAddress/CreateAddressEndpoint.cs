using System;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SS.ProfileService.API.Features.Addresses.Shared;

namespace SS.ProfileService.API.Features.Addresses.CreateAddress;

public static class CreateAddressEndpoint
{
    public static void MapCreateAddressEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles/{userPublicId:guid}/addresses", async (
            Guid userPublicId,
            [FromBody] AddressDto request,
            ISender sender) =>
        {
            var command = new CreateAddressCommand(userPublicId, request);
            var result = await sender.Send(command);
            return result;
        })
        .WithName("CreateAddress")
        .WithTags("Addresses")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
