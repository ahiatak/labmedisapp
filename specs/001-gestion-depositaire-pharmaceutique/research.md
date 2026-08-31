# Phase 0 — Recherche Technique : LABMEDIS

**Feature**: 001-gestion-depositaire-pharmaceutique | **Date**: 2026-08-28

Ce document résout les inconnues techniques du Technical Context (plan.md) qui ne sont pas déjà tranchées par la constitution du projet. La majorité de la stack (architecture 3 couches, PostgreSQL, EF Core, Hangfire, SignalR, NLog/`ILoggerManager`, DTOs manuels, React/TypeScript/Vite) est **imposée sans marge de choix** par `.specify/memory/constitution.md` (v1.0.0) et n'est donc pas rediscutée ici.

---

## 1. Fournisseur EF Core pour PostgreSQL

**Decision**: `Npgsql.EntityFrameworkCore.PostgreSQL` comme fournisseur EF Core 9, avec `UseNpgsql()` dans `AppDbContext`.

**Rationale**: C'est le fournisseur PostgreSQL de référence pour EF Core, maintenu par l'équipe Npgsql, avec support natif des types PostgreSQL utilisés par le modèle (`UUID`/`gen_random_uuid()`, contraintes `CHECK`, index partiels `WHERE deleted_at IS NULL`). La constitution mentionne `Microsoft.EntityFrameworkCore.SqlServer`-équivalent PostgreSQL, ce qui pointe explicitement vers cet équivalent Npgsql plutôt que vers SQL Server.

**Alternatives considered**: `Microsoft.EntityFrameworkCore.SqlServer` (rejeté — contredit le choix PostgreSQL déjà acté dans l'architecture et le modèle de données à 59 tables) ; Dapper en complément pour les requêtes de reporting lourdes (retenu comme option future si les performances de reporting l'exigent, non nécessaire au lancement).

---

## 2. Stockage des jobs Hangfire

**Decision**: `Hangfire.PostgreSql` comme backing store, sur la même base PostgreSQL applicative (schéma dédié `hangfire`).

**Rationale**: La constitution impose Hangfire mais ne précise pas le store ; utiliser PostgreSQL (déjà la base de données du projet) évite d'introduire une dépendance d'infrastructure supplémentaire (ex. SQL Server ou Redis dédié aux jobs) et simplifie la sauvegarde/réplication (voir §6).

**Alternatives considered**: `Hangfire.Redis` (rejeté — ajouterait une dépendance de stockage supplémentaire pour les jobs alors que Redis est déjà réservé au backplane SignalR/cache, sans bénéfice net vu le volume de jobs — 2 jobs quotidiens planifiés : `StockForecastJob`, `ExpiryAlertJob`).

---

## 3. Framework de tests backend

**Decision**: xUnit pour les tests unitaires et d'intégration, `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) pour les tests de contrat/API, Testcontainers (image PostgreSQL officielle) pour fournir une base de données éphémère aux tests d'intégration.

**Rationale**: xUnit est le framework par défaut de l'écosystème .NET 9/ASP.NET Core moderne, avec la meilleure intégration `WebApplicationFactory` et le support natif du parallélisme de tests requis pour respecter l'exigence de couverture >80% de la constitution. Testcontainers garantit que les tests d'intégration (FEFO, PMP, workflows achat→vente) s'exécutent contre un vrai moteur PostgreSQL (contraintes `CHECK`, index partiels) plutôt qu'un fournisseur EF InMemory qui ne les valide pas.

**Alternatives considered**: NUnit/MSTest (rejetés — pas de gain fonctionnel, xUnit est le standard des nouveaux projets .NET) ; EF Core InMemory provider pour les tests (rejeté comme unique stratégie — ne vérifie pas les contraintes CHECK/index partiels critiques pour RG-001/RG-006, gardé uniquement pour des tests unitaires purs sans dépendance base).

---

## 4. Framework de tests frontend

**Decision**: Vitest + React Testing Library pour les tests unitaires/composants ; Playwright pour les parcours end-to-end critiques (achat → réception FEFO → vente → facturation PDF).

**Rationale**: Vitest s'intègre nativement à Vite (déjà retenu par la constitution) sans configuration de bundler séparée ; React Testing Library encourage des tests orientés comportement utilisateur cohérents avec les critères d'acceptation Given/When/Then du spec. Playwright couvre les scénarios multi-pages avec authentification JWT et SignalR, nécessaires pour valider les user stories P1.

**Alternatives considered**: Jest (rejeté — configuration redondante avec Vite déjà imposé) ; Cypress (rejeté au profit de Playwright pour son support multi-navigateurs natif et sa meilleure gestion des WebSockets/SignalR).

---

## 5. Gestion de la concurrence sur les réservations de stock (FR-091)

**Decision**: Vérification optimiste implémentée via une transaction PostgreSQL avec verrou de ligne `SELECT ... FOR UPDATE` sur le(s) lot(s) candidats au moment de la confirmation de commande, combinée à la contrainte CHECK déjà existante `reserved_quantity <= remaining_quantity` sur `stock_lots` comme filet de sécurité en base. La première transaction qui valide obtient la réservation ; toute transaction concurrente qui ne trouve plus de quantité disponible suffisante à la relecture échoue immédiatement avec l'erreur métier `INSUFFICIENT_STOCK` (HTTP 409 Conflict), sans attente prolongée ni blocage global du produit.

