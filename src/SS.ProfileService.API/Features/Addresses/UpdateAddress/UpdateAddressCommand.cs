using System;
using MediatR;
using Microsoft.AspNetCore.Http;
using SS.ProfileService.API.Features.Addresses.Shared;

namespace SS.ProfileService.API.Features.Addresses.UpdateAddress;

public record UpdateAddressCommand(
    Guid UserPublicId,
    Guid AddressPublicId,
    AddressDto Address) : IRequest<IResult>;
