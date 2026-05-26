using System;
using SS.ProfileService.API.Domain.Common;

namespace SS.ProfileService.API.Domain.Entities;

public class UserProfile : BaseEntity
{
    public int UserId { get; set; }
    public Guid UserPublicId { get; set; }
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    // Navigation property for multi-address
    public ICollection<UserAddress> Addresses { get; set; } = [];
}
