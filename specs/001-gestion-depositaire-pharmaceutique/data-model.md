# Data Model — LABMEDIS

**Feature**: 001-gestion-depositaire-pharmaceutique | **Phase**: 1 (Design) | **Date**: 2026-08-28

Modèle de données dérivé du spec ([spec.md](./spec.md), section Key Entities) et enrichi à partir de `wiki/LABMEDIS/03-data-model/` et `06-regles-metier/`. Toute entité métier hérite de `BaseEntity` (Principe III de la constitution) et est exposée via `I[Entité]Repository`/`[Entité]Repository : BaseRepository<T>` (Core) puis `[Entité]Service : [Entité]Repository, I[Entité]Service` (Service), conformément au Principe II. Les champs monétaires/décimaux ne sont montrés ici qu'en type de stockage (`decimal`) — leur représentation dans les DTOs Request est `string` (Principe VI, voir `contracts/`).

## Convention Transverse — BaseEntity

| Champ | Type | Règle |
|---|---|---|
| `Id` | `Guid` | Clé primaire (UUID) |
| `CreatedAt` | `DateTime` | Horodatage de création (UTC) |
| `UpdatedAt` | `DateTime?` | Horodatage de dernière modification |
| `DeletedAt` | `DateTime?` | Horodatage de suppression logique |
| `IsDeleted` | `bool` | Soft delete exclusif (FR-084) ; Query Filter EF Core global |

---

## 1. Référentiel (US1 — FR-001 à FR-011)

### Product
| Champ | Type | Règle |
|---|---|---|
| `Designation` | `string(250)` | Unique parmi les produits actifs (FR-002) |
| `CategoryId` | `Guid` | FK → Category, requis (FR-001) |
| `TherapeuticClassId` | `Guid?` | FK → TherapeuticClass |
| `PharmaceuticalForm` | `string(100)?` | Liste contrôlée |
| `Dosage` | `string(100)?` | |
| `CodeCip` | `string(50)?` | Unique parmi les produits actifs |
| `DefaultTransportMode` | `enum` | Maritime \| Aerien \| Express \| Terrestre |
| `ManufactureLeadDays` | `int?` | ≥ 0, alimente MRP (FR-063) |
| `DeliveryLeadDays` | `int?` | ≥ 0, alimente MRP |
| `SafetyStockQty` | `int` | ≥ 0, défaut 0 |
| `VatRate` | `decimal(5,4)` | 0.0000–0.9999 (FR-087) |
| `IsTaxable` | `bool` | Défaut `true` (FR-087, RG-007) |
| `IsActive` | `bool` | Défaut `true` ; masque des sélections si `false` (FR-005) |

**Relations** : N:1 `Category`, N:1 `TherapeuticClass`, N:N `Supplier` (via `ProductSupplier`), 1:N `ProductPackaging`, 1:N `StockLot`, 1:N `ProductPrice`.

### Category / TherapeuticClass / PharmaceuticalForm (référentiels)
Listes contrôlées simples (`Id`, `Name`, `IsActive`) — aucune saisie libre autorisée pour ces axes (FR-003).

### ProductPackaging
| Champ | Type | Règle |
|---|---|---|
| `ProductId` | `Guid` | FK → Product |
| `PackagingType` | `enum` | Unité \| Carton \| Palette \| ColisExpress |
| `QuantityPerPackage` | `int` | > 0 |

### Supplier
| Champ | Type | Règle |
|---|---|---|
| `Name` | `string(200)` | Unique parmi actifs (FR-007) |
| `Country` | `string(100)` | Requis |
| `DefaultCurrencyId` | `Guid` | FK → Currency |
| `AvgManufactureDays` / `AvgDeliveryDays` | `int?` | Utilisés par MRP (FR-063) |
| `IsActive` | `bool` | |

### ProductSupplier (association ordonnée)
`ProductId`, `SupplierId`, `Priority` (int, ordre de préférence).

### Customer
| Champ | Type | Règle |
|---|---|---|
| `Name` | `string(200)` | Unique parmi actifs (FR-008) |
| `Type` | `enum` | Répartiteur \| Hôpital \| Clinique \| Pharmacie \| CentraleAchat \| Autre |
| `PaymentDays` | `int` | Défaut 30 |
| `CreditLimit` | `decimal(15,2)?` | Plafond d'encours (FR-009) |
| `IsActive` | `bool` | Bloque toute nouvelle commande si `false` (FR-010) |

### CustomerProductPrice (tarifs négociés)
`CustomerId`, `ProductId`, `UnitPrice` (`decimal`), `ValidFrom`/`ValidTo` (`DateOnly`) — **contrainte** : aucun chevauchement de période pour un même couple client/produit (FR-011).

---

