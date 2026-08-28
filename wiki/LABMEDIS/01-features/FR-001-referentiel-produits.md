---
id: "LABMEDIS-FR-001"
projet: "LABMEDIS"
type: "feature"
titre: "FR-001 — Référentiel Produits"
priorite: "Critique"
statut: "validé"
source_raw: ["raw/PRD_CLAUDE.md §8.1", "raw/PRD_Qwen.md §5.1", "raw/LABMEDIS-modele-donnees.md §D2"]
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#feature", "#produit", "#référentiel"]
depends_on: ["[[ENT-001-product]]", "[[API-002-products]]", "[[FR-009-prevision-mrp]]"]
---

# FR-001 — Référentiel Produits

> [!abstract] 🏛️ Salle du Conseil
> **ARIA :** Le référentiel produits est le socle de tout le système. Chaque produit mal configuré = erreur de pricing, de stock, de TVA.
> **MARCUS :** Liste contrôlée pour catégorie, forme, classe thérapeutique — zéro saisie libre tolérée.
> **ZARA :** Le flag `IsTaxable` pour les réactifs labo DOIT être par produit, pas hérité de la catégorie seule.
> **LEON :** Toutes les règles en impératif. Validations exhaustives. Crit Gherkin testables.
> **Consensus :** Référentiel validé, catalogue de 138 ref existantes à importer depuis Excel.

^[src: raw/PRD_CLAUDE.md §8.1]

---

## Périmètre

Gestion complète du catalogue de produits LABMEDIS : création, modification, désactivation, consultation.

**Catalogue actuel :** 138 références (93 réactifs labo HORIBA ABX, 22 médicaments, 17 produits infantiles France Lait, 2 cosmétiques, 2 compléments alimentaires, 1 insecticide).

---

## Acteurs

| Acteur | Action autorisée |
|---|---|
| Admin | CRUD complet + désactivation + import Excel |
| Direction | CRUD complet |
| Responsable Achats | Création + modification |
| Tous (authentifiés) | Lecture seule |
| Rôles hors liste | Lecture seule si permission `Products.Read` |

---

## Données d'un Produit

| Champ | Type DB | Requis | Règles |
|---|---|---|---|
| `designation` | VARCHAR(250) | ✅ | Unique parmi actifs (index partiel WHERE deleted_at IS NULL) |
| `category_id` | UUID FK | ✅ | Référence table `categories` — liste contrôlée |
| `therapeutic_class_id` | UUID FK | ❌ | Référence `therapeutic_classes` — null autorisé |
| `pharmaceutical_form` | VARCHAR(100) | ❌ | Liste contrôlée (comprimé, sirop, injectable, etc.) |
| `dosage` | VARCHAR(100) | ❌ | ex: "400g", "500ml", "10mg/ml" |
| `code_cip` | VARCHAR(50) | ❌ | Unique parmi actifs |
| `default_transport_mode` | VARCHAR(20) | ❌ | CHECK IN (Maritime, Aerien, Express, Terrestre) |
| `manufacture_lead_days` | INT | ❌ | >= 0 — utilisé par MRP |
| `delivery_lead_days` | INT | ❌ | >= 0 — utilisé par MRP |
| `safety_stock_qty` | INT | ❌ | >= 0, défaut 0 — utilisé par MRP |
| `vat_rate` | DECIMAL(5,4) | ✅ | 0.0000 à 0.9999 |
| `is_taxable` | BOOL | ✅ | Défaut TRUE — OBLIGATOIRE pour réactifs labo |
| `is_active` | BOOL | ✅ | Défaut TRUE |

**Relations :**
- N:N Fournisseurs → `product_suppliers` (fournisseurs habituels du produit)
- 1:N Conditionnements → `product_packagings` (unité/carton/palette/colis express)
- 1:N StockLots, 1:N ProductPrices

---

## Règles Métier (OBLIGATOIRES)

1. La désignation DOIT être unique parmi les produits actifs (index partiel `WHERE deleted_at IS NULL`). Deux produits inactifs PEUVENT avoir la même désignation.
2. La catégorie EST REQUISE et DOIT référencer une valeur de la table `categories`.
3. Le code CIP DOIT être unique parmi les produits actifs s'il est renseigné.
4. Le délai de fabrication (`manufacture_lead_days`) et le délai de livraison (`delivery_lead_days`) DOIVENT être configurables par produit — ils alimentent le calcul du point de commande MRP.
5. Le seuil de stock de sécurité (`safety_stock_qty`) DOIT être configurable par produit (défaut : 0 unités).
6. Un produit PEUT avoir plusieurs conditionnements simultanés (ex: unité 1 pièce + carton 24 + palette 576). Ces conditionnements DOIVENT être dans `product_packagings`, jamais en champ plat.
7. Un produit PEUT être lié à plusieurs fournisseurs habituels via `product_suppliers` — relations ordonnées par priorité.
8. La suppression DOIT être un soft delete (`IsDeleted = true`, `DeletedAt = NOW()`). Le DELETE physique EST INTERDIT.
9. Un produit désactivé (`is_active = false`) NE DOIT PAS apparaître dans les listes de sélection des formulaires d'achat, de vente ou de réception.
10. La catégorie, la forme pharmaceutique et la classe thérapeutique DOIVENT être des listes contrôlées (tables de référentiel) — aucune saisie libre en texte libre.
11. Le flag `is_taxable` DOIT être configuré par produit individuellement pour les réactifs de laboratoire (voir [[RG-007-tva]]).

