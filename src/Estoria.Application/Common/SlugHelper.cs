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
}