## 2. Sécurité, Rôles, Permissions (US2 — FR-012 à FR-019)

### ApplicationUser (extension IdentityUser)
`FirstName`, `LastName`, `IsActive`, `LastLoginDate`, `FailedLoginAttempts` (int, verrouillage à 5 — FR-014), `LockoutEnd`.

### Role (extension IdentityRole) / Permission
`Permission.Code` au format `Module.Action` (unique) ; `RolePermission` (association) ; `UserPermissionException` (dérogation individuelle, `IsGranted` bool) — permet FR-016/FR-019.

### RefreshToken
`UserId`, `Token`, `ExpiresAt`, `RevokedAt?` — supporte FR-012 (jeton 15-30 min / refresh 7-30 j) et FR-018 (révocation).

### LoginAudit (journal de connexion, append-only — exempté du soft delete par le Principe III)
`UserId?`, `AttemptedAt`, `Success` (bool), `IpAddress`, `UserAgent` — FR-017.

---

## 3. Devises & Pricing (US6 — FR-045 à FR-053, FR-085 à FR-088)

### Currency
`Code` (EUR \| USD \| XOF), `Name`.

### ExchangeRate
`CurrencyFromId`, `CurrencyToId`, `Rate` (`decimal(15,6)`), `EffectiveDate`, `IsFixed` (bool — `true` pour EUR/XOF à 655,957, modifiable uniquement par Admin, FR-085).

### PricingProfile
| Champ | Type | Règle |
|---|---|---|
| `Name` | `string(200)` | |
| `SupplierId` | `Guid?` | `null` = règle globale |
| `CategoryId` | `Guid?` | `null` = toutes catégories (FR-047) |
| `TransportMode` | `enum` | Maritime \| Aerien \| Express \| Terrestre |
| `CommissionCoeff` / `FreightCoeff` / `TransitCoeff` / `TransferFeeCoeff` / `TargetMarginCoeff` | `decimal(10,6)` | Jamais codés en dur (FR-045) |
| `IsActive` | `bool` | |

### ProductPrice (historique immuable — FR-050)
`ProductId`, `CumpCfa` (PMP courant), `PvHtCalculated`, `PvHtApplied`, `PriceGap` (= `PvHtCalculated - PvHtApplied`, jamais écrasé — FR-049), `VatRate`, `EffectiveDate`, `CreatedBy`. Chaque changement = **nouvelle ligne**, jamais d'`UPDATE` sur une ligne existante.

---

## 4. Achats & Logistique (US3 — FR-020 à FR-028)

### PurchaseOrder
| Champ | Type | Règle |
|---|---|---|
| `OrderNumber` | `string(50)` | Unique, format `PO-AAAAMMJJ-NNNN` |
| `SupplierId` | `Guid` | Fournisseur actif requis |
| `CurrencyId` | `Guid` | Requis |
| `LockedExchangeRateId` | `Guid` | Figé à la création, jamais recalculé (FR-021, RG-003) |
| `Status` | `enum` (state machine ci-dessous) | |
| `OrderDate` / `ExpectedDeliveryDate` | `DateOnly` | |
| `Incoterm` | `string(20)?` | Optionnel (Assumptions) |
| `TransportMode` | `enum` | |
| `CancellationReason` | `string?` | Requis si `Status = Annulée` (FR-024) |
| `ValidatedBy` / `ValidatedAt` | `Guid?` / `DateTime?` | Renseigné si validation Direction (FR-023) |

**State Machine** (FR-022) :
```
Brouillon → EnAttenteValidation → Validée → Envoyée → EnFabrication → PrêteÀExpédier
→ Expédiée → EnTransit → {PartiellementReçue → EnTransit | Reçue} → Close
[toute étape active] → Annulée (motif obligatoire, terminal, irréversible)
```
Chaque transition crée une ligne `PurchaseOrderStatusHistory` (`FromStatus`, `ToStatus`, `ChangedBy`, `ChangedAt`).

### PurchaseOrderLine
`PurchaseOrderId`, `ProductId` (actif requis), `Quantity` (> 0), `CartonQuantity?`, `UnitPriceForeign` (`decimal`), `PackagingTypeId`.

### Shipment
`ShipmentNumber`, `TransportMode`, `Carrier`, `TransportReference`, `CustomsRegime`, `DepartureDateEstimated/Actual`, `ArrivalDateEstimated/Actual`, `Status`, `ImportAuthorizationRef` (obligatoire si contient un médicament — FR-028).

### ShipmentLine / ImportCost
`ShipmentLine` (`ShipmentId`, `PurchaseOrderLineId`) ; `ImportCost` (`ShipmentId`, `CostType` [Freight\|Transit\|Douane\|Commission\|Transfert\|Assurance\|Manutention], `Amount`, `AllocationKey` [Valeur\|Quantité\|Volume] — FR-026).

