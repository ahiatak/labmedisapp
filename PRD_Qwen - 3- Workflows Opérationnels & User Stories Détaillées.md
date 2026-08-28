# 3. Workflows Opérationnels & User Stories Détaillées

Cette section décrit les processus opérationnels de **LABMEDIS** sous forme de workflows détaillés et de user stories exploitables par l’équipe de développement.

Elle couvre l’ensemble du cycle de vie métier :

1. Gestion des données de référence.
2. Achats internationaux.
3. Suivi logistique et transport.
4. Réception en entrepôt.
5. Contrôle qualité et mise en stock.
6. Gestion des lots et péremptions.
7. Ventes aux répartiteurs et structures de santé.
8. Préparation, livraison et facturation.
9. Retours clients et avoirs.
10. Anticipation des commandes et réapprovisionnement.
11. Inventaires et ajustements.
12. Pilotage financier et reporting.

---

## 3.1 Acteurs et rôles du système

Le système doit gérer plusieurs profils utilisateurs. Chaque profil aura des permissions spécifiques dans le frontend React et des rôles d’autorisation dans le backend .NET.

| Rôle | Description | Responsabilités principales |
|---|---|---|
| Administrateur | Gestion technique et paramétrage du système. | Utilisateurs, rôles, permissions, logs, configuration générale. |
| Direction / Manager | Pilotage stratégique. | Validation des prix, marges, commandes importantes, reporting. |
| Acheteur / Responsable import | Gestion des achats internationaux. | Création des commandes fournisseurs, suivi transport, devises, coûts. |
| Responsable logistique | Suivi des flux physiques. | Conteneurs, fret aérien, maritime, express, transit, réception. |
| Magasinier | Gestion physique de l’entrepôt. | Réception, mise en stock, emplacements, inventaire, préparation. |
| Responsable qualité | Validation des lots. | Quarantaine, libération, non-conformité, péremption. |
| Commercial / Vente | Relation clients. | Devis, commandes clients, prix, disponibilité, livraisons. |
| Comptable | Suivi financier. | Factures, avoirs, TVA, exports, validation des coûts. |
| Préparateur | Préparation physique des commandes. | Picking, scan, colisage, livraison. |

---

## 3.2 Macro-processus global LABMEDIS

Le flux global peut être résumé ainsi :

```text
Fabricant / Fournisseur international
        ↓
Commande d’achat LABMEDIS
        ↓
Transport : Bateau / Avion / Express / Terrestre
        ↓
Transit / Douane
        ↓
Réception entrepôt
        ↓
Contrôle qualité / Quarantaine
        ↓
Mise en stock par lot et emplacement
        ↓
Calcul du prix de revient réel
        ↓
Disponibilité à la vente
        ↓
Commande client : répartiteur, clinique, hôpital, pharmacie
        ↓
Préparation / Picking FEFO
        ↓
Livraison
        ↓
Facturation
        ↓
Suivi paiement / retours / avoirs
        ↓
Analyse : stock, marge, rotation, prévisions
```

---

## 3.3 Workflow 1 : Gestion des données de référence

### 3.3.1 Objectif

Avant toute opération d’achat, de stock ou de vente, le système doit disposer de données de référence fiables :

- produits,
- catégories,
- fournisseurs,
- clients,
- conditionnements,
- devises,
- taux de change,
- prix,
- taux de TVA,
- délais de fabrication,
- délais de livraison,
- seuils de stock.

---

### 3.3.2 Workflow

```text
Création ou modification d’une donnée de référence
        ↓
Contrôle des informations obligatoires
        ↓
Validation métier
        ↓
Enregistrement en base
        ↓
Historisation / Audit
        ↓
Disponibilité dans les modules Achats, Stock, Vente
```

---

### 3.3.3 Règles de gestion

| Donnée | Règle |
|---|---|
| Produit | Une désignation unique doit être définie. |
| Catégorie | Chaque produit doit appartenir à une catégorie. |
| Fournisseur | Un fournisseur doit avoir un pays et une devise par défaut. |
| Client | Un client doit avoir au minimum un nom et une ville. |
| Conditionnement | Le produit peut avoir plusieurs niveaux de conditionnement. |
| TVA | Le taux de TVA doit être défini par catégorie ou produit. |
| Devise | Les devises de référence sont EUR, USD et XOF. |
| Délais | Les délais de fabrication et livraison doivent être configurables. |

---

### 3.3.4 Exemples issus des données fournies

#### Produits

| Produit | Catégorie | Fournisseur |
|---|---|---|
| France Lait 1er âge 400g | Produit infantile | CONTINENTAL COMMODITIES |
| France Lait 2ème âge 900g | Produit infantile | CONTINENTAL COMMODITIES |
| Pommade Maïa 100 ml | Cosmétique | MAIA AFRICA SAS |
| ALLERGICA 10MG CPR B/30 | Médicament | GALPHARMA |
| B-PROTEI ALL 200g | Complément alimentaire | B&B LIFE SCIENCE |
| ABX PENTRA UREA CP | Réactifs de laboratoire | HORIBA |

#### Clients

| Client | Ville |
|---|---|
| CAMEG | Lomé |
| LABOREX TOGO | Lomé |
| TEDIS PHARMA TOGO | Lomé |
| UBIPHARM TOGO | Lomé |
| CHP ANEHO | Aného |
| CHR SOKODE | Sokodé |

#### Fournisseurs

| Fournisseur | Pays |
|---|---|
| HORIBA ABX SAS | France |
| Continental Commodities | France |
| GALPHARMA | Tunisie |
| IBERMA | Maroc |
| B&B LIFE SCIENCE | Inde |
| BIORESEARCH | Suisse |
| MAIA AFRICA SAS | Burkina Faso |
| DEO GRATIAS PHARMA | Togo |

---

### 3.3.5 User Stories

#### US-REF-01 : Créer un produit

**En tant que** responsable produit / administrateur,  
**je veux** créer une fiche produit complète,  
**afin de** pouvoir l’utiliser dans les achats, stocks et ventes.

**Critères d’acceptation :**

1. L’utilisateur peut saisir la désignation, la catégorie, la forme, le dosage, le conditionnement, la classe thérapeutique, le fournisseur, le code CIP, la TVA et les seuils.
2. Le système vérifie que la désignation est unique.
3. Le produit peut être actif ou inactif.
4. Le produit peut avoir plusieurs conditionnements.
5. Le produit peut être lié à un fournisseur principal.
6. La création est journalisée.
7. Le produit est immédiatement disponible dans les autres modules.

---

#### US-REF-02 : Créer un fournisseur

**En tant que** administrateur ou responsable achats,  
**je veux** enregistrer les fournisseurs internationaux et locaux,  
**afin de** rattacher les commandes d’achat et les produits.

**Critères d’acceptation :**

1. Les champs obligatoires sont : nom, pays, adresse, téléphone.
2. Le fournisseur peut avoir une devise par défaut : EUR, USD ou XOF.
3. Le fournisseur peut avoir un délai de fabrication moyen.
4. Le fournisseur peut avoir un délai de livraison moyen.
5. Le fournisseur peut être actif ou inactif.
6. Les modifications sont historisées.

---

#### US-REF-03 : Créer un client

**En tant que** commercial ou administrateur,  
**je veux** enregistrer les clients et répartiteurs,  
**afin de** leur affecter des commandes et des livraisons.

**Critères d’acceptation :**

1. Les informations minimales sont : nom, adresse, téléphone, ville.
2. Le client peut être typé : répartiteur, hôpital, clinique, pharmacie, boutique, autre.
3. Le client peut avoir des conditions commerciales spécifiques.
4. Le client peut être actif ou inactif.
5. Un client inactif ne peut pas recevoir de nouvelle commande.

