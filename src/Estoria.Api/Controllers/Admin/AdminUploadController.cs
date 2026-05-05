using Estoria.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/upload")]
[Authorize(Roles = "Admin")]
public class AdminUploadController : ControllerBase
{
    private static readonly HashSet<string> _allowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".mp4"];

    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IFileStorageService _storage;

    public AdminUploadController(IFileStorageService storage) => _storage = storage;

    [HttpPost]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromQuery] string folder = "misc",
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        if (file.Length > MaxBytes)
            return BadRequest("File exceeds the 10 MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            return BadRequest($"File type '{ext}' is not allowed.");

        await using var stream = file.OpenReadStream();
        var url = await _storage.UploadPublicAsync(stream, file.FileName, file.ContentType, folder, ct);

        return Ok(new { url });
    }
}
