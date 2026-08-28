# 5. Sécurité, Rôles (RBAC) & Intégrations

## 5.1 Objectifs du module

Le module **Sécurité, Rôles (RBAC) & Intégrations** doit garantir que :

1. Seuls les utilisateurs autorisés peuvent accéder au système.
2. Chaque utilisateur ne voit et ne fait que ce que son rôle autorise.
3. Les données sensibles (prix, marges, clients, fournisseurs, lots pharmaceutiques) sont protégées.
4. Toutes les actions importantes sont journalisées.
5. Les intégrations externes sont sécurisées, configurables et supervisées.
6. Le système respecte les exigences réglementaires du secteur pharmaceutique.

Dans le contexte LABMEDIS, la sécurité est critique car le système manipule :

- des données de santé indirectes via les produits pharmaceutiques,
- des données commerciales sensibles : clients, répartiteurs, prix, remises,
- des données financières : prix d’achat en devises, marges, prix de revient,
- des données logistiques internationales : fournisseurs, transport, douane,
- des données de traçabilité : lots, péremptions, mouvements de stock.

---

## 5.2 Périmètre fonctionnel

| Domaine | Description |
|---|---|
| Authentification | Connexion, déconnexion, mot de passe, JWT, refresh token. |
| Autorisation | Gestion des rôles, permissions et accès par module. |
| Gestion des utilisateurs | Création, modification, activation/désactivation des comptes. |
| Audit | Historique des actions sensibles. |
| Protection des données | Chiffrement, anonymisation, confidentialité. |
| Sécurité API | Protection contre accès non autorisé, injection, CSRF, XSS. |
| Intégrations | Emails, SMS, taux de change, PDF, code-barres, SignalR, Hangfire. |
| Conformité | Traçabilité pharmaceutique, conservation des logs, réglementation. |

---

## 5.3 Authentification

### 5.3.1 Mécanisme recommandé

Le backend .NET utilisera :

```text
ASP.NET Core Identity + JWT Bearer Authentication
```

Le frontend React utilisera :

```text
JWT Access Token + Refresh Token
```

---

### 5.3.2 Flux d’authentification

```text
Saisie email / mot de passe
        ↓
Appel API /api/auth/login
        ↓
Validation des identifiants par ASP.NET Identity
        ↓
Vérification du statut du compte
        ↓
Vérification des rôles
        ↓
Génération Access Token
        ↓
Génération Refresh Token
        ↓
Retour au frontend React
        ↓
Stockage sécurisé côté client
        ↓
Accès aux API protégées
```

---

### 5.3.3 Access Token et Refresh Token

| Token | Durée recommandée | Usage |
|---|---:|---|
| Access Token | 15 à 30 minutes | Accès aux API. |
| Refresh Token | 7 à 30 jours | Renouveler l’Access Token. |
| Token de réinitialisation mot de passe | 30 minutes à 2 heures | Lien envoyé par email. |

Règles :

1. L’Access Token doit être de courte durée.
2. Le Refresh Token doit être stocké côté backend avec expiration.
3. Le Refresh Token peut être révoqué en cas de déconnexion.
4. Le Refresh Token doit être invalidé si l’utilisateur est désactivé.
5. Les tokens doivent être envoyés uniquement via HTTPS.

---

### 5.3.4 Mot de passe

Règles recommandées :

| Règle | Valeur |
|---|---|
| Longueur minimale | 8 caractères. |
| Caractères obligatoires | Majuscule, minuscule, chiffre, caractère spécial. |
| Verrouillage après tentatives échouées | 5 tentatives. |
| Durée de verrouillage | 15 minutes. |
| Historique des mots de passe | Empêcher réutilisation des 5 derniers. |
| Expiration | Optionnelle, selon politique interne. |
| Réinitialisation | Par email sécurisé. |

---

### 5.3.5 Connexion sécurisée

Le système doit prévoir :

1. Hash des mots de passe avec ASP.NET Identity.
2. Blocage progressif après échecs répétés.
3. Notification en cas de tentative suspecte.
4. Journalisation des connexions réussies et échouées.
5. Déconnexion globale possible par administrateur.
6. Expiration de session après inactivité.

---

## 5.4 Gestion des utilisateurs

### 5.4.1 Entité utilisateur

Le système utilisera ASP.NET Identity avec une entité étendue.

Exemple :

