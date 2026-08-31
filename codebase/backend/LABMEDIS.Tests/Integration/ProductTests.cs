using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T030 — création produit + doublon désignation (409), FR-001/FR-002.</summary>
public class ProductTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<(HttpClient Client, Guid CategoryId)> CreateAuthenticatedClientAsync()
    {
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"admin-{Guid.NewGuid()}@labmedis.test", "Admin");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        Guid categoryId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = $"Catégorie-{Guid.NewGuid():N}" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            categoryId = category.Id;
        }

        return (client, categoryId);
    }

    [Fact]
    public async Task Create_ThenCreateSameDesignationAgain_ReturnsConflict()
    {
        var (client, categoryId) = await CreateAuthenticatedClientAsync();
        var request = new CreateProductRequest
        {
            Designation = $"Paracétamol 500mg {Guid.NewGuid():N}",
            CategoryId = categoryId,
            VatRate = "0.18"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync("/api/products", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }
}
