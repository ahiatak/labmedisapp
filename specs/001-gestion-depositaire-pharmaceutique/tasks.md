---

description: "Task list for LABMEDIS — Système de Gestion Dépositaire Pharmaceutique"
---

# Tasks: Système de Gestion LABMEDIS (Dépositaire Pharmaceutique)

**Input**: Design documents from `/specs/001-gestion-depositaire-pharmaceutique/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (tous présents)

**Tests**: Inclus. La constitution du projet (`.specify/memory/constitution.md` §Qualité) exige une couverture de tests >80% avec des tests unitaires **bloquants** sur FEFO, Pricing (cascade), CUMP/PMP et arrondi CFA, ainsi que des tests d'intégration sur les workflows métier de bout en bout — les tâches de test ci-dessous ne sont donc pas optionnelles.

**Organisation**: Tâches groupées par user story (spec.md) pour permettre une implémentation et une validation indépendantes de chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story concernée (US1 à US13)
- Chemins de fichiers exacts inclus dans chaque description

## Conventions de chemins

Application web à deux dépôts (voir plan.md § Project Structure) :
- Backend : `codebase/backend/LABMEDIS.Core/`, `LABMEDIS.Service/`, `LABMEDIS.Api/`, `LABMEDIS.Tests/`
- Frontend : `codebase/frontend/src/`, `codebase/frontend/tests/`

---

## Phase 1: Setup (Infrastructure Partagée)

**Objectif** : Initialisation du dépôt et de la structure de base des deux applications.

- [X] T001 Créer la solution backend et les 4 projets (`LABMEDIS.Core`, `LABMEDIS.Service`, `LABMEDIS.Api`, `LABMEDIS.Tests`) avec leurs références de projet dans `codebase/backend/`
- [X] T002 [P] Créer le squelette frontend React + TypeScript + Vite dans `codebase/frontend/`
- [X] T003 [P] Ajouter les dépendances NuGet backend (EF Core 9, `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.BulkExtensions`, `Hangfire.Core`+`Hangfire.PostgreSql`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `NLog.Web.AspNetCore`, `DinkToPdf`, `FluentEmail.Core`, Twilio SDK, `UAParser`, `Swashbuckle.AspNetCore`) dans les `.csproj` de `codebase/backend/`
- [X] T004 [P] Ajouter les dépendances frontend (`react-router-dom`, `tailwindcss`, `@microsoft/signalr`, `recharts`, `axios`) dans `codebase/frontend/package.json`
- [X] T005 [P] Configurer le linting/formatage backend (`.editorconfig`, analyzers .NET) dans `codebase/backend/`
- [X] T006 [P] Configurer le linting/formatage frontend (ESLint + Prettier) dans `codebase/frontend/`
- [X] T007 [P] Créer `docker-compose.yml` (PostgreSQL 16 + Redis) pour l'environnement local à la racine du dépôt
- [X] T008 [P] Ajouter les dépendances de test (`xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`) dans `codebase/backend/LABMEDIS.Tests/LABMEDIS.Tests.csproj`
- [X] T009 [P] Configurer l'outillage de test frontend (Vitest, React Testing Library, Playwright) dans `codebase/frontend/`

**Checkpoint** : Les deux applications compilent et démarrent à vide.

---

## Phase 2: Foundational (Prérequis Bloquants)

**Objectif** : Infrastructure transverse requise par **toutes** les user stories. Aucune story ne peut démarrer avant la fin de cette phase.

**⚠️ CRITIQUE** : Bloque toutes les phases suivantes.

- [X] T010 Créer `BaseEntity` (`Id`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `IsDeleted`) dans `codebase/backend/LABMEDIS.Core/Models/Entities/BaseEntity.cs`
- [X] T011 Créer `BaseRepository<T>` (CRUD générique, soft delete exclusif, `BulkInsertAsync`/`BulkUpdateAsync` EFCore.BulkExtensions) dans `codebase/backend/LABMEDIS.Core/Repositories/Base/BaseRepository.cs` (dépend de T010)
- [X] T012 Créer `AppDbContext` avec Query Filter global `IsDeleted = false` appliqué à toute entité héritant de `BaseEntity` dans `codebase/backend/LABMEDIS.Core/AppDbContext.cs` (dépend de T010)
- [X] T013 [P] Créer l'interface `ILoggerManager` et son implémentation NLog dans `codebase/backend/LABMEDIS.Service/Logging/ILoggerManager.cs`, `LoggerManager.cs`
- [X] T014 [P] Créer le middleware global de gestion d'erreurs (aucun `StatusCode(500)` explicite en amont, le middleware capte les crashs critiques) dans `codebase/backend/LABMEDIS.Api/Middleware/ErrorHandlingMiddleware.cs`
- [X] T015 [P] Créer les extensions `HttpContext.GetIp()`/`GetUserAgentName()`/`GetRequestData()` (via UAParser) dans `codebase/backend/LABMEDIS.Api/Extensions/HttpContextExtensions.cs`
- [X] T016 [P] Créer les extensions `string.ToDecimal()` et `decimal.ToCfaRounded()` (arrondi XOF `MidpointRounding.AwayFromZero`) dans `codebase/backend/LABMEDIS.Service/Extensions/DecimalExtensions.cs`
- [X] T017 Configurer ASP.NET Core Identity (`ApplicationUser`, `ApplicationRole`) + authentification JWT Bearer dans `codebase/backend/LABMEDIS.Api/Program.cs` (dépend de T012)
- [X] T018 Configurer Hangfire (storage `Hangfire.PostgreSql`, dashboard, scheduler de jobs récurrents) dans `codebase/backend/LABMEDIS.Api/Program.cs` (dépend de T012)
- [X] T019 Configurer SignalR avec backplane `StackExchangeRedis` (enregistrement des Hubs) dans `codebase/backend/LABMEDIS.Api/Program.cs`
- [X] T020 [P] Configurer Swagger/OpenAPI avec support JWT Bearer dans `codebase/backend/LABMEDIS.Api/Program.cs`
- [X] T021 [P] Configurer NLog (cibles/règles) dans `codebase/backend/LABMEDIS.Api/nlog.config` (dépend de T013)
- [X] T022 [P] Configurer le rate limiting natif .NET 9 (`Microsoft.AspNetCore.RateLimiting`) — politique stricte 5/15 min sur `/api/auth/*`, politique générale pour le reste de l'API — dans `codebase/backend/LABMEDIS.Api/Program.cs`
- [X] T023 Créer la migration EF Core baseline (schéma vide, extensions PostgreSQL `pgcrypto`/`gen_random_uuid`) dans `codebase/backend/LABMEDIS.Core/Migrations/` (dépend de T012, T017)
- [X] T024 [P] Créer les entités `CompanyProfile` (config singleton : seuil de validation, TVA défaut) et son repository dans `codebase/backend/LABMEDIS.Core/Models/Entities/CompanyProfile.cs`, `Repositories/CompanyProfile/`
- [X] T025 [P] Créer les entités `Currency`, `ExchangeRate` (taux EUR/XOF fixe, USD/XOF variable historisé) et leurs repositories dans `codebase/backend/LABMEDIS.Core/Models/Entities/Currency.cs`, `ExchangeRate.cs`, `Repositories/Currency/`, `Repositories/ExchangeRate/`
- [X] T026 [P] Créer le squelette de routage frontend (React Router, layout applicatif, stubs `ProtectedRoute`/`PermissionGate`) dans `codebase/frontend/src/routes/`
- [X] T027 [P] Créer le client API frontend avec intercepteur JWT/refresh automatique dans `codebase/frontend/src/services/apiClient.ts`
- [X] T028 [P] Créer le service client SignalR (connexion, abonnement par groupe de rôle, reconnexion automatique) dans `codebase/frontend/src/services/signalrClient.ts`
- [X] T029 [P] Créer les utilitaires i18n (libellés français, dates `JJ/MM/AAAA`, masques de saisie CFA) dans `codebase/frontend/src/i18n/`

**Checkpoint** : Fondation prête — l'implémentation des user stories peut commencer, y compris en parallèle par story.

---

## Phase 3: User Story 1 - Référentiel Produits, Fournisseurs et Clients (Priority: P1) 🎯 MVP (1/7)

**Goal** : Permettre à Admin/Direction/Responsable Achats de créer et maintenir produits, fournisseurs et clients contrôlés, socle de toutes les transactions.

**Independent Test** : Créer un produit, un fournisseur et un client (règles d'unicité, listes contrôlées) et vérifier qu'ils sont immédiatement utilisables dans les formulaires d'achat/vente.

### Tests for User Story 1

- [X] T030 [P] [US1] Test d'intégration : création produit + doublon désignation (409) dans `codebase/backend/LABMEDIS.Tests/Integration/ProductTests.cs`
- [X] T031 [P] [US1] Test d'intégration : import Excel catalogue (rapport d'erreurs par ligne, 200 lignes < 10 s) dans `codebase/backend/LABMEDIS.Tests/Integration/ProductImportTests.cs`

### Implementation for User Story 1

- [X] T032 [P] [US1] Créer les entités référentiel `Category`, `TherapeuticClass`, `PharmaceuticalForm` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T033 [P] [US1] Créer les entités `Product`, `ProductPackaging` dans `codebase/backend/LABMEDIS.Core/Models/Entities/Product.cs`, `ProductPackaging.cs`
- [X] T034 [P] [US1] Créer les entités `Supplier`, `ProductSupplier` dans `codebase/backend/LABMEDIS.Core/Models/Entities/Supplier.cs`, `ProductSupplier.cs`
- [X] T035 [P] [US1] Créer les entités `Customer`, `CustomerProductPrice` dans `codebase/backend/LABMEDIS.Core/Models/Entities/Customer.cs`, `CustomerProductPrice.cs`
- [X] T036 [US1] Créer `IProductRepository`/`ProductRepository : BaseRepository<Product>` (unicité désignation/CIP parmi actifs, exclusion des inactifs des sélections) dans `codebase/backend/LABMEDIS.Core/Repositories/Product/` (dépend de T033)
- [X] T037 [US1] Créer `ISupplierRepository`/`SupplierRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/Supplier/` (dépend de T034)
- [X] T038 [US1] Créer `ICustomerRepository`/`CustomerRepository` (calcul encours = Σ factures non soldées, détection chevauchement tarifs négociés) dans `codebase/backend/LABMEDIS.Core/Repositories/Customer/` (dépend de T035)
- [X] T039 [US1] Créer `CreateProductRequest`/`UpdateProductRequest`/`ProductResponse` (mapping manuel, `vatRate` en `string`) dans `codebase/backend/LABMEDIS.Service/DTOs/Requests/ProductRequest.cs`, `DTOs/Responses/ProductResponse.cs`
- [X] T040 [US1] Créer `CreateSupplierRequest`/`SupplierResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/Requests/SupplierRequest.cs`, `DTOs/Responses/SupplierResponse.cs`
- [X] T041 [US1] Créer `CreateCustomerRequest`/`CustomerResponse` (`creditLimit` en `string`) dans `codebase/backend/LABMEDIS.Service/DTOs/Requests/CustomerRequest.cs`, `DTOs/Responses/CustomerResponse.cs`
- [X] T042 [US1] Implémenter `ProductService : ProductRepository, IProductService` (règles d'unicité, import Excel via `BulkInsertOrUpdateAsync`) dans `codebase/backend/LABMEDIS.Service/Services/ProductService.cs` (dépend de T036, T039)
- [X] T043 [US1] Implémenter `SupplierService : SupplierRepository, ISupplierService` dans `codebase/backend/LABMEDIS.Service/Services/SupplierService.cs` (dépend de T037, T040)
- [X] T044 [US1] Implémenter `CustomerService : CustomerRepository, ICustomerService` (blocage client inactif, alerte/blocage encours) dans `codebase/backend/LABMEDIS.Service/Services/CustomerService.cs` (dépend de T038, T041)
- [X] T045 [US1] Implémenter `ProductsController` (CRUD, import, stock) selon `contracts/products-referentiel.md` dans `codebase/backend/LABMEDIS.Api/Controllers/ProductsController.cs` (dépend de T042)
- [X] T046 [US1] Implémenter `SuppliersController` selon `contracts/products-referentiel.md` dans `codebase/backend/LABMEDIS.Api/Controllers/SuppliersController.cs` (dépend de T043)
- [X] T047 [US1] Implémenter `CustomersController` (dont tarifs négociés) selon `contracts/products-referentiel.md` dans `codebase/backend/LABMEDIS.Api/Controllers/CustomersController.cs` (dépend de T044)
- [X] T048 [P] [US1] Frontend : page Catalogue Produits (liste, création, édition, désactivation, import) dans `codebase/frontend/src/pages/Products/`
- [X] T049 [P] [US1] Frontend : page Fournisseurs dans `codebase/frontend/src/pages/Suppliers/`
- [X] T050 [P] [US1] Frontend : page Clients (dont tarifs négociés, encours) dans `codebase/frontend/src/pages/Customers/`

**Checkpoint** : US1 fonctionnelle et testable indépendamment.

---

## Phase 4: User Story 2 - Authentification, Rôles et Permissions (Priority: P1) (2/7)

**Goal** : Authentification sécurisée et contrôle d'accès granulaire par rôle métier.

**Independent Test** : Se connecter avec des comptes de rôles différents et vérifier que chacun ne voit/n'exécute que les actions permises par son rôle.

### Tests for User Story 2

- [X] T051 [P] [US2] Test d'intégration : login + verrouillage après 5 échecs (423) dans `codebase/backend/LABMEDIS.Tests/Integration/AuthTests.cs`
- [X] T052 [P] [US2] Test d'intégration : accès refusé (403) sur action hors permission de rôle dans `codebase/backend/LABMEDIS.Tests/Integration/AuthorizationTests.cs`

### Implementation for User Story 2

- [X] T053 [P] [US2] Créer les entités `Role`, `Permission`, `RolePermission`, `UserPermissionException` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T054 [P] [US2] Créer les entités `RefreshToken`, `LoginAudit` (append-only) dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T055 [US2] Créer `IUserRepository`/`UserRepository`, `IRoleRepository`/`RoleRepository`, `IPermissionRepository`/`PermissionRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/` (dépend de T053, T054)
- [X] T056 [US2] Créer `LoginRequest`/`RefreshTokenRequest`/`AuthResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/Requests/AuthRequest.cs`, `DTOs/Responses/AuthResponse.cs`
- [X] T057 [US2] Implémenter `UserService : UserRepository, IUserService` (login, verrouillage 5/15min, révocation jetons, `GetCurrentUserAsync`) dans `codebase/backend/LABMEDIS.Service/Services/UserService.cs` (dépend de T055, T056)
- [X] T058 [US2] Implémenter `RoleService`/`PermissionService` (CRUD rôles, seed des 10 rôles métier LABMEDIS) dans `codebase/backend/LABMEDIS.Service/Services/RoleService.cs` (dépend de T055)
- [X] T059 [US2] Implémenter le fournisseur de policy d'autorisation par permission (claims `Module.Action`) dans `codebase/backend/LABMEDIS.Api/Authorization/PermissionAuthorizationHandler.cs` (dépend de T058)
- [X] T060 [US2] Implémenter `AuthController` (login/refresh/logout/forgot-reset-password/me) selon `contracts/auth.md` dans `codebase/backend/LABMEDIS.Api/Controllers/AuthController.cs` (dépend de T057)
- [X] T061 [US2] Implémenter `UsersController`/`RolesController` (administration comptes/rôles) dans `codebase/backend/LABMEDIS.Api/Controllers/UsersController.cs`, `RolesController.cs` (dépend de T058)
- [X] T062 [P] [US2] Frontend : page Connexion + cycle de vie refresh token dans `codebase/frontend/src/pages/Auth/`
- [X] T063 [P] [US2] Frontend : hook `useAuth` branché sur `/api/auth/me`, `ProtectedRoute`/`PermissionGate` opérationnels dans `codebase/frontend/src/hooks/useAuth.ts` (dépend de T026)
- [X] T064 [P] [US2] Frontend : page Administration Utilisateurs/Rôles dans `codebase/frontend/src/pages/Admin/Users/`

**Checkpoint** : US1 et US2 fonctionnelles indépendamment ; toute route protégée peut désormais être testée avec de vrais comptes.

---

## Phase 5: User Story 3 - Achat International (Commande Fournisseur) (Priority: P1) (3/7)

**Goal** : Cycle de vie complet de la commande d'achat, taux de change figé, validation Direction conditionnelle.

**Independent Test** : Créer une commande, la faire progresser dans sa machine à états, vérifier le figement du taux et le respect du seuil de validation.

### Tests for User Story 3

- [X] T065 [P] [US3] Test d'intégration : cycle de vie commande d'achat + seuil de validation Direction dans `codebase/backend/LABMEDIS.Tests/Integration/PurchaseOrderTests.cs`
- [X] T066 [P] [US3] Test d'intégration : annulation sans motif refusée (400) dans `codebase/backend/LABMEDIS.Tests/Integration/PurchaseOrderCancelTests.cs`

### Implementation for User Story 3

- [X] T067 [P] [US3] Créer les entités `PurchaseOrder`, `PurchaseOrderLine`, `PurchaseOrderStatusHistory` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T068 [P] [US3] Créer les entités `Shipment`, `ShipmentLine`, `ImportCost` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T069 [US3] Créer `IPurchaseOrderRepository`/`PurchaseOrderRepository` (requêtes machine à états, seuil de montant) dans `codebase/backend/LABMEDIS.Core/Repositories/PurchaseOrder/` (dépend de T067)
- [X] T070 [US3] Créer `IShipmentRepository`/`ShipmentRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/Shipment/` (dépend de T068)
- [X] T071 [US3] Créer `CreatePurchaseOrderRequest`/`PurchaseOrderResponse` (`unitPriceForeign` en `string`) dans `codebase/backend/LABMEDIS.Service/DTOs/Requests/PurchaseOrderRequest.cs`, `DTOs/Responses/PurchaseOrderResponse.cs`
- [X] T072 [US3] Créer `ShipmentRequest`/`ShipmentResponse`/`ImportCostRequest` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T073 [US3] Implémenter `PurchaseOrderService : PurchaseOrderRepository, IPurchaseOrderService` (machine à états FR-022, figement taux FR-021, routage validation FR-023) dans `codebase/backend/LABMEDIS.Service/Services/PurchaseOrderService.cs` (dépend de T069, T071, T025)
- [X] T074 [US3] Implémenter `ShipmentService : ShipmentRepository, IShipmentService` (répartition frais logistiques, référence DPML obligatoire si médicament) dans `codebase/backend/LABMEDIS.Service/Services/ShipmentService.cs` (dépend de T070, T072)
- [X] T075 [US3] Implémenter `PurchaseOrdersController` (create/submit/validate/cancel/status-history) selon `contracts/purchase-orders.md` dans `codebase/backend/LABMEDIS.Api/Controllers/PurchaseOrdersController.cs` (dépend de T073)
- [X] T076 [US3] Implémenter `ShipmentsController` selon `contracts/shipments.md` dans `codebase/backend/LABMEDIS.Api/Controllers/ShipmentsController.cs` (dépend de T074)
- [X] T077 [P] [US3] Frontend : page Commandes d'Achat (création, machine à états, badges de statut) dans `codebase/frontend/src/pages/PurchaseOrders/`
- [X] T078 [P] [US3] Frontend : page Expéditions/Logistique dans `codebase/frontend/src/pages/Shipments/`

**Checkpoint** : US1, US2, US3 fonctionnelles indépendamment.

---

## Phase 6: User Story 4 - Réception de Lots, Stockage et Traçabilité FEFO (Priority: P1) (4/7)

**Goal** : Réception physique par lot, algorithme FEFO backend, traçabilité intégrale des mouvements.

**Independent Test** : Réceptionner un lot, le placer dans un emplacement, demander une sortie de stock et vérifier que le lot le plus proche de la péremption est proposé en premier.

### Tests for User Story 4

- [X] T079 [P] [US4] **Test unitaire bloquant (constitution §Qualité)** : algorithme FEFO (sélection, exclusion lots périmés/non-libérés, dérogation motivée) dans `codebase/backend/LABMEDIS.Tests/Unit/FefoAllocationTests.cs`
- [X] T080 [P] [US4] Test d'intégration : réception commande → création lots → PRU figé → PMP recalculé dans `codebase/backend/LABMEDIS.Tests/Integration/StockReceptionTests.cs`
- [X] T081 [P] [US4] Test d'intégration : blocage réception d'un lot sous le seuil de péremption de sa catégorie dans `codebase/backend/LABMEDIS.Tests/Integration/ExpiryThresholdTests.cs`

### Implementation for User Story 4

- [X] T082 [P] [US4] Créer les entités `Warehouse`, `StorageLocation` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T083 [P] [US4] Créer les entités `StockLot`, `StockLotLocation` (contraintes CHECK quantités) dans `codebase/backend/LABMEDIS.Core/Models/Entities/StockLot.cs`, `StockLotLocation.cs`
- [X] T084 [P] [US4] Créer l'entité `StockMovement` (append-only) dans `codebase/backend/LABMEDIS.Core/Models/Entities/StockMovement.cs`
- [X] T085 [US4] Créer `IStockLotRepository`/`StockLotRepository` (`GetFefoCandidates` avec verrou `SELECT ... FOR UPDATE`, recherche par péremption/statut qualité) dans `codebase/backend/LABMEDIS.Core/Repositories/StockLot/` (dépend de T083)
- [X] T086 [US4] Créer `IStorageLocationRepository`/`StorageLocationRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/StorageLocation/` (dépend de T082)
- [X] T087 [US4] Créer `IStockMovementRepository`/`StockMovementRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/StockMovement/` (dépend de T084)
- [X] T088 [US4] Créer `ReceiveLotsRequest`/`StockLotResponse`/`FefoSuggestionResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T089 [US4] Implémenter `StockLotService : StockLotRepository, IStockLotService` (réception, PRU/PMP figés RG-002, algorithme FEFO RG-001, dérogation motivée, blocage péremption) dans `codebase/backend/LABMEDIS.Service/Services/StockLotService.cs` (dépend de T085, T088) — **cœur métier critique**
- [X] T090 [US4] Implémenter `StockMovementService : StockMovementRepository, IStockMovementService` dans `codebase/backend/LABMEDIS.Service/Services/StockMovementService.cs` (dépend de T087)
- [X] T091 [US4] Étendre `PurchaseOrderService.Receive()` pour créer les lots via `StockLotService` (workflow réception complet) dans `codebase/backend/LABMEDIS.Service/Services/PurchaseOrderService.cs` (dépend de T073, T089)
- [X] T092 [US4] Implémenter `ExpiryAlertJob` (Hangfire quotidien — alertes J-30/60/90/120, transition automatique vers `Périmé`) dans `codebase/backend/LABMEDIS.Service/Jobs/ExpiryAlertJob.cs` (dépend de T089)
- [X] T093 [US4] Implémenter `StockController` (`available`/`movements`/`lots/{id}`/`fefo-suggestion`/`allocate`) selon `contracts/stock.md` dans `codebase/backend/LABMEDIS.Api/Controllers/StockController.cs` (dépend de T089, T090)
- [X] T094 [P] [US4] Frontend : page Réception Stock (saisie lots, écarts qté commandée/reçue, alerte péremption) dans `codebase/frontend/src/pages/StockReception/`
- [X] T095 [P] [US4] Frontend : page Entrepôt/Emplacements (stock par lot/emplacement) dans `codebase/frontend/src/pages/Warehouse/`

**Checkpoint** : US1 à US4 fonctionnelles indépendamment — traçabilité FEFO opérationnelle.

---

## Phase 7: User Story 5 - Contrôle Qualité et Quarantaine des Lots (Priority: P1) (5/7)

**Goal** : Le Responsable Qualité est seul habilité à libérer un lot en quarantaine ; aucun lot non conforme n'est vendable.

**Independent Test** : Mettre un lot en quarantaine, tenter de le vendre (doit être bloqué), le libérer avec le rôle Responsable Qualité uniquement.

### Tests for User Story 5

- [X] T096 [P] [US5] Test d'intégration : libération de lot réservée au rôle Responsable Qualité (403 sinon) dans `codebase/backend/LABMEDIS.Tests/Integration/QualityReleaseTests.cs`
- [X] T097 [P] [US5] Test d'intégration : vente bloquée si le lot n'est pas au statut "Libéré" dans `codebase/backend/LABMEDIS.Tests/Integration/QualityBlockSaleTests.cs`

### Implementation for User Story 5

- [X] T098 [US5] Étendre `StockLotService` avec `Quarantine()`/`Release()`/`MarkNonConforme()`/`Destroy()`/`SuspectFalsified()` (machine à états qualité RG-008, motifs obligatoires, journalisation) dans `codebase/backend/LABMEDIS.Service/Services/StockLotService.cs` (dépend de T089)
- [X] T099 [US5] Étendre `StockController` avec les endpoints `quarantine`/`release`/`non-conforme`/`destroy`/`suspected-falsified` (permission `Quality.Release`) selon `contracts/stock.md` dans `codebase/backend/LABMEDIS.Api/Controllers/StockController.cs` (dépend de T098)
- [X] T100 [US5] Implémenter l'émission des notifications `quarantine:prolonged` et `lot:suspectedFalsified` dans `codebase/backend/LABMEDIS.Service/Services/StockLotService.cs` (dépend de T098) — réalisé lors de l'implémentation de US12 (`NotificationService`/`NotificationHub`, T158-T159), une fois ces dépendances disponibles
- [X] T101 [P] [US5] Frontend : écran Contrôle Qualité (liste des lots en quarantaine, actions libérer/rejeter/détruire) dans `codebase/frontend/src/pages/Quality/`

**Checkpoint** : US1 à US5 fonctionnelles indépendamment.

---

## Phase 8: User Story 6 - Tarification et Moteur de Calcul du Prix de Revient (Priority: P1) (6/7)

**Goal** : Calcul automatique et traçable du prix de revient et du prix de vente en cascade.

**Independent Test** : Simuler un calcul de prix (prix d'achat, taux, profil) et vérifier le résultat sans dépendre d'une commande réelle.

### Tests for User Story 6

- [X] T102 [P] [US6] **Test unitaire bloquant (constitution §Qualité)** : cascade de pricing (PA→PR→PV, arrondi CFA uniquement en sortie) dans `codebase/backend/LABMEDIS.Tests/Unit/PricingCascadeTests.cs`
- [X] T103 [P] [US6] **Test unitaire bloquant** : calcul CUMP/PMP pondéré multi-lots dans `codebase/backend/LABMEDIS.Tests/Unit/CumpCalculationTests.cs`
- [X] T104 [P] [US6] **Test unitaire bloquant** : arrondi CFA (`Math.Round`, `AwayFromZero`, zéro décimale) dans `codebase/backend/LABMEDIS.Tests/Unit/CfaRoundingTests.cs`

### Implementation for User Story 6

- [X] T105 [P] [US6] Créer les entités `PricingProfile`, `ProductPrice` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T106 [US6] Créer `IPricingProfileRepository`/`PricingProfileRepository` (résolution par catégorie/transport avec repli sur profil global) dans `codebase/backend/LABMEDIS.Core/Repositories/PricingProfile/` (dépend de T105)
- [X] T107 [US6] Créer `IProductPriceRepository`/`ProductPriceRepository` (historique immuable, jamais d'update) dans `codebase/backend/LABMEDIS.Core/Repositories/ProductPrice/` (dépend de T105)
- [X] T108 [US6] Créer `SimulatePricingRequest`/`PricingProfileRequest`/`ApplyPriceRequest`/`PricingResponse` (montants en `string`) dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T109 [US6] Implémenter `PricingService : PricingProfileRepository, IPricingService` (cascade RG-004, simulate, apply-price avec écart conservé) dans `codebase/backend/LABMEDIS.Service/Services/PricingService.cs` (dépend de T106, T107, T108, T016)
- [X] T110 [US6] Implémenter `PricingController` (permission `Pricing.Update` réservée Admin/Direction) selon `contracts/pricing.md` dans `codebase/backend/LABMEDIS.Api/Controllers/PricingController.cs` (dépend de T109)
- [X] T111 [P] [US6] Frontend : page Simulateur de Pricing dans `codebase/frontend/src/pages/Pricing/Simulator/`
- [X] T112 [P] [US6] Frontend : page Gestion des Profils de Pricing (Admin/Direction) dans `codebase/frontend/src/pages/Pricing/Profiles/`

**Checkpoint** : US1 à US6 fonctionnelles indépendamment.

---

## Phase 9: User Story 7 - Ventes et Facturation avec Traçabilité des Lots (Priority: P1) (7/7) 🎯 MVP complet

**Goal** : Commande de vente avec allocation FEFO, réservation, livraison et facturation traçant chaque lot vendu.

**Independent Test** : Créer une commande de vente, la confirmer, la livrer, la facturer, vérifier le numéro de lot sur le PDF.

### Tests for User Story 7

- [X] T113 [P] [US7] Test d'intégration : cycle complet commande vente confirmée → réservation FEFO → livraison → facture avec numéro de lot dans `codebase/backend/LABMEDIS.Tests/Integration/SaleOrderLifecycleTests.cs`
- [X] T114 [P] [US7] Test d'intégration : conflit de réservation concurrente (409 `INSUFFICIENT_STOCK`) via deux confirmations parallèles sur la même dernière unité dans `codebase/backend/LABMEDIS.Tests/Integration/ConcurrentReservationTests.cs` (valide FR-091/SC-013)

### Implementation for User Story 7

- [X] T115 [P] [US7] Créer les entités `SaleOrder`, `SaleOrderLine` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T116 [P] [US7] Créer les entités `Delivery`, `DeliveryLine` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T117 [P] [US7] Créer les entités `Invoice`, `InvoiceLine` (`StockLotId` obligatoire) dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T118 [US7] Créer `ISaleOrderRepository`/`SaleOrderRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/SaleOrder/` (dépend de T115)
- [X] T119 [US7] Créer `IInvoiceRepository`/`InvoiceRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/Invoice/` (dépend de T117)
- [X] T120 [US7] Créer `CreateSaleOrderRequest`/`SaleOrderResponse`/`InvoiceResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T121 [US7] Implémenter `SaleOrderService : SaleOrderRepository, ISaleOrderService` (confirmation avec verrou de ligne FEFO — voir research.md §5 —, livraison, facturation, vérification encours client) dans `codebase/backend/LABMEDIS.Service/Services/SaleOrderService.cs` (dépend de T118, T120, T089, T044)
- [X] T122 [US7] Implémenter la génération PDF facture/BL via DinkToPdf (numéro de lot visible) dans `codebase/backend/LABMEDIS.Service/Services/InvoicePdfService.cs` (dépend de T121)
- [X] T123 [US7] Implémenter `SaleOrdersController` selon `contracts/sales.md` dans `codebase/backend/LABMEDIS.Api/Controllers/SaleOrdersController.cs` (dépend de T121, T122)
- [X] T124 [P] [US7] Frontend : page Commandes de Vente (allocation FEFO automatique, statuts, export PDF BL/Facture) dans `codebase/frontend/src/pages/SaleOrders/`

**Checkpoint** : US1 à US7 complètes — **MVP intégral** (référentiel → achat → réception/FEFO → qualité → pricing → vente/facturation traçable).

---

## Phase 10: User Story 8 - Retours Clients et Avoirs (Priority: P2)

**Goal** : Traiter un retour client (disposition stock/quarantaine/destruction) et générer l'avoir correspondant.

**Independent Test** : Initier un retour sur une commande livrée, vérifier lot/délai, choisir une disposition, confirmer la génération de l'avoir.

- [X] T125 [P] [US8] Test d'intégration : retour → disposition → génération avoir dans `codebase/backend/LABMEDIS.Tests/Integration/CustomerReturnTests.cs`
- [X] T126 [P] [US8] Créer les entités `CustomerReturn`, `ReturnLine`, `CreditNote` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T127 [US8] Créer `ICustomerReturnRepository`/`CustomerReturnRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/CustomerReturn/` (dépend de T126)
- [X] T128 [US8] Créer `CreateReturnRequest`/`CustomerReturnResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T129 [US8] Implémenter `CustomerReturnService : CustomerReturnRepository, ICustomerReturnService` (dispositions, avoir) dans `codebase/backend/LABMEDIS.Service/Services/CustomerReturnService.cs` (dépend de T127, T128, T089)
- [X] T130 [US8] Étendre `SaleOrdersController` avec `POST .../returns` selon `contracts/sales.md` dans `codebase/backend/LABMEDIS.Api/Controllers/SaleOrdersController.cs` (dépend de T129)
- [X] T131 [P] [US8] Frontend : écran Retours Clients (initiation, disposition, avoir) dans `codebase/frontend/src/pages/Returns/`

**Checkpoint** : US8 fonctionnelle indépendamment (nécessite qu'une vente existe déjà, produite par US7).

---

## Phase 11: User Story 9 - Inventaire Physique et Ajustements de Stock (Priority: P2)

**Goal** : Session d'inventaire par périmètre, gel des mouvements, calcul des écarts, ajustements motivés.

**Independent Test** : Créer une session sur un périmètre, geler les mouvements, saisir un comptage, calculer les écarts, valider les ajustements.

- [X] T132 [P] [US9] Test d'intégration : session inventaire → gel mouvements → écarts → ajustements motivés dans `codebase/backend/LABMEDIS.Tests/Integration/InventorySessionTests.cs`
- [X] T133 [P] [US9] Créer les entités `InventorySession`, `InventoryCount` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T134 [US9] Créer `IInventorySessionRepository`/`InventorySessionRepository` dans `codebase/backend/LABMEDIS.Core/Repositories/InventorySession/` (dépend de T133)
- [X] T135 [US9] Créer `InventorySessionRequest`/`CountRequest`/`InventorySessionResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T136 [US9] Implémenter `InventorySessionService : InventorySessionRepository, IInventorySessionService` (gel, écarts, ajustements motivés via `StockMovementService`) dans `codebase/backend/LABMEDIS.Service/Services/InventorySessionService.cs` (dépend de T134, T135, T090)
- [X] T137 [US9] Étendre `StockController` avec `inventory-sessions` selon `contracts/stock.md` dans `codebase/backend/LABMEDIS.Api/Controllers/StockController.cs` (dépend de T136)
- [X] T138 [P] [US9] Frontend : écran Inventaire (création session, comptage, validation écarts) dans `codebase/frontend/src/pages/Inventory/`

**Checkpoint** : US9 fonctionnelle indépendamment.

---

## Phase 12: User Story 10 - Prévision des Besoins (MRP) et Réapprovisionnement (Priority: P2)

**Goal** : Suggestions quotidiennes automatiques de réapprovisionnement basées sur la consommation et les délais fournisseurs.

**Independent Test** : Déclencher le calcul de prévision pour un produit avec historique connu et vérifier la suggestion créée (quantité, date limite).

- [X] T139 [P] [US10] Test unitaire : calcul du point de commande (consommation moyenne × délai total + stock de sécurité) dans `codebase/backend/LABMEDIS.Tests/Unit/ReorderPointCalculationTests.cs`
- [X] T140 [P] [US10] Créer les entités `ForecastParameter`, `SupplierLeadTime`, `ForecastCalculation`, `ReorderSuggestion` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T141 [US10] Créer `IForecastRepository`/`ForecastRepository` (agrégation consommation glissante 90 j) dans `codebase/backend/LABMEDIS.Core/Repositories/Forecast/` (dépend de T140)
- [X] T142 [US10] Créer `ForecastParametersRequest`/`ReorderSuggestionResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T143 [US10] Implémenter `ForecastService : ForecastRepository, IForecastService` (point de commande, statut de criticité, convert/reject) dans `codebase/backend/LABMEDIS.Service/Services/ForecastService.cs` (dépend de T141, T142)
- [X] T144 [US10] Implémenter `StockForecastJob` (Hangfire quotidien) dans `codebase/backend/LABMEDIS.Service/Jobs/StockForecastJob.cs` (dépend de T143)
- [X] T145 [US10] Implémenter `ForecastController` selon `contracts/forecast.md` dans `codebase/backend/LABMEDIS.Api/Controllers/ForecastController.cs` (dépend de T143)
- [X] T146 [P] [US10] Frontend : écran Suggestions de Réapprovisionnement (convertir/rejeter, criticité) dans `codebase/frontend/src/pages/Forecast/`

**Checkpoint** : US10 fonctionnelle indépendamment.

---

## Phase 13: User Story 11 - Reporting et Tableaux de Bord (Priority: P2)

**Goal** : Tableaux de bord et rapports adaptés à chaque rôle, exportables.

**Independent Test** : Interroger chaque rapport avec des données existantes et vérifier l'exactitude des agrégations et la disponibilité d'un export.

- [X] T147 [P] [US11] Test d'intégration : rapports ventes/stock/pricing renvoient des agrégations correctes + export dans `codebase/backend/LABMEDIS.Tests/Integration/ReportingTests.cs`
- [X] T148 [US11] Créer `IReportingRepository`/`ReportingRepository` (requêtes agrégées CA/marge/stock/rotation) dans `codebase/backend/LABMEDIS.Core/Repositories/Reporting/`
- [X] T149 [US11] Créer les DTOs Response de reporting (Direction/Stock/Sales/Pricing/Quality) dans `codebase/backend/LABMEDIS.Service/DTOs/Responses/`
- [X] T150 [US11] Implémenter `ReportingService : ReportingRepository, IReportingService` (calcul KPI, export PDF/Excel) dans `codebase/backend/LABMEDIS.Service/Services/ReportingService.cs` (dépend de T148, T149)
- [X] T151 [US11] Implémenter `ReportsController` selon `contracts/reporting.md` dans `codebase/backend/LABMEDIS.Api/Controllers/ReportsController.cs` (dépend de T150)
- [X] T152 [P] [US11] Frontend : page Dashboard (widgets par rôle, abonnement SignalR pour mise à jour temps réel) dans `codebase/frontend/src/pages/Dashboard/`
- [X] T153 [P] [US11] Frontend : pages Rapports (stock, ventes, pricing, qualité, export) dans `codebase/frontend/src/pages/Reports/`

**Checkpoint** : US11 fonctionnelle indépendamment.

---

## Phase 14: User Story 12 - Notifications Temps Réel (Priority: P2)

**Goal** : Alertes temps réel ciblées par rôle, sans polling, persistées pour les utilisateurs hors ligne.

**Independent Test** : Déclencher un événement et vérifier la notification temps réel pour les rôles concernés, avec état lu/non lu par utilisateur.

- [X] T154 [P] [US12] Test d'intégration : notification persistée retrouvée après reconnexion (FR-094) dans `codebase/backend/LABMEDIS.Tests/Integration/NotificationPersistenceTests.cs`
- [X] T155 [P] [US12] Créer les entités `Notification`, `NotificationRead` dans `codebase/backend/LABMEDIS.Core/Models/Entities/`
- [X] T156 [US12] Créer `INotificationRepository`/`NotificationRepository` (état lu/non lu par utilisateur) dans `codebase/backend/LABMEDIS.Core/Repositories/Notification/` (dépend de T155)
- [X] T157 [US12] Créer `NotificationResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/Responses/NotificationResponse.cs`
- [X] T158 [US12] Implémenter `NotificationService : NotificationRepository, INotificationService` (émission ciblée par rôle, persistance garantie, relais email/SMS via `INotificationService` FluentEmail/Twilio) dans `codebase/backend/LABMEDIS.Service/Services/NotificationService.cs` (dépend de T156, T157)
- [X] T159 [US12] Implémenter `NotificationHub` (groupes SignalR par rôle/permission) selon `contracts/notifications.md` dans `codebase/backend/LABMEDIS.Service/Hubs/NotificationHub.cs` (dépend de T158) — consolide les émissions déjà déclenchées par T073/T092/T098/T144
- [X] T160 [US12] Implémenter `NotificationsController` (`list`/`read`/`mark-all-read`) dans `codebase/backend/LABMEDIS.Api/Controllers/NotificationsController.cs` (dépend de T158)
- [X] T161 [P] [US12] Frontend : centre de notifications (badge, liste, marquage lu, restitution à la reconnexion) dans `codebase/frontend/src/components/NotificationCenter/`

**Checkpoint** : US12 fonctionnelle indépendamment.

---

## Phase 15: User Story 13 - Gestion Documentaire et Conformité Réglementaire (Priority: P3)

**Goal** : Pièces justificatives rattachées aux lots/expéditions, traçabilité complète pour un rappel produit.

**Independent Test** : Attacher un document à un lot, simuler un rappel et vérifier la liste des clients ayant reçu ce lot.

- [X] T162 [P] [US13] Test d'intégration : recherche des clients ayant reçu un lot donné (rappel produit) dans `codebase/backend/LABMEDIS.Tests/Integration/LotRecallTraceabilityTests.cs`
- [X] T163 [P] [US13] Créer l'entité `RegulatoryAttachment` dans `codebase/backend/LABMEDIS.Core/Models/Entities/RegulatoryAttachment.cs`
- [X] T164 [US13] Créer `IRegulatoryAttachmentRepository`/`RegulatoryAttachmentRepository` (requête chaîne vente→lot→expédition→achat→fournisseur) dans `codebase/backend/LABMEDIS.Core/Repositories/RegulatoryAttachment/` (dépend de T163)
- [X] T165 [US13] Créer `AttachmentRequest`/`LotTraceabilityResponse` dans `codebase/backend/LABMEDIS.Service/DTOs/`
- [X] T166 [US13] Implémenter `ComplianceService : RegulatoryAttachmentRepository, IComplianceService` (rattachement pièces, traçabilité rappel, clients par lot) dans `codebase/backend/LABMEDIS.Service/Services/ComplianceService.cs` (dépend de T164, T165)
- [X] T167 [US13] Implémenter `ComplianceController` (attachments, lot-traceability, recall) dans `codebase/backend/LABMEDIS.Api/Controllers/ComplianceController.cs` (dépend de T166)
- [X] T168 [P] [US13] Frontend : écran Conformité/Rappel de Lot (pièces jointes, recherche clients par lot) dans `codebase/frontend/src/pages/Compliance/`

**Checkpoint** : Les 13 user stories sont désormais toutes fonctionnelles indépendamment.

---

## Phase 16: Polish & Cross-Cutting Concerns

**Objectif** : Renforcements transverses affectant plusieurs user stories.

- [X] T169 [P] Créer `AuditLog` + `AuditLogRepository` et le middleware d'enregistrement automatique des actions sensibles (rétention illimitée, FR-092) dans `codebase/backend/LABMEDIS.Core/Models/Entities/AuditLog.cs`, `codebase/backend/LABMEDIS.Api/Middleware/AuditLoggingMiddleware.cs`
- [X] T170 [P] Vérifier/finaliser les Query Filters `IsDeleted` globaux et les index uniques partiels `WHERE deleted_at IS NULL` sur toutes les entités (migration de consolidation) dans `codebase/backend/LABMEDIS.Core/AppDbContext.cs`
- [X] T171 [P] Appliquer le masquage des données financières selon les permissions de l'utilisateur consultant (revue transverse des Response DTOs sensibles) dans `codebase/backend/LABMEDIS.Service/DTOs/Responses/`
- [X] T172 [P] Finaliser la configuration CORS restreint + redirection HTTPS obligatoire dans `codebase/backend/LABMEDIS.Api/Program.cs`
- [ ] T173 Exécuter la suite de tests complète et vérifier la couverture >80% ainsi que le caractère bloquant des tests FEFO/Pricing/CUMP/arrondi (constitution §Qualité) en CI
  - **Bloqué dans cet environnement** : Docker n'est pas installé sur cette machine (`docker`/Docker Desktop absents). `dotnet test codebase/backend/LABMEDIS.Tests` a été exécuté le 2026-08-31 : les 25 tests **Unit** passent (100%), y compris les 4 suites bloquantes constitution §Qualité (`FefoAllocationTests`, `PricingCascadeTests`, `CumpCalculationTests`, `CfaRoundingTests`). Les 21 tests **Integration**/**Contract** échouent tous avec `System.ArgumentException: Docker is either not running or misconfigured` car `CustomWebApplicationFactory` provisionne PostgreSQL via Testcontainers — la couverture globale (0,16% mesurée) ne reflète donc que le sous-ensemble Unit et ne peut pas valider le seuil >80%.
  - **À exécuter dès qu'un Docker fonctionnel est disponible** (poste dev ou CI) :
    ```bash
    cd codebase/backend
    dotnet test LABMEDIS.Tests/LABMEDIS.Tests.csproj -c Release --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
    # puis générer le rapport de couverture (ex. reportgenerator) et vérifier line-rate > 80%
    ```
