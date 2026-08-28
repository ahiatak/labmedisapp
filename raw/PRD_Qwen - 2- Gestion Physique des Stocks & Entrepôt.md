Voici la section **2. Gestion Physique des Stocks & Entrepôt**, rédigée comme une spécification complète et exploitable pour le développement du projet avec :

- Frontend : **ReactJS** dans `./codebase/frontend`
- Backend : **.NET 9** dans `./codebase/backend`
- Architecture backend stricte : `Core`, `Service`, `Api`
- Règles d’or : héritage `Service : Repository`, soft delete, `ILoggerManager`, mapping manuel, DTO avec valeurs numériques/monétaires en `string`, Hangfire, SignalR.

---

# 2. 📦 Gestion Physique des Stocks & Entrepôt

## 2.1 Objectif du module

Le module **Gestion Physique des Stocks & Entrepôt** doit permettre à LABMEDIS de piloter avec précision :

1. La réception des produits importés ou achetés localement.
2. La gestion des produits par **lot**.
3. La gestion des **dates de péremption**.
4. Le stockage physique dans des **emplacements précis**.
5. Les mouvements de stock : entrée, sortie, transfert, ajustement, retour, destruction.
6. La disponibilité réelle des produits pour les ventes.
7. L’anticipation des ruptures et des péremptions.
8. La traçabilité complète de chaque produit, du fournisseur jusqu’au client.

Ce module est critique car LABMEDIS gère :

- des produits pharmaceutiques,
- des produits infantiles,
- des compléments alimentaires,
- des cosmétiques,
- des insecticides,
- des réactifs de laboratoire,
- des produits importés par avion, bateau ou express.

---

## 2.2 Périmètre fonctionnel couvert

Le module doit couvrir les processus suivants :

| Processus | Description |
|---|---|
| Réception fournisseur | Enregistrer l’arrivée d’un conteneur, d’un fret aérien, d’un express ou d’un achat local. |
| Création de lots | Chaque produit reçu doit être rattaché à un numéro de lot et à une date de péremption. |
| Mise en stock | Ranger les produits dans des emplacements physiques. |
| Gestion des emplacements | Identifier précisément où se trouve chaque produit dans l’entrepôt. |
| Gestion des conditionnements | Gérer les cartons, boîtes, flacons, tubes, sachets, etc. |
| Mouvements internes | Transférer des produits d’un emplacement à un autre. |
| Préparation de commande | Réserver, picker et sortir les produits selon une règle FEFO/FIFO. |
| Inventaire | Réaliser des inventaires complets ou partiels. |
| Gestion des péremptions | Alerter sur les lots proches de péremption. |
| Gestion des anomalies | Quarantaine, produits endommagés, manquants, détruits ou périmés. |
| Traçabilité | Historiser tous les mouvements avec utilisateur, date, motif et référence. |

---

## 2.3 Concepts métier principaux

### 2.3.1 Produit

Le produit est la fiche de référence commerciale et logistique.

Exemples présents dans les données :

- `France Lait 1er âge 400g`
- `France Lait 2ème âge 900g`
- `Pommade Maïa 100 ml`
- `ALLERGICA 10MG CPR B/30`
- `ABX PENTRA UREA CP`
- `B-PROTEI ALL 200g`

Un produit possède notamment :

| Champ | Description |
|---|---|
| Désignation | Nom commercial du produit. |
| Catégorie | Produit infantile, médicament, cosmétique, complément alimentaire, insecticide, réactif de laboratoire. |
| Forme | Boîte, flacon, tube, gel, sachet, etc. |
| Dosage | Exemple : `400g`, `100ml`, `10mg`, `30g`. |
| Conditionnement | Exemple : `carton/12`, `carton/6`, `carton/72`. |
| Classe thérapeutique | Antalgique, antibiotique, antihistaminique, lait infantile, etc. |
| Code CIP | Code identifiant produit, utile pour intégrations, étiquettes ou recherches. |
| Fournisseur | Fournisseur principal du produit. |
| TVA | Taux applicable, ex : 18% ou 0%. |
| Mode de transport par défaut | Maritime, aérien, express, terrestre. |
| Seuil de stock minimum | Quantité déclenchant une alerte de réapprovisionnement. |
| Délai de fabrication | Nombre de jours estimés chez le fabricant. |
| Délai de livraison | Nombre de jours estimés entre expédition et réception. |

---

### 2.3.2 Lot

Le lot est une notion obligatoire dans le domaine pharmaceutique.

Chaque réception de produit doit permettre d’enregistrer :

| Information | Description |
|---|---|
| Numéro de lot fournisseur | Numéro imprimé sur les produits. |
| Numéro de lot interne | Identifiant technique généré par le système si nécessaire. |
| Date de péremption | Date limite d’utilisation ou de vente. |
| Date de réception | Date d’entrée physique dans l’entrepôt. |
| Fournisseur | Fournisseur du lot. |
| Commande fournisseur | Commande d’achat associée. |
| Mode de transport | Bateau, avion, express, terrestre. |
| Statut qualité | En quarantaine, libéré, non conforme, périmé, détruit. |
| Prix de revient unitaire | Coût réel du lot après intégration des frais logistiques. |
| Quantité reçue | Quantité totale reçue. |
| Quantité restante | Quantité actuellement disponible. |

**Règle importante :**  
Un même produit peut exister dans plusieurs lots différents au même moment.

Exemple :

| Produit | Lot | Péremption | Quantité |
|---|---|---|---|
| France Lait 1er âge 400g | LOT-A123 | 10/2026 | 480 |
| France Lait 1er âge 400g | LOT-B456 | 02/2027 | 720 |

---

### 2.3.3 Emplacement

L’emplacement représente la position physique du produit dans l’entrepôt.

Exemple de structure possible :

```text
MAGASIN-01
├── ZONE-A
│   ├── ALLEE-01
│   │   ├── RACK-01
│   │   │   ├── NIVEAU-01
│   │   │   └── NIVEAU-02
│   │   └── RACK-02
│   └── ALLEE-02
├── ZONE-B
├── ZONE-FROID
├── ZONE-QUARANTAINE
└── ZONE-PERIMES
```

