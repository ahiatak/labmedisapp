# Quickstart — Validation de bout en bout : LABMEDIS

**Feature**: 001-gestion-depositaire-pharmaceutique | **Phase**: 1 (Design)

Ce guide décrit comment faire tourner le système localement et valider, scénario par scénario, que les user stories P1 fonctionnent réellement — sans dupliquer le détail des contrats (`contracts/`) ou du modèle de données (`data-model.md`).

## Prérequis

- .NET 9 SDK
- Node.js 20+ et npm
- PostgreSQL 16+ (local ou conteneur Docker)
- Redis (pour le backplane SignalR)
- Docker (recommandé pour lancer PostgreSQL/Redis rapidement plutôt que les installer en local)

## Mise en route

```bash
# Infrastructure locale (PostgreSQL + Redis)
docker compose up -d postgres redis

# Backend
cd codebase/backend
dotnet restore
dotnet ef database update --project LABMEDIS.Core --startup-project LABMEDIS.Api
dotnet run --project LABMEDIS.Api

# Frontend (autre terminal)
cd codebase/frontend
npm install
npm run dev
```

L'API expose Swagger sur `/swagger` (environnement de développement) — utile pour rejouer manuellement les scénarios ci-dessous avant que l'UI ne soit branchée.

## Données de départ

Importer le référentiel initial via `POST /api/products/import` (fichier Excel des 138 références), créer les 8 fournisseurs et 12 clients connus (voir `data-model.md` §1), et au moins un `PricingProfile` global avec les coefficients validés (Commission ×1.25, Freight ×1.03, Transit ×1.09, Transfert ×1.07, Marge ×1.10).

## Scénarios de validation (alignés sur `spec.md`)

### 1. Référentiel (US1)
1. Créer un produit avec `POST /api/products` (désignation unique, catégorie existante, `vatRate: "0.18"`).
2. Retenter la même désignation → attendu `409 DESIGNATION_DUPLICATE`.
3. Désactiver le produit (`DELETE /api/products/{id}`) puis chercher via le sélecteur de commande de vente → attendu absent des résultats.

**Preuve de réussite** : FR-001, FR-002, FR-005 vérifiés ; correspond au Scénario 1-3 de US1 dans `spec.md`.

### 2. Auth & RBAC (US2)
1. `POST /api/auth/login` avec un compte `Commercial` → recevoir `accessToken` + permissions.
2. Avec ce token, appeler `POST /api/stock/lots/{id}/release` → attendu `403` (seul `Quality.Release` peut libérer un lot).
3. Saisir un mauvais mot de passe 5 fois de suite → 6e tentative attendue `423 Locked`.

**Preuve de réussite** : FR-014, FR-016, SC-011.

### 3. Achat International → Réception → FEFO (US3, US4, US5) — chemin critique
1. Créer une commande d'achat (`POST /api/purchase-orders`) avec un montant sous le seuil de validation → statut `Brouillon`.
2. `POST /api/purchase-orders/{id}/submit` puis `validate` → statut `Validée`.
3. Faire progresser jusqu'à `EnTransit` (endpoints de statut/`shipments`).
4. `POST /api/purchase-orders/{id}/receive` avec un lot dont la péremption est > 90 jours → lot créé au statut `EnRéception`, PRU calculé et figé.
5. `POST /api/stock/lots/{id}/release` (compte Responsable Qualité) → statut `Libéré`.
6. `GET /api/stock/fefo-suggestion?productId=&quantity=` → le lot libéré apparaît en premier si sa péremption est la plus proche.

**Preuve de réussite** : FR-020 à FR-043, SC-001, SC-002, SC-009 — chaîne complète achat → stock → traçabilité.

### 4. Pricing (US6)
1. `POST /api/pricing/simulate` avec `purchasePriceForeign: "3.41"`, `exchangeRate: "655.957"` et le profil global.
2. Vérifier `targetPriceHtCfa` ≈ `3695` (valeurs validées France Lait 400g, voir `pricing.md`).
3. Appliquer un `pvHtApplied` différent (`PUT /api/pricing/products/{id}/apply-price`) → vérifier que `priceGap` est calculé et qu'une nouvelle ligne d'historique apparaît (`GET /api/pricing/products/{id}/history`) sans écraser l'ancienne.

**Preuve de réussite** : FR-045 à FR-050, SC-008.

### 5. Vente → Facturation avec traçabilité (US7)
1. Créer une commande de vente pour un client actif (`POST /api/sale-orders`) sur le produit dont un lot est `Libéré`.
2. `POST /api/sale-orders/{id}/confirm` → réservation créée sur le lot FEFO.
3. `POST /api/sale-orders/{id}/deliver` puis `POST /api/sale-orders/{id}/invoice`.
4. `GET /api/sale-orders/{id}/invoice/pdf` → le PDF généré fait apparaître le numéro de lot vendu.

**Preuve de réussite** : FR-054 à FR-059, SC-010.

### 6. Conflit de réservation concurrente (FR-091, clarification de session)
1. Amener le stock disponible d'un lot à exactement 1 unité.
2. Depuis deux clients HTTP distincts, envoyer simultanément deux `POST /api/sale-orders/{id}/confirm` visant chacun 1 unité de ce lot.
3. Attendu : une confirmation réussit (`200`), l'autre échoue avec `409 INSUFFICIENT_STOCK` — aucune des deux quantités réservées ne dépasse le stock réellement disponible.

**Preuve de réussite** : FR-091, SC-013 — valide la décision de concurrence documentée dans `research.md` §5.

### 7. Notification hors-ligne (FR-094, clarification de session)
1. Se connecter avec un utilisateur `Responsable Qualité`, puis se déconnecter du Hub SignalR (fermer l'onglet).
2. Déclencher un événement `lot:expiringSoon` pendant la déconnexion (ex. avancer la date système ou exécuter `POST /api/forecast/run`/job d'alerte péremption).
3. Se reconnecter et appeler `GET /api/notifications?unreadOnly=true` → la notification émise pendant la déconnexion est présente.

**Preuve de réussite** : FR-094 — valide qu'aucune alerte critique n'est perdue.

## Tests automatisés correspondants

Chaque scénario ci-dessus doit avoir un test d'intégration équivalent dans `codebase/backend/LABMEDIS.Tests/Integration/` (Testcontainers PostgreSQL, voir `research.md` §3) exécuté en CI ; les scénarios FEFO/Pricing/CUMP/arrondi CFA (constitution §Qualité) sont **bloquants** pour tout merge.