- [X] T174 [P] Rédiger la documentation développeur (mise en route, migrations, scripts) dans `codebase/backend/README.md`, `codebase/frontend/README.md`
- [ ] T175 Exécuter les 7 scénarios de `quickstart.md` de bout en bout et consigner les résultats
  - **Bloqué dans cet environnement** : les scénarios nécessitent PostgreSQL + Redis (`docker compose up -d postgres redis`) et l'API/le frontend démarrés localement ; Docker n'étant pas disponible sur cette machine, aucun scénario n'a pu être rejoué de bout en bout. Le build backend (`dotnet build LABMEDIS.sln -c Release`) et le build frontend (`npm run build`) réussissent tous les deux sans erreur, ce qui confirme que le code est prêt à être exécuté dès que l'infrastructure locale (Docker) sera disponible.
  - **À exécuter dès que Docker est disponible** : suivre `quickstart.md` § Mise en route, puis rejouer les 7 scénarios et consigner Pass/Fail par scénario.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance — démarrage immédiat.
- **Foundational (Phase 2)** : dépend de Setup — **bloque** toutes les user stories.
- **User Stories (Phases 3-15)** : dépendent toutes de Foundational. US1 et US2 sont des prérequis *données/accès* de fait pour toutes les suivantes (un produit doit exister pour être acheté ; un compte doit exister pour agir) mais chaque story reste testable isolément une fois ces données de base présentes. Ordre d'implémentation recommandé = ordre de priorité P1 (US1→US7) puis P2 (US8→US12) puis P3 (US13).
- **Polish (Phase 16)** : dépend de toutes les user stories retenues pour la version livrée.

