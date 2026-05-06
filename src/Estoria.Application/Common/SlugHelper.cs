using System.Text.RegularExpressions;

namespace Estoria.Application.Common;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var slug = text.ToLowerInvariant()
            .Replace("ä", "a").Replace("ö", "o").Replace("ü", "u")
            .Replace("õ", "o").Replace("š", "s").Replace("ž", "z");

        slug = Regex.Replace(slug, @"[\s\-_]+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = slug.Trim('-');

        return slug;
    }

    /// <summary>
    /// Returns <paramref name="baseSlug"/> if it is not already taken,
    /// otherwise appends "-2", "-3", … until a free candidate is found.
    /// The caller provides the uniqueness check so this helper stays
    /// DB-agnostic; close over any exclude-id and CancellationToken there.
    /// </summary>
    public static async Task<string> UniqueAsync(
        string baseSlug,
        Func<string, Task<bool>> existsAsync)
    {
        if (!await existsAsync(baseSlug)) return baseSlug;
        for (var i = 2; i <= 100; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!await existsAsync(candidate)) return candidate;
        }
        return $"{baseSlug}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