```csharp
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public DateTime? LastPasswordChangeDate { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

---

### 5.4.2 Informations utilisateur

| Champ | Description |
|---|---|
| Nom | Nom de famille. |
| Prénom | Prénom. |
| Email | Identifiant de connexion. |
| Téléphone | Optionnel. |
| Rôle | Rôle principal ou plusieurs rôles. |
| Statut | Actif ou inactif. |
| Dernier login | Date de dernière connexion. |
| Créé par | Utilisateur administrateur ayant créé le compte. |
| Date création | Horodatage. |
| Soft delete | Compte désactivé mais non supprimé physiquement. |

---

### 5.4.3 User Stories utilisateurs

#### US-SEC-01 : Créer un utilisateur

**En tant que** administrateur,  
**je veux** créer un utilisateur,  
**afin de** lui donner accès au système.

**Critères d’acceptation :**

1. L’administrateur saisit nom, prénom, email, téléphone.
2. L’administrateur affecte un ou plusieurs rôles.
3. Le système vérifie que l’email est unique.
4. Le système envoie un email d’activation ou de définition du mot de passe.
5. L’utilisateur est créé avec statut actif ou inactif.
6. L’action est journalisée.

---

#### US-SEC-02 : Désactiver un utilisateur

**En tant que** administrateur,  
**je veux** désactiver un utilisateur,  
**afin de** bloquer son accès sans supprimer ses données.

**Critères d’acceptation :**

1. L’utilisateur désactivé ne peut plus se connecter.
2. Ses tokens actifs sont révoqués.
3. Ses données historiques sont conservées.
4. L’action est journalisée.
5. Le compte peut être réactivé.

---

#### US-SEC-03 : Réinitialiser un mot de passe

**En tant qu’utilisateur**,  
**je veux** réinitialiser mon mot de passe,  
**afin de** récupérer l’accès à mon compte.

**Critères d’acceptation :**

1. L’utilisateur saisit son email.
2. Le système envoie un lien sécurisé.
3. Le lien expire après une durée limitée.
4. Le nouveau mot de passe respecte la politique.
5. L’ancien mot de passe ne peut pas être réutilisé immédiatement.
6. L’action est journalisée.

---

## 5.5 RBAC : Gestion des rôles et permissions

### 5.5.1 Principe

Le système doit utiliser un modèle RBAC :

```text
User → Role → Permission
```

Un utilisateur peut avoir un ou plusieurs rôles.  
Un rôle contient plusieurs permissions.  
Une permission correspond à une action sur un module.

---

### 5.5.2 Rôles recommandés

| Rôle | Description |
|---|---|
| Admin | Accès total technique et fonctionnel. |
| Direction | Pilotage, validation, reporting, marges. |
| Responsable achats | Commandes fournisseurs, prévisions, suggestions MRP. |
| Responsable logistique | Transport, expéditions, réception, conteneurs. |
| Magasinier | Réception, mise en stock, inventaire, préparation. |
| Responsable qualité | Quarantaine, libération lots, non-conformités. |
| Commercial | Commandes clients, devis, disponibilité. |
| Comptable | Factures, avoirs, TVA, exports. |
| Préparateur | Préparation des commandes clients. |
| Lecture seule | Consultation limitée sans action. |

---

### 5.5.3 Permissions par module

Chaque permission peut être représentée sous forme de claim.

Format recommandé :

```text
Module.Action
```

Exemples :

```text
Products.Read
Products.Create
Products.Update
Products.Delete

Stock.Read
Stock.Receive
Stock.Adjust
Stock.Transfer
Stock.Destroy

Sales.Read
Sales.Create
Sales.Validate
Sales.Deliver
Sales.Invoice

Purchases.Read
Purchases.Create
Purchases.Validate
Purchases.Receive

Pricing.Read
Pricing.Simulate
Pricing.Update
Pricing.Approve

Forecast.Read
Forecast.Run
Forecast.Simulate
Forecast.ValidateSuggestion
Forecast.ConvertToPurchaseOrder

Users.Read
Users.Create
Users.Update
Users.Disable
Users.AssignRole
```

---

### 5.5.4 Matrice de permissions recommandée

| Module | Admin | Direction | Achats | Logistique | Magasinier | Qualité | Commercial | Comptable |
|---|---|---|---|---|---|---|---|---|
| Produits - Lecture | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Produits - Création | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Produits - Modification | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Clients - Lecture | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Clients - Création | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Fournisseurs - Lecture | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Fournisseurs - Création | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Achats - Lecture | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Achats - Création | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Achats - Validation | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Transport - Lecture | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Transport - Gestion | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Réception - Saisie | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Réception - Validation | ✅ | ✅ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| Lots - Lecture | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Lots - Quarantaine | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Lots - Libération | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Stock - Lecture | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Stock - Ajustement | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Stock - Destruction | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |
| Ventes - Lecture | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Ventes - Création | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Ventes - Validation | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Ventes - Livraison | ✅ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| Facturation | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Avoirs | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Pricing - Lecture | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Pricing - Simulation | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Pricing - Modification | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Pricing - Approbation | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| MRP - Lecture | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| MRP - Lancer calcul | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| MRP - Convertir suggestion | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Inventaire - Lecture | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | ❌ | ✅ |
| Inventaire - Saisie | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Inventaire - Validation | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Utilisateurs - Gestion | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Logs - Lecture | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

---

### 5.5.5 Entités RBAC recommandées

#### `Role`

```csharp
public class ApplicationRole : IdentityRole
{
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

---

#### `Permission`

```csharp
public class Permission : BaseEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Module { get; set; }
    public string Description { get; set; }
    public bool IsSystem { get; set; }
}
```

---

#### `RolePermission`

```csharp
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }

    public ApplicationRole Role { get; set; }
    public Permission Permission { get; set; }
}
```

---

#### `UserPermissionException`

```csharp
public class UserPermissionException : BaseEntity
{
    public string UserId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; }
    public string Reason { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public ApplicationUser User { get; set; }
    public Permission Permission { get; set; }
}
```

Cette table permet des exceptions individuelles si nécessaire, sans modifier tout un rôle.

---

### 5.5.6 User Stories RBAC

#### US-RBAC-01 : Créer un rôle

**En tant que** administrateur,  
**je veux** créer un rôle,  
**afin de** regrouper des permissions.

**Critères d’acceptation :**

1. L’administrateur saisit le nom et la description.
2. Le rôle peut être actif ou inactif.
3. Le rôle peut être marqué système pour éviter suppression.
4. L’action est journalisée.

---

#### US-RBAC-02 : Affecter des permissions à un rôle

**En tant que** administrateur,  
**je veux** affecter des permissions à un rôle,  
**afin de** contrôler les accès par fonction.

**Critères d’acceptation :**

1. Les permissions sont regroupées par module.
2. L’administrateur peut cocher/décocher des permissions.
3. Les modifications sont appliquées immédiatement.
4. Les permissions système critiques peuvent être protégées.
5. L’action est journalisée.

---

#### US-RBAC-03 : Affecter un rôle à un utilisateur

**En tant que** administrateur,  
**je veux** affecter un rôle à un utilisateur,  
**afin de** définir ses droits.

**Critères d’acceptation :**

1. Un utilisateur peut avoir plusieurs rôles.
2. L’administrateur peut retirer un rôle.
3. Les rôles inactifs ne peuvent pas être affectés.
4. Les changements sont journalisés.
5. Les tokens existants sont invalidés si nécessaire.

---

#### US-RBAC-04 : Vérifier une permission côté backend

**En tant que** système,  
**je veux** vérifier les permissions avant chaque action,  
**afin d’empêcher les accès non autorisés.

**Critères d’acceptation :**

1. Chaque endpoint API vérifie l’authentification.
2. Chaque endpoint API vérifie la permission requise.
3. Une action sans permission retourne `403 Forbidden`.
4. Le refus est journalisé.
5. Le message retourné est clair mais non technique.

---

## 5.6 Autorisation côté API .NET

### 5.6.1 Attributs recommandés

Les contrôleurs API devront utiliser :

```csharp
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
}
```

Pour une permission spécifique :

```csharp
[Authorize(Policy = "Products.Create")]
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
}
```

---

### 5.6.2 Politique de permission

Exemple d’enregistrement dans `Program.cs` :

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Products.Read", policy =>
        policy.RequireClaim("permission", "Products.Read"));

    options.AddPolicy("Products.Create", policy =>
        policy.RequireClaim("permission", "Products.Create"));

    options.AddPolicy("Products.Update", policy =>
        policy.RequireClaim("permission", "Products.Update"));

    options.AddPolicy("Pricing.Approve", policy =>
        policy.RequireClaim("permission", "Pricing.Approve"));
});
```

---

### 5.6.3 Génération des claims

Lors du login, les permissions doivent être ajoutées aux claims JWT.

Exemple :

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.GivenName, user.FirstName),
    new Claim(ClaimTypes.Surname, user.LastName),
    new Claim("permission", "Products.Read"),
    new Claim("permission", "Stock.Read"),
    new Claim("permission", "Sales.Create")
};
```

