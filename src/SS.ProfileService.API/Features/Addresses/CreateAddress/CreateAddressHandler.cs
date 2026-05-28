using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Ganss.Xss;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Domain.Entities;
using SS.ProfileService.API.Infrastructure.Data;

namespace SS.ProfileService.API.Features.Addresses.CreateAddress;

public class CreateAddressHandler(
    ApplicationDbContext dbContext,
    IValidator<CreateAddressCommand> validator) : IRequestHandler<CreateAddressCommand, IResult>
{
    private static readonly HtmlSanitizer Sanitizer = new();

    public async Task<IResult> Handle(CreateAddressCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var profile = await dbContext.UserProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.UserPublicId == command.UserPublicId && p.DeletedAt == null, cancellationToken);

        if (profile == null)
        {
            return Results.NotFound(new { Message = $"Profile for User (PublicId: {command.UserPublicId}) not found." });
        }

        var newAddress = new UserAddress
        {
            PublicId = Guid.NewGuid(),
            AddressLabel = Sanitizer.Sanitize(command.Address.AddressLabel),
            ReceiverName = Sanitizer.Sanitize(command.Address.ReceiverName),
            ReceiverPhone = Sanitizer.Sanitize(command.Address.ReceiverPhone),
            StreetAddress = Sanitizer.Sanitize(command.Address.StreetAddress),
            City = Sanitizer.Sanitize(command.Address.City),
            StateProvince = Sanitizer.Sanitize(command.Address.StateProvince),
            PostalCode = Sanitizer.Sanitize(command.Address.PostalCode),
            Country = Sanitizer.Sanitize(command.Address.Country),
            Latitude = command.Address.Latitude,
            Longitude = command.Address.Longitude,
            IsDefault = command.Address.IsDefault,
            CreatedBy = "System"
        };

        // Enforce only one default address.
        // Step 1: Clear existing defaults first in a separate round-trip.
        // This avoids the PostgreSQL partial unique index violation (uq_user_addresses_default_idx)
        // that occurs when INSERT of the new default and UPDATE of the old default happen in the same batch.
        if (newAddress.IsDefault)
        {
            var existingDefaults = profile.Addresses.Where(a => a.DeletedAt == null && a.IsDefault).ToList();
            if (existingDefaults.Count > 0)
            {
                foreach (var addr in existingDefaults)
                {
                    addr.IsDefault = false;
                    addr.UpdatedAt = DateTimeOffset.UtcNow;
                    addr.UpdatedBy = "System";
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else if (!profile.Addresses.Any(a => a.DeletedAt == null))
        {
            // If it is the first address, make it default
            newAddress.IsDefault = true;
        }

        // Step 2: Add new address and persist.
        profile.Addresses.Add(newAddress);
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedBy = "System";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            newAddress.PublicId,
            newAddress.AddressLabel,
            newAddress.ReceiverName,
            newAddress.ReceiverPhone,
            newAddress.StreetAddress,
            newAddress.City,
            newAddress.StateProvince,
            newAddress.PostalCode,
            newAddress.Country,
            newAddress.Latitude,
            newAddress.Longitude,
            newAddress.IsDefault
        });
    }
}
