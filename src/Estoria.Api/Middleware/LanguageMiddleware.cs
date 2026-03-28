using Estoria.Domain.Enums;

namespace Estoria.Api.Middleware;

public class LanguageMiddleware
{
    private readonly RequestDelegate _next;

    public LanguageMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var lang = Language.En;

        if (context.Request.Query.TryGetValue("lang", out var langParam) &&
            Enum.TryParse<Language>(langParam, ignoreCase: true, out var parsedLang))
        {
            lang = parsedLang;
        }
        else
        {
            var acceptLanguage = context.Request.Headers.AcceptLanguage.FirstOrDefault();
            if (acceptLanguage is not null && acceptLanguage.Length >= 2)
            {
                lang = acceptLanguage[..2].ToLowerInvariant() switch
                {
                    "et" => Language.Et,
                    "ru" => Language.Ru,
                    _    => Language.En
                };
            }
        }

        context.Items["Language"] = lang;
        await _next(context);
    }
}
