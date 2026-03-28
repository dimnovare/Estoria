using Estoria.Application.DTOs.Contact;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly ContactService _svc;

    public ContactController(ContactService svc) => _svc = svc;

    [HttpPost]
    [EnableRateLimiting("contact")]
    public async Task<IActionResult> Submit(
        [FromBody] CreateContactDto dto,
        CancellationToken ct = default)
    {
        var id = await _svc.SubmitAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, new { id });
    }
}