Un emplacement peut être de type :

| Type d’emplacement | Usage |
|---|---|
| Réception | Zone temporaire où les produits viennent d’arriver. |
| Quarantaine | Produits en attente de validation qualité. |
| Stockage | Emplacement principal de stockage. |
| Picking | Emplacement facile d’accès pour préparation de commande. |
| Réserve | Stock en hauteur ou en réserve. |
| Chaîne du froid | Produits nécessitant une température contrôlée. |
| Produits périmés | Zone isolée pour produits expirés. |
| Produits détruits | Emplacement logique avant destruction physique. |
| Transit | Produits en cours de transfert. |

**Règle importante :**  
Un même lot peut être stocké à plusieurs emplacements.

Exemple :

| Lot | Emplacement | Quantité |
|---|---|---|
| LOT-A123 | ZONE-A / ALLEE-01 / RACK-01 / NIVEAU-01 | 300 |
| LOT-A123 | ZONE-A / ALLEE-01 / RACK-02 / NIVEAU-03 | 180 |

---

### 2.3.4 Conditionnement et unités

Les fichiers produits montrent des conditionnements variables :

| Produit | Conditionnement |
|---|---|
| France Lait 1er âge 400g | carton/12 |
| France Lait 1er âge 900g | carton/6 |
| Pommade Maïa 100 ml | carton/72 |
| Pommade Maïa 250 ml | carton/48 |
| Strick Out Gel 30g | carton/200 |
| ALLERGICA 10MG CPR B/30 | carton/54 |
| Effermol inj perf 100ml | carton/50 |

Le système doit donc gérer plusieurs niveaux d’unités :

| Niveau | Exemple |
|---|---|
| Unité de base | La boîte, le flacon, le tube, la plaquette. |
| Carton | Regroupement de plusieurs unités. |
| Palette | Regroupement de plusieurs cartons, si nécessaire. |
| Colis express | Cas spécifique pour réception express. |

**Exemple :**

Produit : `France Lait 1er âge 400g`  
Conditionnement : `carton/12`

Si le magasin reçoit 40 cartons :

```text
Quantité reçue en cartons = 40
Quantité reçue en unités de base = 40 × 12 = 480 boîtes
```

Le système doit toujours enregistrer :

1. La quantité en conditionnement d’origine.
2. La quantité en unité de base.

Cela est indispensable car le fichier PRD mentionne :

> Si un carton a 40 produits, on enregistre 40 produits, mais on garde aussi que c’est venu dans des cartons.

---

## 2.4 Règles de gestion des stocks

### 2.4.1 Toute entrée en stock doit être tracée

Une entrée en stock peut provenir de :

| Source | Exemple |
|---|---|
| Commande fournisseur internationale | Conteneur France Lait reçu par bateau. |
| Commande fournisseur locale | Achat chez DEO GRATIAS PHARMA. |
| Retour client | Produit retourné par LABOREX TOGO ou UBIPHARM. |
| Ajustement inventaire | Correction après comptage physique. |
| Transfert entrant | Produit venant d’un autre magasin ou dépôt. |
| Régularisation | Correction d’erreur de saisie. |

Chaque entrée doit contenir :

- Produit.
- Lot.
- Date de péremption.
- Quantité.
- Emplacement cible.
- Utilisateur ayant effectué l’opération.
- Date et heure.
- Document source : commande, bon de livraison, inventaire, retour.
- Motif si ajustement.

---

### 2.4.2 Toute sortie de stock doit être tracée

Une sortie de stock peut provenir de :

| Source | Exemple |
|---|---|
| Commande client | Vente à LABOREX TOGO, CAMEG, TEDIS PHARMA, UBIPHARM. |
| Destruction | Produit périmé ou endommagé. |
| Perte | Produit perdu ou non retrouvé. |
| Ajustement inventaire | Correction après comptage. |
| Transfert sortant | Produit déplacé vers un autre emplacement ou dépôt. |
| Échantillon | Produit utilisé pour démonstration ou contrôle. |

Chaque sortie doit :

- Décrémenter la quantité du lot concerné.
- Mettre à jour l’emplacement concerné.
- Enregistrer l’utilisateur.
- Enregistrer la date.
- Enregistrer le motif.
- Empêcher une sortie si le stock disponible est insuffisant.

---

### 2.4.3 Gestion du stock disponible

Le système doit distinguer plusieurs quantités :

| Quantité | Définition |
|---|---|
| Stock physique | Quantité réellement présente dans l’entrepôt. |
| Stock réservé | Quantité réservée pour des commandes clients non encore livrées. |
| Stock disponible | Stock physique moins stock réservé. |
| Stock en quarantaine | Quantité non vendable. |
| Stock périmé | Quantité expirée, non vendable. |
| Stock attendu | Quantité commandée chez fournisseur mais non reçue. |
| Stock en transit | Quantité reçue mais pas encore mise en emplacement définitif. |

Formule de base :

```text
Stock disponible = Stock physique - Stock réservé - Stock bloqué
```

Exemple :

| Produit | Stock physique | Réservé | Quarantaine | Périmé | Disponible |
|---|---:|---:|---:|---:|---:|
| France Lait 1er âge 400g | 1200 | 240 | 0 | 0 | 960 |

---

### 2.4.4 Règle de sortie : FEFO par défaut

Dans le domaine pharmaceutique, la règle recommandée est :

```text
FEFO = First Expired, First Out
```

Cela signifie que le lot dont la date de péremption est la plus proche doit être proposé en premier lors d’une sortie.

Exemple :

| Lot | Date de péremption | Quantité disponible |
|---|---|---:|
| LOT-A | 30/09/2026 | 200 |
| LOT-B | 31/12/2026 | 500 |
| LOT-C | 30/06/2027 | 300 |

Si une commande client demande 150 unités de `France Lait 1er âge 400g`, le système doit proposer :

```text
LOT-A : 150 unités
```

Le système peut fonctionner de deux manières :

#### Option recommandée : Allocation automatique FEFO

Le backend sélectionne automatiquement les lots selon :

1. Date de péremption ascendante.
2. Emplacement prioritaire.
3. Statut libéré.
4. Quantité disponible.

