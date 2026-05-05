using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;

namespace Estoria.Application.Common;

/// <summary>
/// Thrown by services when the current user lacks permission for an operation.
/// Controllers translate this into 403 Forbidden via ExceptionHandlingMiddleware.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>
/// Centralizes the role / ownership checks the CRM services share.
/// Stateless — created per service via DI.
/// </summary>
public class AuthorizationGuard
{
    private readonly ICurrentUserService _currentUser;

    public AuthorizationGuard(ICurrentUserService currentUser) => _currentUser = currentUser;

    public Guid CurrentUserId =>
        _currentUser.UserId
        ?? throw new ForbiddenException("Operation requires an authenticated user.");

    public bool IsAdmin => _currentUser.IsInRole(UserRole.Admin);
    public bool IsAgent => _currentUser.IsInRole(UserRole.Agent);

    /// <summary>
    /// Allow the action only if the caller is the resource owner OR an Admin.
    /// Raises ForbiddenException otherwise.
    /// </summary>
    public void RequireOwnershipOrAdmin(Guid ownerId)
    {
        if (IsAdmin) return;
        if (CurrentUserId == ownerId) return;
        throw new ForbiddenException("You can only modify resources you own.");
    }

    /// <summary>
    /// Block the action when the caller is read-only on this domain.
    /// Editor and Marketing roles are read-only on deals/contacts/activities;
    /// Admin and Agent get write access.
    /// </summary>
    public void RequireWriteAccess()
    {
        if (IsAdmin || IsAgent) return;

        // Editors / Marketing land here. Anonymous principals are blocked
        // earlier by [Authorize] on the controller, so we don't need to
        // distinguish "no role" here.
        throw new ForbiddenException("This role has read-only access to CRM data.");
    }
}