---

## 5.7 Autorisation côté frontend React

### 5.7.1 Gestion des routes protégées

Le frontend React doit implémenter :

1. Un `ProtectedRoute`.
2. Un `PermissionGate`.
3. Un `RoleGuard`.
4. Un menu dynamique selon permissions.
5. Des boutons désactivés ou masqués selon permissions.

---

### 5.7.2 Exemple de logique React

```tsx
const hasPermission = (user: User | null, permission: string) => {
  return user?.permissions?.includes(permission) ?? false;
};
```

```tsx
{hasPermission(currentUser, "Sales.Create") && (
  <Button onClick={openCreateSaleModal}>
    Nouvelle commande
  </Button>
)}
```

---

### 5.7.3 Règles frontend

| Règle | Description |
|---|---|
| Menu dynamique | Afficher uniquement les modules autorisés. |
| Boutons conditionnels | Masquer ou désactiver les actions non autorisées. |
| Redirection | Rediriger vers login si token expiré. |
| Page 403 | Afficher une page accès refusé. |
| Protection formulaires | Empêcher accès direct par URL. |
| Session timeout | Déconnecter après inactivité. |
| Toasts sécurité | Informer l’utilisateur des refus. |

---

## 5.8 Audit et traçabilité

### 5.8.1 Objectif

Toutes les actions sensibles doivent être auditées.

Exemples :

- connexion,
- échec de connexion,
- création produit,
- modification prix,
- validation commande fournisseur,
- réception lot,
- libération lot,
- ajustement stock,
- destruction produit,
- création facture,
- création avoir,
- modification rôle,
- modification utilisateur.

---

### 5.8.2 Entité AuditLog

```csharp
public class AuditLog : BaseEntity
{
    public string UserId { get; set; }
    public string UserFullName { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string HttpMethod { get; set; }
    public string Path { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public string RequestData { get; set; }
    public string ResponseMessage { get; set; }
    public bool IsSuccess { get; set; }
    public DateTime ExecutedAt { get; set; }

    public ApplicationUser User { get; set; }
}
```

---

### 5.8.3 Règles d’audit

| Règle | Description |
|---|---|
| Horodatage | Chaque action doit avoir date/heure UTC. |
| Utilisateur | Chaque action doit identifier l’utilisateur. |
| IP | Adresse IP conservée. |
| UserAgent | UserAgent conservé. |
| Module | Module concerné identifié. |
| Action | Nom d’action clair. |
| Données sensibles | Ne pas journaliser mots de passe ou tokens. |
| Conservation | Logs conservés selon durée légale ou politique interne. |
| Consultation | Seuls Admin/Direction/Comptable peuvent consulter les logs. |

---

### 5.8.4 Lien avec ILoggerManager

Le backend utilisera `ILoggerManager` selon la règle imposée.

Exemple :

```csharp
_logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début [ValiderCommandeFournisseur] | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");
```

```csharp
_logger.LogError(ex, $"{user?.LastName} ... | Echec [ValiderCommandeFournisseur] : {ex.Message} | IP: {Request.GetIp()}");
```

En complément, une table `AuditLog` pourra stocker les actions métier structurées.

---

## 5.9 Sécurité des données sensibles

### 5.9.1 Catégories de données sensibles

| Donnée | Sensibilité |
|---|---|
| Prix d’achat fournisseur | Haute |
| Prix de revient | Haute |
| Marge | Haute |
| Prix client spécifique | Haute |
| Liste clients | Haute |
| Liste fournisseurs | Haute |
| Lots pharmaceutiques | Haute |
| Dates de péremption | Haute |
| Commandes | Moyenne à haute |
| Factures | Haute |
| Logs | Moyenne à haute |
| Mots de passe | Critique |
| Tokens | Critique |