### User Story Dependencies

| Story | Dépend de (données/service) | Indépendamment testable |
|---|---|---|
| US1 Référentiel | Foundational uniquement | ✅ |
| US2 Auth/RBAC | Foundational uniquement | ✅ |
| US3 Achat | US1 (produits/fournisseurs), US2 (rôles) | ✅ |
| US4 Réception/Stock/FEFO | US3 (réception liée à une commande) | ✅ |
| US5 Qualité/Quarantaine | US4 (lots existants) | ✅ |
| US6 Pricing | US1 (produits) — indépendant de US3/US4 pour la simulation | ✅ |
| US7 Ventes/Facturation | US1 (clients), US4/US5 (lots libérés), US6 (prix) | ✅ |
| US8 Retours | US7 (ventes livrées) | ✅ |
| US9 Inventaire | US4 (lots/emplacements) | ✅ |
| US10 MRP | US1 (produits), US4 (mouvements historiques) | ✅ |
| US11 Reporting | Données produites par US1/US3/US4/US6/US7 | ✅ |
| US12 Notifications | Événements émis par US3/US4/US5/US10 (consolidation du Hub) | ✅ |
| US13 Conformité | US4/US7 (lots et ventes tracés) | ✅ |

### Within Each User Story

- Tests écrits en premier, doivent échouer avant l'implémentation.
- Entités (Core) → Repositories (Core) → DTOs (Service) → Services (Service, hérite du Repository) → Contrôleurs (Api) → Frontend.
- La story est considérée complète et validée uniquement après passage du Checkpoint.

