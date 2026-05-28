using System;

namespace SS.ProfileService.API.Features.Addresses.Shared;

public record AddressDto(
    Guid? PublicId,
    string AddressLabel,
    string ReceiverName,
    string ReceiverPhone,
    string StreetAddress,
    string City,
    string StateProvince,
    string PostalCode,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    bool IsDefault);
