---
id: "LABMEDIS-RG-001"
projet: "LABMEDIS"
type: "rule"
titre: "RG-001 — FEFO (First Expired, First Out)"
priorite: "Critique"
statut: "validé"
source_raw: ["raw/PRD_Qwen - 2- Gestion Physique des Stocks & Entrepôt.md §2.4.4", "raw/PRD_CLAUDE.md §10.5"]
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#rule", "#stock", "#fefo"]
depends_on: ["[[ENT-006-stock-lot]]", "[[FR-005-entreposage-stock]]", "[[FR-008-ventes-facturation]]"]
---

# LABMEDIS-RG-001 : FEFO — First Expired, First Out

> [!abstract] 🏛️ Salle du Conseil
> **ARIA :** FEFO est une règle pharmaceutique obligatoire — pas un choix de conception.
> **MARCUS :** L'algorithme de sélection DOIT être implémenté côté backend. Le frontend AFFICHE la sélection proposée.
> **ZARA :** La dérogation manuelle DOIT être tracée — si un magasinier choisit LOT-B avant LOT-A sans motif, c'est une faille réglementaire.
> **LEON :** DOIT/NE DOIT PAS sur chaque ligne. Algorithme en pseudo-code testable.
> **Consensus :** FEFO automatique par défaut, dérogation autorisée avec conditions et traçabilité obligatoire.

^[src: raw/PRD_Qwen - 2.md §2.4.4]

---

## Définition

**FEFO = First Expired, First Out** : le lot dont la date de péremption est la plus proche DOIT être sorti en premier lors de toute sortie de stock (vente, transfert, picking).

---

## Algorithme de Sélection FEFO (Backend)

```
ENTRÉE : product_id, quantité_demandée

1. Récupérer TOUS les lots du produit depuis stock_lots
2. EXCLURE les lots avec expiry_date <= TODAY (périmés)
3. EXCLURE les lots avec quality_status IN ('EnQuarantaine', 'NonConforme', 'Détruit', 'EnAttenteLibération', 'SuspectéFalsifié')
4. INCLURE UNIQUEMENT les lots avec quality_status = 'Libéré'
5. CALCULER quantité_disponible_par_lot = remaining_quantity - reserved_quantity
6. EXCLURE les lots avec quantité_disponible <= 0
7. TRIER par expiry_date ASC (péremption la plus proche en premier)
8. TRIER secondairement par emplacement prioritaire (location_type = 'Picking' en premier)
9. ALLOUER jusqu'à satisfaire quantité_demandée :
   - Prélever sur LOT[0] jusqu'à épuisement OU satisfaction
   - Si insuffisant : continuer sur LOT[1], LOT[2]...
10. SI stock total disponible < quantité_demandée :
    RETOURNER erreur INSUFFICIENT_STOCK avec stock_disponible = X
    OU RETOURNER allocation partielle si mode partiel activé

SORTIE : liste [{lot_id, quantité_allouée, expiry_date, emplacement}]
```

---

## Statuts de Lot Proposables à la Vente

| Statut | Proposable en vente | Raison |
|---|---|---|
| `Libéré` | ✅ OUI | Contrôle qualité validé |
| `En réception` | ❌ NON | Pas encore contrôlé |
| `En quarantaine` | ❌ NON | Bloqué qualité |
| `Non conforme` | ❌ NON | Bloqué qualité |
| `En attente de libération` | ❌ NON | Pas libéré fabricant |
| `Suspecté falsifié` | ❌ NON | Bloqué, autorité à notifier |
| `Périmé` | ❌ NON | Date dépassée |
| `Détruit` | ❌ NON | Hors stock physique |

---

## Dérogation Manuelle (Conditions Cumulatives)

Un magasinier PEUT choisir manuellement un lot différent du premier FEFO SI ET SEULEMENT SI :

| Condition | Vérification backend |
|---|---|
| Le lot choisi n'est PAS périmé | `expiry_date > TODAY` |
| Le lot choisi a le statut `Libéré` | `quality_status = 'Libéré'` |
| La quantité disponible est suffisante | `remaining_qty - reserved_qty >= demandé` |
| Un motif est saisi | Champ `reason` NON NULL, NON VIDE |
| L'action est journalisée | Log ILoggerManager + AuditLog |

> [!warning] INFÉRÉ — Si le lot choisi n'est PAS le premier dans l'ordre FEFO, le frontend DOIT afficher un avertissement visible : "Attention : ce lot n'est pas le premier FEFO. Motif obligatoire."

---

## Exemple Métier

| Lot | Péremption | Dispo |
|---|---|---|
| LOT-A | 30/09/2026 | 200 unités |
| LOT-B | 31/12/2026 | 500 unités |
| LOT-C | 30/06/2027 | 300 unités |

**Commande client : 250 unités**

Allocation FEFO automatique :
```
LOT-A : 200 unités (épuisé)
LOT-B : 50 unités
Total : 250 unités ✅
```

> [!danger] JAMAIS allouer LOT-B en premier sans raison. Le backend DOIT rejeter une allocation qui ne respecte pas l'ordre FEFO sans motif documenté.

---

## Cas Limites

> [!failure] Cas : Stock total insuffisant
> **Condition :** Somme(quantités disponibles lots Libérés) < quantité demandée
> **Comportement DOIT :** RENVOYER `422 Unprocessable Entity`
> ```json
> { "error": "INSUFFICIENT_STOCK", "rule": "RG-001", "available": 180, "requested": 250, "product_id": "uuid" }
> ```

> [!failure] Cas : Tous les lots périmés ou bloqués
> **Condition :** Aucun lot en statut `Libéré` avec quantité disponible > 0
> **Comportement DOIT :** RENVOYER `422` avec `"error": "NO_AVAILABLE_LOT"`

> [!failure] Cas : Péremption très proche (< 30 jours)
> **Condition :** Le premier lot FEFO périme dans < 30 jours
> **Comportement DOIT :** AFFICHER une alerte `"warning"` dans la réponse mais autoriser l'allocation
> ```json
> { "data": [...], "warnings": [{"type": "EXPIRY_SOON", "lot_id": "uuid", "days_remaining": 15}] }
> ```

---

## Réservation de Stock

Lors de la confirmation d'une commande client :
- Le backend DOIT créer une réservation par lot alloué
- La réservation DOIT réduire le `stock_disponible` (pas le `stock_physique`)
- Le `stock_physique` n'est réduit QU'À la validation de la livraison
- Si la commande est annulée, la réservation DOIT être libérée immédiatement

```
stock_disponible = stock_physique - stock_réservé - stock_quarantaine
```

---

## Règles Complémentaires Péremption

| Seuil | Catégorie | Action |
|---|---|---|
| 120 jours | Produit infantile | Alerte SignalR + email |
| 90 jours | Médicament | Alerte SignalR + email |
| 90 jours | Cosmétique / Complément | Alerte SignalR |
| 60 jours | Réactif de laboratoire | Alerte SignalR |
| Périmé | Tous | Blocage automatique, déplacement zone Périmés |

---

*Source : raw/PRD_Qwen - 2- Gestion Physique des Stocks.md §2.4.4 | raw/PRD_CLAUDE.md §10.5*
← [[_index|Hub LABMEDIS]] | ↑ [[../_meta/index|Index Global]]
