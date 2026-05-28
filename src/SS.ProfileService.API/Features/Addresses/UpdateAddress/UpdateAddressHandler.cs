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
using SS.ProfileService.API.Features.Addresses.Shared;

namespace SS.ProfileService.API.Features.Addresses.UpdateAddress;

public class UpdateAddressHandler(
    ApplicationDbContext dbContext,
    IValidator<UpdateAddressCommand> validator) : IRequestHandler<UpdateAddressCommand, IResult>
{
    private static readonly HtmlSanitizer Sanitizer = new();

    public async Task<IResult> Handle(UpdateAddressCommand command, CancellationToken cancellationToken)
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

        var address = profile.Addresses
            .FirstOrDefault(a => a.PublicId == command.AddressPublicId && a.DeletedAt == null);

        if (address == null)
        {
            return Results.NotFound(new { Message = $"Address with PublicId {command.AddressPublicId} not found for this profile." });
        }

        // Sanitize string inputs
        var sanitizedAddressLabel = Sanitizer.Sanitize(command.Address.AddressLabel);
        var sanitizedReceiverName = Sanitizer.Sanitize(command.Address.ReceiverName);
        var sanitizedReceiverPhone = Sanitizer.Sanitize(command.Address.ReceiverPhone);
        var sanitizedStreetAddress = Sanitizer.Sanitize(command.Address.StreetAddress);
        var sanitizedCity = Sanitizer.Sanitize(command.Address.City);
        var sanitizedStateProvince = Sanitizer.Sanitize(command.Address.StateProvince);
        var sanitizedPostalCode = Sanitizer.Sanitize(command.Address.PostalCode);
        var sanitizedCountry = Sanitizer.Sanitize(command.Address.Country);

        // If setting as default, clear other defaults first in a separate SaveChangesAsync.
        // This avoids the PostgreSQL partial unique index violation (uq_user_addresses_default_idx)
        // when the UPDATE for the new default and the UPDATE for the old default land in the same batch.
        if (command.Address.IsDefault && !address.IsDefault)
        {
            var existingDefaults = profile.Addresses
                .Where(a => a.DeletedAt == null && a.IsDefault && a.PublicId != address.PublicId)
                .ToList();
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

        // Update address fields
        address.AddressLabel = sanitizedAddressLabel;
        address.ReceiverName = sanitizedReceiverName;
        address.ReceiverPhone = sanitizedReceiverPhone;
        address.StreetAddress = sanitizedStreetAddress;
        address.City = sanitizedCity;
        address.StateProvince = sanitizedStateProvince;
        address.PostalCode = sanitizedPostalCode;
        address.Country = sanitizedCountry;
        address.Latitude = command.Address.Latitude;
        address.Longitude = command.Address.Longitude;
        address.IsDefault = command.Address.IsDefault;
        address.UpdatedAt = DateTimeOffset.UtcNow;
        address.UpdatedBy = "System";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new AddressDto(
            address.PublicId,
            address.AddressLabel,
            address.ReceiverName,
            address.ReceiverPhone,
            address.StreetAddress,
            address.City,
            address.StateProvince,
            address.PostalCode,
            address.Country,
            address.Latitude,
            address.Longitude,
            address.IsDefault
        ));
    }
}
