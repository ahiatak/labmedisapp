using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using LABMEDIS.Api.Authorization;
using LABMEDIS.Api.Middleware;
using LABMEDIS.Core.Repositories.CompanyProfile;
using LABMEDIS.Core.Repositories.Currency;
using LABMEDIS.Core.Repositories.ExchangeRate;
using LABMEDIS.Core.Repositories.LoginAudit;
using LABMEDIS.Core.Repositories.RefreshToken;
using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog is the exclusive logging provider (Principle IV) — replace the default providers.
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();
    builder.Services.AddScoped<ILoggerManager, LoggerManager>();

    // --- Data (PostgreSQL / EF Core) ---
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // --- Configuration domain (CompanyProfile, Currency, ExchangeRate) ---
    builder.Services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
    builder.Services.AddScoped<ICurrencyService, CurrencyService>();
    builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

    // --- US1: Référentiel (Produits, Fournisseurs, Clients) ---
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ISupplierService, SupplierService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IReferentielService, ReferentielService>();
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.Base.BaseRepository<Category>>();
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.Base.BaseRepository<TherapeuticClass>>();
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.Base.BaseRepository<PharmaceuticalForm>>();

    // --- US2: Authentification, Rôles et Permissions ---
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<ILoginAuditRepository, LoginAuditRepository>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // --- US3: Achat International (Commandes d'Achat, Expéditions) ---
    builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
    builder.Services.AddScoped<IShipmentService, ShipmentService>();

    // --- US4: Réception, Stock, Traçabilité FEFO ---
    builder.Services.AddScoped<IWarehouseService, WarehouseService>();
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.Base.BaseRepository<Warehouse>>();
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.Base.BaseRepository<LABMEDIS.Core.Models.Entities.StorageLocation>>();
    builder.Services.AddScoped<IStockLotService, StockLotService>();
    builder.Services.AddScoped<IStockMovementService, StockMovementService>();
    builder.Services.AddScoped<LABMEDIS.Service.Jobs.ExpiryAlertJob>();

    // --- US6: Tarification ---
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.ProductPrice.IProductPriceRepository, LABMEDIS.Core.Repositories.ProductPrice.ProductPriceRepository>();
    builder.Services.AddScoped<IPricingService, PricingService>();

    // --- US7: Ventes et Facturation ---
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.Invoice.IInvoiceRepository, LABMEDIS.Core.Repositories.Invoice.InvoiceRepository>();
    builder.Services.AddScoped<ISaleOrderService, SaleOrderService>();
    // Lazy singleton factory — new PdfTools() loads the native libwkhtmltox library, which is
    // only present once the container image bundles it (research.md §8); registering it this
    // way means a missing native library fails a PDF request, never application startup.
    builder.Services.AddSingleton<DinkToPdf.Contracts.IConverter>(_ => new DinkToPdf.SynchronizedConverter(new DinkToPdf.PdfTools()));
    builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();

    // --- US8: Retours Clients et Avoirs ---
    builder.Services.AddScoped<ICustomerReturnService, CustomerReturnService>();

    // --- US9: Inventaire Physique ---
    builder.Services.AddScoped<IInventorySessionService, InventorySessionService>();

    // --- US10: Prévision (MRP) et Réapprovisionnement ---
    builder.Services.AddScoped<IForecastService, ForecastService>();
    builder.Services.AddScoped<LABMEDIS.Service.Jobs.StockForecastJob>();

    // --- US11: Reporting et Tableaux de Bord ---
    builder.Services.AddScoped<IReportingService, ReportingService>();

    // --- US12: Notifications Temps Réel ---
    builder.Services.AddScoped<INotificationService, NotificationService>();

    // --- US13: Gestion Documentaire et Conformité Réglementaire ---
    builder.Services.AddScoped<IComplianceService, ComplianceService>();

    // --- Polish: Audit trail (FR-089/FR-092) ---
    builder.Services.AddScoped<LABMEDIS.Core.Repositories.AuditLog.IAuditLogRepository, LABMEDIS.Core.Repositories.AuditLog.AuditLogRepository>();
    // Same lazy singleton IConverter registration as US7's InvoicePdfService (see comment
    // there) — registering it again here would throw "already registered"; only add if not
    // already present so this controller can be wired independently of SaleOrders.
    builder.Services.TryAddSingleton<DinkToPdf.Contracts.IConverter>(_ => new DinkToPdf.SynchronizedConverter(new DinkToPdf.PdfTools()));

    // --- Identity + JWT Bearer (FR-012 à FR-019) ---
    builder.Services
        .AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // FR-013 — politique de mot de passe.
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;

            // FR-014 — verrouillage 5 tentatives / 15 minutes.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
    if (string.IsNullOrWhiteSpace(jwtSigningKey))
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey n'est pas configuré. Définissez-le via appsettings.Development.json (local) " +
            "ou la variable d'environnement Jwt__SigningKey (production) — jamais en dur dans le code source.");
    }

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // Allow SignalR clients to send the JWT via query string on the hub handshake.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // --- Hangfire (jobs planifiés — StockForecastJob, ExpiryAlertJob ajoutés au fil des stories) ---
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
    builder.Services.AddHangfireServer();

    // --- SignalR (+ backplane Redis) — Hubs mappés au fil de leur implémentation (US3-US12) ---
    var signalRBuilder = builder.Services.AddSignalR();
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        signalRBuilder.AddStackExchangeRedis(redisConnectionString);
    }

    // --- Rate limiting natif .NET 9 (FR-014 : 5/15min sur l'authentification) ---
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status423Locked;

        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0
                }));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 300,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    // --- CORS restreint (constitution §Sécurité) ---
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("LabmedisFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // --- Swagger / OpenAPI avec support JWT Bearer ---
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "LABMEDIS API", Version = "v1" });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Jeton JWT — Bearer {token}",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        options.AddSecurityDefinition("Bearer", securityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, [] } });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        // Seeds the 10 built-in LABMEDIS roles and the FR-015 permission catalogue on startup
        // (idempotent — inserts only permissions/roles that do not already exist).
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
        await roleService.EnsureSeededAsync();

        var currencyService = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
        await currencyService.EnsureSeededAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHangfireDashboard("/hangfire");
    }

    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseHttpsRedirection();
    app.UseCors("LabmedisFrontend");
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<AuditLoggingMiddleware>();

    app.MapControllers();
    app.MapHub<LABMEDIS.Service.Hubs.NotificationHub>("/hubs/notifications");

    // --- Jobs récurrents Hangfire ---
    RecurringJob.AddOrUpdate<LABMEDIS.Service.Jobs.ExpiryAlertJob>(
        "expiry-alert-job", job => job.RunAsync(CancellationToken.None), Cron.Daily);
    RecurringJob.AddOrUpdate<LABMEDIS.Service.Jobs.StockForecastJob>(
        "stock-forecast-job", job => job.RunAsync(CancellationToken.None), Cron.Daily);

    app.Run();
}
catch (HostAbortedException)
{
    // Intentionally thrown by EF Core design-time tooling (e.g. `dotnet ef migrations add`)
    // right after WebApplicationBuilder.Build() — not a real startup failure, rethrow silently.
    throw;
}
catch (Exception ex)
{
    logger.Error(ex, "Le démarrage de l'application a échoué.");
    throw;
}
finally
{
    LogManager.Shutdown();
}

/// <summary>Exposed so LABMEDIS.Tests can bootstrap WebApplicationFactory&lt;Program&gt; against this entry point.</summary>
public partial class Program;
