using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public class GetProfileByIdHandler(ApplicationDbContext dbContext) : IRequestHandler<GetProfileQuery, IResult>
{
    public async Task<IResult> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        var profileQuery = dbContext.UserProfiles.AsNoTracking().Where(p => p.DeletedAt == null);

        if (query.UserPublicId.HasValue)
        {
            profileQuery = profileQuery.Where(p => p.UserPublicId == query.UserPublicId.Value);
        }
        else if (query.UserId.HasValue)
        {
            profileQuery = profileQuery.Where(p => p.UserId == query.UserId.Value);
        }
        else if (query.ProfileId.HasValue)
        {
            profileQuery = profileQuery.Where(p => p.Id == query.ProfileId.Value);
        }
        else
        {
            return Results.BadRequest(new { Message = "Either UserId, UserPublicId, or ProfileId must be provided." });
        }

        var profile = await profileQuery
            .Include(p => p.Addresses.Where(a => a.DeletedAt == null))
            .Select(p => new ProfileResponse(
            p.Id,
            p.PublicId,
            p.UserId,
            p.UserPublicId,
            p.FullName,
            p.PhoneNumber,
            p.AvatarUrl,
            p.Bio,
            p.Gender,
            p.DateOfBirth,
            p.CreatedAt,
            p.UpdatedAt,
            p.Addresses.Select(a => new AddressResponse(
                a.PublicId,
                a.AddressLabel,
                a.ReceiverName,
                a.ReceiverPhone,
                a.StreetAddress,
                a.City,
                a.StateProvince,
                a.PostalCode,
                a.Country,
                a.Latitude,
                a.Longitude,
                a.IsDefault
            )).ToList()
        )).FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            return Results.NotFound(new { Message = "Profile not found." });
        }

        return Results.Ok(profile);
    }
}
