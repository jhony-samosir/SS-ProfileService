using System;
using MediatR;
using Microsoft.AspNetCore.Http;
using SS.ProfileService.API.Features.Addresses.Shared;

namespace SS.ProfileService.API.Features.Addresses.CreateAddress;

public record CreateAddressCommand(
    Guid UserPublicId,
    AddressDto Address) : IRequest<IResult>;