---

## Validations API

```csharp
public class CreateProductRequest
{
    [Required(ErrorMessage = "La désignation est obligatoire")]
    [StringLength(250, MinimumLength = 1)]
    public string Designation { get; set; }

    [Required(ErrorMessage = "La catégorie est obligatoire")]
    public Guid CategoryId { get; set; }

    public Guid? TherapeuticClassId { get; set; }

    [StringLength(100)]
    public string PharmaceuticalForm { get; set; }

    [StringLength(100)]
    public string Dosage { get; set; }

    [StringLength(50)]
    public string CodeCip { get; set; }

    [AllowedValues("Maritime", "Aerien", "Express", "Terrestre")]
    public string DefaultTransportMode { get; set; }

    public int? ManufactureLeadDays { get; set; }   // >= 0
    public int? DeliveryLeadDays { get; set; }       // >= 0
    public int SafetyStockQty { get; set; } = 0;     // >= 0

    [SwaggerSchema("Taux TVA string — ex: '0.18' (18%) ou '0.00' (exonéré)")]
    [Required]
    public string VatRate { get; set; }              // STRING OBLIGATOIRE

    public bool IsTaxable { get; set; } = true;
}
```

---

## Codes Réponse API

| Code | Situation |
|---|---|
| `201 Created` | Produit créé avec succès |
| `400 Bad Request` | Données invalides (champs manquants, format incorrect) |
| `401 Unauthorized` | Token absent ou expiré |
| `403 Forbidden` | Permission `Products.Create` absente |
| `409 Conflict` | Désignation déjà utilisée par un produit actif |
| `422 Unprocessable Entity` | Catégorie inexistante / CIP déjà utilisé |

---

## Critères d'Acceptation (Gherkin)

```gherkin
Fonctionnalité: Référentiel Produits

  Scénario: Création nominale
    Étant donné que je suis connecté en tant que Responsable Achats
    Et que la catégorie "Produit infantile" existe
    Quand je soumets {"designation": "France Lait 1er âge 400g", "category_id": "...", "vat_rate": "0.18"}
    Alors je reçois 201 Created
    Et le produit est visible dans le catalogue

  Scénario: Désignation dupliquée
    Étant donné qu'un produit actif "France Lait 1er âge 400g" existe déjà
    Quand je soumets une création avec la même désignation
    Alors je reçois 409 Conflict
    Et le message d'erreur DOIT contenir "DESIGNATION_DUPLICATE"

  Scénario: Soft-delete puis recréation
    Étant donné qu'un produit "France Lait 1er âge 400g" est soft-deleted
    Quand je crée un nouveau produit "France Lait 1er âge 400g"
    Alors je reçois 201 Created (le produit supprimé ne bloque pas la recréation)

  Scénario: Montant TVA en décimal côté backend
    Étant donné que je soumets vat_rate = "0.18"
    Alors le backend convertit en decimal 0.18 avant stockage
    Et le prix TTC calculé = PV HT × 1.18

  Scénario: Produit désactivé invisible
    Étant donné qu'un produit "X" est désactivé (is_active = false)
    Quand un commercial cherche "X" dans le formulaire de commande vente
    Alors "X" N'APPARAÎT PAS dans les résultats de recherche
```

---

## Import Masse

- L'import Excel (`.xlsx`) du catalogue LABMEDIS DOIT être supporté (138+ lignes)
- Implémentation : `EFCore.BulkExtensions.BulkInsertOrUpdateAsync`
- Validation : chaque ligne validée avant import — rapport d'erreurs par ligne
- Performance cible : 200 produits en < 10 secondes

---

*Source : raw/PRD_CLAUDE.md §8.1 | raw/PRD_Qwen.md §5.1 | raw/LABMEDIS-modele-donnees.md §D2*
← [[_index|Hub LABMEDIS]] | Voir aussi : [[ENT-001-product]] | [[API-002-products]] | [[FR-006-tarification-pricing]]