---

#### US-REF-04 : Configurer les taux de TVA

**En tant que** responsable financier,  
**je veux** définir les taux de TVA par catégorie ou produit,  
**afin de** garantir la conformité fiscale des ventes.

**Critères d’acceptation :**

1. Les produits infantiles peuvent être taxés à 18%.
2. Les cosmétiques peuvent être taxés à 18%.
3. Les compléments alimentaires peuvent être taxés à 18%.
4. Les médicaments peuvent être exonérés, sauf règle contraire.
5. Le taux peut être surchargé au niveau produit.
6. Le système applique automatiquement la TVA lors de la vente.

---

## 3.4 Workflow 2 : Achat international

### 3.4.1 Objectif

Permettre à LABMEDIS de commander des produits auprès de fournisseurs internationaux, en gérant :

- les devises,
- les quantités,
- les conditionnements,
- les prix d’achat,
- les taux de change,
- les délais de fabrication,
- les délais de livraison,
- le mode de transport.

---

### 3.4.2 Workflow détaillé

```text
Expression du besoin
        ↓
Vérification du stock disponible
        ↓
Vérification du stock en transit
        ↓
Vérification des prévisions de rupture
        ↓
Sélection du fournisseur
        ↓
Création de la commande d’achat
        ↓
Ajout des lignes produits
        ↓
Saisie des prix d’achat en devise
        ↓
Saisie du taux de change
        ↓
Calcul du montant total
        ↓
Validation par responsable
        ↓
Envoi au fournisseur
        ↓
Suivi du statut de fabrication
        ↓
Confirmation d’expédition
        ↓
Association au transport : bateau, avion, express, terrestre
```

---

### 3.4.3 Statuts d’une commande fournisseur

| Statut | Description |
|---|---|
| Brouillon | Commande non validée. |
| En attente de validation | Commande soumise à validation. |
| Validée | Commande confirmée par LABMEDIS. |
| Envoyée au fournisseur | Commande transmise au fabricant. |
| En fabrication | Le fournisseur produit les marchandises. |
| Prête à expédier | Fabrication terminée. |
| Expédiée | Marchandise remise au transporteur. |
| En transit | En cours de dédouanement ou transport. |
| Partiellement reçue | Une partie seulement est reçue. |
| Reçue | Toute la commande est reçue. |
| Close | Commande terminée. |
| Annulée | Commande annulée. |

---

### 3.4.4 Règles de gestion

| Règle | Description |
|---|---|
| Devise | La commande doit préciser la devise du fournisseur. |
| Taux de change | Le taux de change doit être saisi ou récupéré au moment de la commande. |
| Prix unitaire | Le prix unitaire est saisi dans la devise du fournisseur. |
| Contre-valeur CFA | Le système calcule automatiquement la contre-valeur CFA. |
| Conditionnement | La quantité peut être saisie en cartons et/ou en unités de base. |
| Délai | La commande doit estimer une date de réception. |
| Transport | Le mode de transport doit être sélectionné. |
| Validation | Une commande validée ne peut plus être modifiée librement. |
| Annulation | Une commande annulée doit conserver un motif. |

---

### 3.4.5 Exemple métier

LABMEDIS commande chez **CONTINENTAL COMMODITIES** :

| Produit | Quantité carton | Conditionnement | Quantité unités |
|---|---:|---|---:|
| France Lait 1er âge 400g | 100 | carton/12 | 1 200 |
| France Lait 2ème âge 900g | 50 | carton/6 | 300 |
| France Lait AR 400g | 30 | carton/12 | 360 |

Prix d’achat fournisseur :

| Produit | PA Euro |
|---|---:|
| France Lait 1er âge 400g | 3,41 € |
| France Lait 2ème âge 900g | 7,43 € |
| France Lait AR 400g | 4,41 € |

Taux de change estimé :

```text
1 EUR = 656 XOF
```

La commande devra calculer la contre-valeur CFA.

---

### 3.4.6 User Stories

#### US-ACH-01 : Créer une commande fournisseur

**En tant que** responsable achats,  
**je veux** créer une commande fournisseur,  
**afin de** lancer l’approvisionnement des produits.

**Critères d’acceptation :**

1. L’utilisateur choisit un fournisseur actif.
2. La devise du fournisseur est proposée par défaut.
3. L’utilisateur peut modifier le taux de change.
4. L’utilisateur peut ajouter plusieurs lignes de produits.
5. Chaque ligne contient le produit, la quantité, le prix unitaire et le conditionnement.
6. Le système calcule le montant total en devise et en CFA.
7. La commande est enregistrée en statut brouillon.
8. L’utilisateur peut soumettre la commande à validation.

---

#### US-ACH-02 : Valider une commande fournisseur

**En tant que** direction ou responsable achats,  
**je veux** valider une commande fournisseur,  
**afin de** confirmer l’engagement d’achat.

**Critères d’acceptation :**

1. Seuls les utilisateurs autorisés peuvent valider.
2. Une commande incomplète ne peut pas être validée.
3. La validation fige les lignes principales.
4. Le statut passe à `Validée`.
5. La date de validation est enregistrée.
6. L’utilisateur valideur est journalisé.

---

#### US-ACH-03 : Suivre le statut de fabrication

**En tant que** responsable achats,  
**je veux** mettre à jour le statut de fabrication d’une commande,  
**afin de** suivre l’avancement chez le fournisseur.

**Critères d’acceptation :**

1. Les statuts possibles incluent : `En fabrication`, `Prête à expédier`, `Expédiée`.
2. Chaque changement de statut est horodaté.
3. L’utilisateur peut ajouter un commentaire.
4. Le système peut notifier les utilisateurs concernés via SignalR.
5. L’historique des statuts est consultable.

---

#### US-ACH-04 : Associer un mode de transport

**En tant que** responsable logistique,  
**je veux** associer un mode de transport à une commande,  
**afin de** suivre les coûts et délais logistiques.

**Critères d’acceptation :**

1. Les modes disponibles sont : maritime, aérien, express, terrestre.
2. Le mode de transport influence le calcul du prix de revient.
3. L’utilisateur peut saisir le numéro de conteneur, le numéro de vol, le numéro de tracking ou le numéro de BL.
4. Le système enregistre la date d’expédition estimée.
5. Le système enregistre la date d’arrivée estimée.
6. Le transport peut être partiel : une commande peut être divisée en plusieurs expéditions.

---

## 3.5 Workflow 3 : Transport, transit et douane

### 3.5.1 Objectif

Suivre la marchandise depuis le fournisseur jusqu’à l’entrepôt LABMEDIS.

Ce workflow est important car les produits peuvent arriver :

- par bateau,
- par avion,
- par express,
- par route.

Le mode de transport influence :

- le coût logistique,
- le délai,
- le prix de revient,
- la stratégie de réapprovisionnement.

---

### 3.5.2 Workflow détaillé

```text
Commande fournisseur expédiée
        ↓
Enregistrement de l’expédition
        ↓
Sélection du mode de transport
        ↓
Saisie des références : conteneur, LTA, tracking, BL
        ↓
Suivi du transit international
        ↓
Arrivée au port / aéroport
        ↓
Transit douanier
        ↓
Enregistrement des frais : freight, transit, douane, transfert
        ↓
Allocation des frais aux produits reçus
        ↓
Réception physique
```

---

### 3.5.3 Entités à prévoir

| Entité | Description |
|---|---|
| Shipment | Expédition liée à une ou plusieurs commandes. |
| ShipmentLine | Lignes de produits transportées. |
| TransportMode | Mode : maritime, aérien, express, terrestre. |
| CustomsOperation | Informations douanières. |
| LogisticsCost | Frais logistiques associés. |
| ShipmentEvent | Historique des événements de transport. |

