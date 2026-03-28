using Estoria.Application.DTOs.Newsletter;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly NewsletterService _svc;

    public NewsletterController(NewsletterService svc) => _svc = svc;

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(
        [FromBody] SubscribeDto dto,
        CancellationToken ct = default)
    {
        await _svc.SubscribeAsync(dto, ct);
        return Ok();
    }
}
