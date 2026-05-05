using Estoria.Domain.Base;
using Estoria.Domain.Enums;

namespace Estoria.Domain.Entities;

public class UserRoleAssignment : BaseEntity
{
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }

    public User User { get; set; } = null!;
}
