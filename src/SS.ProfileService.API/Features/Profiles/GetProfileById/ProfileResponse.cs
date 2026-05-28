using System;
using System.Collections.Generic;

namespace SS.ProfileService.API.Features.Profiles.GetProfileById;

public record AddressResponse(
    Guid PublicId,
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

public record ProfileResponse(
    int Id,
    Guid PublicId,
    int UserId,
    Guid UserPublicId,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Bio,
    string? Gender,
    DateOnly? DateOfBirth,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IEnumerable<AddressResponse> Addresses);
