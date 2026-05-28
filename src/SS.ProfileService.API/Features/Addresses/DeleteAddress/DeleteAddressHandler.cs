using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;

namespace SS.ProfileService.API.Features.Addresses.DeleteAddress;

public class DeleteAddressHandler(ApplicationDbContext dbContext) : IRequestHandler<DeleteAddressCommand, IResult>
{
    public async Task<IResult> Handle(DeleteAddressCommand command, CancellationToken cancellationToken)
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

        // Soft delete
        address.DeletedAt = DateTimeOffset.UtcNow;
        address.DeletedBy = "System";

        // If deleted address was default, assign default to next active address
        if (address.IsDefault)
        {
            var nextActive = profile.Addresses
                .FirstOrDefault(a => a.DeletedAt == null && a.PublicId != address.PublicId);
            if (nextActive != null)
            {
                nextActive.IsDefault = true;
                nextActive.UpdatedAt = DateTimeOffset.UtcNow;
                nextActive.UpdatedBy = "System";
            }
        }

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedBy = "System";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
