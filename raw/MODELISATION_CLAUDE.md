# LABMEDIS — Modèle de données

**Projet :** Plateforme de gestion dépositaire pharmaceutique LABMEDIS (achats internationaux, stock par lot, tarification, distribution, ventes, prévision)
**Version :** 1.0 — modèle de données validé
**Date :** 28 août 2026
**Moteur cible :** PostgreSQL (16.15, testé)

---

## Bandeau de statut

| Indicateur | Valeur | Preuve |
|---|---|---|
| Tables | **55** | `information_schema.tables`, introspection réelle |
| Colonnes | **636** | `information_schema.columns` |
| Clés primaires | **55** | une par table, UUID |
| Clés étrangères | **112** | toutes avec `ON DELETE` explicite et justifié |
| Index uniques (dont 55 PK) | **85** | dont 30 contraintes d'unicité métier hors PK, plusieurs partielles (`WHERE deleted_at IS NULL`) |
| Index secondaires (hors PK) | **101** | `pg_indexes` |
| Triggers | **55** | maintien de `updated_at` (un par table) |
| Contraintes CHECK métier explicites | **51** | dont 27 énumérations de statut (`VARCHAR + CHECK IN (...)`) |
| Références polymorphes documentées | **3** | `audit_logs`, `stock_movements`, `notifications` — sans FK stricte, assumé (règle 2.5) |
| Diagrammes Mermaid | **13** | 1 diagramme maître, 8 ERD de domaine, 4 diagrammes de séquence — **tous validés par un parseur réel** (`mermaid` + `jsdom`, sans navigateur headless) |
| Exécution du script complet | ✅ **réelle** | recréation de la base + `psql -v ON_ERROR_STOP=1` après chaque domaine ajouté, puis une dernière fois sur le document assemblé (Phase 6.6) |
| Scénario de bout en bout | ✅ **inséré et interrogé réellement** | cf. §8, 8 domaines exercés, résultats vérifiés ligne à ligne contre les valeurs réelles de `Structure_de_prix.xlsx` |
| `EXPLAIN (ANALYZE, BUFFERS)` | ✅ **exécuté sur volume réaliste** | 12 400 lots synthétiques générés pour un test d'index honnête (cf. §8) |

Ce document, `schema.sql` et le résumé conversationnel constituent les trois livrables de la mission. Aucune affirmation de validation ci-dessous ne repose sur une relecture mentale : chaque preuve renvoie à une commande réellement exécutée dans le sandbox de développement.

---

## 1. Méthodologie suivie

### 1.1 Ce qui a été lu

Les 12 fichiers fournis ont été lus **intégralement**, y compris ceux non affichés automatiquement en contexte du fait de leur taille : `PRD_CLAUDE.md` (56 Ko) et les modules `PRD_Qwen` 2 à 6 (260 Ko cumulés). Une clarification factuelle s'impose : le texte de mission en 6 phases n'est **pas** l'un des 12 fichiers uploadés — c'est le message collé directement dans la conversation. `PRD_CLAUDE.md`, lui, est un document livrable à part entière : un PRD indépendant et le plus abouti des sources (contexte réglementaire togolais/UEMOA inclus, hypothèses et questions ouvertes explicitées).

Les trois fichiers Excel (`Liste_des_clients_et_fournisseurs.xlsx`, `Liste_des_produits_actualisée_LABMEDIS.xlsx`, `Structure_de_prix.xlsx`) ont été lus ligne par ligne via `openpyxl`, sans échantillonnage, et leurs chiffres recoupés programmatiquement (comptage exact des catégories, des doublons, vérification arithmétique de la cascade de prix).

### 1.2 Conventions de modélisation retenues

Conformément au cadrage de la mission (section 2.5) :

- Clés primaires en `UUID` (`gen_random_uuid()`, natif PostgreSQL ≥ 13, vérifié disponible sans extension).
- `snake_case`, tables au pluriel, FK nommées `[table_singulier]_id`.
- Statuts en `VARCHAR + CHECK (... IN (...))`, jamais d'`ENUM` natif — 27 colonnes de ce type.
- `created_at` / `updated_at` (trigger `set_updated_at()`, seul trigger du modèle — un invariant qui ne peut pas vivre sereinement en couche applicative) / `deleted_at` (soft delete) sur toutes les tables métier.
- Unicité compatible avec le soft delete : les contraintes d'unicité métier (désignation produit, numéro de commande, email...) sont des **index uniques partiels** (`WHERE deleted_at IS NULL`), pas des contraintes `UNIQUE` classiques — testé réellement (§8.5) : une désignation redevient disponible après suppression logique, sans jamais permettre un doublon parmi les lignes actives.
- Chaque FK précise son `ON DELETE`, avec une politique générale documentée une fois plutôt que 112 fois de façon identique (cf. §9.3), et un commentaire ponctuel uniquement quand une FK déroge à la politique générale.
- Aucune contrainte SQL figée sur une valeur mouvante dans le temps (ex. un écart d'inventaire n'est jamais une colonne générée comparant deux valeurs qui évoluent indépendamment — le calcul vit en couche service, cf. commentaire sur `inventory_counts`).
- Toute donnée financière transactionnelle est figée en instantané sur l'enregistrement (`stock_lots.unit_cost_cfa`, `product_prices.pv_ht_applied`, `invoice_lines.vat_rate`...), en plus d'exister dans une table de configuration versionnée (`pricing_profiles`, `exchange_rates`).
- Trois références polymorphes assumées explicitement (jamais de FK stricte vers une cible variable) : `audit_logs.entity_type/entity_id`, `stock_movements.source_document_type/id`, `notifications.source_document_type/id`.

### 1.3 Ce qui a été réellement testé (Phase 3)

1. **Exécution du script domaine par domaine.** Après l'ajout de chaque nouveau domaine, la base a été recréée à vide (`dropdb`/`createdb`) et le script cumulé exécuté avec `psql -v ON_ERROR_STOP=1` — 8 exécutions réussies, dans cet ordre : Sécurité → Référentiel Commercial → Pricing & Devises → Achats & Logistique → Entreposage & Stock → Ventes & Facturation → Prévision MRP → Reporting & Notifications (ordre choisi pour qu'aucune FK ne pointe vers un domaine défini plus tard).
2. **Scénario métier de bout en bout inséré avec de vraies données**, alignées sur les chiffres réels vérifiés de `Structure_de_prix.xlsx` (gamme France Lait) : un utilisateur multi-rôles, une commande fournisseur complète avec historique de statuts, une expédition maritime, deux lots au même produit avec des péremptions différentes, une vente avec allocation FEFO, une livraison, une facture, un retour partiel avec avoir, un calcul de prévision MRP déclenchant une suggestion de commande, une notification et un log d'audit. Détail et résultats vérifiés en §8.
3. **Contraintes réellement éprouvées, pas seulement déclarées** : une tentative de suppression d'un fournisseur encore référencé a été rejetée par PostgreSQL (`ON DELETE RESTRICT` réel) ; une tentative d'insertion de lot avec `remaining_quantity > initial_quantity` a été rejetée par la contrainte `CHECK` (découverte en cours de génération de données de test, non anticipée — preuve que la contrainte est active et non un vœu pieux dans un commentaire) ; l'index unique partiel a été vérifié en conditions réelles de suppression logique.
4. **`EXPLAIN (ANALYZE, BUFFERS)`** sur deux requêtes sensibles à la performance, contre un volume synthétique de 12 400 lots (une table quasi vide aurait rendu le test malhonnête : l'optimiseur PostgreSQL préfère à raison un balayage séquentiel sur une poignée de lignes, ce qui n'aurait rien prouvé sur l'usage réel de l'index).
5. **13 diagrammes Mermaid validés par un vrai parseur** (`mermaid` + `jsdom`, sans navigateur headless — le rendu visuel complet exige Chromium, indisponible en sandbox ; le *parsing* syntaxique n'en a pas besoin et suffit à détecter les erreurs). Le parseur a effectivement détecté deux erreurs réelles en cours de rédaction (points-virgules dans du texte de message de séquence, incompatibles avec la grammaire Mermaid) — corrigées puis revalidées.

Là où l'exécution réelle n'était pas possible (rendu visuel complet des diagrammes), cela est dit explicitement plutôt que simulé : la relecture manuelle du rendu visuel final (au-delà du parsing syntaxique) reste à faire par un humain ouvrant les fichiers `.mmd` dans un éditeur Mermaid.

---

## 2. Comparaison avec le brouillon initial

`PRD_CLAUDE.md` contient, en section 9, un schéma conceptuel simplifié (17 entités, un diagramme Mermaid `erDiagram` sommaire). Ce n'est pas un modèle de données détaillé (pas de types, pas de contraintes, pas d'index) mais c'est le seul brouillon structurel préexistant parmi les sources : il sert de point de comparaison, conformément à la Phase 1.5 de la mission. Le modèle livré ici n'a pas remplacé ce brouillon à l'aveugle — chaque écart significatif est tracé ci-dessous vers le document source qui l'exige.

| Lacune constatée dans le brouillon PRD_CLAUDE §9 | Document source qui l'exige | Solution apportée dans ce modèle |
|---|---|---|
| RBAC réduit à `Utilisateur.rôle(s)` (texte), sans rôles/permissions explicites | PRD_Qwen-5 §5.5 ("le RBAC doit être représenté par des tables explicites, jamais par un simple champ texte libre") ; règle 2.6 de la mission | `roles`, `permissions`, `role_permissions`, `user_roles`, `user_permission_exceptions` (5 tables, domaine 1) |
| Aucune table d'audit (seul `ILoggerManager`/NLog, fichier) | PRD_Qwen-5 §5.8.2 (entité `AuditLog`) | `audit_logs`, avec référence polymorphe documentée |
| Aucun jeton de renouvellement persisté | PRD_Qwen-5 §5.3.3 ("le Refresh Token doit être stocké côté backend avec expiration... révocable") | `refresh_tokens` |
| Licence de dépositaire mentionnée en prose (PRD_CLAUDE §17.1) mais absente de son propre modèle §9 | PRD_CLAUDE §17.1 lui-même (incohérence interne prose/modèle) | `company_profile` |
| Classe thérapeutique et fournisseur multiple = attributs bruts sur `Produit` | PRD_CLAUDE §8.1.2 et §8.1.6 eux-mêmes ("listes contrôlées", "un ou plusieurs fournisseurs habituels") | `therapeutic_classes` (référentiel), `product_suppliers` (association N:N) |
| Coefficients de coût attachés directement au `Lot`, non réutilisables/paramétrables | PRD_Qwen-1 §1.2 ("les coefficients ne doivent jamais être codés en dur... stockés en base pour permettre à la direction de les ajuster") | `pricing_profiles`, réutilisé par plusieurs lots/produits |
| `Expedition` liée à une seule `CommandeAchat` (FK directe) | PRD_Qwen module 3 §3.5.4 (une expédition peut consolider plusieurs commandes) — **[CONTRADICTION]**, cf. §9.1 | `shipment_lines` référence `purchase_order_lines`, jamais l'entête de commande |
| Aucun registre des frais logistiques réels | PRD_Qwen module 3 §3.5.3/§US-LOG-02 | `import_costs` |
| Aucun historique de statut (`Statut` = simple champ) | PRD_Qwen module 3 §US-ACH-03 ("chaque changement de statut est horodaté") | `purchase_order_status_history`, `sale_order_status_history` |
| `Lot` implicitement mono-emplacement | PRD_Qwen-2 §2.3.3 ("un même lot peut être stocké à plusieurs emplacements") | `stock_lot_locations` (association N:N avec quantité réservée) |
| Aucune gestion d'inventaire | PRD_Qwen-2 §2.8 (workflow complet) | `inventory_sessions`, `inventory_counts` |
| **Module Prévision/MRP entièrement absent du modèle §9** (alors que décrit en prose détaillée §8.9) | PRD_Qwen-4 (module entier, formules chiffrées) | `forecast_parameters`, `supplier_lead_times`, `forecast_calculations`, `reorder_suggestions` (domaine 7 complet) |
| Livraison et facturation fusionnées dans `CommandeVente`/`LigneCommandeVente` | PRD_CLAUDE §8.8.3/§18.2 lui-même (BL et facture distincts) ; PRD_Qwen module 3, workflows 9 et 10 séparés | `deliveries`/`delivery_lines` distincts de `invoices`/`invoice_lines` |
| **Aucun avoir ni retour client dans le modèle §9** (alors que décrits en détail prose §20.3) | PRD_CLAUDE §20.3 lui-même | `credit_notes`, `credit_note_lines`, `customer_returns` |
| Aucun tarif négocié par client | PRD_Qwen module 3 §US-VEN-05 | `customer_product_prices` |
| **Aucune table de reporting/agrégation** | PRD_Qwen-6 §6.24 (`DailySalesSummary`, `DailyStockSummary`, `DailyForecastSummary`, `MonthlyFinancialSummary`) | Domaine 8 complet (5 tables) |