### Parallel Opportunities

- Toutes les tâches Setup marquées `[P]` sont parallélisables.
- Toutes les tâches Foundational marquées `[P]` sont parallélisables (T013 à T016, T020-T022, T024-T029).
- Une fois Foundational terminé, US1 et US2 peuvent démarrer en parallèle (aucune dépendance croisée).
- Au sein d'une story, toutes les créations d'entités et toutes les pages frontend marquées `[P]` sont parallélisables.
- Plusieurs développeurs peuvent se répartir les stories P2 (US8-US12) en parallèle une fois US1-US7 (MVP) livrées.

---

## Parallel Example: User Story 1

```bash
# Tests (en parallèle) :
Task: "Test d'intégration création produit + doublon désignation dans codebase/backend/LABMEDIS.Tests/Integration/ProductTests.cs"
Task: "Test d'intégration import Excel catalogue dans codebase/backend/LABMEDIS.Tests/Integration/ProductImportTests.cs"

# Entités (en parallèle) :
Task: "Créer Category, TherapeuticClass, PharmaceuticalForm dans codebase/backend/LABMEDIS.Core/Models/Entities/"
Task: "Créer Product, ProductPackaging dans codebase/backend/LABMEDIS.Core/Models/Entities/Product.cs"
Task: "Créer Supplier, ProductSupplier dans codebase/backend/LABMEDIS.Core/Models/Entities/Supplier.cs"
Task: "Créer Customer, CustomerProductPrice dans codebase/backend/LABMEDIS.Core/Models/Entities/Customer.cs"

# Frontend (en parallèle, après les contrôleurs) :
Task: "Page Catalogue Produits dans codebase/frontend/src/pages/Products/"
Task: "Page Fournisseurs dans codebase/frontend/src/pages/Suppliers/"
Task: "Page Clients dans codebase/frontend/src/pages/Customers/"
```

