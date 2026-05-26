using FluentValidation;
using Ganss.Xss;
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
    private static readonly HtmlSanitizer Sanitizer = new();

    public async Task<IResult> Handle(CreateProfileCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        // Check if profile already exists for this UserId or UserPublicId
        var existingProfile = await dbContext.UserProfiles
            .AnyAsync(p => (p.UserId == command.UserId || p.UserPublicId == command.UserPublicId) && p.DeletedAt == null, cancellationToken);
            
        if (existingProfile)
        {
            return Results.Conflict(new { Message = $"Profile for User (Id: {command.UserId} / PublicId: {command.UserPublicId}) already exists." });
        }

        // Sanitize string inputs to prevent XSS
        var sanitizedFullName = Sanitizer.Sanitize(command.FullName);
        var sanitizedPhoneNumber = command.PhoneNumber != null ? Sanitizer.Sanitize(command.PhoneNumber) : null;
        var sanitizedAvatarUrl = command.AvatarUrl != null ? Sanitizer.Sanitize(command.AvatarUrl) : null;
        var sanitizedBio = command.Bio != null ? Sanitizer.Sanitize(command.Bio) : null;

        var profile = new UserProfile
        {
            UserId = command.UserId,
            UserPublicId = command.UserPublicId,
            FullName = sanitizedFullName,
            PhoneNumber = sanitizedPhoneNumber,
            AvatarUrl = sanitizedAvatarUrl,
            Bio = sanitizedBio,
            Gender = command.Gender,
            DateOfBirth = command.DateOfBirth,
            CreatedBy = "System"
        };

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/profiles/{profile.UserPublicId}", new
        {
            profile.Id,
            profile.PublicId,
            profile.UserId,
            profile.UserPublicId,
            profile.FullName,
            profile.PhoneNumber,
            profile.AvatarUrl,
            profile.Bio,
            profile.Gender,
            profile.DateOfBirth,
            profile.CreatedAt
        });
    }
}
