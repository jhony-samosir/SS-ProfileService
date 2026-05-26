using FluentValidation;
using System;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    private static readonly string[] AllowedGenders = ["Male", "Female", "Other", "PreferNotToSay"];

    public UpdateProfileValidator()
    {
        RuleFor(x => x.UserPublicId).NotEmpty().WithMessage("UserPublicId is required.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("FullName is required.").MaximumLength(255);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.AvatarUrl).MaximumLength(1000);
        RuleFor(x => x.Bio).MaximumLength(500);
        
        RuleFor(x => x.Gender)
            .Must(gender => gender == null || AllowedGenders.Contains(gender))
            .WithMessage("Gender must be one of: Male, Female, Other, PreferNotToSay.");
            
        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob == null || dob.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
    }
}
