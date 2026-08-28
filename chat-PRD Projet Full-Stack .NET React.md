### USER
Ecris le PRD du projet. lit et analyse tous les fichiers minutieusement afin d'écrire PRD vraiment complet. 

le projet sera développé comme suis

dans le dossier ./codebase/frontend le frontend en reactjs
et le dossier ./codebase/backend le backend en .net 9


Dens le dossier backend voici comment sera structurer le code.

**Rôle et Objectif**  
Tu es un développeur expert Full-Stack .NET Core (C#). Ton objectif est d'implémenter de nouvelles fonctionnalités (CRUD complet ou processus métiers) en respectant **strictement** l'architecture, les design patterns et les conventions de codage existantes du projet `[NomProjet]`.  
⚠️ **Règle absolue** : Ne dévie jamais de ces règles, n'invente pas de nouvelles architectures et ne propose pas de "meilleures pratiques" génériques qui contredisent ce document. Ton code doit s'intégrer de manière invisible et cohérente dans la base de code existante.

---

### 🏛️ 1. ARCHITECTURE GLOBALE (3 Couches)
1. **`[NomProjet].Core`** : Entités (Models), Interfaces et Repositories (Entity Framework Core).
2. **`[NomProjet].Service`** : Logique métier, DTOs (Requests/Responses), Services, Jobs (Hangfire) et SignalR.
3. **`[NomProjet].Presentation`** : Soit `[NomProjet].Api` (Contrôleurs API), soit `[NomProjet].BackOffice` (Contrôleurs MVC, Vues Razor, JS). *Adapte ta génération selon la couche cible demandée.*

---

### 🏆 2. RÈGLES D'OR (NON-NÉGOCIABLES)
1. **Héritage Service/Repository** : N'injecte JAMAIS le Repository dans le Service via le constructeur. Le Service DOIT hériter du Repository.  
   *Signature obligatoire* : `public class [Entité]Service : [Entité]Repository, I[Entité]Service`
2. **Soft Delete** : Utilise toujours `IsDeleted = true` plutôt que la suppression physique.
3. **Logging** : Utilise exclusivement `ILoggerManager` (wrapper NLog du projet), **jamais** le `ILogger<T>` standard de Microsoft.
4. **Mapping** : Privilégie le **mapping manuel**. Si AutoMapper est explicitement requis, le Profile doit être dans le *même fichier physique* que le Request.
5. **Gestion de la Culture (Frontend)** : Dans les DTOs `Request`, utilise toujours le type `string` pour les champs numériques, monétaires ou décimaux afin d'éviter les problèmes de formatage culturel côté frontend. La conversion se fait via une méthode manuelle (ex: `.ToDouble()`).

---

### 📦 3. SPÉCIFICATIONS PAR COUCHE

#### A. Couche Core (`[NomProjet].Core`)
* **Entités** : Doivent hériter de `BaseEntity` (qui gère `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`). Emplacement : `/Models/Entities`.
* **Repositories** :  
  * Interface : `public interface I[Entité]Repository`  
  * Classe : `public class [Entité]Repository : BaseRepository<[Entité]>, I[Entité]Repository`  
  * N'ajoute dans le Repository que les requêtes complexes (avec `.Include`, `.ThenInclude` ou clauses `Where` avancées). Le CRUD de base est géré par `BaseRepository`.

#### B. Couche Service (`[NomProjet].Service`)
* **DTOs Requests** :  
  * Utilise `[SwaggerSchema(Description = "...")]` si applicable.  
  * Propriétés décimales/monétaires en `string`.  
  * Doit contenir une méthode de mapping manuel : `public [Entité] To[Entité]()`.
* **DTOs Responses** :  
  * Le constructeur DOIT prendre l'entité en paramètre pour le mapping manuel : `public [Entité]Response([Entité] entity) { this.Id = entity.Id; ... }`.
* **Service** : Implémente l'interface et hérite du Repository comme spécifié dans les "Règles d'Or".

#### C. Couche Présentation : API (`[NomProjet].Api`) *(Si applicable)*
* **Contrôleur** : Attributs `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`.
* **Injection** : `I[Entité]Service`, `ILoggerManager`, `IUserService` (si contexte utilisateur requis).
* **Structure d'action obligatoire** :
  1. Récupération de l'utilisateur courant.
  2. Log d'information *avant* l'action :  
     `_logger.LogInfo($"{user.LastName} {user.FirstName} ({user.UserName}) | Début [NomAction] | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserAgent: {Request.GetUserAgentName()}");`
  3. Bloc `try { ... } catch (Exception ex) { ... }`.
  4. Dans le `try` : Appel au service et retour du résultat (ex: `Ok(response)`).
  5. Dans le `catch` :  
     `_logger.LogError(ex, $"{user.LastName} ... | Erreur [NomAction] : {ex.Message} | IP: {Request.GetIp()}");`  
     `return BadRequest(new { message = "Un message convivial et sécurisé pour l'utilisateur" });`  
     ⚠️ **Ne retourne jamais `StatusCode(500)`**. Le middleware global gère les crashs critiques.


---

### 🛠️ 4. OUTILS ET LIBRAIRIES SPÉCIFIQUES
* **Background Jobs** : Utilise exclusivement **Hangfire**. Place les jobs dans `[NomProjet].Service/Jobs`.
* **Temps réel** : Utilise **SignalR** pour les notifications. Pas de polling. Les SMS/Emails passent par `INotificationService` (FluentEmail / Twilio).
* **Opérations de masse** : Pour insérer/mettre à jour/supprimer des milliers de lignes, utilise les méthodes `BulkInsertAsync`, `BulkUpdateAsync` du `BaseRepository` (EFCore.BulkExtensions).



## 📝 6. LOGS, ERREURS ET NOTIFICATIONS

*   **Logger** : Utilise exclusivement `ILoggerManager` (wrapper NLog), **jamais** `ILogger<T>`.
*   **Format de Log (À respecter à la lettre)** :
    ```csharp
    // Au début du Try
    _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début [NomAction] | {Request.Method} {Request.Path} {Request.GetRequestData()} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");
    
    // Dans le Catch
    _logger.LogError(ex, $"{user?.LastName} ... | Echec [NomAction] : {ex.Message} | IP: {Request.GetIp()}");
    ```
*   **Notifications UI (Toasts)** : Utilise la classe utilitaire statique avant un `RedirectToAction` :
    *   Succès : `Notif.Success($"Opération réussie", new string[] { "Toast" });`
    *   Erreur : `Notif.Error($"Erreur : {ex.Message}", new string[] { "Toast" });`

---

## 🧰 7. STACK TECHNIQUE & PACKAGES CLÉS (Référence)
*   **Data** : `Microsoft.EntityFrameworkCore.SqlServer`, `EFCore.BulkExtensions`
*   **Sécurité** : `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
*   **Background/Realtime** : `Hangfire`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`
*   **Utils** : `NLog`, `UAParser` (pour le User-Agent), `DinkToPdf` (PDF), `FluentEmail`, `Stripe`

---

### ✅ 5. CHECKLIST DE VÉRIFICATION AVANT GÉNÉRATION
Avant de produire le code, assure-toi mentalement que :
- [ ] Les interfaces (`I[Entité]Repository`, `I[Entité]Service`) sont générées avant les classes concrètes.
- [ ] Le Service hérite bien du Repository (`class Service : Repository, IService`).
- [ ] Les champs décimaux des Requests sont des `string` avec une méthode de conversion `.To[Entité]()`.
- [ ] Les logs utilisent `ILoggerManager` avec le format exact (User, Action, Method, Path, IP, UserAgent).
- [ ] Aucune modale Bootstrap n'est codée en dur dans les vues MVC (utilisation des fonctions JS globales).
- [ ] Le Soft Delete est appliqué partout.

**Prends une profonde respiration. Analyse la demande spécifique de l'utilisateur. Génère un code C# propre, complet et parfaitement intégré dans ce moule architectural, fichier par fichier, sans omettre aucune dépendance.**



**Rôle et Objectif**  
Tu es un développeur expert Full-Stack .NET Core (C#). Ton objectif est d'implémenter de nouvelles fonctionnalités (CRUD complet ou processus métiers) en respectant **strictement** l'architecture, les design patterns et les conventions de codage existantes du projet `[NomProjet]`.  
⚠️ **Règle absolue** : Ne dévie jamais de ces règles, n'invente pas de nouvelles architectures et ne propose pas de "meilleures pratiques" génériques qui contredisent ce document. Ton code doit s'intégrer de manière invisible et cohérente dans la base de code existante.

---

### 🏛️ 1. ARCHITECTURE GLOBALE (3 Couches)
1. **`[NomProjet].Core`** : Entités (Models), Interfaces et Repositories (Entity Framework Core).
2. **`[NomProjet].Service`** : Logique métier, DTOs (Requests/Responses), Services, Jobs (Hangfire) et SignalR.
3. **`[NomProjet].Presentation`** : Soit `[NomProjet].Api` (Contrôleurs API), soit `[NomProjet].BackOffice` (Contrôleurs MVC, Vues Razor, JS). *Adapte ta génération selon la couche cible demandée.*

---

### 🏆 2. RÈGLES D'OR (NON-NÉGOCIABLES)
1. **Héritage Service/Repository** : N'injecte JAMAIS le Repository dans le Service via le constructeur. Le Service DOIT hériter du Repository.  
   *Signature obligatoire* : `public class [Entité]Service : [Entité]Repository, I[Entité]Service`
2. **Soft Delete** : Utilise toujours `IsDeleted = true` plutôt que la suppression physique.
3. **Logging** : Utilise exclusivement `ILoggerManager` (wrapper NLog du projet), **jamais** le `ILogger<T>` standard de Microsoft.
4. **Mapping** : Privilégie le **mapping manuel**. Si AutoMapper est explicitement requis, le Profile doit être dans le *même fichier physique* que le Request.
5. **Gestion de la Culture (Frontend)** : Dans les DTOs `Request`, utilise toujours le type `string` pour les champs numériques, monétaires ou décimaux afin d'éviter les problèmes de formatage culturel côté frontend. La conversion se fait via une méthode manuelle (ex: `.ToDouble()`).

---

### 📦 3. SPÉCIFICATIONS PAR COUCHE

#### A. Couche Core (`[NomProjet].Core`)
* **Entités** : Doivent hériter de `BaseEntity` (qui gère `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`). Emplacement : `/Models/Entities`.
* **Repositories** :  
  * Interface : `public interface I[Entité]Repository`  
  * Classe : `public class [Entité]Repository : BaseRepository<[Entité]>, I[Entité]Repository`  
  * N'ajoute dans le Repository que les requêtes complexes (avec `.Include`, `.ThenInclude` ou clauses `Where` avancées). Le CRUD de base est géré par `BaseRepository`.

#### B. Couche Service (`[NomProjet].Service`)
* **DTOs Requests** :  
  * Utilise `[SwaggerSchema(Description = "...")]` si applicable.  
  * Propriétés décimales/monétaires en `string`.  
  * Doit contenir une méthode de mapping manuel : `public [Entité] To[Entité]()`.
* **DTOs Responses** :  
  * Le constructeur DOIT prendre l'entité en paramètre pour le mapping manuel : `public [Entité]Response([Entité] entity) { this.Id = entity.Id; ... }`.
* **Service** : Implémente l'interface et hérite du Repository comme spécifié dans les "Règles d'Or".

#### C. Couche Présentation : API (`[NomProjet].Api`) *(Si applicable)*
* **Contrôleur** : Attributs `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`.
* **Injection** : `I[Entité]Service`, `ILoggerManager`, `IUserService` (si contexte utilisateur requis).
* **Structure d'action obligatoire** :
  1. Récupération de l'utilisateur courant.
  2. Log d'information *avant* l'action :  
     `_logger.LogInfo($"{user.LastName} {user.FirstName} ({user.UserName}) | Début [NomAction] | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserAgent: {Request.GetUserAgentName()}");`
  3. Bloc `try { ... } catch (Exception ex) { ... }`.
  4. Dans le `try` : Appel au service et retour du résultat (ex: `Ok(response)`).
  5. Dans le `catch` :  
     `_logger.LogError(ex, $"{user.LastName} ... | Erreur [NomAction] : {ex.Message} | IP: {Request.GetIp()}");`  
     `return BadRequest(new { message = "Un message convivial et sécurisé pour l'utilisateur" });`  
     ⚠️ **Ne retourne jamais `StatusCode(500)`**. Le middleware global gère les crashs critiques.


---

### 🛠️ 4. OUTILS ET LIBRAIRIES SPÉCIFIQUES
* **Background Jobs** : Utilise exclusivement **Hangfire**. Place les jobs dans `[NomProjet].Service/Jobs`.
* **Temps réel** : Utilise **SignalR** pour les notifications. Pas de polling. Les SMS/Emails passent par `INotificationService` (FluentEmail / Twilio).
* **Opérations de masse** : Pour insérer/mettre à jour/supprimer des milliers de lignes, utilise les méthodes `BulkInsertAsync`, `BulkUpdateAsync` du `BaseRepository` (EFCore.BulkExtensions).



## 📝 6. LOGS, ERREURS ET NOTIFICATIONS

*   **Logger** : Utilise exclusivement `ILoggerManager` (wrapper NLog), **jamais** `ILogger<T>`.
*   **Format de Log (À respecter à la lettre)** :
    ```csharp
    // Au début du Try
    _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début [NomAction] | {Request.Method} {Request.Path} {Request.GetRequestData()} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");
    
    // Dans le Catch
    _logger.LogError(ex, $"{user?.LastName} ... | Echec [NomAction] : {ex.Message} | IP: {Request.GetIp()}");
    ```
*   **Notifications UI (Toasts)** : Utilise la classe utilitaire statique avant un `RedirectToAction` :
    *   Succès : `Notif.Success($"Opération réussie", new string[] { "Toast" });`
    *   Erreur : `Notif.Error($"Erreur : {ex.Message}", new string[] { "Toast" });`

---

## 🧰 7. STACK TECHNIQUE & PACKAGES CLÉS (Référence)
*   **Data** : `Microsoft.EntityFrameworkCore.SqlServer`, `EFCore.BulkExtensions`
*   **Sécurité** : `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
*   **Background/Realtime** : `Hangfire`, `Microsoft.AspNetCore.SignalR.StackExchangeRedis`
*   **Utils** : `NLog`, `UAParser` (pour le User-Agent), `DinkToPdf` (PDF), `FluentEmail`, `Stripe`

---

### ✅ 5. CHECKLIST DE VÉRIFICATION AVANT GÉNÉRATION
Avant de produire le code, assure-toi mentalement que :
- [ ] Les interfaces (`I[Entité]Repository`, `I[Entité]Service`) sont générées avant les classes concrètes.
- [ ] Le Service hérite bien du Repository (`class Service : Repository, IService`).
- [ ] Les champs décimaux des Requests sont des `string` avec une méthode de conversion `.To[Entité]()`.
- [ ] Les logs utilisent `ILoggerManager` avec le format exact (User, Action, Method, Path, IP, UserAgent).
- [ ] Aucune modale Bootstrap n'est codée en dur dans les vues MVC (utilisation des fonctions JS globales).
- [ ] Le Soft Delete est appliqué partout.

**Prends une profonde respiration. Analyse la demande spécifique de l'utilisateur. Génère un code C# propre, complet et parfaitement intégré dans ce moule architectural, fichier par fichier, sans omettre aucune dépendance.**




### ASSISTANT
Voici le **Product Requirements Document (PRD)** complet et détaillé pour le projet de système de gestion intégrée (ERP métier) destiné au dépositaire pharmaceutique **LABMEDIS**. Ce document a été conçu en analysant minutieusement les fichiers fournis (listes de produits, clients, fournisseurs, structures de prix) et les notes de cadrage métier, tout en intégrant strictement vos exigences architecturales pour le backend `.NET 9` et le frontend `ReactJS`.

---

# 📄 Product Requirements Document (PRD)
**Projet :** ERP LABMEDIS - Gestion de Flux Pharmaceutique International  
**Version :** 1.0  
**Date :** 28 Août 2026  
**Stack Technique :** ReactJS (Frontend) / .NET 9 (Backend)

---

## 1. 📋 Executive Summary
**LABMEDIS** est un dépositaire pharmaceutique opérant au Togo et agissant comme un acteur majeur du commerce international. L'entreprise achète des produits (médicaments, laits infantiles, réactifs de laboratoire, cosmétiques) à des fabricants internationaux (France, Tunisie, Inde, Maroc, Suisse, Burkina Faso) et les distribue à des répartiteurs locaux, des hôpitaux et des cliniques.

**L'objectif du projet** est de développer une application web sur-mesure permettant de piloter l'intégralité de la chaîne de valeur : de l'achat international multi-devises (EUR, USD, XOF) et du suivi logistique (fret maritime, aérien, transit douanier), jusqu'à la gestion des stocks par lots, le calcul complexe des prix de revient pondérés, et la vente aux répartiteurs. Un enjeu majeur est l'anticipation des ruptures de stock via la gestion des délais de fabrication et de transport.

---

## 2. 🏢 Contexte Métier & Écosystème

### 2.1. Les Acteurs
*   **Les Fabricants / Fournisseurs Internationaux :**
    *   *Continental Commodities (France)* : Laits infantiles et céréales (France Lait).
    *   *HORIBA ABX SAS (France)* : Réactifs et équipements de laboratoire.
    *   *GALPHARMA (Tunisie)*, *IBERMA (Maroc)*, *B&B LIFE SCIENCE (Inde)*, *BIORESEARCH (Suisse)* : Médicaments et compléments alimentaires.
    *   *Maïa Africa SAS (Burkina Faso)*, *DEO GRATIAS PHARMA (Togo)* : Cosmétiques et médicaments locaux.
*   **Le Dépositaire (LABMEDIS) :** Achète en gros, gère le dédouanement, le stockage, et fixe les prix de vente.
*   **Les Clients (Répartiteurs & Structures de Santé) :**
    *   *Répartiteurs/Grossistes* : CAMEG, LABOREX TOGO, TEDIS PHARMA TOGO, UBIPHARM TOGO, DOGTA LAFIE, OCDI.
    *   *Structures de santé* : Clinique Mère et Enfant, CHP Aného, CHR Sokodé, Clinique les p'tits anges, Groupe Levant Sarl.

### 2.2. Typologie des Produits
Le catalogue est vaste et hétérogène, nécessitant une gestion fine des conditionnements et de la fiscalité :
1.  **Produits Infantiles** (France Lait 1er/2ème/3ème âge, AR, LF, Confort, Céréales) : Soumis à une TVA de 18%.
2.  **Médicaments** (Antalgiques, Antibiotiques, Antihistaminiques, etc.) : Gestion stricte des lots et péremptions.
3.  **Réactifs de Laboratoire** (ABX Pentra, Horiba) : Produits techniques, souvent sensibles à la température.
4.  **Compléments Alimentaires & Cosmétiques** (B-PROTEI, Pommade Maïa).

---

## 3. ⚙️ Périmètre Fonctionnel (Modules)

### Module 1 : Achats Internationaux & Logistique (Import)
*   **Gestion Multi-devises :** Saisie des Prix d'Achat (PA) en EUR ou USD. Conversion automatique ou manuelle vers le XOF (CFA) selon le taux de change du jour.
*   **Suivi des Conteneurs & Fret :**
    *   Distinction cruciale entre **Fret Maritime** (Bateau - lent, moins cher) et **Fret Aérien / Express** (Rapide, très cher).
    *   Suivi des statuts : *Commandé -> En fabrication -> Expédié -> En transit douanier -> Reçu en entrepôt*.
*   **Allocation des Frais (Landing Cost) :** Répartition des frais annexes (Commissions promo, Freight, Transit, Frais de transfert, Douane) sur les lignes de commandes pour calculer le vrai coût d'achat.

### Module 2 : Gestion des Stocks & Traçabilité (Lots)
*   **Identification Unique par Lot :** Chaque arrivage génère un N° de Lot unique avec une Date de Péremption.
*   **Gestion des Emplacements :** Adressage de l'entrepôt (Allée, Rack, Étagère) pour retrouver physiquement les produits (cartons, flacons, boîtes).
*   **Inventaire & Mouvements :** Entrées, Sorties, Ajustements, Transferts inter-dépôts.
*   **Alertes :** Notifications (SignalR) pour les produits approchant de leur date de péremption (FEFO : First Expired, First Out).

### Module 3 : Moteur de Pricing & Prix de Revient (CUMP)
*   **Structure de Prix Dynamique :** Reprendre la logique du fichier *Structure de prix.xlsx* :
    *   `PA (Euro)` ➔ `PA (CFA)`
    *   `+ Coeff. Commissions / Freight / Transit / Frais Transfert`
    *   `= Prix de Revient (PR) unitaire`
    *   `+ Marge (ex: 1.10 pour 10%)` ➔ `PV HT Calculé`
*   **CUMP Pondéré par Mode de Transport :** Le système doit maintenir un Coût Unitaire Moyen Pondéré, mais permettre une analyse de rentabilité séparée entre les lots arrivés par Bateau vs Avion.
*   **Gestion de la TVA :** Application automatique de la TVA (ex: 18% sur les laits et compléments, exonération ou taux spécifique sur les médicaments).

### Module 4 : Ventes & Répartiteurs
*   **Catalogue Client :** Prix négociés par répartiteur (CAMEG, LABOREX, etc.).
*   **Gestion des Commandes :** Validation des commandes, vérification de la disponibilité par lot (respect des péremptions).
*   **Facturation & Livraison :** Génération des Bons de Livraison (BL) et Factures.

### Module 5 : Prévisions & Anticipation (MRP simplifié)
*   **Délais de Reconstitution :** Configuration par fournisseur et produit (ex: 3 à 4 mois de délai de fabrication + transport maritime).
*   **Alertes de Réapprovisionnement :** Le système calcule le "Stock d'alerte" en fonction de la vélocité des ventes (rotation) et du délai d'import, et suggère des commandes d'achat.

---

## 4. 🏗️ Architecture Technique & Stack

Le projet sera monorepo ou structuré en deux dépôts distincts :
*   📂 `./codebase/frontend` : **ReactJS** (TypeScript, Vite, TailwindCSS ou Material-UI, Redux/Zustand pour le state management, SignalR Client pour le temps réel).
*   📂 `./codebase/backend` : **.NET 9** (C#), structuré en 3 couches strictes.

### 4.1. Règles d'Or du Backend (Non-Négociables)
Conformément à vos directives, l'architecture du backend (`LABMEDIS.Api`, `LABMEDIS.Service`, `LABMEDIS.Core`) respectera scrupuleusement :
1.  **Héritage Service/Repository** : `public class ProductService : ProductRepository, IProductService`.
2.  **Soft Delete Global** : Utilisation systématique de `IsDeleted = true` (via `BaseEntity` et Query Filters EF Core).
3.  **Logging** : Utilisation exclusive de `ILoggerManager` (NLog) avec traçabilité complète (User, IP, UserAgent, Action).
4.  **DTOs & Culture Frontend** : Tous les montants financiers (PA, PV, Marge, Frais) dans les `Requests` seront typés en `string` pour éviter les erreurs de parsing de culture (`,` vs `.`), avec mapping manuel via `.ToDecimal()`.
5.  **Background Jobs** : **Hangfire** pour les tâches récurrentes (ex: vérification quotidienne des péremptions, calcul des prévisions de stock).
6.  **Temps Réel** : **SignalR** pour pousser les alertes de stock critique ou de fin de lot au dashboard React.

---

## 5. 🗄️ Modèle de Données Conceptuel (Entités Core)

Voici les entités principales qui seront créées dans `LABMEDIS.Core/Models/Entities` (héritant toutes de `BaseEntity`) :

### 5.1. Master Data
*   **`Category`** : (ex: Lait infantile, Médicament, Réactif, Cosmétique). Gère les règles de TVA par défaut.
*   **`Supplier`** : (ex: HORIBA ABX SAS, Continental Commodities). Contient : Devise par défaut, Délai de fabrication moyen (jours), Pays.
*   **`Customer`** : (ex: CAMEG, LABOREX TOGO). Contient : Type (Répartiteur/Hôpital), Adresse, Limite de crédit.
*   **`Product`** : Désignation, Code CIP, Catégorie, Forme (Boîte, Flacon), Conditionnement (Carton de 12), Prix de vente cible, TVA.

### 5.2. Achats & Import
*   **`PurchaseOrder`** : Commande fournisseur. Contient : N° de conteneur, Mode de transport (Aérien/Maritime), Taux de change, Statut, Date de départ/arrivée prévue.
*   **`PurchaseOrderLine`** : Produit, Quantité, PA Unitaire (Devise), PA Unitaire (CFA).
*   **`ImportCost`** : Table pour éclater les frais (Freight, Transit, Douane, Commission) et les allouer aux lignes de commande (prorata).

### 5.3. Stocks
*   **`StockLot`** : N° de Lot, Date de péremption, Date de réception, Produit, Quantité initiale, Quantité restante, Emplacement, Mode d'arrivée (Bateau/Avion), Prix de Revient Unitaire (PRU) du lot.
*   **`StockMovement`** : Historique des entrées/sorties (Type : Achat, Vente, Ajustement, Perte/Péremption).

### 5.4. Ventes
*   **`SaleOrder`** : Commande client.
*   **`SaleOrderLine`** : Produit, Lot spécifique sélectionné (pour respecter le FEFO), Quantité, Prix de Vente HT, TVA.

---

## 6. 💻 Spécifications des Endpoints API (Exemples Clés)

### 🔹 Calcul de la Structure de Prix (Simulation)
*   **Endpoint** : `POST /api/pricing/simulate`
*   **Request (DTO)** :
    ```csharp
    public class PricingSimulationRequest {
        public int ProductId { get; set; }
        public string PurchasePriceEur { get; set; } // String pour la culture
        public string FreightCost { get; set; }
        public string TransitCost { get; set; }
        public string MarginMultiplier { get; set; } // ex: "1.10"
    }
    ```
*   **Logique Service** : Convertit les strings en `decimal`, applique la formule du fichier *Structure de prix.xlsx*, et retourne le `PV HT calculé` et la `Différence` avec le prix catalogue actuel.

### 🔹 Réception de Conteneur (Création de Lots)
*   **Endpoint** : `POST /api/purchase-orders/{id}/receive`
*   **Logique** :
    1. Valide le conteneur.
    2. Pour chaque ligne, crée une entité `StockLot` avec le N° de lot, la péremption, et calcule le PRU final en intégrant les frais de transit alloués.
    3. Met à jour le stock global.
    4. Log via `ILoggerManager` : `"Admin | Réception Conteneur CONT-2026-04 | POST /api/... | IP: ..."`

### 🔹 Alerte de Prévision (Job Hangfire)
*   **Job** : `StockForecastJob` (Exécuté chaque nuit).
*   **Logique** :
    1. Calcule la vitesse de rotation (Ventes des 90 derniers jours).
    2. Compare le stock actuel + stock en transit avec le stock nécessaire pour couvrir le *Délai Fournisseur + Délai Transport*.
    3. Si rupture prévue, crée une notification SignalR et une suggestion de `PurchaseOrder`.

---

## 7. 🚀 Exigences Non-Fonctionnelles

1.  **Sécurité & Rôles** :
    *   *Administrateur* : Accès total, configuration des marges et structures de prix.
    *   *Magasinier* : Saisie des réceptions, gestion des emplacements, inventaire.
    *   *Commercial* : Création de commandes clients, consultation des stocks disponibles.
    *   *Comptable* : Validation des frais de transit, vue sur les marges réelles.
2.  **Performance** :
    *   Utilisation de `EFCore.BulkExtensions` pour l'import massif des catalogues produits (ex: import Excel des 100+ réactifs HORIBA).
    *   Mise en cache (MemoryCache / Redis) des catalogues produits et des taux de change.
3.  **Audit** :
    *   Traçabilité financière stricte : on ne supprime jamais une commande ou un mouvement de stock (Soft Delete + Logs NLog horodatés).
4.  **UI/UX (Frontend React)** :
    *   Tableaux de bord avec graphiques (Recharts) montrant la répartition des stocks par date de péremption.
    *   Formulaires de saisie avec masques de saisie pour les devises (gestion automatique des séparateurs de milliers CFA).
    *   Scanner de code-barres / QR codes pour la gestion des emplacements et des lots en entrepôt.

---

## 8. 📅 Stratégie de Déploiement & Roadmap

### Phase 1 : Fondations & Master Data (Semaines 1-3)
*   Setup du monorepo (`./codebase/frontend` et `./codebase/backend`).
*   Implémentation de `BaseEntity`, `BaseRepository`, `ILoggerManager`.
*   CRUD des Fournisseurs, Clients, et Catégories.
*   Import initial du catalogue produit (depuis les fichiers Excel fournis).

### Phase 2 : Achats, Imports & Moteur de Prix (Semaines 4-7)
*   Gestion des commandes fournisseurs multi-devises.
*   Implémentation du moteur de calcul de Prix de Revient (Landing Cost).
*   Réception de marchandises et génération des `StockLots`.

### Phase 3 : Stocks, Ventes & Alertes (Semaines 8-11)
*   Gestion des emplacements et mouvements de stock.
*   Workflow de vente (Commande ➔ Allocation de Lot ➔ BL ➔ Facture).
*   Mise en place des Jobs Hangfire pour les alertes de péremption et de réapprovisionnement.

### Phase 4 : Dashboard, SignalR & Recette (Semaines 12-14)
*   Intégration SignalR pour les notifications temps réel (Stock faible, conteneur arrivé au port).
*   Tests de charge et UAT (User Acceptance Testing) avec l'équipe LABMEDIS.
*   Déploiement sur serveur de production (Linux/Docker ou Windows Server/IIS).

---
*Ce PRD sert de contrat de référence pour l'équipe de développement. Toute déviation par rapport à l'architecture .NET spécifiée (notamment l'héritage Service/Repository et la gestion des DTOs en `string`) sera rejetée lors des revues de code (Pull Requests).*

### USER
Quels sont les documen qui manque pour rendre se prd complet

### ASSISTANT
Excellente question. Bien que les 4 fichiers fournis offrent une **très bonne base métier** (catalogue, clients/fournisseurs, structure de prix d'une gamme, et vision macro du flux), il reste des **zones d'ombre critiques** qui bloqueraient le développement de l'architecture .NET/React si elles n'étaient pas clarifiées.

Voici la liste exhaustive des documents et informations manquantes, classées par domaine d'impact sur le PRD et le code futur :

---

### 🚨 1. Règles Financières & Moteur de Pricing (CRITIQUE pour le Backend .NET)

Le fichier *Structure de prix.xlsx* ne montre que la gamme **France Lait**. Pour que le moteur de calcul dans `[LABMEDIS].Service` soit complet, il manque :

*   **La formule mathématique exacte du Prix de Revient (PR) :**
    *   Dans le fichier, on voit les coefficients : `Commissions Promo (1.25)`, `Freight (1.03)`, `Transit (1.09)`, `Frais transfert (1.07)`.
    *   *Question :* S'agit-il d'une **multiplication en cascade** (`PA CFA * 1.25 * 1.03 * 1.09 * 1.07`) ou d'une **addition de pourcentages** ?
    *   *Impact code :* Cela change radicalement la méthode `CalculateLandingCost()` dans le `PricingService`.
*   **Les structures de prix pour les autres gammes :**
    *   Comment sont calculés les prix pour les **Médicaments** (GALPHARMA, IBERMA) et les **Réactifs de Labo** (HORIBA) ? Les coefficients de freight/transit sont-ils identiques, ou spécifiques à chaque fournisseur/mode de transport ?
*   **La gestion des devises et du taux de change :**
    *   Le taux Euro/CFA et USD/CFA est-il **fixe** (ex: 655.957 XOF pour 1 EUR), **saisi manuellement** à chaque commande, ou **récupéré via une API** (Banque de France, BCE) ?
*   **Les règles de TVA (Très important au Togo) :**
    *   On voit 18% sur les laits infantiles et cosmétiques.
    *   *Question :* Les **médicaments** sont-ils exonérés de TVA (0%) ? Les **réactifs de laboratoire** sont-ils taxés ?
    *   *Impact code :* Détermination du champ `DefaultVatRate` sur l'entité `Category`.

---

### 📦 2. Gestion Physique des Stocks & Entrepôt (Impact sur le Frontend React & Backend)

*   **Le plan d'adressage de l'entrepôt (WMS) :**
    *   Comment sont structurés les emplacements ? (ex: `Allée-Rack-Niveau-Position` ou `Zone-Étagère-Bac`).
    *   *Impact code :* Structure de l'entité `StorageLocation` et UI de type "map" sur React pour le magasinier.
*   **La règle d'assignation des lots à la vente (FEFO vs FIFO) :**
    *   Lorsqu'un répartiteur (ex: LABOREX) commande 100 boîtes d'`ALLERGICA`, le système doit-il **imposer automatiquement** le lot avec la date de péremption la plus proche (FEFO - First Expired First Out, obligatoire en pharma) ? Ou le magasinier choisit-il manuellement son lot ?
*   **La gestion des unités de mesure (UOM) et du "dépotage" :**
    *   Le fichier montre : `Conditionnement: carton/12` ou `carton/72`.
    *   *Question :* Achète-t-on en **Cartons** mais vend-on en **Boîtes/Flacons** à l'unité ?
    *   *Impact code :* Nécessite un convertisseur d'unités dans le `StockMovementService` (ex: 1 Carton = 12 Boîtes en stock).
*   **La gestion de la chaîne du froid :**
    *   Certains produits (ex: réactifs HORIBA, insulines) nécessitent-ils un suivi de la température ? Y a-t-il des chambres froides à monitorer ?

---

### 🔄 3. Workflows Opérationnels & User Stories Détaillées

Il manque les **cas limites** et les processus de validation (très importants pour les contrôleurs `[LABMEDIS].Api`) :

*   **Le cycle de vie d'une commande fournisseur (Import) :**
    *   Y a-t-il une gestion des **acomptes** versés au fabricant (ex: 30% à la commande, 70% à l'embarquement) ?
    *   Comment gère-t-on les **écarts de livraison** ? (Le fournisseur annonce 1000 cartons, le conteneur n'en contient que 980. Comment régulariser le PRU ?).
*   **Le workflow de vente B2B :**
    *   Passe-t-on par des étapes : `Devis ➔ Commande validée ➔ Bon de Livraison (BL) ➔ Facture` ?
    *   Y a-t-il une gestion des **remises commerciales** spécifiques par répartiteur (ex: LABOREX a -5% sur le catalogue, CAMEG a -2%) ?
*   **La gestion des périmés et retours (Reverse Logistics) :**
    *   Que fait-on d'un lot périmé ? (Destruction physique avec PV de destruction pour la comptabilité ? Retour au fabricant ?).
    *   Comment gère-t-on les retours clients (médicaments endommagés) ?

---

### ⏱️ 4. Moteur d'Anticipation (MRP) & Prévisions

Le transcript audio mentionne : *"il faut peut-être 3 mois, 4 mois d'avance pour commander"*.

*   **Les Lead Times (Délais) par fournisseur :**
    *   Il manque un document listant le **délai de fabrication moyen** par fournisseur (ex: Continental Commodities = 45 jours, GALPHARMA = 30 jours).
*   **Le calcul du Stock d'Alerte (Reorder Point) :**
    *   Sur quelle base calcule-t-on le seuil d'alerte ? (ex: `Ventes moyennes des 90 derniers jours * (Délai fabrication + Délai maritime)`).
*   **Le mode de transport par défaut :**
    *   Peut-on configurer un produit pour qu'il soit **toujours** commandé en Aérien (ex: produits chers et légers) ou toujours en Maritime (ex: laits en poudre volumineux) ?

---

### 🔐 5. Sécurité, Rôles (RBAC) & Intégrations

*   **Matrice des Rôles et Permissions :**
    *   Qui a le droit de modifier la "Structure de Prix" (la marge) ? (Probablement la Direction uniquement).
    *   Le magasinier peut-il créer une commande client, ou seulement préparer les BL ?
    *   *Impact code :* Configuration précise des `[Authorize(Roles = "Admin,Commercial")]` sur les contrôleurs .NET.
*   **Intégrations tierces :**
    *   Le système doit-il exporter les factures vers un logiciel comptable existant (Sage, Ciel) ?
    *   Faut-il une intégration avec les API des transitaires en douane au port de Lomé ?
*   **Impression & Étiquetage :**
    *   Le système doit-il générer des étiquettes de code-barres (EAN-13 ou Datamatrix pour les lots pharma) pour les imprimantes type Zebra ?

---

### 📊 6. Reporting & Tableaux de Bord (KPIs)

Que veut voir le dirigeant de LABMEDIS sur son dashboard React le matin ?
*   Marge réelle par conteneur (Bateau vs Avion).
*   Valorisation du stock actuel (en CFA) par date de péremption.
*   Top 10 des produits à rotation lente (Dead Stock).
*   Suivi des conteneurs en transit (Map with ETA).

---

### 🎯 Résumé : Le "Data Request" à envoyer au client LABMEDIS

Pour débloquer le développement, je vous suggère de demander au client ces **3 documents complémentaires** :

1.  **Le fichier Excel complet de Pricing** (avec les formules actives) pour **toutes** les familles de produits (pas seulement France Lait).
2.  **La grille tarifaire et de remises** appliquée à leurs 4 grands répartiteurs (CAMEG, LABOREX, TEDIS, UBIPHARM).
3.  **Un schéma du processus de réception** (Qui fait quoi entre l'arrivée du conteneur au port de Lomé et la mise en rayon dans l'entrepôt).

Dès que vous avez ces éléments, nous pourrons générer les entités `Core` complètes, les DTOs `Service` et les contrôleurs `Api` en respectant scrupuleusement votre architecture .NET 9.

### USER
ecris moi les 1. Règles Financières & Moteur de Pricing (CRITIQUE pour le Backend .NET)

### ASSISTANT
Voici la spécification détaillée et technique de la section **1. Règles Financières & Moteur de Pricing**, rédigée spécifiquement pour servir de contrat de développement pour le backend `.NET 9` et le frontend `ReactJS`. 

Cette analyse est basée sur la rétro-ingénierie exacte du fichier *Structure de prix.xlsx* (Gamme France Lait) et des règles fiscales togolaises applicables au catalogue LABMEDIS.

---

# 1. 💰 Règles Financières & Moteur de Pricing (Spécifications Backend)

Le moteur de pricing de LABMEDIS ne se limite pas à une simple marge commerciale. Il s'agit d'un **calcul de coût de revient complet (Landing Cost)** intégrant la logistique internationale multi-modale (Aérien vs Maritime) et les frais de structure, avant application de la marge cible.

## 1.1. La Formule Mathématique du Prix de Revient (PR)
L'analyse du fichier *Structure de prix.xlsx* démontre que le Prix de Revient (PR) est calculé via une **multiplication en cascade** de coefficients multiplicateurs sur le Prix d'Achat (PA) converti en CFA.

**Formule Officielle LABMEDIS :**
```text
PR (CFA) = PA (CFA) × Coeff_Commission × Coeff_Freight × Coeff_Transit × Coeff_FraisTransfert
```

**Exemple vérifié (France Lait 1er âge 400g) :**
*   **PA Euro** : 3,41 €
*   **Taux de change** : ~656 XOF/EUR (2237 / 3,41)
*   **PA CFA** : 2 237 CFA
*   **Coefficients** : Promo (1.25) × Freight (1.03) × Transit (1.09) × Frais transfert (1.07)
*   **Calcul** : `2237 × 1.25 × 1.03 × 1.09 × 1.07 = 3359,17` ➔ **Arrondi à 3 359 CFA**

**Calcul du Prix de Vente HT Cible (PV Théorique) :**
```text
PV HT Calculé = PR (CFA) × Coeff_Marge
```
*   **Marge France Lait** : 1.10 (soit 10% de markup sur le PR)
*   **Calcul** : `3359 × 1.10 = 3694,9` ➔ **Arrondi à 3 695 CFA**
*   *Note Métier :* Le système devra comparer ce `PV HT Calculé` avec le `Prix Labmedis HT` (prix catalogue politique) pour alerter la direction si la marge réelle s'érode (ex: Différence de -35 CFA sur le 1er âge 400g).

## 1.2. Architecture des Coefficients (Entités `Core`)
Les coefficients ne doivent **jamais** être codés en dur. Ils doivent être stockés en base de données pour permettre à la direction de les ajuster sans redéploiement, et doivent pouvoir varier selon le **Mode de Transport** (Aérien vs Maritime) et la **Catégorie de Produit**.

**Entité `PricingProfile` (LABMEDIS.Core) :**
*   `Id` (Guid)
*   `Name` (string, ex: "Import Maritime Lait Infantile")
*   `SupplierId` (Guid? - nullable pour une règle globale)
*   `CategoryId` (Guid? - nullable)
*   `TransportMode` (Enum: Maritime, Aerien, Express, Terrestre)
*   `CommissionCoeff` (decimal)
*   `FreightCoeff` (decimal)
*   `TransitCoeff` (decimal)
*   `TransferFeeCoeff` (decimal)
*   `TargetMarginCoeff` (decimal)

## 1.3. Gestion Multi-Devises et Taux de Change
LABMEDIS achète en **EUR** (Europe, Maroc, Tunisie), **USD** (Inde, Suisse) et revend localement en **XOF (CFA)**.

*   **Règle d'Or :** Le taux de change n'est pas fixe. Il doit être saisi (ou récupéré via API) au moment de l'émission de la `PurchaseOrder` (Commande Fournisseur).
*   **Entité `ExchangeRate`** :
    *   `CurrencyFrom` (ex: EUR)
    *   `CurrencyTo` (ex: XOF)
    *   `Rate` (decimal)
    *   `EffectiveDate` (DateTime)
*   **Logique Service :** Lors de la validation d'une commande d'achat, le système fige le taux de change du jour sur la ligne de commande (`LockedExchangeRate`) pour garantir la traçabilité financière du lot, même si le taux change le lendemain.

## 1.4. Fiscalité et Mapping TVA
L'analyse croisée des fichiers de produits révèle une fiscalité stricte basée sur la **Catégorie Thérapeutique/Commerciale** :

| Catégorie (Entité `Category`) | Taux de TVA par Défaut | Exemples de Produits |
| :--- | :--- | :--- |
| **Produit Infantile** | **18%** | France Lait (1er, 2ème, 3ème âge, Céréales) |
| **Cosmétique** | **18%** | Pommade Maïa |
| **Complément Alimentaire** | **18%** | B-PROTEI (ALL, MOM) |
| **Insecticide** | **18%** (à confirmer) | Strick Out Gel |
| **Médicament** | **0% (Exonéré)** | Galpharma, Iberma (Antibiotiques, Antalgiques...) |
| **Réactif de Laboratoire** | **0% ou 18%** | Horiba ABX (Nécessite un flag `IsTaxable` sur le produit) |

*   **Impact Backend :** Le `SaleOrderService` doit calculer automatiquement le `PV TTC` en fonction du `VatRate` de la catégorie du produit.

## 1.5. Stratégie d'Arrondi Monétaire (Rounding Strategy)
En franc CFA (XOF), il n'y a pas de centimes. Les calculs en cascade génèrent des décimales qui doivent être normalisées pour éviter les écarts de centimes entre le système et la comptabilité réelle.

*   **Règle :** Arrondi à l'entier le plus proche (MidpointRounding.AwayFromZero) pour le XOF.
*   **Implémentation .NET (Extension Method) :**
    ```csharp
    public static decimal ToCfaRounded(this decimal value) 
    {
        return Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }
    ```

---

## ⚙️ Spécifications d'Implémentation (.NET 9)

### A. Couche Service : DTOs et Mapping (Respect des Règles d'Or)
Pour éviter les bugs de désérialisation JSON côté Frontend React (qui utilise des claviers numériques avec des virgules ou des points selon la locale du navigateur), **tous les montants financiers dans les Requests DOIVENT être des `string`**.

**Fichier : `SimulatePricingRequest.cs` (LABMEDIS.Service/DTOs/Requests)**
```csharp
public class SimulatePricingRequest 
{
    [SwaggerSchema(Description = "ID du profil de pricing (ex: Maritime Lait)")]
    public Guid PricingProfileId { get; set; }

    [SwaggerSchema(Description = "Prix d'achat unitaire en devise étrangère (ex: '3.41')")]
    public string PurchasePriceForeign { get; set; } // STRING OBLIGATOIRE

    [SwaggerSchema(Description = "Taux de change du jour (ex: '656.01')")]
    public string ExchangeRate { get; set; } // STRING OBLIGATOIRE

    // Méthode de mapping manuel (Règle d'or)
    public PricingSimulation ToDomainModel() 
    {
        return new PricingSimulation 
        {
            PricingProfileId = this.PricingProfileId,
            PurchasePriceForeign = this.PurchasePriceForeign.ToDecimal(), // Extension method custom
            ExchangeRate = this.ExchangeRate.ToDecimal()
        };
    }
}
```

### B. Couche Service : Logique Métier (`PricingService`)
Le service hérite du repository (Règle d'or) et encapsule la formule en cascade.

```csharp
public class PricingService : PricingRepository, IPricingService
{
    private readonly ILoggerManager _logger;

    public PricingService(AppDbContext context, ILoggerManager logger) : base(context) 
    {
        _logger = logger;
    }

    public PricingSimulationResponse CalculateLandingCost(PricingSimulation request)
    {
        // 1. Récupération du profil (Coefficients)
        var profile = GetById(request.PricingProfileId); 
        
        // 2. Conversion devise
        decimal paCfa = request.PurchasePriceForeign * request.ExchangeRate;

        // 3. Formule en cascade
        decimal prCfa = paCfa 
                      * profile.CommissionCoeff 
                      * profile.FreightCoeff 
                      * profile.TransitCoeff 
                      * profile.TransferFeeCoeff;

        // 4. Marge
        decimal pvHtTheorique = prCfa * profile.TargetMarginCoeff;

        return new PricingSimulationResponse 
        {
            PurchasePriceCfa = paCfa.ToCfaRounded(),
            LandingCostCfa = prCfa.ToCfaRounded(),
            TargetPriceHtCfa = pvHtTheorique.ToCfaRounded()
        };
    }
}
```

### C. Couche API : Contrôleur et Logging
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public PricingController(IPricingService pricingService, ILoggerManager logger, IUserService userService)
    {
        _pricingService = pricingService;
        _logger = logger;
        _userService = userService;
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulatePricing([FromBody] SimulatePricingRequest request)
    {
        var user = await _userService.GetCurrentUserAsync(User);
        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début SimulatePricing | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try 
        {
            var domainModel = request.ToDomainModel();
            var result = _pricingService.CalculateLandingCost(domainModel);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec SimulatePricing : {ex.Message} | IP: {Request.GetIp()}");
            return BadRequest(new { message = "Erreur lors de la simulation du pricing. Vérifiez les formats numériques." });
        }
    }
}
```

---
**✅ Prochaine étape recommandée :** Valider ces formules avec le Directeur Financier de LABMEDIS. Il faut spécifiquement lui poser la question : *"Les coefficients (1.25, 1.03, etc.) s'appliquent-ils de la même manière pour les conteneurs de Médicaments (Galpharma) que pour le Lait, ou avez-vous une autre grille de coefficients pour la pharma ?"*

### USER
ecris moi 2. Gestion Physique des Stocks & Entrepôt (Impact sur le Frontend React & Backend)

### ASSISTANT
Voici la section **2. Gestion Physique des Stocks & Entrepôt**, rédigée comme une spécification complète et exploitable pour le développement du projet avec :

- Frontend : **ReactJS** dans `./codebase/frontend`
- Backend : **.NET 9** dans `./codebase/backend`
- Architecture backend stricte : `Core`, `Service`, `Api`
- Règles d’or : héritage `Service : Repository`, soft delete, `ILoggerManager`, mapping manuel, DTO avec valeurs numériques/monétaires en `string`, Hangfire, SignalR.

---

# 2. 📦 Gestion Physique des Stocks & Entrepôt

## 2.1 Objectif du module

Le module **Gestion Physique des Stocks & Entrepôt** doit permettre à LABMEDIS de piloter avec précision :

1. La réception des produits importés ou achetés localement.
2. La gestion des produits par **lot**.
3. La gestion des **dates de péremption**.
4. Le stockage physique dans des **emplacements précis**.
5. Les mouvements de stock : entrée, sortie, transfert, ajustement, retour, destruction.
6. La disponibilité réelle des produits pour les ventes.
7. L’anticipation des ruptures et des péremptions.
8. La traçabilité complète de chaque produit, du fournisseur jusqu’au client.

Ce module est critique car LABMEDIS gère :

- des produits pharmaceutiques,
- des produits infantiles,
- des compléments alimentaires,
- des cosmétiques,
- des insecticides,
- des réactifs de laboratoire,
- des produits importés par avion, bateau ou express.

---

## 2.2 Périmètre fonctionnel couvert

Le module doit couvrir les processus suivants :

| Processus | Description |
|---|---|
| Réception fournisseur | Enregistrer l’arrivée d’un conteneur, d’un fret aérien, d’un express ou d’un achat local. |
| Création de lots | Chaque produit reçu doit être rattaché à un numéro de lot et à une date de péremption. |
| Mise en stock | Ranger les produits dans des emplacements physiques. |
| Gestion des emplacements | Identifier précisément où se trouve chaque produit dans l’entrepôt. |
| Gestion des conditionnements | Gérer les cartons, boîtes, flacons, tubes, sachets, etc. |
| Mouvements internes | Transférer des produits d’un emplacement à un autre. |
| Préparation de commande | Réserver, picker et sortir les produits selon une règle FEFO/FIFO. |
| Inventaire | Réaliser des inventaires complets ou partiels. |
| Gestion des péremptions | Alerter sur les lots proches de péremption. |
| Gestion des anomalies | Quarantaine, produits endommagés, manquants, détruits ou périmés. |
| Traçabilité | Historiser tous les mouvements avec utilisateur, date, motif et référence. |

---

## 2.3 Concepts métier principaux

### 2.3.1 Produit

Le produit est la fiche de référence commerciale et logistique.

Exemples présents dans les données :

- `France Lait 1er âge 400g`
- `France Lait 2ème âge 900g`
- `Pommade Maïa 100 ml`
- `ALLERGICA 10MG CPR B/30`
- `ABX PENTRA UREA CP`
- `B-PROTEI ALL 200g`

Un produit possède notamment :

| Champ | Description |
|---|---|
| Désignation | Nom commercial du produit. |
| Catégorie | Produit infantile, médicament, cosmétique, complément alimentaire, insecticide, réactif de laboratoire. |
| Forme | Boîte, flacon, tube, gel, sachet, etc. |
| Dosage | Exemple : `400g`, `100ml`, `10mg`, `30g`. |
| Conditionnement | Exemple : `carton/12`, `carton/6`, `carton/72`. |
| Classe thérapeutique | Antalgique, antibiotique, antihistaminique, lait infantile, etc. |
| Code CIP | Code identifiant produit, utile pour intégrations, étiquettes ou recherches. |
| Fournisseur | Fournisseur principal du produit. |
| TVA | Taux applicable, ex : 18% ou 0%. |
| Mode de transport par défaut | Maritime, aérien, express, terrestre. |
| Seuil de stock minimum | Quantité déclenchant une alerte de réapprovisionnement. |
| Délai de fabrication | Nombre de jours estimés chez le fabricant. |
| Délai de livraison | Nombre de jours estimés entre expédition et réception. |

---

### 2.3.2 Lot

Le lot est une notion obligatoire dans le domaine pharmaceutique.

Chaque réception de produit doit permettre d’enregistrer :

| Information | Description |
|---|---|
| Numéro de lot fournisseur | Numéro imprimé sur les produits. |
| Numéro de lot interne | Identifiant technique généré par le système si nécessaire. |
| Date de péremption | Date limite d’utilisation ou de vente. |
| Date de réception | Date d’entrée physique dans l’entrepôt. |
| Fournisseur | Fournisseur du lot. |
| Commande fournisseur | Commande d’achat associée. |
| Mode de transport | Bateau, avion, express, terrestre. |
| Statut qualité | En quarantaine, libéré, non conforme, périmé, détruit. |
| Prix de revient unitaire | Coût réel du lot après intégration des frais logistiques. |
| Quantité reçue | Quantité totale reçue. |
| Quantité restante | Quantité actuellement disponible. |

**Règle importante :**  
Un même produit peut exister dans plusieurs lots différents au même moment.

Exemple :

| Produit | Lot | Péremption | Quantité |
|---|---|---|---|
| France Lait 1er âge 400g | LOT-A123 | 10/2026 | 480 |
| France Lait 1er âge 400g | LOT-B456 | 02/2027 | 720 |

---

### 2.3.3 Emplacement

L’emplacement représente la position physique du produit dans l’entrepôt.

Exemple de structure possible :

```text
MAGASIN-01
├── ZONE-A
│   ├── ALLEE-01
│   │   ├── RACK-01
│   │   │   ├── NIVEAU-01
│   │   │   └── NIVEAU-02
│   │   └── RACK-02
│   └── ALLEE-02
├── ZONE-B
├── ZONE-FROID
├── ZONE-QUARANTAINE
└── ZONE-PERIMES
```

Un emplacement peut être de type :

| Type d’emplacement | Usage |
|---|---|
| Réception | Zone temporaire où les produits viennent d’arriver. |
| Quarantaine | Produits en attente de validation qualité. |
| Stockage | Emplacement principal de stockage. |
| Picking | Emplacement facile d’accès pour préparation de commande. |
| Réserve | Stock en hauteur ou en réserve. |
| Chaîne du froid | Produits nécessitant une température contrôlée. |
| Produits périmés | Zone isolée pour produits expirés. |
| Produits détruits | Emplacement logique avant destruction physique. |
| Transit | Produits en cours de transfert. |

**Règle importante :**  
Un même lot peut être stocké à plusieurs emplacements.

Exemple :

| Lot | Emplacement | Quantité |
|---|---|---|
| LOT-A123 | ZONE-A / ALLEE-01 / RACK-01 / NIVEAU-01 | 300 |
| LOT-A123 | ZONE-A / ALLEE-01 / RACK-02 / NIVEAU-03 | 180 |

---

### 2.3.4 Conditionnement et unités

Les fichiers produits montrent des conditionnements variables :

| Produit | Conditionnement |
|---|---|
| France Lait 1er âge 400g | carton/12 |
| France Lait 1er âge 900g | carton/6 |
| Pommade Maïa 100 ml | carton/72 |
| Pommade Maïa 250 ml | carton/48 |
| Strick Out Gel 30g | carton/200 |
| ALLERGICA 10MG CPR B/30 | carton/54 |
| Effermol inj perf 100ml | carton/50 |

Le système doit donc gérer plusieurs niveaux d’unités :

| Niveau | Exemple |
|---|---|
| Unité de base | La boîte, le flacon, le tube, la plaquette. |
| Carton | Regroupement de plusieurs unités. |
| Palette | Regroupement de plusieurs cartons, si nécessaire. |
| Colis express | Cas spécifique pour réception express. |

**Exemple :**

Produit : `France Lait 1er âge 400g`  
Conditionnement : `carton/12`

Si le magasin reçoit 40 cartons :

```text
Quantité reçue en cartons = 40
Quantité reçue en unités de base = 40 × 12 = 480 boîtes
```

Le système doit toujours enregistrer :

1. La quantité en conditionnement d’origine.
2. La quantité en unité de base.

Cela est indispensable car le fichier PRD mentionne :

> Si un carton a 40 produits, on enregistre 40 produits, mais on garde aussi que c’est venu dans des cartons.

---

## 2.4 Règles de gestion des stocks

### 2.4.1 Toute entrée en stock doit être tracée

Une entrée en stock peut provenir de :

| Source | Exemple |
|---|---|
| Commande fournisseur internationale | Conteneur France Lait reçu par bateau. |
| Commande fournisseur locale | Achat chez DEO GRATIAS PHARMA. |
| Retour client | Produit retourné par LABOREX TOGO ou UBIPHARM. |
| Ajustement inventaire | Correction après comptage physique. |
| Transfert entrant | Produit venant d’un autre magasin ou dépôt. |
| Régularisation | Correction d’erreur de saisie. |

Chaque entrée doit contenir :

- Produit.
- Lot.
- Date de péremption.
- Quantité.
- Emplacement cible.
- Utilisateur ayant effectué l’opération.
- Date et heure.
- Document source : commande, bon de livraison, inventaire, retour.
- Motif si ajustement.

---

### 2.4.2 Toute sortie de stock doit être tracée

Une sortie de stock peut provenir de :

| Source | Exemple |
|---|---|
| Commande client | Vente à LABOREX TOGO, CAMEG, TEDIS PHARMA, UBIPHARM. |
| Destruction | Produit périmé ou endommagé. |
| Perte | Produit perdu ou non retrouvé. |
| Ajustement inventaire | Correction après comptage. |
| Transfert sortant | Produit déplacé vers un autre emplacement ou dépôt. |
| Échantillon | Produit utilisé pour démonstration ou contrôle. |

Chaque sortie doit :

- Décrémenter la quantité du lot concerné.
- Mettre à jour l’emplacement concerné.
- Enregistrer l’utilisateur.
- Enregistrer la date.
- Enregistrer le motif.
- Empêcher une sortie si le stock disponible est insuffisant.

---

### 2.4.3 Gestion du stock disponible

Le système doit distinguer plusieurs quantités :

| Quantité | Définition |
|---|---|
| Stock physique | Quantité réellement présente dans l’entrepôt. |
| Stock réservé | Quantité réservée pour des commandes clients non encore livrées. |
| Stock disponible | Stock physique moins stock réservé. |
| Stock en quarantaine | Quantité non vendable. |
| Stock périmé | Quantité expirée, non vendable. |
| Stock attendu | Quantité commandée chez fournisseur mais non reçue. |
| Stock en transit | Quantité reçue mais pas encore mise en emplacement définitif. |

Formule de base :

```text
Stock disponible = Stock physique - Stock réservé - Stock bloqué
```

Exemple :

| Produit | Stock physique | Réservé | Quarantaine | Périmé | Disponible |
|---|---:|---:|---:|---:|---:|
| France Lait 1er âge 400g | 1200 | 240 | 0 | 0 | 960 |

---

### 2.4.4 Règle de sortie : FEFO par défaut

Dans le domaine pharmaceutique, la règle recommandée est :

```text
FEFO = First Expired, First Out
```

Cela signifie que le lot dont la date de péremption est la plus proche doit être proposé en premier lors d’une sortie.

Exemple :

| Lot | Date de péremption | Quantité disponible |
|---|---|---:|
| LOT-A | 30/09/2026 | 200 |
| LOT-B | 31/12/2026 | 500 |
| LOT-C | 30/06/2027 | 300 |

Si une commande client demande 150 unités de `France Lait 1er âge 400g`, le système doit proposer :

```text
LOT-A : 150 unités
```

Le système peut fonctionner de deux manières :

#### Option recommandée : Allocation automatique FEFO

Le backend sélectionne automatiquement les lots selon :

1. Date de péremption ascendante.
2. Emplacement prioritaire.
3. Statut libéré.
4. Quantité disponible.

#### Option alternative : Sélection manuelle

Le magasinier peut choisir manuellement un lot, mais seulement si :

- le lot n’est pas périmé,
- le lot n’est pas en quarantaine,
- la quantité est suffisante,
- l’action est journalisée,
- un motif peut être demandé si le lot choisi n’est pas le premier FEFO.

---

### 2.4.5 Gestion des dates de péremption

Le système doit gérer plusieurs statuts de péremption :

| Statut | Règle |
|---|---|
| Valide | Date de péremption future. |
| Proche péremption | Date dans un délai configurable : 30, 60, 90, 120 jours. |
| Périmé | Date dépassée. |
| Bientôt périmé chez client | Optionnel : anticipation si produit déjà livré. |

Règles :

1. Un produit périmé ne peut pas être vendu.
2. Un produit périmé peut être déplacé vers une zone `PERIMES`.
3. Un produit périmé peut être détruit via un mouvement de type `DESTRUCTION`.
4. Une alerte doit être générée avant péremption.
5. Les seuils doivent être configurables par catégorie ou par produit.

Exemple de seuils :

| Catégorie | Alerte si péremption dans |
|---|---:|
| Médicament | 90 jours |
| Produit infantile | 120 jours |
| Complément alimentaire | 90 jours |
| Réactif de laboratoire | 60 jours |
| Cosmétique | 90 jours |

---

### 2.4.6 Gestion de la quarantaine

Certains produits reçus peuvent ne pas être immédiatement disponibles à la vente.

Cas possibles :

- Contrôle qualité en attente.
- Produits endommagés pendant le transport.
- Écart de quantité.
- Température non respectée pour produits sensibles.
- Documents fournisseurs manquants.
- Doute sur authenticité ou lot.

Statuts possibles d’un lot :

| Statut | Effet |
|---|---|
| En réception | Produit reçu mais pas encore rangé. |
| En quarantaine | Produit non vendable. |
| Libéré | Produit vendable. |
| Non conforme | Produit bloqué. |
| Périmé | Produit non vendable. |
| Détruit | Produit sorti définitivement du stock vendable. |

Règle backend :

```text
Seuls les lots avec statut = Libéré peuvent être proposés à la vente.
```

---

### 2.4.7 Gestion des retours clients

Les retours clients doivent être gérés avec prudence.

Un retour peut être :

| Type | Règle |
|---|---|
| Retour acceptant remise en stock | Produit intact, lot valide, délai de retour respecté. |
| Retour en quarantaine | Produit à contrôler. |
| Retour refusé | Produit non réintégré. |
| Retour périmé | Produit à détruire. |
| Retour endommagé | Produit à détruire ou à régulariser. |

Un retour client doit toujours être lié à :

- la commande client d’origine,
- la ligne de commande d’origine,
- au lot d’origine si possible,
- au client,
- à la quantité retournée,
- au motif.

---

## 2.5 Gestion des emplacements

### 2.5.1 Adressage des emplacements

Chaque emplacement doit avoir un code unique.

Exemples :

```text
REC-01
QUAR-01
A-01-01-01
A-01-01-02
B-02-03-01
FROID-01
PERIM-01
DESTR-01
```

Interprétation possible :

```text
ZONE-ALLEE-RACK-NIVEAU-POSITION
```

Exemple :

```text
A-01-03-02-01
```

| Segment | Signification |
|---|---|
| A | Zone A |
| 01 | Allée 1 |
| 03 | Rack 3 |
| 02 | Niveau 2 |
| 01 | Position 1 |

---

### 2.5.2 Affectation d’un produit à un emplacement

Le système doit permettre :

1. Affectation manuelle par le magasinier.
2. Affectation par scan de code-barres.
3. Affectation suggérée par le système.
4. Affectation par défaut selon catégorie.
5. Affectation par zone température contrôlée si nécessaire.

Exemples de règles :

| Catégorie | Zone suggérée |
|---|---|
| Produit infantile | Zone sèche |
| Médicament | Zone pharma |
| Réactif de laboratoire | Zone labo / froid si requis |
| Cosmétique | Zone cosmétique |
| Insecticide | Zone spécifique |
| Produits périmés | Zone périmés |

---

### 2.5.3 Règles d’emplacement

Le backend doit pouvoir gérer :

| Règle | Description |
|---|---|
| Emplacement actif/inactif | Un emplacement peut être désactivé. |
| Emplacement verrouillé | Impossible d’y faire des mouvements. |
| Capacité maximale | Optionnel : limite de quantité ou volume. |
| Produit autorisé | Optionnel : certains emplacements réservés à certaines catégories. |
| Emplacement par défaut | Utilisé pour réception ou picking. |
| Emplacement de quarantaine | Obligatoire pour produits bloqués. |
| Emplacement de péremption | Obligatoire pour produits périmés. |

---

## 2.6 Gestion des mouvements de stock

### 2.6.1 Types de mouvements

Le système doit gérer au minimum les types suivants :

| Type de mouvement | Sens | Description |
|---|---|---|
| Réception fournisseur | Entrée | Arrivée d’une commande fournisseur. |
| Mise en stock | Entrée interne | Passage de réception à emplacement définitif. |
| Transfert | Interne | Déplacement entre deux emplacements. |
| Vente | Sortie | Sortie liée à une commande client. |
| Retour client | Entrée | Réintégration ou quarantaine. |
| Ajustement positif | Entrée | Correction inventaire positive. |
| Ajustement négatif | Sortie | Correction inventaire négative. |
| Destruction | Sortie | Produit détruit. |
| Perte | Sortie | Produit perdu. |
| Échantillon | Sortie | Produit utilisé comme échantillon. |
| Quarantaine | Blocage | Produit rendu non disponible. |
| Libération | Déblocage | Produit rendu disponible. |

---

### 2.6.2 Structure d’un mouvement

Chaque mouvement doit enregistrer :

| Champ | Description |
|---|---|
| Type | Réception, vente, transfert, ajustement, etc. |
| Date | Date du mouvement. |
| Utilisateur | Personne ayant effectué l’action. |
| Produit | Produit concerné. |
| Lot | Lot concerné. |
| Quantité | Quantité en unité de base. |
| Conditionnement | Quantité en cartons si applicable. |
| Emplacement source | Pour sortie ou transfert. |
| Emplacement destination | Pour entrée ou transfert. |
| Document source | Commande fournisseur, commande client, inventaire, retour. |
| Motif | Obligatoire pour ajustements, pertes, destructions. |
| Statut | Brouillon, validé, annulé. |

---

### 2.6.3 Interdictions importantes

Le backend doit refuser :

1. Une sortie supérieure à la quantité disponible.
2. Un mouvement sur lot périmé sauf déplacement vers zone périmé ou destruction.
3. Un mouvement sur lot en quarantaine sauf transfert vers quarantaine ou libération.
4. Un mouvement sans lot.
5. Un mouvement sans utilisateur authentifié.
6. Un mouvement sans emplacement source ou destination selon le type.
7. Un ajustement sans motif.
8. Une quantité négative.

---

## 2.7 Gestion des réceptions

### 2.7.1 Réception d’une commande fournisseur

Une réception peut être liée à :

- un conteneur maritime,
- une expédition aérienne,
- un envoi express,
- un achat local,
- un transfert interne.

Informations de réception :

| Champ | Exemple |
|---|---|
| Numéro de réception | REC-2026-000123 |
| Commande fournisseur | PO-2026-000456 |
| Fournisseur | CONTINENTAL COMMODITIES |
| Mode de transport | Maritime, aérien, express, terrestre |
| Numéro de conteneur | Conteneur 4 |
| BL / LTA / Document | Référence transport |
| Date de réception | 28/08/2026 |
| Statut | En cours, validée, close |

---

### 2.7.2 Réception par ligne produit

Chaque ligne reçue doit contenir :

| Champ | Exemple |
|---|---|
| Produit | France Lait 1er âge 400g |
| Quantité commandée | 100 cartons |
| Quantité reçue | 98 cartons |
| Quantité en unités | 1176 boîtes |
| Numéro de lot | LOT123456 |
| Date de péremption | 30/06/2027 |
| État | Conforme, manquant, endommagé, excédent |
| Emplacement cible | REC-01 ou A-01-02-01 |
| Mode de transport | Maritime |

---

### 2.7.3 Gestion des écarts de réception

Le système doit gérer :

| Écart | Règle |
|---|---|
| Quantité reçue inférieure | Statut partiellement reçu, reste à recevoir. |
| Quantité reçue supérieure | Demander validation, possibilité de refus ou régularisation. |
| Produit non commandé | Alerte et possibilité de refus ou réception exceptionnelle. |
| Produit endommagé | Mise en quarantaine ou refus. |
| Lot différent | Validation obligatoire. |
| Date de péremption courte | Alerte et validation responsable. |

---

## 2.8 Gestion des inventaires

### 2.8.1 Types d’inventaire

Le système doit permettre :

| Type | Description |
|---|---|
| Inventaire complet | Comptage de tout l’entrepôt. |
| Inventaire partiel | Comptage d’une zone, d’un rack, d’une catégorie. |
| Inventaire cyclique | Comptage régulier de certains produits. |
| Inventaire par lot | Comptage d’un lot spécifique. |
| Inventaire produit | Comptage d’un produit spécifique. |
| Inventaire emplacement | Comptage de tout ce qui se trouve dans un emplacement. |

---

### 2.8.2 Déroulement d’un inventaire

Étapes recommandées :

1. Création d’une session d’inventaire.
2. Sélection des produits, lots ou emplacements.
3. Gel des mouvements sur le périmètre concerné.
4. Saisie des quantités comptées.
5. Comparaison avec les quantités système.
6. Génération des écarts.
7. Validation des écarts par responsable.
8. Création automatique des ajustements.
9. Clôture de la session.
10. Historisation complète.

---

### 2.8.3 Écarts d’inventaire

| Type d’écart | Action possible |
|---|---|
| Quantité comptée > Quantité système | Ajustement positif avec motif. |
| Quantité comptée < Quantité système | Ajustement négatif avec motif. |
| Lot absent physiquement | Blocage ou perte. |
| Lot présent mais non prévu | Analyse puis intégration ou quarantaine. |
| Péremption différente | Correction de donnée lot avec audit. |

---

## 2.9 Gestion de la préparation de commande

### 2.9.1 Réservation de stock

Lorsqu’une commande client est validée, le système doit pouvoir réserver le stock.

Exemple :

| Produit | Commandé | Réservation |
|---|---:|---:|
| France Lait 1er âge 400g | 120 | 120 |
| Pommade Maïa 100 ml | 50 | 50 |

La réservation doit :

- être liée à la commande client,
- être liée à un lot,
- être liée à un emplacement,
- réduire le stock disponible,
- ne pas réduire immédiatement le stock physique tant que la sortie n’est pas validée.

---

### 2.9.2 Allocation FEFO automatique

Le backend doit proposer une allocation selon :

1. Date de péremption la plus proche.
2. Statut libéré.
3. Emplacement picking prioritaire.
4. Quantité disponible.
5. Emplacement le plus proche ou le plus logique.

Exemple d’algorithme :

```text
1. Récupérer tous les lots disponibles pour le produit.
2. Exclure lots périmés.
3. Exclure lots en quarantaine.
4. Exclure stock déjà réservé.
5. Trier par date de péremption ascendante.
6. Trier par emplacement prioritaire.
7. Allouer jusqu’à satisfaire la quantité demandée.
8. Si stock insuffisant, retourner erreur ou statut partiel.
```

---

### 2.9.3 Préparation physique

Le frontend doit permettre au magasinier de :

- scanner le produit,
- scanner le lot,
- scanner l’emplacement,
- confirmer la quantité,
- signaler un manquant,
- signaler un produit endommagé,
- imprimer un bon de préparation.

---

## 2.10 Gestion des étiquettes et codes-barres

Le système doit prévoir l’impression d’étiquettes pour :

1. Le produit.
2. Le lot.
3. La date de péremption.
4. L’emplacement.
5. Le carton.
6. La palette éventuelle.

Informations recommandées sur l’étiquette lot :

```text
Produit : France Lait 1er âge 400g
Code interne : PROD-000123
Lot : LOT123456
Péremption : 06/2027
Quantité : 12
Emplacement : A-01-02-03
Fournisseur : CONTINENTAL COMMODITIES
Réception : REC-2026-000123
```

Le frontend React devra intégrer une logique de :

- scan de code-barres,
- scan de QR code,
- génération de PDF d’étiquettes,
- prévisualisation d’impression.

---

## 2.11 Alertes et notifications

Le système doit générer des alertes pour :

| Alerte | Déclencheur |
|---|---|
| Stock faible | Quantité disponible inférieure au seuil. |
| Rupture | Quantité disponible = 0. |
| Péremption proche | Lot dans 30, 60, 90 ou 120 jours. |
| Produit périmé | Date de péremption dépassée. |
| Stock en quarantaine | Lots bloqués depuis trop longtemps. |
| Réception en retard | Commande fournisseur non reçue après date prévue. |
| Écart inventaire | Différence non justifiée. |
| Mouvement anormal | Sortie importante ou répétée. |
| Emplacement critique | Emplacement proche de saturation, si capacité gérée. |
| Produit à rotation lente | Produit non vendu depuis X jours. |

Ces alertes peuvent être :

- affichées dans le tableau de bord React,
- envoyées via SignalR,
- envoyées par email via `INotificationService`,
- générées par un job Hangfire.

---

## 2.12 Impact sur le Frontend React

Le frontend doit fournir des interfaces claires, rapides et orientées opération terrain.

### 2.12.1 Pages principales à prévoir

#### 1. Dashboard Stock

Vue globale avec :

- nombre de produits,
- valeur du stock,
- stock faible,
- lots proches péremption,
- lots périmés,
- produits en quarantaine,
- dernières réceptions,
- dernières sorties.

---

#### 2. Liste des produits

Table avec filtres :

- désignation,
- catégorie,
- fournisseur,
- forme,
- classe thérapeutique,
- stock disponible,
- stock réservé,
- seuil minimum,
- statut.

Actions :

- voir détail produit,
- voir lots,
- voir mouvements,
- créer ajustement,
- voir historique.

---

#### 3. Détail produit

Afficher :

- informations générales,
- conditionnements,
- stock par lot,
- stock par emplacement,
- mouvements récents,
- alertes,
- prix de vente,
- prix de revient,
- seuil de réapprovisionnement.

---

#### 4. Liste des lots

Table filtrable par :

- produit,
- numéro de lot,
- fournisseur,
- date de péremption,
- statut,
- emplacement,
- quantité restante.

Badges visuels :

- vert : disponible,
- orange : proche péremption,
- rouge : périmé,
- gris : quarantaine,
- bleu : réception en cours.

---

#### 5. Réception fournisseur

Écran de réception avec :

- sélection de la commande fournisseur,
- affichage des lignes attendues,
- saisie des quantités reçues,
- saisie des lots,
- saisie des dates de péremption,
- choix des emplacements,
- gestion des écarts,
- validation finale.

---

#### 6. Mise en stock

Écran permettant de déplacer les produits de la zone réception vers un emplacement.

Fonctions :

- scan produit,
- scan lot,
- scan emplacement,
- saisie quantité,
- confirmation.

---

#### 7. Transfert interne

Écran pour déplacer un lot :

- d’un emplacement à un autre,
- d’une zone à une autre,
- d’un statut à un autre.

---

#### 8. Préparation de commande

Écran de picking :

- liste des commandes à préparer,
- détail des lignes,
- lots proposés par FEFO,
- scan de validation,
- gestion des manquants,
- clôture préparation.

---

#### 9. Inventaire

Écran dédié :

- création session,
- sélection zone/produit/lot,
- saisie comptage,
- comparaison,
- validation,
- ajustement.

---

#### 10. Alertes et péremptions

Vue dédiée aux lots :

- périmés,
- proches péremption,
- stock faible,
- quarantaine,
- rotation lente.

---

### 2.12.2 Composants React recommandés

| Composant | Usage |
|---|---|
| DataGrid | Listes de produits, lots, mouvements. |
| Modal dynamique | Formulaires de réception, ajustement, transfert. |
| Toast | Notifications de succès/erreur. |
| Badge statut | Lot disponible, quarantaine, périmé. |
| Scanner input | Lecture code-barres ou QR code. |
| DatePicker | Saisie des dates de péremption. |
| Autocomplete | Recherche produit, emplacement, client, fournisseur. |
| Timeline | Historique des mouvements. |
| Kanban | Préparation de commande : à préparer, en cours, prêt, livré. |
| Chart | Rotation, péremptions, valeur stock. |
| PDF preview | Impression étiquettes, BL, fiches inventaire. |

---

### 2.12.3 Règles UX importantes

1. Les actions critiques doivent demander confirmation.
2. Les erreurs API doivent être affichées sous forme de toast.
3. Les champs numériques doivent accepter les saisies locales sans erreur.
4. Les quantités doivent être validées côté frontend avant envoi.
5. Les scans doivent remplir automatiquement les champs.
6. Les lots périmés doivent être visuellement bloqués.
7. Les emplacements doivent être recherchables par code.
8. Les mouvements doivent être historisés et consultables.
9. Les utilisateurs doivent voir uniquement ce que leur rôle autorise.
10. Les opérations de masse doivent afficher une barre de progression.

---

## 2.13 Impact sur le Backend .NET

Le backend doit implémenter les entités, services, repositories et contrôleurs nécessaires.

---

## 2.13.1 Entités principales à créer dans `LABMEDIS.Core`

Toutes les entités doivent hériter de `BaseEntity`.

#### `Product`

```csharp
public class Product : BaseEntity
{
    public string Designation { get; set; }
    public Guid CategoryId { get; set; }
    public string Form { get; set; }
    public string Dosage { get; set; }
    public string Packaging { get; set; }
    public string TherapeuticClass { get; set; }
    public string CipCode { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal? VatRate { get; set; }
    public int? MinStockThreshold { get; set; }
    public int? ManufacturingLeadTimeDays { get; set; }
    public int? DeliveryLeadTimeDays { get; set; }

    public Category Category { get; set; }
    public Supplier Supplier { get; set; }
    public ICollection<StockLot> StockLots { get; set; }
}
```

---

#### `StockLot`

```csharp
public class StockLot : BaseEntity
{
    public Guid ProductId { get; set; }
    public string SupplierBatchNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public DateOnly ReceptionDate { get; set; }
    public LotStatus Status { get; set; }
    public TransportMode? TransportMode { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public decimal UnitCost { get; set; }

    public Product Product { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public ICollection<StockLotLocation> StockLotLocations { get; set; }
    public ICollection<StockMovementLine> StockMovementLines { get; set; }
}
```

---

#### `StockLotLocation`

```csharp
public class StockLotLocation : BaseEntity
{
    public Guid StockLotId { get; set; }
    public Guid StorageLocationId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }

    public StockLot StockLot { get; set; }
    public StorageLocation StorageLocation { get; set; }
}
```

---

#### `StorageLocation`

```csharp
public class StorageLocation : BaseEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    public LocationType Type { get; set; }
    public Guid? ParentLocationId { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public int? Capacity { get; set; }

    public StorageLocation ParentLocation { get; set; }
    public ICollection<StockLotLocation> StockLotLocations { get; set; }
}
```

---

#### `StockMovement`

```csharp
public class StockMovement : BaseEntity
{
    public string Reference { get; set; }
    public StockMovementType Type { get; set; }
    public DateTime MovementDate { get; set; }
    public string UserId { get; set; }
    public string Reason { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string SourceDocumentType { get; set; }
    public StockMovementStatus Status { get; set; }

    public ICollection<StockMovementLine> Lines { get; set; }
}
```

---

#### `StockMovementLine`

```csharp
public class StockMovementLine : BaseEntity
{
    public Guid StockMovementId { get; set; }
    public Guid ProductId { get; set; }
    public Guid StockLotId { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? DestinationLocationId { get; set; }
    public int Quantity { get; set; }

    public StockMovement StockMovement { get; set; }
    public Product Product { get; set; }
    public StockLot StockLot { get; set; }
    public StorageLocation SourceLocation { get; set; }
    public StorageLocation DestinationLocation { get; set; }
}
```

---

#### `InventorySession`

```csharp
public class InventorySession : BaseEntity
{
    public string Reference { get; set; }
    public InventoryType Type { get; set; }
    public InventoryStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string UserId { get; set; }
    public string Comments { get; set; }

    public ICollection<InventoryCount> Counts { get; set; }
}
```

---

#### `InventoryCount`

```csharp
public class InventoryCount : BaseEntity
{
    public Guid InventorySessionId { get; set; }
    public Guid ProductId { get; set; }
    public Guid StockLotId { get; set; }
    public Guid StorageLocationId { get; set; }
    public int SystemQuantity { get; set; }
    public int CountedQuantity { get; set; }
    public int Difference => CountedQuantity - SystemQuantity;
    public string AdjustmentReason { get; set; }
}
```

---

## 2.13.2 Enums recommandés

```csharp
public enum LotStatus
{
    EnReception = 0,
    Quarantaine = 1,
    Libere = 2,
    NonConforme = 3,
    Perime = 4,
    Detruit = 5
}
```

```csharp
public enum LocationType
{
    Reception = 0,
    Quarantaine = 1,
    Stockage = 2,
    Picking = 3,
    Reserve = 4,
    ChaineDuFroid = 5,
    Perimes = 6,
    Destruction = 7,
    Transit = 8
}
```

```csharp
public enum StockMovementType
{
    ReceptionFournisseur = 0,
    MiseEnStock = 1,
    Transfert = 2,
    Vente = 3,
    RetourClient = 4,
    AjustementPositif = 5,
    AjustementNegatif = 6,
    Destruction = 7,
    Perte = 8,
    Echantillon = 9,
    Quarantaine = 10,
    Liberation = 11
}
```

```csharp
public enum TransportMode
{
    Maritime = 0,
    Aerien = 1,
    Express = 2,
    Terrestre = 3
}
```

---

## 2.13.3 Repositories à prévoir

Selon l’architecture imposée :

```csharp
public interface IProductRepository : IBaseRepository<Product>
{
    Task<Product?> GetProductWithStockAsync(Guid productId);
}
```

```csharp
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetProductWithStockAsync(Guid productId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.StockLots)
                .ThenInclude(l => l.StockLotLocations)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
    }
}
```

```csharp
public interface IStockLotRepository : IBaseRepository<StockLot>
{
    Task<List<StockLot>> GetAvailableLotsForProductAsync(Guid productId);
    Task<StockLot?> GetLotWithLocationsAsync(Guid lotId);
}
```

```csharp
public class StockLotRepository : BaseRepository<StockLot>, IStockLotRepository
{
    public StockLotRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<StockLot>> GetAvailableLotsForProductAsync(Guid productId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(l => l.ProductId == productId
                && l.Status == LotStatus.Libere
                && !l.IsDeleted)
            .Include(l => l.StockLotLocations)
                .ThenInclude(sl => sl.StorageLocation)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync();
    }

    public async Task<StockLot?> GetLotWithLocationsAsync(Guid lotId)
    {
        return await _dbSet
            .Include(l => l.StockLotLocations)
                .ThenInclude(sl => sl.StorageLocation)
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == lotId && !l.IsDeleted);
    }
}
```

---

## 2.13.4 Services à prévoir

Services principaux :

```csharp
IProductService
IStockLotService
IStorageLocationService
IStockMovementService
IStockReceptionService
IStockAllocationService
IInventoryService
IStockAlertService
```

---

## 2.13.5 Exemple de service avec héritage obligatoire

```csharp
public interface IStockLotService : IStockLotRepository
{
    Task<List<StockLotResponse>> GetAvailableLotsAsync(Guid productId);
    Task<StockLotResponse> CreateLotAsync(CreateStockLotRequest request);
}
```

```csharp
public class StockLotService : StockLotRepository, IStockLotService
{
    private readonly ILoggerManager _logger;

    public StockLotService(AppDbContext context, ILoggerManager logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<List<StockLotResponse>> GetAvailableLotsAsync(Guid productId)
    {
        var lots = await GetAvailableLotsForProductAsync(productId);

        return lots.Select(l => new StockLotResponse(l)).ToList();
    }

    public async Task<StockLotResponse> CreateLotAsync(CreateStockLotRequest request)
    {
        var lot = request.ToStockLot();

        await InsertAsync(lot);
        await SaveAsync();

        return new StockLotResponse(lot);
    }
}
```

---

## 2.13.6 DTO Requests avec champs numériques en string

Conformément à la règle d’or, les quantités et valeurs décimales doivent être en `string` dans les Requests.

```csharp
public class CreateStockLotRequest
{
    [SwaggerSchema(Description = "Identifiant du produit")]
    public Guid ProductId { get; set; }

    [SwaggerSchema(Description = "Numéro de lot fournisseur")]
    public string SupplierBatchNumber { get; set; }

    [SwaggerSchema(Description = "Date de péremption au format yyyy-MM-dd")]
    public string ExpiryDate { get; set; }

    [SwaggerSchema(Description = "Quantité reçue en unités de base")]
    public string Quantity { get; set; }

    [SwaggerSchema(Description = "Quantité reçue en cartons")]
    public string CartonQuantity { get; set; }

    [SwaggerSchema(Description = "Statut initial du lot")]
    public string Status { get; set; }

    public StockLot ToStockLot()
    {
        return new StockLot
        {
            ProductId = ProductId,
            SupplierBatchNumber = SupplierBatchNumber,
            ExpiryDate = DateOnly.ParseExact(ExpiryDate, "yyyy-MM-dd"),
            Status = Enum.Parse<LotStatus>(Status),
            ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }
}
```

---

## 2.13.7 DTO Responses avec constructeur depuis entité

```csharp
public class StockLotResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductDesignation { get; set; }
    public string SupplierBatchNumber { get; set; }
    public string ExpiryDate { get; set; }
    public string Status { get; set; }
    public int AvailableQuantity { get; set; }

    public StockLotResponse(StockLot entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        SupplierBatchNumber = entity.SupplierBatchNumber;
        ExpiryDate = entity.ExpiryDate.ToString("yyyy-MM-dd");
        Status = entity.Status.ToString();

        AvailableQuantity = entity.StockLotLocations?
            .Where(x => !x.IsDeleted)
            .Sum(x => x.Quantity - x.ReservedQuantity) ?? 0;
    }
}
```

---

## 2.13.8 Endpoints API recommandés

### Produits et stock

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/products` | Liste des produits avec stock. |
| GET | `/api/products/{id}` | Détail produit. |
| GET | `/api/products/{id}/stock` | Stock du produit par lot et emplacement. |
| GET | `/api/products/{id}/movements` | Mouvements du produit. |
| GET | `/api/products/{id}/lots` | Lots du produit. |

---

### Lots

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/stock-lots` | Liste des lots. |
| GET | `/api/stock-lots/{id}` | Détail d’un lot. |
| GET | `/api/stock-lots/expiring` | Lots proches péremption. |
| POST | `/api/stock-lots` | Créer un lot. |
| PUT | `/api/stock-lots/{id}/status` | Changer statut : quarantaine, libération, etc. |
| PUT | `/api/stock-lots/{id}/location` | Déplacer un lot vers un emplacement. |

---

### Mouvements

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/stock-movements` | Liste des mouvements. |
| GET | `/api/stock-movements/{id}` | Détail mouvement. |
| POST | `/api/stock-movements/transfer` | Transfert interne. |
| POST | `/api/stock-movements/adjustment` | Ajustement positif/négatif. |
| POST | `/api/stock-movements/destruction` | Destruction. |
| POST | `/api/stock-movements/loss` | Perte. |

---

### Réceptions

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/stock-receptions` | Liste des réceptions. |
| GET | `/api/stock-receptions/{id}` | Détail réception. |
| POST | `/api/stock-receptions` | Créer une réception. |
| POST | `/api/stock-receptions/{id}/validate` | Valider réception. |
| POST | `/api/stock-receptions/{id}/put-away` | Mise en stock. |

---

### Inventaires

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/inventories` | Liste sessions inventaire. |
| GET | `/api/inventories/{id}` | Détail inventaire. |
| POST | `/api/inventories` | Créer session inventaire. |
| POST | `/api/inventories/{id}/counts` | Saisir comptages. |
| POST | `/api/inventories/{id}/validate` | Valider inventaire. |
| POST | `/api/inventories/{id}/close` | Clôturer inventaire. |

---

## 2.13.9 Exemple de contrôleur

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockLotsController : ControllerBase
{
    private readonly IStockLotService _stockLotService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public StockLotsController(
        IStockLotService stockLotService,
        ILoggerManager logger,
        IUserService userService)
    {
        _stockLotService = stockLotService;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetAvailableLots(Guid productId)
    {
        var user = await _userService.GetCurrentUserAsync(User);

        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début GetAvailableLots | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try
        {
            var result = await _stockLotService.GetAvailableLotsAsync(productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec GetAvailableLots : {ex.Message} | IP: {Request.GetIp()}");

            return BadRequest(new
            {
                message = "Impossible de récupérer les lots disponibles."
            });
        }
    }
}
```

---

## 2.14 Logique d’allocation FEFO côté backend

Le service d’allocation doit proposer les lots à sortir selon FEFO.

Exemple :

```csharp
public class StockAllocationService : StockLotRepository, IStockAllocationService
{
    public async Task<List<StockAllocationResult>> AllocateStockAsync(Guid productId, int requestedQuantity)
    {
        var lots = await GetAvailableLotsForProductAsync(productId);

        var result = new List<StockAllocationResult>();
        int remainingQuantity = requestedQuantity;

        foreach (var lot in lots)
        {
            if (remainingQuantity <= 0)
                break;

            var availableQuantity = lot.StockLotLocations
                .Where(x => !x.IsDeleted)
                .Sum(x => x.Quantity - x.ReservedQuantity);

            if (availableQuantity <= 0)
                continue;

            int allocatedQuantity = Math.Min(availableQuantity, remainingQuantity);

            result.Add(new StockAllocationResult
            {
                StockLotId = lot.Id,
                LotNumber = lot.SupplierBatchNumber,
                ExpiryDate = lot.ExpiryDate,
                AllocatedQuantity = allocatedQuantity
            });

            remainingQuantity -= allocatedQuantity;
        }

        if (remainingQuantity > 0)
        {
            throw new BusinessException($"Stock insuffisant pour le produit demandé. Quantité manquante : {remainingQuantity}");
        }

        return result;
    }
}
```

---

## 2.15 Jobs Hangfire à prévoir

Le module stock doit inclure des traitements différés ou planifiés.

### Job 1 : Alerte péremption

```text
StockExpiryAlertJob
```

Fréquence recommandée : quotidienne.

Actions :

1. Scanner tous les lots libérés.
2. Identifier les lots dont la péremption est proche.
3. Créer une notification.
4. Envoyer une alerte SignalR.
5. Envoyer éventuellement un email.

---

### Job 2 : Stock faible

```text
LowStockAlertJob
```

Actions :

1. Calculer le stock disponible par produit.
2. Comparer avec le seuil minimum.
3. Créer une alerte.
4. Proposer une commande de réapprovisionnement.

---

### Job 3 : Rotation lente

```text
SlowMovingStockJob
```

Actions :

1. Identifier les produits sans mouvement depuis 30, 60 ou 90 jours.
2. Calculer la valeur immobilisée.
3. Générer un rapport.

---

### Job 4 : Clôture automatique des lots périmés

```text
ExpiredLotBlockingJob
```

Actions :

1. Rechercher les lots dont la date de péremption est dépassée.
2. Passer leur statut à `Perime`.
3. Empêcher leur allocation.
4. Créer une alerte.

---

## 2.16 Notifications SignalR à prévoir

Le backend doit pousser en temps réel :

| Événement | Description |
|---|---|
| StockLotCreated | Un nouveau lot a été créé. |
| StockMovementValidated | Un mouvement a été validé. |
| LowStockDetected | Produit sous seuil. |
| ExpiringLotDetected | Lot proche péremption. |
| ExpiredLotDetected | Lot périmé. |
| ReceptionValidated | Réception validée. |
| InventoryDiscrepancyDetected | Écart inventaire détecté. |
| StockReservationCreated | Stock réservé pour commande. |

---

## 2.17 Règles de sécurité et audit

Toutes les opérations de stock doivent être auditées.

### Informations à journaliser

| Donnée | Obligatoire |
|---|---|
| Utilisateur | Oui |
| Date et heure | Oui |
| Action | Oui |
| Produit | Oui |
| Lot | Oui |
| Quantité | Oui |
| Emplacement | Oui |
| Motif | Oui pour ajustements, pertes, destructions |
| IP | Oui |
| UserAgent | Oui |
| Document source | Si applicable |

### Format de log imposé

Dans chaque action API :

```csharp
_logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début [NomAction] | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");
```

Dans le catch :

```csharp
_logger.LogError(ex, $"{user?.LastName} ... | Echec [NomAction] : {ex.Message} | IP: {Request.GetIp()}");
```

---

## 2.18 Règles de validation métier

Le backend doit rejeter les opérations invalides avec des messages clairs.

| Cas | Message recommandé |
|---|---|
| Quantité négative | `La quantité ne peut pas être négative.` |
| Quantité supérieure au stock | `La quantité demandée est supérieure au stock disponible.` |
| Lot périmé | `Ce lot est périmé. Il ne peut pas être utilisé pour une vente.` |
| Lot en quarantaine | `Ce lot est en quarantaine. Il doit être libéré avant utilisation.` |
| Emplacement invalide | `L’emplacement sélectionné est invalide ou inactif.` |
| Motif manquant | `Un motif est obligatoire pour ce type de mouvement.` |
| Date péremption invalide | `La date de péremption doit être future pour une réception normale.` |
| Produit introuvable | `Le produit demandé est introuvable.` |
| Lot introuvable | `Le lot demandé est introuvable.` |
| Ajustement non autorisé | `Votre profil ne permet pas de valider cet ajustement.` |

---

## 2.19 Points à valider avec LABMEDIS

Même si cette section est complète techniquement, certains points métier devront être confirmés avec le client avant développement définitif :

| Question | Impact |
|---|---|
| Un produit peut-il être vendu avant sa mise en stock définitive ? | Impact sur statut réception et stock disponible. |
| La règle FEFO doit-elle être strictement automatique ou modifiable ? | Impact sur allocation des lots. |
| Faut-il gérer plusieurs entrepôts physiques ? | Impact multi-magasins et transferts. |
| Faut-il gérer la chaîne du froid pour certains réactifs HORIBA ? | Impact emplacement et alertes température. |
| Les cartons peuvent-ils être ouverts et revendus à l’unité ? | Impact conversion carton vers unité. |
| Un carton peut-il contenir plusieurs produits différents ? | Impact réception et étiquetage. |
| Faut-il bloquer la vente d’un produit si péremption inférieure à X jours ? | Impact règles métier. |
| Les ajustements d’inventaire nécessitent-ils une validation à deux niveaux ? | Impact rôles et workflow. |
| Faut-il imprimer des étiquettes code-barres ou QR codes ? | Impact module impression. |
| Faut-il une interface mobile PDA pour le magasin ? | Impact design React responsive. |

---

## 2.20 Synthèse des exigences du module

Le module **Gestion Physique des Stocks & Entrepôt** doit permettre à LABMEDIS de :

1. Recevoir les produits par lot.
2. Enregistrer les numéros de lot et dates de péremption.
3. Stocker les produits dans des emplacements précis.
4. Gérer les conditionnements carton/boîte/flacon.
5. Suivre les quantités physiques, réservées et disponibles.
6. Appliquer FEFO par défaut.
7. Bloquer les lots périmés ou en quarantaine.
8. Tracer chaque mouvement de stock.
9. Réaliser des inventaires fiables.
10. Alerter sur stock faible, péremption et rotation lente.
11. Fournir au frontend React des interfaces simples, rapides et scannables.
12. Fournir au backend .NET des services robustes, audités et conformes à l’architecture imposée.

### USER
ecris moi le 3. Workflows Opérationnels & User Stories Détaillées

### ASSISTANT
# 3. 🔄 Workflows Opérationnels & User Stories Détaillées

Cette section décrit les processus opérationnels de **LABMEDIS** sous forme de workflows détaillés et de user stories exploitables par l’équipe de développement.

Elle couvre l’ensemble du cycle de vie métier :

1. Gestion des données de référence.
2. Achats internationaux.
3. Suivi logistique et transport.
4. Réception en entrepôt.
5. Contrôle qualité et mise en stock.
6. Gestion des lots et péremptions.
7. Ventes aux répartiteurs et structures de santé.
8. Préparation, livraison et facturation.
9. Retours clients et avoirs.
10. Anticipation des commandes et réapprovisionnement.
11. Inventaires et ajustements.
12. Pilotage financier et reporting.

---

## 3.1 Acteurs et rôles du système

Le système doit gérer plusieurs profils utilisateurs. Chaque profil aura des permissions spécifiques dans le frontend React et des rôles d’autorisation dans le backend .NET.

| Rôle | Description | Responsabilités principales |
|---|---|---|
| Administrateur | Gestion technique et paramétrage du système. | Utilisateurs, rôles, permissions, logs, configuration générale. |
| Direction / Manager | Pilotage stratégique. | Validation des prix, marges, commandes importantes, reporting. |
| Acheteur / Responsable import | Gestion des achats internationaux. | Création des commandes fournisseurs, suivi transport, devises, coûts. |
| Responsable logistique | Suivi des flux physiques. | Conteneurs, fret aérien, maritime, express, transit, réception. |
| Magasinier | Gestion physique de l’entrepôt. | Réception, mise en stock, emplacements, inventaire, préparation. |
| Responsable qualité | Validation des lots. | Quarantaine, libération, non-conformité, péremption. |
| Commercial / Vente | Relation clients. | Devis, commandes clients, prix, disponibilité, livraisons. |
| Comptable | Suivi financier. | Factures, avoirs, TVA, exports, validation des coûts. |
| Préparateur | Préparation physique des commandes. | Picking, scan, colisage, livraison. |

---

## 3.2 Macro-processus global LABMEDIS

Le flux global peut être résumé ainsi :

```text
Fabricant / Fournisseur international
        ↓
Commande d’achat LABMEDIS
        ↓
Transport : Bateau / Avion / Express / Terrestre
        ↓
Transit / Douane
        ↓
Réception entrepôt
        ↓
Contrôle qualité / Quarantaine
        ↓
Mise en stock par lot et emplacement
        ↓
Calcul du prix de revient réel
        ↓
Disponibilité à la vente
        ↓
Commande client : répartiteur, clinique, hôpital, pharmacie
        ↓
Préparation / Picking FEFO
        ↓
Livraison
        ↓
Facturation
        ↓
Suivi paiement / retours / avoirs
        ↓
Analyse : stock, marge, rotation, prévisions
```

---

## 3.3 Workflow 1 : Gestion des données de référence

### 3.3.1 Objectif

Avant toute opération d’achat, de stock ou de vente, le système doit disposer de données de référence fiables :

- produits,
- catégories,
- fournisseurs,
- clients,
- conditionnements,
- devises,
- taux de change,
- prix,
- taux de TVA,
- délais de fabrication,
- délais de livraison,
- seuils de stock.

---

### 3.3.2 Workflow

```text
Création ou modification d’une donnée de référence
        ↓
Contrôle des informations obligatoires
        ↓
Validation métier
        ↓
Enregistrement en base
        ↓
Historisation / Audit
        ↓
Disponibilité dans les modules Achats, Stock, Vente
```

---

### 3.3.3 Règles de gestion

| Donnée | Règle |
|---|---|
| Produit | Une désignation unique doit être définie. |
| Catégorie | Chaque produit doit appartenir à une catégorie. |
| Fournisseur | Un fournisseur doit avoir un pays et une devise par défaut. |
| Client | Un client doit avoir au minimum un nom et une ville. |
| Conditionnement | Le produit peut avoir plusieurs niveaux de conditionnement. |
| TVA | Le taux de TVA doit être défini par catégorie ou produit. |
| Devise | Les devises de référence sont EUR, USD et XOF. |
| Délais | Les délais de fabrication et livraison doivent être configurables. |

---

### 3.3.4 Exemples issus des données fournies

#### Produits

| Produit | Catégorie | Fournisseur |
|---|---|---|
| France Lait 1er âge 400g | Produit infantile | CONTINENTAL COMMODITIES |
| France Lait 2ème âge 900g | Produit infantile | CONTINENTAL COMMODITIES |
| Pommade Maïa 100 ml | Cosmétique | MAIA AFRICA SAS |
| ALLERGICA 10MG CPR B/30 | Médicament | GALPHARMA |
| B-PROTEI ALL 200g | Complément alimentaire | B&B LIFE SCIENCE |
| ABX PENTRA UREA CP | Réactifs de laboratoire | HORIBA |

#### Clients

| Client | Ville |
|---|---|
| CAMEG | Lomé |
| LABOREX TOGO | Lomé |
| TEDIS PHARMA TOGO | Lomé |
| UBIPHARM TOGO | Lomé |
| CHP ANEHO | Aného |
| CHR SOKODE | Sokodé |

#### Fournisseurs

| Fournisseur | Pays |
|---|---|
| HORIBA ABX SAS | France |
| Continental Commodities | France |
| GALPHARMA | Tunisie |
| IBERMA | Maroc |
| B&B LIFE SCIENCE | Inde |
| BIORESEARCH | Suisse |
| MAIA AFRICA SAS | Burkina Faso |
| DEO GRATIAS PHARMA | Togo |

---

### 3.3.5 User Stories

#### US-REF-01 : Créer un produit

**En tant que** responsable produit / administrateur,  
**je veux** créer une fiche produit complète,  
**afin de** pouvoir l’utiliser dans les achats, stocks et ventes.

**Critères d’acceptation :**

1. L’utilisateur peut saisir la désignation, la catégorie, la forme, le dosage, le conditionnement, la classe thérapeutique, le fournisseur, le code CIP, la TVA et les seuils.
2. Le système vérifie que la désignation est unique.
3. Le produit peut être actif ou inactif.
4. Le produit peut avoir plusieurs conditionnements.
5. Le produit peut être lié à un fournisseur principal.
6. La création est journalisée.
7. Le produit est immédiatement disponible dans les autres modules.

---

#### US-REF-02 : Créer un fournisseur

**En tant que** administrateur ou responsable achats,  
**je veux** enregistrer les fournisseurs internationaux et locaux,  
**afin de** rattacher les commandes d’achat et les produits.

**Critères d’acceptation :**

1. Les champs obligatoires sont : nom, pays, adresse, téléphone.
2. Le fournisseur peut avoir une devise par défaut : EUR, USD ou XOF.
3. Le fournisseur peut avoir un délai de fabrication moyen.
4. Le fournisseur peut avoir un délai de livraison moyen.
5. Le fournisseur peut être actif ou inactif.
6. Les modifications sont historisées.

---

#### US-REF-03 : Créer un client

**En tant que** commercial ou administrateur,  
**je veux** enregistrer les clients et répartiteurs,  
**afin de** leur affecter des commandes et des livraisons.

**Critères d’acceptation :**

1. Les informations minimales sont : nom, adresse, téléphone, ville.
2. Le client peut être typé : répartiteur, hôpital, clinique, pharmacie, boutique, autre.
3. Le client peut avoir des conditions commerciales spécifiques.
4. Le client peut être actif ou inactif.
5. Un client inactif ne peut pas recevoir de nouvelle commande.

---

#### US-REF-04 : Configurer les taux de TVA

**En tant que** responsable financier,  
**je veux** définir les taux de TVA par catégorie ou produit,  
**afin de** garantir la conformité fiscale des ventes.

**Critères d’acceptation :**

1. Les produits infantiles peuvent être taxés à 18%.
2. Les cosmétiques peuvent être taxés à 18%.
3. Les compléments alimentaires peuvent être taxés à 18%.
4. Les médicaments peuvent être exonérés, sauf règle contraire.
5. Le taux peut être surchargé au niveau produit.
6. Le système applique automatiquement la TVA lors de la vente.

---

## 3.4 Workflow 2 : Achat international

### 3.4.1 Objectif

Permettre à LABMEDIS de commander des produits auprès de fournisseurs internationaux, en gérant :

- les devises,
- les quantités,
- les conditionnements,
- les prix d’achat,
- les taux de change,
- les délais de fabrication,
- les délais de livraison,
- le mode de transport.

---

### 3.4.2 Workflow détaillé

```text
Expression du besoin
        ↓
Vérification du stock disponible
        ↓
Vérification du stock en transit
        ↓
Vérification des prévisions de rupture
        ↓
Sélection du fournisseur
        ↓
Création de la commande d’achat
        ↓
Ajout des lignes produits
        ↓
Saisie des prix d’achat en devise
        ↓
Saisie du taux de change
        ↓
Calcul du montant total
        ↓
Validation par responsable
        ↓
Envoi au fournisseur
        ↓
Suivi du statut de fabrication
        ↓
Confirmation d’expédition
        ↓
Association au transport : bateau, avion, express, terrestre
```

---

### 3.4.3 Statuts d’une commande fournisseur

| Statut | Description |
|---|---|
| Brouillon | Commande non validée. |
| En attente de validation | Commande soumise à validation. |
| Validée | Commande confirmée par LABMEDIS. |
| Envoyée au fournisseur | Commande transmise au fabricant. |
| En fabrication | Le fournisseur produit les marchandises. |
| Prête à expédier | Fabrication terminée. |
| Expédiée | Marchandise remise au transporteur. |
| En transit | En cours de dédouanement ou transport. |
| Partiellement reçue | Une partie seulement est reçue. |
| Reçue | Toute la commande est reçue. |
| Close | Commande terminée. |
| Annulée | Commande annulée. |

---

### 3.4.4 Règles de gestion

| Règle | Description |
|---|---|
| Devise | La commande doit préciser la devise du fournisseur. |
| Taux de change | Le taux de change doit être saisi ou récupéré au moment de la commande. |
| Prix unitaire | Le prix unitaire est saisi dans la devise du fournisseur. |
| Contre-valeur CFA | Le système calcule automatiquement la contre-valeur CFA. |
| Conditionnement | La quantité peut être saisie en cartons et/ou en unités de base. |
| Délai | La commande doit estimer une date de réception. |
| Transport | Le mode de transport doit être sélectionné. |
| Validation | Une commande validée ne peut plus être modifiée librement. |
| Annulation | Une commande annulée doit conserver un motif. |

---

### 3.4.5 Exemple métier

LABMEDIS commande chez **CONTINENTAL COMMODITIES** :

| Produit | Quantité carton | Conditionnement | Quantité unités |
|---|---:|---|---:|
| France Lait 1er âge 400g | 100 | carton/12 | 1 200 |
| France Lait 2ème âge 900g | 50 | carton/6 | 300 |
| France Lait AR 400g | 30 | carton/12 | 360 |

Prix d’achat fournisseur :

| Produit | PA Euro |
|---|---:|
| France Lait 1er âge 400g | 3,41 € |
| France Lait 2ème âge 900g | 7,43 € |
| France Lait AR 400g | 4,41 € |

Taux de change estimé :

```text
1 EUR = 656 XOF
```

La commande devra calculer la contre-valeur CFA.

---

### 3.4.6 User Stories

#### US-ACH-01 : Créer une commande fournisseur

**En tant que** responsable achats,  
**je veux** créer une commande fournisseur,  
**afin de** lancer l’approvisionnement des produits.

**Critères d’acceptation :**

1. L’utilisateur choisit un fournisseur actif.
2. La devise du fournisseur est proposée par défaut.
3. L’utilisateur peut modifier le taux de change.
4. L’utilisateur peut ajouter plusieurs lignes de produits.
5. Chaque ligne contient le produit, la quantité, le prix unitaire et le conditionnement.
6. Le système calcule le montant total en devise et en CFA.
7. La commande est enregistrée en statut brouillon.
8. L’utilisateur peut soumettre la commande à validation.

---

#### US-ACH-02 : Valider une commande fournisseur

**En tant que** direction ou responsable achats,  
**je veux** valider une commande fournisseur,  
**afin de** confirmer l’engagement d’achat.

**Critères d’acceptation :**

1. Seuls les utilisateurs autorisés peuvent valider.
2. Une commande incomplète ne peut pas être validée.
3. La validation fige les lignes principales.
4. Le statut passe à `Validée`.
5. La date de validation est enregistrée.
6. L’utilisateur valideur est journalisé.

---

#### US-ACH-03 : Suivre le statut de fabrication

**En tant que** responsable achats,  
**je veux** mettre à jour le statut de fabrication d’une commande,  
**afin de** suivre l’avancement chez le fournisseur.

**Critères d’acceptation :**

1. Les statuts possibles incluent : `En fabrication`, `Prête à expédier`, `Expédiée`.
2. Chaque changement de statut est horodaté.
3. L’utilisateur peut ajouter un commentaire.
4. Le système peut notifier les utilisateurs concernés via SignalR.
5. L’historique des statuts est consultable.

---

#### US-ACH-04 : Associer un mode de transport

**En tant que** responsable logistique,  
**je veux** associer un mode de transport à une commande,  
**afin de** suivre les coûts et délais logistiques.

**Critères d’acceptation :**

1. Les modes disponibles sont : maritime, aérien, express, terrestre.
2. Le mode de transport influence le calcul du prix de revient.
3. L’utilisateur peut saisir le numéro de conteneur, le numéro de vol, le numéro de tracking ou le numéro de BL.
4. Le système enregistre la date d’expédition estimée.
5. Le système enregistre la date d’arrivée estimée.
6. Le transport peut être partiel : une commande peut être divisée en plusieurs expéditions.

---

## 3.5 Workflow 3 : Transport, transit et douane

### 3.5.1 Objectif

Suivre la marchandise depuis le fournisseur jusqu’à l’entrepôt LABMEDIS.

Ce workflow est important car les produits peuvent arriver :

- par bateau,
- par avion,
- par express,
- par route.

Le mode de transport influence :

- le coût logistique,
- le délai,
- le prix de revient,
- la stratégie de réapprovisionnement.

---

### 3.5.2 Workflow détaillé

```text
Commande fournisseur expédiée
        ↓
Enregistrement de l’expédition
        ↓
Sélection du mode de transport
        ↓
Saisie des références : conteneur, LTA, tracking, BL
        ↓
Suivi du transit international
        ↓
Arrivée au port / aéroport
        ↓
Transit douanier
        ↓
Enregistrement des frais : freight, transit, douane, transfert
        ↓
Allocation des frais aux produits reçus
        ↓
Réception physique
```

---

### 3.5.3 Entités à prévoir

| Entité | Description |
|---|---|
| Shipment | Expédition liée à une ou plusieurs commandes. |
| ShipmentLine | Lignes de produits transportées. |
| TransportMode | Mode : maritime, aérien, express, terrestre. |
| CustomsOperation | Informations douanières. |
| LogisticsCost | Frais logistiques associés. |
| ShipmentEvent | Historique des événements de transport. |

---

### 3.5.4 Règles de gestion

| Règle | Description |
|---|---|
| Une expédition peut couvrir plusieurs commandes | Utile si plusieurs commandes sont regroupées dans un conteneur. |
| Une commande peut être expédiée en plusieurs fois | Exemple : réception partielle. |
| Les frais logistiques doivent être alloués | Freight, transit, transfert, douane, commission. |
| Le mode de transport doit être historisé | Important pour le calcul pondéré du prix de revient. |
| Les dates estimées et réelles doivent être distinguées | Date départ estimée, réelle, arrivée estimée, réelle. |

---

### 3.5.5 User Stories

#### US-LOG-01 : Créer une expédition

**En tant que** responsable logistique,  
**je veux** créer une expédition liée à une ou plusieurs commandes,  
**afin de** suivre la marchandise jusqu’à l’entrepôt.

**Critères d’acceptation :**

1. L’expédition peut être liée à une ou plusieurs commandes fournisseurs.
2. Le mode de transport est obligatoire.
3. Les références transport peuvent être saisies.
4. Les dates estimées peuvent être saisies.
5. L’expédition peut contenir plusieurs lignes produits.
6. Le statut initial est `Préparée` ou `En transit`.

---

#### US-LOG-02 : Enregistrer des frais logistiques

**En tant que** responsable import ou comptable,  
**je veux** enregistrer les frais logistiques liés à une expédition,  
**afin de** calculer le prix de revient réel des produits.

**Critères d’acceptation :**

1. Les types de frais peuvent inclure : freight, transit, douane, commission promo, frais transfert, assurance, manutention.
2. Les frais peuvent être saisis en EUR, USD ou XOF.
3. Le système convertit les frais en XOF selon le taux appliqué.
4. Les frais peuvent être répartis au prorata de la valeur, de la quantité ou du volume.
5. La méthode de répartition doit être configurable ou validable.
6. Les frais sont intégrés au calcul du prix de revient à la réception.

---

#### US-LOG-03 : Suivre les événements de transport

**En tant que** responsable logistique,  
**je veux** suivre les événements de transport,  
**afin de** connaître la position et le statut de la marchandise.

**Critères d’acceptation :**

1. Chaque événement contient une date, un statut, une description et un utilisateur.
2. Exemples d’événements : expédié, arrivé au port, en douane, dédouané, livré.
3. Les événements sont affichés dans une timeline.
4. Le dernier événement détermine le statut logistique.
5. Des alertes peuvent être générées en cas de retard.

---

## 3.6 Workflow 4 : Réception physique des produits

### 3.6.1 Objectif

Enregistrer l’arrivée physique des produits dans l’entrepôt LABMEDIS.

La réception doit permettre de :

- comparer les quantités commandées et reçues,
- enregistrer les lots,
- enregistrer les dates de péremption,
- constater les écarts,
- placer les produits en réception ou quarantaine,
- préparer la mise en stock.

---

### 3.6.2 Workflow détaillé

```text
Arrivée physique de la marchandise
        ↓
Sélection de la commande fournisseur ou expédition
        ↓
Contrôle des documents : BL, facture, packing list
        ↓
Contrôle quantitatif
        ↓
Saisie des quantités reçues
        ↓
Saisie des numéros de lot
        ↓
Saisie des dates de péremption
        ↓
Constat des écarts éventuels
        ↓
Placement en zone réception
        ↓
Contrôle qualité
        ↓
Validation ou rejet
        ↓
Mise en stock
```

---

### 3.6.3 Statuts d’une réception

| Statut | Description |
|---|---|
| Brouillon | Réception non confirmée. |
| En cours | Saisie des quantités en cours. |
| En contrôle | En attente de contrôle qualité. |
| Partiellement reçue | Quantité reçue inférieure à la quantité attendue. |
| Reçue | Tous les produits attendus sont reçus. |
| Avec écart | Différence entre commande et réception. |
| Validée | Réception confirmée. |
| Mise en stock | Produits rangés dans les emplacements. |
| Refusée | Réception refusée. |

---

### 3.6.4 Règles de gestion

| Règle | Description |
|---|---|
| Lot obligatoire | Chaque produit pharmaceutique reçu doit avoir un numéro de lot. |
| Péremption obligatoire | Chaque lot doit avoir une date de péremption. |
| Quantité en unités | Le système calcule la quantité en unités de base. |
| Quantité en cartons | Le système conserve aussi la quantité en cartons si disponible. |
| Écart | Si quantité reçue différente, un écart doit être enregistré. |
| Produit non commandé | Peut être signalé et nécessiter une validation. |
| Produit endommagé | Peut être placé en quarantaine. |
| Péremption courte | Doit déclencher une alerte. |
| Statut initial | Par défaut, les produits reçus peuvent être placés en `En réception` ou `Quarantaine`. |

---

### 3.6.5 Exemple métier

Réception d’un conteneur **France Lait** :

| Produit | Commandé | Reçu | Lot | Péremption |
|---|---:|---:|---|---|
| France Lait 1er âge 400g | 100 cartons | 100 cartons | LOT-A100 | 30/06/2027 |
| France Lait 2ème âge 900g | 50 cartons | 48 cartons | LOT-B200 | 31/05/2027 |
| France Lait AR 400g | 30 cartons | 30 cartons | LOT-C300 | 30/04/2027 |

Le système doit détecter :

```text
France Lait 2ème âge 900g : manquant 2 cartons
```

Un écart doit être enregistré.

---

### 3.6.6 User Stories

#### US-REC-01 : Créer une réception fournisseur

**En tant que** magasinier,  
**je veux** créer une réception à partir d’une commande fournisseur,  
**afin d’enregistrer les produits arrivés.

**Critères d’acceptation :**

1. L’utilisateur sélectionne une commande fournisseur validée.
2. Le système affiche les lignes attendues.
3. L’utilisateur saisit les quantités reçues.
4. L’utilisateur saisit les numéros de lot.
5. L’utilisateur saisit les dates de péremption.
6. Le système calcule les quantités en unités de base.
7. La réception peut être enregistrée en brouillon.

---

#### US-REC-02 : Gérer les écarts de réception

**En tant que** magasinier ou responsable logistique,  
**je veux** enregistrer les écarts entre quantités commandées et reçues,  
**afin de** garder une trace fiable des réceptions.

**Critères d’acceptation :**

1. Le système compare automatiquement quantité commandée et quantité reçue.
2. Si quantité reçue inférieure, le statut peut être `Partiellement reçue`.
3. Si quantité reçue supérieure, une validation responsable est demandée.
4. Les écarts doivent avoir un motif : manquant, casse, erreur fournisseur, refus, autre.
5. Les écarts sont historisés.
6. Les écarts peuvent générer des alertes.

---

#### US-REC-03 : Enregistrer les lots reçus

**En tant que** magasinier,  
**je veux** enregistrer les numéros de lot et dates de péremption,  
**afin de** garantir la traçabilité pharmaceutique.

**Critères d’acceptation :**

1. Le numéro de lot est obligatoire.
2. La date de péremption est obligatoire.
3. Un même produit peut être reçu en plusieurs lots.
4. Un lot peut être réparti sur plusieurs emplacements après mise en stock.
5. Le lot est lié à la commande fournisseur et au produit.
6. Le lot peut être associé à un mode de transport.

---

#### US-REC-04 : Placer les produits en quarantaine

**En tant que** responsable qualité,  
**je veux** placer certains produits reçus en quarantaine,  
**afin de** les empêcher d’être vendus avant contrôle.

**Critères d’acceptation :**

1. Un lot peut être marqué `En quarantaine`.
2. Les lots en quarantaine ne sont pas disponibles à la vente.
3. L’emplacement de quarantaine peut être défini.
4. Le motif de mise en quarantaine est obligatoire.
5. Le lot peut ensuite être libéré ou rejeté.
6. L’action est journalisée.

---

## 3.7 Workflow 5 : Contrôle qualité et libération des lots

### 3.7.1 Objectif

S’assurer que les produits reçus peuvent être vendus.

Dans le domaine pharmaceutique, certains lots peuvent nécessiter une validation qualité avant mise à disposition.

---

### 3.7.2 Workflow

```text
Lot reçu
    ↓
Statut : En réception ou Quarantaine
    ↓
Contrôle qualité
    ↓
Conforme ?
    ├── Oui → Libération du lot
    └── Non → Non-conformité / Quarantaine prolongée / Rejet
```

---

### 3.7.3 Statuts qualité possibles

| Statut | Description |
|---|---|
| En réception | Produit reçu, pas encore contrôlé. |
| En quarantaine | Produit bloqué en attente de décision. |
| Conforme | Produit accepté. |
| Libéré | Produit disponible à la vente. |
| Non conforme | Produit bloqué. |
| Rejeté | Produit non accepté. |
| Détruit | Produit définitivement sorti. |

---

### 3.7.4 Règles de gestion

| Règle | Description |
|---|---|
| Vente interdite | Un lot non libéré ne peut pas être vendu. |
| Libération tracée | La libération doit être effectuée par un utilisateur autorisé. |
| Motif obligatoire | En cas de non-conformité, un motif est obligatoire. |
| Historique | Tous les changements de statut doivent être historisés. |
| Notification | Une alerte peut être envoyée au responsable qualité. |

---

### 3.7.5 User Stories

#### US-QUA-01 : Libérer un lot

**En tant que** responsable qualité,  
**je veux** libérer un lot conforme,  
**afin de** le rendre disponible à la vente.

**Critères d’acceptation :**

1. Le lot doit être en statut `En quarantaine` ou `En réception`.
2. Seuls les utilisateurs autorisés peuvent libérer un lot.
3. Le statut passe à `Libéré`.
4. Le lot devient visible dans le stock disponible.
5. L’action est journalisée.
6. Une notification peut être envoyée.

---

#### US-QUA-02 : Marquer un lot non conforme

**En tant que** responsable qualité,  
**je veux** marquer un lot comme non conforme,  
**afin d’empêcher sa vente.

**Critères d’acceptation :**

1. Le statut passe à `Non conforme`.
2. Le lot n’est plus disponible à la vente.
3. Un motif est obligatoire.
4. Le lot peut être déplacé vers une zone de quarantaine.
5. L’action est journalisée.

---

## 3.8 Workflow 6 : Mise en stock et adressage

### 3.8.1 Objectif

Ranger physiquement les produits reçus dans des emplacements précis de l’entrepôt.

---

### 3.8.2 Workflow

```text
Lot libéré ou en réception validée
        ↓
Sélection de l’emplacement cible
        ↓
Scan produit / lot / emplacement
        ↓
Saisie de la quantité à ranger
        ↓
Confirmation
        ↓
Mise à jour du stock par lot et emplacement
        ↓
Historisation du mouvement
```

---

### 3.8.3 Règles de gestion

| Règle | Description |
|---|---|
| Emplacement obligatoire | Tout produit en stock doit être localisé. |
| Multi-emplacement | Un lot peut être stocké dans plusieurs emplacements. |
| Scan | La mise en stock peut être faite par scan. |
| Quantité | La quantité mise en stock ne peut pas dépasser la quantité reçue. |
| Zone froide | Certains produits peuvent nécessiter une zone spécifique. |
| Zone quarantaine | Les lots non libérés doivent rester en zone contrôlée. |

---

### 3.8.4 User Stories

#### US-STK-01 : Mettre un produit en stock

**En tant que** magasinier,  
**je veux** ranger un produit dans un emplacement,  
**afin de** le localiser précisément.

**Critères d’acceptation :**

1. L’utilisateur sélectionne un lot reçu.
2. L’utilisateur choisit un emplacement actif.
3. L’utilisateur saisit la quantité.
4. Le système met à jour la quantité du lot à cet emplacement.
5. Un mouvement de stock est créé.
6. L’opération est journalisée.

---

#### US-STK-02 : Transférer un lot entre emplacements

**En tant que** magasinier,  
**je veux** déplacer un lot d’un emplacement à un autre,  
**afin de** réorganiser le stock.

**Critères d’acceptation :**

1. L’utilisateur sélectionne le lot et l’emplacement source.
2. L’utilisateur choisit l’emplacement destination.
3. La quantité transférée ne peut pas dépasser la quantité disponible.
4. Le mouvement est de type `Transfert`.
5. Le stock est mis à jour immédiatement.
6. L’historique conserve l’emplacement source et destination.

---

#### US-STK-03 : Rechercher un produit dans l’entrepôt

**En tant que** magasinier,  
**je veux** savoir où se trouve un produit,  
**afin de** le retrouver rapidement.

**Critères d’acceptation :**

1. La recherche peut se faire par produit, lot, emplacement, fournisseur ou code CIP.
2. Le résultat affiche les emplacements, lots, quantités, statuts et péremptions.
3. Les lots périmés sont clairement identifiés.
4. Les lots en quarantaine sont clairement identifiés.
5. L’information est mise à jour en temps réel.

---

## 3.9 Workflow 7 : Gestion des ventes clients

### 3.9.1 Objectif

Permettre à LABMEDIS de vendre les produits aux :

- répartiteurs,
- cliniques,
- hôpitaux,
- pharmacies,
- autres structures.

Exemples :

- CAMEG,
- LABOREX TOGO,
- TEDIS PHARMA TOGO,
- UBIPHARM TOGO,
- CHP ANEHO,
- CHR SOKODE.

---

### 3.9.2 Workflow détaillé

```text
Demande client
        ↓
Vérification de la fiche client
        ↓
Vérification des produits demandés
        ↓
Vérification du stock disponible par lot
        ↓
Application du prix client / tarif catalogue
        ↓
Application de la TVA
        ↓
Création du devis ou commande
        ↓
Validation commerciale
        ↓
Réservation du stock
        ↓
Préparation de commande
        ↓
Validation de préparation
        ↓
Livraison
        ↓
Facturation
        ↓
Suivi paiement
```

---

### 3.9.3 Statuts d’une commande client

| Statut | Description |
|---|---|
| Brouillon | Commande non confirmée. |
| Devis | Proposition envoyée au client. |
| Confirmée | Commande validée par le client. |
| Réservée | Stock réservé. |
| En préparation | Magasinier prépare la commande. |
| Prête | Préparation terminée. |
| Livrée | Produits remis au client. |
| Facturée | Facture générée. |
| Partiellement livrée | Certains produits livrés seulement. |
| Annulée | Commande annulée. |

---

### 3.9.4 Règles de gestion

| Règle | Description |
|---|---|
| Client actif | Une commande ne peut être créée que pour un client actif. |
| Stock disponible | Le système doit vérifier la disponibilité. |
| Lot | Le système doit proposer les lots selon FEFO. |
| Prix | Le prix peut venir du tarif catalogue ou d’un tarif client spécifique. |
| TVA | La TVA est appliquée selon la catégorie ou le produit. |
| Réservation | La commande confirmée peut réserver le stock. |
| Livraison | Une commande peut être livrée totalement ou partiellement. |
| Facture | Une commande livrée peut générer une facture. |
| Avoir | Un retour peut générer un avoir. |

---

### 3.9.5 Exemple métier

Commande client **LABOREX TOGO** :

| Produit | Quantité demandée | Lot proposé | Prix HT | TVA | Prix TTC |
|---|---:|---|---:|---:|---:|
| France Lait 1er âge 400g | 120 | LOT-A100 | 3 660 | 18% | 4 318,8 |
| France Lait AR 400g | 60 | LOT-C300 | 4 960 | 18% | 5 852,8 |

Le système doit :

1. Vérifier le stock disponible.
2. Proposer les lots avec péremption la plus proche.
3. Réserver le stock.
4. Calculer le total HT.
5. Calculer la TVA.
6. Calculer le total TTC.

---

### 3.9.6 User Stories

#### US-VEN-01 : Créer une commande client

**En tant que** commercial,  
**je veux** créer une commande client,  
**afin de** vendre les produits disponibles.

**Critères d’acceptation :**

1. L’utilisateur sélectionne un client actif.
2. L’utilisateur ajoute des produits.
3. Le système affiche le prix applicable.
4. Le système affiche la disponibilité par lot.
5. Le système calcule les totaux HT, TVA et TTC.
6. La commande peut être enregistrée en brouillon.

---

#### US-VEN-02 : Vérifier la disponibilité du stock

**En tant que** commercial,  
**je veux** voir le stock disponible avant de vendre,  
**afin d’éviter les ventes sans stock.

**Critères d’acceptation :**

1. Le système affiche le stock physique.
2. Le système affiche le stock réservé.
3. Le système affiche le stock disponible.
4. Les lots en quarantaine ne sont pas inclus.
5. Les lots périmés ne sont pas inclus.
6. Le système affiche les lots avec dates de péremption.

---

#### US-VEN-03 : Appliquer FEFO automatiquement

**En tant que** système,  
**je veux** proposer les lots avec la péremption la plus proche,  
**afin de** respecter les bonnes pratiques pharmaceutiques.

**Critères d’acceptation :**

1. Les lots proposés sont triés par date de péremption ascendante.
2. Les lots périmés sont exclus.
3. Les lots en quarantaine sont exclus.
4. Le stock réservé est déduit de la disponibilité.
5. L’utilisateur peut exceptionnellement choisir un autre lot si autorisé.
6. Toute exception est journalisée.

---

#### US-VEN-04 : Réserver le stock

**En tant que** système,  
**je veux** réserver le stock lorsqu’une commande est confirmée,  
**afin d’empêcher la vente du même stock à un autre client.

**Critères d’acceptation :**

1. La réservation est liée à la commande client.
2. La réservation est liée à un lot et un emplacement.
3. Le stock disponible diminue.
4. Le stock physique ne diminue pas avant sortie réelle.
5. La réservation est annulée si la commande est annulée.
6. La réservation est historisée.

---

#### US-VEN-05 : Appliquer un prix spécifique client

**En tant que** responsable commercial,  
**je veux** définir des prix spécifiques par client,  
**afin de** gérer les tarifs négociés avec les répartiteurs.

**Critères d’acceptation :**

1. Un produit peut avoir un prix catalogue par défaut.
2. Un client peut avoir une grille tarifaire spécifique.
3. Le prix spécifique peut avoir une date de début et une date de fin.
4. Le système applique d’abord le prix client spécifique s’il existe.
5. Sinon, il applique le prix catalogue.
6. Les modifications tarifaires sont historisées.

---

## 3.10 Workflow 8 : Préparation de commande

### 3.10.1 Objectif

Transformer une commande client validée en préparation physique.

---

### 3.10.2 Workflow

```text
Commande confirmée
        ↓
Stock réservé
        ↓
Création d’un ordre de préparation
        ↓
Liste des lots et emplacements proposés
        ↓
Picking par scan
        ↓
Contrôle des quantités
        ↓
Gestion des manquants éventuels
        ↓
Validation préparation
        ↓
Commande prête à livrer
```

---

### 3.10.3 Règles de gestion

| Règle | Description |
|---|---|
| Scan recommandé | Le préparateur peut scanner produit, lot, emplacement. |
| FEFO | Le système propose les lots à prélever. |
| Manquant | Si produit manquant, le système doit permettre un signalement. |
| Substitution | Aucune substitution sans accord commercial. |
| Validation | La préparation doit être validée avant livraison. |
| Historique | Chaque prélèvement est journalisé. |

---

### 3.10.4 User Stories

#### US-PREP-01 : Préparer une commande

**En tant que** préparateur,  
**je veux** voir la liste des produits à prélever,  
**afin de** préparer physiquement la commande.

**Critères d’acceptation :**

1. L’écran affiche les lignes de commande.
2. Chaque ligne affiche le produit, la quantité, le lot proposé, l’emplacement.
3. Le préparateur peut confirmer chaque ligne.
4. Le préparateur peut signaler un manquant.
5. Le statut passe à `En préparation`.
6. Une fois terminé, le statut passe à `Prête`.

---

#### US-PREP-02 : Scanner un produit pendant la préparation

**En tant que** préparateur,  
**je veux** scanner le produit ou le lot,  
**afin de** valider rapidement le prélèvement.

**Critères d’acceptation :**

1. Le scan peut remplir automatiquement le champ produit ou lot.
2. Si le lot scanné ne correspond pas à la commande, une erreur est affichée.
3. Si le lot est périmé, une erreur est affichée.
4. Si le lot est en quarantaine, une erreur est affichée.
5. Le scan est journalisé.

---

## 3.11 Workflow 9 : Livraison

### 3.11.1 Objectif

Enregistrer la remise des produits au client.

---

### 3.11.2 Workflow

```text
Commande prête
        ↓
Création du bon de livraison
        ↓
Confirmation de livraison
        ↓
Sortie de stock
        ↓
Décrémentation du stock physique
        ↓
Annulation des réservations
        ↓
Génération du BL
        ↓
Facturation éventuelle
```

---

### 3.11.3 Règles de gestion

| Règle | Description |
|---|---|
| BL | Chaque livraison doit générer un bon de livraison. |
| Sortie de stock | La livraison diminue le stock physique. |
| Lot | La sortie doit être liée au lot livré. |
| Livraison partielle | Une commande peut être livrée en plusieurs fois. |
| Signature | Optionnellement, la livraison peut être signée ou confirmée. |
| Facturation | Une livraison peut déclencher la facturation. |

---

### 3.11.4 User Stories

#### US-LIV-01 : Livrer une commande

**En tant que** responsable logistique ou livreur,  
**je veux** confirmer la livraison d’une commande,  
**afin de** sortir les produits du stock.

**Critères d’acceptation :**

1. La commande doit être en statut `Prête` ou `En livraison`.
2. Le système génère un bon de livraison.
3. La livraison crée un mouvement de stock de type `Vente`.
4. Le stock physique est décrémenté.
5. Les réservations sont soldées.
6. Le statut passe à `Livrée` ou `Partiellement livrée`.

---

#### US-LIV-02 : Générer un bon de livraison

**En tant que** utilisateur autorisé,  
**je veux** générer un bon de livraison PDF,  
**afin de** le remettre au client.

**Critères d’acceptation :**

1. Le BL contient la date, le client, les produits, lots, quantités.
2. Le BL peut afficher ou masquer les prix selon les droits.
3. Le BL est téléchargeable en PDF.
4. Le BL peut être imprimé.
5. Le BL est historisé.

---

## 3.12 Workflow 10 : Facturation

### 3.12.1 Objectif

Générer les factures clients après livraison.

---

### 3.12.2 Workflow

```text
Livraison validée
        ↓
Sélection des lignes livrées
        ↓
Application des prix
        ↓
Application de la TVA
        ↓
Génération facture
        ↓
Validation comptable
        ↓
Suivi paiement
```

---

### 3.12.3 Règles de gestion

| Règle | Description |
|---|---|
| Facture liée | Une facture est liée à une commande et/ou à des livraisons. |
| TVA | La TVA doit être calculée ligne par ligne. |
| Acompte | Optionnel : gestion des acomptes. |
| Avoir | Un retour peut générer une facture d’avoir. |
| Numérotation | Les factures doivent avoir une numérotation unique. |
| Export | Les factures peuvent être exportées en PDF. |

---

### 3.12.4 User Stories

#### US-FAC-01 : Générer une facture

**En tant que** comptable ou utilisateur autorisé,  
**je veux** générer une facture après livraison,  
**afin de** facturer le client.

**Critères d’acceptation :**

1. La facture reprend les lignes livrées.
2. Le prix HT, la TVA et le TTC sont calculés.
3. La facture est liée au client et à la commande.
4. La facture possède un numéro unique.
5. La facture peut être imprimée en PDF.
6. La facture est historisée.

---

#### US-FAC-02 : Gérer un avoir

**En tant que** comptable,  
**je veux** créer un avoir pour un retour client,  
**afin de** régulariser la facturation.

**Critères d’acceptation :**

1. L’avoir peut être total ou partiel.
2. L’avoir est lié à une facture ou à un retour.
3. L’avoir contient les produits retournés.
4. L’avoir calcule la TVA correspondante.
5. L’avoir est numéroté et historisé.
6. L’avoir peut être exporté en PDF.

---

## 3.13 Workflow 11 : Retours clients

### 3.13.1 Objectif

Gérer les produits retournés par les clients.

---

### 3.13.2 Workflow

```text
Demande de retour
        ↓
Identification de la commande d’origine
        ↓
Sélection des produits retournés
        ↓
Sélection du lot d’origine
        ↓
Motif du retour
        ↓
Réception du retour
        ↓
Décision : remise en stock, quarantaine, destruction, refus
        ↓
Mise à jour du stock
        ↓
Avoir éventuel
```

---

### 3.13.3 Types de retour

| Type | Description |
|---|---|
| Retour accepté | Produit remis en stock. |
| Retour en quarantaine | Produit à contrôler. |
| Retour refusé | Produit non accepté. |
| Retour détruit | Produit détruit. |
| Retour périmé | Produit périmé retourné. |
| Retour erreur | Produit livré par erreur. |

---

### 3.13.4 Règles de gestion

| Règle | Description |
|---|---|
| Lien commande | Le retour doit être relié à une commande ou livraison. |
| Lot | Le retour doit identifier le lot si possible. |
| État | L’état du produit retourné doit être évalué. |
| Stock | Le stock est mis à jour uniquement si le retour est accepté. |
| Avoir | Un avoir peut être généré. |
| Traçabilité | Tous les retours sont historisés. |

---

### 3.13.5 User Stories

#### US-RET-01 : Créer un retour client

**En tant que** commercial ou magasinier,  
**je veux** enregistrer un retour client,  
**afin de** régulariser le stock et la facturation.

**Critères d’acceptation :**

1. L’utilisateur sélectionne le client.
2. L’utilisateur sélectionne la commande ou livraison d’origine.
3. L’utilisateur sélectionne les produits retournés.
4. L’utilisateur saisit les quantités.
5. L’utilisateur saisit le motif.
6. Le système propose une décision : remise en stock, quarantaine, destruction.

---

#### US-RET-02 : Remettre un produit en stock après retour

**En tant que** responsable qualité ou magasinier,  
**je veux** remettre un produit retourné en stock,  
**afin de** le rendre disponible s’il est conforme.

**Critères d’acceptation :**

1. Le produit doit être accepté.
2. Le lot doit être identifiable.
3. Le produit ne doit pas être périmé.
4. Le produit doit être en état de vente.
5. Le stock est incrémenté.
6. Un mouvement de type `Retour client` est créé.

---

## 3.14 Workflow 12 : Prévisions et réapprovisionnement

### 3.14.1 Objectif

Anticiper les ruptures de stock.

Le PRD audio indique clairement :

> Il faut pouvoir estimer qu’un produit risque de finir et commander 3 ou 4 mois à l’avance afin que le fabricant puisse produire et livrer.

---

### 3.14.2 Workflow

```text
Analyse du stock disponible
        ↓
Analyse du stock en transit
        ↓
Analyse des ventes historiques
        ↓
Calcul de la vitesse de rotation
        ↓
Calcul de la consommation prévisionnelle
        ↓
Intégration des délais de fabrication
        ↓
Intégration des délais de transport
        ↓
Détection des risques de rupture
        ↓
Génération des suggestions de commande
        ↓
Validation par responsable achats
```

---

### 3.14.3 Formule simplifiée de réapprovisionnement

```text
Délai total = Délai de fabrication + Délai de transport + Délai de transit + Délai interne
```

```text
Stock nécessaire = Consommation moyenne journalière × Délai total
```

```text
Point de commande = Stock nécessaire + Stock de sécurité
```

```text
Risque de rupture = Stock disponible + Stock en transit <= Point de commande
```

---

### 3.14.4 Exemple

Produit : `France Lait 1er âge 400g`

| Paramètre | Valeur |
|---|---:|
| Ventes moyennes journalières | 20 unités |
| Délai fabrication | 45 jours |
| Délai transport maritime | 40 jours |
| Délai transit douane | 15 jours |
| Délai interne | 5 jours |
| Délai total | 105 jours |
| Stock de sécurité | 300 unités |

Calcul :

```text
Stock nécessaire = 20 × 105 = 2100 unités
Point de commande = 2100 + 300 = 2400 unités
```

Si :

```text
Stock disponible + Stock en transit <= 2400
```

alors le système propose une commande.

---

### 3.14.5 User Stories

#### US-PREV-01 : Configurer les délais de réapprovisionnement

**En tant que** responsable achats,  
**je veux** définir les délais de fabrication et de livraison par produit ou fournisseur,  
**afin de** calculer les besoins futurs.

**Critères d’acceptation :**

1. Les délais peuvent être définis par fournisseur.
2. Les délais peuvent être surchargés par produit.
3. Les délais incluent fabrication, transport, transit et interne.
4. Les délais sont utilisés dans le calcul de prévision.
5. Les modifications sont historisées.

---

#### US-PREV-02 : Détecter les produits à risque de rupture

**En tant que** responsable achats,  
**je veux** voir les produits proches de la rupture,  
**afin de** commander à temps.

**Critères d’acceptation :**

1. Le système calcule le stock disponible.
2. Le système prend en compte le stock en transit.
3. Le système prend en compte la consommation historique.
4. Le système prend en compte les délais fournisseurs.
5. Les produits à risque sont affichés dans un tableau.
6. Des alertes sont générées.

---

#### US-PREV-03 : Générer une suggestion de commande

**En tant que** responsable achats,  
**je veux** générer une suggestion de commande à partir des prévisions,  
**afin de** accélérer le réapprovisionnement.

**Critères d’acceptation :**

1. Le système propose les produits à commander.
2. Le système propose une quantité suggérée.
3. La quantité peut être modifiée avant validation.
4. L’utilisateur peut transformer la suggestion en commande fournisseur.
5. La suggestion conserve la justification du calcul.
6. L’historique des suggestions est consultable.

---

## 3.15 Workflow 13 : Inventaires et ajustements

### 3.15.1 Objectif

Garantir la fiabilité du stock physique et du stock système.

---

### 3.15.2 Workflow inventaire

```text
Création session inventaire
        ↓
Choix du périmètre : produit, lot, emplacement, zone, catégorie
        ↓
Gel temporaire des mouvements
        ↓
Comptage physique
        ↓
Saisie des quantités comptées
        ↓
Comparaison avec stock système
        ↓
Identification des écarts
        ↓
Validation responsable
        ↓
Génération des ajustements
        ↓
Clôture inventaire
```

---

### 3.15.3 Types d’inventaire

| Type | Description |
|---|---|
| Inventaire général | Tout l’entrepôt. |
| Inventaire par zone | Une zone ou allée. |
| Inventaire par produit | Un produit spécifique. |
| Inventaire par lot | Un lot spécifique. |
| Inventaire par catégorie | Exemple : produits infantiles. |
| Inventaire tournant | Inventaire cyclique de certains produits. |

---

### 3.15.4 Règles de gestion

| Règle | Description |
|---|---|
| Mouvements gelés | Pendant un inventaire actif, les mouvements du périmètre concerné peuvent être bloqués. |
| Écart obligatoire | Tout écart doit être visible. |
| Validation | Les ajustements doivent être validés par un responsable. |
| Motif | Les ajustements doivent avoir un motif. |
| Traçabilité | Les écarts et ajustements sont historisés. |

---

### 3.15.5 User Stories

#### US-INV-01 : Créer une session d’inventaire

**En tant que** magasinier ou responsable stock,  
**je veux** créer une session d’inventaire,  
**afin de** compter les produits.

**Critères d’acceptation :**

1. L’utilisateur choisit le périmètre.
2. Le système génère les lignes à compter.
3. Les quantités système sont enregistrées.
4. Les mouvements du périmètre peuvent être gelés.
5. La session est enregistrée avec un statut `En cours`.

---

#### US-INV-02 : Saisir les quantités comptées

**En tant que** magasinier,  
**je veux** saisir les quantités physiques comptées,  
**afin de** comparer avec le stock système.

**Critères d’acceptation :**

1. L’utilisateur peut saisir par ligne.
2. Le système calcule la différence automatiquement.
3. Les écarts positifs et négatifs sont affichés.
4. L’utilisateur peut ajouter un commentaire.
5. Les saisies sont sauvegardées.

---

#### US-INV-03 : Valider les ajustements d’inventaire

**En tant que** responsable stock,  
**je veux** valider les écarts d’inventaire,  
**afin de** mettre à jour le stock système.

**Critères d’acceptation :**

1. Seuls les utilisateurs autorisés peuvent valider.
2. Le système crée des mouvements d’ajustement.
3. Les motifs sont obligatoires.
4. Le stock système est mis à jour.
5. La session passe au statut `Clôturée`.
6. L’historique est conservé.

---

## 3.16 Workflow 14 : Gestion des péremptions

### 3.16.1 Objectif

Éviter la vente de produits périmés et limiter les pertes.

---

### 3.16.2 Workflow

```text
Job quotidien ou écran alertes
        ↓
Analyse des lots actifs
        ↓
Identification des lots proches péremption
        ↓
Classement par seuil : 30, 60, 90, 120 jours
        ↓
Notification aux responsables
        ↓
Actions possibles : promotion, transfert, destruction, quarantaine
```

---

### 3.16.3 Règles de gestion

| Règle | Description |
|---|---|
| Lot périmé | Non vendable. |
| Lot proche péremption | Visible avec alerte. |
| FEFO | Les lots proches péremption sont proposés en priorité. |
| Destruction | Les produits périmés peuvent être détruits. |
| Historique | Tous les traitements sont historisés. |
| Notification | Les alertes peuvent être envoyées par SignalR ou email. |

---

### 3.16.4 User Stories

#### US-PER-01 : Visualiser les lots proches de péremption

**En tant que** responsable stock,  
**je veux** voir les lots proches de péremption,  
**afin de** prendre une décision avant expiration.

**Critères d’acceptation :**

1. L’écran affiche les lots par seuil : 30, 60, 90, 120 jours.
2. Les lots sont filtrables par produit, catégorie, fournisseur, emplacement.
3. Le stock restant est affiché.
4. La valeur du stock proche péremption peut être affichée.
5. L’utilisateur peut exporter la liste.

---

#### US-PER-02 : Bloquer les lots périmés

**En tant que** système,  
**je veux** bloquer automatiquement les lots périmés,  
**afin d’empêcher leur vente.

**Critères d’acceptation :**

1. Un job Hangfire vérifie quotidiennement les dates de péremption.
2. Les lots dont la date est dépassée passent au statut `Périmé`.
3. Les lots périmés ne peuvent pas être proposés à la vente.
4. Une alerte est générée.
5. L’action est journalisée.

---

## 3.17 Workflow 15 : Destruction, perte et échantillons

### 3.17.1 Objectif

Gérer les sorties de stock exceptionnelles.

---

### 3.17.2 Types de sortie exceptionnelle

| Type | Description |
|---|---|
| Destruction | Produit périmé, endommagé ou non conforme. |
| Perte | Produit non retrouvé en inventaire. |
| Échantillon | Produit offert ou utilisé pour démonstration. |
| Casse | Produit cassé pendant manutention. |
| Expired | Produit périmé. |

---

### 3.17.3 Règles de gestion

| Règle | Description |
|---|---|
| Motif obligatoire | Toute sortie exceptionnelle doit avoir un motif. |
| Validation | Certaines sorties doivent être validées par responsable. |
| Traçabilité | Le mouvement doit être historisé. |
| Stock | Le stock doit être décrémenté. |
| Valeur | La valeur du stock perdu peut être calculée. |

---

### 3.17.4 User Stories

#### US-DESTR-01 : Détruire un lot

**En tant que** responsable qualité ou stock,  
**je veux** enregistrer la destruction d’un lot,  
**afin de** le sortir définitivement du stock.

**Critères d’acceptation :**

1. L’utilisateur sélectionne le lot et la quantité.
2. Le motif est obligatoire.
3. Le stock est décrémenté.
4. Le lot peut passer au statut `Détruit` si totalement détruit.
5. Un mouvement de type `Destruction` est créé.
6. L’opération est journalisée.

---

#### US-PERTE-01 : Enregistrer une perte

**En tant que** responsable stock,  
**je veux** enregistrer une perte de produit,  
**afin de** corriger le stock.

**Critères d’acceptation :**

1. L’utilisateur sélectionne produit, lot, emplacement.
2. La quantité perdue est saisie.
3. Le motif est obligatoire.
4. Le stock est décrémenté.
5. Le mouvement est historisé.
6. Une alerte peut être envoyée si la quantité est importante.

---

## 3.18 Workflow 16 : Pricing et validation tarifaire

### 3.18.1 Objectif

Définir et contrôler les prix de vente.

LABMEDIS définit ses prix à partir :

- du prix d’achat,
- des frais logistiques,
- des coefficients de commissions,
- du freight,
- du transit,
- des frais de transfert,
- de la marge.

---

### 3.18.2 Workflow pricing

```text
Prix d’achat fournisseur
        ↓
Conversion en CFA
        ↓
Application commissions promo
        ↓
Application freight
        ↓
Application transit
        ↓
Application frais transfert
        ↓
Calcul du prix de revient
        ↓
Application marge
        ↓
Prix de vente HT calculé
        ↓
Comparaison avec prix catalogue
        ↓
Validation direction
        ↓
Publication tarif
```

---

### 3.18.3 Exemple France Lait

| Produit | PA Euro | PA CFA | Coefficients | PR CFA | Marge | PV HT calculé |
|---|---:|---:|---|---:|---:|---:|
| France Lait 1er âge 400g | 3,41 | 2 237 | 1.25 × 1.03 × 1.09 × 1.07 | 3 359 | 1.10 | 3 695 |

---

### 3.18.4 User Stories

#### US-PRICE-01 : Simuler un prix de revient

**En tant que** responsable financier,  
**je veux** simuler le prix de revient d’un produit,  
**afin de** décider du prix de vente.

**Critères d’acceptation :**

1. L’utilisateur saisit le prix d’achat en devise.
2. L’utilisateur saisit le taux de change.
3. L’utilisateur sélectionne un profil de coûts.
4. Le système calcule le prix de revient CFA.
5. Le système calcule le prix de vente cible selon la marge.
6. Le système compare avec le prix catalogue actuel.

---

#### US-PRICE-02 : Valider un prix de vente

**En tant que** direction,  
**je veux** valider les prix de vente,  
**afin de** contrôler la politique tarifaire.

**Critères d’acceptation :**

1. Les prix peuvent être proposés par le responsable pricing.
2. La direction peut approuver ou rejeter.
3. Un prix validé possède une date d’effet.
4. Les anciens prix sont historisés.
5. Le prix validé est appliqué aux ventes futures.
6. Les modifications sont journalisées.

---

## 3.19 Workflow 17 : Notifications et alertes

### 3.19.1 Objectif

Informer les utilisateurs en temps réel des événements importants.

---

### 3.19.2 Types d’alertes

| Alerte | Déclencheur |
|---|---|
| Stock faible | Produit sous le seuil minimum. |
| Rupture | Stock disponible nul. |
| Péremption proche | Lot proche de la date limite. |
| Lot périmé | Lot expiré. |
| Réception en retard | Commande non reçue à la date prévue. |
| Écart réception | Différence commande/réception. |
| Quarantaine | Lot bloqué. |
| Préparation prête | Commande prête à livrer. |
| Facture générée | Facture créée. |
| Retour client | Retour enregistré. |

---

### 3.19.3 User Stories

#### US-NOTIF-01 : Recevoir des notifications temps réel

**En tant qu’utilisateur connecté,  
**je veux** recevoir des notifications temps réel,  
**afin de** réagir rapidement aux événements importants.

**Critères d’acceptation :**

1. Le backend utilise SignalR.
2. Les notifications sont affichées dans le frontend React.
3. L’utilisateur peut marquer une notification comme lue.
4. Les notifications peuvent être filtrées par type.
5. Les notifications critiques peuvent être affichées en toast.
6. Les notifications sont historisées.

---

## 3.20 Synthèse des règles transverses

### 3.20.1 Règles obligatoires

| Règle | Description |
|---|---|
| Lot obligatoire | Tout produit pharmaceutique doit être suivi par lot. |
| Péremption obligatoire | Tout lot doit avoir une date de péremption. |
| Traçabilité | Toute entrée/sortie doit être historisée. |
| Soft delete | Aucune suppression physique dans le backend. |
| Rôles | Chaque action doit vérifier les permissions. |
| Audit | Chaque action sensible doit journaliser utilisateur, date, IP, UserAgent. |
| Statuts | Les entités principales doivent avoir des statuts clairs. |
| Prix | Les montants financiers doivent être manipulés avec précision. |
| Devise | Toute commande internationale doit préciser la devise. |
| Transport | Toute réception doit pouvoir être associée à un mode de transport. |

---

### 3.20.2 Règles frontend React

| Règle | Description |
|---|---|
| Formulaires | Validation côté client avant envoi API. |
| Toasts | Afficher les succès et erreurs via notifications. |
| Statuts | Afficher les statuts avec badges colorés. |
| Tables | Pagination, filtres, tri. |
| Scan | Prise en charge des lecteurs code-barres/QR. |
| Temps réel | Notifications via SignalR. |
| Responsive | Interfaces utilisables sur desktop et tablette. |
| Droits | Masquer les actions non autorisées selon le rôle. |

---

### 3.20.3 Règles backend .NET

| Règle | Description |
|---|---|
| Architecture | Core, Service, Api. |
| Héritage | `Service : Repository, IService`. |
| Soft delete | `IsDeleted = true`. |
| Logging | `ILoggerManager` uniquement. |
| DTO Request | Valeurs numériques/monétaires en `string`. |
| Mapping | Manuel via `ToEntity()`. |
| Response | Constructeur depuis entité. |
| Exceptions | `try/catch` dans les contrôleurs. |
| HTTP | Ne jamais retourner `StatusCode(500)` directement. |
| Jobs | Hangfire pour traitements planifiés. |
| Temps réel | SignalR pour notifications. |
| Bulk | `BulkInsertAsync` / `BulkUpdateAsync` pour opérations massives. |

---

## 3.21 Matrice de priorisation des user stories

| Module | Priorité | Justification |
|---|---|---|
| Produits, clients, fournisseurs | Haute | Données de référence indispensables. |
| Commandes fournisseurs | Haute | Cœur du flux d’achat. |
| Transport et expédition | Haute | Import international essentiel. |
| Réception et lots | Haute | Traçabilité pharmaceutique obligatoire. |
| Stock et emplacements | Haute | Gestion physique indispensable. |
| Ventes clients | Haute | Cœur du chiffre d’affaires. |
| Préparation et livraison | Haute | Flux opérationnel quotidien. |
| Facturation | Haute | Suivi financier obligatoire. |
| Pricing | Haute | Spécificité LABMEDIS. |
| Prévisions | Moyenne à haute | Très demandé mais peut être itératif. |
| Retours | Moyenne | Important mais peut venir après flux principal. |
| Inventaire | Moyenne | Important pour fiabilité stock. |
| Notifications | Moyenne | Améliore l’expérience opérationnelle. |
| Reporting avancé | Moyenne | Pilotage direction. |

---

## 3.22 Points à valider avec LABMEDIS

Avant développement définitif, les points suivants doivent être confirmés :

| Point à valider | Impact |
|---|---|
| Les médicaments sont-ils toujours exonérés de TVA ? | Facturation et pricing. |
| Les réactifs HORIBA sont-ils soumis à TVA ? | Facturation. |
| Faut-il gérer plusieurs entrepôts ? | Architecture stock. |
| Faut-il gérer la chaîne du froid ? | Emplacements et alertes. |
| Faut-il valider les prix par workflow ? | Module pricing. |
| Faut-il gérer les acomptes fournisseurs ? | Achats et finances. |
| Faut-il gérer les remises commerciales ? | Ventes. |
| Faut-il gérer les livraisons partielles ? | Ventes et stock. |
| Faut-il gérer les numéros de facture séquentiels ? | Comptabilité. |
| Faut-il intégrer un logiciel comptable ? | Export et intégration. |
| Faut-il une application mobile/PDA ? | Interface magasinier. |
| Faut-il imprimer des étiquettes ? | Module impression. |
| Faut-il bloquer automatiquement les lots proches péremption ? | Règle qualité. |
| Faut-il plusieurs niveaux de validation pour les ajustements ? | Sécurité. |

---

## 3.23 Synthèse

Les workflows opérationnels de LABMEDIS doivent couvrir un flux complet :

```text
Données de référence
    → Achats internationaux
    → Transport
    → Réception
    → Lots et péremptions
    → Stock et emplacements
    → Pricing
    → Ventes
    → Préparation
    → Livraison
    → Facturation
    → Retours
    → Prévisions
```

Chaque étape doit être :

- tracée,
- sécurisée,
- historisée,
- journalisée,
- associée à des rôles,
- visible dans le frontend React,
- implémentée dans le backend .NET selon l’architecture imposée.

Cette base permet ensuite de définir précisément les entités, endpoints API, services, DTOs, règles de validation et écrans React à développer.

### USER
ecris moi le 4. Moteur d'Anticipation (MRP) & Prévisions

### ASSISTANT
# 4. 🧠 Moteur d’Anticipation (MRP) & Prévisions

## 4.1 Objectif du module

Le module **Moteur d’Anticipation (MRP) & Prévisions** doit permettre à LABMEDIS d’anticiper les ruptures de stock et de déclencher les commandes fournisseurs au bon moment.

Dans le contexte LABMEDIS, cette anticipation est critique car :

- les produits sont achetés auprès de fournisseurs internationaux,
- les délais de fabrication peuvent être longs,
- les délais de transport international peuvent être importants,
- le transit douanier peut ajouter des délais supplémentaires,
- certains produits arrivent par bateau, avion ou express,
- la rupture d’un produit pharmaceutique ou infantile peut avoir un impact commercial fort.

Le PRD audio mentionne clairement ce besoin :

> Il faut pouvoir estimer qu’un produit risque de finir et commencer déjà par commander ce produit. Pour commander ce produit, il faut peut-être 3 mois ou 4 mois d’avance afin que le fabricant puisse fabriquer et livrer.

Le module devra donc répondre aux questions suivantes :

1. Quels produits risquent une rupture ?
2. Dans combien de jours la rupture peut-elle arriver ?
3. Quand faut-il déclencher la commande ?
4. Quelle quantité faut-il commander ?
5. Quel mode de transport est le plus pertinent ?
6. Quel fournisseur faut-il solliciter ?
7. La commande doit-elle être urgente ?

---

## 4.2 Périmètre fonctionnel

Le module couvre :

| Fonctionnalité | Description |
|---|---|
| Analyse du stock disponible | Stock physique, réservé, quarantaine, périmé. |
| Analyse du stock en transit | Commandes fournisseurs non encore reçues. |
| Analyse des ventes historiques | Ventes réelles sur 30, 60, 90, 180 ou 365 jours. |
| Calcul de la consommation moyenne | Moyenne simple, pondérée ou saisonnalisée. |
| Calcul des délais fournisseurs | Fabrication, transport, transit, réception, contrôle qualité. |
| Calcul du point de commande | Seuil déclenchant une suggestion de commande. |
| Calcul du besoin net | Quantité manquante projetée. |
| Calcul de la quantité suggérée | Quantité à commander. |
| Simulation de réapprovisionnement | Simulation avant commande réelle. |
| Suggestions de commande | Génération automatique de propositions. |
| Transformation en commande fournisseur | Conversion d’une suggestion en commande d’achat. |
| Alertes de rupture | Notification des produits critiques. |
| Suivi des prévisions | Historique des prévisions et des décisions. |

---

## 4.3 Concepts métier principaux

### 4.3.1 Produit prévisionnel

Un produit prévisionnel est un produit suivi par le moteur MRP.

Exemples :

- France Lait 1er âge 400g
- France Lait 2ème âge 900g
- ALLERGICA 10MG CPR B/30
- GRIPEX Adulte sans sucre
- PECTOLYSE ADULTE SANS SUCRE
- ABX PENTRA UREA CP
- B-PROTEI ALL 200g

Pour chaque produit, le système doit connaître :

| Donnée | Description |
|---|---|
| Stock disponible | Quantité vendable actuellement. |
| Stock réservé | Quantité réservée pour commandes clients. |
| Stock en quarantaine | Quantité non vendable. |
| Stock périmé | Quantité non vendable. |
| Stock en transit | Quantité commandée mais non reçue. |
| Ventes historiques | Historique des sorties. |
| Consommation moyenne | Vitesse de consommation du produit. |
| Délai fournisseur | Délai de fabrication et livraison. |
| Délai transport | Maritime, aérien, express, terrestre. |
| Délai transit/douane | Délai estimé de dédouanement. |
| Délai interne | Réception, contrôle qualité, mise en stock. |
| Stock de sécurité | Stock minimum de protection. |
| Couverture cible | Nombre de jours de stock souhaité. |
| Seuil de réapprovisionnement | Point de commande. |

---

### 4.3.2 Horizon de prévision

L’horizon de prévision est la période sur laquelle le système projette le stock.

Exemples :

| Horizon | Usage |
|---|---|
| 30 jours | Analyse court terme. |
| 60 jours | Analyse opérationnelle. |
| 90 jours | Analyse de réapprovisionnement standard. |
| 180 jours | Analyse semi-annuelle. |
| 365 jours | Analyse annuelle et saisonnalité. |

Pour LABMEDIS, un horizon recommandé est :

```text
Horizon MRP par défaut : 180 jours
```

Cela permet d’anticiper les commandes internationales qui peuvent nécessiter 3 à 4 mois d’avance.

---

### 4.3.3 Lead time total

Le lead time total est le délai complet entre la décision de commander et la disponibilité du produit dans l’entrepôt LABMEDIS.

Il doit être décomposé en plusieurs étapes :

```text
Lead time total =
    Délai de fabrication
  + Délai de préparation fournisseur
  + Délai de transport international
  + Délai de transit / douane
  + Délai de réception entrepôt
  + Délai de contrôle qualité
  + Délai de mise en stock
```

Exemple :

| Étape | Délai |
|---|---:|
| Fabrication | 45 jours |
| Préparation fournisseur | 5 jours |
| Transport maritime | 40 jours |
| Transit / douane | 15 jours |
| Réception entrepôt | 3 jours |
| Contrôle qualité | 2 jours |
| Mise en stock | 2 jours |
| **Lead time total** | **112 jours** |

---

### 4.3.4 Modes de transport et délais

Le mode de transport influence fortement le MRP.

| Mode | Délai estimé | Coût estimé | Usage recommandé |
|---|---:|---:|---|
| Maritime | Long | Faible | Commandes volumineuses non urgentes. |
| Aérien | Moyen | Élevé | Produits critiques ou urgents. |
| Express | Court | Très élevé | Rupture imminente, petites quantités. |
| Terrestre | Variable | Moyen | Fournisseurs régionaux. |

Exemple fournisseur :

| Fournisseur | Pays | Mode probable |
|---|---|---|
| Continental Commodities | France | Maritime ou aérien |
| HORIBA ABX SAS | France | Aérien ou express |
| GALPHARMA | Tunisie | Maritime ou terrestre/aérien |
| IBERMA | Maroc | Maritime ou terrestre |
| B&B LIFE SCIENCE | Inde | Maritime ou aérien |
| BIORESEARCH | Suisse | Aérien |
| MAIA AFRICA SAS | Burkina Faso | Terrestre |
| DEO GRATIAS PHARMA | Togo | Terrestre local |

---

### 4.3.5 Stock disponible

Le stock disponible est la quantité réellement vendable.

Formule :

```text
Stock disponible =
    Stock physique
  - Stock réservé
  - Stock en quarantaine
  - Stock périmé
```

Le moteur MRP ne doit jamais se baser uniquement sur le stock physique.

---

### 4.3.6 Stock en transit

Le stock en transit correspond aux quantités déjà commandées mais non encore reçues.

Formule :

```text
Stock en transit =
    Quantités sur commandes fournisseurs validées
  + Quantités sur expéditions en cours
  - Quantités déjà reçues
```

Le moteur doit pouvoir distinguer :

| Statut | Effet sur MRP |
|---|---|
| Commande brouillon | Non inclus. |
| Commande validée | Inclus. |
| Commande expédiée | Inclus. |
| Commande en transit | Inclus. |
| Commande partiellement reçue | Inclus pour le reste. |
| Commande annulée | Exclu. |

---

### 4.3.7 Consommation moyenne

La consommation moyenne est la vitesse à laquelle un produit sort du stock.

Elle peut être calculée à partir :

- des ventes,
- des sorties de stock,
- des livraisons clients,
- des commandes clients confirmées.

Formule simple :

```text
Consommation moyenne journalière =
    Quantité vendue sur la période / Nombre de jours de la période
```

Exemple :

| Produit | Ventes 90 jours | Consommation journalière |
|---|---:|---:|
| France Lait 1er âge 400g | 1 800 | 20 unités/jour |
| ALLERGICA 10MG CPR B/30 | 540 | 6 unités/jour |
| Pommade Maïa 100 ml | 1 440 | 16 unités/jour |

---

## 4.4 Formules principales du moteur MRP

### 4.4.1 Couverture de stock actuelle

La couverture de stock indique combien de jours le stock disponible peut tenir.

Formule :

```text
Couverture de stock =
    Stock disponible / Consommation moyenne journalière
```

Exemple :

```text
Stock disponible = 1 200
Consommation moyenne = 20 unités/jour

Couverture = 1 200 / 20 = 60 jours
```

Interprétation :

| Couverture | Statut |
|---:|---|
| Inférieure au lead time | Risque élevé de rupture. |
| Entre lead time et lead time + 15 jours | Risque modéré. |
| Supérieure à lead time + 30 jours | Stock confortable. |
| Supérieure à couverture maximale | Surstock potentiel. |

---

### 4.4.2 Point de commande

Le point de commande est le seuil qui doit déclencher une commande.

Formule :

```text
Point de commande =
    Consommation moyenne journalière × Lead time total
  + Stock de sécurité
```

Exemple :

| Paramètre | Valeur |
|---|---:|
| Consommation moyenne | 20 unités/jour |
| Lead time total | 110 jours |
| Stock de sécurité | 300 unités |

Calcul :

```text
Point de commande = 20 × 110 + 300 = 2 500 unités
```

Si :

```text
Stock disponible + Stock en transit <= Point de commande
```

alors le système déclenche une suggestion de commande.

---

### 4.4.3 Stock cible

Le stock cible est le niveau de stock que LABMEDIS souhaite atteindre après réception de la commande.

Formule :

```text
Stock cible =
    Consommation moyenne journalière × Couverture cible
  + Stock de sécurité
```

Exemple :

| Paramètre | Valeur |
|---|---:|
| Consommation moyenne | 20 unités/jour |
| Couverture cible | 120 jours |
| Stock de sécurité | 300 unités |

Calcul :

```text
Stock cible = 20 × 120 + 300 = 2 700 unités
```

---

### 4.4.4 Besoin net

Le besoin net représente la quantité manquante projetée.

Formule :

```text
Besoin net =
    Stock cible
  - Stock disponible
  - Stock en transit
```

Si :

```text
Besoin net > 0
```

alors une commande est nécessaire.

Si :

```text
Besoin net <= 0
```

alors aucune commande n’est nécessaire.

Exemple :

| Élément | Quantité |
|---|---:|
| Stock cible | 2 700 |
| Stock disponible | 1 200 |
| Stock en transit | 500 |

Calcul :

```text
Besoin net = 2 700 - 1 200 - 500 = 1 000
```

Le système suggère de commander 1 000 unités.

---

### 4.4.5 Quantité suggérée

La quantité suggérée peut être ajustée selon plusieurs contraintes :

```text
Quantité suggérée =
    Max(Besoin net, Quantité minimale fournisseur)
```

Puis ajustée selon :

| Contrainte | Description |
|---|---|
| Conditionnement | Arrondir au carton complet. |
| Quantité minimale fournisseur | MOQ imposée par le fabricant. |
| Capacité financière | Budget disponible. |
| Capacité de stockage | Limites physiques de l’entrepôt. |
| Péremption | Éviter le surstock sur produits à rotation lente. |
| Mode de transport | Optimisation du remplissage conteneur. |

---

### 4.4.6 Arrondi au conditionnement

Les produits LABMEDIS sont souvent conditionnés en cartons.

Exemples :

| Produit | Conditionnement |
|---|---|
| France Lait 1er âge 400g | carton/12 |
| France Lait 1er âge 900g | carton/6 |
| Pommade Maïa 100 ml | carton/72 |
| ALLERGICA 10MG CPR B/30 | carton/54 |
| GRIPEX Adulte sans sucre B/12 | carton/48 |

Si le besoin net est de 1 000 unités pour `France Lait 1er âge 400g` :

```text
Conditionnement = 12 unités par carton

1 000 / 12 = 83,33 cartons
```

Le système doit arrondir au carton supérieur :

```text
84 cartons
```

Quantité finale :

```text
84 × 12 = 1 008 unités
```

---

### 4.4.7 Date limite de commande

La date limite de commande est la date maximale à laquelle il faut commander pour éviter la rupture.

Formule :

```text
Date limite de commande =
    Date de rupture estimée - Lead time total
```

Exemple :

| Paramètre | Valeur |
|---|---|
| Date du jour | 28/08/2026 |
| Couverture stock actuel | 60 jours |
| Date de rupture estimée | 27/10/2026 |
| Lead time total | 110 jours |
| Date limite de commande | 09/07/2026 |

Si la date limite de commande est déjà dépassée, le système doit marquer le produit comme **urgent**.

---

## 4.5 Workflow du moteur MRP

### 4.5.1 Workflow global

```text
Déclenchement du calcul MRP
        ↓
Récupération des produits actifs suivis MRP
        ↓
Récupération du stock disponible
        ↓
Récupération du stock réservé
        ↓
Récupération du stock en quarantaine
        ↓
Récupération du stock périmé
        ↓
Récupération du stock en transit
        ↓
Récupération des ventes historiques
        ↓
Calcul de la consommation moyenne
        ↓
Application de la saisonnalité éventuelle
        ↓
Récupération des délais fournisseurs
        ↓
Calcul du lead time total
        ↓
Calcul du point de commande
        ↓
Calcul du stock cible
        ↓
Calcul du besoin net
        ↓
Détection des risques de rupture
        ↓
Génération des suggestions de commande
        ↓
Classement par priorité
        ↓
Affichage dans le dashboard React
        ↓
Validation par responsable achats
        ↓
Transformation en commande fournisseur
```

---

### 4.5.2 Déclenchement du MRP

Le moteur peut être déclenché de plusieurs manières :

| Mode | Description |
|---|---|
| Automatique quotidien | Job Hangfire chaque jour. |
| Automatique hebdomadaire | Job Hangfire chaque lundi. |
| Manuel | Bouton “Lancer le calcul MRP” dans React. |
| Sur événement | Après réception importante, vente exceptionnelle ou rupture. |
| Simulation | Test sans génération de suggestion réelle. |

Recommandation :

```text
Job quotidien à 05h00 UTC
```

---

## 4.6 Calcul de la consommation historique

### 4.6.1 Sources de données

La consommation peut provenir de :

1. Commandes clients livrées.
2. Mouvements de stock de type vente.
3. Bons de livraison validés.
4. Factures.
5. Retours clients déduits.

Formule recommandée :

```text
Consommation nette =
    Sorties vente
  - Retours clients acceptés
```

---

### 4.6.2 Périodes recommandées

| Période | Usage |
|---|---|
| 30 jours | Analyse très court terme. |
| 60 jours | Analyse court terme. |
| 90 jours | Base recommandée pour MRP. |
| 180 jours | Analyse tendance. |
| 365 jours | Analyse saisonnalité. |

---

### 4.6.3 Moyenne simple

```text
Consommation journalière = Quantité vendue / Nombre de jours
```

Exemple :

```text
1 800 unités vendues sur 90 jours

1 800 / 90 = 20 unités/jour
```

---

### 4.6.4 Moyenne pondérée

Pour donner plus de poids aux ventes récentes :

| Période | Pondération |
|---|---:|
| 30 derniers jours | 50% |
| 31 à 60 jours | 30% |
| 61 à 90 jours | 20% |

Formule :

```text
Consommation pondérée =
    (Conso période 1 × 50%)
  + (Conso période 2 × 30%)
  + (Conso période 3 × 20%)
```

---

### 4.6.5 Gestion des ventes exceptionnelles

Certaines ventes peuvent fausser la prévision.

Exemple :

- grosse commande exceptionnelle CAMEG,
- appel d’offres,
- commande ponctuelle d’une clinique,
- livraison importante à LABOREX TOGO.

Le système doit permettre :

1. de marquer une vente comme exceptionnelle,
2. de l’exclure du calcul MRP,
3. de la réintégrer manuellement,
4. de lisser la consommation sur plusieurs mois.

---

## 4.7 Gestion des délais fournisseurs

### 4.7.1 Paramètres par fournisseur

Chaque fournisseur doit pouvoir avoir des délais configurables.

Exemple :

| Fournisseur | Fabrication | Transport maritime | Transport aérien | Transit |
|---|---:|---:|---:|---:|
| Continental Commodities | 45 j | 40 j | 7 j | 15 j |
| HORIBA ABX SAS | 20 j | 35 j | 5 j | 10 j |
| GALPHARMA | 30 j | 25 j | 5 j | 10 j |
| B&B LIFE SCIENCE | 60 j | 45 j | 10 j | 20 j |
| MAIA AFRICA SAS | 15 j | - | - | 5 j |
| DEO GRATIAS PHARMA | 7 j | - | - | 0 j |

---

### 4.7.2 Paramètres par produit

Certains produits peuvent avoir des délais spécifiques.

Exemple :

| Produit | Délai spécifique |
|---|---|
| Réactifs HORIBA | Délai technique, contrôle qualité particulier. |
| France Lait | Production par lot, conteneur. |
| GRIPEX | Produit saisonnier. |
| ABX PENTRA UREA CP | Produit technique de laboratoire. |

---

### 4.7.3 Délai interne LABMEDIS

Le délai interne doit inclure :

| Étape | Délai estimé |
|---|---:|
| Réception physique | 1 à 3 jours |
| Contrôle documentaire | 1 jour |
| Contrôle qualité | 1 à 3 jours |
| Mise en stock | 1 à 2 jours |
| Libération lot | 1 jour |

Valeur par défaut recommandée :

```text
Délai interne = 5 jours
```

---

## 4.8 Niveaux de priorité MRP

Le moteur doit classer les produits selon leur criticité.

### 4.8.1 Formule de criticité

```text
Jours avant rupture =
    Stock disponible / Consommation moyenne journalière
```

### 4.8.2 Classification recommandée

| Niveau | Condition | Action recommandée |
|---|---|---|
| Critique | Jours avant rupture <= Lead time total | Commander immédiatement. |
| Urgent | Jours avant rupture <= Lead time total + 15 jours | Créer suggestion prioritaire. |
| À surveiller | Jours avant rupture <= Lead time total + 30 jours | Surveiller dans le dashboard. |
| Normal | Jours avant rupture > Lead time total + 30 jours | Pas d’action immédiate. |
| Surstock | Couverture > couverture cible + 60 jours | Alerter sur surstock. |

---

## 4.9 Suggestions de commande

### 4.9.1 Contenu d’une suggestion

Chaque suggestion de commande doit contenir :

| Champ | Description |
|---|---|
| Produit | Produit concerné. |
| Fournisseur | Fournisseur recommandé. |
| Quantité suggérée | Quantité calculée. |
| Conditionnement | Quantité en cartons. |
| Stock disponible | Stock actuel. |
| Stock en transit | Stock déjà commandé. |
| Consommation moyenne | Vitesse de consommation. |
| Lead time total | Délai total estimé. |
| Date limite de commande | Date maximale pour commander. |
| Date de réception estimée | Date estimée après commande. |
| Mode de transport recommandé | Maritime, aérien, express. |
| Niveau de priorité | Critique, urgent, normal. |
| Coût estimé | Estimation du coût d’achat. |
| Statut | Suggestion, validée, convertie, rejetée. |

---

### 4.9.2 Exemple de suggestion

| Champ | Valeur |
|---|---|
| Produit | France Lait 1er âge 400g |
| Fournisseur | CONTINENTAL COMMODITIES |
| Stock disponible | 1 200 |
| Stock en transit | 500 |
| Consommation moyenne | 20 unités/jour |
| Lead time total | 110 jours |
| Point de commande | 2 500 |
| Stock cible | 2 700 |
| Besoin net | 1 000 |
| Conditionnement | carton/12 |
| Quantité suggérée | 1 008 unités |
| Cartons suggérés | 84 |
| Mode recommandé | Maritime |
| Priorité | Urgent |

---

## 4.10 Mode de transport recommandé

Le moteur peut recommander un mode de transport selon l’urgence.

### 4.10.1 Règle simple

```text
Si jours avant rupture < lead time maritime
alors recommander aérien ou express
sinon recommander maritime
```

### 4.10.2 Exemple

Produit : `ALLERGICA 10MG CPR B/30`

| Paramètre | Valeur |
|---|---:|
| Stock disponible | 120 |
| Consommation | 6 unités/jour |
| Jours avant rupture | 20 jours |
| Lead time maritime | 55 jours |
| Lead time aérien | 15 jours |

Décision :

```text
Maritime impossible car rupture avant réception.
Recommandation : aérien.
```

---

## 4.11 Gestion de la saisonnalité

Certains produits peuvent avoir une consommation saisonnière.

Exemples possibles :

| Produit | Saison possible |
|---|---|
| GRIPEX Adulte | Saison grippale. |
| GRIPEX Enfant | Saison grippale. |
| ALLERGICA | Périodes d’allergie. |
| Pommade Maïa | Périodes fortes, anti-moustiques. |
| Strick Out | Périodes de traitement insecticide. |
| France Lait | Consommation régulière mais sensible aux campagnes. |

Le système doit permettre :

1. d’activer un coefficient saisonnier par produit,
2. de définir des périodes hautes et basses,
3. d’appliquer un multiplicateur de consommation,
4. de comparer N-1 si historique disponible.

Formule :

```text
Consommation prévisionnelle =
    Consommation historique × Coefficient saisonnier
```

Exemple :

```text
Consommation historique = 20 unités/jour
Coefficient saisonnier = 1,30

Consommation prévisionnelle = 26 unités/jour
```

---

## 4.12 Gestion du surstock

Le MRP ne doit pas uniquement prévenir les ruptures. Il doit aussi éviter le surstock.

### 4.12.1 Détection du surstock

```text
Si couverture actuelle > couverture cible + seuil surstock
alors alerte surstock
```

Exemple :

| Paramètre | Valeur |
|---|---:|
| Couverture cible | 120 jours |
| Seuil surstock | 60 jours |
| Couverture actuelle | 220 jours |

Alerte :

```text
Surstock détecté : 220 jours de couverture.
```

---

### 4.12.2 Actions possibles

| Action | Description |
|---|---|
| Ne pas recommander commande | Bloquer suggestion automatique. |
| Proposer promotion | Accélérer rotation. |
| Proposer transfert | Déplacer vers un autre canal. |
| Proposer retour fournisseur | Si accord fournisseur. |
| Alerter direction | Décision commerciale ou financière. |

---

## 4.13 Intégration avec les achats

Le moteur MRP doit être connecté au module Achats.

### 4.13.1 Transformation d’une suggestion en commande

Workflow :

```text
Suggestion MRP
    ↓
Validation responsable achats
    ↓
Ajustement quantité si besoin
    ↓
Sélection fournisseur
    ↓
Sélection mode de transport
    ↓
Création commande fournisseur
    ↓
Statut suggestion : Convertie
```

---

### 4.13.2 Règles de conversion

| Règle | Description |
|---|---|
| Une suggestion peut être convertie en commande | Oui. |
| Plusieurs suggestions peuvent être regroupées | Oui, par fournisseur. |
| Une suggestion peut être rejetée | Oui, avec motif. |
| Une suggestion peut être modifiée | Oui, avant conversion. |
| Une suggestion convertie doit être liée à la commande | Oui, traçabilité. |
| Une commande issue du MRP doit conserver l’historique | Oui. |

---

## 4.14 Alertes et notifications

### 4.14.1 Types d’alertes MRP

| Alerte | Déclencheur |
|---|---|
| Risque critique | Rupture estimée avant réception possible. |
| Risque urgent | Point de commande atteint. |
| Produit à surveiller | Couverture proche du seuil. |
| Retard fournisseur | Commande non expédiée à la date prévue. |
| Retard transport | Expédition en retard. |
| Surstock | Couverture excessive. |
| Consommation anormale | Hausse ou baisse brutale des ventes. |
| Suggestion non traitée | Suggestion en attente depuis trop longtemps. |

---

### 4.14.2 Notifications temps réel

Le backend doit utiliser SignalR pour envoyer :

```text
MRP_CALCULATION_COMPLETED
LOW_STOCK_ALERT
CRITICAL_STOCK_ALERT
OVERSTOCK_ALERT
REORDER_SUGGESTION_CREATED
REORDER_SUGGESTION_CONVERTED
SUPPLIER_DELAY_ALERT
```

---

## 4.15 Impact sur le Frontend React

Le frontend doit fournir un espace dédié aux prévisions et au MRP.

---

### 4.15.1 Page Dashboard Prévisions

Cette page doit afficher :

| Indicateur | Description |
|---|---|
| Produits critiques | Produits avec risque de rupture. |
| Produits urgents | Produits à commander rapidement. |
| Produits à surveiller | Produits proches du seuil. |
| Produits en surstock | Produits avec couverture excessive. |
| Suggestions en attente | Suggestions non converties. |
| Commandes en transit | Commandes non reçues. |
| Valeur du stock | Valeur financière du stock. |
| Valeur à commander | Estimation des achats nécessaires. |

---

### 4.15.2 Tableau des produits à risque

Colonnes recommandées :

| Colonne | Description |
|---|---|
| Produit | Désignation. |
| Catégorie | Produit infantile, médicament, etc. |
| Fournisseur | Fournisseur principal. |
| Stock disponible | Quantité vendable. |
| Stock en transit | Quantité commandée. |
| Consommation/jour | Vitesse de sortie. |
| Couverture | Nombre de jours restants. |
| Lead time | Délai total. |
| Point de commande | Seuil de déclenchement. |
| Date limite commande | Date maximale. |
| Priorité | Critique, urgent, normal. |
| Action | Voir détail, créer suggestion, commander. |

---

### 4.15.3 Détail produit prévisionnel

La page détail doit afficher :

1. Informations produit.
2. Stock actuel.
3. Stock réservé.
4. Stock en quarantaine.
5. Stock en transit.
6. Ventes historiques.
7. Graphique de consommation.
8. Paramètres MRP.
9. Délais fournisseurs.
10. Suggestions précédentes.
11. Historique des décisions.

---

### 4.15.4 Graphiques recommandés

| Graphique | Usage |
|---|---|
| Courbe des ventes | Visualiser la consommation. |
| Courbe de projection stock | Visualiser la rupture future. |
| Barres par mois | Analyser la saisonnalité. |
| Jauge de couverture | Visualiser les jours restants. |
| Diagramme par fournisseur | Suivre les achats à prévoir. |
| Histogramme des écarts | Comparer prévision et réel. |

---

### 4.15.5 Simulation MRP

Le frontend doit permettre une simulation avant commande.

Paramètres modifiables :

- consommation moyenne,
- stock de sécurité,
- couverture cible,
- délai fournisseur,
- mode de transport,
- quantité suggérée,
- conditionnement.

Résultats :

- besoin net,
- quantité recommandée,
- date de réception estimée,
- coût estimé,
- impact sur couverture.

---

## 4.16 Impact sur le Backend .NET

Le backend doit implémenter les entités, services, jobs et endpoints nécessaires au moteur MRP.

---

## 4.16.1 Entités principales

Toutes les entités doivent hériter de `BaseEntity`.

### `ForecastParameter`

```csharp
public class ForecastParameter : BaseEntity
{
    public Guid ProductId { get; set; }
    public bool IsMtpEnabled { get; set; }
    public int ForecastHorizonDays { get; set; }
    public int SafetyStock { get; set; }
    public int TargetCoverageDays { get; set; }
    public int ReorderCoverageDays { get; set; }
    public int OverstockThresholdDays { get; set; }
    public ForecastMethod Method { get; set; }
    public decimal? SeasonalityFactor { get; set; }

    public Product Product { get; set; }
}
```

---

### `SupplierLeadTime`

```csharp
public class SupplierLeadTime : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid? ProductId { get; set; }
    public TransportMode TransportMode { get; set; }
    public int ManufacturingLeadTimeDays { get; set; }
    public int PreparationLeadTimeDays { get; set; }
    public int TransportLeadTimeDays { get; set; }
    public int CustomsLeadTimeDays { get; set; }
    public int InternalLeadTimeDays { get; set; }

    public Supplier Supplier { get; set; }
    public Product Product { get; set; }
}
```

---

### `ForecastCalculation`

```csharp
public class ForecastCalculation : BaseEntity
{
    public Guid ProductId { get; set; }
    public DateTime CalculationDate { get; set; }
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }
    public int TransitStock { get; set; }
    public decimal AverageDailyConsumption { get; set; }
    public int LeadTimeDays { get; set; }
    public int SafetyStock { get; set; }
    public int ReorderPoint { get; set; }
    public int TargetStock { get; set; }
    public int NetRequirement { get; set; }
    public int CoverageDays { get; set; }
    public ForecastRiskLevel RiskLevel { get; set; }

    public Product Product { get; set; }
}
```

---

### `ReorderSuggestion`

```csharp
public class ReorderSuggestion : BaseEntity
{
    public Guid ForecastCalculationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SupplierId { get; set; }
    public int SuggestedQuantity { get; set; }
    public int SuggestedCartonQuantity { get; set; }
    public TransportMode SuggestedTransportMode { get; set; }
    public DateTime SuggestedOrderDate { get; set; }
    public DateTime EstimatedReceptionDate { get; set; }
    public SuggestionStatus Status { get; set; }
    public string RejectionReason { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public ForecastCalculation ForecastCalculation { get; set; }
    public Product Product { get; set; }
    public Supplier Supplier { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
}
```

---

## 4.16.2 Enums recommandés

```csharp
public enum ForecastMethod
{
    Average30Days = 0,
    Average60Days = 1,
    Average90Days = 2,
    Weighted90Days = 3,
    Seasonal365Days = 4,
    Manual = 5
}
```

```csharp
public enum ForecastRiskLevel
{
    Normal = 0,
    Watch = 1,
    Urgent = 2,
    Critical = 3,
    Overstock = 4
}
```

```csharp
public enum SuggestionStatus
{
    Pending = 0,
    Validated = 1,
    Converted = 2,
    Rejected = 3,
    Expired = 4
}
```

---

## 4.16.3 Services recommandés

```csharp
IForecastParameterService
ISupplierLeadTimeService
IForecastCalculationService
IReorderSuggestionService
IMrpService
```

Le service principal doit respecter la règle d’architecture :

```csharp
public class MrpService : ForecastCalculationRepository, IMrpService
{
    private readonly ILoggerManager _logger;

    public MrpService(AppDbContext context, ILoggerManager logger) : base(context)
    {
        _logger = logger;
    }
}
```

---

## 4.16.4 Logique de calcul simplifiée

```csharp
public class MrpCalculationResult
{
    public int AvailableStock { get; set; }
    public int TransitStock { get; set; }
    public decimal AverageDailyConsumption { get; set; }
    public int LeadTimeDays { get; set; }
    public int SafetyStock { get; set; }
    public int ReorderPoint { get; set; }
    public int TargetStock { get; set; }
    public int NetRequirement { get; set; }
    public int CoverageDays { get; set; }
    public ForecastRiskLevel RiskLevel { get; set; }
}
```

```csharp
public MrpCalculationResult CalculateProductMrp(
    int availableStock,
    int transitStock,
    decimal averageDailyConsumption,
    int leadTimeDays,
    int safetyStock,
    int targetCoverageDays)
{
    int reorderPoint = Convert.ToInt32(Math.Ceiling(averageDailyConsumption * leadTimeDays)) + safetyStock;

    int targetStock = Convert.ToInt32(Math.Ceiling(averageDailyConsumption * targetCoverageDays)) + safetyStock;

    int netRequirement = targetStock - availableStock - transitStock;

    if (netRequirement < 0)
        netRequirement = 0;

    int coverageDays = averageDailyConsumption > 0
        ? Convert.ToInt32(Math.Floor(availableStock / averageDailyConsumption))
        : int.MaxValue;

    var riskLevel = ForecastRiskLevel.Normal;

    if (coverageDays <= leadTimeDays)
        riskLevel = ForecastRiskLevel.Critical;
    else if (coverageDays <= leadTimeDays + 15)
        riskLevel = ForecastRiskLevel.Urgent;
    else if (coverageDays <= leadTimeDays + 30)
        riskLevel = ForecastRiskLevel.Watch;

    return new MrpCalculationResult
    {
        AvailableStock = availableStock,
        TransitStock = transitStock,
        AverageDailyConsumption = averageDailyConsumption,
        LeadTimeDays = leadTimeDays,
        SafetyStock = safetyStock,
        ReorderPoint = reorderPoint,
        TargetStock = targetStock,
        NetRequirement = netRequirement,
        CoverageDays = coverageDays,
        RiskLevel = riskLevel
    };
}
```

---

## 4.16.5 Endpoints API recommandés

### Paramètres MRP

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/forecast/parameters` | Liste des paramètres produits. |
| GET | `/api/forecast/parameters/{productId}` | Paramètres d’un produit. |
| POST | `/api/forecast/parameters` | Créer paramètres. |
| PUT | `/api/forecast/parameters/{productId}` | Modifier paramètres. |

---

### Calcul MRP

| Méthode | Route | Description |
|---|---|---|
| POST | `/api/forecast/run` | Lancer le calcul MRP global. |
| POST | `/api/forecast/run/{productId}` | Lancer le calcul pour un produit. |
| GET | `/api/forecast/calculations` | Historique des calculs. |
| GET | `/api/forecast/calculations/{productId}` | Dernier calcul d’un produit. |

---

### Produits à risque

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/forecast/products-at-risk` | Produits en risque de rupture. |
| GET | `/api/forecast/critical-products` | Produits critiques. |
| GET | `/api/forecast/overstock-products` | Produits en surstock. |

---

### Suggestions

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reorder-suggestions` | Liste des suggestions. |
| GET | `/api/reorder-suggestions/{id}` | Détail d’une suggestion. |
| POST | `/api/reorder-suggestions` | Créer une suggestion manuelle. |
| POST | `/api/reorder-suggestions/{id}/validate` | Valider une suggestion. |
| POST | `/api/reorder-suggestions/{id}/reject` | Rejeter une suggestion. |
| POST | `/api/reorder-suggestions/{id}/convert-to-purchase-order` | Convertir en commande fournisseur. |

---

### Simulation

| Méthode | Route | Description |
|---|---|---|
| POST | `/api/forecast/simulate` | Simuler un besoin MRP. |
| POST | `/api/forecast/simulate/product/{productId}` | Simuler pour un produit. |

---

## 4.16.6 Exemple de contrôleur

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IMrpService _mrpService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public ForecastController(
        IMrpService mrpService,
        ILoggerManager logger,
        IUserService userService)
    {
        _mrpService = mrpService;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet("products-at-risk")]
    public async Task<IActionResult> GetProductsAtRisk()
    {
        var user = await _userService.GetCurrentUserAsync(User);

        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début GetProductsAtRisk | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try
        {
            var result = await _mrpService.GetProductsAtRiskAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec GetProductsAtRisk : {ex.Message} | IP: {Request.GetIp()}");

            return BadRequest(new
            {
                message = "Impossible de récupérer les produits à risque."
            });
        }
    }
}
```

---

## 4.17 Jobs Hangfire recommandés

### 4.17.1 Job quotidien MRP

```text
DailyMrpCalculationJob
```

Fréquence recommandée :

```text
Tous les jours à 05h00
```

Actions :

1. Récupérer les produits actifs suivis MRP.
2. Calculer les stocks disponibles.
3. Calculer les stocks en transit.
4. Calculer les consommations moyennes.
5. Appliquer les paramètres MRP.
6. Calculer les risques.
7. Générer les suggestions.
8. Envoyer les notifications SignalR.
9. Enregistrer l’historique.

---

### 4.17.2 Job de suivi des commandes en transit

```text
TransitDelayAlertJob
```

Actions :

1. Vérifier les commandes fournisseurs non reçues.
2. Comparer la date de réception estimée avec la date actuelle.
3. Détecter les retards.
4. Alerter les responsables achats/logistique.

---

### 4.17.3 Job de suivi des suggestions non traitées

```text
PendingSuggestionAlertJob
```

Actions :

1. Identifier les suggestions en attente depuis plus de X jours.
2. Envoyer une relance.
3. Marquer certaines suggestions comme expirées si nécessaire.

---

## 4.18 User Stories détaillées

### US-MRP-01 : Configurer les paramètres MRP d’un produit

**En tant que** responsable achats,  
**je veux** configurer les paramètres MRP d’un produit,  
**afin de** contrôler son réapprovisionnement.

**Critères d’acceptation :**

1. L’utilisateur peut activer ou désactiver le MRP sur un produit.
2. L’utilisateur peut définir le stock de sécurité.
3. L’utilisateur peut définir la couverture cible.
4. L’utilisateur peut définir l’horizon de prévision.
5. L’utilisateur peut choisir la méthode de calcul.
6. Les paramètres sont historisés.
7. Le moteur utilise ces paramètres lors du calcul.

---

### US-MRP-02 : Configurer les délais fournisseurs

**En tant que** responsable achats,  
**je veux** définir les délais fournisseurs par mode de transport,  
**afin de** calculer correctement la date limite de commande.

**Critères d’acceptation :**

1. Les délais peuvent être définis par fournisseur.
2. Les délais peuvent être surchargés par produit.
3. Les délais peuvent être définis par mode de transport.
4. Le système calcule automatiquement le lead time total.
5. Les délais sont modifiables.
6. Les modifications sont journalisées.

---

### US-MRP-03 : Lancer un calcul MRP manuel

**En tant que** responsable achats,  
**je veux** lancer un calcul MRP manuel,  
**afin de** mettre à jour les prévisions.

**Critères d’acceptation :**

1. Un bouton permet de lancer le calcul.
2. Le système affiche une progression.
3. Le calcul peut être global ou par produit.
4. Le résultat est affiché dans le dashboard.
5. Le calcul est historisé.
6. Une notification SignalR est envoyée à la fin.

---

### US-MRP-04 : Consulter les produits à risque

**En tant que** direction ou responsable achats,  
**je veux** voir les produits à risque de rupture,  
**afin de** prioriser les commandes.

**Critères d’acceptation :**

1. La liste affiche les produits critiques, urgents et à surveiller.
2. Le tableau peut être filtré par catégorie, fournisseur et priorité.
3. Le tableau affiche la couverture actuelle.
4. Le tableau affiche la date limite de commande.
5. Le tableau affiche la quantité suggérée.
6. L’utilisateur peut accéder au détail du produit.

---

### US-MRP-05 : Voir le détail prévisionnel d’un produit

**En tant que** responsable achats,  
**je veux** voir le détail prévisionnel d’un produit,  
**afin de** comprendre pourquoi une commande est suggérée.

**Critères d’acceptation :**

1. Le détail affiche le stock disponible.
2. Le détail affiche le stock en transit.
3. Le détail affiche la consommation moyenne.
4. Le détail affiche le lead time total.
5. Le détail affiche le point de commande.
6. Le détail affiche le besoin net.
7. Le détail affiche un graphique de projection.
8. Le détail affiche l’historique des suggestions.

---

### US-MRP-06 : Simuler une commande

**En tant que** responsable achats,  
**je veux** simuler une commande,  
**afin de** vérifier l’impact avant validation.

**Critères d’acceptation :**

1. L’utilisateur peut modifier la quantité.
2. L’utilisateur peut modifier le mode de transport.
3. L’utilisateur peut modifier les délais.
4. Le système recalcule la couverture future.
5. Le système affiche une estimation de coût.
6. La simulation n’enregistre pas de commande réelle.

---

### US-MRP-07 : Valider une suggestion de commande

**En tant que** responsable achats,  
**je veux** valider une suggestion de commande,  
**afin de** préparer la commande fournisseur.

**Critères d’acceptation :**

1. La suggestion doit être en statut `Pending`.
2. L’utilisateur peut ajuster la quantité.
3. L’utilisateur peut ajuster le fournisseur.
4. L’utilisateur peut ajuster le mode de transport.
5. La validation change le statut en `Validated`.
6. L’action est journalisée.

---

### US-MRP-08 : Convertir une suggestion en commande fournisseur

**En tant que** responsable achats,  
**je veux** convertir une suggestion en commande fournisseur,  
**afin de** lancer l’achat.

**Critères d’acceptation :**

1. La suggestion validée peut être convertie.
2. Le système crée une commande fournisseur.
3. La commande contient le produit et la quantité.
4. La commande est liée à la suggestion.
5. La suggestion passe au statut `Converted`.
6. La commande est visible dans le module Achats.

---

### US-MRP-09 : Rejeter une suggestion

**En tant que** responsable achats,  
**je veux** rejeter une suggestion,  
**afin de** ne pas commander un produit non nécessaire.

**Critères d’acceptation :**

1. Le rejet nécessite un motif.
2. La suggestion passe au statut `Rejected`.
3. Le motif est enregistré.
4. Le produit reste suivi par le MRP.
5. L’action est journalisée.

---

### US-MRP-10 : Recevoir une alerte de rupture critique

**En tant que** responsable achats ou direction,  
**je veux** recevoir une alerte lorsqu’un produit est critique,  
**afin de** prendre une décision rapide.

**Critères d’acceptation :**

1. L’alerte est envoyée via SignalR.
2. L’alerte peut aussi être envoyée par email si configurée.
3. L’alerte contient le produit, le stock, la couverture et la date limite.
4. L’utilisateur peut cliquer sur l’alerte pour ouvrir le détail.
5. L’alerte est historisée.

---

## 4.19 Exemple complet : France Lait 1er âge 400g

### Données

| Paramètre | Valeur |
|---|---:|
| Produit | France Lait 1er âge 400g |
| Fournisseur | CONTINENTAL COMMODITIES |
| Conditionnement | carton/12 |
| Stock disponible | 1 200 |
| Stock réservé | 150 |
| Stock en transit | 500 |
| Ventes 90 jours | 1 800 |
| Consommation moyenne | 20 unités/jour |
| Délai fabrication | 45 jours |
| Délai transport maritime | 40 jours |
| Délai transit/douane | 15 jours |
| Délai interne | 10 jours |
| Lead time total | 110 jours |
| Stock de sécurité | 300 |
| Couverture cible | 120 jours |

---

### Calcul du stock réellement disponible

```text
Stock physique = 1 200
Stock réservé = 150

Stock réellement disponible = 1 200 - 150 = 1 050
```

---

### Calcul couverture

```text
Couverture = 1 050 / 20 = 52,5 jours
```

---

### Calcul point de commande

```text
Point de commande = 20 × 110 + 300 = 2 500
```

---

### Stock pris en compte

```text
Stock disponible + Stock en transit = 1 050 + 500 = 1 550
```

---

### Déclenchement

```text
1 550 <= 2 500
```

Donc :

```text
Commande recommandée
```

---

### Stock cible

```text
Stock cible = 20 × 120 + 300 = 2 700
```

---

### Besoin net

```text
Besoin net = 2 700 - 1 550 = 1 150
```

---

### Arrondi conditionnement

```text
1 150 / 12 = 95,83 cartons
```

Arrondi :

```text
96 cartons
```

Quantité finale :

```text
96 × 12 = 1 152 unités
```

---

### Résultat

| Résultat | Valeur |
|---|---:|
| Couverture actuelle | 52,5 jours |
| Lead time total | 110 jours |
| Niveau de risque | Critique |
| Quantité suggérée | 1 152 unités |
| Conditionnement | 96 cartons |
| Mode recommandé | Aérien si urgence, maritime si encore possible |
| Action | Commander immédiatement |

---

## 4.20 Règles de validation métier

Le moteur doit respecter les règles suivantes :

| Règle | Description |
|---|---|
| Produit actif | Seuls les produits actifs sont calculés. |
| Produit suivi MRP | Le produit doit avoir le MRP activé. |
| Consommation nulle | Si aucune vente, ne pas suggérer commande automatique. |
| Stock négatif | Interdire les calculs avec stock négatif. |
| Délais manquants | Signaler les produits sans délais configurés. |
| Fournisseur manquant | Signaler les produits sans fournisseur principal. |
| Commande en cours | Ne pas proposer commande si besoin déjà couvert. |
| Surstock | Ne pas suggérer commande si couverture excessive. |
| Quantité minimale | Respecter le MOQ fournisseur si défini. |
| Conditionnement | Arrondir au carton supérieur si obligatoire. |

---

## 4.21 KPIs du module MRP

Le système doit fournir des indicateurs de performance :

| KPI | Description |
|---|---|
| Nombre de produits critiques | Produits en risque immédiat. |
| Nombre de produits urgents | Produits à commander rapidement. |
| Nombre de suggestions générées | Volume de suggestions MRP. |
| Suggestions converties | Suggestions transformées en commande. |
| Suggestions rejetées | Suggestions non retenues. |
| Taux de rupture | Nombre de ruptures réelles. |
| Taux de service | Capacité à servir les clients sans rupture. |
| Précision des prévisions | Écart entre prévision et consommation réelle. |
| Couverture moyenne | Nombre moyen de jours de stock. |
| Valeur du surstock | Valeur immobilisée inutilement. |
| Valeur à commander | Montant estimé des achats nécessaires. |
| Délai moyen fournisseur | Délai réel constaté. |

---

## 4.22 Points à valider avec LABMEDIS

Avant développement complet, les points suivants doivent être confirmés :

| Question | Impact |
|---|---|
| Quelle est la période de vente de référence recommandée ? | Calcul de consommation. |
| Faut-il utiliser 30, 60, 90 ou 180 jours ? | Précision MRP. |
| Faut-il une moyenne pondérée ? | Prévision plus fine. |
| Faut-il gérer la saisonnalité ? | Produits comme GRIPEX. |
| Faut-il un stock de sécurité par produit ou par catégorie ? | Paramétrage. |
| Faut-il une couverture cible différente par produit ? | Paramétrage. |
| Faut-il bloquer les commandes si surstock ? | Règle métier. |
| Faut-il suggérer automatiquement le mode de transport ? | Aide à la décision. |
| Faut-il gérer plusieurs fournisseurs pour un même produit ? | Flexibilité achat. |
| Faut-il gérer les quantités minimales fournisseur ? | Arrondi quantité. |
| Faut-il convertir plusieurs suggestions en une seule commande ? | Regroupement fournisseur. |
| Faut-il notifier par email en plus de SignalR ? | Alertes. |
| Faut-il exporter les suggestions en Excel ? | Reporting. |
| Faut-il comparer prévision et consommation réelle ? | Amélioration continue. |

---

## 4.23 Synthèse

Le module **Moteur d’Anticipation (MRP) & Prévisions** doit permettre à LABMEDIS de passer d’une gestion réactive à une gestion proactive des approvisionnements.

Il doit répondre automatiquement à :

```text
Quel produit commander ?
Quand commander ?
Quelle quantité commander ?
Chez quel fournisseur ?
Par quel mode de transport ?
Quel est le risque de rupture ?
Quel est l’impact sur le stock futur ?
```

Le module devra être intégré :

- au stock, pour connaître la disponibilité réelle,
- aux achats, pour transformer les suggestions en commandes,
- au transport, pour estimer les délais,
- aux ventes, pour analyser la consommation,
- au frontend React, pour fournir un dashboard décisionnel,
- au backend .NET, via des services robustes, des jobs Hangfire et des notifications SignalR.

### USER
ecris moi le 5. Sécurité, Rôles (RBAC) & Intégrations

### ASSISTANT
# 5. 🔐 Sécurité, Rôles (RBAC) & Intégrations

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

### USER
ecris le 6. Reporting & Tableaux de Bord (KPIs)

### ASSISTANT
# 6. 📊 Reporting & Tableaux de Bord (KPIs)

## 6.1 Objectif du module

Le module **Reporting & Tableaux de Bord** doit permettre à LABMEDIS de piloter l’activité à partir d’indicateurs fiables, actualisés et exploitables.

Il doit répondre aux besoins suivants :

1. Donner à la direction une vision globale de l’activité.
2. Permettre aux achats d’anticiper les ruptures et les commandes internationales.
3. Donner à la logistique une visibilité sur les conteneurs, frets aériens, express et transits.
4. Permettre au magasin de suivre les lots, péremptions, emplacements et inventaires.
5. Donner aux commerciaux une vision des ventes, clients, marges et disponibilités.
6. Permettre à la comptabilité de suivre les prix, marges, TVA, factures et avoirs.
7. Fournir des indicateurs de conformité pharmaceutique : lots, quarantaine, péremptions, destructions, traçabilité.

Le reporting doit être connecté aux modules :

- Achats internationaux,
- Transport et transit,
- Réception,
- Stock et lots,
- Ventes clients,
- Pricing,
- MRP / Prévisions,
- Qualité,
- Finance,
- Sécurité / Audit.

---

## 6.2 Utilisateurs cibles et besoins de reporting

| Profil | Objectif principal | Indicateurs attendus |
|---|---|---|
| Direction | Pilotage global | CA, marge, stock, ruptures, achats, trésorerie. |
| Responsable achats | Anticiper les commandes | Ruptures, suggestions MRP, délais fournisseurs, commandes en cours. |
| Responsable logistique | Suivre les flux internationaux | Conteneurs, expéditions, retards, transit, réception. |
| Magasinier | Gérer l’entrepôt | Emplacements, lots, péremptions, inventaires, écarts. |
| Responsable qualité | Sécuriser les produits | Quarantaine, lots non conformes, péremptions, destructions. |
| Commercial | Vendre et servir les clients | Ventes, disponibilité, clients, livraisons, retours. |
| Comptable | Suivre la performance financière | Factures, avoirs, TVA, marges, prix de revient. |
| Administrateur | Superviser le système | Utilisateurs, connexions, erreurs, logs, intégrations. |

---

## 6.3 Types de tableaux de bord

Le système doit proposer plusieurs tableaux de bord spécialisés.

### 6.3.1 Dashboard Direction

Objectif : vision globale et décisionnelle.

Indicateurs principaux :

- chiffre d’affaires,
- marge brute,
- valeur du stock,
- produits critiques,
- ruptures potentielles,
- commandes fournisseurs en cours,
- expéditions en transit,
- top produits vendus,
- top clients,
- alertes qualité,
- trésorerie estimée si disponible.

---

### 6.3.2 Dashboard Achats & Import

Objectif : piloter les achats internationaux.

Indicateurs principaux :

- commandes fournisseurs en cours,
- commandes validées non expédiées,
- commandes expédiées non reçues,
- commandes en retard,
- valeur des achats en devises,
- valeur des achats convertis en CFA,
- délai moyen fournisseur,
- taux de réception à temps,
- écarts de réception,
- suggestions MRP à traiter.

---

### 6.3.3 Dashboard Logistique & Transport

Objectif : suivre les flux physiques internationaux.

Indicateurs principaux :

- expéditions en cours,
- conteneurs en transit,
- fret aérien en cours,
- express en cours,
- retard de transport,
- délais moyens par mode de transport,
- coûts logistiques par expédition,
- coûts logistiques par produit,
- écarts entre dates estimées et dates réelles.

---

### 6.3.4 Dashboard Stock & Entrepôt

Objectif : piloter la disponibilité physique.

Indicateurs principaux :

- valeur du stock,
- stock par catégorie,
- stock par fournisseur,
- stock par emplacement,
- stock disponible,
- stock réservé,
- stock en quarantaine,
- stock périmé,
- lots proches péremption,
- produits à rotation lente,
- produits en surstock,
- écarts d’inventaire.

---

### 6.3.5 Dashboard Ventes

Objectif : suivre l’activité commerciale.

Indicateurs principaux :

- chiffre d’affaires HT,
- chiffre d’affaires TTC,
- TVA collectée,
- volume vendu par produit,
- volume vendu par catégorie,
- ventes par client,
- ventes par répartiteur,
- ventes par ville,
- livraisons en cours,
- commandes non livrées,
- retours clients,
- taux de service.

---

### 6.3.6 Dashboard Pricing & Finance

Objectif : contrôler la rentabilité.

Indicateurs principaux :

- prix d’achat moyen,
- prix de revient moyen,
- marge théorique,
- marge réelle,
- écart entre prix calculé et prix pratiqué,
- rentabilité par produit,
- rentabilité par catégorie,
- rentabilité par fournisseur,
- rentabilité par conteneur,
- rentabilité par mode de transport,
- valeur des factures,
- valeur des avoirs.

---

### 6.3.7 Dashboard MRP & Prévisions

Objectif : anticiper les ruptures.

Indicateurs principaux :

- produits critiques,
- produits urgents,
- produits à surveiller,
- produits en surstock,
- couverture de stock,
- point de commande atteint,
- suggestions en attente,
- suggestions converties,
- suggestions rejetées,
- précision des prévisions,
- date limite de commande.

---

### 6.3.8 Dashboard Qualité & Conformité

Objectif : sécuriser les produits pharmaceutiques.

Indicateurs principaux :

- lots en quarantaine,
- lots libérés,
- lots non conformes,
- lots périmés,
- lots proches péremption,
- produits détruits,
- retours clients non conformes,
- historique de traçabilité,
- lots ayant fait l’objet d’un rappel.

---

## 6.4 KPIs stratégiques Direction

### 6.4.1 Chiffre d’affaires HT

Formule :

```text
Chiffre d’affaires HT =
    Somme des lignes de facture HT
```

ou :

```text
Chiffre d’affaires HT =
    Somme des livraisons validées × Prix de vente HT
```

Périodes recommandées :

- jour,
- semaine,
- mois,
- trimestre,
- année.

---

### 6.4.2 Chiffre d’affaires TTC

Formule :

```text
Chiffre d’affaires TTC =
    Chiffre d’affaires HT + TVA
```

Exemple produit soumis à TVA 18% :

```text
Prix HT = 3 660 CFA
TVA = 18%

Prix TTC = 3 660 × 1,18 = 4 318,8 CFA
```

---

### 6.4.3 Marge brute

Formule :

```text
Marge brute =
    Chiffre d’affaires HT - Coût de revient des produits vendus
```

Taux de marge :

```text
Taux de marge =
    Marge brute / Chiffre d’affaires HT × 100
```

---

### 6.4.4 Valeur du stock

Formule :

```text
Valeur du stock =
    Somme(Quantité disponible × Prix de revient unitaire)
```

La valeur du stock peut être affichée :

- par produit,
- par catégorie,
- par fournisseur,
- par emplacement,
- par lot,
- par mode de transport.

---

### 6.4.5 Nombre de produits critiques

Formule :

```text
Produits critiques =
    Nombre de produits dont la couverture actuelle <= lead time total
```

---

### 6.4.6 Taux de rupture

Formule :

```text
Taux de rupture =
    Nombre de produits en rupture / Nombre total de produits actifs × 100
```

---

### 6.4.7 Taux de service client

Formule :

```text
Taux de service =
    Commandes livrées complètes / Total commandes clients × 100
```

Variante :

```text
Taux de service quantité =
    Quantité livrée / Quantité commandée × 100
```

---

## 6.5 KPIs Achats & Import

### 6.5.1 Nombre de commandes fournisseurs

Formule :

```text
Nombre de commandes =
    Commandes créées sur une période
```

Ventilation :

- brouillon,
- validée,
- envoyée,
- en fabrication,
- expédiée,
- en transit,
- partiellement reçue,
- reçue,
- close,
- annulée.

---

### 6.5.2 Valeur des achats

Formule :

```text
Valeur des achats =
    Somme(Quantité × Prix unitaire en devise)
```

Contre-valeur CFA :

```text
Valeur des achats CFA =
    Somme(Quantité × Prix unitaire devise × Taux de change)
```

Exemple :

| Produit | PA Euro | Quantité | Total Euro |
|---|---:|---:|---:|
| France Lait 1er âge 400g | 3,41 | 1 200 | 4 092 € |
| France Lait 2ème âge 900g | 7,43 | 300 | 2 229 € |

Total :

```text
6 321 €
```

Si taux de change :

```text
1 EUR = 656 CFA
```

Alors :

```text
6 321 × 656 = 4 146 576 CFA
```

---

### 6.5.3 Délai moyen fournisseur

Formule :

```text
Délai moyen fournisseur =
    Somme(Date réception réelle - Date commande) / Nombre de commandes reçues
```

---

### 6.5.4 Taux de réception à temps

Formule :

```text
Taux de réception à temps =
    Commandes reçues à la date prévue / Commandes reçues × 100
```

---

### 6.5.5 Taux d’écart de réception

Formule :

```text
Taux d’écart =
    Réceptions avec écart / Total réceptions × 100
```

Types d’écart :

- manquant,
- excédent,
- produit endommagé,
- lot non conforme,
- péremption courte,
- produit non commandé.

---

### 6.5.6 Commandes en retard

Formule :

```text
Commandes en retard =
    Commandes non reçues dont la date prévue de réception est dépassée
```

---

### 6.5.7 Suggestions MRP en attente

Formule :

```text
Suggestions en attente =
    Suggestions MRP non converties et non rejetées
```

---

## 6.6 KPIs Logistique & Transport

### 6.6.1 Nombre d’expéditions en cours

Formule :

```text
Expéditions en cours =
    Expéditions non arrivées à l’entrepôt
```

Ventilation :

- maritime,
- aérienne,
- express,
- terrestre.

---

### 6.6.2 Valeur du stock en transit

Formule :

```text
Valeur stock en transit =
    Somme(Quantités expédiées non reçues × Prix de revient estimé)
```

---

### 6.6.3 Coût logistique par expédition

Formule :

```text
Coût logistique =
    Freight
  + Transit
  + Douane
  + Frais transfert
  + Assurance
  + Manutention
  + Autres frais
```

---

### 6.6.4 Coût logistique par produit

Formule :

```text
Coût logistique unitaire =
    Coût logistique total alloué / Quantité reçue
```

---

### 6.6.5 Délai moyen par mode de transport

Formule :

```text
Délai moyen transport =
    Somme(Date arrivée réelle - Date expédition réelle) / Nombre d’expéditions
```

---

### 6.6.6 Taux de retard transport

Formule :

```text
Taux de retard transport =
    Expéditions arrivées en retard / Total expéditions × 100
```

---

## 6.7 KPIs Stock & Entrepôt

### 6.7.1 Stock disponible par produit

Formule :

```text
Stock disponible =
    Stock physique
  - Stock réservé
  - Stock quarantaine
  - Stock périmé
```

---

### 6.7.2 Valeur du stock par catégorie

Formule :

```text
Valeur stock catégorie =
    Somme des quantités disponibles × Prix de revient
```

Exemple de catégories :

- produit infantile,
- médicament,
- cosmétique,
- complément alimentaire,
- insecticide,
- réactifs de laboratoire.

---

### 6.7.3 Couverture de stock

Formule :

```text
Couverture de stock =
    Stock disponible / Consommation moyenne journalière
```

---

### 6.7.4 Rotation de stock

Formule :

```text
Rotation de stock =
    Coût des marchandises vendues / Valeur moyenne du stock
```

Variante en quantité :

```text
Rotation quantité =
    Quantité vendue / Quantité moyenne en stock
```

---

### 6.7.5 Produits à rotation lente

Formule :

```text
Produits à rotation lente =
    Produits sans sortie depuis X jours
```

Seuils recommandés :

- 30 jours,
- 60 jours,
- 90 jours,
- 180 jours.

---

### 6.7.6 Lots proches péremption

Formule :

```text
Lots proches péremption =
    Lots actifs dont la date de péremption est dans 30, 60, 90 ou 120 jours
```

---

### 6.7.7 Valeur des lots proches péremption

Formule :

```text
Valeur lots proches péremption =
    Somme(Quantité restante × Prix de revient)
```

---

### 6.7.8 Taux de démarque

Formule :

```text
Taux de démarque =
    Valeur des pertes, destructions et ajustements négatifs / Valeur du stock × 100
```

---

### 6.7.9 Précision d’inventaire

Formule :

```text
Précision inventaire =
    Lignes comptées conformes / Total lignes comptées × 100
```

ou :

```text
Écart inventaire =
    Quantité comptée - Quantité système
```

---

## 6.8 KPIs Ventes

### 6.8.1 Ventes par période

Formule :

```text
Ventes période =
    Somme des lignes de commandes livrées ou facturées
```

Périodes :

- jour,
- semaine,
- mois,
- trimestre,
- année.

---

### 6.8.2 Ventes par produit

Formule :

```text
Ventes produit =
    Somme(Quantité vendue × Prix de vente HT)
```

---

### 6.8.3 Ventes par client

Formule :

```text
Ventes client =
    Somme des factures ou livraisons validées par client
```

Exemples de clients :

- CAMEG,
- LABOREX TOGO,
- TEDIS PHARMA TOGO,
- UBIPHARM TOGO,
- DOGTA LAFIE,
- CHP ANEHO,
- CHR SOKODE,
- Clinique Mère et enfant l’étoile,
- Clinique les p’tits anges,
- Groupe Levant Sarl.

---

### 6.8.4 Ventes par catégorie

Formule :

```text
Ventes catégorie =
    Somme des ventes des produits appartenant à la catégorie
```

Exemples :

- produit infantile,
- médicament,
- cosmétique,
- complément alimentaire,
- insecticide,
- réactifs de laboratoire.

---

### 6.8.5 Panier moyen client

Formule :

```text
Panier moyen =
    Chiffre d’affaires total / Nombre de commandes
```

---

### 6.8.6 Taux de retour client

Formule :

```text
Taux de retour =
    Montant des retours / Montant des ventes × 100
```

ou :

```text
Taux de retour quantité =
    Quantité retournée / Quantité vendue × 100
```

---

### 6.8.7 Commandes en attente de livraison

Formule :

```text
Commandes en attente =
    Commandes confirmées non livrées ou partiellement livrées
```

---

## 6.9 KPIs Pricing & Finance

### 6.9.1 Prix de revient unitaire

Formule de base :

```text
Prix de revient unitaire =
    Prix d’achat CFA
  + Frais logistiques alloués
  + Frais de transit alloués
  + Frais transfert alloués
  + Commissions allouées
```

Dans le cas France Lait, le fichier structure de prix utilise des coefficients :

```text
PR CFA =
    PA CFA
  × Commission Promo
  × Freight
  × Transit
  × Frais transfert
```

Exemple :

```text
PA CFA France Lait 1er âge 400g = 2 237
Coefficients = 1,25 × 1,03 × 1,09 × 1,07

PR CFA = 2 237 × 1,25 × 1,03 × 1,09 × 1,07 = 3 359 CFA
```

---

### 6.9.2 Prix de vente théorique

Formule :

```text
PV HT théorique =
    Prix de revient × Coefficient de marge
```

Exemple France Lait :

```text
PR = 3 359
Marge = 1,10

PV HT théorique = 3 359 × 1,10 = 3 695 CFA
```

---

### 6.9.3 Écart prix calculé / prix pratiqué

Formule :

```text
Écart prix =
    Prix Labmedis HT - PV HT calculé
```

Exemple :

| Produit | PV HT calculé | Prix Labmedis HT | Écart |
|---|---:|---:|---:|
| France Lait 1er âge 400g | 3 695 | 3 660 | -35 |
| France Lait 1er âge 900g | 8 050 | 7 915 | -135 |
| France Lait AR | 4 778 | 4 960 | +182 |
| France Lait LF | 4 692 | 4 870 | +178 |

Interprétation :

- écart négatif : prix pratiqué inférieur au prix cible,
- écart positif : prix pratiqué supérieur au prix cible.

---

### 6.9.4 Marge réelle par produit

Formule :

```text
Marge réelle =
    Prix de vente HT - Prix de revient réel
```

Taux de marge :

```text
Taux de marge réelle =
    Marge réelle / Prix de vente HT × 100
```

---

### 6.9.5 Marge par catégorie

Formule :

```text
Marge catégorie =
    CA HT catégorie - Coût de revient catégorie
```

---

### 6.9.6 Marge par fournisseur

Formule :

```text
Marge fournisseur =
    CA HT des produits du fournisseur - Coût de revient des produits du fournisseur
```

---

### 6.9.7 Marge par mode de transport

Formule :

```text
Marge transport =
    CA HT des lots transportés - Coût de revient incluant transport
```

Ventilation :

- maritime,
- aérien,
- express,
- terrestre.

Objectif :

Comparer la rentabilité des produits arrivés par bateau, avion ou express.

---

### 6.9.8 Valeur des factures

Formule :

```text
Valeur factures =
    Somme des factures émises sur une période
```

---

### 6.9.9 Valeur des avoirs

Formule :

```text
Valeur avoirs =
    Somme des avoirs émis sur une période
```

---

### 6.9.10 TVA collectée

Formule :

```text
TVA collectée =
    Somme de la TVA sur factures clients
```

---

### 6.9.11 TVA sur achats

Formule :

```text
TVA déductible / récupérable =
    TVA payée sur achats ou importations, selon règles fiscales applicables
```

Ce point devra être validé avec la comptabilité LABMEDIS.

---

## 6.10 KPIs MRP & Prévisions

### 6.10.1 Nombre de produits à risque

Formule :

```text
Produits à risque =
    Produits critiques + Produits urgents + Produits à surveiller
```

---

### 6.10.2 Couverture moyenne

Formule :

```text
Couverture moyenne =
    Somme des couvertures produits / Nombre de produits suivis
```

---

### 6.10.3 Valeur à commander

Formule :

```text
Valeur à commander =
    Somme des besoins nets × Prix d’achat estimé
```

---

### 6.10.4 Suggestions converties

Formule :

```text
Taux de conversion suggestions =
    Suggestions converties / Suggestions générées × 100
```

---

### 6.10.5 Suggestions rejetées

Formule :

```text
Taux de rejet =
    Suggestions rejetées / Suggestions générées × 100
```

---

### 6.10.6 Précision des prévisions

Formule :

```text
Écart prévision =
    Consommation réelle - Consommation prévue
```

Taux d’erreur :

```text
Taux d’erreur =
    Abs(Consommation réelle - Consommation prévue) / Consommation réelle × 100
```

---

## 6.11 KPIs Qualité & Conformité

### 6.11.1 Lots en quarantaine

Formule :

```text
Lots en quarantaine =
    Lots avec statut quarantaine
```

---

### 6.11.2 Lots libérés

Formule :

```text
Lots libérés =
    Lots avec statut libéré
```

---

### 6.11.3 Lots non conformes

Formule :

```text
Lots non conformes =
    Lots avec statut non conforme
```

---

### 6.11.4 Lots périmés

Formule :

```text
Lots périmés =
    Lots dont la date de péremption est dépassée
```

---

### 6.11.5 Valeur des produits périmés

Formule :

```text
Valeur produits périmés =
    Somme(Quantité périmée × Prix de revient)
```

---

### 6.11.6 Valeur des destructions

Formule :

```text
Valeur destructions =
    Somme(Quantité détruite × Prix de revient)
```

---

### 6.11.7 Taux de conformité réception

Formule :

```text
Taux de conformité =
    Réceptions sans anomalie / Total réceptions × 100
```

---

## 6.12 KPIs Clients

### 6.12.1 Top clients par chiffre d’affaires

Formule :

```text
Top clients =
    Clients classés par CA HT décroissant
```

Exemple de clients à suivre :

- CAMEG,
- LABOREX TOGO,
- TEDIS PHARMA TOGO,
- UBIPHARM TOGO,
- DOGTA LAFIE,
- OCDI,
- Groupe Levant Sarl,
- Cliniques et hôpitaux.

---

### 6.12.2 Nombre de commandes par client

Formule :

```text
Commandes client =
    Nombre de commandes validées par client
```

---

### 6.12.3 Fréquence d’achat

Formule :

```text
Fréquence d’achat =
    Nombre de commandes client / Période
```

---

### 6.12.4 Clients inactifs

Formule :

```text
Clients inactifs =
    Clients actifs sans commande depuis X jours
```

Seuils recommandés :

- 30 jours,
- 60 jours,
- 90 jours,
- 180 jours.

---

### 6.12.5 Retours par client

Formule :

```text
Retours client =
    Somme des quantités ou montants retournés par client
```

---

## 6.13 KPIs Fournisseurs

### 6.13.1 Top fournisseurs par valeur d’achat

Formule :

```text
Top fournisseurs =
    Fournisseurs classés par valeur d’achat décroissante
```

Exemples :

- CONTINENTAL COMMODITIES,
- HORIBA ABX SAS,
- GALPHARMA,
- IBERMA,
- B&B LIFE SCIENCE,
- BIORESEARCH,
- MAIA AFRICA SAS,
- DEO GRATIAS PHARMA.

---

### 6.13.2 Délai moyen par fournisseur

Formule :

```text
Délai moyen fournisseur =
    Moyenne(Date réception réelle - Date commande)
```

---

### 6.13.3 Taux de conformité fournisseur

Formule :

```text
Taux de conformité fournisseur =
    Réceptions conformes / Total réceptions fournisseur × 100
```

---

### 6.13.4 Valeur des achats par fournisseur

Formule :

```text
Valeur achats fournisseur =
    Somme des commandes validées par fournisseur
```

---

### 6.13.5 Nombre de litiges fournisseur

Formule :

```text
Litiges fournisseur =
    Nombre d’écarts, retards, non-conformités ou refus liés au fournisseur
```

---

## 6.14 KPIs Produits

### 6.14.1 Top produits vendus

Formule :

```text
Top produits =
    Produits classés par quantité vendue ou CA HT
```

---

### 6.14.2 Produits à forte marge

Formule :

```text
Produits à forte marge =
    Produits classés par taux de marge décroissant
```

---

### 6.14.3 Produits à faible marge

Formule :

```text
Produits à faible marge =
    Produits dont le taux de marge est inférieur à un seuil
```

---

### 6.14.4 Produits à rotation rapide

Formule :

```text
Produits à rotation rapide =
    Produits avec nombre élevé de sorties sur une période
```

---

### 6.14.5 Produits dormants

Formule :

```text
Produits dormants =
    Produits sans mouvement de sortie depuis X jours
```

---

### 6.14.6 Produits en surstock

Formule :

```text
Produits en surstock =
    Produits dont la couverture dépasse le seuil cible
```

---

## 6.15 Exemple de tableau de bord Direction

| Widget | Indicateur | Visualisation |
|---|---|---|
| CA du mois | Chiffre d’affaires HT | Carte numérique |
| Marge du mois | Taux de marge | Carte numérique |
| Valeur stock | Valeur au prix de revient | Carte numérique |
| Produits critiques | Nombre de produits à risque | Badge rouge |
| Commandes en cours | Commandes fournisseurs | Jauge |
| Expéditions en transit | Conteneurs/avions/express | Liste |
| Top ventes | Produits les plus vendus | Barres |
| Top clients | Clients par CA | Tableau |
| Péremptions | Lots à 30/60/90 jours | Tableau |
| Alertes | Notifications critiques | Liste |

---

## 6.16 Exemple de tableau de bord Achats

| Widget | Indicateur | Visualisation |
|---|---|---|
| Commandes à valider | Nombre | Badge |
| Suggestions MRP | Nombre en attente | Tableau |
| Commandes en retard | Nombre | Liste rouge |
| Valeur achats en cours | Montant CFA | Carte |
| Valeur en transit | Montant CFA | Carte |
| Délais fournisseurs | Moyenne jours | Barres |
| Réceptions prévues | 30 prochains jours | Calendrier |
| Écarts réception | Nombre | Graphique |

---

## 6.17 Exemple de tableau de bord Stock

| Widget | Indicateur | Visualisation |
|---|---|---|
| Stock disponible | Quantité/valeur | Carte |
| Stock réservé | Quantité | Carte |
| Quarantaine | Lots bloqués | Badge |
| Péremption 30 jours | Lots concernés | Tableau |
| Péremption 60 jours | Lots concernés | Tableau |
| Péremption 90 jours | Lots concernés | Tableau |
| Valeur stock | Par catégorie | Camembert |
| Mouvements récents | Entrées/sorties | Timeline |
| Inventaire | Écarts récents | Tableau |
| Emplacements | Occupation | Tableau |

---

## 6.18 Exemple de tableau de bord Ventes

| Widget | Indicateur | Visualisation |
|---|---|---|
| CA jour | Montant HT | Carte |
| CA mois | Montant HT | Carte |
| Commandes en cours | Nombre | Badge |
| Livraisons du jour | Nombre | Liste |
| Retours clients | Montant/quantité | Badge |
| Top clients | CA par client | Barres |
| Top produits | Quantité vendue | Barres |
| Ventes par catégorie | CA | Camembert |
| Commandes non livrées | Liste | Tableau |

---

## 6.19 Exemple de tableau de bord Pricing

| Widget | Indicateur | Visualisation |
|---|---|---|
| Prix de revient moyen | Par catégorie | Carte |
| Marge moyenne | Taux | Carte |
| Écart prix cible | Produits avec écart | Tableau |
| Produits à faible marge | Liste | Tableau rouge |
| Rentabilité par conteneur | Marge réelle | Tableau |
| Rentabilité par transport | Maritime vs aérien | Barres |
| TVA collectée | Montant | Carte |
| Avoirs | Montant | Carte |

---

## 6.20 Exemple de tableau de bord MRP

| Widget | Indicateur | Visualisation |
|---|---|---|
| Produits critiques | Nombre | Badge rouge |
| Produits urgents | Nombre | Badge orange |
| Produits à surveiller | Nombre | Badge jaune |
| Suggestions en attente | Nombre | Tableau |
| Valeur à commander | Montant estimé | Carte |
| Couverture moyenne | Jours | Jauge |
| Date limite commande | Produits à commander | Tableau |
| Suggestions converties | Taux | Graphique |

---

## 6.21 Rapports standards à produire

Le système doit pouvoir générer plusieurs rapports standards.

### 6.21.1 Rapport ventes

Contenu :

- période,
- client,
- produit,
- catégorie,
- quantité vendue,
- prix HT,
- TVA,
- prix TTC,
- marge.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.2 Rapport stock

Contenu :

- produit,
- lot,
- emplacement,
- quantité physique,
- quantité réservée,
- quantité disponible,
- date de péremption,
- statut,
- valeur stock.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.3 Rapport achats

Contenu :

- fournisseur,
- commande,
- devise,
- taux de change,
- montant devise,
- montant CFA,
- statut,
- mode transport,
- réception.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.4 Rapport transport

Contenu :

- expédition,
- fournisseur,
- mode transport,
- conteneur/LTA/tracking,
- date expédition,
- date arrivée estimée,
- date arrivée réelle,
- frais logistiques,
- retard.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.5 Rapport péremptions

Contenu :

- produit,
- lot,
- date péremption,
- quantité restante,
- emplacement,
- valeur stock,
- seuil critique.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.6 Rapport qualité

Contenu :

- lots en quarantaine,
- lots libérés,
- lots non conformes,
- lots périmés,
- destructions,
- retours clients.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.7 rapport inventaire

Contenu :

- session inventaire,
- produit,
- lot,
- emplacement,
- quantité système,
- quantité comptée,
- écart,
- motif,
- utilisateur,
- date.

Format :

- écran,
- Excel,
- PDF.

---

### 6.21.8 Rapport pricing

Contenu :

- produit,
- PA devise,
- PA CFA,
- coefficients,
- prix de revient,
- marge,
- PV HT calculé,
- prix Labmedis HT,
- écart.

Format :

- écran,
- Excel,
- PDF.

---

## 6.22 Rapports avancés recommandés

### 6.22.1 Analyse de rentabilité par conteneur

Objectif :

Comparer la rentabilité réelle d’un conteneur.

Données :

- produits du conteneur,
- prix d’achat,
- frais freight,
- frais transit,
- frais transfert,
- commissions,
- prix de vente,
- marge réelle.

---

### 6.22.2 Analyse de rentabilité par mode de transport

Objectif :

Comparer les produits arrivés par bateau, avion ou express.

Données :

- coût logistique unitaire,
- délai,
- prix de revient,
- marge,
- rotation.

---

### 6.22.3 Analyse de saisonnalité

Objectif :

Identifier les périodes de forte vente.

Produits potentiellement saisonniers :

- GRIPEX Adulte,
- GRIPEX Enfant,
- Pommade Maïa,
- Strick Out,
- certains antitussifs.

---

### 6.22.4 Analyse de performance fournisseurs

Objectif :

Comparer les fournisseurs selon :

- délai,
- conformité,
- prix,
- qualité,
- litiges,
- volume acheté.

---

### 6.22.5 Analyse de performance clients

Objectif :

Identifier les clients les plus rentables et les plus actifs.

Critères :

- chiffre d’affaires,
- fréquence,
- volume,
- retours,
- marge,
- ponctualité de paiement si gérée.

---

## 6.23 Fréquence de calcul des indicateurs

| Type d’indicateur | Fréquence recommandée |
|---|---|
| Stock disponible | Temps réel. |
| Réservations commandes | Temps réel. |
| Ventes du jour | Temps réel ou quasi temps réel. |
| Alertes péremption | Quotidien. |
| Alertes stock faible | Quotidien. |
| MRP | Quotidien ou manuel. |
| Marge réelle | Quotidien ou après facturation. |
| Rapports mensuels | Mensuel. |
| Rapports annuels | Annuel. |
| Logs sécurité | Temps réel. |

---

## 6.24 Agrégation des données

Pour éviter de recalculer tous les indicateurs en temps réel sur de gros volumes, le système peut prévoir des tables d’agrégation.

### 6.24.1 Tables d’agrégation recommandées

#### `DailySalesSummary`

```csharp
public class DailySalesSummary : BaseEntity
{
    public DateTime SalesDate { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public int QuantitySold { get; set; }
    public decimal TotalAmountHt { get; set; }
    public decimal TotalVatAmount { get; set; }
    public decimal TotalAmountTtc { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossMargin { get; set; }
}
```

---

#### `DailyStockSummary`

```csharp
public class DailyStockSummary : BaseEntity
{
    public DateTime SummaryDate { get; set; }
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public int PhysicalStock { get; set; }
    public int ReservedStock { get; set; }
    public int AvailableStock { get; set; }
    public int QuarantineStock { get; set; }
    public int ExpiredStock { get; set; }
    public decimal StockValue { get; set; }
}
```

---

#### `DailyForecastSummary`

```csharp
public class DailyForecastSummary : BaseEntity
{
    public DateTime SummaryDate { get; set; }
    public Guid ProductId { get; set; }
    public int AvailableStock { get; set; }
    public int TransitStock { get; set; }
    public decimal AverageDailyConsumption { get; set; }
    public int CoverageDays { get; set; }
    public int ReorderPoint { get; set; }
    public int NetRequirement { get; set; }
    public ForecastRiskLevel RiskLevel { get; set; }
}
```

---

#### `MonthlyFinancialSummary`

```csharp
public class MonthlyFinancialSummary : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSalesHt { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalSalesTtc { get; set; }
    public decimal TotalCostOfGoodsSold { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal PurchaseAmountCfa { get; set; }
    public decimal LogisticsCost { get; set; }
}
```

---

## 6.25 Endpoints API Reporting recommandés

### Dashboard

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/dashboard/management` | Dashboard direction. |
| GET | `/api/dashboard/purchasing` | Dashboard achats. |
| GET | `/api/dashboard/logistics` | Dashboard logistique. |
| GET | `/api/dashboard/stock` | Dashboard stock. |
| GET | `/api/dashboard/sales` | Dashboard ventes. |
| GET | `/api/dashboard/pricing` | Dashboard pricing. |
| GET | `/api/dashboard/forecast` | Dashboard MRP. |
| GET | `/api/dashboard/quality` | Dashboard qualité. |

---

### KPIs ventes

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/sales/summary` | Synthèse ventes. |
| GET | `/api/reports/sales/by-product` | Ventes par produit. |
| GET | `/api/reports/sales/by-customer` | Ventes par client. |
| GET | `/api/reports/sales/by-category` | Ventes par catégorie. |
| GET | `/api/reports/sales/trend` | Tendance ventes. |

---

### KPIs stock

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/stock/valuation` | Valorisation stock. |
| GET | `/api/reports/stock/by-category` | Stock par catégorie. |
| GET | `/api/reports/stock/expiring-lots` | Lots proches péremption. |
| GET | `/api/reports/stock/slow-moving` | Produits dormants. |
| GET | `/api/reports/stock/coverage` | Couverture stock. |

---

### KPIs achats

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/purchases/summary` | Synthèse achats. |
| GET | `/api/reports/purchases/by-supplier` | Achats par fournisseur. |
| GET | `/api/reports/purchases/open-orders` | Commandes en cours. |
| GET | `/api/reports/purchases/delays` | Retards achats. |

---

### KPIs transport

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/logistics/shipments` | Expéditions en cours. |
| GET | `/api/reports/logistics/costs` | Coûts logistiques. |
| GET | `/api/reports/logistics/delays` | Retards transport. |
| GET | `/api/reports/logistics/by-transport-mode` | Analyse par mode. |

---

### KPIs pricing

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/pricing/margin` | Marges produits. |
| GET | `/api/reports/pricing/variance` | Écarts prix cible. |
| GET | `/api/reports/pricing/by-container` | Rentabilité conteneur. |
| GET | `/api/reports/pricing/by-transport-mode` | Rentabilité transport. |

---

### KPIs MRP

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/forecast/at-risk` | Produits à risque. |
| GET | `/api/reports/forecast/coverage` | Couverture produits. |
| GET | `/api/reports/forecast/suggestions` | Suggestions MRP. |
| GET | `/api/reports/forecast/accuracy` | Précision prévisions. |

---

### Export

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reports/export/sales` | Export ventes Excel/PDF. |
| GET | `/api/reports/export/stock` | Export stock Excel/PDF. |
| GET | `/api/reports/export/purchases` | Export achats Excel/PDF. |
| GET | `/api/reports/export/expiring-lots` | Export péremptions. |
| GET | `/api/reports/export/pricing` | Export pricing. |

---

## 6.26 Impact sur le Frontend React

Le frontend doit proposer une interface claire, interactive et filtrable.

---

### 6.26.1 Composants recommandés

| Composant | Usage |
|---|---|
| Carte KPI | Afficher un indicateur principal. |
| Graphique barres | Comparer produits, clients, fournisseurs. |
| Graphique lignes | Tendance dans le temps. |
| Camembert | Répartition par catégorie. |
| Jauge | Couverture stock, objectif, marge. |
| Tableau paginé | Listes détaillées. |
| Timeline | Suivi expéditions, mouvements. |
| Calendrier | Réceptions prévues, péremptions. |
| Badge statut | Critique, urgent, normal, surstock. |
| Bouton export | Export Excel ou PDF. |
| Filtres globaux | Période, fournisseur, catégorie, client. |
| Notifications | Alertes temps réel. |

---

### 6.26.2 Filtres recommandés

Les rapports doivent pouvoir être filtrés par :

- date de début,
- date de fin,
- période prédéfinie : aujourd’hui, 7 jours, 30 jours, mois, trimestre, année,
- produit,
- catégorie,
- fournisseur,
- client,
- lot,
- emplacement,
- mode de transport,
- statut,
- devise,
- utilisateur.

---

### 6.26.3 Règles UX

1. Les KPIs critiques doivent être visibles immédiatement.
2. Les indicateurs négatifs doivent être mis en évidence.
3. Les graphiques doivent être lisibles sans formation complexe.
4. Les montants doivent être affichés en CFA par défaut.
5. Les montants en devises doivent être affichés si nécessaire.
6. Les exports doivent respecter les permissions.
7. Les rapports longs doivent afficher un indicateur de chargement.
8. Les données temps réel doivent être mises à jour via SignalR si pertinent.
9. Les tableaux doivent être triables et paginés.
10. Les rapports doivent pouvoir être actualisés manuellement.

---

## 6.27 Sécurité des rapports

### 6.27.1 Règles d’accès

| Rapport | Rôles autorisés |
|---|---|
| Direction | Admin, Direction. |
| Achats | Admin, Direction, Achats. |
| Logistique | Admin, Direction, Achats, Logistique. |
| Stock | Admin, Direction, Logistique, Magasinier, Qualité. |
| Ventes | Admin, Direction, Commercial. |
| Pricing | Admin, Direction, Comptable. |
| Finance | Admin, Direction, Comptable. |
| Qualité | Admin, Direction, Qualité. |
| Audit | Admin, Direction. |

---

### 6.27.2 Règles de masquage

Si l’utilisateur n’a pas la permission :

- ne pas afficher les prix d’achat,
- ne pas afficher les marges,
- ne pas afficher les coûts logistiques,
- ne pas permettre l’export financier,
- ne pas afficher les données clients sensibles.

---

## 6.28 Export des rapports

### 6.28.1 Formats recommandés

| Format | Usage |
|---|---|
| Excel | Analyse opérationnelle. |
| CSV | Intégration ou traitement externe. |
| PDF | Document officiel, archive, envoi email. |

---

### 6.28.2 Règles d’export

1. L’export doit être journalisé.
2. L’export doit respecter les permissions.
3. L’export doit contenir la période filtrée.
4. L’export doit contenir la date de génération.
5. L’export doit contenir l’utilisateur générateur.
6. Les montants doivent être formatés en CFA.
7. Les dates doivent être formatées clairement.

---

## 6.29 Notifications liées au reporting

Le système doit envoyer des alertes lorsque :

| Événement | Notification |
|---|---|
| Rupture critique | Alerte direction/achats. |
| Stock faible | Alerte achats/magasin. |
| Lot proche péremption | Alerte stock/qualité. |
| Lot périmé | Alerte qualité. |
| Commande fournisseur en retard | Alerte achats/logistique. |
| Transport en retard | Alerte logistique. |
| Marge faible | Alerte direction/pricing. |
| Écart inventaire important | Alerte stock/direction. |
| Suggestion MRP non traitée | Alerte achats. |
| Rapport quotidien généré | Notification ou email. |

---

## 6.30 Jobs Hangfire pour reporting

### 6.30.1 Job quotidien de synthèse ventes

```text
DailySalesSummaryJob
```

Actions :

1. Calculer les ventes du jour.
2. Agréger par produit, client, catégorie.
3. Calculer TVA et marge.
4. Alimenter `DailySalesSummary`.

---

### 6.30.2 Job quotidien de synthèse stock

```text
DailyStockSummaryJob
```

Actions :

1. Calculer stock physique.
2. Calculer stock réservé.
3. Calculer stock disponible.
4. Calculer valeur stock.
5. Alimenter `DailyStockSummary`.

---

### 6.30.3 Job quotidien MRP

```text
DailyForecastSummaryJob
```

Actions :

1. Calculer couverture.
2. Calculer besoins.
3. Identifier risques.
4. Alimenter `DailyForecastSummary`.

---

### 6.30.4 Job mensuel financier

```text
MonthlyFinancialSummaryJob
```

Actions :

1. Agréger ventes mensuelles.
2. Agréger achats.
3. Agréger coûts logistiques.
4. Calculer marge globale.
5. Alimenter `MonthlyFinancialSummary`.

---

## 6.31 Exemple de contrôleur Reporting

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public ReportsController(
        IReportService reportService,
        ILoggerManager logger,
        IUserService userService)
    {
        _reportService = reportService;
        _logger = logger;
        _userService = userService;
    }

    [Authorize(Policy = "Reports.Sales.Read")]
    [HttpGet("sales/summary")]
    public async Task<IActionResult> GetSalesSummary([FromQuery] ReportFilterRequest request)
    {
        var user = await _userService.GetCurrentUserAsync(User);

        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début GetSalesSummary | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try
        {
            var result = await _reportService.GetSalesSummaryAsync(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec GetSalesSummary : {ex.Message} | IP: {Request.GetIp()}");

            return BadRequest(new
            {
                message = "Impossible de récupérer la synthèse des ventes."
            });
        }
    }
}
```

---

## 6.32 User Stories Reporting

### US-REP-01 : Consulter le dashboard direction

**En tant que** direction,  
**je veux** consulter un tableau de bord global,  
**afin de** suivre la performance de LABMEDIS.

**Critères d’acceptation :**

1. Le dashboard affiche le CA du mois.
2. Le dashboard affiche la marge du mois.
3. Le dashboard affiche la valeur du stock.
4. Le dashboard affiche les produits critiques.
5. Le dashboard affiche les commandes en cours.
6. Le dashboard affiche les alertes importantes.
7. Les données sont actualisées.

---

### US-REP-02 : Consulter les ventes par produit

**En tant que** commercial ou direction,  
**je veux** voir les ventes par produit,  
**afin d’identifier les produits performants.

**Critères d’acceptation :**

1. L’utilisateur choisit une période.
2. Le système affiche les quantités vendues.
3. Le système affiche le CA HT.
4. Le système affiche le CA TTC.
5. Le système peut filtrer par catégorie.
6. Le système peut exporter en Excel.

---

### US-REP-03 : Consulter les ventes par client

**En tant que** commercial ou direction,  
**je veux** voir les ventes par client,  
**afin de** suivre les répartiteurs et structures de santé.

**Critères d’acceptation :**

1. Le système affiche les clients.
2. Le système affiche le nombre de commandes.
3. Le système affiche le CA HT.
4. Le système affiche le CA TTC.
5. Le système peut trier par CA décroissant.
6. Le système peut exporter en Excel.

---

### US-REP-04 : Consulter la valeur du stock

**En tant que** direction ou magasinier,  
**je veux** voir la valeur du stock,  
**afin de** suivre l’immobilisation financière.

**Critères d’acceptation :**

1. Le système affiche la valeur par produit.
2. Le système affiche la valeur par catégorie.
3. Le système affiche la valeur par fournisseur.
4. Le système distingue stock disponible, réservé, quarantaine, périmé.
5. Le système peut filtrer par emplacement.
6. Le système peut exporter en Excel.

---

### US-REP-05 : Consulter les lots proches péremption

**En tant que** responsable stock ou qualité,  
**je veux** voir les lots proches de la péremption,  
**afin de** prendre des actions avant expiration.

**Critères d’acceptation :**

1. Le système affiche les lots à 30 jours.
2. Le système affiche les lots à 60 jours.
3. Le système affiche les lots à 90 jours.
4. Le système affiche les quantités restantes.
5. Le système affiche la valeur concernée.
6. Le système peut exporter la liste.

---

### US-REP-06 : Consulter la marge par produit

**En tant que** direction ou comptable,  
**je veux** consulter la marge par produit,  
**afin de** contrôler la rentabilité.

**Critères d’acceptation :**

1. Le système affiche le prix de vente HT.
2. Le système affiche le prix de revient.
3. Le système affiche la marge.
4. Le système affiche le taux de marge.
5. Le système peut filtrer par catégorie.
6. L’accès est réservé aux rôles autorisés.

---

### US-REP-07 : Consulter les commandes fournisseurs en cours

**En tant que** responsable achats,  
**je veux** voir les commandes fournisseurs en cours,  
**afin de** suivre les approvisionnements.

**Critères d’acceptation :**

1. Le système affiche les commandes validées.
2. Le système affiche les commandes expédiées.
3. Le système affiche les commandes en transit.
4. Le système affiche les dates estimées de réception.
5. Le système affiche les retards.
6. Le système peut filtrer par fournisseur.

---

### US-REP-08 : Consulter les expéditions en transit

**En tant que** responsable logistique,  
**je veux** voir les expéditions en transit,  
**afin de** suivre les conteneurs, avions et express.

**Critères d’acceptation :**

1. Le système affiche le mode de transport.
2. Le système affiche la référence transport.
3. Le système affiche la date d’expédition.
4. Le système affiche la date d’arrivée estimée.
5. Le système affiche les retards.
6. Le système peut afficher les coûts logistiques.

---

### US-REP-09 : Exporter un rapport

**En tant qu’utilisateur autorisé**,  
**je veux** exporter un rapport,  
**afin de** l’analyser ou l’archiver.

**Critères d’acceptation :**

1. L’export respecte les filtres appliqués.
2. L’export peut être Excel ou PDF selon le rapport.
3. L’export contient la date de génération.
4. L’export est journalisé.
5. L’export n’est accessible qu’aux rôles autorisés.

---

### US-REP-10 : Recevoir un rapport automatique

**En tant que** direction ou responsable,  
**je veux** recevoir certains rapports automatiquement,  
**afin de** suivre l’activité sans connexion quotidienne.

**Critères d’acceptation :**

1. Le rapport peut être quotidien, hebdomadaire ou mensuel.
2. Le rapport peut être envoyé par email.
3. Le rapport peut être en PDF ou Excel.
4. Le destinataire doit être autorisé.
5. L’envoi est journalisé.
6. Les erreurs d’envoi sont tracées.

---

## 6.33 Points à valider avec LABMEDIS

| Question | Impact |
|---|---|
| Quels KPIs sont prioritaires pour la direction ? | Priorisation dashboard. |
| Faut-il afficher les marges à tous les rôles ? | Permissions. |
| Faut-il exporter les rapports en Excel, PDF ou les deux ? | Export. |
| Faut-il envoyer des rapports automatiques par email ? | Hangfire + FluentEmail. |
| Faut-il comparer plusieurs périodes ? | Graphiques comparatifs. |
| Faut-il gérer plusieurs devises dans les rapports achats ? | Reporting international. |
| Faut-il analyser la rentabilité par conteneur ? | Reporting logistique. |
| Faut-il analyser la rentabilité par mode de transport ? | Pricing et logistique. |
| Faut-il un rapport TVA spécifique ? | Comptabilité. |
| Faut-il des rapports réglementaires pharmaceutiques ? | Qualité/conformité. |
| Faut-il suivre les paiements clients ? | Reporting financier supplémentaire. |
| Faut-il intégrer un outil BI externe ? | Optionnel. |
| Faut-il conserver les snapshots quotidiens combien de temps ? | Archivage. |

---

## 6.34 Synthèse

Le module **Reporting & Tableaux de Bord** doit fournir à LABMEDIS une vision complète et fiable de :

- la performance commerciale,
- la rentabilité réelle,
- la valeur du stock,
- les risques de rupture,
- les flux logistiques internationaux,
- la qualité pharmaceutique,
- les achats fournisseurs,
- les alertes opérationnelles.

Il doit reposer sur :

1. Des KPIs clairs et calculés automatiquement.
2. Des tableaux de bord par rôle.
3. Des graphiques interactifs dans React.
4. Des endpoints API sécurisés dans .NET.
5. Des agrégations performantes.
6. Des exports Excel et PDF.
7. Des notifications temps réel via SignalR.
8. Des jobs Hangfire pour les synthèses quotidiennes.
9. Des permissions strictes selon les rôles.
10. Des rapports adaptés aux spécificités du commerce pharmaceutique international.