---

## 5. Stock & Traçabilité (US4, US5, US9 — FR-029 à FR-044)

### Warehouse / StorageLocation
`StorageLocation.Code` (unique, format `ZONE-ALLÉE-RACK-NIVEAU-POSITION`), `LocationType` (Réception \| Quarantaine \| Stockage \| Picking \| Réserve \| ChaineDuFroid \| ProduitsPérimés \| ProduitsDétruits \| Transit — FR-034), `MaxCapacity?`, `IsLocked` (bool).

### StockLot (entité de traçabilité centrale)
| Champ | Type | Règle |
|---|---|---|
| `ProductId` | `Guid` | Requis |
| `ShipmentId` | `Guid?` | |
| `SupplierLotNumber` | `string(100)` | Unique par couple (fournisseur, produit) — RG-006 |
| `InternalLotNumber` | `string(100)` | Unique global, format `{code_produit}-{AAAAMMJJ}-{NNN}` |
| `ReceptionDate` / `ManufacturingDate?` / `ExpiryDate` | `DateOnly` | `ExpiryDate` obligatoire (FR-029) |
| `InitialQuantity` | `int` | > 0, **immuable après création** |
| `RemainingQuantity` | `int` | ≥ 0, ≤ `InitialQuantity` (contrainte CHECK) |
| `ReservedQuantity` | `int` | ≥ 0, ≤ `RemainingQuantity` (contrainte CHECK — filet de sécurité concurrence, voir research.md §5) |
| `UnitCostCfa` | `decimal(15,2)` | PRU figé à réception, **jamais recalculé** (FR-032) |
| `PricingProfileId` | `Guid?` | |
| `QualityStatus` | `enum` (state machine ci-dessous) | |
| `QuarantineReason` | `string?` | Requis si statut Quarantaine/NonConforme (FR-042) |
| `ReleasedBy` / `ReleasedAt` | `Guid?` / `DateTime?` | Rôle Responsable Qualité uniquement |
| `ReceivedBy` | `Guid` | |

**State Machine `QualityStatus`** (FR-040) :
```
EnRéception → {EnQuarantaine (motif obligatoire) → Libéré | NonConforme | Détruit}
EnRéception → EnAttenteLibération → Libéré
Libéré → Périmé (automatique dès ExpiryDate < aujourd'hui — FR-043)
[tout statut actif] → SuspectéFalsifié (alerte autorité — FR-083)
```
`QtyDisponible = RemainingQuantity − ReservedQuantity`. Seul le statut `Libéré` est proposable à la vente (FR-041).

### StockLotLocation (répartition multi-emplacement — FR-039)
`StockLotId`, `StorageLocationId`, `Quantity` (> 0), `ReservedQuantity`.

### StockMovement (append-only, traçabilité — FR-038)
`StockLotId`, `MovementType` (RéceptionFournisseur \| MiseEnStock \| Transfert \| Vente \| RetourClient \| AjustementPositif \| AjustementNegatif \| Destruction \| Perte \| Échantillon \| Quarantaine \| Libération), `Quantity`, `SourceLocationId?`, `DestinationLocationId?`, `UserId`, `SourceDocumentType`/`SourceDocumentId` (référence polymorphe vers PO/SaleOrder/Return/InventorySession), `Reason?`.

### InventorySession / InventoryCount (US9 — FR-044)
`InventorySession` (`Perimeter` [warehouse/zone/emplacement], `Status` [Ouverte \| Gelée \| EnComptage \| Validée \| Clôturée], `FrozenAt`) ; `InventoryCount` (`InventorySessionId`, `StockLotId`, `SystemQuantity`, `CountedQuantity`, `Variance` (calculé), `AdjustmentReason` — obligatoire si écart validé).

---

## 6. Ventes, Facturation, Retours (US7, US8 — FR-054 à FR-062, FR-091)

### SaleOrder
`OrderNumber` (unique), `CustomerId`, `CurrencyId`, `Status` (state machine ci-dessous), `OrderDate`, `TotalHt`/`TotalTva`/`TotalTtc` (`decimal`), `CreatedBy`.

**State Machine** (FR-056) :
```
Brouillon → Confirmée → Livrée → Facturée
[Brouillon | Confirmée] → Annulée (libère toute réservation de stock — FR-055)
```

### SaleOrderLine
`SaleOrderId`, `ProductId`, `Quantity`, `AllocatedStockLotId` (résultat FEFO ou dérogation motivée — FR-036/FR-037), `UnitPriceHt`, `DerogationReason?` (non vide si lot ≠ premier FEFO).

### Delivery / DeliveryLine
`Delivery` (`SaleOrderId`, `DeliveryDate`, distincte de la facture — FR-057) ; `DeliveryLine` (`SaleOrderLineId`, `QuantityDelivered`).