Le brouillon reste juste sur l'essentiel qu'il couvre (traçabilité par lot, prix pondéré, FEFO, multi-devises) : ces principes sont conservés à l'identique. L'écart de volume (17 entités conceptuelles contre 55 tables détaillées) s'explique par le niveau de détail demandé ici (types, contraintes, index) et par la couverture de modules que le brouillon ne traitait pas encore (RBAC détaillé, MRP, facturation/avoirs, reporting) — pas par un désaccord de fond.

---

## 3. Diagramme maître

Vue d'ensemble des 55 tables regroupées par domaine, ne montrant que les relations structurantes inter-domaines (le détail complet, y compris intra-domaine, figure dans les 8 ERD de la section 4). Fichier source : `diagrams/master.mmd`, validé par le parseur Mermaid (type `flowchart-v2`, 104 lignes).

```mermaid
flowchart TB
    subgraph D1["1 — Sécurité & Utilisateurs"]
        users["users"]
        roles["roles"]
        permissions["permissions"]
        audit_logs["audit_logs"]
        company_profile["company_profile"]
    end

    subgraph D2["2 — Référentiel Commercial"]
        products["products"]
        categories["categories"]
        suppliers["suppliers"]
        customers["customers"]
        therapeutic_classes["therapeutic_classes"]
    end

    subgraph D3["3 — Pricing & Devises"]
        currencies["currencies"]
        exchange_rates["exchange_rates"]
        pricing_profiles["pricing_profiles"]
        product_prices["product_prices"]
    end

    subgraph D4["4 — Achats & Logistique"]
        purchase_orders["purchase_orders"]
        purchase_order_lines["purchase_order_lines"]
        shipments["shipments"]
        shipment_lines["shipment_lines"]
        import_costs["import_costs"]
    end

    subgraph D5["5 — Entreposage & Stock"]
        warehouses["warehouses"]
        storage_locations["storage_locations"]
        stock_lots["stock_lots"]
        stock_movements["stock_movements"]
        inventory_sessions["inventory_sessions"]
    end

    subgraph D6["6 — Ventes & Facturation"]
        sale_orders["sale_orders"]
        deliveries["deliveries"]
        invoices["invoices"]
        credit_notes["credit_notes"]
        customer_returns["customer_returns"]
    end

    subgraph D7["7 — Prévision MRP"]
        forecast_parameters["forecast_parameters"]
        forecast_calculations["forecast_calculations"]
        reorder_suggestions["reorder_suggestions"]
        supplier_lead_times["supplier_lead_times"]
    end

    subgraph D8["8 — Reporting & Notifications"]
        daily_sales_summary["daily_sales_summary"]
        daily_stock_summary["daily_stock_summary"]
        notifications["notifications"]
    end

    %% --- Relations structurantes inter-domaines (sélection ; le détail
    %% complet de chaque domaine, y compris intra-domaine, est dans les
    %% 8 ERD détaillés) ---
    products -->|catégorise| categories
    suppliers -.->|fournit| products
    users ==>|authentifie / audite| D1

    products --> purchase_order_lines
    suppliers --> purchase_orders
    currencies --> purchase_orders
    exchange_rates -->|taux figé| purchase_orders
    purchase_order_lines --> shipment_lines
    shipments --> shipment_lines
    import_costs --> shipments

    purchase_order_lines ==>|réceptionnée en| stock_lots
    shipment_lines ==>|réceptionnée en| stock_lots
    pricing_profiles -->|coefficients| stock_lots
    products --> stock_lots
    warehouses --> storage_locations
    storage_locations --> stock_lots

    customers --> sale_orders
    stock_lots ==>|allocation FEFO| sale_orders
    sale_orders --> deliveries
    deliveries --> invoices
    invoices --> credit_notes
    sale_orders -.->|retour| customer_returns

    products --> forecast_parameters
    suppliers --> supplier_lead_times
    daily_sales_summary -.->|consommation historique| forecast_calculations
    forecast_calculations --> reorder_suggestions
    reorder_suggestions ==>|conversion| purchase_orders

    sale_orders -.-> daily_sales_summary
    stock_lots -.-> daily_stock_summary
    reorder_suggestions -.->|alerte| notifications
    users -.->|destinataire| notifications

    classDef domain fill:#f4f4f4,stroke:#666,stroke-width:1px;
    class D1,D2,D3,D4,D5,D6,D7,D8 domain;
```

---

## 4. Sections par domaine

### 4.1 Domaine 1 — Sécurité & Utilisateurs

**Rôle métier.** Authentifie les utilisateurs, porte le modèle RBAC (utilisateur → rôle(s) → permissions) qui conditionne l'accès à tous les autres domaines, journalise toute action sensible et conserve le paramétrage réglementaire de l'entreprise (licence de dépositaire). Domaine racine : ne dépend d'aucun autre domaine métier.

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Le mot de passe n'est jamais stocké en clair | `users.password_hash` | Règle générale de sécurité ; PRD_Qwen-5 §5.9.2 |
| Verrouillage après tentatives échouées répétées | `users.failed_login_attempts`, `users.lockout_end_at` | PRD_Qwen-5 §5.3.4 (5 tentatives, verrouillage 15 min) |
| Un utilisateur peut avoir plusieurs rôles | `user_roles` (association N:N) | PRD_Qwen-5 §5.5.6 US-RBAC-03 ; vérifié dans le scénario (Kokou Amegan = Achats + Direction) |
| RBAC représenté par des tables explicites, jamais un champ texte libre | `roles`, `permissions`, `role_permissions` | PRD_Qwen-5 §5.5.1/§5.5.5 ; règle 2.6 de la mission |
| Dérogation individuelle de permission sans modifier tout un rôle | `user_permission_exceptions` | PRD_Qwen-5 §5.5.5 (entité `UserPermissionException`) |
| Le jeton de renouvellement est révocable et expire | `refresh_tokens.expires_at`, `revoked_at` | PRD_Qwen-5 §5.3.3 |
| Toute action sensible est journalisée (utilisateur, IP, User-Agent, module, résultat) | `audit_logs` | PRD_Qwen-5 §5.8.2/§5.8.3 ; format aligné sur `ILoggerManager` (PRD_CLAUDE §12.5) |
| La cible d'un log d'audit peut être n'importe quelle entité métier | `audit_logs.entity_type` / `entity_id` (référence polymorphe, sans FK stricte, documentée) | Règle 2.5 de la mission |
| La licence de dépositaire a une échéance à surveiller | `company_profile.depositary_license_expires_at` | PRD_CLAUDE §17.1 ("alerte avant échéance, mécanisme similaire à l'alerte de péremption produit") |
| Email unique parmi les comptes actifs, réutilisable après suppression logique | index partiel `ux_users_email … WHERE deleted_at IS NULL` | Convention 2.5 (soft delete + unicité) |

```mermaid
erDiagram
    audit_logs {
        uuid id PK
        uuid user_id FK
        string user_full_name
        string action
        string module
        string http_method
        string path
        string entity_type
        uuid entity_id
        string ip_address
        string user_agent
        boolean is_success
        string response_message
        timestamptz executed_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    company_profile {
        uuid id PK
        string company_name
        string address
        string depositary_license_number
        date depositary_license_issued_at
        date depositary_license_expires_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    permissions {
        uuid id PK
        string code UK
        string module
        string name
        string description
        boolean is_system
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    refresh_tokens {
        uuid id PK
        uuid user_id FK
        string token_hash UK
        timestamptz issued_at
        timestamptz expires_at
        timestamptz revoked_at
        string revoked_reason
        string created_ip
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    role_permissions {
        uuid id PK
        uuid role_id FK
        uuid permission_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    roles {
        uuid id PK
        string code UK
        string name
        string description
        boolean is_system
        boolean is_active
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    user_permission_exceptions {
        uuid id PK
        uuid user_id FK
        uuid permission_id FK
        boolean is_granted
        string reason
        timestamptz valid_from
        timestamptz valid_to
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    user_roles {
        uuid id PK
        uuid user_id FK
        uuid role_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    users {
        uuid id PK
        string email
        string password_hash
        string first_name
        string last_name
        string phone
        boolean is_active
        timestamptz last_login_at
        timestamptz last_password_change_at
        int failed_login_attempts
        timestamptz lockout_end_at
        uuid created_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    users |o--o{ audit_logs : "user"
    users ||--o{ refresh_tokens : "user"
    roles ||--o{ role_permissions : "role"
    permissions ||--o{ role_permissions : "permission"
    users ||--o{ user_permission_exceptions : "user"
    permissions ||--o{ user_permission_exceptions : "permission"
    users ||--o{ user_roles : "user"
    roles ||--o{ user_roles : "role"
    users |o--o{ users : "created by user"
```

### 4.2 Domaine 2 — Référentiel Commercial

