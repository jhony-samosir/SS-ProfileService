using FluentValidation;
using SS.ProfileService.API.Features.Addresses.Shared;

namespace SS.ProfileService.API.Features.Addresses.CreateAddress;

public class CreateAddressValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.UserPublicId).NotEmpty().WithMessage("UserPublicId is required.");
        RuleFor(x => x.Address).NotNull().WithMessage("Address details are required.");

        RuleFor(x => x.Address.AddressLabel).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address.ReceiverName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Address.ReceiverPhone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address.StreetAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Address.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address.StateProvince).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Address.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address.Latitude).InclusiveBetween(-90, 90).When(x => x.Address.Latitude.HasValue);
        RuleFor(x => x.Address.Longitude).InclusiveBetween(-180, 180).When(x => x.Address.Longitude.HasValue);
    }
}
