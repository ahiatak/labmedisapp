# LABMEDIS — Modèle de données complet (DDD, validé, documenté)


## PHASE 1 — Découverte complète (résultat de lecture)

Les **5 fichiers** fournis ont été lus intégralement (aucun n'a été tronqué dans ce qui m'a été transmis) :

| Fichier | Contenu extrait |
|---|---|
| `PRD_brut.md` | Vision métier brute : dépositaire pharma, commerce international multi-devises (EUR/USD/XOF), fabricant → dépositaire → répartiteur → pharmacie/client ; lots étiquetés, transport avion/bateau/express, prix achat/revient/vente, anticipation des ruptures (délais 3-4 mois), conditionnement carton ↔ unité. |
| `PRD_CLAUDE.md` | PRD structuré : modèle de données (§9), règles de gestion critiques (§10), architecture technique & règles d'or (§12), cadre réglementaire Togo/UEMOA (§17), hypothèses ouvertes (§13). |
| `PRD_Qwen-2` | Spécification Stock/Entrepôt : entités (`StockLot`, `StockLotLocation`, `StorageLocation`, `StockMovement`, `InventorySession`…), FEFO, quarantaine, péremption, mouvements, inventaires. |
| `PRD_Qwen-1` | Moteur de pricing : formule PR en cascade de coefficients, `PricingProfile`, multi-devises, `ExchangeRate`, mapping TVA, arrondi CFA. |
| `PRD_Qwen-3` | Workflows & user stories : acteurs/rôles, statuts de commandes, réceptions, retours/avoirs, prévisions, RBAC implicite. |

**Acteurs/rôles identifiés** (Qwen-3 §3.1, PRD_CLAUDE §6) : Administrateur, Direction/Manager, Acheteur/Import, Responsable logistique, Magasinier, Responsable qualité, Commercial/Vente, Comptable, Préparateur. Aucun document n'impose d'exclusivité stricte entre rôles → **cumul autorisé** (modélisé par table de jointure N-N).

**Contraintes réglementaires fortes** (PRD_CLAUDE §17) : traçabilité par lot obligatoire, FEFO, quarantaine/libération, chaîne du froid possible, TVA configurable par produit (jamais déduite de la catégorie), licence dépositaire à tracer.

---

## Décisions prises sur le CONTEXTE vide (avant de modéliser)

| Item | Décision retenue | Justification / renvoi |
|---|---|---|
| Nom du projet | **LABMEDIS** | Unanime dans les 4 specs. |
| Moteur cible | **PostgreSQL** | Voir `[CONTRADICTION C-1]` ci-dessous. |
| Systèmes externes | **Aucun en v1** | Comptabilité = hors périmètre (PRD_CLAUDE §5.2) ; API taux de change = hors périmètre v1 (§5.2). Donc **pas de table Bridge** ; section intégration = "néant en v1, pattern prévu en V2". |
| Mode d'exécution | **Autonome complet** | Valeur par défaut du prompt. |

`[CONTRADICTION C-1 : PRD_CLAUDE §12.6 impose "Microsoft.EntityFrameworkCore.SqlServer" ; la mission demande PostgreSQL par défaut. Décision retenue : PostgreSQL, car (a) c'est le défaut explicite de la mission, (b) les conventions imposées — snake_case, triggers, UUID, VARCHAR+CHECK — sont natives et robustes en PostgreSQL, (c) EF Core cible PostgreSQL via Npgsql sans casser la stack .NET 9. Conséquence à confirmer : si la prod reste sur SQL Server, prévoir une traduction du script (types uuid→uniqueidentifier, triggers→AFTER UPDATE triggers T-SQL).]`

`[CONTRADICTION C-2 : le champ "Forme" est utilisé de deux façons incompatibles dans les fichiers Excel sources (PRD_CLAUDE §2.3.2) — tantôt contenance/dosage, tantôt forme galénique. Décision retenue : deux colonnes distinctes et non ambiguës : form (forme galénique : comprimé, sirop, crème…) et dosage (contenance : 400g, 100ml…). Conforme à la recommandation PRD_CLAUDE §13.2.]`

`[CONTRADICTION C-3 : PRD_CLAUDE §2.2.3 dit le taux EUR/XOF "fixe 655,957" ; PRD_Qwen-1 §1.3 dit "le taux de change n'est pas fixe, il doit être saisi à la commande". Réconciliation (pas un blocage) : la fixité ne concerne QUE la paire EUR/XOF (parité) ; la règle d'or transversale reste de FIGER un instantané de taux sur chaque transaction, y compris EUR. Le modèle stocke donc un snapshot de taux sur chaque ligne d'achat, en plus d'une table de taux versionnée.]`

---

## PHASE 2 — Stress-test de conception (scénario de bout en bout)

**Scénario testé conceptuellement** : *LABMEDIS commande du France Lait 1er âge à Continental Commodities (EUR), reçoit le lot par bateau, le met en quarantaine puis le libère, le range, calcule son PR/PV, le vend à LABOREX TOGO avec réservation FEFO, livre, facture, puis gère un retour partiel avec avoir.*

Déroulé des domaines sollicités, dans l'ordre :
1. **Référentiels** (catégorie, forme, produit, conditionnement) → 2. **Partenaires** (fournisseur, client) → 3. **Devises/Pricing** (taux EUR figé, profil coefficients maritimes) → 4. **Achats** (PO + lignes, taux snapshoté) → 5. **Logistique** (shipment maritime, frais, événements) → 6. **Stock** (réception → lot en quarantaine → libération → mise en stock emplacement → mouvement) → 7. **Tarification produit** (PR/PMP recalculé, PV, historique) → 8. **Ventes** (commande, réservation FEFO, préparation, livraison/BL, facture) → 9. **Retour** (return + avoir + réintégration stock) → 10. **Prévision/Notifications** (alerte péremption, suggestion réappro).

**Verdict du stress-test** : le découpage tient. Deux corrections appliquées *avant* de détailler : (a) la **réservation de stock** doit être une table à part entière (pas juste un flag sur la ligne de vente) pour permettre l'allocation FEFO multi-lots ; (b) l'**avoir** est modélisé comme une facture de type `AVOIR` (pas une table séparée) pour éviter une référence circulaire facture↔avoir. Aucune référence circulaire irréductible ne subsiste (voir §2.4 de la mission).

---

## PHASE 3 — Méthode de validation appliquée (transparence)

Pas d'environnement exécutable disponible (voir bandeau). J'applique donc la **Phase 3.5** : **relecture manuelle** du script, domaine par domaine, en vérifiant à la main : ordre de création (aucune FK ne pointe vers une table non encore créée), cohérence de type entre chaque PK `uuid` et sa FK, unicité des noms de contraintes, validité des `CHECK`. **Cette relecture n'est PAS une exécution** — elle est présentée comme telle partout dans le document.

---

## LIVRABLE 1 — `schema.sql`

