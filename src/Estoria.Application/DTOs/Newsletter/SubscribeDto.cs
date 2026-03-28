using System.ComponentModel.DataAnnotations;
using Estoria.Domain.Enums;

namespace Estoria.Application.DTOs.Newsletter;

public class SubscribeDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public Language? Language { get; set; }
}
