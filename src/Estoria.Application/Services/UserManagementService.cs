using Estoria.Application.Common;
using Estoria.Application.DTOs.Admin.Users;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class UserManagementService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly AuditService _audit;

    public UserManagementService(IAppDbContext db, IPasswordHasher hasher, AuditService audit)
    {
        _db     = db;
        _hasher = hasher;
        _audit  = audit;
    }

    public async Task<PagedResult<UserListDto>> GetListAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<User> query = _db.Users
            .AsNoTracking()
            .Include(u => u.RoleAssignments);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(term)
                                  || u.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<UserListDto>
        {
            Items = users.Select(ToListDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<UserDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        return user is null ? null : ToDetailDto(user);
    }

    public async Task<Guid> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var emailNormalized = dto.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized, ct))
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists.");

        var user = new User
        {
            Email        = emailNormalized,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName     = dto.FullName.Trim(),
            Phone        = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            PhotoUrl     = dto.PhotoUrl,
            Languages    = dto.Languages?.ToList() ?? [],
            TeamMemberId = dto.TeamMemberId,
            IsActive     = true,
        };

        foreach (var role in dto.Roles.Distinct())
            user.RoleAssignments.Add(new UserRoleAssignment { Role = role });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "User.Create",
            entityType: nameof(User),
            entityId: user.Id,
            details: new { user.Email, user.FullName, Roles = dto.Roles.Select(r => r.ToString()).ToArray() },
            ct: ct);

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var oldRoles = user.RoleAssignments.Select(a => a.Role).ToArray();

        user.FullName     = dto.FullName.Trim();
        user.Phone        = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        user.PhotoUrl     = dto.PhotoUrl;
        user.Languages    = dto.Languages?.ToList() ?? [];
        user.TeamMemberId = dto.TeamMemberId;
        user.IsActive     = dto.IsActive;

        // Roles: full-replace via diff. Naive remove-all-then-add-all collides
        // with the composite unique index (UserId, Role) when EF batches the
        // INSERTs before the DELETEs commit, so we only touch the rows that
        // actually changed.
        var existingRoles = user.RoleAssignments.Select(a => a.Role).ToHashSet();
        var newRoles      = dto.Roles.Distinct().ToHashSet();

        foreach (var assignment in user.RoleAssignments.Where(a => !newRoles.Contains(a.Role)).ToList())
            _db.UserRoles.Remove(assignment);

        // Add through DbSet so EF marks as Added — adding through the
        // tracked navigation collection promotes a newly-constructed entity
        // (Id pre-populated by BaseEntity constructor) to Modified state,
        // which then UPDATEs a row that doesn't exist.
        foreach (var role in newRoles.Where(r => !existingRoles.Contains(r)))
            _db.UserRoles.Add(new UserRoleAssignment { UserId = user.Id, Role = role });

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "User.Update",
            entityType: nameof(User),
            entityId: user.Id,
            details: new
            {
                user.Email,
                user.FullName,
                user.IsActive,
                OldRoles = oldRoles.Select(r => r.ToString()).ToArray(),
                NewRoles = dto.Roles.Select(r => r.ToString()).ToArray(),
            },
            ct: ct);
    }

    public async Task SetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        user.PasswordHash = _hasher.Hash(newPassword);
        await _db.SaveChangesAsync(ct);

        // Don't log the password — just that it was reset.
        await _audit.LogAsync(
            "User.PasswordReset",
            entityType: nameof(User),
            entityId: user.Id,
            details: new { user.Email },
            ct: ct);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        if (!user.IsActive) return; // already soft-deleted, no-op

        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "User.Deactivate",
            entityType: nameof(User),
            entityId: user.Id,
            details: new { user.Email },
            ct: ct);
    }

    // -------------------------------------------------------------------------
    // Mappers
    // -------------------------------------------------------------------------

    private static UserListDto ToListDto(User u) => new()
    {
        Id          = u.Id,
        Email       = u.Email,
        FullName    = u.FullName,
        Roles       = u.RoleAssignments.Select(a => a.Role.ToString()).ToArray(),
        PhotoUrl    = u.PhotoUrl,
        Languages   = u.Languages.ToArray(),
        IsActive    = u.IsActive,
        LastLoginAt = u.LastLoginAt,
        CreatedAt   = u.CreatedAt,
    };

    private static UserDetailDto ToDetailDto(User u) => new()
    {
        Id           = u.Id,
        Email        = u.Email,
        FullName     = u.FullName,
        Phone        = u.Phone,
        PhotoUrl     = u.PhotoUrl,
        Roles        = u.RoleAssignments.Select(a => a.Role.ToString()).ToArray(),
        Languages    = [.. u.Languages],
        IsActive     = u.IsActive,
        TeamMemberId = u.TeamMemberId,
        LastLoginAt  = u.LastLoginAt,
        CreatedAt    = u.CreatedAt,
        UpdatedAt    = u.UpdatedAt,
    };
}
