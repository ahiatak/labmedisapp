<!--
Sync Impact Report
- Version change: (unratified template) → 1.0.0
- Modified principles: N/A (genesis ratification — template placeholders replaced)
- Added sections:
  - Core Principles I–IX (Architecture 3 couches, Héritage Service→Repository, Soft Delete,
    Logging ILoggerManager, Mapping manuel, Contrats financiers en string, Structure contrôleur
    API, Traçabilité pharmaceutique FEFO/Quarantaine, Stack technique imposée)
  - Structure du Dépôt & Stack Technique de Référence
  - Sécurité et Conformité
  - Qualité, Tests et Workflow de Développement
  - Governance
- Removed sections: none (all template placeholder sections populated)
- Templates requiring follow-up:
  - .specify/templates/plan-template.md — ⚠ pending manual check that its Constitution Check
    gate references match the 9 principles below (not modified by this command per scope guard)
  - .specify/templates/spec-template.md — ⚠ pending manual check for alignment (not modified)
  - .specify/templates/tasks-template.md — ⚠ pending manual check for alignment (not modified)
- Deferred/TODO placeholders: none — RATIFICATION_DATE set to the date this constitution was
  first authored from the wiki/LABMEDIS source documents (2026-08-28); confirm with the project
  owner if an earlier informal ratification date should be used instead.
-->

# LABMEDIS Constitution

## Core Principles

### I. Architecture Trois Couches Étanche

Le backend .NET 9 DOIT être organisé en trois projets distincts et strictement séparés :
`LABMEDIS.Core` (entités, interfaces, repositories EF Core), `LABMEDIS.Service` (logique
métier, DTOs Requests/Responses, Services, Jobs Hangfire, Hubs SignalR) et
`LABMEDIS.Presentation` (`LABMEDIS.Api` pour les contrôleurs API et/ou `LABMEDIS.BackOffice`
pour les contrôleurs MVC/Vues Razor/JS). Une couche NE DOIT PAS contourner la couche adjacente :
la Présentation appelle exclusivement la couche Service, jamais directement Core. Le frontend
React vit dans `./codebase/frontend` et le backend dans `./codebase/backend`, chacun dans son
propre dossier racine, sans mélange de responsabilités entre les deux dépôts applicatifs.
**Rationale** : cette séparation est actée comme non négociable dans la documentation
d'architecture du projet ; elle garantit qu'un développeur ne lisant que cette organisation peut
situer correctement tout nouveau code, et qu'aucune dépendance croisée incontrôlée ne s'installe
entre couches.

### II. Héritage Service → Repository (Injection de Repository Interdite)

Chaque `I[Entité]Repository` et sa classe `[Entité]Repository : BaseRepository<[Entité]>,
I[Entité]Repository` DOIVENT exister avant le service correspondant. Le Repository ne DOIT
contenir que les requêtes complexes (`.Include`, `.ThenInclude`, clauses `Where` avancées) — le
CRUD de base est fourni par `BaseRepository`. Chaque service DOIT respecter la signature
obligatoire `public class [Entité]Service : [Entité]Repository, I[Entité]Service` : l'injection
du repository par constructeur dans le service EST INTERDITE, le service hérite du repository.
**Rationale** : convention établie et systématique du projet ; elle évite la sur-abstraction DI
répétée sur un modèle de données de 59 tables et garantit une API de service homogène partout
dans la base de code.

### III. Soft Delete Exclusif & Intégrité des Données

Toute suppression DOIT être logique : `IsDeleted = true` et `DeletedAt = DateTime.UtcNow`, jamais
un DELETE physique, sauf sur les tables append-only explicitement exemptées (ex.
`user_password_history`, `notification_reads`). `BaseEntity` DOIT porter `Id (Guid)`,
`CreatedAt`, `UpdatedAt`, `DeletedAt`, `IsDeleted`. Les Query Filters EF Core DOIVENT exclure
globalement `IsDeleted = true`, et les index uniques DOIVENT être partiels
(`WHERE deleted_at IS NULL`). **Rationale** : objectif produit explicite — interdire la
suppression physique des données — et exigence de traçabilité réglementaire pour un dépositaire
pharmaceutique ; toute perte physique d'historique est un échec de conformité.

### IV. Logging ILoggerManager Exclusif

`ILogger<T>` standard de Microsoft EST INTERDIT dans le code applicatif. Seul `ILoggerManager`
(wrapper NLog du projet) DOIT être utilisé. Chaque action de contrôleur DOIT logger, avant
exécution, un message Info au format exact `"{LastName} {FirstName} ({UserName}) | Début
[NomAction] | {Method} {Path} IP: {IP} UserAgent: {UA}"`, et dans le bloc `catch`, un message
Error au format exact `"{LastName} {FirstName} ({UserName}) | Echec [NomAction] : {ExMessage} |
IP: {IP}"`. **Rationale** : uniformité d'audit et corrélation utilisateur/action/IP requise pour
couvrir 100% des actions sensibles, conformément à l'exigence d'auditabilité du produit.

### V. Mapping Manuel des DTOs

