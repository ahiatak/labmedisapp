---
id: "LABMEDIS-ENT-006"
projet: "LABMEDIS"
type: "entity"
titre: "ENT-006 — StockLot (Lot de Stock)"
priorite: "Critique"
statut: "validé"
source_raw: ["raw/LABMEDIS-modele-donnees.md §D5", "raw/PRD_Qwen - 2- Gestion Physique des Stocks.md §2.3", "raw/PRD_CLAUDE.md §9"]
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#entity", "#stock", "#lot", "#traçabilité"]
depends_on: ["[[ENT-001-product]]", "[[ENT-005-shipment]]", "[[ENT-015-warehouse-location]]", "[[RG-001-fefo]]", "[[RG-008-quarantaine]]"]
---

# ENT-006 — StockLot (Lot de Stock Pharmaceutique)

> [!abstract] 🏛️ Salle du Conseil
> **ARIA :** Le lot est l'unité de traçabilité centrale du système. Tout mouvement de stock DOIT référencer un lot.
> **MARCUS :** `remaining_quantity <= initial_quantity` DOIT être une contrainte CHECK en base — pas seulement en service.
> **ZARA :** Le PRU fig\u00e9 sur le lot EST CRITIQUE pour le calcul du PMP. JAMAIS de recalcul a posteriori.
> **LEON :** Statuts lot exhaustifs. Contraintes CHECK documentées. Algorithme FEFO en lien.
> **Consensus :** Lot = unité immuable après création. Seul `remaining_quantity` et `quality_status` sont mutables.

---

## Schéma PostgreSQL

```sql
CREATE TABLE stock_lots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    shipment_id UUID REFERENCES shipments(id) ON DELETE SET NULL,

    -- Identification du lot
    supplier_lot_number VARCHAR(100) NOT NULL,  -- N° lot du fournisseur
    internal_lot_number VARCHAR(100) NOT NULL UNIQUE,  -- N° lot interne LABMEDIS

    -- Dates critiques
    reception_date DATE NOT NULL,
    manufacturing_date DATE,
    expiry_date DATE NOT NULL,  -- OBLIGATOIRE pour FEFO

    -- Quantités (contraintes CHECK validées réellement en PostgreSQL)
    initial_quantity INT NOT NULL CHECK (initial_quantity > 0),
    remaining_quantity INT NOT NULL CHECK (remaining_quantity >= 0),
    reserved_quantity INT NOT NULL DEFAULT 0 CHECK (reserved_quantity >= 0),
    CHECK (remaining_quantity <= initial_quantity),
    CHECK (reserved_quantity <= remaining_quantity),

    -- Coût figé à la réception (JAMAIS recalculé a posteriori)
    unit_cost_cfa DECIMAL(15,2) NOT NULL,  -- PRU en CFA à la réception
    pricing_profile_id UUID REFERENCES pricing_profiles(id) ON DELETE SET NULL,

    -- Qualité & statut
    quality_status VARCHAR(30) NOT NULL DEFAULT 'EnRéception'
        CHECK (quality_status IN (
            'EnRéception', 'EnQuarantaine', 'Libéré',
            'NonConforme', 'Périmé', 'Détruit',
            'EnAttenteLibération', 'SuspectéFalsifié'
        )),
    quarantine_reason TEXT,  -- OBLIGATOIRE si quality_status IN (EnQuarantaine, NonConforme)
    released_by UUID REFERENCES users(id) ON DELETE SET NULL,
    released_at TIMESTAMPTZ,

    -- Audit
    received_by UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    deleted_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

-- Index critiques pour performance FEFO
CREATE INDEX ix_stock_lots_product_expiry ON stock_lots (product_id, expiry_date ASC)
    WHERE is_deleted = FALSE AND quality_status = 'Libéré';

CREATE INDEX ix_stock_lots_product_status ON stock_lots (product_id, quality_status)
    WHERE is_deleted = FALSE;

CREATE INDEX ix_stock_lots_expiry_alert ON stock_lots (expiry_date)
    WHERE is_deleted = FALSE AND quality_status NOT IN ('Périmé', 'Détruit');

-- Trigger updated_at
CREATE TRIGGER set_stock_lots_updated_at
    BEFORE UPDATE ON stock_lots
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
```

---

## Entité C# (LABMEDIS.Core)

