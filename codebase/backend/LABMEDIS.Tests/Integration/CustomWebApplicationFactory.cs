using LABMEDIS.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace LABMEDIS.Tests.Integration;

/// <summary>
/// Spins up a throwaway PostgreSQL 16 container per test collection (Testcontainers, per
/// plan.md Testing) and boots the real ASP.NET Core host against it, migrations applied, so
/// integration tests exercise the actual EF Core provider (query filters, partial unique
/// indexes, ILike) rather than the InMemory provider.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("labmedis_test")
        .WithUsername("labmedis")
        .WithPassword("labmedis_test_password")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = postgresContainer.GetConnectionString(),
                ["Redis:ConnectionString"] = ""
            });
        });
    }

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
