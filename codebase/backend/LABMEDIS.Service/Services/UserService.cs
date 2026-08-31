using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.LoginAudit;
using LABMEDIS.Core.Repositories.RefreshToken;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RefreshTokenEntity = LABMEDIS.Core.Models.Entities.RefreshToken;

namespace LABMEDIS.Service.Services;

/// <summary>
/// ASP.NET Core Identity already provides the full data-access abstraction for
/// <see cref="ApplicationUser"/> (UserManager) — unlike the custom BaseEntity-derived
/// entities of the domain model, User is not wrapped in an additional
/// I[Entité]Repository/BaseRepository layer (Principle II targets the 59 business tables,
/// not the framework-provided Identity store). UserService therefore injects UserManager
/// directly rather than inheriting a repository. Covers US2 (FR-012 à FR-019).
/// </summary>
public class UserService(
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService,
    IRefreshTokenRepository refreshTokenRepository,
    ILoginAuditRepository loginAuditRepository,
    IConfiguration configuration) : IUserService
{
    public async Task<CurrentUserContext?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        return await BuildContextAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException(400, "MISSING_CREDENTIALS", "Email et mot de passe sont requis.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);

        async Task AuditAsync(bool success) =>
            await loginAuditRepository.AddAsync(new Core.Models.Entities.LoginAudit
            {
                UserId = user?.Id,
                Email = request.Email,
                Success = success,
                IpAddress = ipAddress,
                UserAgent = userAgent
            }, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await AuditAsync(false);
            throw new AppException(401, "INVALID_CREDENTIALS", "Identifiants invalides.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await AuditAsync(false);
            throw new AppException(423, "ACCOUNT_LOCKED", "Ce compte est temporairement verrouillé suite à plusieurs échecs de connexion.");
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            await AuditAsync(false);
            throw new AppException(401, "INVALID_CREDENTIALS", "Identifiants invalides.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginDate = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await AuditAsync(true);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await refreshTokenRepository.GetActiveByTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new AppException(401, "REFRESH_TOKEN_INVALID", "Le jeton de renouvellement est invalide ou expiré.");

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
            ?? throw new AppException(401, "REFRESH_TOKEN_INVALID", "Le jeton de renouvellement est invalide ou expiré.");

        existing.RevokedAt = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(existing, cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await refreshTokenRepository.GetActiveByTokenAsync(request.RefreshToken, cancellationToken);
        if (existing is null)
        {
            return;
        }

        existing.RevokedAt = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(existing, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Generic response regardless of outcome (Principle: never reveal whether an email
        // exists) — the reset token itself is relayed by email, not returned here.
        await userManager.FindByEmailAsync(request.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new AppException(400, "RESET_FAILED", "Impossible de réinitialiser le mot de passe.");

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            throw new AppException(400, "RESET_FAILED", string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = userManager.Users.ToList();
        var result = new List<UserResponse>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UserResponse(user, roles.ToList()));
        }

        return result;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            throw new AppException(409, "EMAIL_ALREADY_USED", "Un compte avec cet email existe déjà.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new AppException(400, "USER_CREATE_FAILED", string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        if (request.Roles.Count > 0)
        {
            await userManager.AddToRolesAsync(user, request.Roles);
        }

        var roles = await userManager.GetRolesAsync(user);
        return new UserResponse(user, roles.ToList());
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        user.IsActive = false;
        await userManager.UpdateAsync(user);
        await refreshTokenRepository.RevokeAllForUserAsync(id, cancellationToken);
    }

    public async Task SetPermissionExceptionsAsync(Guid userId, UpdateUserPermissionExceptionsRequest request, CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByIdAsync(userId.ToString()) is null)
        {
            throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");
        }

        await permissionService.SetUserExceptionsAsync(userId, request.Exceptions.Select(e => (e.PermissionId, e.IsGranted, e.Reason)), cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(user, cancellationToken);

        var accessTokenMinutes = configuration.GetValue("Jwt:AccessTokenMinutes", 30);
        var refreshTokenDays = configuration.GetValue("Jwt:RefreshTokenDays", 7);
        var signingKey = configuration["Jwt:SigningKey"]!;
        var expiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        claims.AddRange(context.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(context.Permissions.Select(permission => new Claim("permission", permission)));

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var refreshToken = new RefreshTokenEntity
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };
        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = refreshToken.Token,
            ExpiresAt = expiresAt,
            User = context
        };
    }

    private async Task<CurrentUserContext> BuildContextAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionService.GetEffectivePermissionCodesForRoleNamesAsync(user.Id, roles, cancellationToken);

        return new CurrentUserContext
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName ?? user.Email ?? user.Id.ToString(),
            Roles = roles.ToList(),
            Permissions = permissions.ToList()
        };
    }
}