---

### 3.5.4 Règles de gestion

| Règle | Description |
|---|---|
| Une expédition peut couvrir plusieurs commandes | Utile si plusieurs commandes sont regroupées dans un conteneur. |
| Une commande peut être expédiée en plusieurs fois | Exemple : réception partielle. |
| Les frais logistiques doivent être alloués | Freight, transit, transfert, douane, commission. |
| Le mode de transport doit être historisé | Important pour le calcul pondéré du prix de revient. |
| Les dates estimées et réelles doivent être distinguées | Date départ estimée, réelle, arrivée estimée, réelle. |

---

### 3.5.5 User Stories

#### US-LOG-01 : Créer une expédition

**En tant que** responsable logistique,  
**je veux** créer une expédition liée à une ou plusieurs commandes,  
**afin de** suivre la marchandise jusqu’à l’entrepôt.

**Critères d’acceptation :**

1. L’expédition peut être liée à une ou plusieurs commandes fournisseurs.
2. Le mode de transport est obligatoire.
3. Les références transport peuvent être saisies.
4. Les dates estimées peuvent être saisies.
5. L’expédition peut contenir plusieurs lignes produits.
6. Le statut initial est `Préparée` ou `En transit`.

---

#### US-LOG-02 : Enregistrer des frais logistiques

**En tant que** responsable import ou comptable,  
**je veux** enregistrer les frais logistiques liés à une expédition,  
**afin de** calculer le prix de revient réel des produits.

**Critères d’acceptation :**

1. Les types de frais peuvent inclure : freight, transit, douane, commission promo, frais transfert, assurance, manutention.
2. Les frais peuvent être saisis en EUR, USD ou XOF.
3. Le système convertit les frais en XOF selon le taux appliqué.
4. Les frais peuvent être répartis au prorata de la valeur, de la quantité ou du volume.
5. La méthode de répartition doit être configurable ou validable.
6. Les frais sont intégrés au calcul du prix de revient à la réception.

---

#### US-LOG-03 : Suivre les événements de transport

**En tant que** responsable logistique,  
**je veux** suivre les événements de transport,  
**afin de** connaître la position et le statut de la marchandise.

**Critères d’acceptation :**

1. Chaque événement contient une date, un statut, une description et un utilisateur.
2. Exemples d’événements : expédié, arrivé au port, en douane, dédouané, livré.
3. Les événements sont affichés dans une timeline.
4. Le dernier événement détermine le statut logistique.
5. Des alertes peuvent être générées en cas de retard.

---

## 3.6 Workflow 4 : Réception physique des produits

### 3.6.1 Objectif

Enregistrer l’arrivée physique des produits dans l’entrepôt LABMEDIS.

La réception doit permettre de :

- comparer les quantités commandées et reçues,
- enregistrer les lots,
- enregistrer les dates de péremption,
- constater les écarts,
- placer les produits en réception ou quarantaine,
- préparer la mise en stock.

---

### 3.6.2 Workflow détaillé

```text
Arrivée physique de la marchandise
        ↓
Sélection de la commande fournisseur ou expédition
        ↓
Contrôle des documents : BL, facture, packing list
        ↓
Contrôle quantitatif
        ↓
Saisie des quantités reçues
        ↓
Saisie des numéros de lot
        ↓
Saisie des dates de péremption
        ↓
Constat des écarts éventuels
        ↓
Placement en zone réception
        ↓
Contrôle qualité
        ↓
Validation ou rejet
        ↓
Mise en stock
```

---

### 3.6.3 Statuts d’une réception

| Statut | Description |
|---|---|
| Brouillon | Réception non confirmée. |
| En cours | Saisie des quantités en cours. |
| En contrôle | En attente de contrôle qualité. |
| Partiellement reçue | Quantité reçue inférieure à la quantité attendue. |
| Reçue | Tous les produits attendus sont reçus. |
| Avec écart | Différence entre commande et réception. |
| Validée | Réception confirmée. |
| Mise en stock | Produits rangés dans les emplacements. |
| Refusée | Réception refusée. |

---

### 3.6.4 Règles de gestion

| Règle | Description |
|---|---|
| Lot obligatoire | Chaque produit pharmaceutique reçu doit avoir un numéro de lot. |
| Péremption obligatoire | Chaque lot doit avoir une date de péremption. |
| Quantité en unités | Le système calcule la quantité en unités de base. |
| Quantité en cartons | Le système conserve aussi la quantité en cartons si disponible. |
| Écart | Si quantité reçue différente, un écart doit être enregistré. |
| Produit non commandé | Peut être signalé et nécessiter une validation. |
| Produit endommagé | Peut être placé en quarantaine. |
| Péremption courte | Doit déclencher une alerte. |
| Statut initial | Par défaut, les produits reçus peuvent être placés en `En réception` ou `Quarantaine`. |

---

### 3.6.5 Exemple métier

Réception d’un conteneur **France Lait** :

| Produit | Commandé | Reçu | Lot | Péremption |
|---|---:|---:|---|---|
| France Lait 1er âge 400g | 100 cartons | 100 cartons | LOT-A100 | 30/06/2027 |
| France Lait 2ème âge 900g | 50 cartons | 48 cartons | LOT-B200 | 31/05/2027 |
| France Lait AR 400g | 30 cartons | 30 cartons | LOT-C300 | 30/04/2027 |

Le système doit détecter :

```text
France Lait 2ème âge 900g : manquant 2 cartons
```

Un écart doit être enregistré.

---

### 3.6.6 User Stories

#### US-REC-01 : Créer une réception fournisseur

**En tant que** magasinier,  
**je veux** créer une réception à partir d’une commande fournisseur,  
**afin d’enregistrer les produits arrivés.

**Critères d’acceptation :**

1. L’utilisateur sélectionne une commande fournisseur validée.
2. Le système affiche les lignes attendues.
3. L’utilisateur saisit les quantités reçues.
4. L’utilisateur saisit les numéros de lot.
5. L’utilisateur saisit les dates de péremption.
6. Le système calcule les quantités en unités de base.
7. La réception peut être enregistrée en brouillon.

---

#### US-REC-02 : Gérer les écarts de réception

**En tant que** magasinier ou responsable logistique,  
**je veux** enregistrer les écarts entre quantités commandées et reçues,  
**afin de** garder une trace fiable des réceptions.

**Critères d’acceptation :**

1. Le système compare automatiquement quantité commandée et quantité reçue.
2. Si quantité reçue inférieure, le statut peut être `Partiellement reçue`.
3. Si quantité reçue supérieure, une validation responsable est demandée.
4. Les écarts doivent avoir un motif : manquant, casse, erreur fournisseur, refus, autre.
5. Les écarts sont historisés.
6. Les écarts peuvent générer des alertes.

---

#### US-REC-03 : Enregistrer les lots reçus

**En tant que** magasinier,  
**je veux** enregistrer les numéros de lot et dates de péremption,  
**afin de** garantir la traçabilité pharmaceutique.

**Critères d’acceptation :**

1. Le numéro de lot est obligatoire.
2. La date de péremption est obligatoire.
3. Un même produit peut être reçu en plusieurs lots.
4. Un lot peut être réparti sur plusieurs emplacements après mise en stock.
5. Le lot est lié à la commande fournisseur et au produit.
6. Le lot peut être associé à un mode de transport.

---

#### US-REC-04 : Placer les produits en quarantaine

**En tant que** responsable qualité,  
**je veux** placer certains produits reçus en quarantaine,  
**afin de** les empêcher d’être vendus avant contrôle.

