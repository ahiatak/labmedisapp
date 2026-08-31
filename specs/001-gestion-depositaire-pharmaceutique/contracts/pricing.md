# Contrat API — Tarification (US6)

Toutes les routes `[Authorize]`, préfixées `api/pricing`.

## POST /api/pricing/simulate
**Permission** : `Pricing.Read` (tous rôles autorisés à consulter une simulation).

Request (Principe VI — montants en `string`) :
```json
{ "purchasePriceForeign": "string (ex: '3.41')", "exchangeRate": "string (ex: '655.957')", "pricingProfileId": "guid" }
```
Response `200` :
```json
{ "purchasePriceCfa": "string", "landingCostCfa": "string", "targetPriceHtCfa": "string", "targetPriceTtcCfa": "string" }
```
Calcul (FR-045, FR-046, FR-048, RG-004) : conversion devise → cascade `× CommissionCoeff × FreightCoeff × TransitCoeff × TransferFeeCoeff` (= prix de revient) → `× TargetMarginCoeff` (= PV HT calculé) → `× (1 + vatRate)` (= PV TTC). Précision `decimal` conservée jusqu'à l'arrondi CFA final (`ToCfaRounded()`).
Erreurs : `422 PRICING_PROFILE_NOT_FOUND` (aucun profil actif pour la combinaison catégorie/transport, avec tentative de repli sur le profil global) · `422 EXCHANGE_RATE_MISSING`.

## GET /api/pricing/profiles / POST /api/pricing/profiles / PUT /api/pricing/profiles/{id}
**Permission** : `Pricing.Update` — **réservé Admin et Direction** (FR-052). Corps : coefficients (`string`), `categoryId?`, `transportMode`, `supplierId?`.

## PUT /api/pricing/products/{id}/apply-price
**Permission** : `Pricing.Update`. Applique un `PvHtApplied` (peut différer du calculé) — crée une **nouvelle** ligne `ProductPrice` (jamais d'update, FR-050), calcule et conserve `PriceGap` (FR-049).
```json
{ "pvHtApplied": "string" }
```

## GET /api/pricing/products/{id}/history
Historique complet des lignes `ProductPrice` (PMP, PV calculé, PV appliqué, écart, date).

---

**Traçabilité** : FR-045 à FR-053, FR-087, SC-008.
