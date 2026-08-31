# Contrat API — Stock, Traçabilité FEFO, Qualité, Inventaire (US4, US5, US9)

Toutes les routes `[Authorize]`, préfixées `api/stock`.

## Disponibilité & Mouvements

| Route | Permission | Description |
|---|---|---|
| `GET /api/stock/available?productId=` | `Stock.Read` | `RemainingQuantity − ReservedQuantity − Quarantaine − Périmé` par produit (FR-035) |
| `POST /api/stock/movements` | `Stock.Move` | Enregistre un mouvement libre (transfert, ajustement) — `reason` obligatoire pour les ajustements (FR-038) |
| `GET /api/stock/lots/{id}` | `Stock.Read` | Détail d'un lot (statut, quantités, PRU) |

## FEFO & Allocation

| Route | Permission | Description |
|---|---|---|
| `GET /api/stock/fefo-suggestion?productId=&quantity=` | `Stock.Read` | Retourne la liste ordonnée `[{lotId, expiryDate, quantityAllocated, location}]` selon l'algorithme FEFO (FR-036). `422 INSUFFICIENT_STOCK` si le total disponible est inférieur à la quantité demandée ; `422 NO_AVAILABLE_LOT` si aucun lot `Libéré` disponible. |
| `POST /api/stock/lots/allocate` | `Stock.Move` | Alloue explicitement un lot différent du 1er FEFO — `{ "lotId": "guid", "quantity": "int", "reason": "string (non vide)" }` → `400` si `reason` vide (FR-037) ; `409 INSUFFICIENT_STOCK` en cas de conflit de réservation concurrente (FR-091, voir research.md §5) |

## Qualité & Quarantaine

| Route | Permission | Description |
|---|---|---|
| `POST /api/stock/lots/{id}/quarantine` | `Stock.Move` | `{ "reason": "string (non vide)", "quarantineLocationId": "guid" }` (FR-042) |
| `POST /api/stock/lots/{id}/release` | `Quality.Release` (**Responsable Qualité uniquement**) | Transition `Libéré` — `403` pour tout autre rôle (FR-042, Principe VIII) |
| `POST /api/stock/lots/{id}/non-conforme` | `Quality.Release` | `{ "reason": "string (non vide)" }` |
| `POST /api/stock/lots/{id}/destroy` | `Quality.Release` + `Admin` | `{ "destructionDocumentRef": "string (requis)" }` |
| `POST /api/stock/lots/{id}/suspected-falsified` | `Admin` \| `Direction` | Déclenche l'alerte autorité compétente (FR-083) |

## Inventaire (US9)

| Route | Permission | Description |
|---|---|---|
| `POST /api/stock/inventory-sessions` | `Inventory.Manage` | Crée une session sur un périmètre (zone/emplacement), gèle les mouvements (FR-044) |
| `GET /api/stock/inventory-sessions/{id}` | `Inventory.Manage` | Détail + écarts calculés |
| `POST /api/stock/inventory-sessions/{id}/counts` | `Inventory.Manage` | `{ "stockLotId": "guid", "countedQuantity": "int" }` |
| `POST /api/stock/inventory-sessions/{id}/validate` | `Inventory.Validate` | Crée les ajustements motivés et clôture la session |

---

**Traçabilité** : FR-029 à FR-044, FR-091, SC-001, SC-002, SC-009, SC-013.
