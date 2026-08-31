using System.Net.Http.Json;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>Seeds a test user directly through Identity (bypassing the HTTP layer) and logs it in through the real /api/auth/login endpoint to obtain a JWT for authorization tests.</summary>
public static class TestAuthHelper
{
    public const string DefaultPassword = "P@ssw0rd!2026";

    public static async Task<string> CreateUserAndLoginAsync(CustomWebApplicationFactory factory, string email, params string[] roles)
    {
        using (var scope = factory.Services.CreateScope())
        {
            var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
            await roleService.EnsureSeededAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            if (roles.Length > 0)
            {
                await userManager.AddToRolesAsync(user, roles);
            }
        }

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = DefaultPassword });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.AccessToken;
    }
}
