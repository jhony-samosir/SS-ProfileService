using System;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace SS.ProfileService.API.Features.Addresses.DeleteAddress;

public record DeleteAddressCommand(
    Guid UserPublicId,
    Guid AddressPublicId) : IRequest<IResult>;
