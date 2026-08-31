# Contrat API — Commandes d'Achat (US3)

Toutes les routes `[Authorize]`, préfixées `api/purchase-orders`.

| Route | Permission | Description |
|---|---|---|
| `GET /api/purchase-orders` | `PurchaseOrders.Read` | Liste filtrable par statut/fournisseur |
| `GET /api/purchase-orders/{id}` | `PurchaseOrders.Read` | Détail + lignes |
| `POST /api/purchase-orders` | `PurchaseOrders.Create` | Création au statut `Brouillon`, taux de change figé (FR-020, FR-021) |
| `POST /api/purchase-orders/{id}/submit` | `PurchaseOrders.Create` | Soumission — routage automatique vers `EnAttenteValidation` si montant > seuil (FR-023) |
| `POST /api/purchase-orders/{id}/validate` | `PurchaseOrders.Validate` (Direction, ou Achats si sous le seuil) | Transition → `Validée` |
| `POST /api/purchase-orders/{id}/cancel` | `PurchaseOrders.Create` | Requiert `{ "reason": "string (non vide)" }` (FR-024) — `400` si motif vide |
| `POST /api/purchase-orders/{id}/receive` | `Stock.Receive` | **Critique** — crée les lots, calcule PRU, recalcule PMP (FR-032, FR-033) |
| `GET /api/purchase-orders/{id}/status-history` | `PurchaseOrders.Read` | Historique complet des transitions (FR-022) |

**CreatePurchaseOrderRequest** :
```json
{
  "supplierId": "guid (requis, fournisseur actif)",
  "currencyId": "guid (requis)",
  "transportMode": "Maritime|Aerien|Express|Terrestre",
  "incoterm": "string|null",
  "lines": [
    { "productId": "guid", "quantity": "int (>0)", "cartonQuantity": "int|null", "unitPriceForeign": "string — STRING obligatoire", "packagingTypeId": "guid" }
  ]
}
```
Codes : `201` · `400` · `403` (permission/rôle insuffisant) · `422` (`EXCHANGE_RATE_MISSING` — aucun taux actif pour la devise à la date, voir Edge Cases spec).

**ReceivePurchaseOrderRequest** (`POST .../receive`) :
```json
[
  { "lineId": "guid", "lotNumber": "string", "expiryDate": "date", "quantityReceived": "int", "cartonsReceived": "int|null", "storageLocationId": "guid", "qualityStatus": "EnRéception|EnQuarantaine" }
]
```
Règles appliquées côté service (FR-029 à FR-032) : numéro de lot + péremption obligatoires ; blocage automatique si péremption sous le seuil catégorie (FR-031) ; PRU figé = `PA_CFA × coefficients` (voir `pricing.md`). Réponse `200` avec la liste des lots créés et le nouveau statut de la commande (`Reçue` ou `PartiellementReçue`).

---

**Traçabilité** : FR-020 à FR-028, FR-029 à FR-033 (réception), Edge Cases (taux de change manquant, annulation sans motif).