---

## Implementation Strategy

### MVP First (User Stories 1 à 7 uniquement)

1. Compléter Phase 1 : Setup.
2. Compléter Phase 2 : Foundational (**critique** — bloque toutes les stories).
3. Compléter Phases 3 à 9 : US1 → US7, dans l'ordre (chaque story dépend des données produites par les précédentes dans ce chemin critique achat→stock→vente).
4. **ARRÊTER et VALIDER** : rejouer les scénarios 1 à 6 de `quickstart.md`.
5. Démo/déploiement du MVP (cycle complet achat international → réception/FEFO → contrôle qualité → pricing → vente/facturation traçable).

### Incremental Delivery (au-delà du MVP)

1. MVP (US1-US7) livré et validé.
2. Ajouter US8 (Retours) → tester indépendamment → livrer.
3. Ajouter US9 (Inventaire) → tester indépendamment → livrer.
4. Ajouter US10 (MRP) → tester indépendamment → livrer.
5. Ajouter US11 (Reporting) → tester indépendamment → livrer.
6. Ajouter US12 (Notifications) → tester indépendamment → livrer (peut aussi être développé en parallèle de US8-US11, le Hub consolide simplement les émissions déjà présentes).
7. Ajouter US13 (Conformité) → tester indépendamment → livrer.
8. Phase 16 (Polish) → durcissement transverse final.

