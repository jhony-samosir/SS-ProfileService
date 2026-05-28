using System;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace SS.ProfileService.API.Features.Addresses.SetDefaultAddress;

public record SetDefaultAddressCommand(
    Guid UserPublicId,
    Guid AddressPublicId) : IRequest<IResult>;
