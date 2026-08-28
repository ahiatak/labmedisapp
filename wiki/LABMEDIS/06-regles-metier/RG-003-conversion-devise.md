# RG-003 : Règles devises

## 1. EUR/XOF
- Taux FIXE à 655.957.
- Modifiable UNIQUEMENT par Admin avec action explicite.

## 2. USD/XOF
- Taux variable, saisi manuellement par Admin, historisé avec date d'application.

## 3. Taux figé sur commande
- Le `locked_exchange_rate_id` EST figé sur la commande et JAMAIS recalculé a posteriori.
- Date de figement : date d'émission de la commande.

## 4. Conversion en cascade
- Tous les calculs de pricing en CFA DOIVENT utiliser le taux figé du lot.