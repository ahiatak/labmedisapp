-- =====================================================================
-- SCÉNARIO MÉTIER DE BOUT EN BOUT — Phase 3.3
-- Utilise des UUID fixes et lisibles pour permettre les références
-- croisées explicites entre INSERT (pas de dépendance à \gset).
-- Données volontairement alignées sur les valeurs RÉELLES vérifiées en
-- Phase 1 (Structure_de_prix.xlsx, gamme France Lait) pour permettre
-- une vérification directe des montants calculés.
-- =====================================================================

-- ---- Référentiel devises ----
INSERT INTO currencies (code, name, symbol, decimal_places) VALUES
    ('EUR','Euro','€',2),
    ('USD','Dollar américain','$',2),
    ('XOF','Franc CFA (UEMOA)','CFA',0);

INSERT INTO exchange_rates (id, from_currency, to_currency, rate, effective_date, source) VALUES
    ('10000000-0000-0000-0000-000000000001','EUR','XOF',655.957,'2026-01-01','manuel'),
    ('10000000-0000-0000-0000-000000000002','USD','XOF',590.500,'2026-08-01','manuel');

-- ---- Sécurité : rôles, permissions, utilisateurs ----
INSERT INTO roles (id, code, name, description, is_system) VALUES
    ('20000000-0000-0000-0000-000000000001','ADMIN','Administrateur','Accès total technique et fonctionnel', true),
    ('20000000-0000-0000-0000-000000000002','DIRECTION','Direction','Pilotage, validation, marges, reporting', false),
    ('20000000-0000-0000-0000-000000000003','ACHATS','Responsable achats','Commandes fournisseurs, prévisions, MRP', false),
    ('20000000-0000-0000-0000-000000000004','MAGASINIER','Magasinier','Réception, mise en stock, inventaire', false),
    ('20000000-0000-0000-0000-000000000005','QUALITE','Responsable qualité','Quarantaine, libération de lot', false),
    ('20000000-0000-0000-0000-000000000006','COMMERCIAL','Commercial','Commandes clients, devis, disponibilité', false),
    ('20000000-0000-0000-0000-000000000007','COMPTABLE','Comptable','Factures, avoirs, TVA, exports', false);

INSERT INTO permissions (id, code, module, name) VALUES
    ('21000000-0000-0000-0000-000000000001','Pricing.Approve','Pricing','Valider un prix de vente'),
    ('21000000-0000-0000-0000-000000000002','Pricing.Read','Pricing','Consulter les prix'),
    ('21000000-0000-0000-0000-000000000003','StockLots.Release','Lots','Libérer un lot en quarantaine'),
    ('21000000-0000-0000-0000-000000000004','Purchases.Validate','Achats','Valider une commande fournisseur'),
    ('21000000-0000-0000-0000-000000000005','Sales.Create','Ventes','Créer une commande client');

-- Seule la Direction peut approuver un prix (US-PRICE-02 / matrice §5.5.4)
INSERT INTO role_permissions (role_id, permission_id) VALUES
    ('20000000-0000-0000-0000-000000000002','21000000-0000-0000-0000-000000000001'),
    ('20000000-0000-0000-0000-000000000002','21000000-0000-0000-0000-000000000002'),
    ('20000000-0000-0000-0000-000000000003','21000000-0000-0000-0000-000000000002'),
    ('20000000-0000-0000-0000-000000000003','21000000-0000-0000-0000-000000000004'),
    ('20000000-0000-0000-0000-000000000005','21000000-0000-0000-0000-000000000003'),
    ('20000000-0000-0000-0000-000000000006','21000000-0000-0000-0000-000000000005');