**Rôle métier.** Porte le catalogue produit et les référentiels contrôlés (catégories, classes thérapeutiques, fournisseurs, clients) qui remplacent la saisie libre à l'origine des incohérences observées dans les fichiers Excel actuels (libellés fournisseur variables, doublons de désignation produit). Dépend uniquement du domaine Sécurité (`created_by`).

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Fiche fournisseur unique, pas de saisie libre du nom | `suppliers.name` (index unique partiel) | PRD_CLAUDE §2.3.1 (constat des doublons « HORIBA » / « HORIBA ABX SAS ») |
| Désignation produit unique parmi les produits actifs | `products.designation` (index unique partiel) | PRD_Qwen module 3 §US-REF-01 ; élimine les 9 doublons exacts constatés en Phase 1 |
| « Forme » = forme pharmaceutique réelle, distincte du conditionnement | `products.pharmaceutical_form` vs `products.dosage`/`carton_quantity` | PRD_CLAUDE §2.3.2 (incohérence Forme/Dosage entre les deux feuilles Excel, vérifiée en Phase 1) |
| La TVA n'est jamais déduite automatiquement de la seule catégorie | `categories.default_vat_rate` (défaut) + `products.vat_rate_override` (surcharge nullable) | PRD_CLAUDE §17.4 ; confirmé empiriquement par l'exception ABX DILUENT 20L (réactif à TVA 18%, Phase 1) |
| Un produit peut avoir plusieurs fournisseurs habituels, avec un fournisseur principal | `product_suppliers` (N:N), `is_primary` | PRD_CLAUDE §8.1.2 |
| Le « répartiteur » n'est pas une entité séparée, c'est un type de client | `customers.customer_type = 'repartiteur'` | PRD_CLAUDE §9.3 (décision documentée, cohérente avec les données réelles qui mélangent pharmacies/cliniques/répartiteurs) |
| Vérification de l'autorisation de distribution avant référencement fournisseur | `suppliers.distribution_authorization_verified` | PRD_CLAUDE §17.5 point 1 (BPD/WHO-GDP) |
| Vérification de la licence client avant livraison | `customers.license_verified` | PRD_CLAUDE §17.5 point 2 |
| Seuil d'alerte de péremption configurable par catégorie | `categories.expiry_alert_days` | PRD_Qwen-2 §2.4.5 (60 à 120 j selon catégorie) |
| Classes thérapeutiques et catégories comme listes contrôlées | `therapeutic_classes`, `categories` | PRD_CLAUDE §8.1.6 |

```mermaid
erDiagram
    categories {
        uuid id PK
        string code UK
        string name
        numeric default_vat_rate
        int expiry_alert_days
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    customers {
        uuid id PK
        string name
        string customer_type "pharmacie|clinique|hopital|centrale_achat|repartiteur|autre"
        string address
        string po_box
        string phone
        string city
        int payment_term_days
        numeric credit_limit_cfa
        boolean license_verified
        boolean is_active
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    product_suppliers {
        uuid id PK
        uuid product_id FK
        uuid supplier_id FK
        boolean is_primary
        string origin_country
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    products {
        uuid id PK
        string designation
        uuid category_id FK
        uuid therapeutic_class_id FK
        string pharmaceutical_form
        string dosage
        string unit_label
        int carton_quantity
        string cip_code
        uuid primary_supplier_id FK
        string default_origin_country
        string default_transport_mode "maritime|aerien|express|terrestre"
        numeric vat_rate_override
        int manufacturing_lead_time_days
        int delivery_lead_time_days
        int min_stock_threshold
        boolean requires_cold_chain
        boolean is_active
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    suppliers {
        uuid id PK
        string name
        string address
        string po_box
        string phone
        string country
        string default_currency FK
        boolean is_local
        boolean is_active
        boolean distribution_authorization_verified
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    therapeutic_classes {
        uuid id PK
        string name
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    products ||--o{ product_suppliers : "product"
    suppliers ||--o{ product_suppliers : "supplier"
    categories ||--o{ products : "category"
    therapeutic_classes |o--o{ products : "therapeutic class"
    suppliers |o--o{ products : "primary supplier"
    currencies ||--o{ suppliers : "default currency"
```

### 4.3 Domaine 3 — Pricing & Devises

