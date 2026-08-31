# LABMEDIS — Backend

API .NET 9 pour le système de gestion du dépositaire pharmaceutique LABMEDIS. Architecture trois couches strictes (voir `.specify/memory/constitution.md`) : `LABMEDIS.Core` (entités EF Core, repositories) → `LABMEDIS.Service` (logique métier, DTOs, jobs, hubs SignalR) → `LABMEDIS.Api` (contrôleurs, middleware, configuration).

## Prérequis

- .NET 9 SDK
- PostgreSQL 16+ et Redis (voir `docker-compose.yml` à la racine du dépôt)
- Docker (recommandé pour PostgreSQL/Redis en local)

## Mise en route

```bash
# Infrastructure locale (depuis la racine du dépôt)
docker compose up -d postgres redis

cd codebase/backend
dotnet restore
dotnet ef database update --project LABMEDIS.Core --startup-project LABMEDIS.Api
dotnet run --project LABMEDIS.Api
```

L'API expose Swagger sur `/swagger` en environnement de développement, et le dashboard Hangfire sur `/hangfire`.

Au premier démarrage, `IRoleService.EnsureSeededAsync()` crée automatiquement les 10 rôles métier LABMEDIS et le catalogue de permissions (`PermissionCatalog`), et `ICurrencyService.EnsureSeededAsync()` crée les 3 devises supportées (EUR/USD/XOF).

### Configuration requise

`Jwt:SigningKey` doit être défini (via `appsettings.Development.json` en local, ou la variable d'environnement `Jwt__SigningKey` en production) — l'application refuse de démarrer sans lui. Voir `appsettings.json`/`appsettings.Development.json` pour les autres clés (`ConnectionStrings:DefaultConnection`, `Redis:ConnectionString`, `Cors:AllowedOrigins`).

## Migrations EF Core

```bash
dotnet ef migrations add <Nom> --project LABMEDIS.Core --startup-project LABMEDIS.Api -o Migrations
dotnet ef database update --project LABMEDIS.Core --startup-project LABMEDIS.Api
```

## Tests

```bash
dotnet test LABMEDIS.Tests/LABMEDIS.Tests.csproj
```

- **`LABMEDIS.Tests/Unit/`** — tests purs, sans dépendance externe (FEFO, cascade de pricing, CUMP, arrondi CFA, point de commande MRP). Ceux marqués **bloquants** dans `tasks.md` (FEFO, Pricing, CUMP, arrondi) DOIVENT passer avant tout merge (constitution §Qualité).
- **`LABMEDIS.Tests/Integration/`** — tests de bout en bout via `WebApplicationFactory<Program>` + PostgreSQL éphémère (Testcontainers). **Nécessitent Docker** pour démarrer le conteneur PostgreSQL — sans Docker disponible, ces tests échouent à l'initialisation de `CustomWebApplicationFactory`, pas à cause d'un défaut du code testé.

Pour n'exécuter que les tests unitaires (aucune dépendance Docker) :

```bash
dotnet test LABMEDIS.Tests/LABMEDIS.Tests.csproj --filter "FullyQualifiedName~LABMEDIS.Tests.Unit"
```

## Notes d'architecture

- **Mapping manuel** des DTOs (jamais d'AutoMapper) — voir `LABMEDIS.Service/DTOs/`.
- **Service hérite du Repository** (jamais d'injection de repository) — chaque `[Entité]Service` hérite de `[Entité]Repository : BaseRepository<T>`. Les entités adossées à ASP.NET Core Identity (`ApplicationUser`/`ApplicationRole`) dérogent à cette règle et utilisent `UserManager`/`RoleManager` directement (voir le commentaire en tête de `UserService`/`RoleService`).
- **Soft delete exclusif** : toute entité héritant de `BaseEntity` est filtrée automatiquement (`IsDeleted = false`) par un Query Filter global dans `AppDbContext.OnModelCreating`. Les entités de traçabilité append-only (`StockMovement`, `LoginAudit`, `AuditLog`, `PurchaseOrderStatusHistory`, `NotificationRead`, `ShipmentEvent`) héritent d'`AppendOnlyEntity` à la place et ne sont jamais soft-supprimées.
- **Montants financiers en `string`** sur tous les DTO Request (Principe VI) — conversion via `.ToDecimal()`/`.ToCfaRounded()` (`LABMEDIS.Service/Extensions/DecimalExtensions.cs`).
- **Logging exclusif via `ILoggerManager`** (jamais `ILogger<T>`) — chaque action de contrôleur logue un début et, en cas d'échec anticipé, une erreur avec IP/User-Agent.
- **Permissions** : policies ASP.NET Core résolues dynamiquement par nom (`[Authorize(Policy = "Products.Read")]`) via `PermissionPolicyProvider`/`PermissionAuthorizationHandler` — le rôle `Admin` a un accès implicite à tout.
- **Notifications temps réel** : `NotificationService.EmitAsync` persiste toujours la notification avant de la pousser via SignalR (`NotificationHub`, groupes `Role:X`/`Permission:Y`) — un utilisateur hors ligne au moment de l'émission la retrouve via `GET /api/notifications` (FR-094). Le relais email/SMS critique (FluentEmail/Twilio) nécessite des identifiants SMTP/Twilio non fournis dans cet environnement de développement ; il journalise l'intention via `ILoggerManager` en attendant leur configuration.
- **PDF** (factures, rapports) via `DinkToPdf`, qui nécessite la bibliothèque native `libwkhtmltox` au runtime (voir `research.md` §8) — non incluse par défaut sur les images Linux minimales, à empaqueter explicitement dans le Dockerfile de production. `IConverter` est enregistré comme singleton paresseux : son absence ne fait échouer que les requêtes PDF, jamais le démarrage de l'application.

## Écarts connus par rapport aux conventions documentées

- Les tables utilisent la convention par défaut d'EF Core (PascalCase, ex. `AspNetUsers`, `StockLots`) plutôt que le `snake_case` mentionné dans `data-model.md` — établi dès la migration `InitialCreate`, avant l'implémentation des user stories.
