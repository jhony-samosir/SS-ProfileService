using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Domain.Entities;
using SS.ProfileService.API.Infrastructure.Data;

namespace SS.ProfileService.API.Features.Profiles.CreateProfile;

public class CreateProfileHandler(
    ApplicationDbContext dbContext,
    IValidator<CreateProfileCommand> validator) : IRequestHandler<CreateProfileCommand, IResult>
{
    public async Task<IResult> Handle(CreateProfileCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Check if profile already exists for this UserId
        var existingProfile = await dbContext.Profiles
            .AnyAsync(p => p.UserId == command.UserId && p.DeletedAt == null, cancellationToken);
            
        if (existingProfile)
        {
            return Results.Conflict(new { Message = $"Profile for UserId '{command.UserId}' already exists." });
        }

        var profile = new Profile
        {
            UserId = command.UserId,
            FullName = command.FullName,
            PhoneNumber = command.PhoneNumber,
            Address = command.Address,
            AvatarUrl = command.AvatarUrl,
            CreatedBy = "System"
        };

        dbContext.Profiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/profiles/{profile.UserId}", new
        {
            profile.Id,
            profile.PublicId,
            profile.UserId,
            profile.FullName,
            profile.PhoneNumber,
            profile.Address,
            profile.AvatarUrl,
            profile.CreatedAt
        });
    }
}
