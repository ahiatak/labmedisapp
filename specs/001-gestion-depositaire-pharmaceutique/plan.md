# Implementation Plan: Système de Gestion LABMEDIS (Dépositaire Pharmaceutique)

**Branch**: `001-gestion-depositaire-pharmaceutique` | **Date**: 2026-08-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-gestion-depositaire-pharmaceutique/spec.md`

## Summary

LABMEDIS est un système de gestion intégral pour un dépositaire pharmaceutique (achats internationaux, réception/stock/traçabilité FEFO, contrôle qualité, tarification en cascade, ventes/facturation, retours, inventaire, MRP, reporting, notifications temps réel, conformité BPD). L'approche technique est **entièrement imposée par la constitution du projet** (`.specify/memory/constitution.md`, v1.0.0) : backend .NET 9 en architecture 3 couches strictes (`LABMEDIS.Core` / `LABMEDIS.Service` / `LABMEDIS.Api`), PostgreSQL via EF Core, Hangfire pour les jobs planifiés, SignalR pour le temps réel (zéro polling), NLog via `ILoggerManager`, mapping manuel des DTOs, soft delete exclusif ; frontend React + TypeScript + Vite. Ce plan traduit les 13 user stories et 94 exigences fonctionnelles du spec en structure de projet, modèle de données et contrats d'API, sans dévier des Règles d'Or constitutionnelles.

## Technical Context

**Language/Version**: Backend — C# 13 / .NET 9. Frontend — TypeScript 5.x sur React 18+ (Vite).

**Primary Dependencies**:
- Backend : ASP.NET Core Identity + JWT Bearer, Entity Framework Core 9 + `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.BulkExtensions`, Hangfire (+ `Hangfire.PostgreSql`), `Microsoft.AspNetCore.SignalR` + `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, NLog (wrappé par `ILoggerManager`), `DinkToPdf`, `FluentEmail`, Twilio SDK, `UAParser`, Swashbuckle (Swagger/OpenAPI).
- Frontend : React Router, TailwindCSS (ou Material-UI), `@microsoft/signalr` (client SignalR), Recharts, client HTTP (fetch/axios) avec intercepteur JWT/refresh, bibliothèque de masques de saisie CFA.

**Storage**: PostgreSQL 16+ (référentiel de ~59 tables métier ; PK UUID, `snake_case` pluriel, FK `[singulier]_id`, statuts `VARCHAR` + `CHECK`, index unique partiel `WHERE deleted_at IS NULL`, trigger `set_updated_at()`). Redis pour le backplane SignalR et le cache applicatif.

**Testing**: Backend — xUnit + `Microsoft.AspNetCore.Mvc.Testing` (tests d'intégration API) + Testcontainers (PostgreSQL éphémère pour les tests d'intégration/contrat). Frontend — Vitest + React Testing Library (unitaire/composant) + Playwright (parcours end-to-end critiques : achat, réception FEFO, vente/facturation).

**Target Platform**: Backend — conteneur Linux (Docker) hébergeant l'API ASP.NET Core 9, déployable derrière un reverse proxy HTTPS. Frontend — SPA servie statiquement, compatible navigateurs web modernes de bureau (poste LABMEDIS).

**Project Type**: Application web (frontend + backend séparés) — Option 2 (voir Project Structure).

**Performance Goals**: Catalogue produit < 500 ms P95 (SC-003) ; notification temps réel perçue < 1 s après l'événement (SC-005) ; import Excel de 200 produits < 10 s (SC-004) ; aucune dégradation perceptible jusqu'à ~30 utilisateurs actifs simultanés (SC-014).

**Constraints**: Disponibilité continue 24/7 avec fenêtres de maintenance planifiées hors heures ouvrées (SC-015) ; RPO ≈ quelques minutes en cas d'incident (SC-016) ; rétention illimitée des données de traçabilité, aucune purge (FR-092) ; zéro suppression physique (Principe III de la constitution) ; zéro survente sous confirmations concurrentes (FR-091) ; aucun polling pour le temps réel (Principe IX) ; tous les champs financiers des DTO Request en `string` (Principe VI) ; formatage France/local 100% français, dates `JJ/MM/AAAA`.