-- Utilisateur multi-rôles : Kokou AMEGAN cumule Responsable Achats ET Direction
-- (structure réduite typique d'une PME togolaise — cf. scénario exigé en 3.3)
INSERT INTO users (id, email, password_hash, first_name, last_name, is_active) VALUES
    ('30000000-0000-0000-0000-000000000001','k.amegan@labmedis.tg','$2a$bcrypt_hash_placeholder_1','Kokou','Amegan', true),
    ('30000000-0000-0000-0000-000000000002','a.mensah@labmedis.tg','$2a$bcrypt_hash_placeholder_2','Ama','Mensah', true),
    ('30000000-0000-0000-0000-000000000003','e.tossou@labmedis.tg','$2a$bcrypt_hash_placeholder_3','Essi','Tossou', true);

INSERT INTO user_roles (user_id, role_id) VALUES
    ('30000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000003'), -- Kokou = Achats
    ('30000000-0000-0000-0000-000000000001','20000000-0000-0000-0000-000000000002'), -- Kokou = Direction (multi-rôle)
    ('30000000-0000-0000-0000-000000000002','20000000-0000-0000-0000-000000000004'), -- Ama = Magasinier
    ('30000000-0000-0000-0000-000000000003','20000000-0000-0000-0000-000000000006'); -- Essi = Commercial

INSERT INTO company_profile (company_name, address, depositary_license_number, depositary_license_issued_at, depositary_license_expires_at) VALUES
    ('LABMEDIS SARL','Zone portuaire, Lomé, Togo','DPML-TG-2023-0147','2023-04-10','2028-04-10');

-- ---- Référentiel commercial ----
INSERT INTO categories (id, code, name, default_vat_rate, expiry_alert_days) VALUES
    ('40000000-0000-0000-0000-000000000001','INFANTILE','Produit infantile',0.1800,120),
    ('40000000-0000-0000-0000-000000000002','MEDICAMENT','Médicament',NULL,90),
    ('40000000-0000-0000-0000-000000000003','REACTIF','Réactif de laboratoire',NULL,60);

INSERT INTO therapeutic_classes (id, name) VALUES
    ('41000000-0000-0000-0000-000000000001','Lait infantile');

INSERT INTO suppliers (id, name, address, phone, country, default_currency, is_local, distribution_authorization_verified) VALUES
    ('42000000-0000-0000-0000-000000000001','Continental Commodities','174 Bd Haussmann, Paris','+33 1 40 13 71 17','France','EUR', false, true);

INSERT INTO customers (id, name, customer_type, address, phone, city, payment_term_days, credit_limit_cfa, license_verified) VALUES
    ('43000000-0000-0000-0000-000000000001','LABOREX TOGO','repartiteur','Rue des hydrocarbures, Lomé','22 20 25 10','Lomé',30,50000000, true);

INSERT INTO products (id, designation, category_id, therapeutic_class_id, pharmaceutical_form, dosage, unit_label, carton_quantity, primary_supplier_id, default_origin_country, default_transport_mode, min_stock_threshold) VALUES
    ('44000000-0000-0000-0000-000000000001','France Lait 1er âge 400g','40000000-0000-0000-0000-000000000001','41000000-0000-0000-0000-000000000001','Lait en poudre','boite/400g','boîte',12,'42000000-0000-0000-0000-000000000001','France','maritime',1500);

INSERT INTO product_suppliers (product_id, supplier_id, is_primary, origin_country) VALUES
    ('44000000-0000-0000-0000-000000000001','42000000-0000-0000-0000-000000000001', true,'France');

-- ---- Pricing : profil de coefficients vérifié empiriquement ----
INSERT INTO pricing_profiles (id, name, supplier_id, category_id, transport_mode, commission_coeff, freight_coeff, transit_coeff, transfer_fee_coeff, target_margin_coeff) VALUES
    ('45000000-0000-0000-0000-000000000001','Import Maritime Lait Infantile — Continental Commodities','42000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','maritime',1.25,1.03,1.09,1.07,1.10);

-- ---- Achats : commande fournisseur (statuts complets, historisés) ----
INSERT INTO purchase_orders (id, order_number, supplier_id, currency, locked_exchange_rate_id, incoterm, status, order_date, expected_delivery_date, validated_by_user_id, validated_at, created_by_user_id) VALUES
    ('46000000-0000-0000-0000-000000000001','PO-2026-000456','42000000-0000-0000-0000-000000000001','EUR','10000000-0000-0000-0000-000000000001','CIF','recue','2026-05-15','2026-08-20','30000000-0000-0000-0000-000000000001','2026-05-16 09:00:00+00','30000000-0000-0000-0000-000000000001');

INSERT INTO purchase_order_lines (id, purchase_order_id, product_id, quantity_ordered_units, quantity_ordered_cartons, unit_price_foreign) VALUES
    ('47000000-0000-0000-0000-000000000001','46000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001',1200,100,3.41);

INSERT INTO purchase_order_status_history (purchase_order_id, status, comment, changed_by_user_id, changed_at) VALUES
    ('46000000-0000-0000-0000-000000000001','brouillon','Création de la commande','30000000-0000-0000-0000-000000000001','2026-05-15 08:00:00+00'),
    ('46000000-0000-0000-0000-000000000001','validee','Validée par la Direction (cumul de rôle)','30000000-0000-0000-0000-000000000001','2026-05-16 09:00:00+00'),
    ('46000000-0000-0000-0000-000000000001','expediee','Conteneur CONT-2026-04 chargé au Havre','30000000-0000-0000-0000-000000000001','2026-06-02 00:00:00+00'),
    ('46000000-0000-0000-0000-000000000001','recue','Réception complète entrepôt Lomé','30000000-0000-0000-0000-000000000002','2026-08-20 14:00:00+00');

-- ---- Logistique : expédition maritime ----
INSERT INTO shipments (id, shipment_reference, transport_mode, carrier, transport_reference, customs_regime, status, departure_date_estimated, departure_date_actual, arrival_date_estimated, arrival_date_actual) VALUES
    ('48000000-0000-0000-0000-000000000001','EXP-2026-000789','maritime','CMA CGM','CONT-2026-04','mise_a_consommation','receptionnee','2026-06-01','2026-06-02','2026-08-15','2026-08-20');

INSERT INTO shipment_lines (id, shipment_id, purchase_order_line_id, quantity_shipped_units) VALUES
    ('49000000-0000-0000-0000-000000000001','48000000-0000-0000-0000-000000000001','47000000-0000-0000-0000-000000000001',1200);

INSERT INTO shipment_events (shipment_id, event_status, description, event_date, recorded_by_user_id) VALUES
    ('48000000-0000-0000-0000-000000000001','expedie','Chargement conteneur, port du Havre','2026-06-02 08:00:00+00','30000000-0000-0000-0000-000000000001'),
    ('48000000-0000-0000-0000-000000000001','arrive_port','Arrivée Port Autonome de Lomé','2026-08-15 06:00:00+00','30000000-0000-0000-0000-000000000002'),
    ('48000000-0000-0000-0000-000000000001','dedouane','Dédouanement effectué, mise à la consommation','2026-08-19 00:00:00+00','30000000-0000-0000-0000-000000000002'),
    ('48000000-0000-0000-0000-000000000001','livre','Livré entrepôt LABMEDIS Lomé','2026-08-20 14:00:00+00','30000000-0000-0000-0000-000000000002');

INSERT INTO import_costs (shipment_id, cost_type, amount, currency, allocation_method) VALUES
    ('48000000-0000-0000-0000-000000000001','freight',612000,'XOF','valeur'),
    ('48000000-0000-0000-0000-000000000001','douane',245000,'XOF','valeur');

-- ---- Entreposage : entrepôt, emplacements hiérarchiques, DEUX lots
--      (pour vérifier réellement l'allocation FEFO) ----
INSERT INTO warehouses (id, code, name, address) VALUES
    ('4a000000-0000-0000-0000-000000000001','ENT-LOME','Entrepôt Principal Lomé','Zone portuaire, Lomé');

INSERT INTO storage_locations (id, warehouse_id, parent_location_id, code, name, location_type) VALUES
    ('4b000000-0000-0000-0000-000000000001','4a000000-0000-0000-0000-000000000001',NULL,'ZONE-A','Zone A — produits infantiles','stockage'),
    ('4b000000-0000-0000-0000-000000000002','4a000000-0000-0000-0000-000000000001','4b000000-0000-0000-0000-000000000001','A-01-02-01','Zone A / Allée 1 / Rack 2 / Niveau 1','stockage'),
    ('4b000000-0000-0000-0000-000000000003','4a000000-0000-0000-0000-000000000001','4b000000-0000-0000-0000-000000000001','A-01-03-01','Zone A / Allée 1 / Rack 3 / Niveau 1','stockage'),
    ('4b000000-0000-0000-0000-000000000004','4a000000-0000-0000-0000-000000000001',NULL,'QUAR-01','Zone de quarantaine','quarantaine');

-- Lot A : PRU calculé via la cascade de coefficients vérifiée
--   PA_CFA = 3.41 * 655.957 = 2236.81337
--   PR     = 2236.81337 * 1.25 * 1.03 * 1.09 * 1.07 = 3358.824... -> arrondi 3359 CFA
-- Péremption la PLUS PROCHE : doit être proposé en premier par FEFO.
INSERT INTO stock_lots (id, product_id, supplier_id, purchase_order_line_id, shipment_line_id, pricing_profile_id, supplier_batch_number, transport_mode, reception_date, expiry_date, status, initial_quantity, remaining_quantity, carton_quantity_received, unit_cost_cfa) VALUES
    ('4c000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','42000000-0000-0000-0000-000000000001','47000000-0000-0000-0000-000000000001','49000000-0000-0000-0000-000000000001','45000000-0000-0000-0000-000000000001','LOT-A123','maritime','2026-08-20','2027-06-30','libere',1200,1200,100,3359);

-- Lot B : péremption plus lointaine, arrivé plus tôt par avion (autre commande, mêmes coefficients pour simplifier),
-- ne doit PAS être proposé avant épuisement du lot A dont la péremption est plus proche (règle FEFO).
INSERT INTO purchase_orders (id, order_number, supplier_id, currency, locked_exchange_rate_id, incoterm, status, order_date) VALUES
    ('46000000-0000-0000-0000-000000000002','PO-2026-000301','42000000-0000-0000-0000-000000000001','EUR','10000000-0000-0000-0000-000000000001','CIF','recue','2026-03-01');
INSERT INTO purchase_order_lines (id, purchase_order_id, product_id, quantity_ordered_units, quantity_ordered_cartons, unit_price_foreign) VALUES
    ('47000000-0000-0000-0000-000000000002','46000000-0000-0000-0000-000000000002','44000000-0000-0000-0000-000000000001',720,60,3.41);
INSERT INTO stock_lots (id, product_id, supplier_id, purchase_order_line_id, shipment_line_id, pricing_profile_id, supplier_batch_number, transport_mode, reception_date, expiry_date, status, initial_quantity, remaining_quantity, carton_quantity_received, unit_cost_cfa) VALUES
    ('4c000000-0000-0000-0000-000000000002','44000000-0000-0000-0000-000000000001','42000000-0000-0000-0000-000000000001','47000000-0000-0000-0000-000000000002',NULL,'45000000-0000-0000-0000-000000000001','LOT-B456','maritime','2026-04-15','2027-11-30','libere',720,720,60,3359);

INSERT INTO stock_lot_locations (stock_lot_id, storage_location_id, quantity, reserved_quantity) VALUES
    ('4c000000-0000-0000-0000-000000000001','4b000000-0000-0000-0000-000000000002',1200,0),
    ('4c000000-0000-0000-0000-000000000002','4b000000-0000-0000-0000-000000000003',720,0);

-- Mouvement de réception (traçabilité entrée)
INSERT INTO stock_movements (id, reference, movement_type, movement_date, user_id, source_document_type, source_document_id, status) VALUES
    ('4d000000-0000-0000-0000-000000000001','MVT-2026-000001','reception_fournisseur','2026-08-20 14:00:00+00','30000000-0000-0000-0000-000000000002','purchase_order','46000000-0000-0000-0000-000000000001','valide');
INSERT INTO stock_movement_lines (stock_movement_id, product_id, stock_lot_id, destination_location_id, quantity) VALUES
    ('4d000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','4c000000-0000-0000-0000-000000000001','4b000000-0000-0000-0000-000000000002',1200);

-- ---- Pricing appliqué : PV HT calculé vs Prix Labmedis (écart conservé) ----
INSERT INTO product_prices (product_id, pricing_profile_id, pr_unit_cfa, pv_ht_calculated, pv_ht_applied, vat_rate, effective_from, created_by_user_id) VALUES
    ('44000000-0000-0000-0000-000000000001','45000000-0000-0000-0000-000000000001',3359,3695,3660,0.1800,'2026-08-20','30000000-0000-0000-0000-000000000001');

-- ---- Ventes : commande LABOREX TOGO, allocation FEFO -> LOT-A123 (péremption la plus proche) ----
INSERT INTO sale_orders (id, order_number, customer_id, currency, status, order_date, total_ht_cfa, total_vat_cfa, total_ttc_cfa, created_by_user_id) VALUES
    ('4e000000-0000-0000-0000-000000000001','SO-2026-000112','43000000-0000-0000-0000-000000000001','XOF','facturee','2026-08-25',439200,79056,518256,'30000000-0000-0000-0000-000000000003');

INSERT INTO sale_order_lines (id, sale_order_id, product_id, allocated_stock_lot_id, quantity, unit_price_ht_cfa, vat_rate) VALUES
    ('4f000000-0000-0000-0000-000000000001','4e000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','4c000000-0000-0000-0000-000000000001',120,3660,0.1800);

INSERT INTO sale_order_status_history (sale_order_id, status, changed_by_user_id, changed_at) VALUES
    ('4e000000-0000-0000-0000-000000000001','confirmee','30000000-0000-0000-0000-000000000003','2026-08-25 10:00:00+00'),
    ('4e000000-0000-0000-0000-000000000001','livree','30000000-0000-0000-0000-000000000002','2026-08-25 16:00:00+00'),
    ('4e000000-0000-0000-0000-000000000001','facturee','30000000-0000-0000-0000-000000000003','2026-08-25 16:30:00+00');

INSERT INTO deliveries (id, delivery_number, sale_order_id, delivery_date, status, delivered_by_user_id) VALUES
    ('50000000-0000-0000-0000-000000000001','BL-2026-000098','4e000000-0000-0000-0000-000000000001','2026-08-25','confirmee','30000000-0000-0000-0000-000000000002');
INSERT INTO delivery_lines (delivery_id, sale_order_line_id, stock_lot_id, quantity_delivered) VALUES
    ('50000000-0000-0000-0000-000000000001','4f000000-0000-0000-0000-000000000001','4c000000-0000-0000-0000-000000000001',120);

-- Mouvement de sortie : décrémente le lot A (le plus proche de péremption)
INSERT INTO stock_movements (id, reference, movement_type, movement_date, user_id, source_document_type, source_document_id, status) VALUES
    ('4d000000-0000-0000-0000-000000000002','MVT-2026-000002','vente','2026-08-25 16:00:00+00','30000000-0000-0000-0000-000000000002','sale_order','4e000000-0000-0000-0000-000000000001','valide');
INSERT INTO stock_movement_lines (stock_movement_id, product_id, stock_lot_id, source_location_id, quantity) VALUES
    ('4d000000-0000-0000-0000-000000000002','44000000-0000-0000-0000-000000000001','4c000000-0000-0000-0000-000000000001','4b000000-0000-0000-0000-000000000002',120);
UPDATE stock_lot_locations SET quantity = quantity - 120 WHERE stock_lot_id = '4c000000-0000-0000-0000-000000000001' AND storage_location_id = '4b000000-0000-0000-0000-000000000002';
UPDATE stock_lots SET remaining_quantity = remaining_quantity - 120 WHERE id = '4c000000-0000-0000-0000-000000000001';

INSERT INTO invoices (id, invoice_number, customer_id, sale_order_id, currency, status, invoice_date, total_ht_cfa, total_vat_cfa, total_ttc_cfa) VALUES
    ('51000000-0000-0000-0000-000000000001','FAC-2026-000210','43000000-0000-0000-0000-000000000001','4e000000-0000-0000-0000-000000000001','XOF','emise','2026-08-25',439200,79056,518256);
INSERT INTO invoice_lines (invoice_id, product_id, stock_lot_id, quantity, unit_price_ht_cfa, vat_rate, line_total_ht_cfa, line_total_ttc_cfa) VALUES
    ('51000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','4c000000-0000-0000-0000-000000000001',120,3660,0.1800,439200,518256);

-- ---- Retour client partiel (5 unités endommagées) + avoir ----
INSERT INTO customer_returns (id, return_number, customer_id, sale_order_line_id, original_stock_lot_id, quantity, reason, decision, decided_by_user_id, decided_at) VALUES
    ('52000000-0000-0000-0000-000000000001','RET-2026-000015','43000000-0000-0000-0000-000000000001','4f000000-0000-0000-0000-000000000001','4c000000-0000-0000-0000-000000000001',5,'Carton endommagé pendant le transport client','destruction','30000000-0000-0000-0000-000000000002','2026-08-27 09:00:00+00');
INSERT INTO credit_notes (id, credit_note_number, invoice_id, customer_id, reason, total_ht_cfa, total_vat_cfa, total_ttc_cfa, issued_at) VALUES
    ('53000000-0000-0000-0000-000000000001','AV-2026-000031','51000000-0000-0000-0000-000000000001','43000000-0000-0000-0000-000000000001','Retour partiel — 5 unités endommagées',18300,3294,21594,'2026-08-27');
INSERT INTO credit_note_lines (credit_note_id, invoice_line_id, product_id, quantity, unit_price_ht_cfa, vat_rate, line_total_ttc_cfa)
    SELECT '53000000-0000-0000-0000-000000000001', il.id, '44000000-0000-0000-0000-000000000001', 5, 3660, 0.1800, 21594
    FROM invoice_lines il WHERE il.invoice_id = '51000000-0000-0000-0000-000000000001';
UPDATE customer_returns SET credit_note_id = '53000000-0000-0000-0000-000000000001' WHERE id = '52000000-0000-0000-0000-000000000001';

-- ---- MRP : calcul de prévision + suggestion de commande (produit sous seuil) ----
INSERT INTO forecast_parameters (product_id, is_enabled, forecast_horizon_days, safety_stock, target_coverage_days, consumption_method) VALUES
    ('44000000-0000-0000-0000-000000000001', true, 180, 300, 120, 'moyenne_90j');

INSERT INTO supplier_lead_times (supplier_id, product_id, transport_mode, manufacturing_lead_time_days, preparation_lead_time_days, transport_lead_time_days, customs_lead_time_days, internal_lead_time_days) VALUES
    ('42000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','maritime',45,5,40,15,5);

-- Vente historique (alimente daily_sales_summary, source de la consommation MRP)
INSERT INTO daily_sales_summary (sales_date, customer_id, product_id, category_id, quantity_sold, total_amount_ht_cfa, total_vat_cfa, total_amount_ttc_cfa, total_cost_cfa, gross_margin_cfa) VALUES
    ('2026-08-25','43000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001',120,439200,79056,518256,403080,36120);

-- Calcul MRP : stock disponible = 1200-120(lotA)+720(lotB) = 1800 ; consommation ~20/j (hypothèse 90j) ;
-- lead time total = 45+5+40+15+5 = 110j ; reorder point = 20*110+300 = 2500 -> 1800 <= 2500 => suggestion
INSERT INTO forecast_calculations (id, product_id, calculation_date, available_stock, reserved_stock, transit_stock, average_daily_consumption, lead_time_days, safety_stock, reorder_point, target_stock, net_requirement, coverage_days, risk_level) VALUES
    ('54000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','2026-08-28',1800,0,0,20.0,110,300,2500,2700,900,90,'urgent');

INSERT INTO reorder_suggestions (id, forecast_calculation_id, product_id, supplier_id, suggested_quantity_units, suggested_quantity_cartons, suggested_transport_mode, suggested_order_date, estimated_reception_date, status) VALUES
    ('55000000-0000-0000-0000-000000000001','54000000-0000-0000-0000-000000000001','44000000-0000-0000-0000-000000000001','42000000-0000-0000-0000-000000000001',900,75,'aerien','2026-08-28','2026-11-15','en_attente');

-- ---- Notification + audit ----
INSERT INTO notifications (notification_type, recipient_role_id, channel, title, message, source_document_type, source_document_id) VALUES
    ('ReorderSuggestionCreated','20000000-0000-0000-0000-000000000003','signalr','Suggestion de réapprovisionnement urgente','France Lait 1er âge 400g : couverture 90j < délai fournisseur 110j','reorder_suggestions','55000000-0000-0000-0000-000000000001');

INSERT INTO audit_logs (user_id, user_full_name, action, module, http_method, path, entity_type, entity_id, ip_address, user_agent, is_success, response_message) VALUES
    ('30000000-0000-0000-0000-000000000002','Ama Mensah','ReceiveShipment','Stock','POST','/api/purchase-orders/46000000-0000-0000-0000-000000000001/receive','purchase_orders','46000000-0000-0000-0000-000000000001','41.207.66.10','Mozilla/5.0', true,'Réception validée, 2 lots créés');

-- Synthèse quotidienne stock (pour vérification croisée dashboard)
INSERT INTO daily_stock_summary (summary_date, product_id, category_id, supplier_id, physical_stock, reserved_stock, available_stock, quarantine_stock, expired_stock, stock_value_cfa) VALUES
    ('2026-08-28','44000000-0000-0000-0000-000000000001','40000000-0000-0000-0000-000000000001','42000000-0000-0000-0000-000000000001',1800,0,1800,0,0,1800*3359);
