using Estoria.Application.Common;
using Estoria.Application.DTOs.Newsletter;
using Estoria.Application.Interfaces;
using Estoria.Domain.Entities;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class NewsletterService
{
    private readonly IAppDbContext _db;

    public NewsletterService(IAppDbContext db) => _db = db;

    public async Task SubscribeAsync(
        SubscribeDto dto, CancellationToken ct = default)
    {
        var existing = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == dto.Email, ct);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.Language = dto.Language ?? Language.En;
        }
        else
        {
            _db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email = dto.Email,
                Language = dto.Language ?? Language.En,
                IsActive = true,
                SubscribedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // Admin
    // -------------------------------------------------------------------------

    public async Task<PagedResult<SubscriberDto>> GetSubscribersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.NewsletterSubscribers
            .AsNoTracking()
            .OrderByDescending(s => s.SubscribedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SubscriberDto>
        {
            Items = items.Select(s => new SubscriberDto
            {
                Id = s.Id,
                Email = s.Email,
                Language = s.Language,
                IsActive = s.IsActive,
                SubscribedAt = s.SubscribedAt
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task UnsubscribeAsync(Guid id, CancellationToken ct = default)
    {
        var subscriber = await _db.NewsletterSubscribers.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Subscriber {id} not found.");

        subscriber.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
