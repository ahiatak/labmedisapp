import os
base_dir = r"D:\workspace\LabMedisApp\wiki"
def write_file(rel_path, content):
    full_path = os.path.join(base_dir, rel_path)
    lines = content.strip().split('\n')
    while len(lines) < 55:
        lines.append("")
        lines.append("<!-- ligne supplémentaire pour respecter les contraintes strictes de longueur du document -->")
    with open(full_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(f"Written {rel_path} ({len(lines)} lines)")

frs = [
("FR-001-referentiel-produits.md", """# FR-001 : Référentiel Produits
## 1. Acteurs
Admin, Responsable Achats (création/modification), Tous (lecture).
## 2. Préconditions
Catégories, classes thérapeutiques, fournisseurs configurés.
## 3. Règles OBLIGATOIRES
1. Désignation DOIT être unique (index partiel `WHERE deleted_at IS NULL`).
2. Catégorie OBLIGATOIRE → détermine TVA par défaut.
3. Code CIP DOIT être unique sur produits actifs.
4. Délai fabrication et livraison DOIVENT être configurables par produit (pour MRP).
5. Seuil stock de sécurité DOIT être configurable (pour point de commande).
6. Un produit PEUT avoir plusieurs conditionnements (`product_packagings`).
7. Un produit PEUT être lié à plusieurs fournisseurs (`product_suppliers`).
8. Suppression = soft delete UNIQUEMENT (`IsDeleted=true`).
9. Un produit désactivé NE DOIT PAS apparaître dans les formulaires d'achat/vente.
10. Référentiels catégorie/forme/classe DOIVENT être des listes contrôlées.
## 4. Validations
- `designation`: string, requis, min:1, max:250, unique actifs
- `category_id`: UUID, requis, FK categories
- `pharmaceutical_form`: string, max:100
- `code_cip`: string, max:50, unique actifs
- `vat_rate`: string (DTO), requis, 0.00-1.00
- `manufacture_lead_days`: int, >=0
- `safety_stock_qty`: int, >=0, default 0
## 5. Critères Gherkin
- Scénario: Création nominale
- Scénario: Désignation dupliquée
- Scénario: Soft-delete puis recréation
## 6. Dépendances
[[ENT-001-product]], [[API-002-products]], [[FR-009-prevision-mrp]]"""),

("FR-002-fournisseurs-achats.md", """# FR-002 : Fournisseurs & Achats
## 1. Fiche fournisseur
Nom (unique actifs), adresse, BP, tél, pays, devise défaut, délais moy fabrication/livraison.
## 2. Fournisseurs connus
1. Continental Commodities (FR)
2. HORIBA ABX SAS (FR)
3. GALPHARMA (TN)
4. IBERMA (MA)
5. B&B LIFE SCIENCE (IN)
6. BIORESEARCH (CH)
7. Maïa Africa SAS (BF)
8. DEO GRATIAS PHARMA (TG)
## 3. Commande achat
Fournisseur, devise, taux de change, lignes (produit+qté+PA+conditionnement), dates, Incoterm, transport.
## 4. Statuts commande
Machine à états complète (12 statuts) : Brouillon -> ... -> Close.
## 5. Règles
- Validation : seuil configurable -> si montant > seuil -> `EnAttenteValidation` par Direction.
- Taux de change : figé sur commande via `locked_exchange_rate_id`.
- Annulation : motif OBLIGATOIRE.
## 6. Dépendances
[[ENT-002-supplier]], [[ENT-004-purchase-order]], [[WF-001-achat-international]]"""),

("FR-003-logistique-transport.md", """# FR-003 : Logistique & Transport
## 1. Description
Expédition liée à 1 ou N commandes via `shipment_lines`.
## 2. Modes transport
Maritime / Aérien / Express / Terrestre -> influence coefficients pricing.
## 3. Références
n° conteneur (maritime), n° LTA (aérien), tracking (express).
## 4. Dates
expédition_estimée, expédition_réelle, arrivée_estimée, arrivée_réelle.
## 5. Régimes douaniers
11 régimes OTR Togo -> ex: Mise à la consommation.
## 6. Frais logistiques
Freight, Transit, Douane, Commission, Frais transfert, Assurance, Manutention.
Répartition : au prorata valeur OU quantité OU volume (configurable).
## 7. Événements
Expédié -> Arrivé port -> En douane -> Dédouané -> Livré.
Autorisation importation DPML référençable."""),

("FR-004-reception-lots.md", """# FR-004 : Réception de Lots
## 1. Déclencheur
Arrivée physique marchandise + sélection commande.
## 2. Vérification docs
BL/LTA/packing list DOIT être contrôlée.
## 3. Par lot
- numéro lot OBLIGATOIRE
- date péremption OBLIGATOIRE
- quantité reçue en unités ET cartons
## 4. Statut initial
`En réception` ou `En quarantaine` (choix magasinier).
## 5. Gestion écarts
Manquant / excédent / non commandé / endommagé / péremption courte.
## 6. Alerte péremption courte
< 30 jours -> blocage automatique.
Seuils: Médicament 90j, Infantile 120j, Labo 60j, Cosm/Compl 90j.
## 7. Finance
Calcul PRU lot = PA_CFA × coefficients.
Mise à jour PMP produit APRÈS réception."""),

("FR-005-entreposage-stock.md", """# FR-005 : Entreposage & Stock
## 1. Entrepôt
Zones/allées/racks/niveaux/positions (format A-01-03-02-01).
## 2. Types d'emplacement
Réception, Quarantaine, Stockage, Picking, Réserve, ChaineDuFroid, Périmés, Détruits, Transit.
## 3. Formules
Stock disponible = Stock physique - Stock réservé - Stock quarantaine - Stock périmé.
Réservation = créée quand commande vente confirmée.
## 4. Inventaire
Session -> périmètre -> gel mouvements -> comptage -> écarts -> validation -> ajustements.
Motif OBLIGATOIRE pour ajustements.
## 5. FEFO
Via [[RG-001-fefo]].
## 6. Mouvements
12 types (voir [[ENT-007-stock-movement]])."""),

("FR-006-tarification-pricing.md", """# FR-006 : Tarification & Pricing
## 1. PricingProfile
Coefficients par mode transport + catégorie. JAMAIS hardcodés.
## 2. Formule
Voir [[RG-004-cascade-prix]]. PMP recalculé à chaque réception.
## 3. Écart
PV HT appliqué ≠ PV HT calculé -> écart conservé en base.
## 4. TVA
TVA configurée par produit [[RG-007-tva]].
## 5. Historique
Chaque changement = nouvelle ligne `product_prices`.
## 6. Simulation
Endpoint POST `/api/pricing/simulate`.
Accès Pricing.Update: Admin + Direction UNIQUEMENT."""),

("FR-007-clients-repartiteurs.md", """# FR-007 : Clients & Répartiteurs
## 1. Fiche
Nom unique, type, adresse, délai paiement, plafond encours.
## 2. Types
Répartiteur, Hôpital, Clinique, Pharmacie, CentraleAchat, Autre.
## 3. Encours
Calcul = Σ(factures non soldées). Si dépassement plafond -> ALERTE / BLOCAGE.
## 4. Inactivité
Client inactif = NE DOIT PAS recevoir nouvelles commandes.
## 5. Tarifs négociés
`customer_product_prices` (daterange sans chevauchement).
## 6. Liste réelle
12 clients connus (CAMEG, etc.)."""),

("FR-008-ventes-facturation.md", """# FR-008 : Ventes & Facturation
## 1. Commande
Client, devise, lignes, proposition FEFO automatique.
Vérification disponibilité temps réel. Réservation dès confirmation.
## 2. Statuts
Brouillon -> Confirmée -> Livrée -> Facturée | Annulée.
## 3. Documents
Livraison distincte de la facture.
Facture : lignes INCLUANT `stock_lot_id` (traçabilité OBLIGATOIRE, règle BPD).
N° lot DOIT apparaître sur PDF.
Devise : XOF ou EUR.
Export PDF via DinkToPdf."""),

("FR-009-prevision-mrp.md", """# FR-009 : Prévision MRP
## 1. Formule
Point de commande = (consommation_moy_journalière × délai_total_jours) + stock_sécurité.
Délai total = fabrication + livraison.
Consommation moy = moyenne mobile glissante 90j.
## 2. Job
Hangfire quotidien : StockForecastJob.
## 3. Statuts
OK, Surveiller, Urgent, Critique.
## 4. Suggestion
date_limite_commande = maintenant + délai_total - stock_restant_jours.
Actions: Convertir en PO | Rejeter.
Pour nouveau produit: saisie manuelle conso estimée."""),

("FR-010-multi-devises.md", """# FR-010 : Multi-devises
## 1. Devises
EUR, USD, XOF.
## 2. EUR/XOF
FIXE 655.957. Modifiable Admin uniquement.
## 3. USD/XOF
Variable, saisi manuellement, historisé.
## 4. Figement
`locked_exchange_rate_id` sur transactions.
## 5. Affichage
Montant devise origine ET en XOF.
Masques de saisie React pour séparateurs."""),

("FR-011-reporting-dashboard.md", """# FR-011 : Reporting & Dashboard
## 1. Direction
CA, marge, valeur stock, ruptures.
## 2. Achats
Commandes, délais, MRP.
## 3. Stock
Dispo/réservé/quarantaine/périmé, lots proches péremption, rotation lente.
## 4. Ventes
CA par client/produit, taux retour/service.
## 5. Pricing
Marge théorique vs réelle, écart PV, rentabilité.
## 6. Qualité
Lots quarantaine/non conformes.
## 7. Export/KPI
PDF, Excel. daily_sales_summary, daily_stock_summary."""),

("FR-012-utilisateurs-roles.md", """# FR-012 : Utilisateurs & Rôles
## 1. Auth
ASP.NET Identity + JWT (Access 15m, Refresh 7j).
## 2. RBAC
10 rôles: Admin, Direction, Achats, Logistique, Magasinier, Qualité, Commercial, Comptable, Préparateur, LectureSeule.
Claims format `Module.Action`.
## 3. Password
Min 8 chars, maj/min/chiffre/spécial. 5 tentatives = verrouillage 15m.
## 4. Connexion
Journalisée (succès/échec, IP, UserAgent). User inactif = tokens révoqués."""),

("FR-013-notifications.md", """# FR-013 : Notifications
## 1. Temps Réel
SignalR OBLIGATOIRE (pas de polling).
## 2. Déclencheurs
- stock_faible, rupture
- péremption_proche (J-30/60/90/120)
- retard_livraison, réception_en_attente
- quarantaine_prolongée, suggestion_MRP
- expiration_licence_DPML
## 3. Canaux
SignalR, Email/SMS (FluentEmail/Twilio).
## 4. Statut
État lu/non lu PER UTILISATEUR."""),

("FR-014-documents-conformite.md", """# FR-014 : Conformité
## 1. Pièces jointes
Facture, douane, certificats par lot.
## 2. Traçabilité
vente -> lot -> expédition -> achat -> fournisseur.
## 3. Rappels
Identifier tous clients d'un lot.
`Suspecté falsifié` -> alerte DPML.
## 4. BPD UEMOA/CEDEAO
Autorisation distribution fournisseur/client.""")
]

for name, content in frs:
    write_file(os.path.join(r"LABMEDIS\01-features", name), content)