#### Option alternative : Sélection manuelle

Le magasinier peut choisir manuellement un lot, mais seulement si :

- le lot n’est pas périmé,
- le lot n’est pas en quarantaine,
- la quantité est suffisante,
- l’action est journalisée,
- un motif peut être demandé si le lot choisi n’est pas le premier FEFO.

---

### 2.4.5 Gestion des dates de péremption

Le système doit gérer plusieurs statuts de péremption :

| Statut | Règle |
|---|---|
| Valide | Date de péremption future. |
| Proche péremption | Date dans un délai configurable : 30, 60, 90, 120 jours. |
| Périmé | Date dépassée. |
| Bientôt périmé chez client | Optionnel : anticipation si produit déjà livré. |

Règles :

1. Un produit périmé ne peut pas être vendu.
2. Un produit périmé peut être déplacé vers une zone `PERIMES`.
3. Un produit périmé peut être détruit via un mouvement de type `DESTRUCTION`.
4. Une alerte doit être générée avant péremption.
5. Les seuils doivent être configurables par catégorie ou par produit.

Exemple de seuils :

| Catégorie | Alerte si péremption dans |
|---|---:|
| Médicament | 90 jours |
| Produit infantile | 120 jours |
| Complément alimentaire | 90 jours |
| Réactif de laboratoire | 60 jours |
| Cosmétique | 90 jours |

---

### 2.4.6 Gestion de la quarantaine

Certains produits reçus peuvent ne pas être immédiatement disponibles à la vente.

Cas possibles :

- Contrôle qualité en attente.
- Produits endommagés pendant le transport.
- Écart de quantité.
- Température non respectée pour produits sensibles.
- Documents fournisseurs manquants.
- Doute sur authenticité ou lot.

Statuts possibles d’un lot :

| Statut | Effet |
|---|---|
| En réception | Produit reçu mais pas encore rangé. |
| En quarantaine | Produit non vendable. |
| Libéré | Produit vendable. |
| Non conforme | Produit bloqué. |
| Périmé | Produit non vendable. |
| Détruit | Produit sorti définitivement du stock vendable. |

Règle backend :

```text
Seuls les lots avec statut = Libéré peuvent être proposés à la vente.
```

---

### 2.4.7 Gestion des retours clients

Les retours clients doivent être gérés avec prudence.

Un retour peut être :

| Type | Règle |
|---|---|
| Retour acceptant remise en stock | Produit intact, lot valide, délai de retour respecté. |
| Retour en quarantaine | Produit à contrôler. |
| Retour refusé | Produit non réintégré. |
| Retour périmé | Produit à détruire. |
| Retour endommagé | Produit à détruire ou à régulariser. |

Un retour client doit toujours être lié à :

- la commande client d’origine,
- la ligne de commande d’origine,
- au lot d’origine si possible,
- au client,
- à la quantité retournée,
- au motif.

---

## 2.5 Gestion des emplacements

### 2.5.1 Adressage des emplacements

Chaque emplacement doit avoir un code unique.

Exemples :

```text
REC-01
QUAR-01
A-01-01-01
A-01-01-02
B-02-03-01
FROID-01
PERIM-01
DESTR-01
```

Interprétation possible :

```text
ZONE-ALLEE-RACK-NIVEAU-POSITION
```

Exemple :

```text
A-01-03-02-01
```

| Segment | Signification |
|---|---|
| A | Zone A |
| 01 | Allée 1 |
| 03 | Rack 3 |
| 02 | Niveau 2 |
| 01 | Position 1 |

---

### 2.5.2 Affectation d’un produit à un emplacement

Le système doit permettre :

1. Affectation manuelle par le magasinier.
2. Affectation par scan de code-barres.
3. Affectation suggérée par le système.
4. Affectation par défaut selon catégorie.
5. Affectation par zone température contrôlée si nécessaire.

Exemples de règles :

| Catégorie | Zone suggérée |
|---|---|
| Produit infantile | Zone sèche |
| Médicament | Zone pharma |
| Réactif de laboratoire | Zone labo / froid si requis |
| Cosmétique | Zone cosmétique |
| Insecticide | Zone spécifique |
| Produits périmés | Zone périmés |

---

### 2.5.3 Règles d’emplacement

Le backend doit pouvoir gérer :

| Règle | Description |
|---|---|
| Emplacement actif/inactif | Un emplacement peut être désactivé. |
| Emplacement verrouillé | Impossible d’y faire des mouvements. |
| Capacité maximale | Optionnel : limite de quantité ou volume. |
| Produit autorisé | Optionnel : certains emplacements réservés à certaines catégories. |
| Emplacement par défaut | Utilisé pour réception ou picking. |
| Emplacement de quarantaine | Obligatoire pour produits bloqués. |
| Emplacement de péremption | Obligatoire pour produits périmés. |

---

## 2.6 Gestion des mouvements de stock

### 2.6.1 Types de mouvements

Le système doit gérer au minimum les types suivants :

| Type de mouvement | Sens | Description |
|---|---|---|
| Réception fournisseur | Entrée | Arrivée d’une commande fournisseur. |
| Mise en stock | Entrée interne | Passage de réception à emplacement définitif. |
| Transfert | Interne | Déplacement entre deux emplacements. |
| Vente | Sortie | Sortie liée à une commande client. |
| Retour client | Entrée | Réintégration ou quarantaine. |
| Ajustement positif | Entrée | Correction inventaire positive. |
| Ajustement négatif | Sortie | Correction inventaire négative. |
| Destruction | Sortie | Produit détruit. |
| Perte | Sortie | Produit perdu. |
| Échantillon | Sortie | Produit utilisé comme échantillon. |
| Quarantaine | Blocage | Produit rendu non disponible. |
| Libération | Déblocage | Produit rendu disponible. |

---

### 2.6.2 Structure d’un mouvement

Chaque mouvement doit enregistrer :