**Rationale**: Cette approche répond exactement à la clarification actée (FR-091/SC-013) — acceptation en parallèle, rejet ciblé du perdant — tout en restant proportionnée au volume réel (~200 ventes/jour, ~30 utilisateurs concurrents, SC-014) : un verrou de ligne court (borné à la durée de la transaction de confirmation) ne crée pas de contention perceptible à cette échelle, contrairement à un verrou pessimiste au niveau produit qui bloquerait toutes les ventes d'un même produit en attente.

**Alternatives considered**: Concurrence optimiste par jeton de version (`RowVersion`/`xmin` PostgreSQL) sans verrou explicite (rejetée — complexifie la boucle de retry côté service pour un gain négligeable au volume de LABMEDIS) ; verrou pessimiste au niveau produit entier (rejeté — sérialise inutilement toutes les ventes d'un même produit, y compris sur des lots différents non concernés par le conflit).

---

## 6. Sauvegarde et réplication (RPO ≈ quelques minutes, SC-016)

**Decision**: PostgreSQL configuré en réplication en continu (streaming replication avec archivage WAL) vers une instance de secours, complétée par des sauvegardes complètes quotidiennes ; bascule (failover) manuelle ou automatisée documentée en procédure d'exploitation (hors périmètre du code applicatif).

**Rationale**: La réplication en continu (WAL streaming) est le mécanisme standard PostgreSQL permettant un RPO de l'ordre de quelques minutes (voire secondes) sans développement applicatif spécifique — c'est une décision d'infrastructure/exploitation, pas une fonctionnalité du code LABMEDIS, mais elle conditionne le choix d'hébergement (§7).

**Alternatives considered**: Sauvegarde quotidienne seule (rejetée — RPO de 24h, incompatible avec SC-016) ; réplication synchrone multi-site (jugée disproportionnée au stade actuel — retenue comme piste d'évolution si LABMEDIS l'exige explicitement).

---

## 7. Cible d'hébergement

**Decision**: Conteneurs Linux (Docker) pour l'API .NET 9 et les jobs Hangfire, orchestrés simplement (Docker Compose ou équivalent) derrière un reverse proxy HTTPS ; PostgreSQL et Redis hébergés en instances gérées ou conteneurisées avec réplication (§6) permettant la disponibilité continue visée (SC-015).

**Rationale**: .NET 9 est pleinement multiplateforme et le déploiement conteneurisé Linux est le standard actuel de l'écosystème ASP.NET Core, facilitant la portabilité et les fenêtres de maintenance planifiées sans interruption longue (redéploiement conteneur à conteneur).

**Alternatives considered**: IIS sur Windows Server (rejeté — aucune contrainte du projet ne l'impose, et l'ajout d'une dépendance Windows complique la disponibilité continue et la reproductibilité des environnements) ; hébergement PaaS spécifique à un fournisseur cloud (non retenu à ce stade — décision d'infrastructure à affiner avec LABMEDIS, sans impact sur l'architecture applicative).

---

## 8. Génération PDF (DinkToPdf) sur cible Linux

**Decision**: Conserver `DinkToPdf` (imposé par la constitution) en empaquetant explicitement ses dépendances natives `libwkhtmltox` compatibles Linux dans l'image de conteneur de `LABMEDIS.Api`/`LABMEDIS.Service`.

**Rationale**: `DinkToPdf` s'appuie sur une bibliothèque native (`libwkhtmltox`) qui n'est pas incluse automatiquement sur les images Linux minimales ; le retenir tel qu'imposé par la constitution nécessite simplement de documenter cette dépendance native dans le Dockerfile plutôt que de dévier du choix constitutionnel.

**Alternatives considered**: Remplacer par une bibliothèque PDF « pure .NET » (rejeté — contredirait le Principe IX de la constitution qui impose `DinkToPdf` sans dérogation).

---

## 9. Rate limiting API

**Decision**: Middleware de rate limiting natif ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`, disponible depuis .NET 7+) configuré avec une politique dédiée à l'authentification (5 tentatives / 15 minutes, alignée sur FR-014/09-securite.md) et une politique générale plus permissive sur le reste de l'API pour absorber les pics de charge des ~30 utilisateurs concurrents (SC-014) sans les pénaliser.

**Rationale**: Évite d'introduire une dépendance tierce supplémentaire pour une fonctionnalité déjà couverte nativement par le framework cible (.NET 9), et permet une politique différenciée entre le point d'authentification (strict) et le reste de l'API (permissif) — cohérent avec la clarification de session précédente jugeant le rate limiting général comme un détail de configuration plutôt qu'une ambiguïté fonctionnelle.

**Alternatives considered**: `AspNetCoreRateLimit` (bibliothèque tierce populaire mais redondante avec la fonctionnalité native désormais disponible dans .NET 9).

---

## Récapitulatif — Aucune inconnue restante

Tous les éléments du Technical Context (plan.md) sont désormais résolus. Aucun marqueur `NEEDS CLARIFICATION` ne subsiste ; la Phase 1 (data-model.md, contracts/, quickstart.md) peut s'appuyer sur ces décisions sans hypothèse supplémentaire.
