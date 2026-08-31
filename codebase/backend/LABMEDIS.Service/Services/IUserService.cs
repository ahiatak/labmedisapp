using System.Security.Claims;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

/// <summary>
/// User account service. <see cref="GetCurrentUserAsync"/> is used by every controller for
/// its mandatory log line (Principle VII); login/lockout/refresh-token/permission resolution
/// support US2 (Authentification, Rôles et Permissions).
/// </summary>
public interface IUserService
{
    Task<CurrentUserContext?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetPermissionExceptionsAsync(Guid userId, UpdateUserPermissionExceptionsRequest request, CancellationToken cancellationToken = default);
}
