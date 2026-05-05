using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.CRM.Activities;

public class ActivityDto
{
    public Guid Id { get; set; }
    public Guid? DealId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Outcome { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityWriteDto
{
    public Guid? DealId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? PropertyId { get; set; }

    [Required]
    public ActivityType Type { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }
    public DateTime? OccurredAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Outcome { get; set; }
}
