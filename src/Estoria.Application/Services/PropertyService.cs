using System.Text.Json;
using Estoria.Application.Common;
using Estoria.Application.DTOs.Properties;
using Estoria.Application.DTOs.Team;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Estoria.Application.Services;

public class PropertyService
{
    private static readonly JsonSerializerOptions EventJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly IAppDbContext _db;
    private readonly AuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PropertyService> _logger;

    public PropertyService(
        IAppDbContext db,
        AuditService audit,
        ICurrentUserService currentUser,
        ILogger<PropertyService> logger)
    {
        _db          = db;
        _audit       = audit;
        _currentUser = currentUser;
        _logger      = logger;
    }

    // -------------------------------------------------------------------------
    // PropertyEvent helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Append a <see cref="PropertyEvent"/> row. Wrapped in try/catch by
    /// design — a failed history entry is a degraded feature, not a broken
    /// save, so the property mutation that triggered the event must still
    /// succeed even if the event row can't be written. Saves immediately to
    /// avoid coupling failures with the parent transaction.
    /// </summary>
    public async Task LogPropertyEventAsync(
        Guid propertyId,
        PropertyEventType type,
        object? prev,
        object? next,
        Guid? userId,
        CancellationToken ct = default)
    {
        try
        {
            _db.PropertyEvents.Add(new PropertyEvent
            {
                PropertyId   = propertyId,
                Type         = type,
                PreviousJson = SerializePayload(prev),
                NewJson      = SerializePayload(next),
                UserId       = userId,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "PROPERTY_EVENT_FAIL propertyId={PropertyId} type={Type} — degraded but property change persisted",
                propertyId, type);
        }
    }

    private static string? SerializePayload(object? payload)
    {
        if (payload is null) return null;
        try { return JsonSerializer.Serialize(payload, EventJsonOptions); }
        catch { return null; }
    }

    // -------------------------------------------------------------------------
    // Public
    // -------------------------------------------------------------------------

    public async Task<PagedResult<PropertyListDto>> GetListAsync(
        Language lang,
        PropertyFilterDto? filter,
        int page = 1,
        int pageSize = 12,
        CancellationToken ct = default)
    {
        var query = _db.Properties
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Images.Where(i => i.IsCover))
            .Where(p => p.Status == PropertyStatus.Active);

        if (filter is not null)
        {
            if (filter.Type.HasValue)
                query = query.Where(p => p.PropertyType == filter.Type.Value);
            if (filter.Transaction.HasValue)
                query = query.Where(p => p.TransactionType == filter.Transaction.Value);
            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(p => p.Translations.Any(t => t.City.ToLower().Contains(filter.City.ToLower())));
            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            if (filter.MinSize.HasValue)
                query = query.Where(p => p.Size >= filter.MinSize.Value);
            if (filter.MaxSize.HasValue)
                query = query.Where(p => p.Size <= filter.MaxSize.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<PropertyListDto>
        {
            Items = items.Select(p => ToListDto(p, lang)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PropertyDetailDto?> GetBySlugAsync(
        string slug, Language lang, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Features)
            .Include(p => p.Agent).ThenInclude(a => a.Translations)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

        return property is null ? null : ToDetailDto(property, lang);
    }

    /// <summary>
    /// Returns the most recent <paramref name="limit"/> events for the property,
    /// newest first. Public-safe: only the event types that make sense for a
    /// visitor are returned (price/status/featured) — agent and image events
    /// are filtered out at the DB level so we don't even pull them.
    /// </summary>
    public async Task<List<PropertyEventDto>?> GetPublicHistoryAsync(
        string slug, int limit = 20, CancellationToken ct = default)
    {
        var propertyId = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);

        if (propertyId is null) return null;

        // Visitor-safe whitelist. Image events would expose URL churn that the
        // history widget shouldn't display; agent changes are admin context.
        var publicTypes = new[]
        {
            PropertyEventType.Created,
            PropertyEventType.PriceChanged,
            PropertyEventType.StatusChanged,
            PropertyEventType.PublishedFirst,
            PropertyEventType.Featured,
            PropertyEventType.Unfeatured,
        };

        limit = Math.Clamp(limit, 1, 100);

        var rows = await _db.PropertyEvents
            .AsNoTracking()
            .Where(e => e.PropertyId == propertyId.Value && publicTypes.Contains(e.Type))
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(e => new PropertyEventDto
        {
            Id           = e.Id,
            Type         = e.Type,
            CreatedAt    = e.CreatedAt,
            PreviousJson = e.PreviousJson,
            NewJson      = e.NewJson,
        }).ToList();
    }

    public async Task<List<PropertyListDto>> GetFeaturedAsync(
        Language lang, int count = 6, CancellationToken ct = default)
    {
        var items = await _db.Properties
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Images.Where(i => i.IsCover))
            .Where(p => p.IsFeatured && p.Status == PropertyStatus.Active)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .ToListAsync(ct);

        return items.Select(p => ToListDto(p, lang)).ToList();
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<PagedResult<AdminPropertyDetailDto>> GetAllAdminAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Properties
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Images)
            .Include(p => p.Features)
            .Include(p => p.Agent).ThenInclude(a => a.Translations)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AdminPropertyDetailDto>
        {
            Items = items.Select(ToAdminDetailDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminPropertyDetailDto?> GetByIdAdminAsync(
        Guid id, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Features)
            .Include(p => p.Agent).ThenInclude(a => a.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return property is null ? null : ToAdminDetailDto(property);
    }

    public async Task<Guid> CreateAsync(
        CreatePropertyDto dto, CancellationToken ct = default)
    {
        var enTitle = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Title
            : dto.Translations.Values.First().Title;

        var property = new Property
        {
            Slug = SlugHelper.GenerateSlug(enTitle),
            TransactionType = dto.TransactionType,
            PropertyType = dto.PropertyType,
            Price = dto.Price,
            Currency = dto.Currency,
            Size = dto.Size,
            Rooms = dto.Rooms,
            Bedrooms = dto.Bedrooms,
            Bathrooms = dto.Bathrooms,
            Floor = dto.Floor,
            TotalFloors = dto.TotalFloors,
            YearBuilt = dto.YearBuilt,
            EnergyClass = dto.EnergyClass,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsFeatured = dto.IsFeatured,
            AgentId = dto.AgentId
        };

        foreach (var (lang, trans) in dto.Translations)
            property.Translations.Add(new PropertyTranslation
            {
                PropertyId = property.Id,
                Language = lang,
                Title = trans.Title,
                Description = trans.Description,
                Address = trans.Address,
                City = trans.City,
                District = trans.District
            });

        foreach (var feature in dto.Features)
            property.Features.Add(new PropertyFeature
            {
                PropertyId = property.Id,
                Feature = feature
            });

        _db.Properties.Add(property);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Property.Create",
            entityType: nameof(Property),
            entityId: property.Id,
            details: new { property.Slug, property.PropertyType, property.TransactionType, property.Price },
            ct: ct);

        await LogPropertyEventAsync(
            property.Id,
            PropertyEventType.Created,
            prev: null,
            next: new
            {
                property.Slug,
                property.PropertyType,
                property.TransactionType,
                property.Price,
                property.Currency,
                property.Status,
                property.IsFeatured,
                property.AgentId,
            },
            userId: _currentUser.UserId,
            ct: ct);

        return property.Id;
    }

    public async Task UpdateAsync(
        Guid id, UpdatePropertyDto dto, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Translations)
            .Include(p => p.Features)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException($"Property {id} not found.");

        // Capture before-state once so we can diff after persist and emit
        // the right PropertyEvent rows. Storing scalars only — Translations
        // / Features churn isn't part of the public history widget.
        var prevPrice      = property.Price;
        var prevAgentId    = property.AgentId;
        var prevIsFeatured = property.IsFeatured;
        var prevCurrency   = property.Currency;

        var enTitle = dto.Translations.TryGetValue(Language.En, out var enTrans)
            ? enTrans.Title
            : dto.Translations.Values.First().Title;

        property.Slug = SlugHelper.GenerateSlug(enTitle);
        property.TransactionType = dto.TransactionType;
        property.PropertyType = dto.PropertyType;
        property.Price = dto.Price;
        property.Currency = dto.Currency;
        property.Size = dto.Size;
        property.Rooms = dto.Rooms;
        property.Bedrooms = dto.Bedrooms;
        property.Bathrooms = dto.Bathrooms;
        property.Floor = dto.Floor;
        property.TotalFloors = dto.TotalFloors;
        property.YearBuilt = dto.YearBuilt;
        property.EnergyClass = dto.EnergyClass;
        property.Latitude = dto.Latitude;
        property.Longitude = dto.Longitude;
        property.IsFeatured = dto.IsFeatured;
        property.AgentId = dto.AgentId;

        _db.PropertyTranslations.RemoveRange(property.Translations);
        _db.PropertyFeatures.RemoveRange(property.Features);
        property.Translations.Clear();
        property.Features.Clear();

        foreach (var (lang, trans) in dto.Translations)
            property.Translations.Add(new PropertyTranslation
            {
                PropertyId = property.Id,
                Language = lang,
                Title = trans.Title,
                Description = trans.Description,
                Address = trans.Address,
                City = trans.City,
                District = trans.District
            });

        foreach (var feature in dto.Features)
            property.Features.Add(new PropertyFeature
            {
                PropertyId = property.Id,
                Feature = feature
            });

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Property.Update",
            entityType: nameof(Property),
            entityId: property.Id,
            details: new { property.Slug, property.PropertyType, property.TransactionType, property.Price },
            ct: ct);

        var actorId = _currentUser.UserId;

        if (prevPrice != property.Price)
        {
            await LogPropertyEventAsync(
                property.Id,
                PropertyEventType.PriceChanged,
                prev: new { Price = prevPrice, Currency = prevCurrency },
                next: new { property.Price, property.Currency },
                userId: actorId,
                ct: ct);
        }

        if (prevIsFeatured != property.IsFeatured)
        {
            await LogPropertyEventAsync(
                property.Id,
                property.IsFeatured ? PropertyEventType.Featured : PropertyEventType.Unfeatured,
                prev: new { IsFeatured = prevIsFeatured },
                next: new { property.IsFeatured },
                userId: actorId,
                ct: ct);
        }

        if (prevAgentId != property.AgentId)
        {
            await LogPropertyEventAsync(
                property.Id,
                PropertyEventType.AgentChanged,
                prev: new { AgentId = prevAgentId },
                next: new { property.AgentId },
                userId: actorId,
                ct: ct);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var property = await _db.Properties.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Property {id} not found.");

        var prevStatus = property.Status;
        property.Status = PropertyStatus.Archived;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "Property.Delete",
            entityType: nameof(Property),
            entityId: property.Id,
            details: new { property.Slug },
            ct: ct);

        if (prevStatus != property.Status)
        {
            await LogPropertyEventAsync(
                property.Id,
                PropertyEventType.StatusChanged,
                prev: new { Status = prevStatus },
                next: new { property.Status },
                userId: _currentUser.UserId,
                ct: ct);
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static PropertyTranslation? ResolveTranslation(
        List<PropertyTranslation> list, Language lang)
        => list.FirstOrDefault(t => t.Language == lang)
           ?? list.FirstOrDefault(t => t.Language == Language.En)
           ?? list.FirstOrDefault();

    private static TeamMemberListDto MapAgent(TeamMember agent, Language lang)
    {
        var t = agent.Translations.FirstOrDefault(x => x.Language == lang)
                ?? agent.Translations.FirstOrDefault(x => x.Language == Language.En)
                ?? agent.Translations.FirstOrDefault();
        return new TeamMemberListDto
        {
            Id = agent.Id,
            Slug = agent.Slug,
            Name = t?.Name ?? string.Empty,
            Role = t?.Role ?? string.Empty,
            PhotoUrl = agent.PhotoUrl,
            Phone = agent.Phone,
            Email = agent.Email,
            Languages = agent.Languages
        };
    }

    private static PropertyListDto ToListDto(Property p, Language lang)
    {
        var t = ResolveTranslation(p.Translations, lang);
        var cover = p.Images.FirstOrDefault();
        return new PropertyListDto
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = t?.Title ?? string.Empty,
            Address = t?.Address ?? string.Empty,
            City = t?.City ?? string.Empty,
            Price = p.Price,
            Currency = p.Currency,
            Size = p.Size,
            Rooms = p.Rooms,
            PropertyType = p.PropertyType,
            TransactionType = p.TransactionType,
            Status = p.Status,
            CoverImageUrl  = cover?.Url,
            CoverThumbUrl  = cover?.ThumbUrl,
            CoverMediumUrl = cover?.MediumUrl,
            CoverLargeUrl  = cover?.LargeUrl,
            IsFeatured = p.IsFeatured
        };
    }

    private static PropertyDetailDto ToDetailDto(Property p, Language lang)
    {
        var t = ResolveTranslation(p.Translations, lang);
        return new PropertyDetailDto
        {
            Id = p.Id,
            Slug = p.Slug,
            Title = t?.Title ?? string.Empty,
            Address = t?.Address ?? string.Empty,
            City = t?.City ?? string.Empty,
            District = t?.District,
            Description = t?.Description ?? string.Empty,
            Price = p.Price,
            Currency = p.Currency,
            Size = p.Size,
            Rooms = p.Rooms,
            Bedrooms = p.Bedrooms,
            Bathrooms = p.Bathrooms,
            Floor = p.Floor,
            TotalFloors = p.TotalFloors,
            YearBuilt = p.YearBuilt,
            EnergyClass = p.EnergyClass,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            PropertyType = p.PropertyType,
            TransactionType = p.TransactionType,
            Status = p.Status,
            IsFeatured = p.IsFeatured,
            PublishedAt = p.PublishedAt,
            CoverImageUrl = p.Images.FirstOrDefault(i => i.IsCover)?.Url,
            Features = p.Features.Select(f => f.Feature).ToList(),
            Images = p.Images.Select(i => new PropertyImageDto
            {
                Id               = i.Id,
                Url              = i.Url,
                ThumbUrl         = i.ThumbUrl,
                MediumUrl        = i.MediumUrl,
                LargeUrl         = i.LargeUrl,
                ProcessingStatus = i.ProcessingStatus,
                ProcessingError  = i.ProcessingError,
                AltText          = i.AltText,
                SortOrder        = i.SortOrder,
                IsCover          = i.IsCover,
            }).ToList(),
            Agent = MapAgent(p.Agent, lang)
        };
    }

    private static AdminPropertyDetailDto ToAdminDetailDto(Property p)
    {
        var agentTrans = p.Agent.Translations
            .FirstOrDefault(t => t.Language == Language.En)
            ?? p.Agent.Translations.FirstOrDefault();

        return new AdminPropertyDetailDto
        {
            Id = p.Id,
            Slug = p.Slug,
            Price = p.Price,
            Currency = p.Currency,
            Size = p.Size,
            Rooms = p.Rooms,
            Bedrooms = p.Bedrooms,
            Bathrooms = p.Bathrooms,
            Floor = p.Floor,
            TotalFloors = p.TotalFloors,
            YearBuilt = p.YearBuilt,
            EnergyClass = p.EnergyClass,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            IsFeatured = p.IsFeatured,
            PropertyType = p.PropertyType,
            TransactionType = p.TransactionType,
            Status = p.Status,
            PublishedAt = p.PublishedAt,
            CreatedAt = p.CreatedAt,
            Agent = new TeamMemberListDto
            {
                Id = p.Agent.Id,
                Slug = p.Agent.Slug,
                Name = agentTrans?.Name ?? string.Empty,
                Role = agentTrans?.Role ?? string.Empty,
                PhotoUrl = p.Agent.PhotoUrl,
                Phone = p.Agent.Phone,
                Email = p.Agent.Email,
                Languages = p.Agent.Languages
            },
            Translations = p.Translations.ToDictionary(
                t => t.Language,
                t => new PropertyTranslationDto
                {
                    Title = t.Title,
                    Description = t.Description,
                    Address = t.Address,
                    City = t.City,
                    District = t.District
                }),
            Images = p.Images.OrderBy(i => i.SortOrder).Select(i => new PropertyImageDto
            {
                Id = i.Id,
                Url = i.Url,
                AltText = i.AltText,
                SortOrder = i.SortOrder,
                IsCover = i.IsCover
            }).ToList(),
            Features = p.Features.Select(f => f.Feature).ToList()
        };
    }
}
