---
id: "LABMEDIS-RG-004"
projet: "LABMEDIS"
type: "rule"
titre: "RG-004 — Cascade des Prix & Moteur de Pricing"
priorite: "Critique"
statut: "validé"
source_raw: ["raw/PRD_Qwen - 1- Règles Financières & Moteur de Pricing.md §1.1-1.5", "raw/PRD_CLAUDE.md §10.4"]
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#rule", "#pricing", "#finance"]
depends_on: ["[[RG-003-conversion-devise]]", "[[ENT-008-pricing-profile]]", "[[ENT-009-product-price]]"]
---

# LABMEDIS-RG-004 : Cascade des Prix & Moteur de Pricing

> [!abstract] 🏛️ Salle du Conseil
> **ARIA :** Le moteur de prix est le cœur financier de LABMEDIS. Toute erreur ici = marge faussée.
> **MARCUS :** Les coefficients ne peuvent JAMAIS être hardcodés. Table PricingProfile obligatoire.
> **ZARA :** Validé sur données réelles France Lait 400g. Chaque étape DOIT être arrondie CFA uniquement en final.
> **LEON :** DOIT/NE DOIT PAS partout. Formule exacte avec valeurs validées.
> **Consensus :** Formule cascade en 5 étapes, coefficients en BDD, arrondi uniquement en sortie finale.

^[src: raw/PRD_Qwen - 1.md §1.1]

---

## Formule Officielle LABMEDIS (5 étapes)

```
ÉTAPE 1 — Conversion devise
PA (CFA) = PA (EUR ou USD) × taux_change_figé

ÉTAPE 2 — Application Commission/Promotion
= PA (CFA) × Coeff_Commission

ÉTAPE 3 — Application Fret
= résultat_étape2 × Coeff_Freight

ÉTAPE 4 — Application Transit
= résultat_étape3 × Coeff_Transit

ÉTAPE 5 — Application Frais de Transfert
PR (CFA) = résultat_étape4 × Coeff_FraisTransfert

ÉTAPE 6 — Calcul Prix de Vente
PV HT Calculé = PR (CFA) × Coeff_Marge
```

**Forme condensée :**
```
PR (CFA) = PA (CFA) × Coeff_Commission × Coeff_Freight × Coeff_Transit × Coeff_FraisTransfert
PV HT Calculé = PR (CFA) × Coeff_Marge
```

---

## Valeurs Validées (Gamme France Lait 1er âge 400g)

| Étape | Opération | Résultat |
|---|---|---|
| PA (EUR) | Donnée source | 3,41 € |
| Taux EUR/XOF | 655,957 (fixe) | — |
| PA (CFA) | 3,41 × 655,957 | 2 237 CFA |
| × Coeff_Commission | × 1,25 | 2 796 CFA |
| × Coeff_Freight | × 1,03 | 2 880 CFA |
| × Coeff_Transit | × 1,09 | 3 139 CFA |
| × Coeff_FraisTransfert | × 1,07 | **PR = 3 359 CFA** |
| × Coeff_Marge | × 1,10 | **PV HT Calculé = 3 695 CFA** |

> [!info] IMPLICITE — Les calculs intermédiaires DOIVENT conserver la précision `decimal` complète. L'arrondi CFA (`MidpointRounding.AwayFromZero`) ne s'applique QU'AU RÉSULTAT FINAL (PR et PV HT).

---

## Règles Métier

1. Les coefficients DOIVENT être stockés dans la table [[ENT-008-pricing-profile|PricingProfile]] en base de données.
2. Les coefficients NE DOIVENT JAMAIS être hardcodés dans le code source.
3. Les coefficients PEUVENT varier selon : `TransportMode` (Maritime/Aérien/Express/Terrestre) ET `CategoryId` (nullable = règle globale).
4. Le `PV HT Appliqué` PEUT différer du `PV HT Calculé` (ajustement manuel autorisé par Direction/Admin).
5. L'écart (`PV HT Calculé - PV HT Appliqué`) DOIT être calculé et stocké dans [[ENT-009-product-price|product_prices.price_gap]].
6. L'écart NE DOIT JAMAIS être écrasé ou mis à zéro sans action explicite.
7. Toute modification de PV DOIT créer une nouvelle ligne dans `product_prices` (historique) — jamais d'UPDATE sur l'ancienne ligne.
8. Le taux de change utilisé DOIT être le taux figé au moment de la commande d'achat (`locked_exchange_rate_id`).

---

## Entité PricingProfile (Coefficients)

