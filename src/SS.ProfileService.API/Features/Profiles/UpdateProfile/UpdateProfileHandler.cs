using FluentValidation;
using Ganss.Xss;
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
    private static readonly HtmlSanitizer Sanitizer = new();

    public async Task<IResult> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserPublicId == command.UserPublicId && p.DeletedAt == null, cancellationToken);

        if (profile == null)
        {
            return Results.NotFound(new { Message = $"Profile for User (PublicId: {command.UserPublicId}) not found." });
        }

        // Sanitize string inputs to prevent XSS
        var sanitizedFullName = Sanitizer.Sanitize(command.FullName);
        var sanitizedPhoneNumber = command.PhoneNumber != null ? Sanitizer.Sanitize(command.PhoneNumber) : null;
        var sanitizedAvatarUrl = command.AvatarUrl != null ? Sanitizer.Sanitize(command.AvatarUrl) : null;
        var sanitizedBio = command.Bio != null ? Sanitizer.Sanitize(command.Bio) : null;

        // Apply changes
        profile.FullName = sanitizedFullName;
        profile.PhoneNumber = sanitizedPhoneNumber;
        profile.AvatarUrl = sanitizedAvatarUrl;
        profile.Bio = sanitizedBio;
        profile.Gender = command.Gender;
        profile.DateOfBirth = command.DateOfBirth;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedBy = "System";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
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
            profile.UpdatedAt
        });
    }
}
