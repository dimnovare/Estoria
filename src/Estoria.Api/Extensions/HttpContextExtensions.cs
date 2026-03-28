using Estoria.Domain.Enums;

namespace Estoria.Api.Extensions;

public static class HttpContextExtensions
{
    public static Language GetLanguage(this HttpContext ctx)
    {
        return ctx.Items.TryGetValue("Language", out var lang) && lang is Language l
            ? l
            : Language.En;
    }
}
