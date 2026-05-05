using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message} | Inner: {Inner}",
                ex.Message,
                ex.InnerException?.Message ?? "none");
            Console.WriteLine($"[Estoria] Unhandled exception: {ex}");

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            KeyNotFoundException  => (StatusCodes.Status404NotFound,            "Not Found"),
            ArgumentException     => (StatusCodes.Status400BadRequest,          "Bad Request"),
            _                     => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Status   = status,
            Title    = title,
            Detail   = ex.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