```csharp
public class PricingProfile : BaseEntity
{
    public string Name { get; set; }              // ex: "Import Maritime Lait Infantile"
    public Guid? SupplierId { get; set; }         // null = règle globale
    public Guid? CategoryId { get; set; }         // null = toutes catégories
    public string TransportMode { get; set; }     // Maritime|Aerien|Express|Terrestre

    // Coefficients — JAMAIS hardcodés
    public decimal CommissionCoeff { get; set; }  // défaut 1.25
    public decimal FreightCoeff { get; set; }     // défaut 1.03
    public decimal TransitCoeff { get; set; }     // défaut 1.09
    public decimal TransferFeeCoeff { get; set; } // défaut 1.07
    public decimal TargetMarginCoeff { get; set; }// défaut 1.10

    public bool IsActive { get; set; } = true;
}
```

---

## Implémentation C# — Service Pricing

```csharp
public class PricingService : PricingRepository, IPricingService
{
    private readonly ILoggerManager _logger;

    public PricingService(AppDbContext context, ILoggerManager logger) : base(context)
    {
        _logger = logger;
    }

    public PricingSimulationResponse CalculateLandingCost(PricingSimulation request)
    {
        // 1. Récupérer le profil de coefficients
        var profile = GetById(request.PricingProfileId);
        if (profile == null) throw new Exception("PricingProfile introuvable.");

        // 2. Conversion devise (string → decimal)
        decimal paCfa = request.PurchasePriceForeign.ToDecimal() * request.ExchangeRate.ToDecimal();

        // 3. Formule en cascade (précision decimal préservée)
        decimal prCfa = paCfa
                      * profile.CommissionCoeff
                      * profile.FreightCoeff
                      * profile.TransitCoeff
                      * profile.TransferFeeCoeff;

        // 4. Marge
        decimal pvHtCalcule = prCfa * profile.TargetMarginCoeff;

        // 5. Arrondi CFA uniquement en sortie finale
        return new PricingSimulationResponse
        {
            PurchasePriceCfa = paCfa.ToCfaRounded(),
            LandingCostCfa = prCfa.ToCfaRounded(),
            TargetPriceHtCfa = pvHtCalcule.ToCfaRounded()
        };
    }
}
```

---

## DTO Request (Règle d'or — montants en string)

```csharp
public class SimulatePricingRequest
{
    [SwaggerSchema(Description = "ID du profil pricing (ex: Maritime Lait Infantile)")]
    public Guid PricingProfileId { get; set; }

    [SwaggerSchema(Description = "Prix d'achat en devise étrangère — format string (ex: '3.41')")]
    public string PurchasePriceForeign { get; set; } // STRING OBLIGATOIRE

    [SwaggerSchema(Description = "Taux de change du jour — format string (ex: '655.957')")]
    public string ExchangeRate { get; set; } // STRING OBLIGATOIRE

    public PricingSimulation ToDomainModel() => new PricingSimulation
    {
        PricingProfileId = this.PricingProfileId,
        PurchasePriceForeign = this.PurchasePriceForeign,
        ExchangeRate = this.ExchangeRate
    };
}
```

---

## Extension Method — Arrondi CFA

```csharp
public static class DecimalExtensions
{
    /// <summary>
    /// Arrondi CFA (XOF) : zéro décimale, AwayFromZero.
    /// APPLIQUER UNIQUEMENT sur le résultat final, pas sur les intermédiaires.
    /// </summary>
    public static decimal ToCfaRounded(this decimal value)
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal ToDecimal(this string value)
    {
        return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
}
```

---

## Calcul TVA (Post-Prix de Vente)

```
PV TTC = PV HT Appliqué × (1 + taux_TVA)
```

| Catégorie | Taux TVA | Référence |
|---|---|---|
| Produit infantile | 18% (0.18) | Hors liste UEMOA |
| Cosmétique | 18% | Hors liste UEMOA |
| Complément alimentaire | 18% | Hors liste UEMOA |
| Insecticide | 18% | À confirmer |
| Médicament | 0% | Directive UEMOA 06/2002 |
| Réactif de laboratoire | Flag `IsTaxable` par produit | Variable |

Voir [[RG-007-tva]] pour détail exhaustif.

---

## Cas Limites & Erreurs

> [!failure] Cas : PricingProfile introuvable pour le mode de transport du lot
> **Condition :** Aucun profil actif pour `(CategoryId, TransportMode)` combinaison
> **Comportement :** RENVOYER `422 Unprocessable Entity` avec `{ "error": "PRICING_PROFILE_NOT_FOUND", "rule": "RG-004" }`
> **Fallback :** Proposer le profil global (`CategoryId = null`) si disponible

> [!failure] Cas : Taux de change manquant pour la devise
> **Condition :** Aucun taux actif pour la paire `(deviseSource, XOF)` à la date de commande
> **Comportement :** RENVOYER `422` avec `{ "error": "EXCHANGE_RATE_MISSING" }`

---

*Source : raw/PRD_Qwen - 1- Règles Financières & Moteur de Pricing.md §1.1-1.5 | raw/PRD_CLAUDE.md §10.4*
← [[_index|Hub LABMEDIS]] | ↑ [[../_meta/index|Index Global]]
