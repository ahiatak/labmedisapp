# RG-005 : Soft Delete

## 1. Règle
- `IsDeleted = true` sur TOUTE suppression.
- JAMAIS de DELETE physique en base (zéro cascade delete physique).

## 2. Index unique
- Les index uniques DOIVENT être partiels : `WHERE deleted_at IS NULL`.

## 3. Entités concernées
- TOUTES les tables métier sauf tables append-only (user_password_history, notification_reads).