**Critères d’acceptation :**

1. Un lot peut être marqué `En quarantaine`.
2. Les lots en quarantaine ne sont pas disponibles à la vente.
3. L’emplacement de quarantaine peut être défini.
4. Le motif de mise en quarantaine est obligatoire.
5. Le lot peut ensuite être libéré ou rejeté.
6. L’action est journalisée.

---

## 3.7 Workflow 5 : Contrôle qualité et libération des lots

### 3.7.1 Objectif

S’assurer que les produits reçus peuvent être vendus.

Dans le domaine pharmaceutique, certains lots peuvent nécessiter une validation qualité avant mise à disposition.

---

### 3.7.2 Workflow

```text
Lot reçu
    ↓
Statut : En réception ou Quarantaine
    ↓
Contrôle qualité
    ↓
Conforme ?
    ├── Oui → Libération du lot
    └── Non → Non-conformité / Quarantaine prolongée / Rejet
```

---

### 3.7.3 Statuts qualité possibles

| Statut | Description |
|---|---|
| En réception | Produit reçu, pas encore contrôlé. |
| En quarantaine | Produit bloqué en attente de décision. |
| Conforme | Produit accepté. |
| Libéré | Produit disponible à la vente. |
| Non conforme | Produit bloqué. |
| Rejeté | Produit non accepté. |
| Détruit | Produit définitivement sorti. |

---

### 3.7.4 Règles de gestion

| Règle | Description |
|---|---|
| Vente interdite | Un lot non libéré ne peut pas être vendu. |
| Libération tracée | La libération doit être effectuée par un utilisateur autorisé. |
| Motif obligatoire | En cas de non-conformité, un motif est obligatoire. |
| Historique | Tous les changements de statut doivent être historisés. |
| Notification | Une alerte peut être envoyée au responsable qualité. |

---

### 3.7.5 User Stories

#### US-QUA-01 : Libérer un lot

**En tant que** responsable qualité,  
**je veux** libérer un lot conforme,  
**afin de** le rendre disponible à la vente.

**Critères d’acceptation :**

1. Le lot doit être en statut `En quarantaine` ou `En réception`.
2. Seuls les utilisateurs autorisés peuvent libérer un lot.
3. Le statut passe à `Libéré`.
4. Le lot devient visible dans le stock disponible.
5. L’action est journalisée.
6. Une notification peut être envoyée.

---

#### US-QUA-02 : Marquer un lot non conforme

**En tant que** responsable qualité,  
**je veux** marquer un lot comme non conforme,  
**afin d’empêcher sa vente.

**Critères d’acceptation :**

1. Le statut passe à `Non conforme`.
2. Le lot n’est plus disponible à la vente.
3. Un motif est obligatoire.
4. Le lot peut être déplacé vers une zone de quarantaine.
5. L’action est journalisée.

---

## 3.8 Workflow 6 : Mise en stock et adressage

### 3.8.1 Objectif

Ranger physiquement les produits reçus dans des emplacements précis de l’entrepôt.

---

### 3.8.2 Workflow

```text
Lot libéré ou en réception validée
        ↓
Sélection de l’emplacement cible
        ↓
Scan produit / lot / emplacement
        ↓
Saisie de la quantité à ranger
        ↓
Confirmation
        ↓
Mise à jour du stock par lot et emplacement
        ↓
Historisation du mouvement
```

---

### 3.8.3 Règles de gestion

| Règle | Description |
|---|---|
| Emplacement obligatoire | Tout produit en stock doit être localisé. |
| Multi-emplacement | Un lot peut être stocké dans plusieurs emplacements. |
| Scan | La mise en stock peut être faite par scan. |
| Quantité | La quantité mise en stock ne peut pas dépasser la quantité reçue. |
| Zone froide | Certains produits peuvent nécessiter une zone spécifique. |
| Zone quarantaine | Les lots non libérés doivent rester en zone contrôlée. |

---

### 3.8.4 User Stories

#### US-STK-01 : Mettre un produit en stock

**En tant que** magasinier,  
**je veux** ranger un produit dans un emplacement,  
**afin de** le localiser précisément.

**Critères d’acceptation :**

1. L’utilisateur sélectionne un lot reçu.
2. L’utilisateur choisit un emplacement actif.
3. L’utilisateur saisit la quantité.
4. Le système met à jour la quantité du lot à cet emplacement.
5. Un mouvement de stock est créé.
6. L’opération est journalisée.

---

#### US-STK-02 : Transférer un lot entre emplacements

**En tant que** magasinier,  
**je veux** déplacer un lot d’un emplacement à un autre,  
**afin de** réorganiser le stock.

**Critères d’acceptation :**

1. L’utilisateur sélectionne le lot et l’emplacement source.
2. L’utilisateur choisit l’emplacement destination.
3. La quantité transférée ne peut pas dépasser la quantité disponible.
4. Le mouvement est de type `Transfert`.
5. Le stock est mis à jour immédiatement.
6. L’historique conserve l’emplacement source et destination.

---

#### US-STK-03 : Rechercher un produit dans l’entrepôt

**En tant que** magasinier,  
**je veux** savoir où se trouve un produit,  
**afin de** le retrouver rapidement.

**Critères d’acceptation :**

1. La recherche peut se faire par produit, lot, emplacement, fournisseur ou code CIP.
2. Le résultat affiche les emplacements, lots, quantités, statuts et péremptions.
3. Les lots périmés sont clairement identifiés.
4. Les lots en quarantaine sont clairement identifiés.
5. L’information est mise à jour en temps réel.

---

## 3.9 Workflow 7 : Gestion des ventes clients

### 3.9.1 Objectif

Permettre à LABMEDIS de vendre les produits aux :

- répartiteurs,
- cliniques,
- hôpitaux,
- pharmacies,
- autres structures.

Exemples :

- CAMEG,
- LABOREX TOGO,
- TEDIS PHARMA TOGO,
- UBIPHARM TOGO,
- CHP ANEHO,
- CHR SOKODE.

---

### 3.9.2 Workflow détaillé

```text
Demande client
        ↓
Vérification de la fiche client
        ↓
Vérification des produits demandés
        ↓
Vérification du stock disponible par lot
        ↓
Application du prix client / tarif catalogue
        ↓
Application de la TVA
        ↓
Création du devis ou commande
        ↓
Validation commerciale
        ↓
Réservation du stock
        ↓
Préparation de commande
        ↓
Validation de préparation
        ↓
Livraison
        ↓
Facturation
        ↓
Suivi paiement
```

---

### 3.9.3 Statuts d’une commande client

| Statut | Description |
|---|---|
| Brouillon | Commande non confirmée. |
| Devis | Proposition envoyée au client. |
| Confirmée | Commande validée par le client. |
| Réservée | Stock réservé. |
| En préparation | Magasinier prépare la commande. |
| Prête | Préparation terminée. |
| Livrée | Produits remis au client. |
| Facturée | Facture générée. |
| Partiellement livrée | Certains produits livrés seulement. |
| Annulée | Commande annulée. |

---

### 3.9.4 Règles de gestion

| Règle | Description |
|---|---|
| Client actif | Une commande ne peut être créée que pour un client actif. |
| Stock disponible | Le système doit vérifier la disponibilité. |
| Lot | Le système doit proposer les lots selon FEFO. |
| Prix | Le prix peut venir du tarif catalogue ou d’un tarif client spécifique. |
| TVA | La TVA est appliquée selon la catégorie ou le produit. |
| Réservation | La commande confirmée peut réserver le stock. |
| Livraison | Une commande peut être livrée totalement ou partiellement. |
| Facture | Une commande livrée peut générer une facture. |
| Avoir | Un retour peut générer un avoir. |