**Scale/Scope**: 138 références produits initiales (93 réactifs labo, 22 médicaments, 17 produits infantiles, 2 cosmétiques, 2 compléments, 1 insecticide), 8 fournisseurs, 12 clients connus, ~200 ventes/jour, ~59 tables métier, 13 user stories, 94 exigences fonctionnelles, 10 domaines d'API, 26 entités clés.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principe (constitution v1.0.0) | Statut | Justification |
|---|---|---|---|
| I | Architecture Trois Couches Étanche | ✅ PASS | Project Structure ci-dessous instancie exactement `LABMEDIS.Core` / `LABMEDIS.Service` / `LABMEDIS.Api` sous `codebase/backend/`, et le frontend React sous `codebase/frontend/`, sans mélange de couches. |
| II | Héritage Service → Repository | ✅ PASS | Chaque entité aura `I[Entité]Repository`/`[Entité]Repository : BaseRepository<T>` dans Core puis `[Entité]Service : [Entité]Repository, I[Entité]Service` dans Service (voir data-model.md) — appliqué dans `/speckit-tasks` et l'implémentation, aucune injection de repository. |
| III | Soft Delete Exclusif & Intégrité | ✅ PASS | `BaseEntity` (Id/CreatedAt/UpdatedAt/DeletedAt/IsDeleted) + Query Filters globaux planifiés dans data-model.md ; couvre FR-084/FR-092 et SC-007. |
| IV | Logging ILoggerManager Exclusif | ✅ PASS | `ILoggerManager` (NLog) retenu comme unique logger dans Primary Dependencies ; format de log imposé appliqué à chaque contrôleur en phase tasks/implémentation. |
| V | Mapping Manuel des DTOs | ✅ PASS | Contracts (Phase 1) documentent des DTOs Request/Response mappés manuellement (`To[Entité]()`, constructeur `[Entité]Response(entity)`), AutoMapper exclu. |
| VI | Contrats de Données Financières en String | ✅ PASS | Tous les champs monétaires/décimaux des DTO Request documentés en `string` dans `contracts/` (ex. `purchasePriceForeign`, `exchangeRate`), conversion via extensions `.ToDecimal()`/`.ToCfaRounded()`. |
| VII | Structure de Contrôleur API Obligatoire | ✅ PASS | `contracts/` décrit, pour chaque domaine, les codes de réponse alignés sur le contrat imposé (jamais de 500 explicite, `BadRequest` avec message convivial dans le catch). |
| VIII | Traçabilité Pharmaceutique (FEFO/Quarantaine) | ✅ PASS | data-model.md modélise `StockLot`/`quality_status` et l'algorithme FEFO backend-only, conforme à US4/US5/US8 du constitution et FR-036/037/040-043. |
| IX | Stack Technique Imposée | ✅ PASS | Hangfire (`LABMEDIS.Service/Jobs`), SignalR (+Redis backplane, zéro polling), `BulkInsertAsync`/`BulkUpdateAsync` retenus explicitement dans Primary Dependencies et Project Structure. |

**Résultat** : Aucune violation. `Complexity Tracking` reste vide — toutes les décisions techniques découlent directement de la constitution, sans écart à justifier.

### Post-Design Constitution Check (après Phase 1)

Re-vérifié après génération de `research.md`, `data-model.md`, `contracts/*` et `quickstart.md` : aucune décision de recherche (Phase 0) ni aucun contrat (Phase 1) n'introduit de déviation. En particulier — `data-model.md` confirme `BaseEntity`/soft delete partout sauf entités append-only explicitement listées (Principe III) ; tous les contrats `contracts/*.md` documentent des montants en `string` et des codes d'erreur sans jamais `500` explicite (Principes VI, VII) ; `research.md` §5 (concurrence) et §6 (RPO) ne modifient aucune règle métier, seulement l'implémentation technique de garanties déjà actées dans le spec. **Gate toujours PASS.**

## Project Structure

### Documentation (this feature)

```text
specs/001-gestion-depositaire-pharmaceutique/
├── plan.md              # Ce fichier (/speckit-plan)
├── research.md          # Phase 0 (/speckit-plan)
├── data-model.md         # Phase 1 (/speckit-plan)
├── quickstart.md         # Phase 1 (/speckit-plan)
├── contracts/             # Phase 1 (/speckit-plan) — un fichier par domaine d'API
│   ├── auth.md
│   ├── products-referentiel.md
│   ├── purchase-orders.md
│   ├── shipments.md
│   ├── stock.md
│   ├── pricing.md
│   ├── sales.md
│   ├── forecast.md
│   ├── reporting.md
│   └── notifications.md
├── checklists/
│   └── requirements.md   # Déjà généré par /speckit-specify
└── tasks.md               # Phase 2 (/speckit-tasks — PAS créé par /speckit-plan)
```