| Champ | Description |
|---|---|
| Type | Réception, vente, transfert, ajustement, etc. |
| Date | Date du mouvement. |
| Utilisateur | Personne ayant effectué l’action. |
| Produit | Produit concerné. |
| Lot | Lot concerné. |
| Quantité | Quantité en unité de base. |
| Conditionnement | Quantité en cartons si applicable. |
| Emplacement source | Pour sortie ou transfert. |
| Emplacement destination | Pour entrée ou transfert. |
| Document source | Commande fournisseur, commande client, inventaire, retour. |
| Motif | Obligatoire pour ajustements, pertes, destructions. |
| Statut | Brouillon, validé, annulé. |

---

### 2.6.3 Interdictions importantes

Le backend doit refuser :

1. Une sortie supérieure à la quantité disponible.
2. Un mouvement sur lot périmé sauf déplacement vers zone périmé ou destruction.
3. Un mouvement sur lot en quarantaine sauf transfert vers quarantaine ou libération.
4. Un mouvement sans lot.
5. Un mouvement sans utilisateur authentifié.
6. Un mouvement sans emplacement source ou destination selon le type.
7. Un ajustement sans motif.
8. Une quantité négative.

---

## 2.7 Gestion des réceptions

### 2.7.1 Réception d’une commande fournisseur

Une réception peut être liée à :

- un conteneur maritime,
- une expédition aérienne,
- un envoi express,
- un achat local,
- un transfert interne.

Informations de réception :

| Champ | Exemple |
|---|---|
| Numéro de réception | REC-2026-000123 |
| Commande fournisseur | PO-2026-000456 |
| Fournisseur | CONTINENTAL COMMODITIES |
| Mode de transport | Maritime, aérien, express, terrestre |
| Numéro de conteneur | Conteneur 4 |
| BL / LTA / Document | Référence transport |
| Date de réception | 28/08/2026 |
| Statut | En cours, validée, close |

---

### 2.7.2 Réception par ligne produit

Chaque ligne reçue doit contenir :

| Champ | Exemple |
|---|---|
| Produit | France Lait 1er âge 400g |
| Quantité commandée | 100 cartons |
| Quantité reçue | 98 cartons |
| Quantité en unités | 1176 boîtes |
| Numéro de lot | LOT123456 |
| Date de péremption | 30/06/2027 |
| État | Conforme, manquant, endommagé, excédent |
| Emplacement cible | REC-01 ou A-01-02-01 |
| Mode de transport | Maritime |

---

### 2.7.3 Gestion des écarts de réception

Le système doit gérer :

| Écart | Règle |
|---|---|
| Quantité reçue inférieure | Statut partiellement reçu, reste à recevoir. |
| Quantité reçue supérieure | Demander validation, possibilité de refus ou régularisation. |
| Produit non commandé | Alerte et possibilité de refus ou réception exceptionnelle. |
| Produit endommagé | Mise en quarantaine ou refus. |
| Lot différent | Validation obligatoire. |
| Date de péremption courte | Alerte et validation responsable. |

---

## 2.8 Gestion des inventaires

### 2.8.1 Types d’inventaire

Le système doit permettre :

| Type | Description |
|---|---|
| Inventaire complet | Comptage de tout l’entrepôt. |
| Inventaire partiel | Comptage d’une zone, d’un rack, d’une catégorie. |
| Inventaire cyclique | Comptage régulier de certains produits. |
| Inventaire par lot | Comptage d’un lot spécifique. |
| Inventaire produit | Comptage d’un produit spécifique. |
| Inventaire emplacement | Comptage de tout ce qui se trouve dans un emplacement. |

---

### 2.8.2 Déroulement d’un inventaire

Étapes recommandées :

1. Création d’une session d’inventaire.
2. Sélection des produits, lots ou emplacements.
3. Gel des mouvements sur le périmètre concerné.
4. Saisie des quantités comptées.
5. Comparaison avec les quantités système.
6. Génération des écarts.
7. Validation des écarts par responsable.
8. Création automatique des ajustements.
9. Clôture de la session.
10. Historisation complète.

---

### 2.8.3 Écarts d’inventaire

| Type d’écart | Action possible |
|---|---|
| Quantité comptée > Quantité système | Ajustement positif avec motif. |
| Quantité comptée < Quantité système | Ajustement négatif avec motif. |
| Lot absent physiquement | Blocage ou perte. |
| Lot présent mais non prévu | Analyse puis intégration ou quarantaine. |
| Péremption différente | Correction de donnée lot avec audit. |

---

## 2.9 Gestion de la préparation de commande

### 2.9.1 Réservation de stock

Lorsqu’une commande client est validée, le système doit pouvoir réserver le stock.

Exemple :

| Produit | Commandé | Réservation |
|---|---:|---:|
| France Lait 1er âge 400g | 120 | 120 |
| Pommade Maïa 100 ml | 50 | 50 |

La réservation doit :

- être liée à la commande client,
- être liée à un lot,
- être liée à un emplacement,
- réduire le stock disponible,
- ne pas réduire immédiatement le stock physique tant que la sortie n’est pas validée.

---

### 2.9.2 Allocation FEFO automatique

Le backend doit proposer une allocation selon :

1. Date de péremption la plus proche.
2. Statut libéré.
3. Emplacement picking prioritaire.
4. Quantité disponible.
5. Emplacement le plus proche ou le plus logique.

Exemple d’algorithme :

```text
1. Récupérer tous les lots disponibles pour le produit.
2. Exclure lots périmés.
3. Exclure lots en quarantaine.
4. Exclure stock déjà réservé.
5. Trier par date de péremption ascendante.
6. Trier par emplacement prioritaire.
7. Allouer jusqu’à satisfaire la quantité demandée.
8. Si stock insuffisant, retourner erreur ou statut partiel.
```

---

### 2.9.3 Préparation physique

Le frontend doit permettre au magasinier de :

- scanner le produit,
- scanner le lot,
- scanner l’emplacement,
- confirmer la quantité,
- signaler un manquant,
- signaler un produit endommagé,
- imprimer un bon de préparation.

---

## 2.10 Gestion des étiquettes et codes-barres

Le système doit prévoir l’impression d’étiquettes pour :

1. Le produit.
2. Le lot.
3. La date de péremption.
4. L’emplacement.
5. Le carton.
6. La palette éventuelle.

Informations recommandées sur l’étiquette lot :

