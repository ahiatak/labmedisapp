using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace LABMEDIS.Tests.Integration;

/// <summary>T031 — import Excel catalogue : rapport d'erreurs par ligne, 200 lignes en moins de 10s (FR-006/SC-004).</summary>
public class ProductImportTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Import_TwoHundredRowsWithSomeErrors_CompletesUnderTenSecondsAndReportsErrorsPerRow()
    {
        var token = await TestAuthHelper.CreateUserAndLoginAsync(factory, $"admin-{Guid.NewGuid()}@labmedis.test", "Admin");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string categoryName;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category { Name = $"Réactifs-{Guid.NewGuid():N}" };
            context.Categories.Add(category);
            await context.SaveChangesAsync();
            categoryName = category.Name;
        }

        const int totalRows = 200;
        const int invalidRows = 3; // rows with an unknown category → reported as per-row errors

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Produits");
        sheet.Cell(1, 1).Value = "Désignation";
        sheet.Cell(1, 2).Value = "Catégorie";
        sheet.Cell(1, 3).Value = "TVA";
        sheet.Cell(1, 4).Value = "CodeCIP";

        for (var i = 1; i <= totalRows; i++)
        {
            var row = i + 1;
            sheet.Cell(row, 1).Value = $"Produit import {i}-{Guid.NewGuid():N}";
            sheet.Cell(row, 2).Value = i <= invalidRows ? "CatégorieInconnue" : categoryName;
            sheet.Cell(row, 3).Value = "0.18";
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "catalogue.xlsx");

        var stopwatch = Stopwatch.StartNew();
        var response = await client.PostAsync("/api/products/import", content);
        stopwatch.Stop();

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<ProductImportResponse>();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"L'import de {totalRows} lignes a pris {stopwatch.Elapsed.TotalSeconds:F1}s (limite SC-004 : 10s).");
        Assert.NotNull(report);
        Assert.Equal(totalRows, report!.TotalRows);
        Assert.Equal(invalidRows, report.Errors.Count);
        Assert.Equal(totalRows - invalidRows, report.SuccessCount);
        Assert.All(report.Errors, e => Assert.True(e.RowNumber >= 2 && e.RowNumber <= invalidRows + 1));
    }
}
