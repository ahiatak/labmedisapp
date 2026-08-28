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