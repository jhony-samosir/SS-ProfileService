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

        RuleForEach(x => x.Addresses).ChildRules(address =>
        {
            address.RuleFor(x => x.AddressLabel).NotEmpty().MaximumLength(100);
            address.RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(255);
            address.RuleFor(x => x.ReceiverPhone).NotEmpty().MaximumLength(50);
            address.RuleFor(x => x.StreetAddress).NotEmpty().MaximumLength(500);
            address.RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            address.RuleFor(x => x.StateProvince).NotEmpty().MaximumLength(100);
            address.RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
            address.RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            address.RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
            address.RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        });

        RuleFor(x => x.Addresses)
            .Must(addresses => addresses == null || addresses.Count(a => a.IsDefault) <= 1)
            .WithMessage("Only one default address is allowed.");
    }
}
