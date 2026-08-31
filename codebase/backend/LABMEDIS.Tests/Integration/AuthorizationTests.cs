using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Service.DTOs.Requests;

namespace LABMEDIS.Tests.Integration;

/// <summary>T052 — accès refusé (403) sur action hors permission de rôle (FR-015/SC-011).</summary>
public class AuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CommercialRole_AttemptingToCreateProduct_IsForbidden()
    {
        // The Commercial role only carries Products.Read (PermissionCatalog.DefaultRolePermissions) —
        // product catalogue maintenance is reserved to Admin/Direction/Responsable Achats (US1).
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Designation = $"Produit interdit {Guid.NewGuid():N}",
            CategoryId = Guid.NewGuid(),
            VatRate = "0.18"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CommercialRole_ReadingProducts_IsAllowed()
    {
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"commercial-{Guid.NewGuid()}@labmedis.test", "Commercial");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
