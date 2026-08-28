Voici la spécification détaillée et technique de la section **1. Règles Financières & Moteur de Pricing**, rédigée spécifiquement pour servir de contrat de développement pour le backend `.NET 9` et le frontend `ReactJS`. 

Cette analyse est basée sur la rétro-ingénierie exacte du fichier *Structure de prix.xlsx* (Gamme France Lait) et des règles fiscales togolaises applicables au catalogue LABMEDIS.

---

# 1. 💰 Règles Financières & Moteur de Pricing (Spécifications Backend)

Le moteur de pricing de LABMEDIS ne se limite pas à une simple marge commerciale. Il s'agit d'un **calcul de coût de revient complet (Landing Cost)** intégrant la logistique internationale multi-modale (Aérien vs Maritime) et les frais de structure, avant application de la marge cible.

## 1.1. La Formule Mathématique du Prix de Revient (PR)
L'analyse du fichier *Structure de prix.xlsx* démontre que le Prix de Revient (PR) est calculé via une **multiplication en cascade** de coefficients multiplicateurs sur le Prix d'Achat (PA) converti en CFA.

**Formule Officielle LABMEDIS :**
```text
PR (CFA) = PA (CFA) × Coeff_Commission × Coeff_Freight × Coeff_Transit × Coeff_FraisTransfert
```

**Exemple vérifié (France Lait 1er âge 400g) :**
*   **PA Euro** : 3,41 €
*   **Taux de change** : ~656 XOF/EUR (2237 / 3,41)
*   **PA CFA** : 2 237 CFA
*   **Coefficients** : Promo (1.25) × Freight (1.03) × Transit (1.09) × Frais transfert (1.07)
*   **Calcul** : `2237 × 1.25 × 1.03 × 1.09 × 1.07 = 3359,17` ➔ **Arrondi à 3 359 CFA**

**Calcul du Prix de Vente HT Cible (PV Théorique) :**
```text
PV HT Calculé = PR (CFA) × Coeff_Marge
```
*   **Marge France Lait** : 1.10 (soit 10% de markup sur le PR)
*   **Calcul** : `3359 × 1.10 = 3694,9` ➔ **Arrondi à 3 695 CFA**
*   *Note Métier :* Le système devra comparer ce `PV HT Calculé` avec le `Prix Labmedis HT` (prix catalogue politique) pour alerter la direction si la marge réelle s'érode (ex: Différence de -35 CFA sur le 1er âge 400g).

## 1.2. Architecture des Coefficients (Entités `Core`)
Les coefficients ne doivent **jamais** être codés en dur. Ils doivent être stockés en base de données pour permettre à la direction de les ajuster sans redéploiement, et doivent pouvoir varier selon le **Mode de Transport** (Aérien vs Maritime) et la **Catégorie de Produit**.

**Entité `PricingProfile` (LABMEDIS.Core) :**
*   `Id` (Guid)
*   `Name` (string, ex: "Import Maritime Lait Infantile")
*   `SupplierId` (Guid? - nullable pour une règle globale)
*   `CategoryId` (Guid? - nullable)
*   `TransportMode` (Enum: Maritime, Aerien, Express, Terrestre)
*   `CommissionCoeff` (decimal)
*   `FreightCoeff` (decimal)
*   `TransitCoeff` (decimal)
*   `TransferFeeCoeff` (decimal)
*   `TargetMarginCoeff` (decimal)

## 1.3. Gestion Multi-Devises et Taux de Change
LABMEDIS achète en **EUR** (Europe, Maroc, Tunisie), **USD** (Inde, Suisse) et revend localement en **XOF (CFA)**.

*   **Règle d'Or :** Le taux de change n'est pas fixe. Il doit être saisi (ou récupéré via API) au moment de l'émission de la `PurchaseOrder` (Commande Fournisseur).
*   **Entité `ExchangeRate`** :
    *   `CurrencyFrom` (ex: EUR)
    *   `CurrencyTo` (ex: XOF)
    *   `Rate` (decimal)
    *   `EffectiveDate` (DateTime)
*   **Logique Service :** Lors de la validation d'une commande d'achat, le système fige le taux de change du jour sur la ligne de commande (`LockedExchangeRate`) pour garantir la traçabilité financière du lot, même si le taux change le lendemain.

## 1.4. Fiscalité et Mapping TVA
L'analyse croisée des fichiers de produits révèle une fiscalité stricte basée sur la **Catégorie Thérapeutique/Commerciale** :

