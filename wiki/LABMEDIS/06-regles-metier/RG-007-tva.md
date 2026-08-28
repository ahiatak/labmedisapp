# RG-007 : Gestion de la TVA

## 1. Catégories
| Catégorie | TVA | Base légale |
|---|---|---|
| Produit infantile | 18% | Hors liste UEMOA |
| Cosmétique | 18% | Hors liste UEMOA |
| Complément alimentaire | 18% | Hors liste UEMOA |
| Insecticide | 18% | À confirmer |
| Médicament | 0% | Directive UEMOA 06/2002 |
| Réactif de laboratoire | Flag IsTaxable | Variable |

## 2. Règles
- La TVA DOIT être configurable par produit (jamais déduite automatiquement de la catégorie seule).
- `PV TTC = PV HT × (1 + taux_TVA)`.