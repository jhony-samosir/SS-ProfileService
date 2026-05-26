using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public class GetProfileByIdHandler(ApplicationDbContext dbContext) : IRequestHandler<GetProfileQuery, IResult>
{
    public async Task<IResult> Handle(GetProfileQuery query, CancellationToken cancellationToken)
    {
        var profileQuery = dbContext.Profiles.AsNoTracking().Where(p => p.DeletedAt == null);

        if (query.UserId.HasValue)
        {
            profileQuery = profileQuery.Where(p => p.UserId == query.UserId.Value);
        }
        else if (query.Id.HasValue)
        {
            profileQuery = profileQuery.Where(p => p.Id == query.Id.Value);
        }
        else
        {
            return Results.BadRequest(new { Message = "Either UserId or profile Id must be provided." });
        }

        var profile = await profileQuery.Select(p => new ProfileResponse(
            p.Id,
            p.PublicId,
            p.UserId,
            p.FullName,
            p.PhoneNumber,
            p.Address,
            p.AvatarUrl,
            p.CreatedAt,
            p.UpdatedAt
        )).FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            return Results.NotFound(new { Message = "Profile not found." });
        }

        return Results.Ok(profile);
    }
}