```sql
-- ============================================================================
-- LABMEDIS — Dépositaire pharmaceutique — Modèle de données v1
-- Moteur cible : PostgreSQL 14+
-- Généré : 2026-08-28
-- NOTE : validation par RELECTURE MANUELLE uniquement (pas d'exécution réelle
--        disponible dans la session de génération). Voir doc §2.
-- Conventions : snake_case, tables pluriel, PK uuid, FK <singulier>_id,
--               soft delete via deleted_at, statuts en VARCHAR+CHECK.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Trigger générique de maintien de updated_at
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- DOMAINE 1 — RÉFÉRENTIELS CATALOGUE
-- ============================================================================

CREATE TABLE categories (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code             varchar(30)  NOT NULL UNIQUE,
    name             varchar(120) NOT NULL,
    default_vat_rate numeric(5,2) NOT NULL DEFAULT 0
        CHECK (default_vat_rate >= 0 AND default_vat_rate <= 100),
    expiry_alert_days int         NOT NULL DEFAULT 90,
    created_at       timestamptz  NOT NULL DEFAULT now(),
    updated_at       timestamptz  NOT NULL DEFAULT now(),
    deleted_at       timestamptz  NULL
);
COMMENT ON TABLE categories IS 'Catégories produit (infantile, médicament, cosmétique…). La TVA par défaut est INDICATIVE : toujours surchargeable au niveau produit (règle PRD §17.4).';

CREATE TABLE product_forms (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name       varchar(80) NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);
COMMENT ON TABLE product_forms IS 'Formes galéniques contrôlées (comprimé, sirop, crème…). Décision C-2 : distinct de dosage.';

CREATE TABLE therapeutic_classes (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name       varchar(120) NOT NULL UNIQUE,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now(),
    deleted_at timestamptz  NULL
);

CREATE TABLE products (
    id                        uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    designation               varchar(200) NOT NULL,
    category_id               uuid NOT NULL REFERENCES categories(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    form_id                   uuid NULL REFERENCES product_forms(id) ON DELETE SET NULL ON UPDATE CASCADE,
    therapeutic_class_id      uuid NULL REFERENCES therapeutic_classes(id) ON DELETE SET NULL ON UPDATE CASCADE,
    dosage                    varchar(60)  NULL,
    cip_code                  varchar(40)  NULL UNIQUE,
    active_principle          varchar(200) NULL,
    default_supplier_id       uuid NULL, -- FK posée après création de suppliers (voir note domaine 2)
    vat_rate                  numeric(5,2) NULL
        CHECK (vat_rate IS NULL OR (vat_rate >= 0 AND vat_rate <= 100)),
    is_taxable                boolean NOT NULL DEFAULT TRUE,
    manufacturing_lead_days   int NULL CHECK (manufacturing_lead_days IS NULL OR manufacturing_lead_days >= 0),
    delivery_lead_days        int NULL CHECK (delivery_lead_days IS NULL OR delivery_lead_days >= 0),
    safety_stock_qty          int NOT NULL DEFAULT 0 CHECK (safety_stock_qty >= 0),
    min_stock_threshold       int NOT NULL DEFAULT 0 CHECK (min_stock_threshold >= 0),
    default_transport_mode    varchar(15) NULL
        CHECK (default_transport_mode IS NULL OR default_transport_mode IN ('MARITIME','AERIEN','EXPRESS','TERRESTRE')),
    is_active                 boolean NOT NULL DEFAULT TRUE,
    created_at                timestamptz NOT NULL DEFAULT now(),
    updated_at                timestamptz NOT NULL DEFAULT now(),
    deleted_at                timestamptz NULL
);
COMMENT ON COLUMN products.vat_rate IS 'NULL = hériter de categories.default_vat_rate. Jamais déduit automatiquement de la catégorie au runtime (règle PRD §17.4).';
COMMENT ON COLUMN products.default_supplier_id IS 'FK logique vers suppliers ; posée en ALTER après création de suppliers pour éviter la référence circulaire.';

CREATE TABLE product_packagings (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id       uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    level            varchar(15) NOT NULL CHECK (level IN ('UNITE','CARTON','PALETTE','COLIS_EXPRESS')),
    units_per_package int NOT NULL CHECK (units_per_package > 0),
    is_default       boolean NOT NULL DEFAULT FALSE,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    deleted_at       timestamptz NULL,
    UNIQUE (product_id, level)
);
COMMENT ON TABLE product_packagings IS 'Plusieurs conditionnements par produit (PRD §8.1.1, Qwen-2 §2.3.4). units_per_package est le standard ; le réel observé à réception vit sur stock_lots.';

-- ============================================================================
-- DOMAINE 2 — PARTENAIRES (fournisseurs & clients)
-- ============================================================================

CREATE TABLE suppliers (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name                  varchar(200) NOT NULL UNIQUE,
    country               varchar(80)  NOT NULL,
    address               varchar(255) NULL,
    po_box                varchar(40)  NULL,
    phone                 varchar(40)  NULL,
    email                 varchar(150) NULL,
    default_currency_code char(3) NOT NULL DEFAULT 'EUR',
    wholesale_license_no  varchar(80) NULL,
    avg_manufacturing_lead_days int NULL,
    avg_delivery_lead_days      int NULL,
    is_active             boolean NOT NULL DEFAULT TRUE,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz NULL
);
COMMENT ON COLUMN suppliers.name IS 'Fiche fournisseur UNIQUE imposée : met fin aux doublons de saisie libre (PRD §2.3.1).';
COMMENT ON COLUMN suppliers.wholesale_license_no IS 'Vérif BPD : autorisation de distribution en gros du fournisseur (PRD §17.5).';

-- Pose de la FK différée products.default_supplier_id
ALTER TABLE products
    ADD CONSTRAINT fk_products_supplier
    FOREIGN KEY (default_supplier_id) REFERENCES suppliers(id)
    ON DELETE SET NULL ON UPDATE CASCADE;

CREATE TABLE clients (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name                  varchar(200) NOT NULL UNIQUE,
    client_type           varchar(25) NOT NULL
        CHECK (client_type IN ('PHARMACIE','CLINIQUE','HOPITAL','CENTRALE_ACHAT','REPARTITEUR','AUTRE')),
    city                  varchar(100) NOT NULL,
    address               varchar(255) NULL,
    po_box                varchar(40)  NULL,
    phone                 varchar(40)  NULL,
    email                 varchar(150) NULL,
    payment_term_days     int NOT NULL DEFAULT 0 CHECK (payment_term_days >= 0),
    credit_limit          numeric(18,2) NULL CHECK (credit_limit IS NULL OR credit_limit >= 0),
    pharmacy_license_no   varchar(80) NULL,
    is_active             boolean NOT NULL DEFAULT TRUE,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz NULL
);
COMMENT ON COLUMN clients.client_type IS 'Le RÉPARTITEUR est un type de client, pas une entité distincte (PRD §9.3, Qwen-3).';
COMMENT ON COLUMN clients.credit_limit IS 'Plafond d''encours autorisé (PRD §20.1).';

-- ============================================================================
-- DOMAINE 3 — IAM & AUDIT
-- ============================================================================

CREATE TABLE users (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_name      varchar(100) NOT NULL UNIQUE,
    email          varchar(200) NOT NULL UNIQUE,
    first_name     varchar(100) NOT NULL,
    last_name      varchar(100) NOT NULL,
    password_hash  varchar(500) NOT NULL,
    security_stamp varchar(200) NULL,
    phone          varchar(40) NULL,
    is_active      boolean NOT NULL DEFAULT TRUE,
    last_login_at  timestamptz NULL,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    deleted_at     timestamptz NULL
);
COMMENT ON COLUMN users.password_hash IS 'PII + SECRET : hash uniquement (ASP.NET Identity), JAMAIS de clair.';
COMMENT ON COLUMN users.email IS 'PII.';

CREATE TABLE roles (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code        varchar(50) NOT NULL UNIQUE,
    name        varchar(120) NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    deleted_at  timestamptz NULL
);

CREATE TABLE permissions (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    module      varchar(60) NOT NULL,
    action      varchar(60) NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (module, action)
);

CREATE TABLE user_roles (
    user_id  uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,
    role_id  uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE ON UPDATE CASCADE,
    granted_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, role_id)
);
COMMENT ON TABLE user_roles IS 'Cumul de rôles autorisé (aucune exclusivité imposée par les sources).';

CREATE TABLE role_permissions (
    role_id       uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE ON UPDATE CASCADE,
    permission_id uuid NOT NULL REFERENCES permissions(id) ON DELETE CASCADE ON UPDATE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE audit_logs (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       uuid NULL REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    action        varchar(120) NOT NULL,
    entity_type   varchar(80)  NOT NULL,
    entity_id     uuid         NULL,
    ip_address    varchar(45)  NULL,
    user_agent    varchar(400) NULL,
    details       jsonb        NULL,
    created_at    timestamptz  NOT NULL DEFAULT now()
);
COMMENT ON COLUMN audit_logs.entity_id IS 'Référence polymorphe volontairement SANS FK stricte (voir décision d''arbitrage §9).';
COMMENT ON TABLE audit_logs IS 'Journalisation imposée par PRD §12.5 (user, IP, UserAgent). Forte croissance : index created_at à créer CONCURRENTLY en prod.';
CREATE INDEX idx_audit_logs_user    ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_created ON audit_logs(created_at);

-- ============================================================================
-- DOMAINE 4 — DEVISES & PRICING
-- ============================================================================

CREATE TABLE currencies (
    code       char(3) PRIMARY KEY,
    name       varchar(60) NOT NULL,
    symbol     varchar(10) NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);

CREATE TABLE exchange_rates (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    currency_from   char(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT ON UPDATE CASCADE,
    currency_to     char(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT ON UPDATE CASCADE,
    rate            numeric(18,6) NOT NULL CHECK (rate > 0),
    effective_date  date NOT NULL,
    rate_type       varchar(10) NOT NULL DEFAULT 'VARIABLE' CHECK (rate_type IN ('FIXE','VARIABLE')),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    deleted_at      timestamptz NULL,
    UNIQUE (currency_from, currency_to, effective_date)
);
COMMENT ON TABLE exchange_rates IS 'Taux versionné par date. EUR/XOF en rate_type=FIXE (655.957), USD/XOF VARIABLE (PRD §5.2, §13.3).';

CREATE TABLE pricing_profiles (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name                varchar(150) NOT NULL,
    supplier_id         uuid NULL REFERENCES suppliers(id) ON DELETE SET NULL ON UPDATE CASCADE,
    category_id         uuid NULL REFERENCES categories(id) ON DELETE SET NULL ON UPDATE CASCADE,
    transport_mode      varchar(15) NOT NULL CHECK (transport_mode IN ('MARITIME','AERIEN','EXPRESS','TERRESTRE')),
    commission_coeff    numeric(10,4) NOT NULL DEFAULT 1 CHECK (commission_coeff > 0),
    freight_coeff       numeric(10,4) NOT NULL DEFAULT 1 CHECK (freight_coeff > 0),
    transit_coeff       numeric(10,4) NOT NULL DEFAULT 1 CHECK (transit_coeff > 0),
    transfer_fee_coeff  numeric(10,4) NOT NULL DEFAULT 1 CHECK (transfer_fee_coeff > 0),
    target_margin_coeff numeric(10,4) NOT NULL DEFAULT 1 CHECK (target_margin_coeff > 0),
    is_active           boolean NOT NULL DEFAULT TRUE,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    deleted_at          timestamptz NULL
);
COMMENT ON TABLE pricing_profiles IS 'Coefficients JAMAIS codés en dur (PRD_Qwen-1 §1.2). Varient par mode de transport (maritime vs aérien) et/ou catégorie/fournisseur.';

CREATE TABLE product_price_history (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id        uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    cost_price_cfa    numeric(18,2) NOT NULL CHECK (cost_price_cfa >= 0),
    theoretical_price_ht numeric(18,2) NOT NULL CHECK (theoretical_price_ht >= 0),
    applied_price_ht  numeric(18,2) NOT NULL CHECK (applied_price_ht >= 0),
    price_gap         numeric(18,2) NOT NULL DEFAULT 0,
    vat_rate          numeric(5,2) NOT NULL DEFAULT 0,
    effective_date    date NOT NULL,
    approved_by       uuid NULL REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    created_at        timestamptz NOT NULL DEFAULT now(),
    updated_at        timestamptz NOT NULL DEFAULT now(),
    deleted_at        timestamptz NULL
);
COMMENT ON COLUMN product_price_history.price_gap IS 'Écart PV calculé vs "Prix LABMEDIS" appliqué : conservé, jamais écrasé (PRD §10.8).';
CREATE INDEX idx_price_history_product ON product_price_history(product_id, effective_date DESC);

-- ============================================================================
-- DOMAINE 5 — ACHATS & LOGISTIQUE
-- ============================================================================

CREATE TABLE purchase_orders (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference              varchar(40) NOT NULL UNIQUE,
    supplier_id            uuid NOT NULL REFERENCES suppliers(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    currency_code          char(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT ON UPDATE CASCADE,
    order_date             date NOT NULL,
    expected_delivery_date date NULL,
    status                 varchar(25) NOT NULL DEFAULT 'BROUILLON'
        CHECK (status IN ('BROUILLON','EN_ATTENTE_VALIDATION','VALIDEE','ENVOYEE','EN_FABRICATION',
                          'PRETE_EXPEDIER','EXPEDIEE','EN_TRANSIT','PARTIELLEMENT_RECUE','RECUE','CLOSE','ANNULEE')),
    incoterm               varchar(10) NULL,
    total_amount           numeric(18,4) NOT NULL DEFAULT 0,
    validated_by           uuid NULL REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    validated_at           timestamptz NULL,
    cancellation_reason    varchar(255) NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    deleted_at             timestamptz NULL
);
COMMENT ON COLUMN purchase_orders.status IS 'Cycle complet PRD_Qwen-3 §3.4.3.';

CREATE TABLE purchase_order_lines (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id     uuid NOT NULL REFERENCES purchase_orders(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id            uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    ordered_qty_units     int NOT NULL CHECK (ordered_qty_units > 0),
    ordered_qty_cartons   int NULL CHECK (ordered_qty_cartons IS NULL OR ordered_qty_cartons > 0),
    unit_price_foreign    numeric(18,4) NOT NULL CHECK (unit_price_foreign >= 0),
    locked_exchange_rate  numeric(18,6) NOT NULL CHECK (locked_exchange_rate > 0),
    unit_price_cfa        numeric(18,2) NOT NULL CHECK (unit_price_cfa >= 0),
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz NULL
);
COMMENT ON COLUMN purchase_order_lines.locked_exchange_rate IS 'INSTANTANÉ du taux figé à la commande (règle d''or, décision C-3) — indépendamment de la table exchange_rates.';

CREATE TABLE shipments (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference             varchar(40) NOT NULL UNIQUE,
    purchase_order_id     uuid NOT NULL REFERENCES purchase_orders(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    transport_mode        varchar(15) NOT NULL CHECK (transport_mode IN ('MARITIME','AERIEN','EXPRESS','TERRESTRE')),
    carrier               varchar(150) NULL,
    transport_ref         varchar(100) NULL,
    customs_regime        varchar(60) NULL,
    import_auth_no        varchar(80) NULL,
    departure_date_est    date NULL,
    departure_date_real   date NULL,
    arrival_date_est      date NULL,
    arrival_date_real     date NULL,
    status                varchar(20) NOT NULL DEFAULT 'PREPAREE'
        CHECK (status IN ('PREPAREE','EN_TRANSIT','ARRIVEE','DEDOUANEE','RECUE','ANNULEE')),
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz NULL
);
COMMENT ON COLUMN shipments.import_auth_no IS 'Autorisation d''importation DPML (PRD §17.2).';
COMMENT ON COLUMN shipments.customs_regime IS 'Référentiel OTR (PRD §17.3).';

CREATE TABLE logistics_costs (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id   uuid NOT NULL REFERENCES shipments(id) ON DELETE CASCADE ON UPDATE CASCADE,
    cost_type     varchar(30) NOT NULL
        CHECK (cost_type IN ('FREIGHT','TRANSIT','DOUANE','COMMISSION','ASSURANCE','MANUTENTION','TRANSFERT','AUTRE')),
    amount_foreign numeric(18,4) NOT NULL CHECK (amount_foreign >= 0),
    currency_code char(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT ON UPDATE CASCADE,
    applied_rate  numeric(18,6) NOT NULL CHECK (applied_rate > 0),
    amount_cfa    numeric(18,2) NOT NULL CHECK (amount_cfa >= 0),
    allocation_method varchar(15) NOT NULL DEFAULT 'VALEUR' CHECK (allocation_method IN ('VALEUR','QUANTITE','VOLUME')),
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    deleted_at    timestamptz NULL
);
COMMENT ON TABLE logistics_costs IS 'Frais alloués au prorata pour le landing cost (PRD_Qwen-3 §3.5, US-LOG-02).';

CREATE TABLE shipment_events (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id  uuid NOT NULL REFERENCES shipments(id) ON DELETE CASCADE ON UPDATE CASCADE,
    event_date   timestamptz NOT NULL,
    status       varchar(60) NOT NULL,
    description  varchar(255) NULL,
    user_id      uuid NULL REFERENCES users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_shipment_events_shipment ON shipment_events(shipment_id, event_date DESC);

-- ============================================================================
-- DOMAINE 6 — STOCK & ENTREPOSAGE
-- ============================================================================

CREATE TABLE warehouses (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name       varchar(120) NOT NULL UNIQUE,
    address    varchar(255) NULL,
    is_active  boolean NOT NULL DEFAULT TRUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);

CREATE TABLE storage_locations (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    warehouse_id     uuid NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    code             varchar(60) NOT NULL UNIQUE,
    name             varchar(120) NULL,
    location_type    varchar(20) NOT NULL DEFAULT 'STOCKAGE'
        CHECK (location_type IN ('RECEPTION','QUARANTAINE','STOCKAGE','PICKING','RESERVE',
                                 'CHAINE_FROID','PERIMES','DESTRUCTION','TRANSIT')),
    parent_location_id uuid NULL REFERENCES storage_locations(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    capacity         int NULL CHECK (capacity IS NULL OR capacity > 0),
    is_active        boolean NOT NULL DEFAULT TRUE,
    is_locked        boolean NOT NULL DEFAULT FALSE,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    deleted_at       timestamptz NULL
);
COMMENT ON TABLE storage_locations IS 'Hiérarchie ZONE→ALLEE→RACK→NIVEAU via parent_location_id (auto-référence).';

CREATE TABLE stock_lots (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id             uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    supplier_id            uuid NOT NULL REFERENCES suppliers(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    shipment_id            uuid NULL REFERENCES shipments(id) ON DELETE SET NULL ON UPDATE CASCADE,
    purchase_order_id      uuid NULL REFERENCES purchase_orders(id) ON DELETE SET NULL ON UPDATE CASCADE,
    supplier_batch_number  varchar(80) NOT NULL,
    internal_batch_number  varchar(80) NULL,
    expiry_date            date NOT NULL,
    reception_date         date NOT NULL DEFAULT CURRENT_DATE,
    status                 varchar(20) NOT NULL DEFAULT 'EN_RECEPTION'
        CHECK (status IN ('EN_RECEPTION','QUARANTAINE','LIBERE','NON_CONFORME','PERIME','DETRUIT')),
    transport_mode         varchar(15) NULL CHECK (transport_mode IN ('MARITIME','AERIEN','EXPRESS','TERRESTRE')),
    received_qty_units     int NOT NULL CHECK (received_qty_units > 0),
    received_qty_cartons   int NULL CHECK (received_qty_cartons IS NULL OR received_qty_cartons >= 0),
    observed_units_per_carton int NULL CHECK (observed_units_per_carton IS NULL OR observed_units_per_carton > 0),
    unit_cost_cfa          numeric(18,2) NOT NULL DEFAULT 0 CHECK (unit_cost_cfa >= 0),
    pricing_profile_id     uuid NULL REFERENCES pricing_profiles(id) ON DELETE SET NULL ON UPDATE CASCADE,
    locked_exchange_rate   numeric(18,6) NULL CHECK (locked_exchange_rate IS NULL OR locked_exchange_rate > 0),
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    deleted_at             timestamptz NULL,
    UNIQUE (supplier_id, product_id, supplier_batch_number)
);
COMMENT ON CONSTRAINT stock_lots_supplier_id_product_id_supplier_bat_key ON stock_lots
    IS 'Unicité du lot par (fournisseur, produit) — règle PRD §10.1.';
COMMENT ON COLUMN stock_lots.observed_units_per_carton IS 'Le réel observé à réception, qui PEUT varier d''un lot à l''autre (PRD §10.2). Jamais la base de calcul de la quantité totale.';
COMMENT ON COLUMN stock_lots.unit_cost_cfa IS 'Prix de revient du lot = landing cost (PA converti × coefficients du profil transport).';
CREATE INDEX idx_stock_lots_product ON stock_lots(product_id);
CREATE INDEX idx_stock_lots_expiry  ON stock_lots(expiry_date);
CREATE INDEX idx_stock_lots_status  ON stock_lots(status);

CREATE TABLE stock_lot_locations (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_lot_id       uuid NOT NULL REFERENCES stock_lots(id) ON DELETE CASCADE ON UPDATE CASCADE,
    storage_location_id uuid NOT NULL REFERENCES storage_locations(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    quantity           int NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    reserved_quantity  int NOT NULL DEFAULT 0 CHECK (reserved_quantity >= 0),
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    deleted_at         timestamptz NULL,
    UNIQUE (stock_lot_id, storage_location_id),
    CHECK (reserved_quantity <= quantity)
);
COMMENT ON TABLE stock_lot_locations IS 'Un lot peut être stocké à PLUSIEURS emplacements (PRD_Qwen-2 §2.3.3).';
CREATE INDEX idx_lotloc_location ON stock_lot_locations(storage_location_id);

CREATE TABLE stock_movements (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference           varchar(40) NOT NULL UNIQUE,
    movement_type       varchar(25) NOT NULL
        CHECK (movement_type IN ('RECEPTION_FOURNISSEUR','MISE_EN_STOCK','TRANSFERT','VENTE','RETOUR_CLIENT',
                                 'AJUSTEMENT_POSITIF','AJUSTEMENT_NEGATIF','DESTRUCTION','PERTE','ECHANTILLON',
                                 'QUARANTAINE','LIBERATION')),
    movement_date       timestamptz NOT NULL DEFAULT now(),
    user_id             uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    reason              varchar(255) NULL,
    source_document_type varchar(40) NULL,
    source_document_id  uuid NULL,
    status              varchar(15) NOT NULL DEFAULT 'VALIDE' CHECK (status IN ('BROUILLON','VALIDE','ANNULE')),
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    deleted_at          timestamptz NULL
);
COMMENT ON COLUMN stock_movements.source_document_id IS 'Référence polymorphe (commande, retour, inventaire) SANS FK stricte — documenté (§9).';
COMMENT ON COLUMN stock_movements.reason IS 'Obligatoire pour ajustements/pertes/destructions (contrôlé en couche applicative).';

CREATE TABLE stock_movement_lines (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_movement_id       uuid NOT NULL REFERENCES stock_movements(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id              uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    stock_lot_id            uuid NOT NULL REFERENCES stock_lots(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    source_location_id      uuid NULL REFERENCES storage_locations(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    destination_location_id uuid NULL REFERENCES storage_locations(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    quantity                int NOT NULL CHECK (quantity > 0),
    created_at              timestamptz NOT NULL DEFAULT now(),
    updated_at              timestamptz NOT NULL DEFAULT now(),
    deleted_at              timestamptz NULL
);
CREATE INDEX idx_movlines_lot ON stock_movement_lines(stock_lot_id);
CREATE INDEX idx_movlines_product ON stock_movement_lines(product_id);

CREATE TABLE inventory_sessions (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference   varchar(40) NOT NULL UNIQUE,
    inventory_type varchar(20) NOT NULL CHECK (inventory_type IN ('COMPLET','PARTIEL','CYCLIQUE','LOT','PRODUIT','EMPLACEMENT')),
    status      varchar(15) NOT NULL DEFAULT 'EN_COURS' CHECK (status IN ('EN_COURS','CLOTUREE','ANNULEE')),
    start_date  timestamptz NOT NULL DEFAULT now(),
    closed_date timestamptz NULL,
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    comments    varchar(500) NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    deleted_at  timestamptz NULL
);

CREATE TABLE inventory_counts (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_session_id uuid NOT NULL REFERENCES inventory_sessions(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id          uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    stock_lot_id        uuid NOT NULL REFERENCES stock_lots(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    storage_location_id uuid NOT NULL REFERENCES storage_locations(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    system_quantity     int NOT NULL CHECK (system_quantity >= 0),
    counted_quantity    int NOT NULL CHECK (counted_quantity >= 0),
    adjustment_reason   varchar(255) NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    deleted_at          timestamptz NULL
);
COMMENT ON TABLE inventory_counts IS 'difference = counted_quantity - system_quantity, calculée en applicatif/requête (pas de colonne générée pour rester portable).';

-- ============================================================================
-- DOMAINE 7 — VENTES & FACTURATION
-- ============================================================================

CREATE TABLE sales_orders (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference        varchar(40) NOT NULL UNIQUE,
    client_id        uuid NOT NULL REFERENCES clients(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    currency_code    char(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT ON UPDATE CASCADE,
    order_date       date NOT NULL DEFAULT CURRENT_DATE,
    status           varchar(20) NOT NULL DEFAULT 'BROUILLON'
        CHECK (status IN ('BROUILLON','DEVIS','CONFIRMEE','RESERVEE','EN_PREPARATION','PRETE',
                          'LIVREE','PARTIELLEMENT_LIVREE','FACTUREE','ANNULEE')),
    total_ht         numeric(18,2) NOT NULL DEFAULT 0,
    total_vat        numeric(18,2) NOT NULL DEFAULT 0,
    total_ttc        numeric(18,2) NOT NULL DEFAULT 0,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    deleted_at       timestamptz NULL
);

CREATE TABLE sales_order_lines (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_id   uuid NOT NULL REFERENCES sales_orders(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id       uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    requested_qty    int NOT NULL CHECK (requested_qty > 0),
    unit_price_ht    numeric(18,2) NOT NULL CHECK (unit_price_ht >= 0),
    vat_rate         numeric(5,2) NOT NULL DEFAULT 0,
    line_total_ht    numeric(18,2) NOT NULL DEFAULT 0,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    deleted_at       timestamptz NULL
);

CREATE TABLE stock_reservations (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_order_id     uuid NOT NULL REFERENCES sales_orders(id) ON DELETE CASCADE ON UPDATE CASCADE,
    sales_order_line_id uuid NOT NULL REFERENCES sales_order_lines(id) ON DELETE CASCADE ON UPDATE CASCADE,
    stock_lot_id       uuid NOT NULL REFERENCES stock_lots(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    storage_location_id uuid NOT NULL REFERENCES storage_locations(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    reserved_quantity  int NOT NULL CHECK (reserved_quantity > 0),
    status             varchar(15) NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','CONSOMMEE','ANNULEE')),
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    deleted_at         timestamptz NULL
);
COMMENT ON TABLE stock_reservations IS 'Allocation FEFO multi-lots par ligne de vente (stress-test §2.2).';
CREATE INDEX idx_reservations_lot ON stock_reservations(stock_lot_id);

CREATE TABLE deliveries (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference      varchar(40) NOT NULL UNIQUE,
    sales_order_id uuid NOT NULL REFERENCES sales_orders(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    delivery_date  date NOT NULL DEFAULT CURRENT_DATE,
    status         varchar(15) NOT NULL DEFAULT 'LIVREE' CHECK (status IN ('PREPAREE','LIVREE','ANNULEE')),
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    deleted_at     timestamptz NULL
);

CREATE TABLE delivery_lines (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_id    uuid NOT NULL REFERENCES deliveries(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id     uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    stock_lot_id   uuid NOT NULL REFERENCES stock_lots(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    quantity       int NOT NULL CHECK (quantity > 0),
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    deleted_at     timestamptz NULL
);
COMMENT ON COLUMN delivery_lines.stock_lot_id IS 'Traçabilité : chaque produit vendu remonte à son lot (PRD §10.6, §8.14.2).';

CREATE TABLE invoices (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference      varchar(40) NOT NULL UNIQUE,
    document_type  varchar(10) NOT NULL DEFAULT 'FACTURE' CHECK (document_type IN ('FACTURE','AVOIR')),
    client_id      uuid NOT NULL REFERENCES clients(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    sales_order_id uuid NULL REFERENCES sales_orders(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    delivery_id    uuid NULL REFERENCES deliveries(id) ON DELETE SET NULL ON UPDATE CASCADE,
    invoice_date   date NOT NULL DEFAULT CURRENT_DATE,
    currency_code  char(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT ON UPDATE CASCADE,
    total_ht       numeric(18,2) NOT NULL DEFAULT 0,
    total_vat      numeric(18,2) NOT NULL DEFAULT 0,
    total_ttc      numeric(18,2) NOT NULL DEFAULT 0,
    is_paid        boolean NOT NULL DEFAULT FALSE,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now(),
    deleted_at     timestamptz NULL
);
COMMENT ON COLUMN invoices.document_type IS 'Un AVOIR est une facture de type AVOIR (montants négatifs en applicatif) — évite une table et une référence circulaire.';

CREATE TABLE invoice_lines (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id    uuid NOT NULL REFERENCES invoices(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id    uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    stock_lot_id  uuid NULL REFERENCES stock_lots(id) ON DELETE SET NULL ON UPDATE CASCADE,
    quantity      int NOT NULL CHECK (quantity > 0),
    unit_price_ht numeric(18,2) NOT NULL,
    vat_rate      numeric(5,2) NOT NULL DEFAULT 0,
    line_total_ht numeric(18,2) NOT NULL DEFAULT 0,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    deleted_at    timestamptz NULL
);

CREATE TABLE customer_returns (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference        varchar(40) NOT NULL UNIQUE,
    client_id        uuid NOT NULL REFERENCES clients(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    sales_order_id   uuid NULL REFERENCES sales_orders(id) ON DELETE SET NULL ON UPDATE CASCADE,
    delivery_id      uuid NULL REFERENCES deliveries(id) ON DELETE SET NULL ON UPDATE CASCADE,
    return_date      date NOT NULL DEFAULT CURRENT_DATE,
    reason           varchar(255) NOT NULL,
    decision         varchar(20) NOT NULL DEFAULT 'EN_ATTENTE'
        CHECK (decision IN ('EN_ATTENTE','REmise_EN_STOCK','QUARANTAINE','REFUSE','DETRUIT')),
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    deleted_at       timestamptz NULL
);

CREATE TABLE return_lines (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_return_id uuid NOT NULL REFERENCES customer_returns(id) ON DELETE CASCADE ON UPDATE CASCADE,
    product_id         uuid NOT NULL REFERENCES products(id) ON DELETE RESTRICT ON UPDATE CASCADE,
    stock_lot_id       uuid NULL REFERENCES stock_lots(id) ON DELETE SET NULL ON UPDATE CASCADE,
    quantity           int NOT NULL CHECK (quantity > 0),
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    deleted_at         timestamptz NULL
);

-- ============================================================================
-- DOMAINE 8 — PRÉVISION & NOTIFICATIONS
-- ============================================================================

CREATE TABLE replenishment_suggestions (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id         uuid NOT NULL REFERENCES products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    suggested_qty      int NOT NULL CHECK (suggested_qty > 0),
    reorder_point      int NOT NULL CHECK (reorder_point >= 0),
    available_stock    int NOT NULL CHECK (available_stock >= 0),
    in_transit_stock   int NOT NULL DEFAULT 0 CHECK (in_transit_stock >= 0),
    avg_daily_consumption numeric(12,4) NOT NULL CHECK (avg_daily_consumption >= 0),
    total_lead_days    int NOT NULL CHECK (total_lead_days >= 0),
    deadline_to_order  date NOT NULL,
    status             varchar(15) NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','CONVERTIE','IGNOREE')),
    converted_po_id    uuid NULL REFERENCES purchase_orders(id) ON DELETE SET NULL ON UPDATE CASCADE,
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    deleted_at         timestamptz NULL
);
COMMENT ON TABLE replenishment_suggestions IS 'Point de commande = conso_moy × délai_total + stock_sécurité (PRD §8.9, Qwen-3 §3.14.3).';

CREATE TABLE notifications (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       uuid NULL REFERENCES users(id) ON DELETE CASCADE ON UPDATE CASCADE,
    notif_type    varchar(40) NOT NULL
        CHECK (notif_type IN ('STOCK_FAIBLE','RUPTURE','PEREMPTION_PROCHE','LOT_PERIME','RECEPTION_RETARD',
                              'ECART_RECEPTION','QUARANTAINE','PREPARATION_PRETE','FACTURE_GENEREE','RETOUR_CLIENT','REAPPRO')),
    channel       varchar(15) NOT NULL DEFAULT 'SIGNALR' CHECK (channel IN ('SIGNALR','EMAIL','SMS')),
    title         varchar(200) NOT NULL,
    body          varchar(1000) NULL,
    ref_entity_type varchar(80) NULL,
    ref_entity_id uuid NULL,
    is_read       boolean NOT NULL DEFAULT FALSE,
    created_at    timestamptz NOT NULL DEFAULT now()
);
COMMENT ON COLUMN notifications.ref_entity_id IS 'Référence polymorphe SANS FK stricte (documenté §9).';
CREATE INDEX idx_notifications_user ON notifications(user_id, is_read);

-- ============================================================================
-- TRIGGERS updated_at (systématiques)
-- ============================================================================
DO $$
DECLARE t text;
BEGIN
    FOR t IN
        SELECT unnest(ARRAY[
          'categories','product_forms','therapeutic_classes','products','product_packagings',
          'suppliers','clients','users','roles','currencies','exchange_rates','pricing_profiles',
          'product_price_history','purchase_orders','purchase_order_lines','shipments','logistics_costs',
          'warehouses','storage_locations','stock_lots','stock_lot_locations','stock_movements',
          'stock_movement_lines','inventory_sessions','inventory_counts','sales_orders','sales_order_lines',
          'stock_reservations','deliveries','delivery_lines','invoices','invoice_lines',
          'customer_returns','return_lines','replenishment_suggestions'
        ])
    LOOP
        EXECUTE format('CREATE TRIGGER trg_%s_updated_at BEFORE UPDATE ON %I
                        FOR EACH ROW EXECUTE FUNCTION set_updated_at();', t, t);
    END LOOP;
END $$;

-- ============================================================================
-- DONNÉES DE RÉFÉRENCE MINIMALES (devises)
-- ============================================================================
INSERT INTO currencies (code, name, symbol) VALUES
 ('XOF','Franc CFA (UEMOA)','FCFA'),
 ('EUR','Euro','€'),
 ('USD','Dollar américain','$')
ON CONFLICT (code) DO NOTHING;
```