---

### 3.9.5 Exemple métier

Commande client **LABOREX TOGO** :

| Produit | Quantité demandée | Lot proposé | Prix HT | TVA | Prix TTC |
|---|---:|---|---:|---:|---:|
| France Lait 1er âge 400g | 120 | LOT-A100 | 3 660 | 18% | 4 318,8 |
| France Lait AR 400g | 60 | LOT-C300 | 4 960 | 18% | 5 852,8 |

Le système doit :

1. Vérifier le stock disponible.
2. Proposer les lots avec péremption la plus proche.
3. Réserver le stock.
4. Calculer le total HT.
5. Calculer la TVA.
6. Calculer le total TTC.

---

### 3.9.6 User Stories

#### US-VEN-01 : Créer une commande client

**En tant que** commercial,  
**je veux** créer une commande client,  
**afin de** vendre les produits disponibles.

**Critères d’acceptation :**

1. L’utilisateur sélectionne un client actif.
2. L’utilisateur ajoute des produits.
3. Le système affiche le prix applicable.
4. Le système affiche la disponibilité par lot.
5. Le système calcule les totaux HT, TVA et TTC.
6. La commande peut être enregistrée en brouillon.

---

#### US-VEN-02 : Vérifier la disponibilité du stock

**En tant que** commercial,  
**je veux** voir le stock disponible avant de vendre,  
**afin d’éviter les ventes sans stock.

**Critères d’acceptation :**

1. Le système affiche le stock physique.
2. Le système affiche le stock réservé.
3. Le système affiche le stock disponible.
4. Les lots en quarantaine ne sont pas inclus.
5. Les lots périmés ne sont pas inclus.
6. Le système affiche les lots avec dates de péremption.

---

#### US-VEN-03 : Appliquer FEFO automatiquement

**En tant que** système,  
**je veux** proposer les lots avec la péremption la plus proche,  
**afin de** respecter les bonnes pratiques pharmaceutiques.

**Critères d’acceptation :**

1. Les lots proposés sont triés par date de péremption ascendante.
2. Les lots périmés sont exclus.
3. Les lots en quarantaine sont exclus.
4. Le stock réservé est déduit de la disponibilité.
5. L’utilisateur peut exceptionnellement choisir un autre lot si autorisé.
6. Toute exception est journalisée.

---

#### US-VEN-04 : Réserver le stock

**En tant que** système,  
**je veux** réserver le stock lorsqu’une commande est confirmée,  
**afin d’empêcher la vente du même stock à un autre client.

**Critères d’acceptation :**

1. La réservation est liée à la commande client.
2. La réservation est liée à un lot et un emplacement.
3. Le stock disponible diminue.
4. Le stock physique ne diminue pas avant sortie réelle.
5. La réservation est annulée si la commande est annulée.
6. La réservation est historisée.

---

#### US-VEN-05 : Appliquer un prix spécifique client

**En tant que** responsable commercial,  
**je veux** définir des prix spécifiques par client,  
**afin de** gérer les tarifs négociés avec les répartiteurs.

**Critères d’acceptation :**

1. Un produit peut avoir un prix catalogue par défaut.
2. Un client peut avoir une grille tarifaire spécifique.
3. Le prix spécifique peut avoir une date de début et une date de fin.
4. Le système applique d’abord le prix client spécifique s’il existe.
5. Sinon, il applique le prix catalogue.
6. Les modifications tarifaires sont historisées.

---

## 3.10 Workflow 8 : Préparation de commande

### 3.10.1 Objectif

Transformer une commande client validée en préparation physique.

---

### 3.10.2 Workflow

```text
Commande confirmée
        ↓
Stock réservé
        ↓
Création d’un ordre de préparation
        ↓
Liste des lots et emplacements proposés
        ↓
Picking par scan
        ↓
Contrôle des quantités
        ↓
Gestion des manquants éventuels
        ↓
Validation préparation
        ↓
Commande prête à livrer
```

---

### 3.10.3 Règles de gestion

| Règle | Description |
|---|---|
| Scan recommandé | Le préparateur peut scanner produit, lot, emplacement. |
| FEFO | Le système propose les lots à prélever. |
| Manquant | Si produit manquant, le système doit permettre un signalement. |
| Substitution | Aucune substitution sans accord commercial. |
| Validation | La préparation doit être validée avant livraison. |
| Historique | Chaque prélèvement est journalisé. |

---

### 3.10.4 User Stories

#### US-PREP-01 : Préparer une commande

**En tant que** préparateur,  
**je veux** voir la liste des produits à prélever,  
**afin de** préparer physiquement la commande.

**Critères d’acceptation :**

1. L’écran affiche les lignes de commande.
2. Chaque ligne affiche le produit, la quantité, le lot proposé, l’emplacement.
3. Le préparateur peut confirmer chaque ligne.
4. Le préparateur peut signaler un manquant.
5. Le statut passe à `En préparation`.
6. Une fois terminé, le statut passe à `Prête`.

---

#### US-PREP-02 : Scanner un produit pendant la préparation

**En tant que** préparateur,  
**je veux** scanner le produit ou le lot,  
**afin de** valider rapidement le prélèvement.

**Critères d’acceptation :**

1. Le scan peut remplir automatiquement le champ produit ou lot.
2. Si le lot scanné ne correspond pas à la commande, une erreur est affichée.
3. Si le lot est périmé, une erreur est affichée.
4. Si le lot est en quarantaine, une erreur est affichée.
5. Le scan est journalisé.

---

## 3.11 Workflow 9 : Livraison

### 3.11.1 Objectif

Enregistrer la remise des produits au client.

---

### 3.11.2 Workflow

```text
Commande prête
        ↓
Création du bon de livraison
        ↓
Confirmation de livraison
        ↓
Sortie de stock
        ↓
Décrémentation du stock physique
        ↓
Annulation des réservations
        ↓
Génération du BL
        ↓
Facturation éventuelle
```

---

### 3.11.3 Règles de gestion

| Règle | Description |
|---|---|
| BL | Chaque livraison doit générer un bon de livraison. |
| Sortie de stock | La livraison diminue le stock physique. |
| Lot | La sortie doit être liée au lot livré. |
| Livraison partielle | Une commande peut être livrée en plusieurs fois. |
| Signature | Optionnellement, la livraison peut être signée ou confirmée. |
| Facturation | Une livraison peut déclencher la facturation. |

---

### 3.11.4 User Stories

#### US-LIV-01 : Livrer une commande

**En tant que** responsable logistique ou livreur,  
**je veux** confirmer la livraison d’une commande,  
**afin de** sortir les produits du stock.

**Critères d’acceptation :**

1. La commande doit être en statut `Prête` ou `En livraison`.
2. Le système génère un bon de livraison.
3. La livraison crée un mouvement de stock de type `Vente`.
4. Le stock physique est décrémenté.
5. Les réservations sont soldées.
6. Le statut passe à `Livrée` ou `Partiellement livrée`.

---

#### US-LIV-02 : Générer un bon de livraison

**En tant que** utilisateur autorisé,  
**je veux** générer un bon de livraison PDF,  
**afin de** le remettre au client.

**Critères d’acceptation :**

1. Le BL contient la date, le client, les produits, lots, quantités.
2. Le BL peut afficher ou masquer les prix selon les droits.
3. Le BL est téléchargeable en PDF.
4. Le BL peut être imprimé.
5. Le BL est historisé.

---

## 3.12 Workflow 10 : Facturation

### 3.12.1 Objectif

Générer les factures clients après livraison.

---

### 3.12.2 Workflow

