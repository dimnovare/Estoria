using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.SavedSearches;

/// <summary>
/// Filter shape persisted as JSON in <c>SavedSearch.FilterJson</c>. Superset
/// of <c>PropertyFilterDto</c>: adds district, room range, and a free-form
/// feature list so the saved-search match logic can be richer than the
/// public property listing filter.
/// </summary>
public class SavedSearchFilter
{
    public PropertyType? Type { get; set; }
    public TransactionType? Transaction { get; set; }

    public string? City { get; set; }
    public string? District { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinSize { get; set; }
    public decimal? MaxSize { get; set; }

    public int? MinRooms { get; set; }
    public int? MaxRooms { get; set; }

    public List<string> Features { get; set; } = new();
}

public class SavedSearchCreateDto
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Name { get; set; }

    public Language PreferredLanguage { get; set; } = Language.En;

    [Required]
    public SavedSearchFilter Filter { get; set; } = new();

    public SavedSearchFrequency Frequency { get; set; } = SavedSearchFrequency.Daily;
}

public class SavedSearchDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid? ContactId { get; set; }
    public string? Name { get; set; }
    public Language PreferredLanguage { get; set; }
    public SavedSearchFilter Filter { get; set; } = new();
    public SavedSearchFrequency Frequency { get; set; }
    public DateTime? LastSentAt { get; set; }
    public int LastResultsCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