---

### 5.9.2 Règles de protection

| Règle | Description |
|---|---|
| HTTPS obligatoire | Toutes les communications doivent être chiffrées. |
| Hash mot de passe | Jamais stocké en clair. |
| Tokens courts | Access Token limité dans le temps. |
| Secrets | Stockés dans variables d’environnement ou secret manager. |
| Configuration | Pas de secrets dans le code source. |
| Données financières | Accès restreint. |
| Export | Contrôle des droits d’export. |
| Masquage | Masquer les prix si l’utilisateur n’a pas le droit. |
| Logs | Ne pas logger mots de passe, tokens, données critiques. |

---

### 5.9.3 Masquage des données

Si un utilisateur n’a pas la permission `Pricing.Read`, le frontend ne doit pas afficher :

- prix d’achat,
- prix de revient,
- marge,
- coût logistique,
- simulation pricing.

Le backend doit aussi refuser l’accès à l’endpoint concerné.

Exemple :

```csharp
[Authorize(Policy = "Pricing.Read")]
[HttpGet("pricing/{productId:guid}")]
public async Task<IActionResult> GetPricing(Guid productId)
{
}
```

---

## 5.10 Sécurité API

### 5.10.1 Règles générales

| Règle | Description |
|---|---|
| Authentification | Toutes les routes métier sont protégées. |
| Autorisation | Vérifier rôle/permission. |
| Validation | Valider les DTO côté serveur. |
| Exceptions | Ne pas exposer les erreurs techniques. |
| Soft delete | Aucune suppression physique. |
| Rate limiting | Limiter les tentatives de login. |
| CORS | Autoriser uniquement le frontend. |
| Headers | Sécurité HTTP recommandée. |
| IP logging | Journaliser les IP. |
| UserAgent logging | Journaliser le UserAgent. |

---

### 5.10.2 Validation des entrées

Toutes les données reçues doivent être validées.

Exemple :

```csharp
public class CreateProductRequest
{
    [Required]
    [StringLength(250)]
    public string Designation { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [SwaggerSchema(Description = "Prix d'achat en devise, format string")]
    public string PurchasePrice { get; set; }
}
```

Règles :

1. Les champs obligatoires doivent être contrôlés.
2. Les chaînes doivent avoir une longueur maximale.
3. Les montants doivent être convertis manuellement.
4. Les GUID invalides doivent être rejetés.
5. Les dates doivent être au format attendu.

---

### 5.10.3 Gestion des erreurs

Le backend ne doit jamais retourner :

```csharp
StatusCode(500)
```

dans les actions contrôleur.

Il doit retourner :

```csharp
return BadRequest(new { message = "Un message convivial et sécurisé pour l'utilisateur" });
```

Le middleware global peut gérer les erreurs critiques.

---

### 5.10.4 Protection contre attaques courantes

| Attaque | Protection |
|---|---|
| SQL Injection | Entity Framework Core avec requêtes paramétrées. |
| XSS | Validation et encodage côté React. |
| CSRF | JWT Bearer + SameSite cookies si cookies utilisés. |
| Brute force | Verrouillage compte + rate limiting login. |
| Accès non autorisé | RBAC + Authorization policies. |
| Exposition erreurs | Messages génériques côté API. |
| Données sensibles | Masquage et permissions. |
| Replay token | Token courte durée + HTTPS. |

---

## 5.11 Sécurité frontend React

### 5.11.1 Stockage des tokens

Recommandation :

| Élément | Stockage |
|---|---|
| Access Token | Memory state ou sessionStorage. |
| Refresh Token | HttpOnly cookie si possible, sinon stockage sécurisé. |
| Permissions | Store applicatif après login. |
| Données sensibles | Ne pas être stockées dans localStorage. |

---

### 5.11.2 Intercepteur HTTP

Le frontend doit utiliser un intercepteur Axios ou Fetch :

