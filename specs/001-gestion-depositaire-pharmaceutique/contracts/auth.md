# Contrat API — Authentification (US2)

Toutes les routes sont préfixées `api/auth`. Contrôleur conforme au Principe VII de la constitution (log Info avant action, `try/catch`, jamais de `StatusCode(500)` explicite). Routes anonymes sauf mention contraire.

## POST /api/auth/login
**Auth**: Anonyme. **Rate limit**: 5 tentatives / 15 minutes par IP (FR-014).

Request:
```json
{ "email": "string", "password": "string" }
```
Response `200 OK`:
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "ISO8601",
  "user": { "id": "guid", "firstName": "string", "lastName": "string", "roles": ["string"], "permissions": ["Module.Action"] }
}
```
Erreurs : `400` identifiants manquants · `401` identifiants invalides · `423 Locked` compte verrouillé (FR-014, message convivial, pas de détail sur le nombre d'essais restants).

## POST /api/auth/refresh
**Auth**: Anonyme (refresh token en corps). Request: `{ "refreshToken": "string" }`. Response `200`: nouveau couple `accessToken`/`refreshToken`. Erreurs : `401` refresh token expiré/révoqué (FR-018).

## POST /api/auth/logout
**Auth**: `[Authorize]`. Révoque le refresh token courant (FR-018). Response `204`.

## POST /api/auth/forgot-password / POST /api/auth/reset-password
**Auth**: Anonyme. Flux standard de réinitialisation par email (FluentEmail). Réponses génériques (`200`) qui ne révèlent jamais si l'email existe (sécurité).

## GET /api/auth/me
**Auth**: `[Authorize]`. Retourne l'utilisateur courant, ses rôles et permissions (alimente `ProtectedRoute`/`PermissionGate` frontend, FR-019).

---

**Traçabilité** : FR-012 à FR-019, SC-011. **Journalisation** : toute tentative (succès/échec) loggée avec IP/UserAgent (FR-017) via `ILoggerManager`, jamais via `ILogger<T>` (Principe IV).
