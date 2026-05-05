using Estoria.Application.Common;
using Estoria.Application.DTOs.Properties;
using Estoria.Application.DTOs.Team;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class TeamService
{
    private readonly IAppDbContext _db;
    private readonly AuditService _audit;

    public TeamService(IAppDbContext db, AuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public async Task<List<TeamMemberListDto>> GetAllActiveAsync(
        Language lang, CancellationToken ct = default)
    {
        var members = await _db.TeamMembers
            .AsNoTracking()
            .Include(m => m.Translations)
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        return members.Select(m => ToListDto(m, lang)).ToList();
    }

    public async Task<TeamMemberDetailDto?> GetBySlugAsync(
        string slug, Language lang, CancellationToken ct = default)
    {
        var member = await _db.TeamMembers
            .AsNoTracking()
            .Include(m => m.Translations)
            .Include(m => m.Properties)
                .ThenInclude(p => p.Translations)
            .Include(m => m.Properties)
                .ThenInclude(p => p.Images.Where(i => i.IsCover))
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsActive, ct);

        return member is null ? null : ToDetailDto(member, lang);
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<List<TeamMemberListDto>> GetAllAdminAsync(
        Language lang, CancellationToken ct = default)
    {
        var members = await _db.TeamMembers
            .AsNoTracking()
            .Include(m => m.Translations)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        return members.Select(m => ToListDto(m, lang)).ToList();
    }

    public async Task<Guid> CreateAsync(
        CreateTeamMemberDto dto, CancellationToken ct = default)
    {
        var enName = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Name
            : dto.Translations.Values.First().Name;

        var member = new TeamMember
        {
            Slug = SlugHelper.GenerateSlug(enName),
            PhotoUrl = dto.PhotoUrl,
            Phone = dto.Phone,
            Email = dto.Email,
            Languages = dto.Languages,
            SortOrder = dto.SortOrder
        };

        foreach (var (lang, trans) in dto.Translations)
            member.Translations.Add(new TeamMemberTranslation
            {
                TeamMemberId = member.Id,
                Language = lang,
                Name = trans.Name,
                Role = trans.Role,
                Bio = trans.Bio
            });

        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Team.Create",
            entityType: nameof(TeamMember),
            entityId: member.Id,
            details: new { member.Slug, member.Email, enName },
            ct: ct);

        return member.Id;
    }

    public async Task UpdateAsync(
        Guid id, UpdateTeamMemberDto dto, CancellationToken ct = default)
    {
        var member = await _db.TeamMembers
            .Include(m => m.Translations)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new KeyNotFoundException($"TeamMember {id} not found.");

        var enName = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Name
            : dto.Translations.Values.First().Name;

        member.Slug = SlugHelper.GenerateSlug(enName);
        member.PhotoUrl = dto.PhotoUrl;
        member.Phone = dto.Phone;
        member.Email = dto.Email;
        member.Languages = dto.Languages;
        member.SortOrder = dto.SortOrder;

        _db.TeamMemberTranslations.RemoveRange(member.Translations);
        member.Translations.Clear();

        foreach (var (lang, trans) in dto.Translations)
            member.Translations.Add(new TeamMemberTranslation
            {
                TeamMemberId = member.Id,
                Language = lang,
                Name = trans.Name,
                Role = trans.Role,
                Bio = trans.Bio
            });

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Team.Update",
            entityType: nameof(TeamMember),
            entityId: member.Id,
            details: new { member.Slug, member.Email },
            ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var member = await _db.TeamMembers.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"TeamMember {id} not found.");

        member.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Team.Delete",
            entityType: nameof(TeamMember),
            entityId: member.Id,
            details: new { member.Slug },
            ct: ct);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static TeamMemberTranslation? ResolveTranslation(
        List<TeamMemberTranslation> list, Language lang)
        => list.FirstOrDefault(t => t.Language == lang)
           ?? list.FirstOrDefault(t => t.Language == Language.En)
           ?? list.FirstOrDefault();

    private static TeamMemberListDto ToListDto(TeamMember m, Language lang)
    {
        var t = ResolveTranslation(m.Translations, lang);
        return new TeamMemberListDto
        {
            Id = m.Id,
            Slug = m.Slug,
            Name = t?.Name ?? string.Empty,
            Role = t?.Role ?? string.Empty,
            PhotoUrl = m.PhotoUrl,
            Phone = m.Phone,
            Email = m.Email,
            Languages = m.Languages
        };
    }

    private static TeamMemberDetailDto ToDetailDto(TeamMember m, Language lang)
    {
        var t = ResolveTranslation(m.Translations, lang);
        return new TeamMemberDetailDto
        {
            Id = m.Id,
            Slug = m.Slug,
            Name = t?.Name ?? string.Empty,
            Role = t?.Role ?? string.Empty,
            Bio = t?.Bio,
            PhotoUrl = m.PhotoUrl,
            Phone = m.Phone,
            Email = m.Email,
            Languages = m.Languages,
            Properties = m.Properties.Select(p =>
            {
                var pt = p.Translations.FirstOrDefault(x => x.Language == lang)
                         ?? p.Translations.FirstOrDefault(x => x.Language == Language.En)
                         ?? p.Translations.FirstOrDefault();
                return new PropertyListDto
                {
                    Id = p.Id,
                    Slug = p.Slug,
                    Title = pt?.Title ?? string.Empty,
                    Address = pt?.Address ?? string.Empty,
                    City = pt?.City ?? string.Empty,
                    Price = p.Price,
                    Currency = p.Currency,
                    Size = p.Size,
                    Rooms = p.Rooms,
                    PropertyType = p.PropertyType,
                    TransactionType = p.TransactionType,
                    Status = p.Status,
                    CoverImageUrl = p.Images.FirstOrDefault()?.Url,
                    IsFeatured = p.IsFeatured
                };
            }).ToList()
        };
    }
}
