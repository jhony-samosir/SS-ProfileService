using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Infrastructure.Data;
using System;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public class UpdateProfileHandler(
    ApplicationDbContext dbContext,
    IValidator<UpdateProfileCommand> validator) : IRequestHandler<UpdateProfileCommand, IResult>
{
    public async Task<IResult> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var profile = await dbContext.Profiles
            .FirstOrDefaultAsync(p => p.UserId == command.UserId && p.DeletedAt == null, cancellationToken);

        if (profile == null)
        {
            return Results.NotFound(new { Message = $"Profile for UserId '{command.UserId}' not found." });
        }

        // Apply changes
        profile.FullName = command.FullName;
        profile.PhoneNumber = command.PhoneNumber;
        profile.Address = command.Address;
        profile.AvatarUrl = command.AvatarUrl;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedBy = "System";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            profile.Id,
            profile.PublicId,
            profile.UserId,
            profile.FullName,
            profile.PhoneNumber,
            profile.Address,
            profile.AvatarUrl,
            profile.UpdatedAt
        });
    }
}