```text
Livraison validée
        ↓
Sélection des lignes livrées
        ↓
Application des prix
        ↓
Application de la TVA
        ↓
Génération facture
        ↓
Validation comptable
        ↓
Suivi paiement
```

---

### 3.12.3 Règles de gestion

| Règle | Description |
|---|---|
| Facture liée | Une facture est liée à une commande et/ou à des livraisons. |
| TVA | La TVA doit être calculée ligne par ligne. |
| Acompte | Optionnel : gestion des acomptes. |
| Avoir | Un retour peut générer une facture d’avoir. |
| Numérotation | Les factures doivent avoir une numérotation unique. |
| Export | Les factures peuvent être exportées en PDF. |

---

### 3.12.4 User Stories

#### US-FAC-01 : Générer une facture

**En tant que** comptable ou utilisateur autorisé,  
**je veux** générer une facture après livraison,  
**afin de** facturer le client.

**Critères d’acceptation :**

1. La facture reprend les lignes livrées.
2. Le prix HT, la TVA et le TTC sont calculés.
3. La facture est liée au client et à la commande.
4. La facture possède un numéro unique.
5. La facture peut être imprimée en PDF.
6. La facture est historisée.

---

#### US-FAC-02 : Gérer un avoir

**En tant que** comptable,  
**je veux** créer un avoir pour un retour client,  
**afin de** régulariser la facturation.

**Critères d’acceptation :**

1. L’avoir peut être total ou partiel.
2. L’avoir est lié à une facture ou à un retour.
3. L’avoir contient les produits retournés.
4. L’avoir calcule la TVA correspondante.
5. L’avoir est numéroté et historisé.
6. L’avoir peut être exporté en PDF.

---

## 3.13 Workflow 11 : Retours clients

### 3.13.1 Objectif

Gérer les produits retournés par les clients.

---

### 3.13.2 Workflow

```text
Demande de retour
        ↓
Identification de la commande d’origine
        ↓
Sélection des produits retournés
        ↓
Sélection du lot d’origine
        ↓
Motif du retour
        ↓
Réception du retour
        ↓
Décision : remise en stock, quarantaine, destruction, refus
        ↓
Mise à jour du stock
        ↓
Avoir éventuel
```

---

### 3.13.3 Types de retour

| Type | Description |
|---|---|
| Retour accepté | Produit remis en stock. |
| Retour en quarantaine | Produit à contrôler. |
| Retour refusé | Produit non accepté. |
| Retour détruit | Produit détruit. |
| Retour périmé | Produit périmé retourné. |
| Retour erreur | Produit livré par erreur. |

---

### 3.13.4 Règles de gestion

| Règle | Description |
|---|---|
| Lien commande | Le retour doit être relié à une commande ou livraison. |
| Lot | Le retour doit identifier le lot si possible. |
| État | L’état du produit retourné doit être évalué. |
| Stock | Le stock est mis à jour uniquement si le retour est accepté. |
| Avoir | Un avoir peut être généré. |
| Traçabilité | Tous les retours sont historisés. |

---

### 3.13.5 User Stories

#### US-RET-01 : Créer un retour client

**En tant que** commercial ou magasinier,  
**je veux** enregistrer un retour client,  
**afin de** régulariser le stock et la facturation.

**Critères d’acceptation :**

1. L’utilisateur sélectionne le client.
2. L’utilisateur sélectionne la commande ou livraison d’origine.
3. L’utilisateur sélectionne les produits retournés.
4. L’utilisateur saisit les quantités.
5. L’utilisateur saisit le motif.
6. Le système propose une décision : remise en stock, quarantaine, destruction.

---

#### US-RET-02 : Remettre un produit en stock après retour

**En tant que** responsable qualité ou magasinier,  
**je veux** remettre un produit retourné en stock,  
**afin de** le rendre disponible s’il est conforme.

**Critères d’acceptation :**

1. Le produit doit être accepté.
2. Le lot doit être identifiable.
3. Le produit ne doit pas être périmé.
4. Le produit doit être en état de vente.
5. Le stock est incrémenté.
6. Un mouvement de type `Retour client` est créé.

---

## 3.14 Workflow 12 : Prévisions et réapprovisionnement

### 3.14.1 Objectif

Anticiper les ruptures de stock.

Le PRD audio indique clairement :

> Il faut pouvoir estimer qu’un produit risque de finir et commander 3 ou 4 mois à l’avance afin que le fabricant puisse produire et livrer.

---

### 3.14.2 Workflow

```text
Analyse du stock disponible
        ↓
Analyse du stock en transit
        ↓
Analyse des ventes historiques
        ↓
Calcul de la vitesse de rotation
        ↓
Calcul de la consommation prévisionnelle
        ↓
Intégration des délais de fabrication
        ↓
Intégration des délais de transport
        ↓
Détection des risques de rupture
        ↓
Génération des suggestions de commande
        ↓
Validation par responsable achats
```

---

### 3.14.3 Formule simplifiée de réapprovisionnement

```text
Délai total = Délai de fabrication + Délai de transport + Délai de transit + Délai interne
```

```text
Stock nécessaire = Consommation moyenne journalière × Délai total
```

```text
Point de commande = Stock nécessaire + Stock de sécurité
```

```text
Risque de rupture = Stock disponible + Stock en transit <= Point de commande
```

---

### 3.14.4 Exemple

Produit : `France Lait 1er âge 400g`

| Paramètre | Valeur |
|---|---:|
| Ventes moyennes journalières | 20 unités |
| Délai fabrication | 45 jours |
| Délai transport maritime | 40 jours |
| Délai transit douane | 15 jours |
| Délai interne | 5 jours |
| Délai total | 105 jours |
| Stock de sécurité | 300 unités |

Calcul :

```text
Stock nécessaire = 20 × 105 = 2100 unités
Point de commande = 2100 + 300 = 2400 unités
```

Si :

```text
Stock disponible + Stock en transit <= 2400
```

alors le système propose une commande.

---

### 3.14.5 User Stories

#### US-PREV-01 : Configurer les délais de réapprovisionnement

**En tant que** responsable achats,  
**je veux** définir les délais de fabrication et de livraison par produit ou fournisseur,  
**afin de** calculer les besoins futurs.

**Critères d’acceptation :**

1. Les délais peuvent être définis par fournisseur.
2. Les délais peuvent être surchargés par produit.
3. Les délais incluent fabrication, transport, transit et interne.
4. Les délais sont utilisés dans le calcul de prévision.
5. Les modifications sont historisées.

---

#### US-PREV-02 : Détecter les produits à risque de rupture

**En tant que** responsable achats,  
**je veux** voir les produits proches de la rupture,  
**afin de** commander à temps.

**Critères d’acceptation :**

1. Le système calcule le stock disponible.
2. Le système prend en compte le stock en transit.
3. Le système prend en compte la consommation historique.
4. Le système prend en compte les délais fournisseurs.
5. Les produits à risque sont affichés dans un tableau.
6. Des alertes sont générées.

---

#### US-PREV-03 : Générer une suggestion de commande

**En tant que** responsable achats,  
**je veux** générer une suggestion de commande à partir des prévisions,  
**afin de** accélérer le réapprovisionnement.

**Critères d’acceptation :**

1. Le système propose les produits à commander.
2. Le système propose une quantité suggérée.
3. La quantité peut être modifiée avant validation.
4. L’utilisateur peut transformer la suggestion en commande fournisseur.
5. La suggestion conserve la justification du calcul.
6. L’historique des suggestions est consultable.

---

## 3.15 Workflow 13 : Inventaires et ajustements

### 3.15.1 Objectif

Garantir la fiabilité du stock physique et du stock système.

---

### 3.15.2 Workflow inventaire

