using System.Net;
using System.Net.Http.Json;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T051 — login + verrouillage après 5 échecs (423), FR-014.</summary>
public class AuthTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAccessAndRefreshTokens()
    {
        var email = $"login-ok-{Guid.NewGuid()}@labmedis.test";
        await TestAuthHelper.CreateUserAndLoginAsync(factory, email, "Commercial");

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = TestAuthHelper.DefaultPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<Service.DTOs.Responses.AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task Login_WithFiveConsecutiveFailures_LocksAccountAndReturns423()
    {
        var email = $"lockout-{Guid.NewGuid()}@labmedis.test";

        using (var scope = factory.Services.CreateScope())
        {
            var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
            await roleService.EnsureSeededAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Test", LastName = "Lockout", IsActive = true, EmailConfirmed = true };
            await userManager.CreateAsync(user, TestAuthHelper.DefaultPassword);
        }

        var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "WrongPassword!123" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var lockedResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = TestAuthHelper.DefaultPassword });
        Assert.Equal((HttpStatusCode)423, lockedResponse.StatusCode);
    }
}