```ts
api.interceptors.request.use(config => {
  const token = getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

Gestion du token expiré :

```ts
api.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401) {
      await tryRefreshToken();
    }
    return Promise.reject(error);
  }
);
```

---

### 5.11.3 Règles UI sécurité

| Règle | Description |
|---|---|
| Page login | Formulaire sécurisé, messages génériques. |
| Page 403 | Accès refusé. |
| Page 404 | Ressource non trouvée. |
| Session expirée | Redirection login. |
| Notifications sécurité | Toasts pour refus ou erreurs. |
| Masquage prix | Selon permission. |
| Désactivation boutons | Actions non autorisées. |
| Protection formulaires | Validation avant envoi. |

---

## 5.12 Conformité pharmaceutique

### 5.12.1 Traçabilité des lots

Le système doit garantir :

1. Chaque produit reçu possède un lot.
2. Chaque lot possède une date de péremption.
3. Chaque mouvement de stock est historisé.
4. Chaque vente peut être reliée à un lot.
5. Chaque retour peut être relié à une commande.
6. Chaque destruction est tracée.
7. Chaque libération qualité est tracée.

---

### 5.12.2 Conservation des données

| Donnée | Durée recommandée |
|---|---|
| Mouvements de stock | Minimum 5 ans. |
| Factures | Selon obligation fiscale locale. |
| Avoirs | Selon obligation fiscale locale. |
| Logs sécurité | Minimum 1 an. |
| Audit métier | Minimum 3 à 5 ans. |
| Lots | Au moins durée de vie produit + X années. |

---

### 5.12.3 Règles qualité

| Règle | Description |
|---|---|
| Quarantaine | Lot non vendable tant que non libéré. |
| Péremption | Lot périmé non vendable. |
| Destruction | Procédure tracée avec motif. |
| Non-conformité | Statut et historique obligatoires. |
| Rappel produit | Pouvoir identifier les clients livrés par lot. |
| Historique | Aucune suppression physique. |

---

### 5.12.4 Rappel produit

En cas de rappel d’un lot, le système doit permettre :

1. Recherche du lot.
2. Liste des réceptions associées.
3. Liste des mouvements de stock.
4. Liste des commandes clients livrées.
5. Liste des clients concernés.
6. Quantité restante en stock.
7. Quantité déjà vendue.
8. Blocage du lot.

Endpoint recommandé :

```text
GET /api/traceability/lots/{lotId}/impact
```

---

## 5.13 Intégrations techniques

## 5.13.1 Vue globale des intégrations

| Intégration | Usage |
|---|---|
| ASP.NET Identity | Authentification. |
| JWT | Sécurité API. |
| SignalR | Notifications temps réel. |
| Hangfire | Jobs planifiés. |
| FluentEmail | Emails transactionnels. |
| Twilio | SMS optionnels. |
| DinkToPdf | Génération PDF. |
| API taux de change | Devises EUR/USD/XOF. |
| Code-barres / QR | Scan produits, lots, emplacements. |
| Redis | Cache et backplane SignalR si nécessaire. |
| SMTP | Serveur email LABMEDIS. |
| Export Excel | Imports/exports master data. |
| Export comptable | Optionnel vers logiciel comptable. |

---

## 5.14 Intégration email

### 5.14.1 Cas d’usage

Le système doit envoyer des emails pour :

| Cas | Description |
|---|---|
| Activation compte | Création utilisateur. |
| Réinitialisation mot de passe | Lien sécurisé. |
| Alerte rupture | Produit critique. |
| Alerte péremption | Lots proches péremption. |
| Validation commande | Commande fournisseur validée. |
| Réception | Réception validée. |
| Facture | Envoi facture PDF. |
| Avoir | Envoi avoir PDF. |
| Erreur critique | Notification technique si nécessaire. |

---

### 5.14.2 Service email

Le backend utilisera :

```text
FluentEmail
```

Interface recommandée :

```csharp
public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendAlertEmailAsync(string module, string message);
    Task SendInvoiceEmailAsync(string to, Guid invoiceId, byte[] pdfFile);
}
```

---

### 5.14.3 Configuration email

Les paramètres doivent être stockés en configuration :

```json
"EmailSettings": {
  "SmtpHost": "smtp.labmedis.com",
  "SmtpPort": 587,
  "UserName": "notification@labmedis.com",
  "Password": "***",
  "EnableSsl": true,
  "FromName": "LABMEDIS ERP",
  "FromAddress": "notification@labmedis.com"
}
```

Le mot de passe ne doit pas être stocké en clair dans le code.

---

## 5.15 Intégration SMS

### 5.15.1 Cas d’usage

Optionnel mais recommandé :

| Cas | Description |
|---|---|
| Alerte critique | Rupture imminente. |
| Réception conteneur | Information logistique. |
| Livraison | Notification client. |
| Code de validation | Sécurité supplémentaire si activée. |

---

### 5.15.2 Service SMS

Le backend utilisera :

```text
Twilio ou équivalent local
```

Interface recommandée :

```csharp
public interface ISmsService
{
    Task SendSmsAsync(string phoneNumber, string message);
}
```

---

## 5.16 Intégration taux de change

### 5.16.1 Contexte

LABMEDIS travaille avec :

- EUR,
- USD,
- XOF.

Le système doit pouvoir gérer :

1. Un taux de change manuel.
2. Un taux de change automatique via API.
3. Un taux historique figé par commande.

---

### 5.16.2 Modes recommandés

| Mode | Description |
|---|---|
| Manuel | Un utilisateur saisit le taux. |
| Automatique | Le système récupère le taux via API. |
| Mixte | Taux automatique proposé, validation manuelle. |

---

### 5.16.3 API possibles

| Source | Usage |
|---|---|
| BCEAO | Référence XOF. |
| Banque centrale | Référence officielle. |
| API bancaire | Taux entreprise. |
| Open Exchange / Fixer | Alternative. |

---

### 5.16.4 Règle métier

Pour chaque commande fournisseur :

```text
Le taux de change appliqué doit être figé à la validation.
```

Cela garantit la traçabilité financière du lot et du prix de revient.

---

## 5.17 Intégration PDF

### 5.17.1 Documents PDF à générer

| Document | Usage |
|---|---|
| Bon de commande fournisseur | Envoi fournisseur. |
| Bon de réception | Validation réception. |
| Bon de livraison | Livraison client. |
| Facture | Facturation client. |
| Avoir | Régularisation. |
| Étiquette lot | Impression entrepôt. |
| Fiche inventaire | Support inventaire. |
| Rapport MRP | Analyse prévision. |

---

### 5.17.2 Technologie

Le backend utilisera :

```text
DinkToPdf ou équivalent
```

Règles :

1. Les templates doivent être versionnés.
2. Les documents doivent être générés côté backend.
3. Les PDF peuvent être téléchargés ou envoyés par email.
4. Les numéros de documents doivent être uniques.
5. Les documents doivent être historisés.

---

## 5.18 Intégration code-barres / QR code

### 5.18.1 Cas d’usage

| Usage | Description |
|---|---|
| Produits | Identification produit. |
| Lots | Identification lot + péremption. |
| Emplacements | Identification adresse entrepôt. |
| Cartons | Identification colis. |
| Commandes | Suivi préparation. |
| Inventaire | Comptage rapide. |

---

### 5.18.2 Types de codes

| Type | Usage recommandé |
|---|---|
| Code-barres EAN/Code 128 | Produits simples. |
| QR Code | Lots, emplacements, informations riches. |
| Datamatrix | Usage pharmaceutique avancé. |

---

### 5.18.3 Frontend React

Le frontend doit gérer :

1. Scan via lecteur USB agissant comme clavier.
2. Scan via caméra tablette/mobile si PWA.
3. Recherche automatique après scan.
4. Validation du code scanné.
5. Affichage d’erreur si code inconnu.

---

### 5.18.4 Backend

Endpoints recommandés :

```text
GET /api/barcode/products/{code}
GET /api/barcode/lots/{code}
GET /api/barcode/locations/{code}
GET /api/barcode/cartons/{code}
```

---

## 5.19 Intégration SignalR

### 5.19.1 Objectif

SignalR doit permettre les notifications temps réel sans polling.

---

### 5.19.2 Événements recommandés

| Événement | Description |
|---|---|
| NotificationCreated | Nouvelle notification. |
| LowStockAlert | Stock faible. |
| CriticalStockAlert | Rupture critique. |
| ExpiryLotAlert | Lot proche péremption. |
| ReceptionCompleted | Réception validée. |
| OrderReady | Commande prête. |
| InvoiceGenerated | Facture générée. |
| MrpCalculationCompleted | Calcul MRP terminé. |
| QualityLotReleased | Lot libéré. |
| QualityLotBlocked | Lot bloqué. |

---

### 5.19.3 Règles

1. Pas de polling frontend.
2. Les notifications doivent être persistées en base.
3. SignalR sert à pousser la notification en temps réel.
4. Si l’utilisateur est hors ligne, il retrouve ses notifications à la connexion.
5. Les notifications doivent être filtrées par rôle.

---

## 5.20 Intégration Hangfire

### 5.20.1 Objectif

Hangfire doit gérer :

| Job | Description |
|---|---|
| MRP quotidien | Calcul prévisions. |
| Alertes péremption | Scan lots proches péremption. |
| Alertes stock faible | Scan seuils produits. |
| Nettoyage notifications | Purge notifications anciennes. |
| Relances suggestions | Suggestions non traitées. |
| Suivi transport | Retards expéditions. |
| Export rapports | Rapports périodiques. |
| Sauvegarde logs | Archivage audit logs. |

---

### 5.20.2 Règles

1. Les jobs sont dans `[LABMEDIS].Service/Jobs`.
2. Les jobs doivent être idempotents.
3. Les erreurs job doivent être loggées.
4. Les jobs critiques doivent envoyer une notification en cas d’échec.
5. Les jobs longs doivent avoir un statut consultable.

---

## 5.21 Intégration comptable

### 5.21.1 Objectif

Le système pourra exporter les données vers un logiciel comptable si LABMEDIS en utilise un.

---

### 5.21.2 Données exportables

| Donnée | Format possible |
|---|---|
| Factures clients | PDF, Excel, CSV. |
| Avoirs | PDF, Excel, CSV. |
| Achats fournisseurs | Excel, CSV. |
| TVA collectée | Excel. |
| TVA déductible | Excel. |
| Valorisation stock | Excel. |
| Mouvements de stock | CSV. |
| Écritures comptables | CSV ou format logiciel cible. |

---

### 5.21.3 Règles

1. L’export doit être réservé au rôle comptable/admin.
2. L’export doit être journalisé.
3. Les montants doivent être formatés correctement.
4. Les dates doivent être normalisées.
5. L’export doit pouvoir être filtré par période.

---

## 5.22 Intégration douane / transitaire

### 5.22.1 Objectif

Suivre les opérations de dédouanement des importations.

---

### 5.22.2 Données possibles

| Donnée | Description |
|---|---|
| Numéro dossier douane | Référence transitaire. |
| Numéro conteneur | Référence maritime. |
| Numéro LTA | Référence aérienne. |
| Date arrivée port/aéroport | Suivi logistique. |
| Date dédouanement | Fin opération douanière. |
| Frais douane | Coût import. |
| Documents | Facture fournisseur, packing list, BL. |

---

### 5.22.3 Intégration possible

| Niveau | Description |
|---|---|
| Niveau 1 | Saisie manuelle dans le système. |
| Niveau 2 | Upload de documents PDF. |
| Niveau 3 | API transitaire si disponible. |
| Niveau 4 | Notification automatique par email parseé, avancé. |

Pour la première version, la saisie manuelle + upload documents est recommandée.

---

## 5.23 Intégration API taux de change : détail technique

### 5.23.1 Entité ExchangeRate

```csharp
public class ExchangeRate : BaseEntity
{
    public string FromCurrency { get; set; }
    public string ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }
    public ExchangeRateSource Source { get; set; }
    public string CreatedByUserId { get; set; }
}
```

---

### 5.23.2 Enum source

```csharp
public enum ExchangeRateSource
{
    Manual = 0,
    Api = 1,
    Import = 2
}
```

---

### 5.23.3 Endpoints

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/exchange-rates` | Liste des taux. |
| GET | `/api/exchange-rates/current` | Taux courant. |
| POST | `/api/exchange-rates` | Créer taux manuel. |
| POST | `/api/exchange-rates/sync` | Forcer synchronisation API. |

