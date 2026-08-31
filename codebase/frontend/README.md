# LABMEDIS — Frontend

SPA React + TypeScript + Vite pour le système de gestion du dépositaire pharmaceutique LABMEDIS. Interface 100% en français (dates `JJ/MM/AAAA`, montants CFA).

## Prérequis

- Node.js 20+ et npm
- Le backend LABMEDIS (`codebase/backend`) démarré et accessible

## Mise en route

```bash
npm install
npm run dev
```

Par défaut, l'application cible `https://localhost:5443` (voir `VITE_API_BASE_URL` dans `.env` si le backend écoute ailleurs). Le serveur de dev tourne sur `http://localhost:5173`.

## Scripts

| Commande | Description |
|---|---|
| `npm run dev` | Serveur de développement Vite (HMR) |
| `npm run build` | Vérification TypeScript (`tsc -b`) puis build de production |
| `npm run preview` | Sert le build de production en local |
| `npm run lint` | Lint via Oxlint |
| `npm run format` | Formatage via Prettier |
| `npm run test:unit` | Tests unitaires/composants (Vitest + React Testing Library) |

## Structure

```text
src/
├── components/    # UI réutilisables (NotificationCenter, etc.)
├── pages/         # Une page par domaine fonctionnel (Products, SaleOrders, Quality, ...)
├── routes/        # AppRouter, Layout, ProtectedRoute, PermissionGate, AuthContext/AuthProvider
├── services/      # apiClient (Axios + intercepteur JWT/refresh), signalrClient
└── i18n/          # Libellés français, formats de date/CFA (labels.ts, format.ts)
```

## Notes d'architecture

- **Authentification** : `AuthProvider` (routes/AuthProvider.tsx) appelle `GET /api/auth/me` au montage si un token est présent, et expose `hasPermission()` pour `PermissionGate`. Le refresh automatique du token sur 401 est géré par l'intercepteur Axios dans `services/apiClient.ts` — aucun composant n'a besoin d'y penser.
- **Temps réel** : une connexion SignalR unique et partagée (`services/signalrClient.ts`, `/hubs/notifications`) est démarrée à la demande par `NotificationCenter` et `DashboardPage`. Aucun polling — les mises à jour arrivent via les événements `notification:new` (rafraîchissement générique) et les événements typés du contrat (`stock:low`, `lot:expiringSoon`, etc.).
- **Permissions** : `PermissionGate` masque les actions (boutons de création/modification/suppression) selon les permissions du token JWT courant ; le backend est la seule source de vérité — chaque action reste rejetée côté API même si l'UI l'affichait par erreur.
- **Montants CFA** : toujours formatés via `formatCfa()`/`formatDualCurrency()` (`i18n/format.ts`), jamais un `toLocaleString()` ad hoc, pour garantir l'arrondi et le séparateur de milliers cohérents avec le backend.

## Écarts connus

- Le canal secondaire email/SMS des notifications critiques (FR-079) n'est pas visible côté frontend : il est géré entièrement côté backend et nécessite des identifiants SMTP/Twilio non configurés dans cet environnement.
