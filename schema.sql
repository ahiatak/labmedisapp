-- =====================================================================
-- LABMEDIS — Modèle de données — Socle commun
-- Fonction générique de maintenance de `updated_at` (rule 2.5 : seul
-- cas où un trigger est justifié, car cet invariant ne peut pas vivre
-- sereinement en couche applicative sans risque d'oubli).
-- =====================================================================

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION set_updated_at() IS
    'Maintient automatiquement updated_at à chaque UPDATE. Appliqué par un trigger BEFORE UPDATE sur chaque table métier.';
-- =====================================================================
-- DOMAINE 1 — SÉCURITÉ & UTILISATEURS (RBAC, audit, authentification)
-- Rôle métier : authentification, autorisation par rôle/permission,
-- traçabilité de toute action sensible, paramétrage de l'entreprise
-- (licence de dépositaire). Domaine racine : ne dépend d'aucun autre.
-- =====================================================================

-- ---------------------------------------------------------------------
-- company_profile : fiche entreprise LABMEDIS (paramétrage), notamment
-- le n° de licence de dépositaire et son échéance (PRD_CLAUDE §17.1).
-- Table à une seule ligne en pratique (pas de contrainte SQL figée sur
-- "il n'existe qu'une ligne" : ce serait une règle mouvante dans le
-- temps si LABMEDIS ouvrait une seconde entité juridique — laissé à la
-- couche applicative, cf. règle 2.5).
-- ---------------------------------------------------------------------
CREATE TABLE company_profile (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_name                VARCHAR(200) NOT NULL,
    address                     VARCHAR(300),
    depositary_license_number   VARCHAR(100),
    depositary_license_issued_at DATE,
    depositary_license_expires_at DATE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                  TIMESTAMPTZ
);
CREATE TRIGGER trg_company_profile_updated_at BEFORE UPDATE ON company_profile
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE company_profile IS 'Fiche réglementaire LABMEDIS : licence de dépositaire DPML et son échéance (PRD_CLAUDE §17.1).';

-- ---------------------------------------------------------------------
-- users : comptes utilisateurs de la plateforme (mappage ASP.NET
-- Identity côté backend — PRD_Qwen-5 §5.4.1). Le mot de passe n'est
-- JAMAIS stocké : seul le hash l'est (colonne explicitement nommée
-- password_hash pour lever toute ambiguïté).
-- ---------------------------------------------------------------------
CREATE TABLE users (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email                   VARCHAR(255) NOT NULL,
    password_hash           VARCHAR(255) NOT NULL,
    first_name              VARCHAR(100) NOT NULL,
    last_name               VARCHAR(100) NOT NULL,
    phone                   VARCHAR(50),
    is_active               BOOLEAN NOT NULL DEFAULT true,
    last_login_at           TIMESTAMPTZ,
    last_password_change_at TIMESTAMPTZ,
    failed_login_attempts   INTEGER NOT NULL DEFAULT 0,
    lockout_end_at          TIMESTAMPTZ,
    created_by_user_id      UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : perdre la trace du créateur n'invalide pas le compte lui-même
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at              TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_users_email ON users (lower(email)) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE users IS 'Comptes utilisateurs (ASP.NET Identity). Email unique parmi les comptes non supprimés (index partiel).';

-- ---------------------------------------------------------------------
-- roles : rôles métier (PRD_Qwen-5 §5.5.2 — 10 rôles recommandés,
-- réconciliés avec les 6 rôles de PRD_CLAUDE §6, cf. décisions
-- d'arbitrage). Jamais un champ texte libre sur l'utilisateur : RBAC
-- représenté par des tables explicites (règle 2.6).
-- ---------------------------------------------------------------------
CREATE TABLE roles (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code        VARCHAR(50) NOT NULL,
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(300),
    is_system   BOOLEAN NOT NULL DEFAULT false, -- rôle système protégé contre la suppression (ex. Admin)
    is_active   BOOLEAN NOT NULL DEFAULT true,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at  TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_roles_code ON roles (code) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_roles_updated_at BEFORE UPDATE ON roles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE roles IS 'Rôles métier (Admin, Direction, Achats, Logistique, Magasinier, Qualité, Commercial, Comptable, Préparateur, Lecture seule).';

-- ---------------------------------------------------------------------
-- permissions : permissions unitaires au format Module.Action
-- (PRD_Qwen-5 §5.5.3).
-- ---------------------------------------------------------------------
CREATE TABLE permissions (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code        VARCHAR(100) NOT NULL, -- ex. 'Products.Create'
    module      VARCHAR(50) NOT NULL,  -- ex. 'Products'
    name        VARCHAR(150) NOT NULL,
    description VARCHAR(300),
    is_system   BOOLEAN NOT NULL DEFAULT false,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at  TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_permissions_code ON permissions (code) WHERE deleted_at IS NULL;
CREATE INDEX ix_permissions_module ON permissions (module);
CREATE TRIGGER trg_permissions_updated_at BEFORE UPDATE ON permissions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE permissions IS 'Permissions unitaires (Module.Action) utilisées comme claims JWT (PRD_Qwen-5 §5.6.3).';

-- ---------------------------------------------------------------------
-- role_permissions : association rôle ↔ permissions.
-- ---------------------------------------------------------------------
CREATE TABLE role_permissions (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id       UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,       -- CASCADE : une association n'a pas de sens sans son rôle
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE, -- CASCADE : idem côté permission
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_role_permissions ON role_permissions (role_id, permission_id) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_role_permissions_updated_at BEFORE UPDATE ON role_permissions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE role_permissions IS 'Matrice rôle → permissions (PRD_Qwen-5 §5.5.4).';

-- ---------------------------------------------------------------------
-- user_roles : un utilisateur peut avoir plusieurs rôles (US-RBAC-03).
-- ---------------------------------------------------------------------
CREATE TABLE user_roles (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE, -- CASCADE : l'affectation n'a pas de sens sans l'utilisateur
    role_id    UUID NOT NULL REFERENCES roles(id) ON DELETE RESTRICT, -- RESTRICT : empêche de supprimer un rôle encore affecté (forcer une réaffectation explicite)
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_user_roles ON user_roles (user_id, role_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_user_roles_user ON user_roles (user_id);
CREATE TRIGGER trg_user_roles_updated_at BEFORE UPDATE ON user_roles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE user_roles IS 'Affectation multi-rôles par utilisateur (US-RBAC-03).';

-- ---------------------------------------------------------------------
-- user_permission_exceptions : dérogations individuelles sans modifier
-- tout un rôle (PRD_Qwen-5 §5.5.5 UserPermissionException).
-- ---------------------------------------------------------------------
CREATE TABLE user_permission_exceptions (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,       -- CASCADE : dérogation individuelle sans objet si l'utilisateur disparaît
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE, -- CASCADE : idem côté permission
    is_granted    BOOLEAN NOT NULL, -- true = accorde explicitement, false = retire explicitement
    reason        VARCHAR(300),
    valid_from    TIMESTAMPTZ,
    valid_to      TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ
);
CREATE INDEX ix_user_permission_exceptions_user ON user_permission_exceptions (user_id);
CREATE TRIGGER trg_user_permission_exceptions_updated_at BEFORE UPDATE ON user_permission_exceptions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE user_permission_exceptions IS 'Dérogations individuelles de permission, accordées ou retirées, sans modifier le rôle (PRD_Qwen-5 §5.5.5).';

-- ---------------------------------------------------------------------
-- refresh_tokens : jetons de renouvellement JWT, révocables
-- (PRD_Qwen-5 §5.3.3).
-- ---------------------------------------------------------------------
CREATE TABLE refresh_tokens (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE, -- CASCADE : un jeton n'a aucun sens sans son utilisateur
    token_hash   VARCHAR(255) NOT NULL, -- jamais le jeton en clair
    issued_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at   TIMESTAMPTZ NOT NULL,
    revoked_at   TIMESTAMPTZ,
    revoked_reason VARCHAR(200),
    created_ip   VARCHAR(64),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at   TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_refresh_tokens_hash ON refresh_tokens (token_hash);
CREATE INDEX ix_refresh_tokens_user ON refresh_tokens (user_id) WHERE revoked_at IS NULL;
CREATE TRIGGER trg_refresh_tokens_updated_at BEFORE UPDATE ON refresh_tokens
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE refresh_tokens IS 'Jetons de renouvellement JWT, hashés, révocables (déconnexion globale, désactivation utilisateur).';

-- ---------------------------------------------------------------------
-- user_password_history : historique des hachages de mot de passe, pour
-- empêcher la réutilisation des 5 derniers (PRD_Qwen-5 §5.3.4). Ajoutée
-- lors de la revue de complétude : absente du modèle initial alors que
-- la règle est explicite dans les sources.
-- ---------------------------------------------------------------------
CREATE TABLE user_password_history (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE, -- CASCADE : historique de mot de passe sans objet si l'utilisateur disparaît
    password_hash VARCHAR(255) NOT NULL, -- jamais en clair, même dans l'historique
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX ix_user_password_history_user ON user_password_history (user_id, created_at DESC);
COMMENT ON TABLE user_password_history IS 'Historique des hachages de mot de passe (5 derniers vérifiés en couche applicative avant tout changement) — PRD_Qwen-5 §5.3.4.';

-- ---------------------------------------------------------------------
-- audit_logs : traçabilité de toute action sensible (PRD_Qwen-5
-- §5.8.2 AuditLog). Référence polymorphe optionnelle vers l'entité
-- métier concernée : PAS de FK stricte (règle 2.5), car la cible peut
-- être n'importe quelle table du modèle — documenté explicitement.
-- ---------------------------------------------------------------------
CREATE TABLE audit_logs (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID REFERENCES users(id) ON DELETE RESTRICT, -- RESTRICT : un log ne doit jamais perdre l'identité de son auteur ; nullable pour les échecs d'authentification (utilisateur non résolu)
    user_full_name   VARCHAR(200), -- dénormalisé : le nom doit rester lisible même si le compte est renommé/désactivé plus tard
    action            VARCHAR(150) NOT NULL,
    module            VARCHAR(50) NOT NULL,
    http_method       VARCHAR(10),
    path              VARCHAR(300),
    entity_type       VARCHAR(100), -- référence polymorphe : nom de table logique (ex. 'purchase_orders'), jamais de FK stricte
    entity_id         UUID,          -- référence polymorphe : id de la ligne concernée, jamais de FK stricte
    ip_address        VARCHAR(64),
    user_agent        VARCHAR(300),
    is_success        BOOLEAN NOT NULL,
    response_message  VARCHAR(500),
    executed_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at        TIMESTAMPTZ
);
CREATE INDEX ix_audit_logs_user ON audit_logs (user_id);
CREATE INDEX ix_audit_logs_executed_at ON audit_logs (executed_at DESC);
CREATE INDEX ix_audit_logs_entity ON audit_logs (entity_type, entity_id);
CREATE TRIGGER trg_audit_logs_updated_at BEFORE UPDATE ON audit_logs
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE audit_logs IS 'Journal d''audit structuré (complète ILoggerManager/NLog). entity_type/entity_id forment une référence polymorphe sans FK stricte, documentée ici explicitement.';
-- =====================================================================
-- DOMAINE 2 — RÉFÉRENTIEL COMMERCIAL (master data)
-- Rôle métier : catalogue produit, fournisseurs, clients et
-- catégories/classes contrôlées — élimine la saisie libre à l'origine
-- des incohérences relevées dans les fichiers Excel actuels
-- (PRD_CLAUDE §2.3, §3). Dépend uniquement du domaine Sécurité
-- (created_by → users).
-- =====================================================================

-- ---------------------------------------------------------------------
-- categories : familles produit contrôlées (PRD_CLAUDE §8.1.6).
-- Porte le taux de TVA par défaut (surchageable par produit, cf.
-- décision d'arbitrage TVA).
-- ---------------------------------------------------------------------
CREATE TABLE categories (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code              VARCHAR(50) NOT NULL,
    name              VARCHAR(100) NOT NULL,
    default_vat_rate  NUMERIC(5,4), -- ex. 0.1800 ; NULL = à statuer produit par produit (cas réactifs de laboratoire, cf. données réelles)
    expiry_alert_days INTEGER NOT NULL DEFAULT 90, -- seuil d'alerte péremption par défaut (PRD_Qwen-2 §2.4.5 : 60 à 120 j selon catégorie)
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at        TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_categories_code ON categories (code) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_categories_updated_at BEFORE UPDATE ON categories
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE categories IS 'Familles produit contrôlées (produit infantile, médicament, réactif de laboratoire, cosmétique, complément alimentaire, insecticide).';

-- ---------------------------------------------------------------------
-- therapeutic_classes : classes thérapeutiques contrôlées (21 valeurs
-- distinctes relevées dans le catalogue réel — cf. Phase 1).
-- ---------------------------------------------------------------------
CREATE TABLE therapeutic_classes (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name       VARCHAR(150) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_therapeutic_classes_name ON therapeutic_classes (lower(name)) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_therapeutic_classes_updated_at BEFORE UPDATE ON therapeutic_classes
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE therapeutic_classes IS 'Classes thérapeutiques contrôlées (antalgiques, antibiotiques, lait infantile, réactifs de laboratoire...).';

-- ---------------------------------------------------------------------
-- suppliers : fournisseurs internationaux et locaux (8 fournisseurs
-- réels vérifiés — France, Togo, Maroc, Tunisie, Inde, Suisse, Burkina
-- Faso). Nom unique pour éliminer les doublons de libellé constatés
-- (« HORIBA » / « HORIBA ABX SAS »...).
-- ---------------------------------------------------------------------
CREATE TABLE suppliers (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                VARCHAR(200) NOT NULL,
    address             VARCHAR(300),
    po_box              VARCHAR(50),
    phone               VARCHAR(100),
    country             VARCHAR(100) NOT NULL,
    default_currency    VARCHAR(3) NOT NULL, -- FK logique vers currencies.code, posée en domaine 3 (pas de dépendance croisée en amont : simple VARCHAR ici, cf. §2.4 ordre des domaines)
    is_local            BOOLEAN NOT NULL DEFAULT false, -- achat local (ex. DEO GRATIAS PHARMA, Togo) : peut sauter le circuit Expédition/Douane
    is_active           BOOLEAN NOT NULL DEFAULT true,
    distribution_authorization_verified BOOLEAN NOT NULL DEFAULT false, -- contrôle BPD/WHO-GDP avant référencement (PRD_CLAUDE §17.5 point 1)
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_suppliers_name ON suppliers (lower(name)) WHERE deleted_at IS NULL;
CREATE INDEX ix_suppliers_country ON suppliers (country);
CREATE TRIGGER trg_suppliers_updated_at BEFORE UPDATE ON suppliers
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE suppliers IS 'Fiche fournisseur unique (référentiel contrôlé, remplace la saisie libre source des doublons observés).';

-- ---------------------------------------------------------------------
-- customers : clients LABMEDIS. Le « répartiteur » n'est PAS une
-- entité séparée : c'est un client dont le type = 'repartiteur'
-- (décision PRD_CLAUDE §9.3, confirmée par les données réelles qui
-- mélangent pharmacies/cliniques/répartiteurs dans une même liste).
-- ---------------------------------------------------------------------
CREATE TABLE customers (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                  VARCHAR(200) NOT NULL,
    customer_type         VARCHAR(30) NOT NULL DEFAULT 'autre'
        CHECK (customer_type IN ('pharmacie','clinique','hopital','centrale_achat','repartiteur','autre')),
    address               VARCHAR(300),
    po_box                VARCHAR(50),
    phone                 VARCHAR(100),
    city                  VARCHAR(100) NOT NULL,
    payment_term_days     INTEGER NOT NULL DEFAULT 30, -- délai de paiement (PRD_CLAUDE §9.2/§20.1)
    credit_limit_cfa      NUMERIC(14,0), -- plafond d'encours autorisé (PRD_CLAUDE §20.1)
    license_verified      BOOLEAN NOT NULL DEFAULT false, -- contrôle BPD avant livraison (PRD_CLAUDE §17.5 point 2)
    is_active             BOOLEAN NOT NULL DEFAULT true,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at            TIMESTAMPTZ
);
CREATE INDEX ix_customers_city ON customers (city);
CREATE INDEX ix_customers_type ON customers (customer_type);
CREATE TRIGGER trg_customers_updated_at BEFORE UPDATE ON customers
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE customers IS 'Clients et répartiteurs (répartiteur = customer_type spécifique, pas une entité distincte — décision §9.3).';

-- ---------------------------------------------------------------------
-- products : catalogue produit. `form` = forme pharmaceutique
-- (comprimé, sirop, crème...) — distincte du conditionnement, ce qui
-- clarifie l'ambiguïté Forme/Dosage relevée entre les deux feuilles du
-- fichier Excel produits (PRD_CLAUDE §2.3.2, vérifiée en Phase 1).
-- vat_rate_override permet de ne JAMAIS déduire la TVA de la seule
-- catégorie (PRD_CLAUDE §17.4, confirmé empiriquement par l'exception
-- ABX DILUENT 20L).
-- ---------------------------------------------------------------------
CREATE TABLE products (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    designation                 VARCHAR(250) NOT NULL,
    category_id                 UUID NOT NULL REFERENCES categories(id) ON DELETE RESTRICT, -- RESTRICT : jamais de produit orphelin de catégorie
    therapeutic_class_id        UUID REFERENCES therapeutic_classes(id) ON DELETE RESTRICT,  -- RESTRICT : référentiel contrôlé, pas de suppression silencieuse
    pharmaceutical_form         VARCHAR(100), -- forme pharmaceutique réelle (comprimé, sirop, crème, gel...), distincte du conditionnement
    dosage                      VARCHAR(100), -- ex. '10mg', '400g' — contenance/dosage unitaire
    unit_label                  VARCHAR(50) NOT NULL DEFAULT 'unité', -- boîte, flacon, tube, plaquette...
    carton_quantity             INTEGER, -- nb d'unités de base par carton standard (indicatif — chaque lot conserve sa propre quantité réelle, règle 10.2)
    cip_code                    VARCHAR(50),
    primary_supplier_id         UUID REFERENCES suppliers(id) ON DELETE SET NULL, -- SET NULL : le produit reste valide même sans fournisseur principal désigné
    default_origin_country      VARCHAR(100),
    default_transport_mode      VARCHAR(20) CHECK (default_transport_mode IN ('maritime','aerien','express','terrestre')),
    vat_rate_override           NUMERIC(5,4), -- surcharge le taux de la catégorie ; jamais déduit automatiquement (PRD_CLAUDE §17.4)
    manufacturing_lead_time_days INTEGER, -- délai de fabrication estimé (module 8.1.3 / MRP)
    delivery_lead_time_days      INTEGER, -- délai de livraison estimé
    min_stock_threshold          INTEGER, -- seuil de stock de sécurité simple (redondant en partie avec forecast_parameters.safety_stock, conservé ici comme valeur par défaut affichable sans dépendre du module MRP)
    requires_cold_chain           BOOLEAN NOT NULL DEFAULT false, -- chaîne du froid (réactifs sensibles — PRD_Qwen-2 §2.19, PRD_CLAUDE §17.6)
    is_active                    BOOLEAN NOT NULL DEFAULT true,
    created_at                   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                   TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_products_designation ON products (lower(designation)) WHERE deleted_at IS NULL;
CREATE INDEX ix_products_category ON products (category_id);
CREATE INDEX ix_products_cip_code ON products (cip_code) WHERE cip_code IS NOT NULL;
CREATE INDEX ix_products_active ON products (is_active) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_products_updated_at BEFORE UPDATE ON products
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE products IS 'Catalogue produit. Désignation unique parmi les produits non supprimés (élimine les ~9 doublons exacts constatés dans le fichier source lors de l''import).';

-- ---------------------------------------------------------------------
-- product_suppliers : un produit peut avoir plusieurs fournisseurs
-- habituels (PRD_CLAUDE §8.1.2).
-- ---------------------------------------------------------------------
CREATE TABLE product_suppliers (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id   UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,   -- CASCADE : association sans objet si le produit disparaît
    supplier_id  UUID NOT NULL REFERENCES suppliers(id) ON DELETE RESTRICT, -- RESTRICT : ne pas supprimer un fournisseur encore associé à un produit actif
    is_primary   BOOLEAN NOT NULL DEFAULT false,
    origin_country VARCHAR(100),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at   TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_product_suppliers ON product_suppliers (product_id, supplier_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_product_suppliers_supplier ON product_suppliers (supplier_id);
CREATE TRIGGER trg_product_suppliers_updated_at BEFORE UPDATE ON product_suppliers
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE product_suppliers IS 'Association produit ↔ fournisseurs habituels, avec fournisseur principal et pays d''origine (PRD_CLAUDE §8.1.2).';

-- ---------------------------------------------------------------------
-- product_packagings : niveaux de conditionnement d'un produit (unité,
-- carton, palette, colis express — PRD_Qwen-2 §2.3.4 : « le système
-- doit gérer plusieurs niveaux d'unités »). Ajoutée lors de la revue de
-- complétude : products.carton_quantity seul ne permet pas de modéliser
-- plusieurs niveaux simultanément (ex. carton ET palette) ; le standard
-- déclaré ici reste indicatif, chaque lot conserve sa propre quantité
-- réellement observée (stock_lots.carton_quantity_received, règle 10.2).
-- ---------------------------------------------------------------------
CREATE TABLE product_packagings (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id          UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- CASCADE : un niveau de conditionnement n'a pas de sens sans son produit
    level                 VARCHAR(15) NOT NULL CHECK (level IN ('unite','carton','palette','colis_express')),
    units_per_package       INTEGER NOT NULL CHECK (units_per_package > 0),
    is_default                BOOLEAN NOT NULL DEFAULT false,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                    TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_product_packagings ON product_packagings (product_id, level) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_product_packagings_updated_at BEFORE UPDATE ON product_packagings
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE product_packagings IS 'Niveaux de conditionnement standard d''un produit (carton/12, palette...). Le réel observé à réception vit sur stock_lots (PRD_Qwen-2 §2.3.4).';
-- =====================================================================
-- DOMAINE 3 — PRICING & DEVISES
-- Rôle métier : conversion multi-devises (EUR, USD, XOF) et moteur de
-- prix de revient / prix de vente (cascade de coefficients vérifiée
-- sur données réelles — PRD_Qwen-1 §1.1, Structure_de_prix.xlsx).
-- Dépend du Référentiel Commercial (produits, catégories, fournisseurs).
-- =====================================================================

-- ---------------------------------------------------------------------
-- currencies : référentiel des devises (PRD_CLAUDE §8.10.1).
-- ---------------------------------------------------------------------
CREATE TABLE currencies (
    code            VARCHAR(3) PRIMARY KEY, -- EUR, USD, XOF (ISO 4217)
    name            VARCHAR(50) NOT NULL,
    symbol          VARCHAR(5) NOT NULL,
    decimal_places  SMALLINT NOT NULL DEFAULT 2, -- 0 pour XOF (pas de centimes, règle 10.3/PRD_Qwen-1 §1.5)
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ
);
CREATE TRIGGER trg_currencies_updated_at BEFORE UPDATE ON currencies
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE currencies IS 'Devises de gestion : EUR, USD (achats), XOF (référence de gestion et de vente).';

ALTER TABLE suppliers
    ADD CONSTRAINT fk_suppliers_default_currency FOREIGN KEY (default_currency)
        REFERENCES currencies(code) ON DELETE RESTRICT; -- RESTRICT : ajouté ici (après la création de currencies) pour respecter l'ordre des domaines sans dépendance amont, cf. §2.4

-- ---------------------------------------------------------------------
-- exchange_rates : taux de change historisés. EUR/XOF est fixe
-- (655,957 — parité officielle, vérifiée empiriquement) ; USD/XOF est
-- variable et saisi/actualisé manuellement (PRD_CLAUDE §5.2 point 4,
-- §10.3). Le taux n'est jamais recalculé après coup : chaque
-- PurchaseOrder fige le taux du jour (cf. domaine Achats).
-- ---------------------------------------------------------------------
CREATE TABLE exchange_rates (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    from_currency    VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : historique financier jamais orphelin
    to_currency      VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : idem
    rate             NUMERIC(18,6) NOT NULL,
    effective_date   DATE NOT NULL,
    source           VARCHAR(20) NOT NULL DEFAULT 'manuel' CHECK (source IN ('manuel','api','import')),
    created_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : le taux reste valide même si l'auteur quitte le système
    created_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at       TIMESTAMPTZ
);
CREATE INDEX ix_exchange_rates_pair_date ON exchange_rates (from_currency, to_currency, effective_date DESC);
CREATE TRIGGER trg_exchange_rates_updated_at BEFORE UPDATE ON exchange_rates
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE exchange_rates IS 'Historique des taux de change appliqués. EUR/XOF fixe (655,957) ; USD/XOF variable, source manuelle ou API.';

-- ---------------------------------------------------------------------
-- pricing_profiles : jeux de coefficients multiplicatifs du moteur de
-- prix de revient (PRD_Qwen-1 §1.2, vérifié sur la gamme France Lait).
-- Jamais codés en dur : stockés en base pour ajustement par la
-- direction sans redéploiement.
-- ---------------------------------------------------------------------
CREATE TABLE pricing_profiles (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name                  VARCHAR(150) NOT NULL,
    supplier_id           UUID REFERENCES suppliers(id) ON DELETE CASCADE,  -- CASCADE : un profil dédié à un fournisseur n'a plus de sens si celui-ci disparaît (nullable = profil global)
    category_id           UUID REFERENCES categories(id) ON DELETE CASCADE, -- CASCADE : idem pour un profil dédié à une catégorie (nullable = toutes catégories)
    transport_mode        VARCHAR(20) NOT NULL CHECK (transport_mode IN ('maritime','aerien','express','terrestre')),
    commission_coeff      NUMERIC(8,4) NOT NULL DEFAULT 1.0000,
    freight_coeff         NUMERIC(8,4) NOT NULL DEFAULT 1.0000,
    transit_coeff         NUMERIC(8,4) NOT NULL DEFAULT 1.0000,
    transfer_fee_coeff    NUMERIC(8,4) NOT NULL DEFAULT 1.0000,
    target_margin_coeff   NUMERIC(8,4) NOT NULL DEFAULT 1.0000,
    is_active             BOOLEAN NOT NULL DEFAULT true,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at            TIMESTAMPTZ
);
CREATE INDEX ix_pricing_profiles_supplier ON pricing_profiles (supplier_id);
CREATE INDEX ix_pricing_profiles_category ON pricing_profiles (category_id);
CREATE TRIGGER trg_pricing_profiles_updated_at BEFORE UPDATE ON pricing_profiles
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE pricing_profiles IS 'Profils de coefficients de coût (PA→PR) par fournisseur/catégorie/mode de transport. Formule vérifiée : PR = PA_CFA × commission × freight × transit × frais_transfert.';

-- ---------------------------------------------------------------------
-- product_prices : historique complet du prix par produit (table
-- append-only : la ligne "courante" est celle où effective_to IS
-- NULL). pr_unit_cfa est figé en instantané au moment de la validation
-- (règle 2.5) ; le PMP vivant est recalculable depuis stock_lots (vue
-- dédiée en domaine Entreposage & Stock).
-- ---------------------------------------------------------------------
CREATE TABLE product_prices (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id          UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT, -- RESTRICT : l'historique de prix ne doit jamais perdre son produit
    pricing_profile_id  UUID REFERENCES pricing_profiles(id) ON DELETE SET NULL,  -- SET NULL : la ligne de prix reste lisible même si le profil est retiré ensuite
    pr_unit_cfa         NUMERIC(14,0) NOT NULL, -- prix de revient (PMP figé à la date d'effet), arrondi CFA
    pv_ht_calculated    NUMERIC(14,0) NOT NULL, -- PR × marge cible
    pv_ht_applied       NUMERIC(14,0) NOT NULL, -- « Prix Labmedis HT » réellement pratiqué (peut différer du calculé)
    vat_rate            NUMERIC(5,4) NOT NULL,  -- taux figé à la date d'effet (produit ou catégorie, résolu à l'écriture)
    effective_from      DATE NOT NULL,
    effective_to        DATE, -- NULL = prix courant
    created_by_user_id  UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : ne bloque pas l'historique si l'auteur quitte le système
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at          TIMESTAMPTZ
);
CREATE INDEX ix_product_prices_product ON product_prices (product_id, effective_from DESC);
CREATE UNIQUE INDEX ux_product_prices_current ON product_prices (product_id) WHERE effective_to IS NULL AND deleted_at IS NULL; -- un seul prix courant par produit
CREATE TRIGGER trg_product_prices_updated_at BEFORE UPDATE ON product_prices
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE product_prices IS 'Historique des prix (PR, PV calculé, PV appliqué, écart implicite = pv_ht_applied - pv_ht_calculated). Une seule ligne "courante" par produit (effective_to NULL).';

-- ---------------------------------------------------------------------
-- pricing_simulations : journal des simulations de prix avant décision
-- (US-PRICE-01), distinct du prix réellement validé et publié.
-- ---------------------------------------------------------------------
CREATE TABLE pricing_simulations (
    id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id             UUID REFERENCES products(id) ON DELETE CASCADE, -- CASCADE : une simulation liée à un produit supprimé perd son objet
    pricing_profile_id     UUID REFERENCES pricing_profiles(id) ON DELETE SET NULL, -- SET NULL : la simulation reste lisible sans le profil
    purchase_price_foreign NUMERIC(14,4) NOT NULL,
    purchase_currency      VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : traçabilité financière
    exchange_rate_used     NUMERIC(18,6) NOT NULL,
    landing_cost_cfa       NUMERIC(14,0) NOT NULL,
    target_price_ht_cfa    NUMERIC(14,0) NOT NULL,
    catalog_price_ht_cfa   NUMERIC(14,0), -- prix catalogue au moment de la simulation, pour comparaison (US-PRICE-01 critère 6)
    simulated_by_user_id   UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : la simulation reste consultable
    simulated_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at             TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at             TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at             TIMESTAMPTZ
);
CREATE INDEX ix_pricing_simulations_product ON pricing_simulations (product_id, simulated_at DESC);
CREATE TRIGGER trg_pricing_simulations_updated_at BEFORE UPDATE ON pricing_simulations
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE pricing_simulations IS 'Journal des simulations « what-if » de prix de revient (endpoint POST /api/pricing/simulate), distinct du prix publié.';
-- =====================================================================
-- DOMAINE 4 — ACHATS & LOGISTIQUE INTERNATIONALE
-- Rôle métier : commandes fournisseurs multi-devises, suivi des
-- expéditions (maritime/aérien/express/terrestre), transit douanier et
-- allocation des frais logistiques. Dépend du Référentiel Commercial
-- (fournisseurs, produits) et de Pricing & Devises (devises, taux).
-- =====================================================================

-- ---------------------------------------------------------------------
-- purchase_orders : commande fournisseur. Le taux de change est figé
-- (locked_exchange_rate_id) à la validation (PRD_Qwen-1 §1.3, règle
-- d'or). Statuts consolidés à partir de PRD_Qwen.md §8.2.3 et
-- PRD_Qwen module 3 §3.4.3 (les deux listes se recoupent très
-- largement ; fusionnées sans perte).
-- ---------------------------------------------------------------------
CREATE TABLE purchase_orders (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number            VARCHAR(50) NOT NULL,
    supplier_id             UUID NOT NULL REFERENCES suppliers(id) ON DELETE RESTRICT, -- RESTRICT : traçabilité financière, jamais de commande orpheline
    currency                VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : idem
    locked_exchange_rate_id UUID REFERENCES exchange_rates(id) ON DELETE RESTRICT, -- RESTRICT : ne jamais perdre le taux figé d'une commande déjà validée ; NULL tant que non validée
    incoterm                VARCHAR(10), -- FOB, CIF, EXW... (PRD_CLAUDE §18.1)
    status                  VARCHAR(30) NOT NULL DEFAULT 'brouillon' CHECK (status IN (
                                 'brouillon','en_attente_validation','validee','envoyee',
                                 'en_fabrication','prete_a_expedier','expediee','en_transit',
                                 'partiellement_recue','recue','close','annulee')),
    order_date               DATE NOT NULL DEFAULT CURRENT_DATE,
    expected_delivery_date   DATE,
    cancellation_reason      VARCHAR(300), -- obligatoire en cas d'annulation (règle 3.4.4), contrôle laissé à la couche applicative
    validated_by_user_id     UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : la commande reste lisible si le valideur quitte le système
    validated_at              TIMESTAMPTZ,
    created_by_user_id        UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : idem
    created_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                 TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_purchase_orders_number ON purchase_orders (order_number) WHERE deleted_at IS NULL;
CREATE INDEX ix_purchase_orders_supplier ON purchase_orders (supplier_id);
CREATE INDEX ix_purchase_orders_status ON purchase_orders (status) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_purchase_orders_updated_at BEFORE UPDATE ON purchase_orders
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE purchase_orders IS 'Commande fournisseur multi-devises. Taux de change figé à la validation (traçabilité financière du lot, règle 10.3).';

-- ---------------------------------------------------------------------
-- purchase_order_lines : lignes de commande (produit, quantité,
-- prix unitaire devise).
-- ---------------------------------------------------------------------
CREATE TABLE purchase_order_lines (
    id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id      UUID NOT NULL REFERENCES purchase_orders(id) ON DELETE CASCADE, -- CASCADE : une ligne n'a pas de sens sans son entête
    product_id             UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,        -- RESTRICT : traçabilité achat jamais perdue
    quantity_ordered_units  INTEGER NOT NULL CHECK (quantity_ordered_units > 0),
    quantity_ordered_cartons INTEGER CHECK (quantity_ordered_cartons IS NULL OR quantity_ordered_cartons > 0),
    unit_price_foreign      NUMERIC(14,4) NOT NULL CHECK (unit_price_foreign >= 0),
    created_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                TIMESTAMPTZ
);
CREATE INDEX ix_purchase_order_lines_po ON purchase_order_lines (purchase_order_id);
CREATE INDEX ix_purchase_order_lines_product ON purchase_order_lines (product_id);
CREATE TRIGGER trg_purchase_order_lines_updated_at BEFORE UPDATE ON purchase_order_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE purchase_order_lines IS 'Lignes de commande fournisseur : quantité (unités + cartons) et prix d''achat unitaire en devise.';

-- ---------------------------------------------------------------------
-- purchase_order_status_history : horodatage de chaque changement de
-- statut (US-ACH-03 : « chaque changement de statut est horodaté »).
-- ---------------------------------------------------------------------
CREATE TABLE purchase_order_status_history (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id  UUID NOT NULL REFERENCES purchase_orders(id) ON DELETE CASCADE, -- CASCADE : historique sans objet si la commande disparaît
    status             VARCHAR(30) NOT NULL,
    comment            VARCHAR(300),
    changed_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : l'historique reste lisible
    changed_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at         TIMESTAMPTZ
);
CREATE INDEX ix_po_status_history_po ON purchase_order_status_history (purchase_order_id, changed_at);
CREATE TRIGGER trg_po_status_history_updated_at BEFORE UPDATE ON purchase_order_status_history
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE purchase_order_status_history IS 'Historique horodaté des statuts de commande fournisseur (US-ACH-03).';

-- ---------------------------------------------------------------------
-- shipments : expédition physique. [CONTRADICTION résolue, cf.
-- décisions d'arbitrage] — PRD_CLAUDE modélise Expedition→Commande en
-- N:1 strict, PRD_Qwen module 3 exige qu'une expédition consolide
-- plusieurs commandes. Résolu ici en N:N via shipment_lines qui
-- référence purchase_order_lines (jamais l'entête de commande) : une
-- expédition n'a donc PAS de purchase_order_id direct.
-- ---------------------------------------------------------------------
CREATE TABLE shipments (
    id                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_reference        VARCHAR(50) NOT NULL,
    transport_mode             VARCHAR(20) NOT NULL CHECK (transport_mode IN ('maritime','aerien','express','terrestre')),
    carrier                     VARCHAR(150),
    transport_reference         VARCHAR(100), -- n° de conteneur / LTA / BL / tracking
    customs_regime               VARCHAR(50), -- référentiel OTR (PRD_CLAUDE §17.3) : mise_a_consommation, entrepot_stockage, transit, zone_franche...
    import_authorization_number  VARCHAR(100), -- autorisation DPML (PRD_CLAUDE §17.2)
    import_authorization_date     DATE,
    status                        VARCHAR(30) NOT NULL DEFAULT 'preparee' CHECK (status IN (
                                       'preparee','expediee','en_transit','dedouanement','receptionnee','annulee')),
    departure_date_estimated      DATE,
    departure_date_actual         DATE,
    arrival_date_estimated        DATE,
    arrival_date_actual           DATE,
    created_at                    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                    TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_shipments_reference ON shipments (shipment_reference) WHERE deleted_at IS NULL;
CREATE INDEX ix_shipments_status ON shipments (status) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_shipments_updated_at BEFORE UPDATE ON shipments
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE shipments IS 'Expédition logistique (conteneur/fret aérien/express). Rattachée aux commandes uniquement via shipment_lines (résolution du [CONTRADICTION] cardinalité).';

-- ---------------------------------------------------------------------
-- shipment_lines : point de résolution N:N entre commandes et
-- expéditions — une expédition peut regrouper des lignes de plusieurs
-- commandes ; une ligne de commande peut être livrée en plusieurs
-- expéditions (réception partielle).
-- ---------------------------------------------------------------------
CREATE TABLE shipment_lines (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id              UUID NOT NULL REFERENCES shipments(id) ON DELETE CASCADE,             -- CASCADE : une ligne n'a pas de sens sans son expédition
    purchase_order_line_id   UUID NOT NULL REFERENCES purchase_order_lines(id) ON DELETE RESTRICT,  -- RESTRICT : traçabilité achat↔transport jamais perdue
    quantity_shipped_units   INTEGER NOT NULL CHECK (quantity_shipped_units > 0),
    created_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                 TIMESTAMPTZ
);
CREATE INDEX ix_shipment_lines_shipment ON shipment_lines (shipment_id);
CREATE INDEX ix_shipment_lines_po_line ON shipment_lines (purchase_order_line_id);
CREATE TRIGGER trg_shipment_lines_updated_at BEFORE UPDATE ON shipment_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE shipment_lines IS 'Ligne d''expédition : quantité effectivement expédiée pour une ligne de commande donnée (résout la cardinalité N:N commande↔expédition).';

-- ---------------------------------------------------------------------
-- shipment_events : timeline des événements de transport (US-LOG-03).
-- ---------------------------------------------------------------------
CREATE TABLE shipment_events (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id   UUID NOT NULL REFERENCES shipments(id) ON DELETE CASCADE, -- CASCADE : événement sans objet si l'expédition disparaît
    event_status  VARCHAR(50) NOT NULL, -- expédié, arrivé au port, en douane, dédouané, livré...
    description   VARCHAR(300),
    event_date    TIMESTAMPTZ NOT NULL DEFAULT now(),
    recorded_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : l'événement reste consultable
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at    TIMESTAMPTZ
);
CREATE INDEX ix_shipment_events_shipment ON shipment_events (shipment_id, event_date);
CREATE TRIGGER trg_shipment_events_updated_at BEFORE UPDATE ON shipment_events
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE shipment_events IS 'Timeline des événements de transport, affichée en frise chronologique (US-LOG-03).';

-- ---------------------------------------------------------------------
-- import_costs : frais logistiques additifs réellement facturés,
-- alloués par expédition (US-LOG-02). Registre comptable complémentaire
-- au moteur de coefficients multiplicatifs (pricing_profiles) — ne
-- pilote PAS le calcul du PRU du lot, cf. [CONTRADICTION] résolue dans
-- les décisions d'arbitrage (méthode multiplicative retenue comme
-- autoritaire car seule vérifiée empiriquement).
-- ---------------------------------------------------------------------
CREATE TABLE import_costs (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id       UUID NOT NULL REFERENCES shipments(id) ON DELETE CASCADE, -- CASCADE : un frais alloué n'a pas de sens sans son expédition
    cost_type         VARCHAR(30) NOT NULL CHECK (cost_type IN (
                           'freight','transit','douane','commission','frais_transfert','assurance','manutention','autre')),
    amount            NUMERIC(14,4) NOT NULL CHECK (amount >= 0),
    currency          VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : traçabilité financière
    allocation_method VARCHAR(20) NOT NULL DEFAULT 'valeur' CHECK (allocation_method IN ('valeur','quantite','volume')),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at        TIMESTAMPTZ
);
CREATE INDEX ix_import_costs_shipment ON import_costs (shipment_id);
CREATE TRIGGER trg_import_costs_updated_at BEFORE UPDATE ON import_costs
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE import_costs IS 'Frais logistiques réels par expédition (freight, transit, douane...), alloués au prorata. Registre comptable — n''alimente pas directement le PRU du lot (cf. décision d''arbitrage).';
-- =====================================================================
-- DOMAINE 5 — ENTREPOSAGE & STOCK
-- Rôle métier : traçabilité par lot, adressage physique de l'entrepôt,
-- mouvements de stock, inventaires. Cœur de la conformité
-- pharmaceutique (FEFO, quarantaine, péremption, rappel de lot).
-- Dépend du Référentiel Commercial (produits) et des Achats (lignes de
-- commande / d'expédition, pour la traçabilité amont du lot).
-- =====================================================================

-- ---------------------------------------------------------------------
-- warehouses : entrepôt physique. Modélisé comme entité de premier
-- niveau bien que LABMEDIS n'en exploite qu'un seul aujourd'hui, pour
-- ne pas bloquer un futur multi-dépôt (ambiguïté non tranchée entre
-- PRD_Qwen-2 §2.19 et les « transferts inter-dépôts » mentionnés
-- ailleurs — cf. décisions d'arbitrage).
-- ---------------------------------------------------------------------
CREATE TABLE warehouses (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code       VARCHAR(20) NOT NULL,
    name       VARCHAR(150) NOT NULL,
    address    VARCHAR(300),
    is_active  BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_warehouses_code ON warehouses (code) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_warehouses_updated_at BEFORE UPDATE ON warehouses
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE warehouses IS 'Entrepôt(s) physique(s) LABMEDIS.';

-- ---------------------------------------------------------------------
-- storage_locations : emplacement d'entrepôt, hiérarchique
-- (zone/allée/rack/niveau — PRD_Qwen-2 §2.3.3/§2.5.1), auto-référencé.
-- ---------------------------------------------------------------------
CREATE TABLE storage_locations (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    warehouse_id      UUID NOT NULL REFERENCES warehouses(id) ON DELETE RESTRICT, -- RESTRICT : jamais d'emplacement orphelin d'entrepôt
    parent_location_id UUID REFERENCES storage_locations(id) ON DELETE RESTRICT, -- RESTRICT : ne pas supprimer un niveau parent tant que des enfants existent (auto-référence, cf. règle 2.4)
    code               VARCHAR(50) NOT NULL,
    name               VARCHAR(150),
    location_type      VARCHAR(20) NOT NULL DEFAULT 'stockage' CHECK (location_type IN (
                            'reception','quarantaine','stockage','picking','reserve',
                            'chaine_froid','perimes','destruction','transit')),
    capacity           INTEGER,
    is_active          BOOLEAN NOT NULL DEFAULT true,
    is_locked          BOOLEAN NOT NULL DEFAULT false,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at         TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_storage_locations_code ON storage_locations (warehouse_id, code) WHERE deleted_at IS NULL;
CREATE INDEX ix_storage_locations_parent ON storage_locations (parent_location_id);
CREATE INDEX ix_storage_locations_type ON storage_locations (location_type);
CREATE TRIGGER trg_storage_locations_updated_at BEFORE UPDATE ON storage_locations
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE storage_locations IS 'Emplacements d''entrepôt, hiérarchie zone→allée→rack→niveau via parent_location_id. Types incluant quarantaine et périmés (obligatoires, PRD_Qwen-2 §2.5.3).';

-- ---------------------------------------------------------------------
-- stock_lots : unité de traçabilité pharmaceutique de base (règle
-- 10.1/10.6). Statuts enrichis de « en_attente_liberation » exigé par
-- PRD_CLAUDE §17.5 (BPD/WHO-GDP), absent de l'énumération initiale de
-- PRD_Qwen-2. unit_cost_cfa = PRU figé au calcul (cascade de
-- coefficients, cf. domaine Pricing). Unicité du n° de lot par couple
-- fournisseur/produit (règle 10.1).
-- ---------------------------------------------------------------------
CREATE TABLE stock_lots (
    id                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id                 UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,               -- RESTRICT : traçabilité pharmaceutique jamais perdue
    supplier_id                 UUID NOT NULL REFERENCES suppliers(id) ON DELETE RESTRICT,               -- RESTRICT : idem — nécessaire à l'unicité (fournisseur, produit, n° de lot)
    purchase_order_line_id       UUID REFERENCES purchase_order_lines(id) ON DELETE RESTRICT,             -- RESTRICT : traçabilité achat ; NULL uniquement pour un stock repris à la migration initiale (§19.3)
    shipment_line_id              UUID REFERENCES shipment_lines(id) ON DELETE RESTRICT,                  -- RESTRICT : traçabilité transport ; NULL pour achat local sans circuit logistique international (is_local)
    pricing_profile_id            UUID REFERENCES pricing_profiles(id) ON DELETE SET NULL,                -- SET NULL : le lot reste valorisé même si le profil est retiré ensuite
    supplier_batch_number         VARCHAR(100) NOT NULL,
    transport_mode                 VARCHAR(20) CHECK (transport_mode IN ('maritime','aerien','express','terrestre')),
    reception_date                  DATE NOT NULL DEFAULT CURRENT_DATE,
    expiry_date                      DATE NOT NULL,
    status                            VARCHAR(30) NOT NULL DEFAULT 'en_reception' CHECK (status IN (
                                          'en_reception','quarantaine','en_attente_liberation','libere',
                                          'non_conforme','perime','detruit')),
    quality_hold_reason                VARCHAR(300), -- motif obligatoire si quarantaine/non_conforme (contrôle applicatif)
    initial_quantity                    INTEGER NOT NULL CHECK (initial_quantity > 0),
    remaining_quantity                  INTEGER NOT NULL CHECK (remaining_quantity >= 0),
    carton_quantity_received             INTEGER, -- cartons réellement comptés à réception (peut différer du standard produit, règle 10.2)
    unit_cost_cfa                        NUMERIC(14,0) NOT NULL, -- PRU du lot, figé (cascade de coefficients)
    reception_discrepancy_reason          VARCHAR(300), -- manquant/excédent/endommagé... (US-REC-02)
    created_at                            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                             TIMESTAMPTZ,
    CHECK (remaining_quantity <= initial_quantity)
);
CREATE UNIQUE INDEX ux_stock_lots_supplier_product_batch ON stock_lots (supplier_id, product_id, supplier_batch_number) WHERE deleted_at IS NULL; -- règle 10.1
CREATE INDEX ix_stock_lots_product_fefo ON stock_lots (product_id, expiry_date) WHERE deleted_at IS NULL AND status = 'libere'; -- allocation FEFO : index couvrant la requête la plus fréquente du système
CREATE INDEX ix_stock_lots_status ON stock_lots (status) WHERE deleted_at IS NULL;
CREATE INDEX ix_stock_lots_expiry ON stock_lots (expiry_date) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_stock_lots_updated_at BEFORE UPDATE ON stock_lots
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE stock_lots IS 'Lot pharmaceutique : unité de traçabilité de base. Statut en_attente_liberation ajouté suite à la recherche réglementaire BPD/WHO-GDP (PRD_CLAUDE §17.5), absent des sources Qwen initiales.';

-- ---------------------------------------------------------------------
-- stock_lot_locations : répartition d'un lot sur un ou plusieurs
-- emplacements (règle : « un même lot peut être stocké à plusieurs
-- emplacements », PRD_Qwen-2 §2.3.3), avec quantité réservée pour
-- gérer la réservation sans décrémenter le stock physique (§2.9.1).
-- ---------------------------------------------------------------------
CREATE TABLE stock_lot_locations (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_lot_id         UUID NOT NULL REFERENCES stock_lots(id) ON DELETE CASCADE,             -- CASCADE : la répartition n'existe qu'à travers son lot
    storage_location_id   UUID NOT NULL REFERENCES storage_locations(id) ON DELETE RESTRICT,     -- RESTRICT : ne jamais perdre la localisation d'un stock existant
    quantity               INTEGER NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    reserved_quantity       INTEGER NOT NULL DEFAULT 0 CHECK (reserved_quantity >= 0),
    created_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                TIMESTAMPTZ,
    CHECK (reserved_quantity <= quantity)
);
CREATE UNIQUE INDEX ux_stock_lot_locations ON stock_lot_locations (stock_lot_id, storage_location_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_stock_lot_locations_location ON stock_lot_locations (storage_location_id);
CREATE TRIGGER trg_stock_lot_locations_updated_at BEFORE UPDATE ON stock_lot_locations
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE stock_lot_locations IS 'Répartition physique d''un lot par emplacement, avec quantité réservée (stock disponible = quantity - reserved_quantity).';

-- ---------------------------------------------------------------------
-- stock_movements : entête de mouvement de stock (entrée/sortie/
-- transfert/ajustement — PRD_Qwen-2 §2.6). source_document_type/id
-- forment une référence polymorphe (commande achat, commande vente,
-- inventaire, retour...) SANS FK stricte, documentée explicitement
-- (règle 2.5).
-- ---------------------------------------------------------------------
CREATE TABLE stock_movements (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference             VARCHAR(50) NOT NULL,
    movement_type          VARCHAR(30) NOT NULL CHECK (movement_type IN (
                                'reception_fournisseur','mise_en_stock','transfert','vente',
                                'retour_client','ajustement_positif','ajustement_negatif',
                                'destruction','perte','echantillon','quarantaine','liberation')),
    movement_date            TIMESTAMPTZ NOT NULL DEFAULT now(),
    user_id                   UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT, -- RESTRICT : un mouvement de stock doit toujours garder son auteur (obligation d'audit pharmaceutique)
    reason                     VARCHAR(300), -- obligatoire pour ajustements/pertes/destructions (règle 2.6.3, contrôle applicatif)
    source_document_type        VARCHAR(50), -- référence polymorphe : 'purchase_order' | 'sale_order' | 'inventory_session' | 'customer_return' ...
    source_document_id           UUID,        -- référence polymorphe : id de la ligne concernée, jamais de FK stricte (règle 2.5)
    status                        VARCHAR(20) NOT NULL DEFAULT 'valide' CHECK (status IN ('brouillon','valide','annule')),
    created_at                    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                     TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_stock_movements_reference ON stock_movements (reference) WHERE deleted_at IS NULL;
CREATE INDEX ix_stock_movements_type_date ON stock_movements (movement_type, movement_date DESC);
CREATE INDEX ix_stock_movements_source ON stock_movements (source_document_type, source_document_id);
CREATE TRIGGER trg_stock_movements_updated_at BEFORE UPDATE ON stock_movements
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE stock_movements IS 'Entête de mouvement de stock. source_document_type/id : référence polymorphe sans FK stricte vers l''origine (achat, vente, inventaire, retour), documentée ici explicitement (règle 2.5).';

-- ---------------------------------------------------------------------
-- stock_movement_lines : lignes de mouvement (produit, lot,
-- emplacements source/destination selon le type — règle 2.6.3).
-- ---------------------------------------------------------------------
CREATE TABLE stock_movement_lines (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_movement_id        UUID NOT NULL REFERENCES stock_movements(id) ON DELETE CASCADE,        -- CASCADE : une ligne n'a pas de sens sans son mouvement
    product_id                UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,               -- RESTRICT : traçabilité jamais perdue
    stock_lot_id                UUID NOT NULL REFERENCES stock_lots(id) ON DELETE RESTRICT,             -- RESTRICT : idem — tout mouvement est rattaché à un lot (règle 2.6.3 point 4)
    source_location_id           UUID REFERENCES storage_locations(id) ON DELETE RESTRICT,              -- RESTRICT : ne jamais perdre la traçabilité spatiale d'un mouvement passé
    destination_location_id       UUID REFERENCES storage_locations(id) ON DELETE RESTRICT,              -- RESTRICT : idem
    quantity                       INTEGER NOT NULL CHECK (quantity > 0),
    created_at                      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                       TIMESTAMPTZ
);
CREATE INDEX ix_stock_movement_lines_movement ON stock_movement_lines (stock_movement_id);
CREATE INDEX ix_stock_movement_lines_lot ON stock_movement_lines (stock_lot_id);
CREATE TRIGGER trg_stock_movement_lines_updated_at BEFORE UPDATE ON stock_movement_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE stock_movement_lines IS 'Ligne de mouvement : produit, lot, emplacements source/destination selon le type de mouvement.';

-- ---------------------------------------------------------------------
-- inventory_sessions : session d'inventaire (PRD_Qwen-2 §2.8).
-- ---------------------------------------------------------------------
CREATE TABLE inventory_sessions (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reference       VARCHAR(50) NOT NULL,
    inventory_type   VARCHAR(30) NOT NULL DEFAULT 'general' CHECK (inventory_type IN (
                          'general','zone','produit','lot','categorie','tournant')),
    status            VARCHAR(20) NOT NULL DEFAULT 'en_cours' CHECK (status IN ('en_cours','validee','cloturee','annulee')),
    warehouse_id       UUID REFERENCES warehouses(id) ON DELETE RESTRICT, -- RESTRICT : conserver la traçabilité de l'entrepôt inventorié
    started_by_user_id  UUID REFERENCES users(id) ON DELETE SET NULL,     -- SET NULL : la session reste consultable
    started_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at              TIMESTAMPTZ,
    comments                VARCHAR(500),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at               TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_inventory_sessions_reference ON inventory_sessions (reference) WHERE deleted_at IS NULL;
CREATE INDEX ix_inventory_sessions_status ON inventory_sessions (status) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_inventory_sessions_updated_at BEFORE UPDATE ON inventory_sessions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE inventory_sessions IS 'Session d''inventaire (général, par zone, produit, lot, catégorie ou tournant).';

-- ---------------------------------------------------------------------
-- inventory_counts : comptage par ligne, écart calculé applicativement
-- (jamais une colonne générée figée sur une comparaison mouvante,
-- règle 2.5 — la comparaison se fait en couche service au moment de la
-- validation, pas en contrainte SQL).
-- ---------------------------------------------------------------------
CREATE TABLE inventory_counts (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_session_id   UUID NOT NULL REFERENCES inventory_sessions(id) ON DELETE CASCADE, -- CASCADE : un comptage n'existe qu'à travers sa session
    product_id              UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,          -- RESTRICT : traçabilité jamais perdue
    stock_lot_id              UUID REFERENCES stock_lots(id) ON DELETE RESTRICT,                 -- RESTRICT : idem ; nullable pour un inventaire par emplacement toutes références confondues
    storage_location_id        UUID NOT NULL REFERENCES storage_locations(id) ON DELETE RESTRICT, -- RESTRICT : traçabilité spatiale jamais perdue
    system_quantity              INTEGER NOT NULL,
    counted_quantity              INTEGER,
    adjustment_reason              VARCHAR(300),
    counted_by_user_id              UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : le comptage reste consultable
    counted_at                       TIMESTAMPTZ,
    created_at                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                         TIMESTAMPTZ
);
CREATE INDEX ix_inventory_counts_session ON inventory_counts (inventory_session_id);
CREATE INDEX ix_inventory_counts_lot ON inventory_counts (stock_lot_id);
CREATE TRIGGER trg_inventory_counts_updated_at BEFORE UPDATE ON inventory_counts
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE inventory_counts IS 'Lignes de comptage d''inventaire. L''écart (counted_quantity - system_quantity) est calculé en couche service, jamais en colonne générée (règle 2.5).';
-- =====================================================================
-- DOMAINE 6 — VENTES & FACTURATION
-- Rôle métier : commandes clients avec allocation FEFO, livraison,
-- facturation et gestion des retours/avoirs. Regroupe Ventes et
-- Facturation dans un seul domaine (flux séquentiel unique dans les
-- sources : PRD_Qwen module 3 workflows 7/9/10/11 s'enchaînent).
-- Dépend du Référentiel Commercial (clients, produits), de
-- l'Entreposage & Stock (lots) et de Pricing & Devises (devises).
-- =====================================================================

-- ---------------------------------------------------------------------
-- customer_product_prices : tarifs négociés par client (US-VEN-05),
-- avec fenêtre de validité ; à défaut, le prix catalogue courant
-- (product_prices) s'applique.
-- ---------------------------------------------------------------------
CREATE TABLE customer_product_prices (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID NOT NULL REFERENCES customers(id) ON DELETE CASCADE, -- CASCADE : un tarif négocié n'a pas de sens sans le client concerné
    product_id       UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,  -- CASCADE : idem côté produit
    unit_price_ht_cfa NUMERIC(14,0) NOT NULL CHECK (unit_price_ht_cfa >= 0),
    valid_from         DATE NOT NULL,
    valid_to            DATE,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at             TIMESTAMPTZ,
    CHECK (valid_to IS NULL OR valid_to >= valid_from)
);
CREATE INDEX ix_customer_product_prices_lookup ON customer_product_prices (customer_id, product_id, valid_from DESC);
CREATE TRIGGER trg_customer_product_prices_updated_at BEFORE UPDATE ON customer_product_prices
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE customer_product_prices IS 'Grille tarifaire négociée par répartiteur/client (US-VEN-05). Prioritaire sur le prix catalogue si une ligne est valide à la date de vente.';

-- ---------------------------------------------------------------------
-- sale_orders : commande client. Statuts consolidés à partir de
-- PRD_Qwen.md §8.8.4 et PRD_Qwen module 3 §3.9.3 (10 statuts).
-- ---------------------------------------------------------------------
CREATE TABLE sale_orders (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number       VARCHAR(50) NOT NULL,
    customer_id         UUID NOT NULL REFERENCES customers(id) ON DELETE RESTRICT, -- RESTRICT : traçabilité commerciale et fiscale jamais perdue
    currency              VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : idem
    status                 VARCHAR(30) NOT NULL DEFAULT 'brouillon' CHECK (status IN (
                                'brouillon','devis','confirmee','reservee','en_preparation',
                                'prete','partiellement_livree','livree','facturee','annulee')),
    order_date              DATE NOT NULL DEFAULT CURRENT_DATE,
    is_exceptional_sale       BOOLEAN NOT NULL DEFAULT false, -- exclusion du calcul de consommation MRP (PRD_Qwen-4 §4.6.5)
    total_ht_cfa               NUMERIC(14,0),
    total_vat_cfa                NUMERIC(14,0),
    total_ttc_cfa                 NUMERIC(14,0),
    created_by_user_id              UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : la commande reste lisible si le créateur quitte le système
    created_at                       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                         TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_sale_orders_number ON sale_orders (order_number) WHERE deleted_at IS NULL;
CREATE INDEX ix_sale_orders_customer ON sale_orders (customer_id);
CREATE INDEX ix_sale_orders_status ON sale_orders (status) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_sale_orders_updated_at BEFORE UPDATE ON sale_orders
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE sale_orders IS 'Commande client (répartiteur, pharmacie, clinique, hôpital, centrale d''achat).';

-- ---------------------------------------------------------------------
-- sale_order_lines : ligne de commande, avec lot alloué (FEFO par
-- défaut, dérogation tracée — règle 10.5).
-- ---------------------------------------------------------------------
CREATE TABLE sale_order_lines (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_order_id             UUID NOT NULL REFERENCES sale_orders(id) ON DELETE CASCADE,  -- CASCADE : une ligne n'a pas de sens sans son entête
    product_id                 UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,    -- RESTRICT : traçabilité commerciale jamais perdue
    allocated_stock_lot_id       UUID REFERENCES stock_lots(id) ON DELETE RESTRICT,           -- RESTRICT : traçabilité FEFO jamais perdue ; nullable tant que non alloué
    quantity                      INTEGER NOT NULL CHECK (quantity > 0),
    unit_price_ht_cfa              NUMERIC(14,0) NOT NULL CHECK (unit_price_ht_cfa >= 0),
    vat_rate                        NUMERIC(5,4) NOT NULL,
    is_fefo_override                 BOOLEAN NOT NULL DEFAULT false, -- dérogation manuelle au lot FEFO proposé (règle 10.5)
    fefo_override_reason               VARCHAR(300),
    created_at                          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                           TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                            TIMESTAMPTZ
);
CREATE INDEX ix_sale_order_lines_so ON sale_order_lines (sale_order_id);
CREATE INDEX ix_sale_order_lines_lot ON sale_order_lines (allocated_stock_lot_id);
CREATE TRIGGER trg_sale_order_lines_updated_at BEFORE UPDATE ON sale_order_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE sale_order_lines IS 'Ligne de commande client : produit, lot alloué (FEFO par défaut), prix et TVA figés à la vente.';

-- ---------------------------------------------------------------------
-- sale_order_status_history : symétrique de purchase_order_status_history.
-- ---------------------------------------------------------------------
CREATE TABLE sale_order_status_history (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_order_id     UUID NOT NULL REFERENCES sale_orders(id) ON DELETE CASCADE, -- CASCADE : historique sans objet si la commande disparaît
    status              VARCHAR(30) NOT NULL,
    comment               VARCHAR(300),
    changed_by_user_id     UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : l'historique reste lisible
    changed_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at                TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                 TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                  TIMESTAMPTZ
);
CREATE INDEX ix_so_status_history_so ON sale_order_status_history (sale_order_id, changed_at);
CREATE TRIGGER trg_so_status_history_updated_at BEFORE UPDATE ON sale_order_status_history
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE sale_order_status_history IS 'Historique horodaté des statuts de commande client.';

-- ---------------------------------------------------------------------
-- deliveries : bon de livraison (US-LIV-01/02). Une commande peut être
-- livrée en plusieurs fois (règle 3.11.3).
-- ---------------------------------------------------------------------
CREATE TABLE deliveries (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_number     VARCHAR(50) NOT NULL,
    sale_order_id         UUID NOT NULL REFERENCES sale_orders(id) ON DELETE RESTRICT, -- RESTRICT : document de livraison jamais orphelin de sa commande
    delivery_date            DATE NOT NULL DEFAULT CURRENT_DATE,
    status                     VARCHAR(20) NOT NULL DEFAULT 'confirmee' CHECK (status IN ('brouillon','confirmee','annulee')),
    delivered_by_user_id         UUID REFERENCES users(id) ON DELETE SET NULL, -- SET NULL : le document reste consultable
    created_at                     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                       TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_deliveries_number ON deliveries (delivery_number) WHERE deleted_at IS NULL;
CREATE INDEX ix_deliveries_sale_order ON deliveries (sale_order_id);
CREATE TRIGGER trg_deliveries_updated_at BEFORE UPDATE ON deliveries
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE deliveries IS 'Bon de livraison. Déclenche la sortie physique de stock (stock_movements type=vente) à la confirmation.';

-- ---------------------------------------------------------------------
-- delivery_lines : ligne livrée = point où le stock est physiquement
-- décrémenté (via un stock_movement créé par le service applicatif).
-- ---------------------------------------------------------------------
CREATE TABLE delivery_lines (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    delivery_id            UUID NOT NULL REFERENCES deliveries(id) ON DELETE CASCADE,         -- CASCADE : une ligne n'a pas de sens sans son bon de livraison
    sale_order_line_id       UUID NOT NULL REFERENCES sale_order_lines(id) ON DELETE RESTRICT,  -- RESTRICT : traçabilité commande↔livraison jamais perdue
    stock_lot_id                UUID NOT NULL REFERENCES stock_lots(id) ON DELETE RESTRICT,       -- RESTRICT : traçabilité pharmaceutique jamais perdue
    quantity_delivered              INTEGER NOT NULL CHECK (quantity_delivered > 0),
    created_at                       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                         TIMESTAMPTZ
);
CREATE INDEX ix_delivery_lines_delivery ON delivery_lines (delivery_id);
CREATE INDEX ix_delivery_lines_lot ON delivery_lines (stock_lot_id);
CREATE TRIGGER trg_delivery_lines_updated_at BEFORE UPDATE ON delivery_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE delivery_lines IS 'Ligne livrée : produit/lot/quantité effectivement remis au client — trace le lot jusqu''au client (règle de rappel, PRD_CLAUDE §5.12.4).';

-- ---------------------------------------------------------------------
-- invoices : facture client (US-FAC-01), numérotation unique.
-- ---------------------------------------------------------------------
CREATE TABLE invoices (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_number     VARCHAR(50) NOT NULL,
    customer_id           UUID NOT NULL REFERENCES customers(id) ON DELETE RESTRICT, -- RESTRICT : document fiscal jamais orphelin
    sale_order_id           UUID REFERENCES sale_orders(id) ON DELETE RESTRICT,        -- RESTRICT : traçabilité jamais perdue ; nullable si facturation groupée future
    currency                  VARCHAR(3) NOT NULL REFERENCES currencies(code) ON DELETE RESTRICT, -- RESTRICT : traçabilité financière
    status                     VARCHAR(20) NOT NULL DEFAULT 'emise' CHECK (status IN ('emise','payee','annulee')),
    invoice_date                 DATE NOT NULL DEFAULT CURRENT_DATE,
    total_ht_cfa                    NUMERIC(14,0) NOT NULL,
    total_vat_cfa                     NUMERIC(14,0) NOT NULL,
    total_ttc_cfa                      NUMERIC(14,0) NOT NULL,
    created_at                          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                           TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                            TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_invoices_number ON invoices (invoice_number) WHERE deleted_at IS NULL;
CREATE INDEX ix_invoices_customer ON invoices (customer_id);
CREATE INDEX ix_invoices_date ON invoices (invoice_date);
CREATE TRIGGER trg_invoices_updated_at BEFORE UPDATE ON invoices
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE invoices IS 'Facture client, numérotation unique (règle 3.12.3).';

-- ---------------------------------------------------------------------
-- invoice_lines : ligne de facture — montants figés en instantané
-- (jamais recalculés après coup, règle 2.5).
-- ---------------------------------------------------------------------
CREATE TABLE invoice_lines (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id         UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,        -- CASCADE : une ligne n'a pas de sens sans sa facture
    product_id           UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,      -- RESTRICT : traçabilité fiscale jamais perdue
    stock_lot_id            UUID REFERENCES stock_lots(id) ON DELETE RESTRICT,            -- RESTRICT : traçabilité pharmaceutique (n° de lot en pied de facture, PRD_CLAUDE §18.2)
    quantity                  INTEGER NOT NULL CHECK (quantity > 0),
    unit_price_ht_cfa           NUMERIC(14,0) NOT NULL CHECK (unit_price_ht_cfa >= 0),
    vat_rate                     NUMERIC(5,4) NOT NULL,
    line_total_ht_cfa              NUMERIC(14,0) NOT NULL,
    line_total_ttc_cfa               NUMERIC(14,0) NOT NULL,
    created_at                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                         TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                          TIMESTAMPTZ
);
CREATE INDEX ix_invoice_lines_invoice ON invoice_lines (invoice_id);
CREATE TRIGGER trg_invoice_lines_updated_at BEFORE UPDATE ON invoice_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE invoice_lines IS 'Ligne de facture. N° de lot conservé pour figurer sur le document légal (traçabilité, PRD_CLAUDE §18.2).';

-- ---------------------------------------------------------------------
-- credit_notes : avoirs (US-FAC-02), liés à une facture et/ou un retour.
-- ---------------------------------------------------------------------
CREATE TABLE credit_notes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    credit_note_number    VARCHAR(50) NOT NULL,
    invoice_id               UUID REFERENCES invoices(id) ON DELETE RESTRICT, -- RESTRICT : traçabilité fiscale jamais perdue
    customer_id                UUID NOT NULL REFERENCES customers(id) ON DELETE RESTRICT, -- RESTRICT : idem
    reason                       VARCHAR(300) NOT NULL,
    total_ht_cfa                   NUMERIC(14,0) NOT NULL,
    total_vat_cfa                    NUMERIC(14,0) NOT NULL,
    total_ttc_cfa                      NUMERIC(14,0) NOT NULL,
    issued_at                            DATE NOT NULL DEFAULT CURRENT_DATE,
    created_at                             TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                              TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                               TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_credit_notes_number ON credit_notes (credit_note_number) WHERE deleted_at IS NULL;
CREATE INDEX ix_credit_notes_invoice ON credit_notes (invoice_id);
CREATE TRIGGER trg_credit_notes_updated_at BEFORE UPDATE ON credit_notes
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE credit_notes IS 'Avoir (note de crédit), total ou partiel, lié à une facture (US-FAC-02).';

CREATE TABLE credit_note_lines (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    credit_note_id      UUID NOT NULL REFERENCES credit_notes(id) ON DELETE CASCADE,  -- CASCADE : une ligne n'a pas de sens sans son avoir
    invoice_line_id        UUID REFERENCES invoice_lines(id) ON DELETE RESTRICT,        -- RESTRICT : traçabilité fiscale jamais perdue
    product_id                UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,  -- RESTRICT : idem
    quantity                    INTEGER NOT NULL CHECK (quantity > 0),
    unit_price_ht_cfa             NUMERIC(14,0) NOT NULL CHECK (unit_price_ht_cfa >= 0),
    vat_rate                       NUMERIC(5,4) NOT NULL,
    line_total_ttc_cfa                NUMERIC(14,0) NOT NULL,
    created_at                          TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                           TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                            TIMESTAMPTZ
);
CREATE INDEX ix_credit_note_lines_cn ON credit_note_lines (credit_note_id);
CREATE TRIGGER trg_credit_note_lines_updated_at BEFORE UPDATE ON credit_note_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE credit_note_lines IS 'Ligne d''avoir.';

-- ---------------------------------------------------------------------
-- customer_returns : entête de retour client (workflow 11 / règle
-- 20.3), décision explicite parmi 4 issues. Restructurée en entête +
-- lignes lors de la revue de complétude : US-RET-01 exige explicitement
-- « l'utilisateur sélectionne LES PRODUITS retournés » (pluriel), donc
-- un retour peut couvrir plusieurs lignes de commande/lots — la version
-- initiale (une seule ligne portée par l'entête) ne le permettait pas.
-- ---------------------------------------------------------------------
CREATE TABLE customer_returns (
    id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    return_number             VARCHAR(50) NOT NULL,
    customer_id                 UUID NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,   -- RESTRICT : traçabilité commerciale jamais perdue
    sale_order_id                 UUID NOT NULL REFERENCES sale_orders(id) ON DELETE RESTRICT, -- RESTRICT : le retour doit toujours référencer la commande d'origine (règle 20.3.1)
    reason                           VARCHAR(300) NOT NULL,
    decision                           VARCHAR(20) NOT NULL DEFAULT 'en_attente' CHECK (decision IN (
                                            'en_attente','remise_stock','quarantaine','destruction','refus')),
    credit_note_id                      UUID REFERENCES credit_notes(id) ON DELETE SET NULL, -- SET NULL : le retour reste consultable même si l'avoir est retiré
    decided_by_user_id                    UUID REFERENCES users(id) ON DELETE SET NULL,        -- SET NULL : le retour reste consultable
    decided_at                              TIMESTAMPTZ,
    created_at                                TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                 TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                  TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_customer_returns_number ON customer_returns (return_number) WHERE deleted_at IS NULL;
CREATE INDEX ix_customer_returns_customer ON customer_returns (customer_id);
CREATE INDEX ix_customer_returns_sale_order ON customer_returns (sale_order_id);
CREATE TRIGGER trg_customer_returns_updated_at BEFORE UPDATE ON customer_returns
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE customer_returns IS 'Entête de retour client, une ou plusieurs lignes (return_lines). Une décision "remise_stock" déclenche un stock_movement type=retour_client par ligne (référence polymorphe source_document_type=customer_return).';

-- ---------------------------------------------------------------------
-- return_lines : produits/lots effectivement retournés dans un retour
-- (US-RET-01 : sélection de plusieurs produits par retour).
-- ---------------------------------------------------------------------
CREATE TABLE return_lines (
    id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_return_id       UUID NOT NULL REFERENCES customer_returns(id) ON DELETE CASCADE,      -- CASCADE : une ligne n'a pas de sens sans son entête de retour
    sale_order_line_id         UUID NOT NULL REFERENCES sale_order_lines(id) ON DELETE RESTRICT,     -- RESTRICT : traçabilité vente↔retour jamais perdue
    original_stock_lot_id        UUID REFERENCES stock_lots(id) ON DELETE RESTRICT,                    -- RESTRICT : traçabilité pharmaceutique du lot d'origine
    quantity                       INTEGER NOT NULL CHECK (quantity > 0),
    created_at                       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                         TIMESTAMPTZ
);
CREATE INDEX ix_return_lines_return ON return_lines (customer_return_id);
CREATE INDEX ix_return_lines_lot ON return_lines (original_stock_lot_id);
CREATE TRIGGER trg_return_lines_updated_at BEFORE UPDATE ON return_lines
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE return_lines IS 'Ligne de retour : produit/lot/quantité retournés, rattachés à la ligne de vente d''origine.';
-- =====================================================================
-- DOMAINE 7 — PRÉVISION & RÉAPPROVISIONNEMENT (MRP)
-- Rôle métier : anticiper les ruptures de stock à partir des délais de
-- fabrication/transport/transit et de la consommation historique, puis
-- déclencher des suggestions de commande (PRD_Qwen-4, entièrement
-- repris ici — c'est le module le plus rigoureusement chiffré des
-- sources). Domaine intentionnellement compact (4 tables) : chaque
-- table correspond exactement à une entité déjà définie et exécutable
-- dans PRD_Qwen-4 §4.16.1 — aucune table ajoutée par convenance.
-- Dépend du Référentiel Commercial (produits, fournisseurs) et des
-- Achats (purchase_orders, pour la conversion d'une suggestion).
-- La consommation historique n'est PAS dupliquée ici : elle est lue
-- depuis daily_sales_summary (domaine Reporting) — cf. décision
-- d'arbitrage « pas de table de consommation redondante ».
-- =====================================================================

-- ---------------------------------------------------------------------
-- forecast_parameters : paramétrage MRP par produit (PRD_Qwen-4
-- §4.16.1 ForecastParameter). Un coefficient de saisonnalité unique
-- par produit est retenu (pas de courbe mensuelle) — simplification
-- documentée en Décisions d'arbitrage.
-- ---------------------------------------------------------------------
CREATE TABLE forecast_parameters (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id                 UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- CASCADE : paramétrage sans objet si le produit disparaît
    is_enabled                   BOOLEAN NOT NULL DEFAULT true,
    forecast_horizon_days           INTEGER NOT NULL DEFAULT 180, -- horizon par défaut recommandé (PRD_Qwen-4 §4.3.2)
    safety_stock                     INTEGER NOT NULL DEFAULT 0,
    target_coverage_days               INTEGER NOT NULL DEFAULT 120,
    overstock_threshold_days             INTEGER NOT NULL DEFAULT 60,
    consumption_method                     VARCHAR(20) NOT NULL DEFAULT 'moyenne_90j' CHECK (consumption_method IN (
                                                'moyenne_30j','moyenne_60j','moyenne_90j','ponderee_90j','saisonniere_365j','manuel')),
    seasonality_factor                       NUMERIC(6,3), -- coefficient unique (PRD_Qwen-4 §4.16.1) ; V2 : profil mensuel (cf. recommandations)
    created_at                                 TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                   TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_forecast_parameters_product ON forecast_parameters (product_id) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_forecast_parameters_updated_at BEFORE UPDATE ON forecast_parameters
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE forecast_parameters IS 'Paramétrage du moteur MRP par produit (stock de sécurité, couverture cible, méthode de calcul).';

-- ---------------------------------------------------------------------
-- supplier_lead_times : délais par fournisseur, éventuellement
-- surchargés par produit et par mode de transport (PRD_Qwen-4 §4.7.1).
-- ---------------------------------------------------------------------
CREATE TABLE supplier_lead_times (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    supplier_id                   UUID NOT NULL REFERENCES suppliers(id) ON DELETE CASCADE, -- CASCADE : paramétrage sans objet si le fournisseur disparaît
    product_id                     UUID REFERENCES products(id) ON DELETE CASCADE,            -- CASCADE : idem ; NULL = délai par défaut du fournisseur, tout produit confondu
    transport_mode                   VARCHAR(20) NOT NULL CHECK (transport_mode IN ('maritime','aerien','express','terrestre')),
    manufacturing_lead_time_days       INTEGER NOT NULL DEFAULT 0,
    preparation_lead_time_days           INTEGER NOT NULL DEFAULT 0,
    transport_lead_time_days               INTEGER NOT NULL DEFAULT 0,
    customs_lead_time_days                   INTEGER NOT NULL DEFAULT 0,
    internal_lead_time_days                    INTEGER NOT NULL DEFAULT 5, -- valeur par défaut recommandée (PRD_Qwen-4 §4.7.3)
    created_at                                   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                    TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                     TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_supplier_lead_times ON supplier_lead_times (supplier_id, COALESCE(product_id, '00000000-0000-0000-0000-000000000000'::uuid), transport_mode) WHERE deleted_at IS NULL;
CREATE INDEX ix_supplier_lead_times_product ON supplier_lead_times (product_id);
CREATE TRIGGER trg_supplier_lead_times_updated_at BEFORE UPDATE ON supplier_lead_times
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE supplier_lead_times IS 'Délais de réapprovisionnement par fournisseur, éventuellement surchargés par produit et par mode de transport (PRD_Qwen-4 §4.7).';

-- ---------------------------------------------------------------------
-- forecast_calculations : instantané d'un calcul MRP pour un produit
-- (table intrinsèquement append-only : chaque exécution du job
-- quotidien crée une nouvelle ligne datée — sert aussi d'historique,
-- pas besoin de table séparée).
-- ---------------------------------------------------------------------
CREATE TABLE forecast_calculations (
    id                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id                  UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- CASCADE : un calcul sans objet si le produit disparaît
    calculation_date               DATE NOT NULL DEFAULT CURRENT_DATE,
    available_stock                  INTEGER NOT NULL,
    reserved_stock                     INTEGER NOT NULL DEFAULT 0,
    transit_stock                        INTEGER NOT NULL DEFAULT 0,
    average_daily_consumption              NUMERIC(10,3) NOT NULL DEFAULT 0,
    lead_time_days                           INTEGER NOT NULL,
    safety_stock                               INTEGER NOT NULL DEFAULT 0,
    reorder_point                                INTEGER NOT NULL,
    target_stock                                   INTEGER NOT NULL,
    net_requirement                                  INTEGER NOT NULL,
    coverage_days                                      INTEGER, -- NULL si consommation moyenne = 0 (division impossible, cf. §4.4.1)
    risk_level                                           VARCHAR(20) NOT NULL CHECK (risk_level IN (
                                                              'normal','a_surveiller','urgent','critique','surstock')),
    created_at                                             TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                              TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                               TIMESTAMPTZ
);
CREATE INDEX ix_forecast_calculations_product_date ON forecast_calculations (product_id, calculation_date DESC);
CREATE INDEX ix_forecast_calculations_risk ON forecast_calculations (risk_level, calculation_date DESC) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_forecast_calculations_updated_at BEFORE UPDATE ON forecast_calculations
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE forecast_calculations IS 'Instantané quotidien du calcul MRP par produit (Job Hangfire DailyMrpCalculationJob). Append-only : sert aussi d''historique des prévisions.';

-- ---------------------------------------------------------------------
-- reorder_suggestions : suggestion de commande générée à partir d'un
-- calcul MRP, convertible en commande fournisseur (PRD_Qwen-4 §4.13).
-- ---------------------------------------------------------------------
CREATE TABLE reorder_suggestions (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    forecast_calculation_id       UUID NOT NULL REFERENCES forecast_calculations(id) ON DELETE RESTRICT, -- RESTRICT : la justification du calcul doit rester consultable tant que la suggestion existe
    product_id                      UUID NOT NULL REFERENCES products(id) ON DELETE RESTRICT,              -- RESTRICT : traçabilité jamais perdue
    supplier_id                       UUID NOT NULL REFERENCES suppliers(id) ON DELETE RESTRICT,             -- RESTRICT : idem
    suggested_quantity_units             INTEGER NOT NULL CHECK (suggested_quantity_units > 0),
    suggested_quantity_cartons             INTEGER,
    suggested_transport_mode                 VARCHAR(20) CHECK (suggested_transport_mode IN ('maritime','aerien','express','terrestre')),
    suggested_order_date                       DATE NOT NULL,
    estimated_reception_date                     DATE,
    status                                         VARCHAR(20) NOT NULL DEFAULT 'en_attente' CHECK (status IN (
                                                        'en_attente','validee','convertie','rejetee','expiree')),
    rejection_reason                                 VARCHAR(300),
    purchase_order_id                                  UUID REFERENCES purchase_orders(id) ON DELETE SET NULL, -- SET NULL : la suggestion reste consultable même si la commande issue est ensuite retirée
    created_at                                           TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                            TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                             TIMESTAMPTZ
);
CREATE INDEX ix_reorder_suggestions_product ON reorder_suggestions (product_id);
CREATE INDEX ix_reorder_suggestions_status ON reorder_suggestions (status) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_reorder_suggestions_updated_at BEFORE UPDATE ON reorder_suggestions
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE reorder_suggestions IS 'Suggestion de réapprovisionnement issue du moteur MRP, convertible en commande fournisseur (US-MRP-08).';
-- =====================================================================
-- DOMAINE 8 — REPORTING, AGRÉGATIONS & NOTIFICATIONS
-- Rôle métier : tables d'agrégation pré-calculées pour les tableaux de
-- bord (évite de scanner l'intégralité du grand livre des ventes/stock
-- à chaque affichage — PRD_Qwen-6 §6.24) et notifications temps réel
-- persistées (SignalR pousse l'événement, cette table porte l'état
-- lu/non-lu — PRD_Qwen-5 §5.19.3 : « si l'utilisateur est hors ligne,
-- il retrouve ses notifications à la connexion »). Dépend du
-- Référentiel Commercial (produits, catégories, clients, fournisseurs)
-- et de la Sécurité (destinataire).
-- =====================================================================

-- ---------------------------------------------------------------------
-- daily_sales_summary : synthèse quotidienne des ventes (PRD_Qwen-6
-- §6.24.1). Alimentée par DailySalesSummaryJob. Sert aussi de source
-- de consommation historique au moteur MRP (domaine 7), évitant une
-- table de consommation redondante.
-- ---------------------------------------------------------------------
CREATE TABLE daily_sales_summary (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sales_date          DATE NOT NULL,
    customer_id            UUID REFERENCES customers(id) ON DELETE CASCADE, -- CASCADE : agrégat sans objet si le client disparaît (rare, mais cohérent avec la nature dérivée de cette table)
    product_id                UUID REFERENCES products(id) ON DELETE CASCADE,  -- CASCADE : idem côté produit
    category_id                  UUID REFERENCES categories(id) ON DELETE CASCADE, -- CASCADE : idem côté catégorie (dénormalisé pour accélérer les agrégations par catégorie)
    quantity_sold                   INTEGER NOT NULL DEFAULT 0,
    total_amount_ht_cfa                NUMERIC(14,0) NOT NULL DEFAULT 0,
    total_vat_cfa                         NUMERIC(14,0) NOT NULL DEFAULT 0,
    total_amount_ttc_cfa                     NUMERIC(14,0) NOT NULL DEFAULT 0,
    total_cost_cfa                              NUMERIC(14,0) NOT NULL DEFAULT 0,
    gross_margin_cfa                               NUMERIC(14,0) NOT NULL DEFAULT 0,
    created_at                                       TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                         TIMESTAMPTZ
);
CREATE INDEX ix_daily_sales_summary_date ON daily_sales_summary (sales_date DESC);
CREATE INDEX ix_daily_sales_summary_product_date ON daily_sales_summary (product_id, sales_date DESC);
CREATE INDEX ix_daily_sales_summary_customer ON daily_sales_summary (customer_id);
CREATE TRIGGER trg_daily_sales_summary_updated_at BEFORE UPDATE ON daily_sales_summary
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE daily_sales_summary IS 'Agrégat quotidien des ventes par client/produit/catégorie. Alimente les dashboards ET la consommation historique du moteur MRP (domaine 7).';

-- ---------------------------------------------------------------------
-- daily_stock_summary : synthèse quotidienne du stock (PRD_Qwen-6
-- §6.24.1 DailyStockSummary).
-- ---------------------------------------------------------------------
CREATE TABLE daily_stock_summary (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    summary_date         DATE NOT NULL,
    product_id              UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- CASCADE : agrégat sans objet si le produit disparaît
    category_id                UUID REFERENCES categories(id) ON DELETE CASCADE,       -- CASCADE : idem, dénormalisé pour accélérer les agrégations
    supplier_id                   UUID REFERENCES suppliers(id) ON DELETE CASCADE,       -- CASCADE : idem
    physical_stock                   INTEGER NOT NULL DEFAULT 0,
    reserved_stock                      INTEGER NOT NULL DEFAULT 0,
    available_stock                        INTEGER NOT NULL DEFAULT 0,
    quarantine_stock                          INTEGER NOT NULL DEFAULT 0,
    expired_stock                                INTEGER NOT NULL DEFAULT 0,
    stock_value_cfa                                 NUMERIC(16,0) NOT NULL DEFAULT 0,
    created_at                                        TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                         TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                          TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_daily_stock_summary ON daily_stock_summary (summary_date, product_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_daily_stock_summary_date ON daily_stock_summary (summary_date DESC);
CREATE TRIGGER trg_daily_stock_summary_updated_at BEFORE UPDATE ON daily_stock_summary
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE daily_stock_summary IS 'Agrégat quotidien du stock (physique, réservé, disponible, quarantaine, périmé, valorisé) par produit.';

-- ---------------------------------------------------------------------
-- daily_forecast_summary : synthèse quotidienne MRP (PRD_Qwen-6
-- §6.24.1 DailyForecastSummary) — vue dashboard, distincte du détail
-- de calcul déjà historisé dans forecast_calculations (domaine 7).
-- ---------------------------------------------------------------------
CREATE TABLE daily_forecast_summary (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    summary_date         DATE NOT NULL,
    product_id              UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE, -- CASCADE : agrégat sans objet si le produit disparaît
    available_stock            INTEGER NOT NULL DEFAULT 0,
    transit_stock                  INTEGER NOT NULL DEFAULT 0,
    average_daily_consumption         NUMERIC(10,3) NOT NULL DEFAULT 0,
    coverage_days                        INTEGER,
    reorder_point                           INTEGER NOT NULL DEFAULT 0,
    net_requirement                            INTEGER NOT NULL DEFAULT 0,
    risk_level                                    VARCHAR(20) NOT NULL CHECK (risk_level IN (
                                                       'normal','a_surveiller','urgent','critique','surstock')),
    created_at                                      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                       TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                        TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_daily_forecast_summary ON daily_forecast_summary (summary_date, product_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_daily_forecast_summary_risk ON daily_forecast_summary (risk_level, summary_date DESC);
CREATE TRIGGER trg_daily_forecast_summary_updated_at BEFORE UPDATE ON daily_forecast_summary
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE daily_forecast_summary IS 'Vue quotidienne condensée du risque de rupture par produit, pour le dashboard MRP (distincte du détail de calcul en domaine 7).';

-- ---------------------------------------------------------------------
-- monthly_financial_summary : synthèse financière mensuelle
-- (PRD_Qwen-6 §6.24.1 MonthlyFinancialSummary).
-- ---------------------------------------------------------------------
CREATE TABLE monthly_financial_summary (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    summary_year         INTEGER NOT NULL,
    summary_month           SMALLINT NOT NULL CHECK (summary_month BETWEEN 1 AND 12),
    total_sales_ht_cfa         NUMERIC(16,0) NOT NULL DEFAULT 0,
    total_vat_cfa                 NUMERIC(16,0) NOT NULL DEFAULT 0,
    total_sales_ttc_cfa              NUMERIC(16,0) NOT NULL DEFAULT 0,
    total_cost_of_goods_sold_cfa        NUMERIC(16,0) NOT NULL DEFAULT 0,
    gross_margin_cfa                       NUMERIC(16,0) NOT NULL DEFAULT 0,
    purchase_amount_cfa                       NUMERIC(16,0) NOT NULL DEFAULT 0,
    logistics_cost_cfa                           NUMERIC(16,0) NOT NULL DEFAULT 0,
    created_at                                     TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                       TIMESTAMPTZ
);
CREATE UNIQUE INDEX ux_monthly_financial_summary ON monthly_financial_summary (summary_year, summary_month) WHERE deleted_at IS NULL;
CREATE TRIGGER trg_monthly_financial_summary_updated_at BEFORE UPDATE ON monthly_financial_summary
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE monthly_financial_summary IS 'Synthèse financière mensuelle (CA, TVA, coût des ventes, marge, achats, coûts logistiques) — Job MonthlyFinancialSummaryJob.';

-- ---------------------------------------------------------------------
-- notifications : notifications persistées, poussées en temps réel via
-- SignalR mais toujours stockées (règle 5.19.3 : retrouvables hors
-- ligne). Référence polymorphe vers la source, comme audit_logs.
-- ---------------------------------------------------------------------
CREATE TABLE notifications (
    id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    notification_type        VARCHAR(50) NOT NULL, -- ex. 'LowStockAlert', 'ExpiringLotDetected', 'ReorderSuggestionCreated'...
    recipient_user_id           UUID REFERENCES users(id) ON DELETE CASCADE,  -- CASCADE : une notification personnelle n'a pas de sens sans son destinataire
    recipient_role_id             UUID REFERENCES roles(id) ON DELETE CASCADE, -- CASCADE : idem pour une diffusion par rôle ; l'un des deux au moins doit être renseigné (contrôle applicatif)
    channel                         VARCHAR(20) NOT NULL DEFAULT 'signalr' CHECK (channel IN ('signalr','email','sms')),
    title                             VARCHAR(200) NOT NULL,
    message                            VARCHAR(500),
    source_document_type                 VARCHAR(50), -- référence polymorphe (règle 2.5), ex. 'stock_lots', 'reorder_suggestions'
    source_document_id                     UUID,        -- référence polymorphe, jamais de FK stricte
    is_read                                  BOOLEAN NOT NULL DEFAULT false,
    read_at                                    TIMESTAMPTZ,
    created_at                                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at                                   TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at                                    TIMESTAMPTZ
);
CREATE INDEX ix_notifications_recipient_user ON notifications (recipient_user_id, is_read) WHERE deleted_at IS NULL;
CREATE INDEX ix_notifications_recipient_role ON notifications (recipient_role_id, is_read) WHERE deleted_at IS NULL;
CREATE INDEX ix_notifications_source ON notifications (source_document_type, source_document_id);
CREATE TRIGGER trg_notifications_updated_at BEFORE UPDATE ON notifications
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
COMMENT ON TABLE notifications IS 'Notifications persistées (poussées en direct via SignalR, retrouvables hors ligne — règle 5.19.3). is_read/read_at s''appliquent uniquement aux notifications personnelles (recipient_user_id renseigné) ; pour une diffusion par rôle (recipient_role_id), l''état lu/non-lu par utilisateur vit dans notification_reads (cf. ci-dessous).';

-- ---------------------------------------------------------------------
-- notification_reads : état lu/non-lu PAR UTILISATEUR pour les
-- notifications diffusées à un rôle entier. Ajoutée lors de la revue de
-- complétude : notifications.is_read est une colonne unique par ligne ;
-- pour une notification de rôle (ex. « Suggestion de réappro urgente »
-- envoyée à tout le rôle Achats), marquer is_read=true la ferait
-- disparaître pour TOUS les utilisateurs du rôle, pas seulement celui
-- qui l'a lue — contraire à PRD_Qwen-5 §5.19.3/§5.27.1 (notification
-- filtrée par rôle, mais chaque destinataire garde son propre état).
-- ---------------------------------------------------------------------
CREATE TABLE notification_reads (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    notification_id   UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE, -- CASCADE : un état de lecture n'a pas de sens sans la notification
    user_id             UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,         -- CASCADE : idem côté utilisateur
    read_at               TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX ux_notification_reads ON notification_reads (notification_id, user_id);
COMMENT ON TABLE notification_reads IS 'État lu/non-lu par utilisateur pour une notification diffusée à un rôle (notifications.recipient_role_id). Une notification personnelle (recipient_user_id) utilise directement notifications.is_read/read_at.';