```text
Produit : France Lait 1er âge 400g
Code interne : PROD-000123
Lot : LOT123456
Péremption : 06/2027
Quantité : 12
Emplacement : A-01-02-03
Fournisseur : CONTINENTAL COMMODITIES
Réception : REC-2026-000123
```

Le frontend React devra intégrer une logique de :

- scan de code-barres,
- scan de QR code,
- génération de PDF d’étiquettes,
- prévisualisation d’impression.

---

## 2.11 Alertes et notifications

Le système doit générer des alertes pour :

| Alerte | Déclencheur |
|---|---|
| Stock faible | Quantité disponible inférieure au seuil. |
| Rupture | Quantité disponible = 0. |
| Péremption proche | Lot dans 30, 60, 90 ou 120 jours. |
| Produit périmé | Date de péremption dépassée. |
| Stock en quarantaine | Lots bloqués depuis trop longtemps. |
| Réception en retard | Commande fournisseur non reçue après date prévue. |
| Écart inventaire | Différence non justifiée. |
| Mouvement anormal | Sortie importante ou répétée. |
| Emplacement critique | Emplacement proche de saturation, si capacité gérée. |
| Produit à rotation lente | Produit non vendu depuis X jours. |

Ces alertes peuvent être :

- affichées dans le tableau de bord React,
- envoyées via SignalR,
- envoyées par email via `INotificationService`,
- générées par un job Hangfire.

---

## 2.12 Impact sur le Frontend React

Le frontend doit fournir des interfaces claires, rapides et orientées opération terrain.

### 2.12.1 Pages principales à prévoir

#### 1. Dashboard Stock

Vue globale avec :

- nombre de produits,
- valeur du stock,
- stock faible,
- lots proches péremption,
- lots périmés,
- produits en quarantaine,
- dernières réceptions,
- dernières sorties.

---

#### 2. Liste des produits

Table avec filtres :

- désignation,
- catégorie,
- fournisseur,
- forme,
- classe thérapeutique,
- stock disponible,
- stock réservé,
- seuil minimum,
- statut.

Actions :

- voir détail produit,
- voir lots,
- voir mouvements,
- créer ajustement,
- voir historique.

---

#### 3. Détail produit

Afficher :

- informations générales,
- conditionnements,
- stock par lot,
- stock par emplacement,
- mouvements récents,
- alertes,
- prix de vente,
- prix de revient,
- seuil de réapprovisionnement.

---

#### 4. Liste des lots

Table filtrable par :

- produit,
- numéro de lot,
- fournisseur,
- date de péremption,
- statut,
- emplacement,
- quantité restante.

Badges visuels :

- vert : disponible,
- orange : proche péremption,
- rouge : périmé,
- gris : quarantaine,
- bleu : réception en cours.

---

#### 5. Réception fournisseur

Écran de réception avec :

- sélection de la commande fournisseur,
- affichage des lignes attendues,
- saisie des quantités reçues,
- saisie des lots,
- saisie des dates de péremption,
- choix des emplacements,
- gestion des écarts,
- validation finale.

---

#### 6. Mise en stock

Écran permettant de déplacer les produits de la zone réception vers un emplacement.

Fonctions :

- scan produit,
- scan lot,
- scan emplacement,
- saisie quantité,
- confirmation.

---

#### 7. Transfert interne

Écran pour déplacer un lot :

- d’un emplacement à un autre,
- d’une zone à une autre,
- d’un statut à un autre.

---

#### 8. Préparation de commande

Écran de picking :

- liste des commandes à préparer,
- détail des lignes,
- lots proposés par FEFO,
- scan de validation,
- gestion des manquants,
- clôture préparation.

---

#### 9. Inventaire

Écran dédié :

- création session,
- sélection zone/produit/lot,
- saisie comptage,
- comparaison,
- validation,
- ajustement.

---

#### 10. Alertes et péremptions

Vue dédiée aux lots :

- périmés,
- proches péremption,
- stock faible,
- quarantaine,
- rotation lente.

---

### 2.12.2 Composants React recommandés

| Composant | Usage |
|---|---|
| DataGrid | Listes de produits, lots, mouvements. |
| Modal dynamique | Formulaires de réception, ajustement, transfert. |
| Toast | Notifications de succès/erreur. |
| Badge statut | Lot disponible, quarantaine, périmé. |
| Scanner input | Lecture code-barres ou QR code. |
| DatePicker | Saisie des dates de péremption. |
| Autocomplete | Recherche produit, emplacement, client, fournisseur. |
| Timeline | Historique des mouvements. |
| Kanban | Préparation de commande : à préparer, en cours, prêt, livré. |
| Chart | Rotation, péremptions, valeur stock. |
| PDF preview | Impression étiquettes, BL, fiches inventaire. |

---

### 2.12.3 Règles UX importantes

1. Les actions critiques doivent demander confirmation.
2. Les erreurs API doivent être affichées sous forme de toast.
3. Les champs numériques doivent accepter les saisies locales sans erreur.
4. Les quantités doivent être validées côté frontend avant envoi.
5. Les scans doivent remplir automatiquement les champs.
6. Les lots périmés doivent être visuellement bloqués.
7. Les emplacements doivent être recherchables par code.
8. Les mouvements doivent être historisés et consultables.
9. Les utilisateurs doivent voir uniquement ce que leur rôle autorise.
10. Les opérations de masse doivent afficher une barre de progression.

---

## 2.13 Impact sur le Backend .NET

Le backend doit implémenter les entités, services, repositories et contrôleurs nécessaires.

---

## 2.13.1 Entités principales à créer dans `LABMEDIS.Core`

Toutes les entités doivent hériter de `BaseEntity`.

#### `Product`

```csharp
public class Product : BaseEntity
{
    public string Designation { get; set; }
    public Guid CategoryId { get; set; }
    public string Form { get; set; }
    public string Dosage { get; set; }
    public string Packaging { get; set; }
    public string TherapeuticClass { get; set; }
    public string CipCode { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal? VatRate { get; set; }
    public int? MinStockThreshold { get; set; }
    public int? ManufacturingLeadTimeDays { get; set; }
    public int? DeliveryLeadTimeDays { get; set; }

    public Category Category { get; set; }
    public Supplier Supplier { get; set; }
    public ICollection<StockLot> StockLots { get; set; }
}
```