---

## 5.24 Gestion des secrets et configuration

### 5.24.1 Règles

| Secret | Stockage recommandé |
|---|---|
| Connection string SQL Server | Secret manager / variables environnement. |
| Clé JWT | Secret manager. |
| SMTP password | Secret manager. |
| Twilio key | Secret manager. |
| API taux de change key | Secret manager. |
| Redis password | Secret manager. |

---

### 5.24.2 Interdictions

1. Aucun secret dans le code source.
2. Aucun secret dans les fichiers commités.
3. Aucun secret dans les logs.
4. Aucun secret dans les réponses API.
5. Aucun secret dans les exports.

---

## 5.25 Sécurité des fichiers et uploads

### 5.25.1 Types de fichiers possibles

| Fichier | Usage |
|---|---|
| PDF facture fournisseur | Dossier achat. |
| PDF packing list | Dossier import. |
| PDF bon livraison | Dossier logistique. |
| PDF certificat qualité | Dossier qualité. |
| Excel import produits | Master data. |
| Excel import clients | Master data. |
| Excel import fournisseurs | Master data. |
| Images étiquettes | Optionnel. |

---

### 5.25.2 Règles upload

| Règle | Description |
|---|---|
| Taille maximale | Limiter à ex. 10 Mo ou 25 Mo. |
| Types autorisés | PDF, XLSX, CSV, PNG, JPG. |
| Scan antivirus | Optionnel mais recommandé. |
| Nommage | Nom fichier sécurisé, sans chemin. |
| Accès | Vérifier permission avant téléchargement. |
| Stockage | Dossier sécurisé ou stockage objet. |

