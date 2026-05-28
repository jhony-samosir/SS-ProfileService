using System;
using System.Collections.Generic;

namespace SS.ProfileService.API.Features.Profiles.UpdateProfile;

public record AddressRequest(
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

public record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    DateOnly? DateOfBirth,
    IEnumerable<AddressRequest>? Addresses);
