# RG-009 : Arrondi CFA

## 1. Règle
- Devise CFA (XOF) : ZÉRO décimale.
- Arrondi : `Math.Round(value, 0, MidpointRounding.AwayFromZero)`.

## 2. Application
- Extension method C# : `public static decimal ToCfaRounded(this decimal value) { return Math.Round(value, 0, MidpointRounding.AwayFromZero); }`
- Sur PA_CFA, PR_CFA, PV_HT, PV_TTC.

## 3. Contrainte
- Les calculs intermédiaires DOIVENT conserver la précision decimal avant arrondi final.