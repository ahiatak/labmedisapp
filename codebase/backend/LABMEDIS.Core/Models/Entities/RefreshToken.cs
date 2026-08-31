namespace LABMEDIS.Core.Models.Entities;

/// <summary>Opaque refresh token (FR-012/FR-018): access token 15-30 min, refresh 7-30 days.</summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
