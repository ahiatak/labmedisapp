---
id: "LABMEDIS-ARCH-001"
projet: "LABMEDIS"
type: "architecture"
titre: "Architecture technique — Stack, Patterns, Contraintes"
priorite: "Critique"
statut: "validé"
source_raw: ["raw/PRD_CLAUDE.md §12", "raw/PRD_Qwen.md §4", "raw/PRD_Qwen - 1- Règles Financières.md §A-C"]
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#architecture", "#backend", "#frontend"]
---

# LABMEDIS — Architecture Technique

> [!abstract] 🏛️ Salle du Conseil
> **ARIA :** Architecture 3 couches imposée. Chaque couche a un rôle précis et étanche.
> **MARCUS :** Les 6 règles d'or sont NON NÉGOCIABLES — toute déviation est rejetée en PR.
> **ZARA :** Un développeur qui ne lit que ce fichier DOIT pouvoir structurer son code correctement.
> **LEON :** Langage impératif sur chaque règle. DOIT/NE DOIT PAS/EST REQUIS sur toutes les lignes.
> **Consensus :** Ce fichier fait foi pour toute revue de code.

---

## 1. Organisation du Dépôt

```
codebase/
├── frontend/    ← ReactJS (TypeScript, Vite, TailwindCSS/MUI)
└── backend/     ← .NET 9 (C#), 3 projets distincts
    ├── LABMEDIS.Core/
    ├── LABMEDIS.Service/
    └── LABMEDIS.Api/
```

---

## 2. Stack Technique

| Composant | Technologie |
|---|---|
| Backend | C# / .NET 9 |
| Frontend | ReactJS + TypeScript + Vite |
| UI Library | TailwindCSS ou Material-UI |
| ORM | Entity Framework Core + EFCore.BulkExtensions |
| Base de données | PostgreSQL (59 tables validées, v18.3) |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Jobs planifiés | **Hangfire** (EXCLUSIF) |
| Temps réel | **SignalR** + StackExchange.Redis |
| Logging | **NLog** via ILoggerManager (EXCLUSIF) |
| PDF | DinkToPdf (BL, factures, rapports) |
| Email | FluentEmail |
| SMS | Twilio |
| Cache | MemoryCache / Redis |
| User-Agent | UAParser |

---

## 3. Les 6 Règles d'Or (NON NÉGOCIABLES)

### Règle 1 — Héritage Service/Repository

Le Service DOIT hériter du Repository. Injection par constructeur EST INTERDITE.

```csharp
// ✅ CORRECT
public class ProductService : ProductRepository, IProductService
{
    public ProductService(AppDbContext context, ILoggerManager logger) : base(context) { }
}

// ❌ INTERDIT
public class ProductService : IProductService
{
    private readonly IProductRepository _repo; // INJECTION INTERDITE
}
```

### Règle 2 — Soft Delete Exclusif

TOUTE suppression DOIT passer par `IsDeleted = true`. DELETE physique EST INTERDIT.

```csharp
entity.IsDeleted = true;
entity.DeletedAt = DateTime.UtcNow;
await UpdateAsync(entity);
```

BaseEntity DOIT porter : `Id (Guid)`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `IsDeleted`.
Query Filters EF Core DOIT exclure `IsDeleted = true` globalement.

### Règle 3 — ILoggerManager Exclusif (NLog)

`ILogger<T>` standard EST INTERDIT. Utiliser EXCLUSIVEMENT `ILoggerManager`.

**Format log OBLIGATOIRE (avant action) :**
```
"{LastName} {FirstName} ({UserName}) | Début [NomAction] | {Method} {Path} IP: {IP} UserManager: {UA}"
```

**Format log OBLIGATOIRE (dans catch) :**
```
"{LastName} {FirstName} ({UserName}) | Echec [NomAction] : {ExMessage} | IP: {IP}"
```

### Règle 4 — Mapping Manuel

AutoMapper EST INTERDIT. Mapping DTO → Entity DOIT être manuel via méthode `To[Entity]()`.
DTO Response DOIT avoir constructeur prenant l'entité : `public XResponse(X entity) { ... }`.

### Règle 5 — Champs Financiers en String dans Requests

Tous les montants dans les DTOs Request DOIVENT être `string`. Types `decimal`/`double` INTERDITS dans les Requests.
Conversion : `.ToDecimal()` (extension method) côté service.

```csharp
public string PurchasePriceEur { get; set; }  // "3.41" — STRING OBLIGATOIRE
public string ExchangeRate { get; set; }        // "656.00" — STRING OBLIGATOIRE
```