---

## LIVRABLE 2 — Document `LABMEDIS-modele-donnees.md`

> ⚠️ Les diagrammes Mermaid ci-dessous ont fait l'objet d'une **relecture syntaxique manuelle** (pas de parseur disponible en session — voir bandeau). Ils sont écrits en syntaxe `erDiagram`/`sequenceDiagram` standard.

### 1. En-tête & bandeau de statut

**Statut : RELECTURE MANUELLE (non exécuté).** Moteur cible : PostgreSQL 14+.
Chiffres issus de la relecture : **41 tables**, **~95 contraintes** (PK/FK/UK/CHECK), **13 index explicites** (+ index implicites de PK/UK), **35 triggers** `updated_at`, **3 devises** seedées. Aucune exécution réelle n'a été possible dans cette session ; ces nombres sont des comptages manuels.

### 2. Méthodologie

Lecture intégrale des 5 fichiers sources. Conventions retenues : `snake_case`, PK `uuid`, FK `<singulier>_id`, statuts `VARCHAR+CHECK`, soft delete `deleted_at`, instantané de taux sur chaque transaction, références polymorphes sans FK. **Réellement testé : RIEN** (pas d'environnement) — remplacé par une relecture ligne-à-ligne assumée et étiquetée comme telle, conformément à la Phase 3.5.

### 3. Comparaison avec les brouillons existants

Les fichiers `PRD_Qwen-2` et `PRD_CLAUDE §9` contenaient déjà des esquisses d'entités. Grille lacune → source → solution :

| Lacune constatée dans les brouillons | Source qui l'exige | Solution apportée |
|---|---|---|
| Pas de table de **réservation de stock** dédiée | Stress-test §2.2 + FEFO multi-lots (Qwen-2 §2.9) | Table `stock_reservations` |
| **Avoir** non modélisé, risque de réf. circulaire | Qwen-3 §3.12.4, PRD §20.3 | Avoir = `invoices.document_type='AVOIR'` |
| Pas de **RBAC explicite** (rôles en texte libre) | Mission §2.6, Qwen-3 §3.1 | Tables `roles`, `permissions`, `user_roles`, `role_permissions` |
| **Conditionnements** multiples absents | PRD §8.1.1, Qwen-2 §2.3.4 | Table `product_packagings` |
| Pas de **snapshot de taux** sur ligne d'achat | Règle d'or mission §2.5, décision C-3 | `purchase_order_lines.locked_exchange_rate` |
| Pas de **journal d'audit** | PRD §12.5 | Table `audit_logs` |
| Frais logistiques non isolés | Qwen-3 US-LOG-02 | Table `logistics_costs` |

### 4. Diagramme maître (relations structurantes uniquement)

```mermaid
erDiagram
    categories ||--o{ products : classe
    product_forms ||--o{ products : forme
    suppliers ||--o{ purchase_orders : recoit
    suppliers ||--o{ stock_lots : fournit
    products ||--o{ purchase_order_lines : concerne
    purchase_orders ||--o{ purchase_order_lines : contient
    purchase_orders ||--o{ shipments : genere
    shipments ||--o{ stock_lots : aboutit_a
    shipments ||--o{ logistics_costs : supporte
    warehouses ||--o{ storage_locations : contient
    storage_locations ||--o{ stock_lot_locations : localise
    stock_lots ||--o{ stock_lot_locations : stocke
    products ||--o{ stock_lots : decline_en
    stock_lots ||--o{ stock_movement_lines : mouvemente
    stock_movements ||--o{ stock_movement_lines : contient
    clients ||--o{ sales_orders : passe
    sales_orders ||--o{ sales_order_lines : contient
    sales_orders ||--o{ deliveries : livre
    deliveries ||--o{ invoices : facture
    clients ||--o{ customer_returns : retourne
    products ||--o{ product_price_history : tarife
    currencies ||--o{ exchange_rates : convertit
    pricing_profiles ||--o{ stock_lots : coute
    products ||--o{ replenishment_suggestions : reappro
    users ||--o{ user_roles : possede
    roles ||--o{ user_roles : attribue
```

### 5. Sections par domaine

#### 5.1 Domaine 1 — Référentiels catalogue
**Rôle métier** : porter les listes contrôlées (catégories, formes, classes thérapeutiques) et la fiche produit, pour éliminer la saisie libre et les doublons hérités d'Excel.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Forme galénique ≠ dosage | `products.form_id` + `products.dosage` | PRD §2.3.2 (C-2) |
| TVA configurable par produit, jamais déduite catégorie | `products.vat_rate`, `products.is_taxable` | PRD §17.4 |
| Plusieurs conditionnements / produit | `product_packagings` | PRD §8.1.1 |
| Délais fabrication+livraison par produit | `products.manufacturing_lead_days`, `delivery_lead_days` | PRD §8.1.3 |
| Seuil stock sécurité | `products.safety_stock_qty`, `min_stock_threshold` | PRD §8.1.4 |

```mermaid
erDiagram
    categories {
        uuid id PK
        varchar code UK
        varchar name
        numeric default_vat_rate
        int expiry_alert_days
    }
    product_forms {
        uuid id PK
        varchar name UK
    }
    therapeutic_classes {
        uuid id PK
        varchar name UK
    }
    products {
        uuid id PK
        varchar designation
        uuid category_id FK
        uuid form_id FK
        uuid therapeutic_class_id FK
        varchar dosage
        varchar cip_code UK
        uuid default_supplier_id FK
        numeric vat_rate
        boolean is_taxable
        int manufacturing_lead_days
        int delivery_lead_days
        int safety_stock_qty
        varchar default_transport_mode
        boolean is_active
    }
    product_packagings {
        uuid id PK
        uuid product_id FK
        varchar level "UNITE,CARTON,PALETTE,COLIS_EXPRESS"
        int units_per_package
        boolean is_default
    }
    categories ||--o{ products : classe
    product_forms ||--o{ products : forme
    therapeutic_classes ||--o{ products : classe_thera
    products ||--o{ product_packagings : conditionne
```

#### 5.2 Domaine 2 — Partenaires
**Rôle métier** : fiches uniques fournisseurs et clients ; le répartiteur est un type de client.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Fiche fournisseur unique (anti-doublon) | `suppliers.name UNIQUE` | PRD §2.3.1 |
| Répartiteur = type de client | `clients.client_type='REPARTITEUR'` | PRD §9.3 |
| Plafond encours + délai paiement | `clients.credit_limit`, `payment_term_days` | PRD §20.1 |
| Vérif licence distribution (BPD) | `suppliers.wholesale_license_no`, `clients.pharmacy_license_no` | PRD §17.5 |

```mermaid
erDiagram
    suppliers {
        uuid id PK
        varchar name UK
        varchar country
        char default_currency_code
        varchar wholesale_license_no
        int avg_manufacturing_lead_days
        boolean is_active
    }
    clients {
        uuid id PK
        varchar name UK
        varchar client_type "PHARMACIE,CLINIQUE,HOPITAL,CENTRALE_ACHAT,REPARTITEUR,AUTRE"
        varchar city
        int payment_term_days
        numeric credit_limit
        varchar pharmacy_license_no
        boolean is_active
    }
```

#### 5.3 Domaine 3 — IAM & Audit
**Rôle métier** : authentification, RBAC explicite, journalisation de toute action sensible.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Pas de mot de passe en clair | `users.password_hash` | Mission §2.6 |
| RBAC par tables explicites | `roles`, `permissions`, `user_roles`, `role_permissions` | Mission §2.6 |
| Cumul de rôles autorisé | jointure N-N `user_roles` | déduit (aucune exclusivité dans les sources) |
| Audit user/IP/UserAgent | `audit_logs` | PRD §12.5 |

```mermaid
erDiagram
    users {
        uuid id PK
        varchar user_name UK
        varchar email UK "PII"
        varchar password_hash "SECRET-hash"
        boolean is_active
    }
    roles {
        uuid id PK
        varchar code UK
    }
    permissions {
        uuid id PK
        varchar module
        varchar action
    }
    user_roles {
        uuid user_id PK,FK
        uuid role_id PK,FK
    }
    role_permissions {
        uuid role_id PK,FK
        uuid permission_id PK,FK
    }
    audit_logs {
        uuid id PK
        uuid user_id FK
        varchar action
        varchar entity_type
        uuid entity_id "polymorphe-sansFK"
        varchar ip_address
        varchar user_agent
    }
    users ||--o{ user_roles : possede
    roles ||--o{ user_roles : attribue
    roles ||--o{ role_permissions : autorise
    permissions ||--o{ role_permissions : accorde
```

#### 5.4 Domaine 4 — Devises & Pricing
**Rôle métier** : gérer le multi-devises, les coefficients de landing cost par mode de transport, et l'historique de prix avec écart conservé.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Coefficients jamais codés en dur | `pricing_profiles` | Qwen-1 §1.2 |
| Coefficients varient par transport | `pricing_profiles.transport_mode` | Qwen-1 §1.2 |
| Taux versionné + instantané | `exchange_rates` + snapshot sur transactions | décision C-3 |
| Écart PV calculé/appliqué conservé | `product_price_history.price_gap` | PRD §10.8 |

```mermaid
erDiagram
    currencies {
        char code PK
        varchar name
    }
    exchange_rates {
        uuid id PK
        char currency_from FK
        char currency_to FK
        numeric rate
        date effective_date
        varchar rate_type "FIXE,VARIABLE"
    }
    pricing_profiles {
        uuid id PK
        uuid supplier_id FK
        uuid category_id FK
        varchar transport_mode "MARITIME,AERIEN,EXPRESS,TERRESTRE"
        numeric commission_coeff
        numeric freight_coeff
        numeric transit_coeff
        numeric transfer_fee_coeff
        numeric target_margin_coeff
    }
    product_price_history {
        uuid id PK
        uuid product_id FK
        numeric cost_price_cfa
        numeric theoretical_price_ht
        numeric applied_price_ht
        numeric price_gap
        date effective_date
        uuid approved_by FK
    }
    currencies ||--o{ exchange_rates : convertit
```

#### 5.5 Domaine 5 — Achats & Logistique
**Rôle métier** : commandes fournisseurs multi-devises, expéditions multi-modales, frais logistiques, événements de transport.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Statuts commande complet | `purchase_orders.status` | Qwen-3 §3.4.3 |
| Taux figé à la commande | `purchase_order_lines.locked_exchange_rate` | C-3 |
| 1 commande → plusieurs expéditions | `shipments.purchase_order_id` | PRD §8.3.3 |
| Frais alloués au prorata | `logistics_costs.allocation_method` | Qwen-3 US-LOG-02 |
| Autorisation import DPML + régime douanier | `shipments.import_auth_no`, `customs_regime` | PRD §17.2, §17.3 |

```mermaid
erDiagram
    purchase_orders {
        uuid id PK
        varchar reference UK
        uuid supplier_id FK
        char currency_code FK
        varchar status "BROUILLON,VALIDEE,EXPEDIEE,RECUE,CLOSE,ANNULEE"
        varchar incoterm
    }
    purchase_order_lines {
        uuid id PK
        uuid purchase_order_id FK
        uuid product_id FK
        int ordered_qty_units
        numeric unit_price_foreign
        numeric locked_exchange_rate "snapshot"
        numeric unit_price_cfa
    }
    shipments {
        uuid id PK
        varchar reference UK
        uuid purchase_order_id FK
        varchar transport_mode
        varchar customs_regime
        varchar import_auth_no
        varchar status "PREPAREE,EN_TRANSIT,ARRIVEE,RECUE"
    }
    logistics_costs {
        uuid id PK
        uuid shipment_id FK
        varchar cost_type "FREIGHT,TRANSIT,DOUANE,COMMISSION,ASSURANCE"
        numeric amount_cfa
        varchar allocation_method "VALEUR,QUANTITE,VOLUME"
    }
    shipment_events {
        uuid id PK
        uuid shipment_id FK
        timestamptz event_date
        varchar status
    }
    purchase_orders ||--o{ purchase_order_lines : contient
    purchase_orders ||--o{ shipments : genere
    shipments ||--o{ logistics_costs : supporte
    shipments ||--o{ shipment_events : trace
```

#### 5.6 Domaine 6 — Stock & Entreposage
**Rôle métier** : réception par lot, quarantaine/libération, emplacements, mouvements, inventaires.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Unicité lot par (fournisseur, produit) | `stock_lots UNIQUE(supplier_id,product_id,supplier_batch_number)` | PRD §10.1 |
| Qté réelle ≠ conditionnement standard | `stock_lots.observed_units_per_carton` | PRD §10.2 |
| Lot multi-emplacement | `stock_lot_locations` | Qwen-2 §2.3.3 |
| Statuts qualité (quarantaine, libéré…) | `stock_lots.status` | Qwen-2 §2.4.6 |
| Emplacements hiérarchiques | `storage_locations.parent_location_id` | Qwen-2 §2.5 |
| Dispo = physique − réservé | `stock_lot_locations.quantity - reserved_quantity` | Qwen-2 §2.4.3 |

```mermaid
erDiagram
    warehouses {
        uuid id PK
        varchar name UK
    }
    storage_locations {
        uuid id PK
        uuid warehouse_id FK
        varchar code UK
        varchar location_type "RECEPTION,QUARANTAINE,STOCKAGE,PICKING,CHAINE_FROID,PERIMES"
        uuid parent_location_id FK
        boolean is_locked
    }
    stock_lots {
        uuid id PK
        uuid product_id FK
        uuid supplier_id FK
        uuid shipment_id FK
        varchar supplier_batch_number
        date expiry_date
        varchar status "EN_RECEPTION,QUARANTAINE,LIBERE,NON_CONFORME,PERIME,DETRUIT"
        int received_qty_units
        int observed_units_per_carton
        numeric unit_cost_cfa
        uuid pricing_profile_id FK
    }
    stock_lot_locations {
        uuid id PK
        uuid stock_lot_id FK
        uuid storage_location_id FK
        int quantity
        int reserved_quantity
    }
    stock_movements {
        uuid id PK
        varchar reference UK
        varchar movement_type "RECEPTION,VENTE,TRANSFERT,DESTRUCTION,QUARANTAINE,LIBERATION"
        uuid user_id FK
        uuid source_document_id "polymorphe-sansFK"
    }
    stock_movement_lines {
        uuid id PK
        uuid stock_movement_id FK
        uuid stock_lot_id FK
        uuid source_location_id FK
        uuid destination_location_id FK
        int quantity
    }
    inventory_sessions {
        uuid id PK
        varchar reference UK
        varchar inventory_type
        varchar status "EN_COURS,CLOTUREE,ANNULEE"
    }
    inventory_counts {
        uuid id PK
        uuid inventory_session_id FK
        uuid stock_lot_id FK
        int system_quantity
        int counted_quantity
    }
    warehouses ||--o{ storage_locations : contient
    storage_locations ||--o{ stock_lot_locations : localise
    stock_lots ||--o{ stock_lot_locations : stocke
    stock_movements ||--o{ stock_movement_lines : contient
    stock_lots ||--o{ stock_movement_lines : mouvemente
    inventory_sessions ||--o{ inventory_counts : compte
```

#### 5.7 Domaine 7 — Ventes & Facturation
**Rôle métier** : commandes clients, réservation FEFO, livraison tracée par lot, facture/avoir, retours.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Réservation multi-lots FEFO | `stock_reservations` | stress-test §2.2 |
| Traçabilité produit vendu → lot | `delivery_lines.stock_lot_id` | PRD §10.6 |
| Avoir = facture de type AVOIR | `invoices.document_type` | Qwen-3 §3.12.4 |
| Retour lié à commande/lot + décision | `customer_returns.decision`, `return_lines` | PRD §20.3 |

```mermaid
erDiagram
    sales_orders {
        uuid id PK
        varchar reference UK
        uuid client_id FK
        varchar status "BROUILLON,CONFIRMEE,RESERVEE,EN_PREPARATION,PRETE,LIVREE,FACTUREE"
        numeric total_ttc
    }
    sales_order_lines {
        uuid id PK
        uuid sales_order_id FK
        uuid product_id FK
        int requested_qty
        numeric unit_price_ht
    }
    stock_reservations {
        uuid id PK
        uuid sales_order_line_id FK
        uuid stock_lot_id FK
        uuid storage_location_id FK
        int reserved_quantity
        varchar status "ACTIVE,CONSOMMEE,ANNULEE"
    }
    deliveries {
        uuid id PK
        varchar reference UK
        uuid sales_order_id FK
    }
    delivery_lines {
        uuid id PK
        uuid delivery_id FK
        uuid stock_lot_id FK
        int quantity
    }
    invoices {
        uuid id PK
        varchar reference UK
        varchar document_type "FACTURE,AVOIR"
        uuid client_id FK
        numeric total_ttc
    }
    customer_returns {
        uuid id PK
        varchar reference UK
        uuid client_id FK
        varchar decision "EN_ATTENTE,QUARANTAINE,REFUSE,DETRUIT"
    }
    sales_orders ||--o{ sales_order_lines : contient
    sales_order_lines ||--o{ stock_reservations : reserve
    sales_orders ||--o{ deliveries : livre
    deliveries ||--o{ delivery_lines : contient
    sales_orders ||--o{ invoices : facture
    clients ||--o{ customer_returns : retourne
```

#### 5.8 Domaine 8 — Prévision & Notifications
**Rôle métier** : anticipation des ruptures (point de commande) et alertes temps réel.

| Règle de gestion | Table/colonne | Source |
|---|---|---|
| Point de commande | `replenishment_suggestions.reorder_point` | PRD §8.9 |
| Alertes stock/péremption/retard | `notifications.notif_type` | PRD §8.13, Qwen-2 §2.11 |

```mermaid
erDiagram
    replenishment_suggestions {
        uuid id PK
        uuid product_id FK
        int suggested_qty
        int reorder_point
        numeric avg_daily_consumption
        int total_lead_days
        date deadline_to_order
        varchar status "ACTIVE,CONVERTIE,IGNOREE"
        uuid converted_po_id FK
    }
    notifications {
        uuid id PK
        uuid user_id FK
        varchar notif_type "STOCK_FAIBLE,PEREMPTION_PROCHE,LOT_PERIME,REAPPRO"
        varchar channel "SIGNALR,EMAIL,SMS"
        uuid ref_entity_id "polymorphe-sansFK"
        boolean is_read
    }
```

### 6. Intégration systèmes externes
**Néant en v1.** La comptabilité générale est hors périmètre (PRD §5.2) et l'API de taux de change externe est exclue (taux saisis manuellement, §5.2). Aucune table Bridge n'est donc créée. **En V2**, si un ERP comptable ou une API de taux doit être branché, appliquer le pattern **Bridge** : une table-pont côté LABMEDIS référençant l'identifiant externe, **sans fusion de schémas**, et les données répliquées marquées comme *cache de lecture* — jamais source de vérité pour une décision financière (règle mission §2.6).

### 7. Sécurité & gouvernance des données

| Table | Colonnes PII / sensibles | Traitement |
|---|---|---|
| `users` | `email`, `first_name`, `last_name`, `phone`, `password_hash`, `security_stamp` | Hash obligatoire pour le mot de passe ; PII à chiffrer-au-repos si conformité renforcée |
| `clients` | `phone`, `email`, `address` | PII ; accès restreint par rôle |
| `suppliers` | `phone`, `email`, `address` | PII professionnelle |
| `audit_logs` | `ip_address`, `user_agent` | PII technique ; rétention à définir |

- **Secrets** : aucun mot de passe/token en clair — uniquement `password_hash` (ASP.NET Identity).
- **Soft delete** : `deleted_at` partout ; aucune suppression physique (PRD §10.7).
- **RBAC** : tables explicites `roles`/`permissions`/jointures, jamais de champ texte libre.
- **Rétention** : données métier conservées indéfiniment (traçabilité pharmaceutique) ; `audit_logs` et `notifications` à purger/archiver selon politique à définir avec LABMEDIS.

### 8. Requêtes clés & indexation

```sql
-- Stock disponible par produit (physique - réservé), lots libérés non périmés
SELECT p.designation,
       SUM(sll.quantity - sll.reserved_quantity) AS disponible
FROM   products p
JOIN   stock_lots sl        ON sl.product_id = p.id
JOIN   stock_lot_locations sll ON sll.stock_lot_id = sl.id
WHERE  sl.status = 'LIBERE'
  AND  sl.expiry_date > CURRENT_DATE
  AND  sl.deleted_at IS NULL
GROUP  BY p.designation;

-- Allocation FEFO : lots proposés pour une vente, péremption la plus proche d'abord
SELECT sl.supplier_batch_number, sl.expiry_date,
       (sll.quantity - sll.reserved_quantity) AS dispo
FROM   stock_lots sl
JOIN   stock_lot_locations sll ON sll.stock_lot_id = sl.id
WHERE  sl.product_id = :product_id
  AND  sl.status = 'LIBERE'
  AND  sl.expiry_date > CURRENT_DATE
ORDER  BY sl.expiry_date ASC;
```
Index supports : `idx_stock_lots_product`, `idx_stock_lots_expiry`, `idx_stock_lots_status`, `idx_lotloc_location`. À forte volumétrie : `stock_movement_lines`, `audit_logs` (index `created_at` à créer `CONCURRENTLY`), `stock_lot_locations`.

### 9. Décisions d'arbitrage
- **Moteur** : PostgreSQL retenu malgré la mention SQL Server des specs → `[CONTRADICTION C-1]` (documentée plus haut).
- **Forme vs dosage** : deux colonnes distinctes → `[CONTRADICTION C-2]`.
- **Taux de change** : instantané sur chaque transaction, même pour EUR fixe → `[CONTRADICTION C-3]`.
- **Répartiteur** : type de client, pas d'entité propre (PRD §9.3).
- **Avoir** : modélisé comme facture de type `AVOIR` (évite la référence circulaire).
- **Références polymorphes** (`audit_logs.entity_id`, `stock_movements.source_document_id`, `notifications.ref_entity_id`) : **pas de FK stricte**, documenté ici — l'intégrité est contrôlée en couche applicative.
- **FK `products.default_supplier_id`** posée en `ALTER` après création de `suppliers` pour éviter la référence circulaire.
- **`difference` d'inventaire** calculée en requête/applicatif (pas de colonne générée), pour rester portable SQL Server le cas échéant.

### 10. Recommandations V2 / hors-scope
Chaîne du froid (capteurs/alertes température), portail répartiteur, application PDA mobile, intégration ERP comptable (pattern Bridge), API taux de change externe, traçabilité unité-par-unité (numéro de série) si exigée, gestion multi-entrepôts avancée.

### 11. Fichiers livrés
`schema.sql` (ci-dessus) et le présent document `LABMEDIS-modele-donnees.md`.

---

## PHASE 6 — Auto-critique & contrôle qualité

- **6.1** Blocs de code : 3 blocs SQL (schema + 2 requêtes) et 9 blocs Mermaid (1 maître + 8 domaines), tous ouverts/fermés → nombre pair, cohérent. ✔ (comptage manuel)
- **6.2** Mermaid : **pas de parseur disponible** → relecture syntaxique manuelle uniquement, déclarée comme telle (pas de validation par parseur affirmée). ⚠
- **6.3** Cohérence noms de tables/colonnes entre SQL, ERD et matrices : vérifiée manuellement (ex. `stock_lot_locations`, `pricing_profiles`, `product_price_history` identiques partout). ✔ (relecture)
- **6.4** Références croisées (§) : vérifiées présentes. ✔
- **6.6** Ré-exécution SQL : **IMPOSSIBLE** (pas d'environnement) → non réalisée, déclarée. ⚠

**Je ne déclare donc PAS le livrable "exécuté/validé par parseur".** Il est livré avec une **relecture manuelle** pour seule preuve, conformément à la Règle n°1.

---

## Résumé conversationnel

- **Preuves de validation réelle** : **aucune exécution réelle n'a été possible** (pas de sandbox, pas de moteur SQL, pas de parseur Mermaid dans cette session). Tout est présenté comme **relecture manuelle**, jamais comme un test — c'était la condition de la mission, je m'y tiens.
- **Contradictions soulevées** : `[C-1]` SQL Server (specs) vs PostgreSQL (mission) → tranché PostgreSQL ; `[C-2]` champ « Forme » ambigu dans les Excel sources → séparé en `form` + `dosage` ; `[C-3]` taux EUR « fixe » vs « taux à figer à la commande » → réconcilié par le snapshot de taux sur chaque transaction.
- **Décisions structurantes (top 5)** :
  1. **Snapshot du taux de change** sur chaque ligne d'achat/lot (`locked_exchange_rate`) en plus d'une table de taux versionnée — protège l'historique financier.
  2. **Réservation de stock dédiée** (`stock_reservations`) pour permettre l'allocation **FEFO multi-lots** par ligne de vente.
  3. **Avoir = facture de type `AVOIR`**, évitant une table et une référence circulaire.
  4. **RBAC explicite** (rôles/permissions/jonctions) + **audit_logs** pour la conformité pharmaceutique.
  5. **Références polymorphes sans FK stricte** (notifications, audit, source de mouvement) — intégrité gérée en applicative, choix documenté.
- **Pour passer en validation "réelle"** : donne-moi un environnement exécutable (bash/Python avec `psql`, ou un accès PostgreSQL) et je relance les Phases 3, 4 et 6 avec de vraies exécutions et de vrais parseurs.