---

## 5.26 Sécurité des imports Excel

### 5.26.1 Contexte

Les fichiers fournis montrent que LABMEDIS utilise Excel pour :

- produits,
- clients,
- fournisseurs,
- structure de prix.

Le système devra permettre des imports Excel.

---

### 5.26.2 Règles

1. Valider les colonnes attendues.
2. Valider les lignes une par une.
3. Détecter les doublons.
4. Retourner un rapport d’erreurs.
5. Ne pas insérer si erreurs bloquantes.
6. Journaliser l’import.
7. Permettre prévisualisation avant import.
8. Utiliser `BulkInsertAsync` si volumineux.

---

### 5.26.3 Exemple de rapport d’import

| Ligne | Champ | Erreur |
|---:|---|---|
| 12 | Désignation | Valeur obligatoire manquante. |
| 25 | Fournisseur | Fournisseur inconnu. |
| 40 | Conditionnement | Format invalide. |
| 58 | TVA | Taux invalide. |

---

## 5.27 Notifications et permissions

### 5.27.1 Règle

Un utilisateur ne doit recevoir que les notifications liées à son rôle.

Exemple :

| Rôle | Notifications |
|---|---|
| Achats | Suggestions MRP, retard fournisseur. |
| Magasinier | Réception, inventaire, péremption. |
| Qualité | Quarantaine, libération, non-conformité. |
| Commercial | Commande, livraison, stock faible. |
| Comptable | Facture, avoir, export. |
| Direction | Alertes critiques, reporting. |
| Admin | Erreurs techniques, sécurité. |

---

## 5.28 Endpoints sécurité recommandés

### Authentification

| Méthode | Route | Description |
|---|---|---|
| POST | `/api/auth/login` | Connexion. |
| POST | `/api/auth/logout` | Déconnexion. |
| POST | `/api/auth/refresh-token` | Renouveler access token. |
| POST | `/api/auth/forgot-password` | Demande réinitialisation. |
| POST | `/api/auth/reset-password` | Réinitialisation mot de passe. |
| GET | `/api/auth/me` | Profil utilisateur courant. |

---

### Utilisateurs

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/users` | Liste utilisateurs. |
| GET | `/api/users/{id}` | Détail utilisateur. |
| POST | `/api/users` | Créer utilisateur. |
| PUT | `/api/users/{id}` | Modifier utilisateur. |
| POST | `/api/users/{id}/disable` | Désactiver utilisateur. |
| POST | `/api/users/{id}/enable` | Réactiver utilisateur. |
| POST | `/api/users/{id}/reset-password` | Réinitialiser mot de passe. |

---

### Rôles et permissions

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/roles` | Liste rôles. |
| GET | `/api/roles/{id}` | Détail rôle. |
| POST | `/api/roles` | Créer rôle. |
| PUT | `/api/roles/{id}` | Modifier rôle. |
| GET | `/api/permissions` | Liste permissions. |
| GET | `/api/roles/{id}/permissions` | Permissions d’un rôle. |
| PUT | `/api/roles/{id}/permissions` | Modifier permissions rôle. |

---

### Audit

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/audit-logs` | Liste logs audit. |
| GET | `/api/audit-logs/{id}` | Détail log. |
| GET | `/api/audit-logs/export` | Export audit. |

---

### Traçabilité lot

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/traceability/lots/{lotId}` | Historique lot. |
| GET | `/api/traceability/lots/{lotId}/impact` | Impact clients/ventes. |

---