AutoMapper EST INTERDIT par défaut. Si son usage est explicitement requis pour un cas précis, le
`Profile` DOIT être déclaré dans le même fichier physique que le `Request` concerné. Chaque DTO
Request DOIT exposer une méthode de mapping manuel `public [Entité] To[Entité]()`. Chaque DTO
Response DOIT exposer un constructeur prenant l'entité en paramètre :
`public [Entité]Response([Entité] entity) { this.Id = entity.Id; ... }`.
**Rationale** : prévisibilité et débogage direct du mapping sur un modèle de données étendu (59
tables), sans réflexion implicite ni configuration à distance du code qu'elle affecte.

### VI. Contrats de Données Financières en String

Tout champ numérique, monétaire ou décimal d'un DTO Request DOIT être typé `string` (jamais
`decimal`/`double`), pour éviter les problèmes de formatage culturel côté frontend ; la
conversion se fait exclusivement via une méthode d'extension manuelle côté service (ex.
`.ToDecimal()`, `.ToDouble()`). Les montants en XOF/CFA DOIVENT être arrondis avec
`Math.Round(value, 0, MidpointRounding.AwayFromZero)` (zéro décimale) ; les calculs
intermédiaires DOIVENT conserver la précision `decimal` jusqu'à l'arrondi final. Le taux
EUR/XOF DOIT rester figé à 655.957. **Rationale** : le modèle d'affaires repose sur
l'import/revente multi-devises (EUR/USD/XOF) avec calcul de PRU, PMP/CUMP et cascade de pricing
— toute imprécision de formatage ou d'arrondi corrompt directement la comptabilité de stock.

### VII. Structure de Contrôleur API Obligatoire (Jamais de 500 Explicite)

Chaque contrôleur DOIT porter `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`,
et injecter `I[Entité]Service`, `ILoggerManager`, et `IUserService` si un contexte utilisateur
est requis. Chaque action DOIT : (1) récupérer l'utilisateur courant, (2) logger l'Info de début
d'action (Principe IV), (3) exécuter la logique métier dans un bloc `try` retournant le résultat
(ex. `Ok(response)`), (4) dans le `catch`, logger l'Error puis retourner
`BadRequest(new { message = "…" })` avec un message convivial et sécurisé pour l'utilisateur. Le
retour explicite de `StatusCode(500)` EST INTERDIT — le middleware global gère les crashs
critiques. **Rationale** : cohérence de l'expérience API sur l'ensemble des endpoints et
prévention de la fuite d'informations techniques sensibles vers le client.

### VIII. Traçabilité Pharmaceutique Non-Négociable (FEFO, Lots, Quarantaine)

Le système DOIT tracer 100% des mouvements de lots. Toute sortie de stock DOIT appliquer FEFO
(First Expired, First Out) par défaut, calculé côté backend uniquement (le frontend affiche la
proposition, il ne décide pas). Une dérogation manuelle à l'ordre FEFO N'EST AUTORISÉE que si le
lot choisi est non périmé, de statut `Libéré`, en quantité suffisante, avec un motif non vide
saisi, et l'action journalisée (log + AuditLog) — toute allocation qui ne respecte pas ces
conditions cumulées DOIT être rejetée. Aucun lot dont le `quality_status` diffère de `Libéré`
NE DOIT être proposable à la vente ; la libération d'un lot en quarantaine EST réservée
exclusivement au rôle Responsable Qualité, action journalisée. Le système DOIT empêcher toute
vente à perte ou avec un lot non conforme. **Rationale** : exigence réglementaire BPD/UEMOA pour
un dépositaire pharmaceutique et critère de succès n°1 du projet — 0% d'erreur sur la
traçabilité des lots ; le pré-mortem du produit identifie le contournement du FEFO comme risque
d'échec majeur, d'où l'obligation de le rendre impossible sans trace.

### IX. Stack Technique Imposée (Pas de Polling, Pas de Nouvelle Architecture)

Les jobs planifiés DOIVENT utiliser exclusivement **Hangfire**, placés dans
`LABMEDIS.Service/Jobs` (ex. `StockForecastJob`, `ExpiryAlertJob`). Les notifications temps réel
DOIVENT utiliser exclusivement **SignalR** — le polling EST INTERDIT. Les emails/SMS DOIVENT
transiter par `INotificationService` (FluentEmail / Twilio). Les opérations de masse (insertion,
mise à jour ou suppression de milliers de lignes) DOIVENT utiliser `BulkInsertAsync` /
`BulkUpdateAsync` du `BaseRepository` (EFCore.BulkExtensions) plutôt qu'une boucle
`SaveChanges`. Aucune architecture alternative ni « meilleure pratique » générique contredisant
les principes de cette constitution NE DOIT être introduite sans amendement formel documenté.
**Rationale** : cohérence technique du dépôt dans la durée et prévention de la dérive
architecturale au fil des contributions successives, sur un projet à 59 tables et de multiples
modules métier interdépendants.

## Structure du Dépôt & Stack Technique de Référence

