using Estoria.Application.Interfaces;
using Estoria.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoria.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/stats")]
[Authorize(Roles = "Admin")]
public class AdminStatsController : ControllerBase
{
    private readonly IAppDbContext _db;

    public AdminStatsController(IAppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var stats = new
        {
            properties    = await _db.Properties.CountAsync(p => p.Status == PropertyStatus.Active, ct),
            blogPosts     = await _db.BlogPosts.CountAsync(b => b.Status == BlogPostStatus.Published, ct),
            teamMembers   = await _db.TeamMembers.CountAsync(t => t.IsActive, ct),
            unreadMessages= await _db.ContactMessages.CountAsync(c => c.Status == ContactStatus.New, ct),
            subscribers   = await _db.NewsletterSubscribers.CountAsync(s => s.IsActive, ct),
        };
        return Ok(stats);
    }
}
