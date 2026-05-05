using Estoria.Application.DTOs.Public;
using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Application.Services;

public class PublicLookupService
{
    private static readonly string[] SiteLanguages = ["et", "en", "ru"];

    private readonly IAppDbContext _db;
    private readonly SiteSettingService _settings;

    public PublicLookupService(IAppDbContext db, SiteSettingService settings)
    {
        _db       = db;
        _settings = settings;
    }

    public async Task<PublicStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var oneYearAgo = DateTime.UtcNow.AddDays(-365);

        var propertiesActive = await _db.Properties
            .CountAsync(p => p.Status == PropertyStatus.Active, ct);

        var successfulDeals = await _db.Properties
            .CountAsync(p => p.Status == PropertyStatus.Sold && p.UpdatedAt >= oneYearAgo, ct);

        var yearsExperience     = await _settings.GetIntAsync("stats.years_experience",     8,  ct);
        var satisfactionPercent = await _settings.GetIntAsync("stats.satisfaction_percent", 98, ct);

        return new PublicStatsDto
        {
            PropertiesActive    = propertiesActive,
            SuccessfulDeals     = successfulDeals,
            YearsExperience     = yearsExperience,
            SatisfactionPercent = satisfactionPercent,
            Languages           = SiteLanguages,
        };
    }

    public async Task<List<CityDto>> GetCitiesAsync(Language lang, CancellationToken ct = default)
    {
        var cities = await QueryCitiesAsync(lang, ct);

        // Fall back to English if the requested language has no rows for any
        // active property. Keeps the dropdown populated for under-translated
        // listings.
        if (cities.Count == 0 && lang != Language.En)
            cities = await QueryCitiesAsync(Language.En, ct);

        return cities;
    }

    public Task<PropertyTypeOptionsDto> GetTypeOptionsAsync(Language lang, CancellationToken ct = default)
    {
        var result = new PropertyTypeOptionsDto
        {
            PropertyTypes = Enum.GetValues<PropertyType>()
                .Select(t => new PropertyTypeOptionDto
                {
                    Value = t.ToString(),
                    Label = LocalizePropertyType(t, lang),
                })
                .ToList(),

            TransactionTypes = Enum.GetValues<TransactionType>()
                .Select(t => new PropertyTypeOptionDto
                {
                    Value = t.ToString(),
                    Label = LocalizeTransactionType(t, lang),
                })
                .ToList(),
        };

        return Task.FromResult(result);
    }

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------

    private async Task<List<CityDto>> QueryCitiesAsync(Language lang, CancellationToken ct)
    {
        return await _db.PropertyTranslations
            .Where(pt => pt.Language == lang && pt.Property.Status == PropertyStatus.Active)
            .GroupBy(pt => pt.City)
            .Select(g => new CityDto
            {
                Name  = g.Key,
                Count = g.Count(),
            })
            .OrderByDescending(c => c.Count)
            .ToListAsync(ct);
    }

    // Static label dictionaries — kept here, not in i18n files, because the
    // backend serves a single canonical label per (type, language) pair to all
    // clients. Frontend i18n files cover UI chrome, not enum vocabularies.
    private static readonly Dictionary<(PropertyType, Language), string> PropertyTypeLabels = new()
    {
        { (PropertyType.Apartment,  Language.En), "Apartment"      },
        { (PropertyType.Apartment,  Language.Et), "Korter"         },
        { (PropertyType.Apartment,  Language.Ru), "Квартира"       },
        { (PropertyType.House,      Language.En), "House"          },
        { (PropertyType.House,      Language.Et), "Maja"           },
        { (PropertyType.House,      Language.Ru), "Дом"            },
        { (PropertyType.Commercial, Language.En), "Commercial"     },
        { (PropertyType.Commercial, Language.Et), "Äripind"        },
        { (PropertyType.Commercial, Language.Ru), "Коммерческая"   },
        { (PropertyType.Land,       Language.En), "Land"           },
        { (PropertyType.Land,       Language.Et), "Maa"            },
        { (PropertyType.Land,       Language.Ru), "Земля"          },
        { (PropertyType.Office,     Language.En), "Office"         },
        { (PropertyType.Office,     Language.Et), "Kontor"         },
        { (PropertyType.Office,     Language.Ru), "Офис"           },
    };

    private static readonly Dictionary<(TransactionType, Language), string> TransactionTypeLabels = new()
    {
        { (TransactionType.Sale, Language.En), "For Sale" },
        { (TransactionType.Sale, Language.Et), "Müük"     },
        { (TransactionType.Sale, Language.Ru), "Продажа"  },
        { (TransactionType.Rent, Language.En), "For Rent" },
        { (TransactionType.Rent, Language.Et), "Üür"      },
        { (TransactionType.Rent, Language.Ru), "Аренда"   },
    };

    private static string LocalizePropertyType(PropertyType t, Language lang)
        => PropertyTypeLabels.TryGetValue((t, lang), out var label) ? label
         : PropertyTypeLabels.TryGetValue((t, Language.En), out var en) ? en
         : t.ToString();

    private static string LocalizeTransactionType(TransactionType t, Language lang)
        => TransactionTypeLabels.TryGetValue((t, lang), out var label) ? label
         : TransactionTypeLabels.TryGetValue((t, Language.En), out var en) ? en
         : t.ToString();
}
