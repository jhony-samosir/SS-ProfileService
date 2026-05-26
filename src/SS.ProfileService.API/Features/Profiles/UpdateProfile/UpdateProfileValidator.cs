using FluentValidation;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.FullName).NotEmpty().WithMessage("FullName is required.").MaximumLength(255);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.AvatarUrl).MaximumLength(1000);
    }
}
