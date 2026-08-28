# RG-002 : Calcul PMP

## 1. Règle Générale
Le PMP DOIT être recalculé à CHAQUE RÉCEPTION de lot.

## 2. Formule
`PMP = Σ(quantité_lot × PRU_lot) / Σ(quantité_lot)`
Sur TOUS les lots disponibles d'un même produit.

## 3. Inclusion des lots
- Statut = `Libéré` ET quantité restante > 0.

## 4. Fixité du PRU
Le PRU d'un lot EST figé à la réception (JAMAIS recalculé a posteriori).

## 5. Pondération
Les lots bateau et avion ont des PRU différents, le PMP DOIT pondérer tous les lots disponibles.