---

#### `StockLot`

```csharp
public class StockLot : BaseEntity
{
    public Guid ProductId { get; set; }
    public string SupplierBatchNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public DateOnly ReceptionDate { get; set; }
    public LotStatus Status { get; set; }
    public TransportMode? TransportMode { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public decimal UnitCost { get; set; }

    public Product Product { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public ICollection<StockLotLocation> StockLotLocations { get; set; }
    public ICollection<StockMovementLine> StockMovementLines { get; set; }
}
```

---

#### `StockLotLocation`

```csharp
public class StockLotLocation : BaseEntity
{
    public Guid StockLotId { get; set; }
    public Guid StorageLocationId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }

    public StockLot StockLot { get; set; }
    public StorageLocation StorageLocation { get; set; }
}
```

---

#### `StorageLocation`

```csharp
public class StorageLocation : BaseEntity
{
    public string Code { get; set; }
    public string Name { get; set; }
    public LocationType Type { get; set; }
    public Guid? ParentLocationId { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public int? Capacity { get; set; }

    public StorageLocation ParentLocation { get; set; }
    public ICollection<StockLotLocation> StockLotLocations { get; set; }
}
```

---

#### `StockMovement`

```csharp
public class StockMovement : BaseEntity
{
    public string Reference { get; set; }
    public StockMovementType Type { get; set; }
    public DateTime MovementDate { get; set; }
    public string UserId { get; set; }
    public string Reason { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public string SourceDocumentType { get; set; }
    public StockMovementStatus Status { get; set; }

    public ICollection<StockMovementLine> Lines { get; set; }
}
```

---

#### `StockMovementLine`

```csharp
public class StockMovementLine : BaseEntity
{
    public Guid StockMovementId { get; set; }
    public Guid ProductId { get; set; }
    public Guid StockLotId { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? DestinationLocationId { get; set; }
    public int Quantity { get; set; }

    public StockMovement StockMovement { get; set; }
    public Product Product { get; set; }
    public StockLot StockLot { get; set; }
    public StorageLocation SourceLocation { get; set; }
    public StorageLocation DestinationLocation { get; set; }
}
```

---

#### `InventorySession`

```csharp
public class InventorySession : BaseEntity
{
    public string Reference { get; set; }
    public InventoryType Type { get; set; }
    public InventoryStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string UserId { get; set; }
    public string Comments { get; set; }

    public ICollection<InventoryCount> Counts { get; set; }
}
```

---

#### `InventoryCount`

```csharp
public class InventoryCount : BaseEntity
{
    public Guid InventorySessionId { get; set; }
    public Guid ProductId { get; set; }
    public Guid StockLotId { get; set; }
    public Guid StorageLocationId { get; set; }
    public int SystemQuantity { get; set; }
    public int CountedQuantity { get; set; }
    public int Difference => CountedQuantity - SystemQuantity;
    public string AdjustmentReason { get; set; }
}
```

---

## 2.13.2 Enums recommandés

```csharp
public enum LotStatus
{
    EnReception = 0,
    Quarantaine = 1,
    Libere = 2,
    NonConforme = 3,
    Perime = 4,
    Detruit = 5
}
```

```csharp
public enum LocationType
{
    Reception = 0,
    Quarantaine = 1,
    Stockage = 2,
    Picking = 3,
    Reserve = 4,
    ChaineDuFroid = 5,
    Perimes = 6,
    Destruction = 7,
    Transit = 8
}
```

```csharp
public enum StockMovementType
{
    ReceptionFournisseur = 0,
    MiseEnStock = 1,
    Transfert = 2,
    Vente = 3,
    RetourClient = 4,
    AjustementPositif = 5,
    AjustementNegatif = 6,
    Destruction = 7,
    Perte = 8,
    Echantillon = 9,
    Quarantaine = 10,
    Liberation = 11
}
```

```csharp
public enum TransportMode
{
    Maritime = 0,
    Aerien = 1,
    Express = 2,
    Terrestre = 3
}
```

---

## 2.13.3 Repositories à prévoir

Selon l’architecture imposée :

```csharp
public interface IProductRepository : IBaseRepository<Product>
{
    Task<Product?> GetProductWithStockAsync(Guid productId);
}
```

```csharp
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetProductWithStockAsync(Guid productId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.StockLots)
                .ThenInclude(l => l.StockLotLocations)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
    }
}
```

```csharp
public interface IStockLotRepository : IBaseRepository<StockLot>
{
    Task<List<StockLot>> GetAvailableLotsForProductAsync(Guid productId);
    Task<StockLot?> GetLotWithLocationsAsync(Guid lotId);
}
```

```csharp
public class StockLotRepository : BaseRepository<StockLot>, IStockLotRepository
{
    public StockLotRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<StockLot>> GetAvailableLotsForProductAsync(Guid productId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(l => l.ProductId == productId
                && l.Status == LotStatus.Libere
                && !l.IsDeleted)
            .Include(l => l.StockLotLocations)
                .ThenInclude(sl => sl.StorageLocation)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync();
    }

    public async Task<StockLot?> GetLotWithLocationsAsync(Guid lotId)
    {
        return await _dbSet
            .Include(l => l.StockLotLocations)
                .ThenInclude(sl => sl.StorageLocation)
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == lotId && !l.IsDeleted);
    }
}
```

---

## 2.13.4 Services à prévoir

Services principaux :

```csharp
IProductService
IStockLotService
IStorageLocationService
IStockMovementService
IStockReceptionService
IStockAllocationService
IInventoryService
IStockAlertService
```

---

## 2.13.5 Exemple de service avec héritage obligatoire

```csharp
public interface IStockLotService : IStockLotRepository
{
    Task<List<StockLotResponse>> GetAvailableLotsAsync(Guid productId);
    Task<StockLotResponse> CreateLotAsync(CreateStockLotRequest request);
}
```