```text
Création session inventaire
        ↓
Choix du périmètre : produit, lot, emplacement, zone, catégorie
        ↓
Gel temporaire des mouvements
        ↓
Comptage physique
        ↓
Saisie des quantités comptées
        ↓
Comparaison avec stock système
        ↓
Identification des écarts
        ↓
Validation responsable
        ↓
Génération des ajustements
        ↓
Clôture inventaire
```

---

### 3.15.3 Types d’inventaire

| Type | Description |
|---|---|
| Inventaire général | Tout l’entrepôt. |
| Inventaire par zone | Une zone ou allée. |
| Inventaire par produit | Un produit spécifique. |
| Inventaire par lot | Un lot spécifique. |
| Inventaire par catégorie | Exemple : produits infantiles. |
| Inventaire tournant | Inventaire cyclique de certains produits. |

---

### 3.15.4 Règles de gestion

| Règle | Description |
|---|---|
| Mouvements gelés | Pendant un inventaire actif, les mouvements du périmètre concerné peuvent être bloqués. |
| Écart obligatoire | Tout écart doit être visible. |
| Validation | Les ajustements doivent être validés par un responsable. |
| Motif | Les ajustements doivent avoir un motif. |
| Traçabilité | Les écarts et ajustements sont historisés. |

---

### 3.15.5 User Stories

#### US-INV-01 : Créer une session d’inventaire

**En tant que** magasinier ou responsable stock,  
**je veux** créer une session d’inventaire,  
**afin de** compter les produits.

**Critères d’acceptation :**

1. L’utilisateur choisit le périmètre.
2. Le système génère les lignes à compter.
3. Les quantités système sont enregistrées.
4. Les mouvements du périmètre peuvent être gelés.
5. La session est enregistrée avec un statut `En cours`.

---

#### US-INV-02 : Saisir les quantités comptées

**En tant que** magasinier,  
**je veux** saisir les quantités physiques comptées,  
**afin de** comparer avec le stock système.

**Critères d’acceptation :**

1. L’utilisateur peut saisir par ligne.
2. Le système calcule la différence automatiquement.
3. Les écarts positifs et négatifs sont affichés.
4. L’utilisateur peut ajouter un commentaire.
5. Les saisies sont sauvegardées.

---

#### US-INV-03 : Valider les ajustements d’inventaire

**En tant que** responsable stock,  
**je veux** valider les écarts d’inventaire,  
**afin de** mettre à jour le stock système.

**Critères d’acceptation :**

1. Seuls les utilisateurs autorisés peuvent valider.
2. Le système crée des mouvements d’ajustement.
3. Les motifs sont obligatoires.
4. Le stock système est mis à jour.
5. La session passe au statut `Clôturée`.
6. L’historique est conservé.

---

## 3.16 Workflow 14 : Gestion des péremptions

### 3.16.1 Objectif

Éviter la vente de produits périmés et limiter les pertes.

---

### 3.16.2 Workflow

```text
Job quotidien ou écran alertes
        ↓
Analyse des lots actifs
        ↓
Identification des lots proches péremption
        ↓
Classement par seuil : 30, 60, 90, 120 jours
        ↓
Notification aux responsables
        ↓
Actions possibles : promotion, transfert, destruction, quarantaine
```

---

### 3.16.3 Règles de gestion

| Règle | Description |
|---|---|
| Lot périmé | Non vendable. |
| Lot proche péremption | Visible avec alerte. |
| FEFO | Les lots proches péremption sont proposés en priorité. |
| Destruction | Les produits périmés peuvent être détruits. |
| Historique | Tous les traitements sont historisés. |
| Notification | Les alertes peuvent être envoyées par SignalR ou email. |

---

### 3.16.4 User Stories

#### US-PER-01 : Visualiser les lots proches de péremption

**En tant que** responsable stock,  
**je veux** voir les lots proches de péremption,  
**afin de** prendre une décision avant expiration.

**Critères d’acceptation :**

1. L’écran affiche les lots par seuil : 30, 60, 90, 120 jours.
2. Les lots sont filtrables par produit, catégorie, fournisseur, emplacement.
3. Le stock restant est affiché.
4. La valeur du stock proche péremption peut être affichée.
5. L’utilisateur peut exporter la liste.

---

#### US-PER-02 : Bloquer les lots périmés

**En tant que** système,  
**je veux** bloquer automatiquement les lots périmés,  
**afin d’empêcher leur vente.

**Critères d’acceptation :**

1. Un job Hangfire vérifie quotidiennement les dates de péremption.
2. Les lots dont la date est dépassée passent au statut `Périmé`.
3. Les lots périmés ne peuvent pas être proposés à la vente.
4. Une alerte est générée.
5. L’action est journalisée.

---

## 3.17 Workflow 15 : Destruction, perte et échantillons

### 3.17.1 Objectif

Gérer les sorties de stock exceptionnelles.

---

### 3.17.2 Types de sortie exceptionnelle

| Type | Description |
|---|---|
| Destruction | Produit périmé, endommagé ou non conforme. |
| Perte | Produit non retrouvé en inventaire. |
| Échantillon | Produit offert ou utilisé pour démonstration. |
| Casse | Produit cassé pendant manutention. |
| Expired | Produit périmé. |

---

### 3.17.3 Règles de gestion

| Règle | Description |
|---|---|
| Motif obligatoire | Toute sortie exceptionnelle doit avoir un motif. |
| Validation | Certaines sorties doivent être validées par responsable. |
| Traçabilité | Le mouvement doit être historisé. |
| Stock | Le stock doit être décrémenté. |
| Valeur | La valeur du stock perdu peut être calculée. |

---

### 3.17.4 User Stories

#### US-DESTR-01 : Détruire un lot

**En tant que** responsable qualité ou stock,  
**je veux** enregistrer la destruction d’un lot,  
**afin de** le sortir définitivement du stock.

**Critères d’acceptation :**

1. L’utilisateur sélectionne le lot et la quantité.
2. Le motif est obligatoire.
3. Le stock est décrémenté.
4. Le lot peut passer au statut `Détruit` si totalement détruit.
5. Un mouvement de type `Destruction` est créé.
6. L’opération est journalisée.

---

#### US-PERTE-01 : Enregistrer une perte

**En tant que** responsable stock,  
**je veux** enregistrer une perte de produit,  
**afin de** corriger le stock.

**Critères d’acceptation :**

1. L’utilisateur sélectionne produit, lot, emplacement.
2. La quantité perdue est saisie.
3. Le motif est obligatoire.
4. Le stock est décrémenté.
5. Le mouvement est historisé.
6. Une alerte peut être envoyée si la quantité est importante.

---

## 3.18 Workflow 16 : Pricing et validation tarifaire

### 3.18.1 Objectif

Définir et contrôler les prix de vente.

LABMEDIS définit ses prix à partir :

- du prix d’achat,
- des frais logistiques,
- des coefficients de commissions,
- du freight,
- du transit,
- des frais de transfert,
- de la marge.

---

### 3.18.2 Workflow pricing

```text
Prix d’achat fournisseur
        ↓
Conversion en CFA
        ↓
Application commissions promo
        ↓
Application freight
        ↓
Application transit
        ↓
Application frais transfert
        ↓
Calcul du prix de revient
        ↓
Application marge
        ↓
Prix de vente HT calculé
        ↓
Comparaison avec prix catalogue
        ↓
Validation direction
        ↓
Publication tarif
```

---

### 3.18.3 Exemple France Lait

| Produit | PA Euro | PA CFA | Coefficients | PR CFA | Marge | PV HT calculé |
|---|---:|---:|---|---:|---:|---:|
| France Lait 1er âge 400g | 3,41 | 2 237 | 1.25 × 1.03 × 1.09 × 1.07 | 3 359 | 1.10 | 3 695 |

