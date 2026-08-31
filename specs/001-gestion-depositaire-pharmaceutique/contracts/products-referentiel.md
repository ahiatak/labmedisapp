# Contrat API — Référentiel : Produits, Fournisseurs, Clients (US1)

Toutes les routes `[Authorize]`. Codes de réponse alignés Principe VII (jamais `500` explicite ; `BadRequest` avec `{ "message": "..." }` convivial dans le `catch`).

## Produits — `api/products`

| Route | Permission | Description |
|---|---|---|
| `GET /api/products` | `Products.Read` | Liste paginée/filtrée (masque les produits `IsActive=false` des sélecteurs via un paramètre `selectableOnly`, FR-005) |
| `GET /api/products/{id}` | `Products.Read` | Détail — `404` si introuvable |
| `POST /api/products` | `Products.Create` | Voir Request ci-dessous |
| `PUT /api/products/{id}` | `Products.Update` | Mise à jour complète |
| `DELETE /api/products/{id}` | `Products.Delete` | Soft delete (`204`) — jamais physique (Principe III) |
| `GET /api/products/{id}/stock` | `Products.Read` | Stock disponible agrégé (tous lots) |
| `POST /api/products/import` | `Products.Create` | Import Excel en masse (FR-006), réponse `202`/`200` avec rapport d'erreurs par ligne |

**CreateProductRequest** (`FR-001`, `FR-002`, `FR-087`) :
```json
{
  "designation": "string (requis, ≤250)",
  "categoryId": "guid (requis)",
  "therapeuticClassId": "guid|null",
  "pharmaceuticalForm": "string|null",
  "dosage": "string|null",
  "codeCip": "string|null",
  "defaultTransportMode": "Maritime|Aerien|Express|Terrestre|null",
  "manufactureLeadDays": "int|null",
  "deliveryLeadDays": "int|null",
  "safetyStockQty": "int (défaut 0)",
  "vatRate": "string (ex: '0.18') — STRING obligatoire (Principe VI)",
  "isTaxable": "bool (défaut true)"
}
```
Codes : `201 Created` · `400` validation · `401`/`403` · `409 Conflict` (`DESIGNATION_DUPLICATE`) · `422` (catégorie inexistante / CIP déjà utilisé).

## Fournisseurs — `api/suppliers`

CRUD standard (`Suppliers.Read`/`Create`/`Update`/`Delete`) + `GET /api/suppliers/{id}/purchase-history`. `name`, `country`, `defaultCurrencyId` obligatoires (FR-007) ; `409` si nom dupliqué parmi actifs.

## Clients — `api/customers`

CRUD standard (`Customers.Read`/`Create`/`Update`/`Delete`) + `GET /api/customers/{id}/outstanding-balance` (encours courant, FR-009) + `GET/PUT /api/customers/{id}/negotiated-prices` (FR-011, validation absence de chevauchement de période → `422 OVERLAPPING_PRICE_PERIOD`).

CreateCustomerRequest :
```json
{
  "name": "string (requis)",
  "type": "Répartiteur|Hôpital|Clinique|Pharmacie|CentraleAchat|Autre",
  "address": "string|null",
  "paymentDays": "int (défaut 30)",
  "creditLimit": "string|null — STRING obligatoire (Principe VI)"
}
```
`409` si nom dupliqué parmi actifs.

---

**Traçabilité** : FR-001 à FR-011, SC-003, SC-004.