```csharp
public class StockLotService : StockLotRepository, IStockLotService
{
    private readonly ILoggerManager _logger;

    public StockLotService(AppDbContext context, ILoggerManager logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<List<StockLotResponse>> GetAvailableLotsAsync(Guid productId)
    {
        var lots = await GetAvailableLotsForProductAsync(productId);

        return lots.Select(l => new StockLotResponse(l)).ToList();
    }

    public async Task<StockLotResponse> CreateLotAsync(CreateStockLotRequest request)
    {
        var lot = request.ToStockLot();

        await InsertAsync(lot);
        await SaveAsync();

        return new StockLotResponse(lot);
    }
}
```

---

## 2.13.6 DTO Requests avec champs numériques en string

Conformément à la règle d’or, les quantités et valeurs décimales doivent être en `string` dans les Requests.

```csharp
public class CreateStockLotRequest
{
    [SwaggerSchema(Description = "Identifiant du produit")]
    public Guid ProductId { get; set; }

    [SwaggerSchema(Description = "Numéro de lot fournisseur")]
    public string SupplierBatchNumber { get; set; }

    [SwaggerSchema(Description = "Date de péremption au format yyyy-MM-dd")]
    public string ExpiryDate { get; set; }

    [SwaggerSchema(Description = "Quantité reçue en unités de base")]
    public string Quantity { get; set; }

    [SwaggerSchema(Description = "Quantité reçue en cartons")]
    public string CartonQuantity { get; set; }

    [SwaggerSchema(Description = "Statut initial du lot")]
    public string Status { get; set; }

    public StockLot ToStockLot()
    {
        return new StockLot
        {
            ProductId = ProductId,
            SupplierBatchNumber = SupplierBatchNumber,
            ExpiryDate = DateOnly.ParseExact(ExpiryDate, "yyyy-MM-dd"),
            Status = Enum.Parse<LotStatus>(Status),
            ReceptionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }
}
```

---

## 2.13.7 DTO Responses avec constructeur depuis entité

```csharp
public class StockLotResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductDesignation { get; set; }
    public string SupplierBatchNumber { get; set; }
    public string ExpiryDate { get; set; }
    public string Status { get; set; }
    public int AvailableQuantity { get; set; }

    public StockLotResponse(StockLot entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        SupplierBatchNumber = entity.SupplierBatchNumber;
        ExpiryDate = entity.ExpiryDate.ToString("yyyy-MM-dd");
        Status = entity.Status.ToString();

        AvailableQuantity = entity.StockLotLocations?
            .Where(x => !x.IsDeleted)
            .Sum(x => x.Quantity - x.ReservedQuantity) ?? 0;
    }
}
```

---

## 2.13.8 Endpoints API recommandés

### Produits et stock

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/products` | Liste des produits avec stock. |
| GET | `/api/products/{id}` | Détail produit. |
| GET | `/api/products/{id}/stock` | Stock du produit par lot et emplacement. |
| GET | `/api/products/{id}/movements` | Mouvements du produit. |
| GET | `/api/products/{id}/lots` | Lots du produit. |

---

### Lots

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/stock-lots` | Liste des lots. |
| GET | `/api/stock-lots/{id}` | Détail d’un lot. |
| GET | `/api/stock-lots/expiring` | Lots proches péremption. |
| POST | `/api/stock-lots` | Créer un lot. |
| PUT | `/api/stock-lots/{id}/status` | Changer statut : quarantaine, libération, etc. |
| PUT | `/api/stock-lots/{id}/location` | Déplacer un lot vers un emplacement. |

---

### Mouvements

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/stock-movements` | Liste des mouvements. |
| GET | `/api/stock-movements/{id}` | Détail mouvement. |
| POST | `/api/stock-movements/transfer` | Transfert interne. |
| POST | `/api/stock-movements/adjustment` | Ajustement positif/négatif. |
| POST | `/api/stock-movements/destruction` | Destruction. |
| POST | `/api/stock-movements/loss` | Perte. |

---

### Réceptions

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/stock-receptions` | Liste des réceptions. |
| GET | `/api/stock-receptions/{id}` | Détail réception. |
| POST | `/api/stock-receptions` | Créer une réception. |
| POST | `/api/stock-receptions/{id}/validate` | Valider réception. |
| POST | `/api/stock-receptions/{id}/put-away` | Mise en stock. |

---

### Inventaires

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/inventories` | Liste sessions inventaire. |
| GET | `/api/inventories/{id}` | Détail inventaire. |
| POST | `/api/inventories` | Créer session inventaire. |
| POST | `/api/inventories/{id}/counts` | Saisir comptages. |
| POST | `/api/inventories/{id}/validate` | Valider inventaire. |
| POST | `/api/inventories/{id}/close` | Clôturer inventaire. |

---

## 2.13.9 Exemple de contrôleur

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockLotsController : ControllerBase
{
    private readonly IStockLotService _stockLotService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public StockLotsController(
        IStockLotService stockLotService,
        ILoggerManager logger,
        IUserService userService)
    {
        _stockLotService = stockLotService;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetAvailableLots(Guid productId)
    {
        var user = await _userService.GetCurrentUserAsync(User);

        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début GetAvailableLots | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try
        {
            var result = await _stockLotService.GetAvailableLotsAsync(productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec GetAvailableLots : {ex.Message} | IP: {Request.GetIp()}");

            return BadRequest(new
            {
                message = "Impossible de récupérer les lots disponibles."
            });
        }
    }
}
```

---

## 2.14 Logique d’allocation FEFO côté backend

Le service d’allocation doit proposer les lots à sortir selon FEFO.

Exemple :

```csharp
public class StockAllocationService : StockLotRepository, IStockAllocationService
{
    public async Task<List<StockAllocationResult>> AllocateStockAsync(Guid productId, int requestedQuantity)
    {
        var lots = await GetAvailableLotsForProductAsync(productId);

        var result = new List<StockAllocationResult>();
        int remainingQuantity = requestedQuantity;

        foreach (var lot in lots)
        {
            if (remainingQuantity <= 0)
                break;

            var availableQuantity = lot.StockLotLocations
                .Where(x => !x.IsDeleted)
                .Sum(x => x.Quantity - x.ReservedQuantity);

            if (availableQuantity <= 0)
                continue;

            int allocatedQuantity = Math.Min(availableQuantity, remainingQuantity);

            result.Add(new StockAllocationResult
            {
                StockLotId = lot.Id,
                LotNumber = lot.SupplierBatchNumber,
                ExpiryDate = lot.ExpiryDate,
                AllocatedQuantity = allocatedQuantity
            });

            remainingQuantity -= allocatedQuantity;
        }

        if (remainingQuantity > 0)
        {
            throw new BusinessException($"Stock insuffisant pour le produit demandé. Quantité manquante : {remainingQuantity}");
        }

        return result;
    }
}
```

