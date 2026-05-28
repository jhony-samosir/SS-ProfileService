using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;

namespace SS.ProfileService.API.Features.Addresses.SetDefaultAddress;

public class SetDefaultAddressHandler(ApplicationDbContext dbContext) : IRequestHandler<SetDefaultAddressCommand, IResult>
{
    public async Task<IResult> Handle(SetDefaultAddressCommand command, CancellationToken cancellationToken)
    {
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

        // Step 1: Clear all existing defaults first and persist immediately.
        // This is done in a separate round-trip to avoid a race condition where
        // PostgreSQL enforces the partial unique index (uq_user_addresses_default_idx)
        // row-by-row within the same batch, causing a duplicate key violation.
        var existingDefaults = profile.Addresses
            .Where(a => a.DeletedAt == null && a.IsDefault && a.PublicId != command.AddressPublicId)
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

        // Step 2: Now safely set the selected address as the default.
        address.IsDefault = true;
        address.UpdatedAt = DateTimeOffset.UtcNow;
        address.UpdatedBy = "System";

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedBy = "System";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { Message = "Default address updated successfully." });
    }
}