### Règle 6 — Structure Contrôleur Obligatoire

```csharp
[ApiController, Route("api/[controller]"), Authorize]
public class XController : ControllerBase
{
    // Injecter : IXService, ILoggerManager, IUserService
    // 1. var user = await _userService.GetCurrentUserAsync(User);
    // 2. _logger.LogInfo("... | Début [Action] | ...");
    // 3. try { action → Ok(result) }
    //    catch (Exception ex) { _logger.LogError(ex, "..."); return BadRequest({message}); }
    // JAMAIS StatusCode(500)
}
```

---

## 4. LABMEDIS.Core — Structure

```
LABMEDIS.Core/
├── Models/Entities/          ← Toutes les entités : héritent de BaseEntity
├── Repositories/Base/        ← BaseRepository<T>
├── Repositories/[Entité]/    ← I[Entité]Repository + [Entité]Repository
└── AppDbContext.cs
```

Le Repository ne contient que les requêtes complexes (`.Include`, `.ThenInclude`, `Where` avancés).
Le CRUD de base vient de `BaseRepository`.

---

## 5. LABMEDIS.Service — Structure

```
LABMEDIS.Service/
├── DTOs/Requests/     ← CreateXRequest.cs, UpdateXRequest.cs
├── DTOs/Responses/    ← XResponse.cs
├── Services/          ← XService.cs (hérite XRepository)
├── Jobs/              ← StockForecastJob.cs, ExpiryAlertJob.cs
└── Hubs/              ← StockAlertHub.cs
```

**Jobs Hangfire obligatoires :**
- `StockForecastJob` : quotidien (nuit) — calcule vélocité 90j, point de commande, crée suggestions MRP
- `ExpiryAlertJob` : quotidien — détecte lots à 30/60/90/120j de péremption, alerte SignalR

---

## 6. Base de Données PostgreSQL — 59 Tables

| Domaine | Tables |
|---|---|
| D1 Sécurité | users, roles, permissions, role_permissions, user_roles, user_permission_exceptions, refresh_tokens, audit_logs, company_profile, user_password_history |
| D2 Référentiel | products, categories, suppliers, customers, therapeutic_classes, product_suppliers, product_packagings |
| D3 Pricing | currencies, exchange_rates, pricing_profiles, product_prices, pricing_simulations, customer_product_prices |
| D4 Achats | purchase_orders, purchase_order_lines, purchase_order_status_history, shipments, shipment_lines, import_costs, shipment_events |
| D5 Stock | warehouses, storage_locations, stock_lots, stock_lot_locations, stock_movements, inventory_sessions, inventory_counts |
| D6 Ventes | sale_orders, sale_order_lines, sale_order_status_history, deliveries, delivery_lines, invoices, invoice_lines, credit_notes, credit_note_lines, customer_returns, return_lines |
| D7 MRP | forecast_parameters, supplier_lead_times, forecast_calculations, reorder_suggestions |
| D8 Reporting | notifications, notification_reads, daily_sales_summary, daily_stock_summary, monthly_financial_summary |

**Conventions :** UUID PK, snake_case, tables pluriel, FK `[singulier]_id`, statuts `VARCHAR+CHECK IN(...)`, index unique partiel `WHERE deleted_at IS NULL`, trigger `set_updated_at()`.

---

## 7. Frontend React

- Auth JWT (access 15-30min, refresh 7-30j) + SignalR temps réel
- `ProtectedRoute` + `PermissionGate` : basés sur permissions JWT claims
- Menu dynamique : affiche UNIQUEMENT les modules autorisés par le rôle
- Formulaires devises : masques saisie CFA (séparateurs milliers)
- Scan barcode/QR : emplacements + lots entrepôt
- Charts : Recharts (péremptions, CA, marges)
- Interface : 100% français, dates DD/MM/YYYY

---

## 8. Notifications SignalR (Pas de Polling)

| Événement | Déclencheur |
|---|---|
| `stock:low` | Stock dispo < seuil produit |
| `stock:outOfStock` | Stock dispo = 0 |
| `lot:expiringSoon` | Péremption dans 30/60/90/120j |
| `shipment:arrived` | Conteneur/fret arrivé |
| `mrp:suggestion` | Suggestion MRP créée |
| `order:lateDelivery` | Commande fournisseur en retard |

---

*Source : raw/PRD_CLAUDE.md §12 | raw/PRD_Qwen.md §4 | raw/PRD_Qwen - 1- Règles Financières.md §A-C*
← [[_index|Hub LABMEDIS]] | ↑ [[../_meta/index|Index Global]]
