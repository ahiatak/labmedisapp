# RG-006 : Unicité du lot

## 1. Unicité
- Le numéro de lot DOIT être unique par couple (fournisseur_id, produit_id).
- Deux lots NE PEUVENT PAS partager le même numéro pour le même produit du même fournisseur.

## 2. Quantité
- Quantité reçue = en unités réelles (JAMAIS calculée depuis carton × nb/carton).

## 3. Emplacements
- Un lot PEUT être stocké sur PLUSIEURS emplacements (`stock_lot_locations`).
- Un emplacement PEUT contenir PLUSIEURS lots différents.