## 5.29 Exemple de contrôleur sécurisé

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILoggerManager _logger;

    public UsersController(IUserService userService, ILoggerManager logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Authorize(Policy = "Users.Create")]
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var currentUser = await _userService.GetCurrentUserAsync(User);

        _logger.LogInfo($"{currentUser?.LastName} {currentUser?.FirstName} ({currentUser?.UserName}) | Début CreateUser | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try
        {
            var result = await _userService.CreateUserAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{currentUser?.LastName} ... | Echec CreateUser : {ex.Message} | IP: {Request.GetIp()}");

            return BadRequest(new
            {
                message = "Impossible de créer l'utilisateur. Vérifiez les informations saisies."
            });
        }
    }
}
```

---

## 5.30 User Stories sécurité et intégrations

### US-INT-01 : Configurer les paramètres email

**En tant que** administrateur,  
**je veux** configurer les paramètres email,  
**afin d’envoyer des notifications système.

**Critères d’acceptation :**

1. Les paramètres SMTP sont configurables.
2. Un email de test peut être envoyé.
3. Les erreurs SMTP sont journalisées.
4. Le mot de passe SMTP n’est pas affiché.
5. L’envoi d’email peut être activé/désactivé.

---

### US-INT-02 : Configurer les notifications SMS

**En tant que** administrateur,  
**je veux** configurer un fournisseur SMS,  
**afin d’envoyer des alertes critiques.

**Critères d’acceptation :**

1. Le fournisseur peut être Twilio ou équivalent.
2. Les clés API sont stockées secrètement.
3. Un SMS de test peut être envoyé.
4. Les erreurs SMS sont journalisées.
5. L’envoi SMS peut être activé/désactivé.

---

### US-INT-03 : Configurer le taux de change

**En tant que** responsable financier,  
**je veux** saisir ou synchroniser le taux de change,  
**afin de** calculer les achats internationaux.

**Critères d’acceptation :**

1. Le taux peut être saisi manuellement.
2. Le taux peut être synchronisé via API si configurée.
3. L’historique des taux est conservé.
4. Le taux utilisé par commande est figé.
5. Les modifications sont journalisées.

---

### US-INT-04 : Générer un PDF

**En tant qu’utilisateur autorisé**,  
**je veux** générer un document PDF,  
**afin de** l’imprimer ou l’envoyer.

**Critères d’acceptation :**

1. Le PDF est généré côté backend.
2. Le document contient les informations métier correctes.
3. Le PDF peut être téléchargé.
4. Le PDF peut être envoyé par email.
5. La génération est journalisée.

---

### US-INT-05 : Scanner un code-barres

**En tant que** magasinier,  
**je veux** scanner un produit, lot ou emplacement,  
**afin de** saisir rapidement les informations.

**Critères d’acceptation :**

1. Le scan remplit automatiquement le champ concerné.
2. Si le code est inconnu, une erreur est affichée.
3. Si le code est ambigu, plusieurs résultats peuvent être proposés.
4. Le scan fonctionne avec lecteur USB type clavier.
5. Le scan peut fonctionner via caméra si supporté.

---

### US-INT-06 : Consulter les logs d’audit

**En tant qu’administrateur ou direction**,  
**je veux** consulter les logs d’audit,  
**afin de** vérifier les actions réalisées.

**Critères d’acceptation :**

1. Les logs affichent utilisateur, date, action, module.
2. Les logs affichent IP et UserAgent.
3. Les logs peuvent être filtrés par période, utilisateur, module.
4. Les logs peuvent être exportés.
5. L’accès aux logs est réservé aux rôles autorisés.

---

### US-INT-07 : Tracer un lot

**En tant que** responsable qualité,  
**je veux** tracer un lot,  
**afin de** connaître ses réceptions, mouvements et clients livrés.

**Critères d’acceptation :**

1. Le système affiche la réception d’origine.
2. Le système affiche les mouvements de stock.
3. Le système affiche les emplacements actuels.
4. Le système affiche les ventes liées.
5. Le système affiche les retours éventuels.
6. Le lot peut être bloqué depuis l’écran de traçabilité.

---

## 5.31 KPIs sécurité et intégrations

| KPI | Description |
|---|---|
| Tentatives de connexion échouées | Détection brute force. |
| Comptes verrouillés | Sécurité active. |
| Actions refusées | Permissions manquantes. |
| Emails envoyés | Suivi notifications. |
| Emails en échec | Détection problème SMTP. |
| SMS envoyés | Suivi alertes. |
| Jobs Hangfire en échec | Supervision technique. |
| Notifications SignalR envoyées | Temps réel. |
| PDF générés | Volume documents. |
| Imports Excel réussis/échoués | Qualité master data. |
| Logs consultés | Audit. |

---

## 5.32 Points à valider avec LABMEDIS

| Question | Impact |
|---|---|
| Faut-il activer la double authentification ? | Sécurité login. |
| Faut-il envoyer des SMS ou seulement emails ? | Intégration Twilio. |
| Quel serveur SMTP utiliser ? | Emails. |
| Faut-il une API de taux de change spécifique ? | Devises. |
| Faut-il exporter vers un logiciel comptable précis ? | Intégration comptable. |
| Faut-il une intégration douane/transitaire ? | Logistique. |
| Faut-il gérer plusieurs langues ? | Interface. |
| Faut-il conserver les documents PDF combien de temps ? | Archivage. |
| Faut-il bloquer les utilisateurs après combien de tentatives ? | Sécurité. |
| Faut-il une durée de session spécifique ? | UX/sécurité. |
| Faut-il gérer les permissions par utilisateur en plus des rôles ? | RBAC avancé. |
| Faut-il masquer les prix à certains rôles ? | Permissions pricing. |
| Faut-il tracer les exports Excel/PDF ? | Audit. |
| Faut-il gérer les appareils mobiles/PDA ? | Scan entrepôt. |

---

## 5.33 Synthèse

Le module **Sécurité, Rôles (RBAC) & Intégrations** doit garantir un système :

- sécurisé,
- traçable,
- conforme au contexte pharmaceutique,
- protégé par rôles et permissions,
- connecté aux outils nécessaires,
- supervisé par logs et notifications.

Il doit reposer sur :

1. **ASP.NET Identity + JWT** pour l’authentification.
2. **RBAC par permissions** pour les autorisations.
3. **AuditLog + ILoggerManager** pour la traçabilité.
4. **FluentEmail + Twilio** pour notifications.
5. **SignalR** pour temps réel.
6. **Hangfire** pour jobs planifiés.
7. **DinkToPdf** pour documents PDF.
8. **API taux de change** pour devises internationales.
9. **Code-barres / QR** pour l’entrepôt.
10. **Exports comptables et Excel** pour la gestion administrative.

Ce module est transverse : il impacte tous les autres modules du projet LABMEDIS.
