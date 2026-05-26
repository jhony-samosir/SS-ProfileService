using System;
using SS.ProfileService.API.Domain.Common;

namespace SS.ProfileService.API.Domain.Entities;

public class Profile : BaseEntity
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
}