| Catégorie (Entité `Category`) | Taux de TVA par Défaut | Exemples de Produits |
| :--- | :--- | :--- |
| **Produit Infantile** | **18%** | France Lait (1er, 2ème, 3ème âge, Céréales) |
| **Cosmétique** | **18%** | Pommade Maïa |
| **Complément Alimentaire** | **18%** | B-PROTEI (ALL, MOM) |
| **Insecticide** | **18%** (à confirmer) | Strick Out Gel |
| **Médicament** | **0% (Exonéré)** | Galpharma, Iberma (Antibiotiques, Antalgiques...) |
| **Réactif de Laboratoire** | **0% ou 18%** | Horiba ABX (Nécessite un flag `IsTaxable` sur le produit) |

*   **Impact Backend :** Le `SaleOrderService` doit calculer automatiquement le `PV TTC` en fonction du `VatRate` de la catégorie du produit.

## 1.5. Stratégie d'Arrondi Monétaire (Rounding Strategy)
En franc CFA (XOF), il n'y a pas de centimes. Les calculs en cascade génèrent des décimales qui doivent être normalisées pour éviter les écarts de centimes entre le système et la comptabilité réelle.

*   **Règle :** Arrondi à l'entier le plus proche (MidpointRounding.AwayFromZero) pour le XOF.
*   **Implémentation .NET (Extension Method) :**
    ```csharp
    public static decimal ToCfaRounded(this decimal value) 
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }
    ```

---

## ⚙️ Spécifications d'Implémentation (.NET 9)

### A. Couche Service : DTOs et Mapping (Respect des Règles d'Or)
Pour éviter les bugs de désérialisation JSON côté Frontend React (qui utilise des claviers numériques avec des virgules ou des points selon la locale du navigateur), **tous les montants financiers dans les Requests DOIVENT être des `string`**.

**Fichier : `SimulatePricingRequest.cs` (LABMEDIS.Service/DTOs/Requests)**
```csharp
public class SimulatePricingRequest 
{
    [SwaggerSchema(Description = "ID du profil de pricing (ex: Maritime Lait)")]
    public Guid PricingProfileId { get; set; }

    [SwaggerSchema(Description = "Prix d'achat unitaire en devise étrangère (ex: '3.41')")]
    public string PurchasePriceForeign { get; set; } // STRING OBLIGATOIRE

    [SwaggerSchema(Description = "Taux de change du jour (ex: '656.01')")]
    public string ExchangeRate { get; set; } // STRING OBLIGATOIRE

    // Méthode de mapping manuel (Règle d'or)
    public PricingSimulation ToDomainModel() 
    {
        return new PricingSimulation 
        {
            PricingProfileId = this.PricingProfileId,
            PurchasePriceForeign = this.PurchasePriceForeign.ToDecimal(), // Extension method custom
            ExchangeRate = this.ExchangeRate.ToDecimal()
        };
    }
}
```

### B. Couche Service : Logique Métier (`PricingService`)
Le service hérite du repository (Règle d'or) et encapsule la formule en cascade.

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
        // 1. Récupération du profil (Coefficients)
        var profile = GetById(request.PricingProfileId); 
        
        // 2. Conversion devise
        decimal paCfa = request.PurchasePriceForeign * request.ExchangeRate;

        // 3. Formule en cascade
        decimal prCfa = paCfa 
                      * profile.CommissionCoeff 
                      * profile.FreightCoeff 
                      * profile.TransitCoeff 
                      * profile.TransferFeeCoeff;

        // 4. Marge
        decimal pvHtTheorique = prCfa * profile.TargetMarginCoeff;

        return new PricingSimulationResponse 
        {
            PurchasePriceCfa = paCfa.ToCfaRounded(),
            LandingCostCfa = prCfa.ToCfaRounded(),
            TargetPriceHtCfa = pvHtTheorique.ToCfaRounded()
        };
    }
}
```

### C. Couche API : Contrôleur et Logging
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public PricingController(IPricingService pricingService, ILoggerManager logger, IUserService userService)
    {
        _pricingService = pricingService;
        _logger = logger;
        _userService = userService;
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulatePricing([FromBody] SimulatePricingRequest request)
    {
        var user = await _userService.GetCurrentUserAsync(User);
        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début SimulatePricing | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try 
        {
            var domainModel = request.ToDomainModel();
            var result = _pricingService.CalculateLandingCost(domainModel);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec SimulatePricing : {ex.Message} | IP: {Request.GetIp()}");
            return BadRequest(new { message = "Erreur lors de la simulation du pricing. Vérifiez les formats numériques." });
        }
    }
}
```