```csharp
public class StockLot : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid? ShipmentId { get; set; }

    public string SupplierLotNumber { get; set; }   // N° fournisseur
    public string InternalLotNumber { get; set; }   // N° interne LABMEDIS (unique)

    public DateTime ReceptionDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }         // OBLIGATOIRE

    public int InitialQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public int ReservedQuantity { get; set; }

    public decimal UnitCostCfa { get; set; }         // PRU figé à réception
    public Guid? PricingProfileId { get; set; }

    public string QualityStatus { get; set; } = "EnRéception";
    public string? QuarantineReason { get; set; }
    public Guid? ReleasedBy { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid ReceivedBy { get; set; }

    // Navigation
    public Product Product { get; set; }
    public Shipment? Shipment { get; set; }
    public PricingProfile? PricingProfile { get; set; }
    public ApplicationUser ReceivedByUser { get; set; }
    public ICollection<StockLotLocation> Locations { get; set; }
    public ICollection<StockMovement> Movements { get; set; }
}
```

---

## Règle Unicité du Numéro de Lot

Le couple `(supplier_lot_number, product_id)` DOIT être unique parmi les lots actifs d'un même fournisseur.
Le numéro de lot interne (`internal_lot_number`) DOIT être unique dans tout le système.

**Format interne recommandé :** `{code_produit}-{AAAAMMJJ}-{NNN}` (ex: `FL400-20260115-001`)

---

## Statuts du Lot — Machine à États

```
EnRéception
    ↓ (contrôle magasinier)
    ├─→ EnQuarantaine (anomalie détectée, motif obligatoire)
    │       ↓ (contrôle Resp. Qualité)
    │       ├─→ Libéré (conforme)
    │       ├─→ NonConforme (rejeté)
    │       └─→ Détruit (destruction physique + document)
    └─→ EnAttenteLibération (en attente accord fabricant/DPML)
            ↓
            └─→ Libéré
                    ↓ (passage du temps)
                    └─→ Périmé (automatique si ExpiryDate < TODAY)

SuspectéFalsifié ← peut être déclenché depuis n'importe quel statut actif
```

| Transition | Acteur autorisé | Condition |
|---|---|---|
| `EnRéception` → `Libéré` | Resp. Qualité | Contrôle OK |
| `EnRéception` → `EnQuarantaine` | Magasinier / Resp. Qualité | Motif obligatoire |
| `EnQuarantaine` → `Libéré` | Resp. Qualité UNIQUEMENT | Après analyse |
| `EnQuarantaine` → `NonConforme` | Resp. Qualité | Motif obligatoire |
| `*` → `Détruit` | Resp. Qualité + Admin | Document destruction obligatoire |
| `*` → `SuspectéFalsifié` | Admin / Direction | Autorité compétente notifiée |
| `Libéré` → `Périmé` | Système (automatique) | ExpiryDate < TODAY |

---

## Quantités Disponibles

```
QtyDisponible = RemainingQuantity - ReservedQuantity
```

- `RemainingQuantity` : décrémenté à la LIVRAISON physique validée
- `ReservedQuantity` : incrémenté à la CONFIRMATION commande vente, décrémenté à la livraison

---

## Table Associée : stock_lot_locations

Un lot PEUT être stocké sur PLUSIEURS emplacements simultanément :

```sql
CREATE TABLE stock_lot_locations (
    stock_lot_id UUID NOT NULL REFERENCES stock_lots(id) ON DELETE CASCADE,
    storage_location_id UUID NOT NULL REFERENCES storage_locations(id) ON DELETE RESTRICT,
    quantity INT NOT NULL CHECK (quantity > 0),
    reserved_quantity INT NOT NULL DEFAULT 0,
    PRIMARY KEY (stock_lot_id, storage_location_id)
);
```

---

## Règle PRU (Prix de Revient Unitaire) — Figé

Le `unit_cost_cfa` EST CALCULÉ à la réception et JAMAIS recalculé :

```
unit_cost_cfa = PA_CFA × Coeff_Commission × Coeff_Freight × Coeff_Transit × Coeff_FraisTransfert
              = PA_EUR × taux_change_figé × produit_coefficients
```

Voir [[RG-004-cascade-prix]] pour la formule complète.

---

## Alertes Péremption (Job Hangfire Quotidien)

| Seuil | Catégorie | Action |
|---|---|---|
| J-120 | Produit infantile | SignalR `lot:expiringSoon` |
| J-90 | Médicament, Cosmétique, Complément | SignalR `lot:expiringSoon` |
| J-60 | Réactif de laboratoire | SignalR `lot:expiringSoon` |
| J-30 | Tous | Alerte urgente + email Resp. Qualité |
| J=0 | Tous | Statut automatique → `Périmé`, blocage vente |

---

*Source : raw/LABMEDIS-modele-donnees.md §D5 | raw/PRD_Qwen - 2.md §2.3 | raw/PRD_CLAUDE.md §9*
← [[_index|Hub LABMEDIS]] | Voir aussi : [[RG-001-fefo]] | [[RG-008-quarantaine]] | [[ENT-007-stock-movement]]
