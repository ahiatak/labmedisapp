---
id: "LABMEDIS-99-CLARIFIER"
projet: "LABMEDIS"
type: "clarification"
titre: "99 — Questions Ouvertes & Points à Clarifier"
priorite: "Critique"
statut: "en attente"
source_raw: ["raw/PRD_CLAUDE.md §21", "raw/LABMEDIS-modele-donnees.md §2.1", "raw/Modelisation_Qwen.md"]
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#clarification", "#bloquant", "#po"]
---

# 99 — Questions Ouvertes & Points à Clarifier

> [!important] Ces questions DOIVENT être résolues avec LABMEDIS avant ou pendant le Sprint 1.
> Questions marquées **[BLOQUANT]** : l'implémentation NE PEUT PAS commencer sans réponse.
> Questions marquées **[IMPORTANT]** : impact sur la conception, une hypothèse a été retenue par défaut.

---

## 🔴 BLQ-001 — Date de figement du taux USD/XOF

**Question :** Le taux USD/XOF est-il figé à la date de (a) création de la commande d'achat, (b) date de réception physique, ou (c) date de paiement fournisseur ?

**Impact :** Calcul du PRU de chaque lot reçu. Si la date change, l'entité `purchase_orders.locked_exchange_rate_id` doit être repensée.

**Hypothèse retenue par défaut :** Date de création de la commande (cohérent avec EUR/XOF et pratique internationale standard).

**Fichiers affectés :** [[ENT-004-purchase-order]], [[RG-003-conversion-devise]], [[WF-001-achat-international]]

**Action requise :** Confirmation LABMEDIS avant Sprint 1.

---

## 🔴 BLQ-002 — TVA des réactifs de laboratoire HORIBA ABX

**Question :** Parmi les 93 références réactifs HORIBA ABX, lesquelles sont soumises à TVA 18% (`is_taxable = true`) ?

**Impact :** TVA incorrecte sur les factures de vente si non clarifiée. 93 produits à qualifier.

**Hypothèse retenue :** Flag `is_taxable` par produit (configurable individuellement) — déjà implémenté dans la structure `products.is_taxable`.

**Action requise :** Liste des réactifs taxables/exonérés fournie par LABMEDIS → import Excel de mise à jour.

**Fichiers affectés :** [[RG-007-tva]], [[ENT-001-product]], [[FR-011-reporting-dashboard]]

---

## 🔴 BLQ-003 — Fréquence de recalcul du PMP (CUMP)

**Question :** Le PMP est-il recalculé (a) à chaque réception de lot uniquement, ou (b) à chaque mouvement de stock sortant également ?

**Impact :** Performance (recalcul à chaque vente = coûteux sur 200+ ventes/jour) vs précision comptable.

**Hypothèse retenue :** Recalcul à chaque réception uniquement (moins coûteux, suffisant pour gestion dépositaire).

**Fichiers affectés :** [[RG-002-cump-pmp]], [[WF-002-reception-mise-en-stock]], [[ENT-009-product-price]]

---

## 🟠 BLQ-004 — Coefficients pricing par catégorie vs globaux

**Question :** Les coefficients (Commission ×1.25, Freight ×1.03, Transit ×1.09, Transfert ×1.07) sont-ils identiques pour TOUTES les catégories de produit, ou varient-ils par catégorie ?

**Contexte :** Seule la gamme France Lait (produits infantiles, transport maritime) est documentée avec ces valeurs. Les 93 réactifs labo (HORIBA ABX, transport aérien express) ont-ils des coefficients différents ?

**Hypothèse retenue :** Coefficients variables par `(CategoryId nullable, TransportMode)` — structure `PricingProfile` déjà prête pour ça.

**Action requise :** Tableau complet des coefficients par catégorie/transport fourni par LABMEDIS.

**Fichiers affectés :** [[RG-004-cascade-prix]], [[ENT-008-pricing-profile]]

---

## 🟠 IMP-001 — Granularité traçabilité (lot vs unité)

**Question :** La traçabilité est-elle requise au niveau **lot** (retenu) ou au niveau **unité individuelle** (numéro de série par boîte) ?

**Impact majeur si unité :** Restructuration complète de `stock_movements` + lecture scanner obligatoire + ~10x plus de données.

**Hypothèse retenue :** Traçabilité au niveau lot (standard pour dépositaire pharmaceutique de distribution).

**Fichiers affectés :** [[ENT-006-stock-lot]], [[ENT-007-stock-movement]], [[FR-005-entreposage-stock]]

---

## 🟠 IMP-002 — Portail répartiteurs (v2)

