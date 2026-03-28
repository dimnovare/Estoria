using Estoria.Api.Extensions;
using Estoria.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Public;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly OfferedServiceService _svc;

    public ServicesController(OfferedServiceService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var lang = HttpContext.GetLanguage();
        return Ok(await _svc.GetAllActiveAsync(lang, ct));
    }
}
