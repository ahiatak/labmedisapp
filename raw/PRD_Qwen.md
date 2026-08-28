
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