**Question :** Un accès applicatif web dédié aux 4 répartiteurs (CAMEG, LABOREX, UBIPHARM, TEDIS) est-il souhaité en v2, leur permettant de passer des commandes directement ?

**Impact si oui :** Architecture auth multi-tenant, isolation des données par client, module commande en ligne.

**Hypothèse retenue :** V1 = accès interne LABMEDIS uniquement. V2 = portail répartiteurs si confirmé.

**Fichiers affectés :** [[FR-007-clients-repartiteurs]], [[09-securite]], [[08-architecture]]

---

## 🟡 IMP-003 — Chaîne du froid (réactifs labo)

**Question :** Les réactifs HORIBA ABX nécessitent-ils une chaîne du froid (+2/+8°C) pour le stockage entrepôt ?

**Impact si oui :** Zone de stockage `ChaineDuFroid` activée, capteurs température à intégrer (ou alerte manuelle), alertes si température hors plage.

**Hypothèse retenue :** Présence de la zone `ChaineDuFroid` dans `storage_locations.location_type` — à activer si confirmé.

**Fichiers affectés :** [[ENT-015-warehouse-location]], [[FR-005-entreposage-stock]]

---

## 🟡 IMP-004 — Export comptable v1

**Question :** Une interface d'export vers un logiciel comptable existant (ex: Sage, QuickBooks) est-elle requise en v1 ?

**Impact si oui :** Module supplémentaire non prévu dans le périmètre actuel (1-2 sprints de plus).

**Hypothèse retenue :** V1 = export Excel/CSV des factures et avoirs. Intégration comptable directe = v2.

**Fichiers affectés :** [[FR-011-reporting-dashboard]], [[API-010-reporting]]

---

## 🟡 IMP-005 — Incoterm obligatoire ou optionnel

**Question :** L'Incoterm (EXW, FOB, CIF, DAP...) est-il obligatoire sur les commandes d'achat ou optionnel ?

**Impact :** Validation du formulaire de création de PO. Répartition des risques transport pour assurance.

**Hypothèse retenue :** Optionnel en v1, affiché sur le document PDF de commande.

**Fichiers affectés :** [[ENT-004-purchase-order]], [[FR-002-fournisseurs-achats]]

---

## 🟡 IMP-006 — Autorisation importation DPML

**Question :** Le numéro d'autorisation d'importation DPML doit-il être saisi pour CHAQUE expédition, ou seulement pour les médicaments ?

**Contexte réglementaire :** BPD Togo : autorisation de distribution requise par le DPML (Direction de la Pharmacie et du Médicament de Lomé).

**Hypothèse retenue :** Champ optionnel `import_authorization_ref` sur `shipments`, obligatoire pour médicaments (validation conditionnelle par catégorie).

**Fichiers affectés :** [[ENT-005-shipment]], [[FR-003-logistique-transport]], [[FR-014-documents-conformite]]

---

## 🟡 IMP-007 — Format numéro de lot interne

**Question :** Le format du numéro de lot interne LABMEDIS est-il libre ou normalisé ?

**Hypothèse retenue :** `{code_produit}-{AAAAMMJJ}-{NNN}` — ex: `FL400-20260115-001`. À valider.

**Fichiers affectés :** [[ENT-006-stock-lot]], [[FR-004-reception-lots]]

---

## ✅ Décisions Actées (Non Négociables)

Ces points ont été confirmés dans les sources primaires et NE SONT PAS à remettre en cause :

| Décision | Source |
|---|---|
| FEFO obligatoire — dérogation manuelle avec motif | PRD_Qwen-2 §2.4.4 |
| EUR/XOF = 655.957 FIXE | PRD_Qwen-1 §1.2 |
| Médicaments TVA 0% (directive UEMOA 06/2002) | PRD_CLAUDE §17.4 |
| Soft delete sur toutes les entités | PRD_Qwen-5 §5.10.4 |
| Logging ILoggerManager (NLog) exclusivement | PRD_CLAUDE §12.2 |
| Héritage Service/Repository | PRD_CLAUDE §12.1 |
| DTO Request montants en string | PRD_Qwen-1 §Règle A |
| AutoMapper INTERDIT | PRD_CLAUDE §12.3 |
| Hangfire pour jobs planifiés | PRD_Qwen-5 §5.23 |
| SignalR pour notifications temps réel | PRD_Qwen-5 §5.22 |

---

*Source : raw/PRD_CLAUDE.md §21 | raw/LABMEDIS-modele-donnees.md §2.1 | raw/PRD_Qwen - 5.md §5.3-5.10*
← [[_index|Hub LABMEDIS]] | ↑ [[../_meta/index|Index Global]]
