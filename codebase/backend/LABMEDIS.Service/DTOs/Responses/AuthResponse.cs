namespace LABMEDIS.Service.DTOs.Responses;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public CurrentUserContext User { get; set; } = new();
}