---

### 3.18.4 User Stories

#### US-PRICE-01 : Simuler un prix de revient

**En tant que** responsable financier,  
**je veux** simuler le prix de revient d’un produit,  
**afin de** décider du prix de vente.

**Critères d’acceptation :**

1. L’utilisateur saisit le prix d’achat en devise.
2. L’utilisateur saisit le taux de change.
3. L’utilisateur sélectionne un profil de coûts.
4. Le système calcule le prix de revient CFA.
5. Le système calcule le prix de vente cible selon la marge.
6. Le système compare avec le prix catalogue actuel.

---

#### US-PRICE-02 : Valider un prix de vente

**En tant que** direction,  
**je veux** valider les prix de vente,  
**afin de** contrôler la politique tarifaire.

**Critères d’acceptation :**

1. Les prix peuvent être proposés par le responsable pricing.
2. La direction peut approuver ou rejeter.
3. Un prix validé possède une date d’effet.
4. Les anciens prix sont historisés.
5. Le prix validé est appliqué aux ventes futures.
6. Les modifications sont journalisées.

---

## 3.19 Workflow 17 : Notifications et alertes

### 3.19.1 Objectif

Informer les utilisateurs en temps réel des événements importants.

---

### 3.19.2 Types d’alertes

| Alerte | Déclencheur |
|---|---|
| Stock faible | Produit sous le seuil minimum. |
| Rupture | Stock disponible nul. |
| Péremption proche | Lot proche de la date limite. |
| Lot périmé | Lot expiré. |
| Réception en retard | Commande non reçue à la date prévue. |
| Écart réception | Différence commande/réception. |
| Quarantaine | Lot bloqué. |
| Préparation prête | Commande prête à livrer. |
| Facture générée | Facture créée. |
| Retour client | Retour enregistré. |

---

### 3.19.3 User Stories

#### US-NOTIF-01 : Recevoir des notifications temps réel

**En tant qu’utilisateur connecté,  
**je veux** recevoir des notifications temps réel,  
**afin de** réagir rapidement aux événements importants.

**Critères d’acceptation :**

1. Le backend utilise SignalR.
2. Les notifications sont affichées dans le frontend React.
3. L’utilisateur peut marquer une notification comme lue.
4. Les notifications peuvent être filtrées par type.
5. Les notifications critiques peuvent être affichées en toast.
6. Les notifications sont historisées.

---

## 3.20 Synthèse des règles transverses

### 3.20.1 Règles obligatoires

| Règle | Description |
|---|---|
| Lot obligatoire | Tout produit pharmaceutique doit être suivi par lot. |
| Péremption obligatoire | Tout lot doit avoir une date de péremption. |
| Traçabilité | Toute entrée/sortie doit être historisée. |
| Soft delete | Aucune suppression physique dans le backend. |
| Rôles | Chaque action doit vérifier les permissions. |
| Audit | Chaque action sensible doit journaliser utilisateur, date, IP, UserAgent. |
| Statuts | Les entités principales doivent avoir des statuts clairs. |
| Prix | Les montants financiers doivent être manipulés avec précision. |
| Devise | Toute commande internationale doit préciser la devise. |
| Transport | Toute réception doit pouvoir être associée à un mode de transport. |

---

### 3.20.2 Règles frontend React

| Règle | Description |
|---|---|
| Formulaires | Validation côté client avant envoi API. |
| Toasts | Afficher les succès et erreurs via notifications. |
| Statuts | Afficher les statuts avec badges colorés. |
| Tables | Pagination, filtres, tri. |
| Scan | Prise en charge des lecteurs code-barres/QR. |
| Temps réel | Notifications via SignalR. |
| Responsive | Interfaces utilisables sur desktop et tablette. |
| Droits | Masquer les actions non autorisées selon le rôle. |

---

### 3.20.3 Règles backend .NET

| Règle | Description |
|---|---|
| Architecture | Core, Service, Api. |
| Héritage | `Service : Repository, IService`. |
| Soft delete | `IsDeleted = true`. |
| Logging | `ILoggerManager` uniquement. |
| DTO Request | Valeurs numériques/monétaires en `string`. |
| Mapping | Manuel via `ToEntity()`. |
| Response | Constructeur depuis entité. |
| Exceptions | `try/catch` dans les contrôleurs. |
| HTTP | Ne jamais retourner `StatusCode(500)` directement. |
| Jobs | Hangfire pour traitements planifiés. |
| Temps réel | SignalR pour notifications. |
| Bulk | `BulkInsertAsync` / `BulkUpdateAsync` pour opérations massives. |

---

## 3.21 Matrice de priorisation des user stories

| Module | Priorité | Justification |
|---|---|---|
| Produits, clients, fournisseurs | Haute | Données de référence indispensables. |
| Commandes fournisseurs | Haute | Cœur du flux d’achat. |
| Transport et expédition | Haute | Import international essentiel. |
| Réception et lots | Haute | Traçabilité pharmaceutique obligatoire. |
| Stock et emplacements | Haute | Gestion physique indispensable. |
| Ventes clients | Haute | Cœur du chiffre d’affaires. |
| Préparation et livraison | Haute | Flux opérationnel quotidien. |
| Facturation | Haute | Suivi financier obligatoire. |
| Pricing | Haute | Spécificité LABMEDIS. |
| Prévisions | Moyenne à haute | Très demandé mais peut être itératif. |
| Retours | Moyenne | Important mais peut venir après flux principal. |
| Inventaire | Moyenne | Important pour fiabilité stock. |
| Notifications | Moyenne | Améliore l’expérience opérationnelle. |
| Reporting avancé | Moyenne | Pilotage direction. |

---

## 3.22 Points à valider avec LABMEDIS

Avant développement définitif, les points suivants doivent être confirmés :

| Point à valider | Impact |
|---|---|
| Les médicaments sont-ils toujours exonérés de TVA ? | Facturation et pricing. |
| Les réactifs HORIBA sont-ils soumis à TVA ? | Facturation. |
| Faut-il gérer plusieurs entrepôts ? | Architecture stock. |
| Faut-il gérer la chaîne du froid ? | Emplacements et alertes. |
| Faut-il valider les prix par workflow ? | Module pricing. |
| Faut-il gérer les acomptes fournisseurs ? | Achats et finances. |
| Faut-il gérer les remises commerciales ? | Ventes. |
| Faut-il gérer les livraisons partielles ? | Ventes et stock. |
| Faut-il gérer les numéros de facture séquentiels ? | Comptabilité. |
| Faut-il intégrer un logiciel comptable ? | Export et intégration. |
| Faut-il une application mobile/PDA ? | Interface magasinier. |
| Faut-il imprimer des étiquettes ? | Module impression. |
| Faut-il bloquer automatiquement les lots proches péremption ? | Règle qualité. |
| Faut-il plusieurs niveaux de validation pour les ajustements ? | Sécurité. |

---

## 3.23 Synthèse

Les workflows opérationnels de LABMEDIS doivent couvrir un flux complet :

```text
Données de référence
    → Achats internationaux
    → Transport
    → Réception
    → Lots et péremptions
    → Stock et emplacements
    → Pricing
    → Ventes
    → Préparation
    → Livraison
    → Facturation
    → Retours
    → Prévisions
```

Chaque étape doit être :

- tracée,
- sécurisée,
- historisée,
- journalisée,
- associée à des rôles,
- visible dans le frontend React,
- implémentée dans le backend .NET selon l’architecture imposée.

Cette base permet ensuite de définir précisément les entités, endpoints API, services, DTOs, règles de validation et écrans React à développer.