**Rôle métier.** Gère les devises et taux de change multi-monnaies, et le moteur de tarification à cascade de coefficients — cœur différenciant de LABMEDIS, vérifié terme à terme sur les 17 produits France Lait de `Structure_de_prix.xlsx`. Dépend du Référentiel Commercial (produits, catégories, fournisseurs).

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Référentiel des devises de gestion (EUR, USD, XOF), 0 décimale pour le XOF | `currencies.code`, `decimal_places` | PRD_CLAUDE §8.10.1 ; règle 10.3 (pas de centime en XOF) |
| Prix de revient = cascade **multiplicative** de coefficients | `pricing_profiles.commission_coeff / freight_coeff / transit_coeff / transfer_fee_coeff`, appliqués à `stock_lots.unit_cost_cfa` | PRD_Qwen-1 §1.1 ; vérifié à l'identique (3358,82 → 3359 CFA) dans le scénario §8 |
| Coefficients jamais codés en dur, ajustables par la direction sans redéploiement | `pricing_profiles` (table de configuration, pas de constante applicative) | PRD_Qwen-1 §1.2 |
| Le taux EUR/XOF est fixe (655,957), non modifiable sans intervention explicite | `exchange_rates` (rien n'empêche en base une nouvelle ligne EUR/XOF ; le caractère « fixe » est une politique applicative, volontairement non figée en contrainte SQL — règle 2.5 : jamais de contrainte sur une valeur mouvante) | PRD_CLAUDE §10.3 ; vérifié exact (2236,81337 / 3,41 = 655,957) en Phase 1 |
| Le taux appliqué à une commande est figé et non recalculé après coup | `purchase_orders.locked_exchange_rate_id` (domaine 4, FK vers `exchange_rates`) | PRD_Qwen-1 §1.3 ; PRD_CLAUDE §10.3 |
| Historique complet des prix, une seule ligne « courante » par produit | `product_prices` (table append-only, `effective_to IS NULL` = prix courant, index unique partiel) | PRD_CLAUDE §8.6.7 ("historiser toute évolution de prix") |
| L'écart entre prix calculé et prix pratiqué est conservé, jamais écrasé | `product_prices.pv_ht_calculated` vs `pv_ht_applied` | Règle 10.8 (PRD_CLAUDE) ; vérifié (-35 CFA) dans le scénario |
| Le prix de revient stocké est arrondi à l'entier CFA (pas de centime) | `stock_lots.unit_cost_cfa NUMERIC(14,0)`, `product_prices.*_cfa NUMERIC(14,0)` | PRD_Qwen-1 §1.5 (`ToCfaRounded()`, `MidpointRounding.AwayFromZero`) |
| Une simulation de prix n'est pas une décision publiée | `pricing_simulations`, distincte de `product_prices` | PRD_Qwen module 3 §US-PRICE-01 |

```mermaid
erDiagram
    currencies {
        string code PK
        string name
        string symbol
        smallint decimal_places
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    exchange_rates {
        uuid id PK
        string from_currency FK
        string to_currency FK
        numeric rate
        date effective_date
        string source "manuel|api|import"
        uuid created_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    pricing_profiles {
        uuid id PK
        string name
        uuid supplier_id FK
        uuid category_id FK
        string transport_mode "maritime|aerien|express|terrestre"
        numeric commission_coeff
        numeric freight_coeff
        numeric transit_coeff
        numeric transfer_fee_coeff
        numeric target_margin_coeff
        boolean is_active
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    pricing_simulations {
        uuid id PK
        uuid product_id FK
        uuid pricing_profile_id FK
        numeric purchase_price_foreign
        string purchase_currency FK
        numeric exchange_rate_used
        numeric landing_cost_cfa
        numeric target_price_ht_cfa
        numeric catalog_price_ht_cfa
        uuid simulated_by_user_id FK
        timestamptz simulated_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    product_prices {
        uuid id PK
        uuid product_id FK,UK
        uuid pricing_profile_id FK
        numeric pr_unit_cfa
        numeric pv_ht_calculated
        numeric pv_ht_applied
        numeric vat_rate
        date effective_from
        date effective_to
        uuid created_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    currencies ||--o{ exchange_rates : "from currency"
    currencies ||--o{ exchange_rates : "to currency"
    users |o--o{ exchange_rates : "created by user"
    suppliers |o--o{ pricing_profiles : "supplier"
    categories |o--o{ pricing_profiles : "category"
    products |o--o{ pricing_simulations : "product"
    pricing_profiles |o--o{ pricing_simulations : "pricing profile"
    currencies ||--o{ pricing_simulations : "purchase currency"
    users |o--o{ pricing_simulations : "simulated by user"
    products ||--o{ product_prices : "product"
    pricing_profiles |o--o{ product_prices : "pricing profile"
    users |o--o{ product_prices : "created by user"
```

### 4.4 Domaine 4 — Achats & Logistique Internationale

**Rôle métier.** Pilote les commandes fournisseurs multi-devises, le suivi des expéditions (maritime/aérien/express/terrestre) et le transit douanier. C'est le domaine où le **[CONTRADICTION]** entre sources a été résolu (cf. §9.1) : la cardinalité commande↔expédition. Dépend du Référentiel Commercial et de Pricing & Devises.

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Le taux de change est figé à la validation de la commande | `purchase_orders.locked_exchange_rate_id` | PRD_Qwen-1 §1.3 |
| Circuit de validation par seuil de montant (au-delà, validation Direction obligatoire) | `purchase_orders.status = 'en_attente_validation'` (statut intermédiaire), seuil laissé en configuration applicative (pas de montant figé en SQL, cf. règle 2.5) | PRD_CLAUDE §20.2 |
| Une commande peut être expédiée en plusieurs fois **et** une expédition peut consolider plusieurs commandes | `shipment_lines.purchase_order_line_id` (jamais de FK directe `shipments → purchase_orders`) | **[CONTRADICTION]** PRD_CLAUDE §9.1/§8.3.3 (N:1) vs PRD_Qwen module 3 §3.5.4 (N:N) — résolu en N:N au niveau ligne, cf. §9.1 |
| Le régime douanier est un référentiel contrôlé (OTR) | `shipments.customs_regime` | PRD_CLAUDE §17.3 (liste des 11 régimes OTR) |
| Une autorisation d'importation DPML peut être associée à l'expédition | `shipments.import_authorization_number/date` | PRD_CLAUDE §17.2 |
| Les frais logistiques réels sont alloués par expédition, méthode configurable | `import_costs.allocation_method` (valeur/quantité/volume) | PRD_Qwen module 3 §US-LOG-02 ; **registre comptable, n'alimente pas le PRU du lot** — cf. [CONTRADICTION] pricing §9.1 |
| Chaque changement de statut de commande est horodaté et consultable | `purchase_order_status_history` | PRD_Qwen module 3 §US-ACH-03 ; vérifié dans le scénario (4 statuts historisés pour PO-2026-000456) |
| Le mode de transport influence le calcul du prix de revient | `stock_lots.transport_mode`, cohérent avec `pricing_profiles.transport_mode` | PRD_Qwen-1 §1.2 ; PRD_brut.md (transcript audio d'origine) |
| Timeline des événements de transport | `shipment_events` | PRD_Qwen module 3 §US-LOG-03 ; vérifié (4 événements pour EXP-2026-000789) |

```mermaid
erDiagram
    import_costs {
        uuid id PK
        uuid shipment_id FK
        string cost_type "freight|transit|douane|commission|frais_transfert|assuran..."
        numeric amount
        string currency FK
        string allocation_method "valeur|quantite|volume"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    purchase_order_lines {
        uuid id PK
        uuid purchase_order_id FK
        uuid product_id FK
        int quantity_ordered_units
        int quantity_ordered_cartons
        numeric unit_price_foreign
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    purchase_order_status_history {
        uuid id PK
        uuid purchase_order_id FK
        string status
        string comment
        uuid changed_by_user_id FK
        timestamptz changed_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    purchase_orders {
        uuid id PK
        string order_number UK
        uuid supplier_id FK
        string currency FK
        uuid locked_exchange_rate_id FK
        string incoterm
        string status "brouillon|en_attente_validation|validee|envoyee|en_fabric..."
        date order_date
        date expected_delivery_date
        string cancellation_reason
        uuid validated_by_user_id FK
        timestamptz validated_at
        uuid created_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    shipment_events {
        uuid id PK
        uuid shipment_id FK
        string event_status
        string description
        timestamptz event_date
        uuid recorded_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    shipment_lines {
        uuid id PK
        uuid shipment_id FK
        uuid purchase_order_line_id FK
        int quantity_shipped_units
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    shipments {
        uuid id PK
        string shipment_reference UK
        string transport_mode "maritime|aerien|express|terrestre"
        string carrier
        string transport_reference
        string customs_regime
        string import_authorization_number
        date import_authorization_date
        string status "preparee|expediee|en_transit|dedouanement|receptionnee|an..."
        date departure_date_estimated
        date departure_date_actual
        date arrival_date_estimated
        date arrival_date_actual
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    shipments ||--o{ import_costs : "shipment"
    currencies ||--o{ import_costs : "currency"
    purchase_orders ||--o{ purchase_order_lines : "purchase order"
    products ||--o{ purchase_order_lines : "product"
    purchase_orders ||--o{ purchase_order_status_history : "purchase order"
    users |o--o{ purchase_order_status_history : "changed by user"
    suppliers ||--o{ purchase_orders : "supplier"
    currencies ||--o{ purchase_orders : "currency"
    exchange_rates |o--o{ purchase_orders : "locked exchange rate"
    users |o--o{ purchase_orders : "validated by user"
    users |o--o{ purchase_orders : "created by user"
    shipments ||--o{ shipment_events : "shipment"
    users |o--o{ shipment_events : "recorded by user"
    shipments ||--o{ shipment_lines : "shipment"
    purchase_order_lines ||--o{ shipment_lines : "purchase order line"
```

### 4.5 Domaine 5 — Entreposage & Stock

**Rôle métier.** Cœur de la conformité pharmaceutique : traçabilité par lot, adressage physique hiérarchique, mouvements de stock, inventaires. Porte les statuts qualité (quarantaine, libération, péremption) et la règle FEFO. Dépend du Référentiel Commercial et des Achats (origine des lots).

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Un lot est l'unité de traçabilité de base | `stock_lots` | PRD_brut.md ; PRD_CLAUDE §4 point 3 |
| Numéro de lot unique par couple fournisseur/produit | index unique partiel `ux_stock_lots_supplier_product_batch` | PRD_CLAUDE règle 10.1 |
| Quantité reçue indépendante du conditionnement standard (un carton peut varier d'un lot à l'autre) | `stock_lots.initial_quantity` (unités réelles) + `carton_quantity_received` (cartons réellement comptés) | PRD_brut.md (« si un carton a 40 produits, on enregistre 40 produits, mais on garde aussi que c'est venu dans des cartons ») ; règle 10.2 |
| Statut « en attente de libération » — un dépositaire stocke des lots non encore libérés par le fabricant, mais ne peut les distribuer avant libération formelle | `stock_lots.status = 'en_attente_liberation'` | PRD_CLAUDE §17.5 point 6 (BPD/WHO-GDP) — **absent de l'énumération initiale de PRD_Qwen-2 §2.13.2**, ajouté ici suite à la recherche réglementaire |
| Seuls les lots libérés peuvent être proposés à la vente | `stock_lots.status = 'libere'` filtré par l'index `ix_stock_lots_product_fefo` | PRD_Qwen-2 §2.4.6 ; vérifié par `EXPLAIN` en §8 |
| Un même lot peut être stocké à plusieurs emplacements | `stock_lot_locations` (N:N produit-emplacement, avec quantité réservée) | PRD_Qwen-2 §2.3.3 |
| Emplacement hiérarchique (zone/allée/rack/niveau) | `storage_locations.parent_location_id` (auto-référence) | PRD_Qwen-2 §2.3.3/§2.5.1 ; vérifié dans le scénario (ZONE-A → A-01-02-01) |
| Zone de quarantaine et zone de périmés obligatoires | `storage_locations.location_type IN ('quarantaine','perimes',...)` | PRD_Qwen-2 §2.5.3 |
| FEFO par défaut, dérogation manuelle tracée | `sale_order_lines.is_fefo_override` (domaine 6) s'appuie sur `stock_lots.expiry_date` | Règle 10.5 |
| Stock disponible = physique − réservé − quarantaine − périmé | `stock_lot_locations.quantity - reserved_quantity`, filtré par `stock_lots.status` | PRD_Qwen-2 §2.4.3 ; vérifié (cohérence stock §8) |
| Toute entrée/sortie est tracée (utilisateur, date, motif, référence source) | `stock_movements` + `stock_movement_lines`, référence polymorphe `source_document_type/id` | PRD_Qwen-2 §2.4.1/§2.4.2/§2.6.2 ; règle 2.5 (référence polymorphe documentée) |
| Écart d'inventaire jamais figé en contrainte SQL (calculé en couche service) | `inventory_counts.system_quantity` / `counted_quantity` (deux colonnes brutes, pas de colonne générée) | Règle 2.5 |

```mermaid
erDiagram
    inventory_counts {
        uuid id PK
        uuid inventory_session_id FK
        uuid product_id FK
        uuid stock_lot_id FK
        uuid storage_location_id FK
        int system_quantity
        int counted_quantity
        string adjustment_reason
        uuid counted_by_user_id FK
        timestamptz counted_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    inventory_sessions {
        uuid id PK
        string reference UK
        string inventory_type "general|zone|produit|lot|categorie|tournant"
        string status "en_cours|validee|cloturee|annulee"
        uuid warehouse_id FK
        uuid started_by_user_id FK
        timestamptz started_at
        timestamptz closed_at
        string comments
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    stock_lot_locations {
        uuid id PK
        uuid stock_lot_id FK
        uuid storage_location_id FK
        int quantity
        int reserved_quantity
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    stock_lots {
        uuid id PK
        uuid product_id FK
        uuid supplier_id FK
        uuid purchase_order_line_id FK
        uuid shipment_line_id FK
        uuid pricing_profile_id FK
        string supplier_batch_number
        string transport_mode "maritime|aerien|express|terrestre"
        date reception_date
        date expiry_date
        string status "en_reception|quarantaine|en_attente_liberation|libere|non..."
        string quality_hold_reason
        int initial_quantity
        int remaining_quantity
        int carton_quantity_received
        numeric unit_cost_cfa
        string reception_discrepancy_reason
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    stock_movement_lines {
        uuid id PK
        uuid stock_movement_id FK
        uuid product_id FK
        uuid stock_lot_id FK
        uuid source_location_id FK
        uuid destination_location_id FK
        int quantity
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    stock_movements {
        uuid id PK
        string reference UK
        string movement_type "reception_fournisseur|mise_en_stock|transfert|vente|retou..."
        timestamptz movement_date
        uuid user_id FK
        string reason
        string source_document_type
        uuid source_document_id
        string status "brouillon|valide|annule"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    storage_locations {
        uuid id PK
        uuid warehouse_id FK
        uuid parent_location_id FK
        string code
        string name
        string location_type "reception|quarantaine|stockage|picking|reserve|chaine_fro..."
        int capacity
        boolean is_active
        boolean is_locked
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    warehouses {
        uuid id PK
        string code UK
        string name
        string address
        boolean is_active
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    inventory_sessions ||--o{ inventory_counts : "inventory session"
    products ||--o{ inventory_counts : "product"
    stock_lots |o--o{ inventory_counts : "stock lot"
    storage_locations ||--o{ inventory_counts : "storage location"
    users |o--o{ inventory_counts : "counted by user"
    warehouses |o--o{ inventory_sessions : "warehouse"
    users |o--o{ inventory_sessions : "started by user"
    stock_lots ||--o{ stock_lot_locations : "stock lot"
    storage_locations ||--o{ stock_lot_locations : "storage location"
    products ||--o{ stock_lots : "product"
    suppliers ||--o{ stock_lots : "supplier"
    purchase_order_lines |o--o{ stock_lots : "purchase order line"
    shipment_lines |o--o{ stock_lots : "shipment line"
    pricing_profiles |o--o{ stock_lots : "pricing profile"
    stock_movements ||--o{ stock_movement_lines : "stock movement"
    products ||--o{ stock_movement_lines : "product"
    stock_lots ||--o{ stock_movement_lines : "stock lot"
    storage_locations |o--o{ stock_movement_lines : "source location"
    storage_locations |o--o{ stock_movement_lines : "destination location"
    users ||--o{ stock_movements : "user"
    warehouses ||--o{ storage_locations : "warehouse"
    storage_locations |o--o{ storage_locations : "parent location"
```

### 4.6 Domaine 6 — Ventes & Facturation

**Rôle métier.** Couvre l'intégralité du flux commercial : commande client avec allocation FEFO, livraison, facturation, retours et avoirs. Regroupe volontairement Ventes et Facturation dans un seul domaine (11 tables, à la limite haute de la fourchette 5-12) : dans les sources, ces deux sous-processus s'enchaînent comme un seul flux séquentiel (PRD_Qwen module 3, workflows 7/9/10/11), pas comme deux domaines indépendants. Dépend du Référentiel Commercial, de l'Entreposage & Stock (lots) et de Pricing & Devises (devises).

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Un client peut avoir un tarif négocié, prioritaire sur le catalogue | `customer_product_prices`, fenêtre de validité | PRD_Qwen module 3 §US-VEN-05 |
| Le stock est réservé à la confirmation, sans décrémenter le stock physique avant sortie réelle | `stock_lot_locations.reserved_quantity` (mis à jour par le service applicatif à la confirmation de `sale_orders`) | PRD_Qwen-2 §2.9.1 ; PRD_Qwen module 3 §US-VEN-04 |
| Le lot alloué et son prix/TVA sont figés à la vente | `sale_order_lines.allocated_stock_lot_id`, `unit_price_ht_cfa`, `vat_rate` | Règle 2.5 (figé en instantané) |
| Toute dérogation à l'allocation FEFO est tracée avec motif | `sale_order_lines.is_fefo_override`, `fefo_override_reason` | PRD_Qwen-2 §2.4.4 (option alternative, motif obligatoire) |
| Une commande peut être livrée en plusieurs fois | `deliveries` (1:N depuis `sale_orders`) | PRD_Qwen module 3 §3.11.3 |
| Le n° de lot figure en pied de facture (traçabilité légale) | `invoice_lines.stock_lot_id` | PRD_CLAUDE §18.2 (gabarit facture/BL) |
| Numérotation de facture unique | index unique partiel `ux_invoices_number` | PRD_Qwen module 3 §3.12.3 |
| Un retour référence toujours la vente et le lot d'origine | `customer_returns.sale_order_line_id` (RESTRICT), `original_stock_lot_id` | PRD_CLAUDE §20.3.1 |
| Un retour a une décision explicite parmi 4 issues | `customer_returns.decision IN ('remise_stock','quarantaine','destruction','refus')` | PRD_Qwen module 3 §3.13.3 ; PRD_CLAUDE §20.3.2 |
| Un retour accepté génère un avoir | `customer_returns.credit_note_id` → `credit_notes` | PRD_CLAUDE §20.3.2 ; vérifié dans le scénario (RET-2026-000015 → AV-2026-000031) |
| Aucun chevauchement de tarif négocié pour un même couple client/produit | contrôle applicatif sur `customer_product_prices` (requête de détection en §8, exécutée et vérifiée) | Cohérence métier ; pas une contrainte `EXCLUDE` PostgreSQL ici (préféré en couche service pour permettre un message d'erreur métier clair) |

```mermaid
erDiagram
    credit_note_lines {
        uuid id PK
        uuid credit_note_id FK
        uuid invoice_line_id FK
        uuid product_id FK
        int quantity
        numeric unit_price_ht_cfa
        numeric vat_rate
        numeric line_total_ttc_cfa
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    credit_notes {
        uuid id PK
        string credit_note_number UK
        uuid invoice_id FK
        uuid customer_id FK
        string reason
        numeric total_ht_cfa
        numeric total_vat_cfa
        numeric total_ttc_cfa
        date issued_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    customer_product_prices {
        uuid id PK
        uuid customer_id FK
        uuid product_id FK
        numeric unit_price_ht_cfa
        date valid_from
        date valid_to
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    customer_returns {
        uuid id PK
        string return_number UK
        uuid customer_id FK
        uuid sale_order_line_id FK
        uuid original_stock_lot_id FK
        int quantity
        string reason
        string decision "en_attente|remise_stock|quarantaine|destruction|refus"
        uuid credit_note_id FK
        uuid decided_by_user_id FK
        timestamptz decided_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    deliveries {
        uuid id PK
        string delivery_number UK
        uuid sale_order_id FK
        date delivery_date
        string status "brouillon|confirmee|annulee"
        uuid delivered_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    delivery_lines {
        uuid id PK
        uuid delivery_id FK
        uuid sale_order_line_id FK
        uuid stock_lot_id FK
        int quantity_delivered
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    invoice_lines {
        uuid id PK
        uuid invoice_id FK
        uuid product_id FK
        uuid stock_lot_id FK
        int quantity
        numeric unit_price_ht_cfa
        numeric vat_rate
        numeric line_total_ht_cfa
        numeric line_total_ttc_cfa
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    invoices {
        uuid id PK
        string invoice_number UK
        uuid customer_id FK
        uuid sale_order_id FK
        string currency FK
        string status "emise|payee|annulee"
        date invoice_date
        numeric total_ht_cfa
        numeric total_vat_cfa
        numeric total_ttc_cfa
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    sale_order_lines {
        uuid id PK
        uuid sale_order_id FK
        uuid product_id FK
        uuid allocated_stock_lot_id FK
        int quantity
        numeric unit_price_ht_cfa
        numeric vat_rate
        boolean is_fefo_override
        string fefo_override_reason
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    sale_order_status_history {
        uuid id PK
        uuid sale_order_id FK
        string status
        string comment
        uuid changed_by_user_id FK
        timestamptz changed_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    sale_orders {
        uuid id PK
        string order_number UK
        uuid customer_id FK
        string currency FK
        string status "brouillon|devis|confirmee|reservee|en_preparation|prete|p..."
        date order_date
        boolean is_exceptional_sale
        numeric total_ht_cfa
        numeric total_vat_cfa
        numeric total_ttc_cfa
        uuid created_by_user_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    credit_notes ||--o{ credit_note_lines : "credit note"
    invoice_lines |o--o{ credit_note_lines : "invoice line"
    products ||--o{ credit_note_lines : "product"
    invoices |o--o{ credit_notes : "invoice"
    customers ||--o{ credit_notes : "customer"
    customers ||--o{ customer_product_prices : "customer"
    products ||--o{ customer_product_prices : "product"
    customers ||--o{ customer_returns : "customer"
    sale_order_lines ||--o{ customer_returns : "sale order line"
    stock_lots |o--o{ customer_returns : "original stock lot"
    credit_notes |o--o{ customer_returns : "credit note"
    users |o--o{ customer_returns : "decided by user"
    sale_orders ||--o{ deliveries : "sale order"
    users |o--o{ deliveries : "delivered by user"
    deliveries ||--o{ delivery_lines : "delivery"
    sale_order_lines ||--o{ delivery_lines : "sale order line"
    stock_lots ||--o{ delivery_lines : "stock lot"
    invoices ||--o{ invoice_lines : "invoice"
    products ||--o{ invoice_lines : "product"
    stock_lots |o--o{ invoice_lines : "stock lot"
    customers ||--o{ invoices : "customer"
    sale_orders |o--o{ invoices : "sale order"
    currencies ||--o{ invoices : "currency"
    sale_orders ||--o{ sale_order_lines : "sale order"
    products ||--o{ sale_order_lines : "product"
    stock_lots |o--o{ sale_order_lines : "allocated stock lot"
    sale_orders ||--o{ sale_order_status_history : "sale order"
    users |o--o{ sale_order_status_history : "changed by user"
    customers ||--o{ sale_orders : "customer"
    currencies ||--o{ sale_orders : "currency"
    users |o--o{ sale_orders : "created by user"
```

### 4.7 Domaine 7 — Prévision & Réapprovisionnement (MRP)

**Rôle métier.** Anticipe les ruptures de stock à partir des délais de fabrication/transport/transit et de la consommation historique, puis déclenche des suggestions de commande. Domaine intentionnellement compact (4 tables, en dessous de la fourchette indicative 5-12) : chacune correspond exactement à une entité déjà définie et exécutable dans PRD_Qwen-4 §4.16.1, sans table ajoutée par convenance pour atteindre un chiffre. Dépend du Référentiel Commercial et des Achats.

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Le MRP est activable/désactivable par produit | `forecast_parameters.is_enabled` | PRD_Qwen-4 §4.16.1 |
| Lead time total = fabrication + préparation + transport + douane + interne | `supplier_lead_times.manufacturing_lead_time_days + preparation_lead_time_days + transport_lead_time_days + customs_lead_time_days + internal_lead_time_days` | PRD_Qwen-4 §4.3.3 ; formule reproduite et vérifiée dans le scénario (110 j) |
| Délais surchargeables par produit et par mode de transport | `supplier_lead_times.product_id` (nullable), `transport_mode` | PRD_Qwen-4 §4.7.1/§4.7.2 |
| Point de commande = conso. moyenne × lead time + stock de sécurité | `forecast_calculations.reorder_point` | PRD_Qwen-4 §4.4.2 ; vérifié (2500) |
| Chaque calcul MRP est un instantané daté, servant aussi d'historique | `forecast_calculations` (append-only, `calculation_date`) | PRD_Qwen-4 §4.16.1 (`ForecastCalculation`) |
| Une suggestion est convertible en commande fournisseur, ou rejetable avec motif | `reorder_suggestions.status`, `purchase_order_id`, `rejection_reason` | PRD_Qwen-4 §4.13/§US-MRP-08/09 ; vérifié dans le scénario (suggestion urgente générée, produit sous couverture) |
| La consommation historique n'est pas dupliquée : lue depuis le domaine Reporting | pas de table dédiée — `forecast_calculations.average_daily_consumption` est calculé à partir de `daily_sales_summary` (domaine 8) | Décision d'arbitrage — évite une double source de vérité entre ce domaine et le domaine Reporting |
| Un coefficient de saisonnalité unique par produit (pas de courbe mensuelle) | `forecast_parameters.seasonality_factor` | PRD_Qwen-4 §4.16.1 (entité C# de référence, plus stricte que la prose §4.11 qui évoque des « périodes ») — simplification documentée, cf. §10 recommandations V2 |

```mermaid
erDiagram
    forecast_calculations {
        uuid id PK
        uuid product_id FK
        date calculation_date
        int available_stock
        int reserved_stock
        int transit_stock
        numeric average_daily_consumption
        int lead_time_days
        int safety_stock
        int reorder_point
        int target_stock
        int net_requirement
        int coverage_days
        string risk_level "normal|a_surveiller|urgent|critique|surstock"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    forecast_parameters {
        uuid id PK
        uuid product_id FK,UK
        boolean is_enabled
        int forecast_horizon_days
        int safety_stock
        int target_coverage_days
        int overstock_threshold_days
        string consumption_method "moyenne_30j|moyenne_60j|moyenne_90j|ponderee_90j|saisonni..."
        numeric seasonality_factor
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    reorder_suggestions {
        uuid id PK
        uuid forecast_calculation_id FK
        uuid product_id FK
        uuid supplier_id FK
        int suggested_quantity_units
        int suggested_quantity_cartons
        string suggested_transport_mode "maritime|aerien|express|terrestre"
        date suggested_order_date
        date estimated_reception_date
        string status "en_attente|validee|convertie|rejetee|expiree"
        string rejection_reason
        uuid purchase_order_id FK
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    supplier_lead_times {
        uuid id PK
        uuid supplier_id FK
        uuid product_id FK
        string transport_mode "maritime|aerien|express|terrestre"
        int manufacturing_lead_time_days
        int preparation_lead_time_days
        int transport_lead_time_days
        int customs_lead_time_days
        int internal_lead_time_days
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    products ||--o{ forecast_calculations : "product"
    products ||--o{ forecast_parameters : "product"
    forecast_calculations ||--o{ reorder_suggestions : "forecast calculation"
    products ||--o{ reorder_suggestions : "product"
    suppliers ||--o{ reorder_suggestions : "supplier"
    purchase_orders |o--o{ reorder_suggestions : "purchase order"
    suppliers ||--o{ supplier_lead_times : "supplier"
    products |o--o{ supplier_lead_times : "product"
```

### 4.8 Domaine 8 — Reporting, Agrégations & Notifications

**Rôle métier.** Fournit des tables d'agrégation pré-calculées pour les tableaux de bord (évite de scanner tout le grand livre des ventes/stock à chaque affichage) et persiste les notifications temps réel pour qu'elles restent consultables hors connexion. Dépend du Référentiel Commercial et de la Sécurité (destinataire).

| Règle de gestion | Table / colonne | Source |
|---|---|---|
| Synthèse quotidienne des ventes par client/produit/catégorie | `daily_sales_summary` | PRD_Qwen-6 §6.24.1 (`DailySalesSummary`) ; alimente aussi la consommation MRP (domaine 7) |
| Synthèse quotidienne du stock (physique/réservé/disponible/quarantaine/périmé/valorisé) | `daily_stock_summary` | PRD_Qwen-6 §6.24.1 (`DailyStockSummary`) |
| Synthèse quotidienne du risque de rupture, distincte du détail de calcul | `daily_forecast_summary` | PRD_Qwen-6 §6.24.1 (`DailyForecastSummary`) — vue dashboard condensée, le détail complet reste dans `forecast_calculations` (domaine 7) |
| Synthèse financière mensuelle (CA, TVA, coût des ventes, marge, achats, logistique) | `monthly_financial_summary` | PRD_Qwen-6 §6.24.1 (`MonthlyFinancialSummary`) |
| Une notification temps réel doit rester retrouvable hors ligne | `notifications.is_read`, persistée en base (pas uniquement poussée via SignalR) | PRD_Qwen-5 §5.19.3 |
| Une notification peut cibler un utilisateur ou un rôle entier | `notifications.recipient_user_id` / `recipient_role_id` (au moins l'un des deux, contrôle applicatif) | PRD_Qwen-5 §5.27.1 (notifications filtrées par rôle) ; vérifié dans le scénario (alerte ReorderSuggestionCreated → rôle Achats) |
| La source d'une notification peut être n'importe quelle entité métier | `notifications.source_document_type/id` (référence polymorphe, sans FK stricte) | Règle 2.5 |

```mermaid
erDiagram
    daily_forecast_summary {
        uuid id PK
        date summary_date
        uuid product_id FK
        int available_stock
        int transit_stock
        numeric average_daily_consumption
        int coverage_days
        int reorder_point
        int net_requirement
        string risk_level "normal|a_surveiller|urgent|critique|surstock"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    daily_sales_summary {
        uuid id PK
        date sales_date
        uuid customer_id FK
        uuid product_id FK
        uuid category_id FK
        int quantity_sold
        numeric total_amount_ht_cfa
        numeric total_vat_cfa
        numeric total_amount_ttc_cfa
        numeric total_cost_cfa
        numeric gross_margin_cfa
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    daily_stock_summary {
        uuid id PK
        date summary_date
        uuid product_id FK
        uuid category_id FK
        uuid supplier_id FK
        int physical_stock
        int reserved_stock
        int available_stock
        int quarantine_stock
        int expired_stock
        numeric stock_value_cfa
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    monthly_financial_summary {
        uuid id PK
        int summary_year
        smallint summary_month
        numeric total_sales_ht_cfa
        numeric total_vat_cfa
        numeric total_sales_ttc_cfa
        numeric total_cost_of_goods_sold_cfa
        numeric gross_margin_cfa
        numeric purchase_amount_cfa
        numeric logistics_cost_cfa
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    notifications {
        uuid id PK
        string notification_type
        uuid recipient_user_id FK
        uuid recipient_role_id FK
        string channel "signalr|email|sms"
        string title
        string message
        string source_document_type
        uuid source_document_id
        boolean is_read
        timestamptz read_at
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at "soft delete"
    }
    products ||--o{ daily_forecast_summary : "product"
    customers |o--o{ daily_sales_summary : "customer"
    products |o--o{ daily_sales_summary : "product"
    categories |o--o{ daily_sales_summary : "category"
    products ||--o{ daily_stock_summary : "product"
    categories |o--o{ daily_stock_summary : "category"
    suppliers |o--o{ daily_stock_summary : "supplier"
    users |o--o{ notifications : "recipient user"
    roles |o--o{ notifications : "recipient role"
```

---

## 5. Flux critiques — diagrammes de séquence

Quatre flux ont été retenus parmi les processus les plus complexes ou les plus sensibles du système (fourchette 2-5 demandée) : le calcul du prix de revient à la réception (cascade de coefficients), l'allocation FEFO jusqu'à la facturation, la boucle de réapprovisionnement automatique (MRP), et l'authentification avec vérification RBAC en direct — ce dernier flux étant explicitement cité en exemple de « flux critique » par la mission. Les 4 fichiers sont validés par le parseur Mermaid (`type=sequence`).

### 5.1 Réception de conteneur → création de lots avec PRU

```mermaid
sequenceDiagram
    autonumber
    actor Mag as Magasinier
    participant API as API .NET (StockReceptionService)
    participant PO as purchase_orders / purchase_order_lines
    participant SH as shipments / shipment_lines
    participant PP as pricing_profiles
    participant ER as exchange_rates
    participant SL as stock_lots
    participant SLL as stock_lot_locations
    participant SM as stock_movements
    participant Hub as SignalR Hub

    Mag->>API: POST /api/stock-receptions (shipment_id, lignes comptées)
    API->>SH: Lit shipment_lines attendues (quantité expédiée)
    API->>PO: Lit purchase_order_lines liées (prix d'achat, devise)
    API->>ER: Lit le taux de change figé sur la commande (locked_exchange_rate_id)
    API->>API: Calcule l'écart quantité reçue vs quantité expédiée
    alt Écart détecté (manquant / excédent)
        API-->>Mag: Demande un motif obligatoire (reception_discrepancy_reason)
    end
    API->>PP: Sélectionne le profil de coefficients (fournisseur + catégorie + mode transport)
    API->>API: PR = PA_CFA × commission × freight × transit × frais_transfert (arrondi CFA)
    API->>SL: INSERT stock_lots (statut = en_reception, unit_cost_cfa calculé)
    API->>SLL: INSERT stock_lot_locations (emplacement de réception)
    API->>SM: INSERT stock_movements (type = reception_fournisseur) + lignes
    API->>Hub: Notifie ReceptionValidated
    Hub-->>Mag: Toast temps réel

    Note over Mag,API: Étape distincte — contrôle qualité
    actor Qual as Responsable Qualité
    Qual->>API: PUT /api/stock-lots/{id}/status (libere)
    API->>SL: UPDATE status = libere (traçabilité : utilisateur, date)
    API->>Hub: Notifie QualityLotReleased
    Note right of SL: Lot vendable uniquement à partir de ce statut (règle FEFO §2.4.6)
```

### 5.2 Commande client → allocation FEFO → livraison → facturation

```mermaid
sequenceDiagram
    autonumber
    actor Com as Commercial
    participant API as API .NET (SaleOrderService)
    participant CPP as customer_product_prices
    participant PPr as product_prices
    participant SL as stock_lots
    participant SLL as stock_lot_locations
    participant SO as sale_orders / sale_order_lines
    participant DEL as deliveries / delivery_lines
    participant SM as stock_movements
    participant INV as invoices / invoice_lines
    participant Hub as SignalR Hub

    Com->>API: POST /api/sale-orders (client, lignes produit+quantité)
    API->>CPP: Recherche un tarif négocié valide à la date (customer_id, product_id)
    alt Tarif négocié trouvé
        API->>API: Utilise customer_product_prices.unit_price_ht_cfa
    else Aucun tarif négocié
        API->>PPr: Lit le prix catalogue courant (effective_to IS NULL)
    end
    API->>SL: Sélectionne les lots libérés du produit, triés par expiry_date ASC (FEFO)
    Note right of SL: Index ix_stock_lots_product_fefo — vérifié par EXPLAIN (Phase 3.4)
    API->>SLL: Vérifie stock disponible = quantity - reserved_quantity
    alt Stock insuffisant
        API-->>Com: Erreur "stock disponible insuffisant"
    end
    API->>SO: INSERT sale_orders + sale_order_lines (allocated_stock_lot_id, prix et TVA figés)
    API->>SLL: UPDATE reserved_quantity += quantité (réservation, stock physique inchangé)
    API->>Hub: Notifie StockReservationCreated

    Note over Com,API: Étape distincte — préparation et livraison
    actor Prep as Préparateur
    Prep->>API: POST /api/deliveries (sale_order_id)
    API->>DEL: INSERT deliveries + delivery_lines (lot, quantité livrée)
    API->>SM: INSERT stock_movements (type = vente) + lignes
    API->>SLL: UPDATE quantity -= quantité livrée, reserved_quantity -= quantité (sortie physique réelle)
    API->>SO: UPDATE status = livree

    Note over Com,API: Étape distincte — facturation
    actor Compt as Comptable
    Compt->>API: POST /api/invoices (sale_order_id)
    API->>INV: INSERT invoices + invoice_lines (n° de lot conservé, TVA figée)
    API->>SO: UPDATE status = facturee
    API->>Hub: Notifie InvoiceGenerated
    Hub-->>Com: Toast temps réel
```

### 5.3 Alerte de réapprovisionnement (job MRP quotidien) → suggestion → conversion en commande

```mermaid
sequenceDiagram
    autonumber
    participant Cron as Hangfire (05h00 UTC)
    participant Job as DailyMrpCalculationJob
    participant FP as forecast_parameters
    participant DSS as daily_sales_summary
    participant SL as stock_lots / stock_lot_locations
    participant PO as purchase_orders (stock en transit)
    participant SLT as supplier_lead_times
    participant FC as forecast_calculations
    participant RS as reorder_suggestions
    participant Hub as SignalR Hub
    actor Ach as Responsable Achats

    Cron->>Job: Déclenche l'exécution quotidienne
    Job->>FP: Liste les produits avec is_enabled = true
    loop Pour chaque produit suivi
        Job->>DSS: Lit la consommation historique (90 derniers jours)
        Job->>SL: Calcule stock disponible = Σ(quantity - reserved_quantity) des lots libérés
        Job->>PO: Calcule stock en transit (commandes validées non reçues)
        Job->>SLT: Lit les délais (fabrication + préparation + transport + douane + interne)
        Job->>Job: Lead time total = Σ délais
        Job->>Job: Point de commande = conso_moy × lead_time + stock_sécurité
        Job->>Job: Stock cible = conso_moy × couverture_cible + stock_sécurité
        Job->>Job: Besoin net = stock_cible − stock_disponible − stock_transit
        Job->>Job: Couverture = stock_disponible / conso_moy, niveau de risque déduit
        Job->>FC: INSERT forecast_calculations (instantané daté)
        alt stock_disponible + stock_transit <= point_de_commande
            Job->>Job: Arrondit la quantité suggérée au carton supérieur
            Job->>RS: INSERT reorder_suggestions (statut = en_attente)
            Job->>Hub: Notifie ReorderSuggestionCreated (rôle Achats)
        end
    end
    Hub-->>Ach: Notification temps réel (produits critiques/urgents)

    Note over Ach,RS: Étape distincte — validation humaine (jamais automatique)
    Ach->>RS: POST /api/reorder-suggestions/{id}/validate (ajuste qté/fournisseur/transport)
    RS-->>Ach: statut = validee
    Ach->>RS: POST /api/reorder-suggestions/{id}/convert-to-purchase-order
    RS->>PO: INSERT purchase_orders + purchase_order_lines (statut = brouillon)
    RS-->>Ach: statut = convertie, purchase_order_id renseigné
```

### 5.4 Authentification et vérification RBAC en direct

```mermaid
sequenceDiagram
    autonumber
    actor U as Utilisateur
    participant FE as Frontend React
    participant Auth as AuthController (/api/auth/login)
    participant Id as ASP.NET Identity
    participant USR as users
    participant UR as user_roles / roles
    participant RP as role_permissions / permissions
    participant RT as refresh_tokens
    participant API as Contrôleur API protégé (ex. PricingController)

    U->>FE: Saisit email / mot de passe
    FE->>Auth: POST /api/auth/login
    Auth->>USR: Recherche par email (index partiel sur users non supprimés)
    alt Compte verrouillé ou inactif
        Auth-->>FE: 401 — message générique (pas de détail technique exposé)
    end
    Auth->>Id: Vérifie le hash du mot de passe
    alt Échec
        Auth->>USR: UPDATE failed_login_attempts += 1
        Auth-->>FE: 401
        Note right of USR: 5 échecs -> lockout_end_at fixé (verrouillage 15 min)
    else Succès
        Auth->>UR: Charge les rôles de l'utilisateur (multi-rôles possible)
        Auth->>RP: Charge l'union des permissions de tous les rôles
        Auth->>Auth: Construit les claims JWT (permission=Pricing.Approve, ...)
        Auth->>RT: INSERT refresh_tokens (hash, expiration)
        Auth-->>FE: Access Token (15-30 min) + Refresh Token
        FE->>FE: Stocke l'access token en mémoire, jamais en localStorage
    end

    Note over FE,API: Requête ultérieure vers une ressource sensible (ex. approbation de prix)
    FE->>API: PUT /api/pricing/{id}/approve (Bearer <access token>)
    API->>API: [Authorize(Policy="Pricing.Approve")] vérifie le claim JWT
    alt Permission absente
        API-->>FE: 403 Forbidden
        API->>API: Journalise le refus (audit_logs, is_success=false)
    else Permission présente
        API->>API: Exécute l'action, journalise (audit_logs, is_success=true)
        API-->>FE: 200 OK
    end
```

---

## 6. Intégration avec un système externe

**Constat de la Phase 1 (à assumer explicitement, pas à masquer) : aucune des intégrations listées par les sources ne constitue un système externe détenant son propre référentiel métier à synchroniser en base.** PRD_Qwen-5 §5.13 énumère les intégrations techniques prévues : ASP.NET Identity (interne), SignalR (interne), Hangfire (interne), FluentEmail/SMTP, Twilio SMS, DinkToPdf, une API de taux de change, un lecteur de codes-barres/QR, un export comptable optionnel. Aucune n'a de schéma de données propre qu'il faudrait « ponter » (pattern Bridge) avec le modèle métier — ce ne sont pas des systèmes de vérité concurrents (pas de CRM, pas d'ERP tiers, pas de passerelle de paiement gérant ses propres identifiants).

Le seul cas limite est le **taux de change**, dont l'origine peut être externe (API de la BCEAO ou équivalent) :

| Donnée | Répliquée en base ? | Interrogée à la demande ? | Fraîcheur |
|---|---|---|---|
| Taux de change EUR/XOF, USD/XOF | Répliquée (`exchange_rates`, historisée) | Non — le taux utilisé pour une transaction est toujours celui figé en base au moment de la validation, jamais relu en direct après coup (traçabilité financière, PRD_Qwen-1 §1.3) | `exchange_rates.source` documente la provenance (`manuel` / `api` / `import`) sans modéliser de pont de table dédié — un simple champ suffit car la donnée n'a pas de structure propre à synchroniser, juste une valeur et une date |
| Export comptable optionnel (PRD_Qwen-5 §5.21) | Non répliqué : export en sortie uniquement (CSV/Excel), jamais de lecture retour | Sans objet — flux à sens unique | Hors périmètre v1 (PRD_CLAUDE §5.2 point 1) |

**Décision sensible non concernée ici** — la règle générale (« une décision sensible relit toujours le système externe en direct, jamais un cache ») ne s'applique à aucune donnée de ce modèle : le taux de change n'est *jamais* relu en direct pour une transaction déjà validée, précisément parce que la traçabilité financière exige l'inverse (figer, ne jamais recalculer rétroactivement — PRD_CLAUDE règle 10.3). Ce n'est pas une exception à la règle de fraîcheur : c'est que la donnée « sensible » ici est le taux *historisé*, pas un système tiers à consulter en direct.

Si LABMEDIS adopte plus tard une intégration douanière/transitaire de niveau 3 ou 4 (API transitaire, PRD_CLAUDE §17.2/§5.22.3), ce serait alors un candidat réel au pattern Bridge : une table `customs_broker_references` porterait l'identifiant externe (numéro de dossier transitaire) en référence sur `shipments`, sans jamais fusionner le schéma du transitaire avec `shipments`. Non implémenté ici car explicitement hors périmètre v1 (PRD_CLAUDE §16.1 point « niveau 1 : saisie manuelle » retenu pour la v1).

---

## 7. Sécurité & gouvernance des données

### 7.1 Données à caractère personnel (PII) par table

| Table | Colonnes PII | Nature | Traitement |
|---|---|---|---|
| `users` | `email`, `first_name`, `last_name`, `phone` | Identité du personnel LABMEDIS | Email indexé unique (partiel), jamais exposé en clair dans les logs ; `password_hash` jamais en clair (règle absolue, cf. §7.3) |
| `audit_logs` | `user_full_name`, `ip_address`, `user_agent` | Données de connexion/traçabilité | Conservation minimale 1 an (PRD_Qwen-5 §5.12.2), accès restreint (§7.4) |
| `refresh_tokens` | `created_ip` | Adresse IP | `token_hash` jamais en clair ; purge recommandée à l'expiration (job Hangfire, hors périmètre DDL) |
| `customers` / `suppliers` | `address`, `phone`, `po_box` | Coordonnées d'entités commerciales (personnes morales, pas des particuliers au sens strict — pharmacies, cliniques, hôpitaux, fournisseurs) | Sensibilité commerciale « Haute » (PRD_Qwen-5 §5.9.1), pas un traitement RGPD de données de particuliers |
| `notifications` | aucune donnée personnelle propre au-delà de `recipient_user_id` (déjà couvert par `users`) | — | — |

Aucune colonne de mot de passe, jeton ou secret n'est stockée en clair nulle part dans le modèle (vérifié par relecture exhaustive des 636 colonnes : seules `password_hash` et `token_hash` portent une donnée d'authentification, toutes deux nommées explicitement pour lever toute ambiguïté sur leur nature de hash).

### 7.2 Données financières sensibles (classification PRD_Qwen-5 §5.9.1)

| Sensibilité | Données | Tables | Masquage prévu |
|---|---|---|---|
| Haute | Prix d'achat, prix de revient, marge | `purchase_order_lines.unit_price_foreign`, `product_prices.pr_unit_cfa`, `stock_lots.unit_cost_cfa` | Accès conditionné à la permission `Pricing.Read` côté API (§7.4) ; le frontend ne doit pas recevoir ces champs si la permission est absente (masquage double : backend ET frontend, PRD_Qwen-5 §5.9.3) |
| Haute | Prix client spécifique | `customer_product_prices.unit_price_ht_cfa` | Idem |
| Haute | Listes clients / fournisseurs | `customers`, `suppliers` | Lecture large (quasi tous les rôles), écriture restreinte |
| Haute | Factures, avoirs | `invoices`, `credit_notes` | Accès Comptable/Direction/Admin (matrice §7.4) |

### 7.3 Secrets — jamais en base, jamais en clair

Conformément à l'interdiction absolue de la mission : aucun mot de passe, token ou secret n'est stocké en clair. Ce modèle ne contient **aucune** colonne de configuration technique (chaînes de connexion, clés API SMTP/Twilio/taux de change, clé JWT) — ces éléments relèvent d'un gestionnaire de secrets ou de variables d'environnement (PRD_Qwen-5 §5.24), hors du périmètre d'un modèle de données métier. Seules deux colonnes portent une donnée d'authentification, et toutes deux sous forme de hash : `users.password_hash`, `refresh_tokens.token_hash`.

### 7.4 Couverture RBAC

Le modèle ne code en dur aucun rôle ni aucune permission : `roles` et `permissions` sont des données, pas des types. La liste de 10 rôles recommandée par PRD_Qwen-5 §5.5.2 (Admin, Direction, Achats, Logistique, Magasinier, Qualité, Commercial, Comptable, Préparateur, Lecture seule) — réconciliée avec les 6 rôles plus généraux de PRD_CLAUDE §6 — se peuple par insertion de lignes, comme vérifié dans le scénario §8 (7 rôles, 5 permissions, 1 utilisateur multi-rôles). La matrice de permissions détaillée de PRD_Qwen-5 §5.5.4 (39 lignes fonctionnelles × 8 rôles) devient des lignes `role_permissions`, pas une matrice figée dans le schéma — elle reste éditable par un administrateur sans migration.

Trois niveaux de contrôle d'accès coexistent, du plus large au plus fin :
1. **Rôle → permissions** (`role_permissions`) : la politique par défaut.
2. **Dérogation individuelle** (`user_permission_exceptions`) : accorde ou retire une permission précise à un utilisateur nommé, avec motif et fenêtre de validité, sans toucher au rôle.
3. **Journalisation de chaque vérification** (`audit_logs`, `is_success=false` sur un refus) : un refus de permission est lui-même un événement audité (PRD_Qwen-5 §5.5.6 US-RBAC-04).

### 7.5 Politique de rétention et suppression

| Donnée | Durée recommandée | Mécanisme |
|---|---|---|
| Mouvements de stock | Minimum 5 ans | Soft delete uniquement — `stock_movements.deleted_at`, jamais de suppression physique |
| Factures, avoirs | Selon obligation fiscale togolaise | Soft delete uniquement |
| Logs de sécurité (`audit_logs`) | Minimum 1 an | Soft delete ; purge physique hors périmètre DDL (job d'archivage applicatif) |
| Lots pharmaceutiques | Durée de vie produit + marge de sécurité | Jamais de suppression physique — traçabilité de rappel de lot (§5.12.4 PRD_Qwen-5) exige l'historique complet |
| Notifications anciennes | Politique applicative (purge recommandée, PRD_Qwen-5 §5.20.1) | Suppression physique acceptable ici *seule exception* : une notification lue et ancienne n'a pas la valeur probatoire d'un mouvement de stock ou d'une facture — à confirmer avec LABMEDIS |

Aucune donnée métier n'est supprimée physiquement dans ce modèle, à la seule exception documentée des notifications anciennes (recommandation, pas une implémentation figée en base).

---

## 8. Requêtes clés (réellement exécutées, Phase 3.4)

Toutes les requêtes ci-dessous ont été exécutées contre la base construite depuis `schema.sql`, avec le scénario métier de bout en bout inséré (cf. `scripts/scenario.sql`, fourni comme fichier annexe). Les deux requêtes sensibles à la performance ont en outre été testées contre un volume synthétique de **12 400 lots** (400 produits × 30 lots), car une table quasi vide aurait rendu tout `EXPLAIN` non représentatif.

### 8.1 Allocation FEFO (chemin le plus chaud du système — exécuté à chaque ligne de vente)

```sql
SELECT id, supplier_batch_number, expiry_date, remaining_quantity
FROM stock_lots
WHERE product_id = :product_id AND status = 'libere' AND deleted_at IS NULL
ORDER BY expiry_date ASC LIMIT 20;
```
**Index dédié :** `ix_stock_lots_product_fefo` (partiel, `WHERE deleted_at IS NULL AND status = 'libere'`).
**Preuve d'exécution (`EXPLAIN ANALYZE, BUFFERS`) :** `Bitmap Index Scan on ix_stock_lots_product_fefo`, 19 lignes retournées, **0,109 ms**. Confirme que l'index partiel est effectivement sélectionné par le planificateur sur un volume réaliste.
**Table à surveiller si le volume grossit :** `stock_lots` — l'index reste efficace tant que la sélectivité par `product_id` reste bonne ; si un même produit accumule des dizaines de milliers de lots actifs (cas non réaliste pour LABMEDIS), un partitionnement par date de réception serait à envisager.

### 8.2 Lots proches de péremption à 90 jours (tableau de bord Qualité/Stock)

```sql
SELECT p.designation, sl.supplier_batch_number, sl.expiry_date, sl.remaining_quantity
FROM stock_lots sl JOIN products p ON p.id = sl.product_id
WHERE sl.status = 'libere' AND sl.deleted_at IS NULL
  AND sl.expiry_date <= CURRENT_DATE + INTERVAL '90 days'
ORDER BY sl.expiry_date ASC LIMIT 50;
```
**Index dédié :** `ix_stock_lots_expiry`. **Preuve :** `Index Scan using ix_stock_lots_expiry`, **0,427 ms** sur 12 400 lots.
**Table à surveiller :** `stock_lots` (même table ; les deux index partiels/simples coexistent sans conflit, PostgreSQL choisit celui qui correspond au filtre de la requête).

### 8.3 Tableau de bord stock — valeur et répartition par catégorie

```sql
SELECT cat.name,
       count(*) FILTER (WHERE sl.status='libere') AS lots_disponibles,
       sum(sl.remaining_quantity * sl.unit_cost_cfa) FILTER (WHERE sl.status='libere') AS valeur_stock_cfa
FROM stock_lots sl JOIN products p ON p.id=sl.product_id JOIN categories cat ON cat.id=p.category_id
WHERE sl.deleted_at IS NULL GROUP BY cat.name ORDER BY valeur_stock_cfa DESC NULLS LAST;
```
Résultat vérifié sur le volume de test : 7 141 lots disponibles, valeur totale 5 031 230 178 CFA pour la catégorie Produit infantile — cohérent avec `count(*) × prix unitaire moyen` généré.

### 8.4 Détection de chevauchement de tarifs négociés client (contrôle d'intégrité applicatif)

```sql
SELECT a.id, b.id
FROM customer_product_prices a JOIN customer_product_prices b
  ON a.customer_id=b.customer_id AND a.product_id=b.product_id AND a.id<b.id
WHERE a.deleted_at IS NULL AND b.deleted_at IS NULL
  AND daterange(a.valid_from, COALESCE(a.valid_to,'infinity'),'[]')
   && daterange(b.valid_from, COALESCE(b.valid_to,'infinity'),'[]');
```
**Testé positivement** : deux tarifs volontairement chevauchants insérés (2026-01-01/2026-12-31 et 2026-06-01/2027-06-30 pour le même couple client/produit) sont correctement détectés. Cette requête sert de garde-fou applicatif avant l'activation d'un nouveau tarif — volontairement **pas** une contrainte `EXCLUDE` PostgreSQL, pour permettre un message d'erreur métier explicite côté service plutôt qu'une erreur SQL brute.

### 8.5 Produits à risque de rupture (dashboard MRP, jointure multi-domaines)

```sql
SELECT p.designation, fc.available_stock, fc.coverage_days, fc.risk_level, fc.net_requirement
FROM forecast_calculations fc JOIN products p ON p.id=fc.product_id
WHERE fc.risk_level IN ('urgent','critique')
  AND fc.calculation_date = (SELECT max(calculation_date) FROM forecast_calculations fc2 WHERE fc2.product_id=fc.product_id)
ORDER BY fc.coverage_days ASC;
```
Résultat vérifié : France Lait 1er âge 400g remonté en risque `urgent` (couverture 90 j < lead time 110 j), avec `net_requirement = 900` — valeurs identiques à celles calculées manuellement dans le scénario (§1.3 point 2 du présent document).
**Table à surveiller :** `forecast_calculations` croît d'une ligne par produit suivi et par jour (append-only) — recommandation V2 : partitionner par mois ou purger au-delà de 12 mois glissants (§10).

---

## 9. Décisions d'arbitrage

### 9.1 Contradictions réelles entre documents sources

**[CONTRADICTION 1 — cardinalité commande d'achat ↔ expédition]**
`PRD_CLAUDE.md` §9.1 (ERD), §9.2 point 5, §7.2 point 3 et §8.3.3 modélise `Expedition` avec une commande d'achat unique (`COMMANDE_ACHAT ||--o{ EXPEDITION` : une expédition appartient à une seule commande, une commande peut produire plusieurs expéditions). `PRD_Qwen` module 3 §3.5.3 (entité `Shipment`), §3.5.4 (« une expédition peut couvrir plusieurs commandes | utile si plusieurs commandes sont regroupées dans un conteneur ») et §3.5.5 (US-LOG-01, critère 1 : « l'expédition peut être liée à une ou plusieurs commandes fournisseurs ») exigent explicitement une relation N:N.
**Décision retenue :** modèle N:N résolu au niveau ligne — `shipment_lines` référence `purchase_order_lines` (jamais l'entête de commande). Une expédition n'a donc pas de `purchase_order_id` direct. Ce choix satisfait les deux exigences (commande livrée en plusieurs expéditions **et** expédition consolidant plusieurs commandes) sans jamais bloquer la livraison. **À confirmer avec LABMEDIS.**

**[CONTRADICTION 2 — mécanisme de calcul du prix de revient]**
`PRD_Qwen-1` §1.1 (et `Structure_de_prix.xlsx`, vérifié empiriquement terme à terme en Phase 1) calcule le prix de revient via une cascade **multiplicative** de coefficients : `PR = PA_CFA × commission × freight × transit × frais_transfert`. `PRD_Qwen` module 3 §3.5.3/§3.5.4 et §US-LOG-02 décrit au contraire les frais logistiques comme des **montants additifs** (freight, transit, douane, commission, assurance, manutention) alloués au prorata (valeur/quantité/volume) sur les lignes d'expédition — un second mécanisme de calcul structurellement différent.
**Décision retenue :** le modèle multiplicatif (`pricing_profiles`) reste la méthode **autoritaire** pour `stock_lots.unit_cost_cfa`, car c'est la seule vérifiée sur données réelles et explicitement documentée comme reproductible à tout le catalogue (PRD_CLAUDE §19.1). La table `import_costs` (montants additifs) est conservée comme registre comptable des frais réellement facturés par expédition — utile au rapprochement comptable et à l'analyse de rentabilité par conteneur (PRD_Qwen-6 §6.22.1) — mais n'alimente pas le calcul du PRU du lot. **À confirmer avec LABMEDIS**, notamment si les deux mécanismes doivent un jour converger (écart entre coût théorique et coût réellement facturé à analyser).

### 9.2 Ambiguïtés tranchées (pas des contradictions — sources simplement silencieuses ou imprécises)

| Point | Sources en tension | Décision |
|---|---|---|
| Multi-entrepôt | PRD_Qwen-2 §2.19 le liste en question ouverte ; PRD_Qwen.md mentionne des « transferts inter-dépôts » ; PRD_CLAUDE §9.2 définit `Entrepot` séparément de `EmplacementStockage` | `warehouses` modélisé en entité de premier niveau — coût de modélisation faible, ne bloque pas le mono-entrepôt actuel, n'interdit pas un futur multi-dépôt |
| Statut de lot « en attente de libération » | Absent de l'énumération `LotStatus` de PRD_Qwen-2 §2.13.2 | Ajouté suite à la recherche réglementaire BPD/WHO-GDP de PRD_CLAUDE §17.5 point 6 — enrichissement, pas un désaccord |
| TVA par défaut vs surcharge produit | PRD_Qwen-1 parle de « taux par défaut » ; PRD_Qwen.md §3.3.5/§8.6.6 et PRD_CLAUDE §17.4 exigent tous deux un override produit | Une fois les trois sources lues en entier, elles s'accordent : `categories.default_vat_rate` + `products.vat_rate_override` nullable |
| Placement du tarif client négocié | Pourrait relever de Pricing (mode de transport/fournisseur) ou de Ventes (client) | Rattaché au domaine Ventes — donnée structurellement client-centrique (US-VEN-05) |
| Réconciliation RBAC 6 rôles (PRD_CLAUDE §6) vs 10 rôles (PRD_Qwen-5 §5.5.2) | Les 6 rôles de PRD_CLAUDE sont un sous-ensemble plus général | Modèle data-driven (`roles` = table, pas un type figé) : accueille indifféremment 6, 10, ou toute autre granularité sans migration |
| Numérotation des désignations dupliquées dans le catalogue source | 9 doublons exacts constatés (Phase 1) alors que la désignation doit être unique (US-REF-01) | Contrainte d'unicité imposée au niveau du modèle cible ; la déduplication du référentiel importé est une étape d'import, pas un assouplissement de la règle |

### 9.3 Autres décisions structurantes

1. **Nommage des tables/colonnes en anglais** (`purchase_orders`, `stock_lots`...) malgré une documentation rédigée en français. Les entités C# de référence dans les PRD Qwen sont déjà nommées en anglais (`PurchaseOrder`, `StockLot`...) ; conserver ce vocabulaire évite une couche de traduction supplémentaire entre le modèle de données et le code EF Core, tout en respectant la convention `snake_case` imposée par la mission.
2. **Séparation Livraison / Facturation** (au lieu d'une fusion `CommandeVente → Facture` comme dans le brouillon PRD_CLAUDE §9) : justifiée par PRD_CLAUDE §8.8.3/§18.2 lui-même et par PRD_Qwen module 3 qui consacre deux workflows distincts (9 et 10).
3. **Pas de contrainte `EXCLUDE` PostgreSQL** pour les chevauchements de tarifs négociés ou de réservations : préféré un contrôle en couche service (requête documentée en §8.4) pour conserver un message d'erreur métier explicite plutôt qu'une erreur de contrainte SQL brute peu lisible pour l'utilisateur final.
4. **Écart d'inventaire jamais en colonne générée** : `inventory_counts.system_quantity` et `counted_quantity` restent deux colonnes indépendantes ; le calcul de l'écart et sa validation vivent en couche service (règle 2.5 de la mission — pas de contrainte SQL figée sur une comparaison mouvante).
5. **Politique `ON DELETE` générale** (appliquée aux 112 FK, avec commentaire ponctuel uniquement en cas de dérogation) : `RESTRICT` vers le référentiel commercial et vers tout enregistrement transactionnel nécessitant une traçabilité obligatoire (produit, fournisseur, client, lot, utilisateur auteur d'un mouvement) ; `CASCADE` pour les lignes de détail vers leur entête (une ligne n'a pas de sens sans son document parent) et pour les associations pures (`role_permissions`, `product_suppliers`...) ; `SET NULL` pour les attributions optionnelles où la ligne reste pleinement valide sans elles (auteur d'une simulation de prix, valideur d'une commande...).

---

## 10. Recommandations pour une V2 / hors périmètre actuel

1. **Saisonnalité MRP à courbe mensuelle.** `forecast_parameters.seasonality_factor` est un coefficient unique par produit (choix v1, cf. §9). Des produits comme GRIPEX (saison grippale) ou Pommade Maïa (anti-moustiques, saisonnier) bénéficieraient d'une table `product_seasonality_periods` (mois, coefficient) en V2 — PRD_Qwen-4 §4.11 le suggère en prose sans l'avoir chiffré dans son entité de référence.
2. **Comptabilité générale.** Explicitement hors périmètre v1 (PRD_CLAUDE §5.2 point 1). `monthly_financial_summary` prépare le terrain pour un export vers un logiciel comptable, mais aucune écriture comptable (grand livre, lettrage) n'est modélisée ici.
3. **Portail self-service répartiteurs.** Hors périmètre v1 (PRD_CLAUDE §5.2 point 2) — `customers.customer_type='repartiteur'` suffit à la v1 ; un accès applicatif dédié nécessiterait un modèle d'authentification externe distinct (hors périmètre de ce modèle de données interne).
4. **Intégration douanière/transitaire de niveau 3-4** (API transitaire en direct). Cf. §6 — actuellement niveau 1 (saisie manuelle + upload de documents), conforme au choix v1 de PRD_CLAUDE §16.1.
5. **Partitionnement de `forecast_calculations` et `audit_logs`.** Ces deux tables croissent en continu (append-only quotidien). Non nécessaire au lancement (volumétrie « quelques centaines de produits », PRD_CLAUDE §11.4) mais à surveiller après 12-18 mois d'exploitation (cf. §8.5).
6. **API de taux de change automatisée.** V1 retient une saisie manuelle pour USD/XOF (PRD_CLAUDE §5.2 point 4) ; `exchange_rates.source='api'` est déjà prévu dans l'énumération pour ne pas bloquer une bascule ultérieure sans migration de schéma.
7. **Traçabilité unité par unité** (numéro de série individuel, pas seulement par lot) : évoquée comme hypothèse ouverte par PRD_CLAUDE §13.1 si LABMEDIS en exprime le besoin — architecture actuelle (traçabilité par lot) à revoir en profondeur si confirmé, impact majeur sur `stock_movement_lines`.

## 11. Fichiers livrés

| Fichier | Contenu |
|---|---|
| `schema.sql` | Script SQL complet, exécutable tel quel sur PostgreSQL ≥ 13 (testé sur 16.15) — 55 tables, 8 domaines dans l'ordre de dépendance |
| `labmedis-modele-donnees.md` | Le présent document |
| `scripts/scenario.sql` *(annexe, référencée en §1.3 et §8)* | Scénario métier de bout en bout inséré et vérifié en Phase 3.3 |