---

## 2.15 Jobs Hangfire à prévoir

Le module stock doit inclure des traitements différés ou planifiés.

### Job 1 : Alerte péremption

```text
StockExpiryAlertJob
```

Fréquence recommandée : quotidienne.

Actions :

1. Scanner tous les lots libérés.
2. Identifier les lots dont la péremption est proche.
3. Créer une notification.
4. Envoyer une alerte SignalR.
5. Envoyer éventuellement un email.

---

### Job 2 : Stock faible

```text
LowStockAlertJob
```

Actions :

1. Calculer le stock disponible par produit.
2. Comparer avec le seuil minimum.
3. Créer une alerte.
4. Proposer une commande de réapprovisionnement.

---

### Job 3 : Rotation lente

```text
SlowMovingStockJob
```

Actions :

1. Identifier les produits sans mouvement depuis 30, 60 ou 90 jours.
2. Calculer la valeur immobilisée.
3. Générer un rapport.

---

### Job 4 : Clôture automatique des lots périmés

```text
ExpiredLotBlockingJob
```

Actions :

1. Rechercher les lots dont la date de péremption est dépassée.
2. Passer leur statut à `Perime`.
3. Empêcher leur allocation.
4. Créer une alerte.

---

## 2.16 Notifications SignalR à prévoir

Le backend doit pousser en temps réel :

| Événement | Description |
|---|---|
| StockLotCreated | Un nouveau lot a été créé. |
| StockMovementValidated | Un mouvement a été validé. |
| LowStockDetected | Produit sous seuil. |
| ExpiringLotDetected | Lot proche péremption. |
| ExpiredLotDetected | Lot périmé. |
| ReceptionValidated | Réception validée. |
| InventoryDiscrepancyDetected | Écart inventaire détecté. |
| StockReservationCreated | Stock réservé pour commande. |

---

## 2.17 Règles de sécurité et audit

Toutes les opérations de stock doivent être auditées.

### Informations à journaliser

| Donnée | Obligatoire |
|---|---|
| Utilisateur | Oui |
| Date et heure | Oui |
| Action | Oui |
| Produit | Oui |
| Lot | Oui |
| Quantité | Oui |
| Emplacement | Oui |
| Motif | Oui pour ajustements, pertes, destructions |
| IP | Oui |
| UserAgent | Oui |
| Document source | Si applicable |

### Format de log imposé

Dans chaque action API :

```csharp
_logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début [NomAction] | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");
```

Dans le catch :

```csharp
_logger.LogError(ex, $"{user?.LastName} ... | Echec [NomAction] : {ex.Message} | IP: {Request.GetIp()}");
```

---

## 2.18 Règles de validation métier

Le backend doit rejeter les opérations invalides avec des messages clairs.

| Cas | Message recommandé |
|---|---|
| Quantité négative | `La quantité ne peut pas être négative.` |
| Quantité supérieure au stock | `La quantité demandée est supérieure au stock disponible.` |
| Lot périmé | `Ce lot est périmé. Il ne peut pas être utilisé pour une vente.` |
| Lot en quarantaine | `Ce lot est en quarantaine. Il doit être libéré avant utilisation.` |
| Emplacement invalide | `L’emplacement sélectionné est invalide ou inactif.` |
| Motif manquant | `Un motif est obligatoire pour ce type de mouvement.` |
| Date péremption invalide | `La date de péremption doit être future pour une réception normale.` |
| Produit introuvable | `Le produit demandé est introuvable.` |
| Lot introuvable | `Le lot demandé est introuvable.` |
| Ajustement non autorisé | `Votre profil ne permet pas de valider cet ajustement.` |

---

## 2.19 Points à valider avec LABMEDIS

Même si cette section est complète techniquement, certains points métier devront être confirmés avec le client avant développement définitif :

| Question | Impact |
|---|---|
| Un produit peut-il être vendu avant sa mise en stock définitive ? | Impact sur statut réception et stock disponible. |
| La règle FEFO doit-elle être strictement automatique ou modifiable ? | Impact sur allocation des lots. |
| Faut-il gérer plusieurs entrepôts physiques ? | Impact multi-magasins et transferts. |
| Faut-il gérer la chaîne du froid pour certains réactifs HORIBA ? | Impact emplacement et alertes température. |
| Les cartons peuvent-ils être ouverts et revendus à l’unité ? | Impact conversion carton vers unité. |
| Un carton peut-il contenir plusieurs produits différents ? | Impact réception et étiquetage. |
| Faut-il bloquer la vente d’un produit si péremption inférieure à X jours ? | Impact règles métier. |
| Les ajustements d’inventaire nécessitent-ils une validation à deux niveaux ? | Impact rôles et workflow. |
| Faut-il imprimer des étiquettes code-barres ou QR codes ? | Impact module impression. |
| Faut-il une interface mobile PDA pour le magasin ? | Impact design React responsive. |

---

## 2.20 Synthèse des exigences du module

Le module **Gestion Physique des Stocks & Entrepôt** doit permettre à LABMEDIS de :

1. Recevoir les produits par lot.
2. Enregistrer les numéros de lot et dates de péremption.
3. Stocker les produits dans des emplacements précis.
4. Gérer les conditionnements carton/boîte/flacon.
5. Suivre les quantités physiques, réservées et disponibles.
6. Appliquer FEFO par défaut.
7. Bloquer les lots périmés ou en quarantaine.
8. Tracer chaque mouvement de stock.
9. Réaliser des inventaires fiables.
10. Alerter sur stock faible, péremption et rotation lente.
11. Fournir au frontend React des interfaces simples, rapides et scannables.
12. Fournir au backend .NET des services robustes, audités et conformes à l’architecture imposée.