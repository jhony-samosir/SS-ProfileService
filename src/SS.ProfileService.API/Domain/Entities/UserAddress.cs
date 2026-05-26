using System;
using SS.ProfileService.API.Domain.Common;

namespace SS.ProfileService.API.Domain.Entities;

public class UserAddress : BaseEntity
{
    public int UserProfileId { get; set; }
    public string AddressLabel { get; set; } = "Home";
    public required string ReceiverName { get; set; }
    public required string ReceiverPhone { get; set; }
    public required string StreetAddress { get; set; }
    public required string City { get; set; }
    public required string StateProvince { get; set; }
    public required string PostalCode { get; set; }
    public string Country { get; set; } = "Indonesia";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }

    // Navigation property
    public UserProfile? UserProfile { get; set; }
}