### Invoice / InvoiceLine
`Invoice` (`InvoiceNumber` unique, `SaleOrderId`, `CustomerId`, `CurrencyId`, `InvoiceDate`/`DueDate`, `Status` [Émise \| Payée \| EnRetard \| Annulée], `TotalHt`/`TotalTva`/`TotalTtc`) ; `InvoiceLine` (`ProductId`, **`StockLotId` obligatoire** — traçabilité BPD, FR-058 —, `Quantity`, `UnitPriceHt`, `VatRate`).

### CustomerReturn / ReturnLine (US8 — FR-060 à FR-062)
`CustomerReturn` (`ReturnNumber`, `SaleOrderId`, `CustomerId`, `ReturnDate`, `Status`, `Reason`, `CreditNoteId?`) ; `ReturnLine` (`SaleOrderLineId`, `OriginalStockLotId?`, `Quantity`, `Disposition` [RemiseEnStock \| Quarantaine \| Destruction], `Motif`).

### CreditNote (avoir)
`CreditNoteNumber`, `CustomerReturnId`, `Amount`, `IssuedAt`.

---

## 7. Prévision (MRP) & Réapprovisionnement (US10 — FR-063 à FR-067)

### ForecastParameter
`ProductId`, `SafetyStockDays`, `ConsumptionWindowDays` (défaut 90 — FR-063), `IsActive`, `ManualEstimatedConsumption?` (FR-066, produits sans historique).

### SupplierLeadTime
`ProductId`, `SupplierId`, `ManufactureDays`/`TransportDays`, `EffectiveDate`.

### ForecastCalculation (résultat quotidien du job Hangfire `StockForecastJob`)
`ProductId`, `CalcDate`, `AvgDailyConsumption`, `ReorderPoint`, `DaysOfStockRemaining`, `TotalLeadDays`, `Status` (OK \| Surveiller \| Urgent \| Critique — FR-067).

### ReorderSuggestion
`ProductId`, `SuggestionDate`, `OrderDeadline`, `SuggestedQuantity`, `Status` (EnAttente \| Converti \| Rejeté — FR-065), `ConvertedPurchaseOrderId?`.

---

## 8. Notifications & Conformité (US12, US13 — FR-076 à FR-083, FR-094)

### Notification (persistée — FR-094)
`EventType` (StockFaible \| Rupture \| PéremptionProche \| RetardLivraison \| RéceptionEnAttente \| QuarantaineProlongée \| SuggestionMrp \| ExpirationDpml \| SuspectéFalsifié), `TargetRoleOrPermission`, `Payload` (contexte métier), `CreatedAt`, `EmailSmsSentAt?`.

### NotificationRead (append-only — état lu/non lu par utilisateur, FR-078)
`NotificationId`, `UserId`, `ReadAt`.

### AuditLog (append-only, rétention illimitée — FR-089, FR-092)
`UserId`, `Action`, `EntityType`/`EntityId`, `Timestamp`, `IpAddress`, `UserAgent`, `Context` (méthode/chemin HTTP).

### RegulatoryAttachment (US13 — FR-080)
`AttachableType`/`AttachableId` (StockLot ou Shipment), `DocumentType` (Facture \| Douane \| Certificat \| AutorisationDpml), `FileReference`, `UploadedBy`, `UploadedAt`.

---

## 9. Configuration

### CompanyProfile (singleton)
`ValidationThresholdCfa` (seuil déclenchant la validation Direction — FR-023), `DefaultVatRate`, `AppName`, etc.

---

## Résumé des Contraintes Transverses Appliquées au Modèle

- **Soft delete exclusif** (Principe III) : `IsDeleted`/`DeletedAt` sur toute entité métier ci-dessus, sauf les entités explicitement append-only (`LoginAudit`, `NotificationRead`, `AuditLog`, `PurchaseOrderStatusHistory`, `StockMovement`) qui ne sont jamais soft-deleted car elles ne sont jamais modifiées après création.
- **Index unique partiel** `WHERE deleted_at IS NULL` sur toutes les colonnes d'unicité métier (`Product.Designation`, `Supplier.Name`, `Customer.Name`, etc.) — cohérent avec FR-002/FR-007/FR-008.
- **Aucun coefficient ni taux figé dans le code** : `PricingProfile`, `ExchangeRate` sont les seules sources de vérité (FR-045, FR-085).
- **Traçabilité FEFO** : `StockLot.ExpiryDate` + `QualityStatus = Libéré` + `QtyDisponible > 0` sont les trois filtres obligatoires de toute requête d'allocation (`StockLotRepository.GetFefoCandidates(productId, quantity)`), avant tri croissant par `ExpiryDate`.
