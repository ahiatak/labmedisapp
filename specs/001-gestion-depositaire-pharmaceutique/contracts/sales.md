# Contrat API — Ventes, Facturation, Retours (US7, US8)

Toutes les routes `[Authorize]`, préfixées `api/sale-orders` (sauf mention).

| Route | Permission | Description |
|---|---|---|
| `POST /api/sale-orders` | `Sales.Create` | Création `Brouillon` (FR-054) |
| `GET /api/sale-orders/{id}/fefo-suggestion` | `Sales.Read` | Allocation FEFO proposée par ligne (délègue à `GET /api/stock/fefo-suggestion`) |
| `POST /api/sale-orders/{id}/confirm` | `Sales.Create` | Réserve le stock pour chaque ligne (FR-055). **`409 INSUFFICIENT_STOCK`** si une confirmation concurrente a déjà consommé la quantité disponible (FR-091/SC-013) — le client relance avec la disponibilité à jour. |
| `POST /api/sale-orders/{id}/cancel` | `Sales.Create` | Libère toute réservation associée (FR-055) |
| `POST /api/sale-orders/{id}/deliver` | `Sales.Deliver` | Génère le bon de livraison, décrémente le stock physique (FR-057) |
| `POST /api/sale-orders/{id}/invoice` | `Sales.Invoice` (Comptable/Direction) | Génère la facture — chaque ligne référence `stockLotId` (FR-058) |
| `GET /api/sale-orders/{id}/invoice/pdf` | `Sales.Read` | Export PDF (DinkToPdf), numéro de lot visible (FR-058) |
| `POST /api/sale-orders/{id}/returns` | `Sales.Create` \| `Quality.Release` | Voir `CreateReturnRequest` ci-dessous (FR-060 à FR-062) |

**CreateSaleOrderRequest** :
```json
{
  "customerId": "guid (requis, actif)",
  "currencyId": "guid (XOF|EUR)",
  "lines": [ { "productId": "guid", "quantity": "int (>0)" } ]
}
```
`400 CUSTOMER_INACTIVE` si le client est inactif (FR-010) · `409` si l'encours dépasse le plafond configuré (FR-009, selon configuration alerte/blocage).

**CreateReturnRequest** :
```json
{
  "saleOrderLineId": "guid",
  "quantity": "int (>0)",
  "disposition": "RemiseEnStock|Quarantaine|Destruction",
  "motif": "string (requis si Quarantaine)"
}
```
Réponse `201` avec l'avoir généré (`creditNoteId`) — FR-062.

---

**Traçabilité** : FR-054 à FR-062, FR-091, SC-002, SC-010, SC-013.
