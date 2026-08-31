namespace LABMEDIS.Core.Models.Entities;

/// <summary>Append-only login journal (FR-017): every attempt, success or failure.</summary>
public class LoginAudit : AppendOnlyEntity
{
    public Guid? UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public bool Success { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;
}