- **Racines** : `./codebase/frontend` (ReactJS + TypeScript + Vite, UI TailwindCSS ou
  Material-UI) et `./codebase/backend` (.NET 9 / C#, projets `LABMEDIS.Core`,
  `LABMEDIS.Service`, `LABMEDIS.Api`).
- **Données** : PostgreSQL comme base de données de référence, via
  `Microsoft.EntityFrameworkCore.SqlServer`-équivalent PostgreSQL et `EFCore.BulkExtensions`.
  Conventions de schéma : PK en UUID, tables en `snake_case` pluriel, FK `[singulier]_id`,
  statuts en `VARCHAR` contraints par `CHECK IN (...)`, index unique partiel
  `WHERE deleted_at IS NULL`, trigger `set_updated_at()` sur chaque table métier.
- **Authentification** : ASP.NET Core Identity + JWT Bearer (access token 15–30 min, refresh
  token 7–30 jours), claims JWT porteurs des permissions.
- **Frontend** : routage protégé par `ProtectedRoute` + `PermissionGate` basés sur les claims de
  permission du JWT ; menu dynamique n'affichant que les modules autorisés par le rôle ;
  interface 100% en français, dates au format `DD/MM/YYYY`, masques de saisie CFA (séparateurs
  de milliers) ; graphiques via Recharts ; scan code-barres/QR pour emplacements et lots.
- **Outils transverses obligatoires** : NLog (via `ILoggerManager`), UAParser (User-Agent),
  DinkToPdf (bons de livraison, factures, rapports), FluentEmail (emails), Twilio (SMS),
  StackExchange.Redis (cache / backplane SignalR).

## Sécurité et Conformité

- HTTPS et CORS restreint DOIVENT être appliqués sur toute l'API.
- Politique de mot de passe : 8 caractères minimum, majuscules/minuscules/chiffres/caractères
  spéciaux requis ; 5 tentatives échouées DOIVENT déclencher un verrouillage de 15 minutes.
- Rate limiting DOIT être appliqué (référence : 5 requêtes / 15 minutes sur les endpoints
  sensibles, notamment authentification).
- Une table `audit_logs` exhaustive DOIT enregistrer les actions sensibles ; le masquage des
  données financières DOIT s'appliquer selon les permissions de l'utilisateur consultant.
- La TVA DOIT respecter la directive UEMOA 06/2002 : médicaments à 0%, autres produits selon le
  flag `is_taxable` configurable par produit.
- Toute nouvelle fonctionnalité touchant les rôles suivants DOIT respecter leurs droits exclusifs
  documentés : Admin (configuration), Direction (validation commandes critiques), Responsable
  Qualité (seul rôle habilité à libérer un lot en quarantaine).

## Qualité, Tests et Workflow de Développement

- La couverture de tests DOIT rester supérieure à 80%, avec tests unitaires obligatoires sur
  FEFO, Pricing, CUMP/PMP et arrondi CFA ; un échec sur les tests FEFO EST bloquant pour tout
  merge.
- Des tests d'intégration DOIVENT couvrir les workflows métier de bout en bout (achat →
  réception → stock → vente → facturation).
- Toute Pull Request modifiant `LABMEDIS.Core` ou `LABMEDIS.Service` DOIT être revue en
  vérifiant explicitement la conformité aux neuf Principes Fondamentaux ci-dessus avant
  approbation ; une déviation NON justifiée par un principe ou une contrainte métier documentée
  DOIT être rejetée en revue.
- Toute complexité additionnelle (nouvelle librairie, nouveau pattern d'architecture, nouvelle
  dépendance) DOIT être justifiée explicitement dans la description de la PR, faute de quoi elle
  DOIT être simplifiée ou refusée.

## Governance

Cette constitution prévaut sur toute pratique générique, préférence de style individuelle ou
« meilleure pratique » suggérée qui la contredirait. Toute déviation aux Règles d'Or (Principes
I à IX) DOIT être rejetée en revue de code, sauf amendement formel de ce document.

Un amendement à cette constitution DOIT : (1) être documenté avec sa justification métier ou
technique, (2) mettre à jour le Sync Impact Report en tête de fichier, (3) recevoir un
incrément de version conforme au versionnage sémantique — MAJOR pour un retrait ou une
redéfinition incompatible d'un principe, MINOR pour l'ajout d'un principe ou un enrichissement
matériel d'une règle existante, PATCH pour une clarification ou correction non sémantique — et
(4) être propagé, si nécessaire, vers les templates dépendants (`plan-template.md`,
`spec-template.md`, `tasks-template.md`, `checklist-template.md`) lors de la prochaine commande
Spec Kit qui les exploite.

Toute revue de code, humaine ou automatisée, DOIT vérifier la conformité à cette constitution
avant approbation. Les guides d'implémentation détaillés (structure de dossiers, exemples de
code par couche) vivent dans `wiki/LABMEDIS/08-architecture.md` et font foi pour l'application
concrète des principes ci-dessus ; en cas de conflit, cette constitution prévaut sur le contenu
du wiki.

**Version**: 1.0.0 | **Ratified**: 2026-08-28 | **Last Amended**: 2026-08-28