### Source Code (repository root)

```text
codebase/
├── backend/
│   ├── LABMEDIS.Core/                        # Entités, interfaces, repositories EF Core
│   │   ├── Models/Entities/                  # Product, Supplier, Customer, PurchaseOrder,
│   │   │                                     #   Shipment, StockLot, StorageLocation,
│   │   │                                     #   StockMovement, PricingProfile, ProductPrice,
│   │   │                                     #   SaleOrder, Invoice, CustomerReturn,
│   │   │                                     #   ForecastCalculation, ReorderSuggestion,
│   │   │                                     #   ApplicationUser, Notification, ... (BaseEntity)
│   │   ├── Repositories/Base/                 # BaseRepository<T> (CRUD générique + soft delete)
│   │   ├── Repositories/[Entité]/             # I[Entité]Repository + [Entité]Repository
│   │   │                                     #   (requêtes complexes : FEFO, encours, MRP...)
│   │   └── AppDbContext.cs                    # DbSets, Query Filters IsDeleted, configuration
│   │
│   ├── LABMEDIS.Service/                      # Logique métier, DTOs, jobs, SignalR
│   │   ├── DTOs/Requests/                     # Create/Update[Entité]Request (montants en string)
│   │   ├── DTOs/Responses/                    # [Entité]Response(entity) — mapping manuel
│   │   ├── Services/                          # [Entité]Service : [Entité]Repository, I[Entité]Service
│   │   ├── Jobs/                              # StockForecastJob, ExpiryAlertJob (Hangfire)
│   │   ├── Hubs/                               # StockAlertHub, NotificationHub (SignalR)
│   │   └── Extensions/                        # ToDecimal(), ToCfaRounded() (arrondi XOF)
│   │
│   ├── LABMEDIS.Api/                          # Contrôleurs API, middleware, config JWT/Swagger
│   │   ├── Controllers/                       # Auth, Products, Suppliers, PurchaseOrders,
│   │   │                                     #   Shipments, Stock, Pricing, SaleOrders,
│   │   │                                     #   Forecast, Reports, Notifications, Users
│   │   ├── Middleware/                        # Gestion des erreurs globales, GetIp/UserAgent
│   │   └── Program.cs                          # DI, Identity, JWT, Hangfire, SignalR, NLog
│   │
│   └── LABMEDIS.Tests/
│       ├── Unit/                              # FEFO, Pricing (cascade), CUMP/PMP, arrondi CFA
│       │                                     #   (bloquants — voir constitution §Qualité)
│       ├── Integration/                       # Workflows bout-en-bout (achat→réception→
│       │                                     #   stock→vente→facturation), Testcontainers PG
│       └── Contract/                          # Un test par endpoint documenté dans contracts/
│
└── frontend/
    ├── src/
    │   ├── components/                        # UI réutilisables (formulaires, tableaux, badges statut)
    │   ├── pages/                              # Dashboard, Produits, Achats, Réception, Stock,
    │   │                                     #   Pricing Simulator, Ventes, Retours, Inventaire,
    │   │                                     #   MRP, Reporting, Notifications, Admin/Rôles
    │   ├── routes/                             # ProtectedRoute, PermissionGate (claims JWT)
    │   ├── services/                           # Client API REST, client SignalR, gestion refresh token
    │   ├── hooks/                               # useAuth, useNotifications, usePermissions
    │   └── i18n/                                # Libellés FR, formats de date JJ/MM/AAAA, masques CFA
    └── tests/
        ├── unit/                               # Composants et hooks (Vitest + RTL)
        └── e2e/                                # Parcours critiques (Playwright)
```

**Structure Decision** : Application web à deux dépôts applicatifs distincts (`./codebase/backend`, `./codebase/frontend`), conformément au Principe I de la constitution. Le backend suit strictement les 3 projets `LABMEDIS.Core` / `LABMEDIS.Service` / `LABMEDIS.Api` (pas de `LABMEDIS.BackOffice` — le frontend React couvre entièrement la présentation, aucune vue Razor n'est nécessaire). Un projet de tests unique `LABMEDIS.Tests` regroupe Unit/Integration/Contract pour respecter l'exigence de couverture >80% et le caractère bloquant des tests FEFO/Pricing/CUMP/arrondi.

## Complexity Tracking

*Aucune violation de la Constitution Check — section laissée vide intentionnellement.*
