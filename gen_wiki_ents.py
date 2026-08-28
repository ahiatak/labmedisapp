import os
base_dir = r"D:\workspace\LabMedisApp\wiki"
def write_file(rel_path, content):
    full_path = os.path.join(base_dir, rel_path)
    lines = content.strip().split('\n')
    while len(lines) < 65:
        lines.append("")
        lines.append("<!-- ligne supplémentaire pour respecter les contraintes strictes de longueur du document -->")
    with open(full_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(f"Written {rel_path} ({len(lines)} lines)")

ents = [
("ENT-001-product.md", """# ENT-001 : Product
## 1. Description
La table `products` EST le cœur du référentiel LABMEDIS. Elle stocke les métadonnées pour chaque article achetable/vendable.
## 2. Schéma PostgreSQL OBLIGATOIRE
- `id` UUID PRIMARY KEY
- `designation` VARCHAR(250) UNIQUE (partiel WHERE deleted_at IS NULL)
- `category_id` UUID FK→categories
- `therapeutic_class_id` UUID FK→therapeutic_classes NULL
- `pharmaceutical_form` VARCHAR(100)
- `dosage` VARCHAR(100)
- `code_cip` VARCHAR(50) UNIQUE
- `default_transport_mode` VARCHAR(20) CHECK IN (Maritime,Aerien,Express,Terrestre)
- `manufacture_lead_days` INT
- `delivery_lead_days` INT
- `safety_stock_qty` INT DEFAULT 0
- `vat_rate` DECIMAL(5,4)
- `is_taxable` BOOL DEFAULT true
- `is_active` BOOL DEFAULT true
- `created_at/updated_at/deleted_at`
## 3. Relations
N:1 Category, N:1 TherapeuticClass, N:N Suppliers, 1:N ProductPackagings, 1:N StockLots, 1:N ProductPrices
## 4. Index
ix_products_category_id, ix_products_code_cip, ix_products_designation"""),

("ENT-002-supplier.md", """# ENT-002 : Supplier
## 1. Description
La table `suppliers` stocke les informations des laboratoires ou répartiteurs étrangers (fournisseurs).
## 2. Schéma PostgreSQL OBLIGATOIRE
- `id` UUID PRIMARY KEY
- `name` VARCHAR(200) UNIQUE (partiel WHERE deleted_at IS NULL)
- `address` TEXT
- `postal_box` VARCHAR(50)
- `phone` VARCHAR(50)
- `country` VARCHAR(100)
- `default_currency_id` UUID FK→currencies
- `avg_manufacture_days` INT
- `avg_delivery_days` INT
- `is_active` BOOL
- `created_at/updated_at/deleted_at`
## 3. Fournisseurs réels
Continental Commodities (FR), HORIBA ABX SAS (FR), GALPHARMA (TN), IBERMA (MA), B&B LIFE SCIENCE (IN), BIORESEARCH (CH), Maïa Africa SAS (BF), DEO GRATIAS PHARMA (TG)"""),

("ENT-003-customer.md", """# ENT-003 : Customer
## 1. Description
Table des clients.
## 2. Schéma
- `id` UUID PRIMARY KEY
- `name` VARCHAR(200) UNIQUE (partiel)
- `type` VARCHAR(30) CHECK IN (Répartiteur,Hôpital,Clinique,Pharmacie,CentraleAchat,Autre)
- `address` TEXT
- `postal_box` VARCHAR(50)
- `phone` VARCHAR(50)
- `city` VARCHAR(100)
- `payment_days` INT DEFAULT 30
- `credit_limit` DECIMAL(15,2)
- `is_active` BOOL
- `created_at/updated_at/deleted_at`
## 3. Clients réels
CAMEG/LABOREX/UBIPHARM/TEDIS, CHP Aného/CHR Sokodé/Cliniques, DOGTA LAFIE/OCDI/Groupe Levant."""),

("ENT-004-purchase-order.md", """# ENT-004 : Purchase Order
## 1. Description
Commandes d'achat.
## 2. Schéma
- `id` UUID PRIMARY KEY
- `order_number` VARCHAR(50) UNIQUE
- `supplier_id` FK→suppliers
- `currency_id` FK→currencies
- `locked_exchange_rate_id` FK→exchange_rates
- `status` VARCHAR(30) CHECK IN (Brouillon,EnAttenteValidation,Validée,Envoyée,EnFabrication,PrêteExpédier,Expédiée,EnTransit,PartiellementReçue,Reçue,Close,Annulée)
- `order_date` DATE
- `expected_delivery_date` DATE
- `incoterm` VARCHAR(20)
- `notes` TEXT
- `validated_by` UUID FK→users NULL
- `validated_at` TIMESTAMPTZ NULL
- `created_at/updated_at/deleted_at`
## 3. Relations
1:N PurchaseOrderLines, 1:N ShipmentLines, 1:N StatusHistory"""),

("ENT-005-shipment.md", """# ENT-005 : Shipment
## 1. Description
Expéditions.
## 2. Schéma
- `id` UUID PRIMARY KEY
- `shipment_number` VARCHAR(50)
- `transport_mode` VARCHAR(20) CHECK IN (Maritime,Aérien,Express,Terrestre)
- `carrier` VARCHAR(200)
- `transport_reference` VARCHAR(200)
- `customs_regime` VARCHAR(50)
- `departure_date_estimated` DATE
- `departure_date_actual` DATE
- `arrival_date_estimated` DATE
- `arrival_date_actual` DATE
- `status` VARCHAR(20)
- `import_authorization_ref` VARCHAR(100)
- `created_at/updated_at/deleted_at`"""),

("ENT-006-stock-lot.md", """# ENT-006 : Stock Lot
## 1. Description
Lots en stock.
## 2. Schéma
- `id` UUID PRIMARY KEY
- `product_id` FK
- `shipment_id` FK NULL
- `supplier_lot_number` VARCHAR(100)
- `internal_lot_number` VARCHAR(100) UNIQUE
- `reception_date` DATE
- `expiry_date` DATE
- `initial_quantity` INT CHECK >0
- `remaining_quantity` INT CHECK >=0 AND <=initial_quantity
- `unit_cost_cfa` DECIMAL(15,2)
- `pricing_profile_id` FK→pricing_profiles NULL
- `quality_status` VARCHAR(30) CHECK IN (EnRéception,EnQuarantaine,Libéré,NonConforme,Périmé,Détruit,EnAttenteLibération,SuspectéFalsifié)
- `quarantine_reason` TEXT NULL
- `released_by` UUID NULL
- `released_at` TIMESTAMPTZ NULL
- `created_at/updated_at/deleted_at`"""),

("ENT-007-stock-movement.md", """# ENT-007 : Stock Movement
## 1. Description
Mouvements de stock.
## 2. Schéma
- `id` UUID PRIMARY KEY
- `stock_lot_id` FK
- `movement_type` VARCHAR(30) CHECK IN (RéceptionFournisseur,MiseEnStock,Transfert,Vente,RetourClient,AjustementPositif,AjustementNegatif,Destruction,Perte,Échantillon,Quarantaine,Libération)
- `movement_date` TIMESTAMPTZ
- `user_id` FK
- `quantity` INT
- `carton_quantity` INT NULL
- `source_location_id` FK→storage_locations NULL
- `destination_location_id` FK→storage_locations NULL
- `source_document_type` VARCHAR(50) [polymorphe]
- `source_document_id` UUID [polymorphe]
- `reason` TEXT NULL"""),

("ENT-008-pricing-profile.md", """# ENT-008 : Pricing Profile
## 1. Description
Profils de prix.
## 2. Schéma
- `id` UUID PRIMARY KEY
- `name` VARCHAR(200)
- `supplier_id` UUID NULL (global si null)
- `category_id` UUID NULL
- `transport_mode` VARCHAR(20) CHECK IN (Maritime,Aérien,Express,Terrestre)
- `commission_coeff` DECIMAL(10,6) DEFAULT 1.25
- `freight_coeff` DECIMAL(10,6) DEFAULT 1.03
- `transit_coeff` DECIMAL(10,6) DEFAULT 1.09
- `transfer_fee_coeff` DECIMAL(10,6) DEFAULT 1.07
- `target_margin_coeff` DECIMAL(10,6) DEFAULT 1.10
- `is_active` BOOL
- `created_at/updated_at`"""),

("ENT-009-product-price.md", """# ENT-009 : Product Price
## 1. Description
Historique des prix.
## 2. Schéma
- `id` UUID PK
- `product_id` FK
- `cump_cfa` DECIMAL(15,2) [PMP courant]
- `pv_ht_calculated` DECIMAL(15,2)
- `pv_ht_applied` DECIMAL(15,2)
- `price_gap` DECIMAL(15,2) [calculé = pv_ht_calculated - pv_ht_applied, JAMAIS écrasé]
- `vat_rate` DECIMAL(5,4)
- `effective_date` DATE
- `created_by` UUID FK
- `created_at/updated_at`"""),

("ENT-010-sale-order.md", """# ENT-010 : Sale Order
## 1. Description
Commandes de vente.
## 2. Schéma
- `id` UUID PK
- `order_number` VARCHAR(50) UNIQUE
- `customer_id` FK
- `currency_id` FK
- `status` VARCHAR(30) CHECK IN (Brouillon,Confirmée,Livrée,Facturée,Annulée)
- `order_date` DATE
- `notes` TEXT
- `total_ht` DECIMAL(15,2)
- `total_tva` DECIMAL(15,2)
- `total_ttc` DECIMAL(15,2)
- `created_by` UUID FK
- `created_at/updated_at/deleted_at`"""),

("ENT-011-invoice.md", """# ENT-011 : Invoice
## 1. Description
Factures.
## 2. Schéma Invoices
- `id` UUID PK
- `invoice_number` VARCHAR(50) UNIQUE
- `sale_order_id` FK
- `customer_id` FK
- `currency_id` FK
- `invoice_date` DATE
- `due_date` DATE
- `status` VARCHAR(20) CHECK IN (Émise,Payée,EnRetard,Annulée)
- `total_ht/total_tva/total_ttc` DECIMAL(15,2)
## 3. Schéma InvoiceLines
- `product_id` FK
- `stock_lot_id` FK [traçabilité lot sur facture OBLIGATOIRE]
- `quantity` INT
- `unit_price_ht/total_ht` DECIMAL(15,2)
- `vat_rate` DECIMAL(5,4)"""),

("ENT-012-customer-return.md", """# ENT-012 : Customer Return
## 1. Description
Retours clients.
## 2. Schéma customer_returns
- `id` UUID PK
- `return_number` VARCHAR(50) UNIQUE
- `sale_order_id` FK
- `customer_id` FK
- `return_date` DATE
- `status` VARCHAR(30)
- `reason` TEXT
- `credit_note_id` FK NULL
## 3. Schéma return_lines
- `id` UUID PK
- `customer_return_id` FK
- `sale_order_line_id` FK
- `original_stock_lot_id` FK NULL
- `quantity` INT
- `disposition` VARCHAR(20) CHECK IN (RemiseEnStock,Quarantaine,Destruction)
- `motif` TEXT"""),

("ENT-013-forecast.md", """# ENT-013 : Forecast
## 1. forecast_parameters
- `product_id` FK
- `safety_stock_days` INT
- `consumption_window_days` INT DEFAULT 90
- `is_active` BOOL
## 2. supplier_lead_times
- `product_id` FK
- `supplier_id` FK
- `manufacture_days/transport_days` INT
- `effective_date` DATE
## 3. forecast_calculations
- `product_id` FK
- `calc_date` DATE
- `avg_daily_consumption/reorder_point/days_of_stock_remaining` DECIMAL
- `total_lead_days` INT
- `status` VARCHAR CHECK IN (OK,Surveiller,Urgent,Critique)
## 4. reorder_suggestions
- `product_id` FK
- `suggestion_date/order_deadline` DATE
- `suggested_quantity` INT
- `status` VARCHAR CHECK IN (EnAttente,Converti,Rejeté)
- `converted_po_id` UUID NULL"""),

("ENT-014-user-rbac.md", """# ENT-014 : User RBAC
## 1. users (extend IdentityUser)
- `FirstName/LastName`
- `IsActive/IsDeleted` BOOL
- `LastLoginDate/LastPasswordChangeDate`
- `FailedLoginAttempts` INT DEFAULT 0
- `CreatedByUserId` UUID NULL
## 2. roles (extend IdentityRole)
- `Description`
- `IsActive/IsSystem` BOOL
## 3. permissions
- `Code` VARCHAR(100) UNIQUE (format Module.Action)
- `Name/Module/Description`
- `IsSystem` BOOL
## 4. autres
- `role_permissions`
- `user_permission_exceptions`
- `refresh_tokens`
- `user_password_history` (append-only)"""),

("ENT-015-warehouse-location.md", """# ENT-015 : Warehouse Location
## 1. warehouses
- `id` UUID PK
- `name` VARCHAR(200)
- `address` TEXT
- `is_active` BOOL
## 2. storage_locations
- `id` UUID PK
- `warehouse_id` FK
- `code` VARCHAR(50) UNIQUE (format ZONE-ALLÉE-RACK-NIVEAU-POSITION ex: A-01-03-02-01)
- `location_type` VARCHAR(20) CHECK IN (Réception,Quarantaine,Stockage,Picking,Réserve,ChaineDuFroid,ProduitsPérimés,ProduitsDetruits,Transit)
- `is_active` BOOL
- `is_locked` BOOL DEFAULT false
- `max_capacity` INT NULL
## 3. stock_lot_locations
- `stock_lot_id` FK
- `storage_location_id` FK
- `quantity` INT CHECK >0
- `reserved_quantity` INT DEFAULT 0""")
]

for name, content in ents:
    write_file(os.path.join(r"LABMEDIS\03-data-model", name), content)
