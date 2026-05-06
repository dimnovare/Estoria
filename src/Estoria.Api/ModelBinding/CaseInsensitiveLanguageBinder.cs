using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Estoria.Api.ModelBinding;

/// <summary>
/// Tolerant model binder for the <see cref="Language"/> enum on query/route
/// parameters. Accepts BCP-47 (en-US), ISO 639-1 (en), and any casing —
/// unparseable values resolve to null instead of returning 400, so the
/// endpoint's own fallback path (defaults to Et) takes over gracefully.
/// </summary>
public class CaseInsensitiveLanguageBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var raw = value.FirstValue;
        if (string.IsNullOrWhiteSpace(raw))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        // Normalize: take portion before first '-' or '_' (en-US → en),
        // then case-insensitive enum parse.
        var two = raw.Split('-', '_')[0].Trim();
        if (Enum.TryParse<Language>(two, ignoreCase: true, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        // Unparseable lang → null instead of 400. The endpoint decides
        // what to do (most fall back to default language).
        bindingContext.Result = ModelBindingResult.Success(null);
        return Task.CompletedTask;
    }
}

public class CaseInsensitiveLanguageBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(Language?) ||
            context.Metadata.ModelType == typeof(Language))
        {
            return new CaseInsensitiveLanguageBinder();
        }
        return null;
    }
}