### Parallel Team Strategy

Avec plusieurs développeurs, une fois Foundational terminé :
- Développeur A : US1 puis US3 puis US4 (chemin de données achat/stock)
- Développeur B : US2 (auth/RBAC, nécessaire tôt pour tester les permissions des autres stories)
- Développeur C : US6 (Pricing, indépendant dès que US1 existe)
- Puis, une fois US1-US7 stabilisées : répartir US8-US13 entre les développeurs disponibles, en parallèle.

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue.
- L'étiquette `[Story]` trace chaque tâche vers sa user story pour audit de couverture.
- Les tests unitaires marqués **bloquants** (FEFO, Pricing, CUMP, arrondi CFA) sont une exigence de la constitution — leur échec DOIT empêcher tout merge.
- Committer après chaque tâche ou groupe logique cohérent.
- S'arrêter à chaque Checkpoint pour valider la story indépendamment avant de poursuivre.
- Éviter : tâches vagues, conflits sur un même fichier au sein d'un groupe `[P]`, dépendances inter-stories qui casseraient l'indépendance testable.

---

## Phase 17: Convergence

- [ ] T176 Refactor services that inject a repository via constructor instead of inheriting it (`InvoicePdfService` has no repository inheritance at all; `SaleOrderService`, `PricingService`, `UserService`, `ReferentielService`, `WarehouseService` inject a secondary repository alongside inheritance) to comply with Constitution Principle II — resolve the single-inheritance conflict (e.g. via a secondary repository exposed through the inherited base, or a documented constitution amendment) in `codebase/backend/LABMEDIS.Service/Services/InvoicePdfService.cs`, `SaleOrderService.cs`, `PricingService.cs`, `UserService.cs`, `ReferentielService.cs`, `WarehouseService.cs` per Constitution Principle II (contradicts)
- [ ] T177 Align all API controllers on the mandated `[Route("api/[controller]")]` convention (currently 12 controllers use custom literal routes) and add the missing class-level `[Authorize]` to `AuthController`, reconciling with `contracts/*.md` route naming where needed, across `codebase/backend/LABMEDIS.Api/Controllers/*.cs` per Constitution Principle VII (contradicts)
- [ ] T178 Persist and journalize every manual FEFO override (lot id, user, quantity, reason, whether it deviated from the suggested lot) instead of validating the reason and discarding it, in `codebase/backend/LABMEDIS.Service/Services/StockLotService.cs` (`AllocateAsync`/`ReserveCoreAsync`) per FR-037 (partial)
- [ ] T179 Apply the category-specific expiry blocking thresholds (60j réactifs labo, 90j médicaments/cosmétiques/compléments, 120j produits infantiles) at reception instead of a flat 30-day threshold for all categories, in `codebase/backend/LABMEDIS.Service/Services/StockLotService.cs` (`ReceiveLineAsync`) per FR-031/US4-AC3 (contradicts)
- [ ] T180 Implement the missing purchase order state transitions (Envoyée, En Fabrication, Prête à Expédier, Expédiée, En Transit) with corresponding endpoints/service methods and status-history entries, in `codebase/backend/LABMEDIS.Service/Services/PurchaseOrderService.cs` and `codebase/backend/LABMEDIS.Api/Controllers/PurchaseOrdersController.cs` per FR-022 (partial)
- [ ] T181 Add a scheduled job that notifies Responsable Achats and Direction when a purchase order's expected delivery date has passed without full reception, in `codebase/backend/LABMEDIS.Service/Jobs/` per FR-027 (missing)
- [ ] T182 Filter the frontend navigation menu so only modules/actions authorized by the connected user's permissions are rendered, instead of a static unfiltered `navItems` array, in `codebase/frontend/src/routes/Layout.tsx` per FR-019/US2-AC6 (missing)
- [ ] T183 Ensure a deactivated user's already-issued JWT access token is rejected before natural expiry (e.g. `OnTokenValidated` re-checking `IsActive`/token revocation), in `codebase/backend/LABMEDIS.Api/Program.cs` per FR-018/US2-AC4 (partial)
- [ ] T184 Make pricing simulation resolve the pricing profile via the category/transport fallback chain (with explicit error/global-profile suggestion when unresolved) and validate the exchange rate against the `ExchangeRate` table server-side instead of trusting client input, in `codebase/backend/LABMEDIS.Service/Services/PricingService.cs` (`SimulateAsync`) per FR-053/US6-AC6 (missing)
- [ ] T185 Add exchange-rate administration (seed EUR/XOF at 655.957 as fixed, Admin-only CRUD with explicit audit logging, manual historized entry for USD/XOF) via a service and controller, in `codebase/backend/LABMEDIS.Service/Services/CurrencyService.cs`/new `ExchangeRateService` and `codebase/backend/LABMEDIS.Api/Controllers/CurrenciesController.cs` per FR-085 (missing)
- [ ] T186 Emit the `dashboardRefresh` SignalR event (or per-KPI events) from relevant services on business events (sale confirmed, stockout, expiry, etc.) targeted at dashboard viewers, since the frontend subscribes to an event nothing currently emits, in `codebase/backend/LABMEDIS.Service/Hubs/NotificationHub.cs` and relevant services per FR-075 (contradicts)
- [ ] T187 Implement the missing notification triggers (retard de livraison, réception en attente, expiration de licence DPML) so all 9 required event types in FR-076 actually emit, including any missing DPML license entity/job, in `codebase/backend/LABMEDIS.Service/` per FR-076 (partial)
- [ ] T188 Scope product/supplier/customer designation and CIP-code uniqueness checks to active records only (add `IsActive` to the existence checks/partial index, not just `IsDeleted`), in `codebase/backend/LABMEDIS.Core/Repositories/Product/`, `Supplier/`, `Customer/` and `AppDbContext.cs` per FR-002 (partial)
- [ ] T189 Scope the supplier lot number uniqueness check to the actual supplier of the receiving purchase order (resolve via Shipment→PurchaseOrderLine→PurchaseOrder.SupplierId) instead of ignoring the supplier id, in `codebase/backend/LABMEDIS.Core/Repositories/StockLot/` and `StockLotService.cs` per FR-030 (contradicts)
- [ ] T190 Transition any non-terminal lot (not already Périmé/Détruit, regardless of current quality status) to Périmé once its expiry date has passed, instead of only lots currently at "Libéré", in `codebase/backend/LABMEDIS.Service/Jobs/ExpiryAlertJob.cs` per FR-043 (partial)
- [ ] T191 Add an explicit expiry-date guard in stock reservation (not solely reliant on the daily expiry job) so an expired lot cannot be reserved/sold same-day before the batch job runs, in `codebase/backend/LABMEDIS.Service/Services/StockLotService.cs` (`ReserveCoreAsync`) per FR-037 (partial)
- [ ] T192 Update `StockLotLocation` quantities on stock transfer movements (decrement source, increment/create destination) so the per-location stock view no longer drifts from movement history, in `codebase/backend/LABMEDIS.Service/Services/StockMovementService.cs` per FR-038/FR-039 (partial)
- [ ] T193 Surface an actual warning/flag (response field or notification) when a customer's outstanding balance exceeds their credit limit under "Alert" enforcement mode, instead of silently doing nothing, in `codebase/backend/LABMEDIS.Service/Services/CustomerService.cs` (`EnsureCanOrderAsync`) per FR-009 (partial)
- [ ] T194 Add CRUD to attach/reorder a product's habitual suppliers by priority (`ProductSupplier`), in `codebase/backend/LABMEDIS.Service/Services/ProductService.cs` and `codebase/backend/LABMEDIS.Api/Controllers/ProductsController.cs` per FR-004 (missing)
- [ ] T195 Render invoice PDF amounts and currency labels according to the invoice's actual currency (XOF zero-decimal AwayFromZero, EUR two-decimal) instead of a hardcoded "XOF" string, in `codebase/backend/LABMEDIS.Service/Services/InvoicePdfService.cs` per FR-059/US7-AC7 (contradicts)
- [ ] T196 Extend the lot traceability endpoint/DTO to include the full chain (shipment → purchase order → supplier), not just the lot and its customers, in `codebase/backend/LABMEDIS.Service/Services/ComplianceService.cs` and `ComplianceResponse.cs` per FR-081/US13-AC2 (partial)
- [ ] T197 Add report export support for the Direction dashboard (FR-068) and rotation/slow-moving report (FR-070), and fix the "stock" export to use the actual stock breakdown rather than expiring lots, in `codebase/backend/LABMEDIS.Service/Services/ReportingService.cs` per FR-074 (partial)
- [ ] T198 Add a "request recount" state/action on an inventory session so an abnormal variance can trigger a re-comptage before closure, in `codebase/backend/LABMEDIS.Core/Models/Entities/InventorySession.cs` and `InventorySessionService.cs` per US9/AC4 (missing)
- [ ] T199 Include in-transit stock (quantities on open, non-closed purchase orders) in the reorder-point comparison and suggested reorder quantity, in `codebase/backend/LABMEDIS.Service/Services/ForecastService.cs` per FR-063/FR-064 (partial)
- [ ] T200 Wire the critical-notification email/SMS relay to real FluentEmail/Twilio dispatch instead of only logging and stamping a timestamp, in `codebase/backend/LABMEDIS.Service/Services/NotificationService.cs` per FR-079 (partial)
- [ ] T201 Replace the per-row duplicate-designation database round-trip in bulk product import with a single preloaded set of existing designations/CIP codes to protect the 200-rows/10s bound at scale, in `codebase/backend/LABMEDIS.Service/Services/ProductService.cs` (`ImportAsync`) per FR-006 (partial)
- [ ] T202 Add `.Include(i => i.Customer)` to the invoice query used for PDF generation so the client name no longer renders blank, in `codebase/backend/LABMEDIS.Core/Repositories/Invoice/InvoiceRepository.cs` (`GetBySaleOrderIdAsync`) per FR-058 (contradicts)
- [ ] T203 Review whether `RolePermission`/`UserPermissionException` physical `RemoveRange` deletes should instead be soft deletes under Constitution Principle III, or explicitly document them as append-only-exempt like the other listed tables, in `codebase/backend/LABMEDIS.Core/Repositories/Permission/PermissionRepository.cs` and `codebase/backend/LABMEDIS.Service/Services/PermissionService.cs` per Constitution Principle III (contradicts)
- [ ] T204 Add backup/replication (continuous archiving or scheduled backup) for the PostgreSQL data volume to satisfy the near-zero RPO requirement, in `docker-compose.yml` / deployment configuration per FR-093/SC-016 (missing)
