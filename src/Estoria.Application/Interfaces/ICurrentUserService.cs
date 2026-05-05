using Estoria.Domain.Enums;

namespace Estoria.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string[] Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(UserRole role);
}
