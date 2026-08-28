# 4. Moteur d’Anticipation (MRP) & Prévisions

## 4.1 Objectif du module

Le module **Moteur d’Anticipation (MRP) & Prévisions** doit permettre à LABMEDIS d’anticiper les ruptures de stock et de déclencher les commandes fournisseurs au bon moment.

Dans le contexte LABMEDIS, cette anticipation est critique car :

- les produits sont achetés auprès de fournisseurs internationaux,
- les délais de fabrication peuvent être longs,
- les délais de transport international peuvent être importants,
- le transit douanier peut ajouter des délais supplémentaires,
- certains produits arrivent par bateau, avion ou express,
- la rupture d’un produit pharmaceutique ou infantile peut avoir un impact commercial fort.

Le PRD audio mentionne clairement ce besoin :

> Il faut pouvoir estimer qu’un produit risque de finir et commencer déjà par commander ce produit. Pour commander ce produit, il faut peut-être 3 mois ou 4 mois d’avance afin que le fabricant puisse fabriquer et livrer.

Le module devra donc répondre aux questions suivantes :

1. Quels produits risquent une rupture ?
2. Dans combien de jours la rupture peut-elle arriver ?
3. Quand faut-il déclencher la commande ?
4. Quelle quantité faut-il commander ?
5. Quel mode de transport est le plus pertinent ?
6. Quel fournisseur faut-il solliciter ?
7. La commande doit-elle être urgente ?

---

## 4.2 Périmètre fonctionnel

Le module couvre :

| Fonctionnalité | Description |
|---|---|
| Analyse du stock disponible | Stock physique, réservé, quarantaine, périmé. |
| Analyse du stock en transit | Commandes fournisseurs non encore reçues. |
| Analyse des ventes historiques | Ventes réelles sur 30, 60, 90, 180 ou 365 jours. |
| Calcul de la consommation moyenne | Moyenne simple, pondérée ou saisonnalisée. |
| Calcul des délais fournisseurs | Fabrication, transport, transit, réception, contrôle qualité. |
| Calcul du point de commande | Seuil déclenchant une suggestion de commande. |
| Calcul du besoin net | Quantité manquante projetée. |
| Calcul de la quantité suggérée | Quantité à commander. |
| Simulation de réapprovisionnement | Simulation avant commande réelle. |
| Suggestions de commande | Génération automatique de propositions. |
| Transformation en commande fournisseur | Conversion d’une suggestion en commande d’achat. |
| Alertes de rupture | Notification des produits critiques. |
| Suivi des prévisions | Historique des prévisions et des décisions. |

---

## 4.3 Concepts métier principaux

### 4.3.1 Produit prévisionnel

Un produit prévisionnel est un produit suivi par le moteur MRP.

Exemples :

- France Lait 1er âge 400g
- France Lait 2ème âge 900g
- ALLERGICA 10MG CPR B/30
- GRIPEX Adulte sans sucre
- PECTOLYSE ADULTE SANS SUCRE
- ABX PENTRA UREA CP
- B-PROTEI ALL 200g

Pour chaque produit, le système doit connaître :

| Donnée | Description |
|---|---|
| Stock disponible | Quantité vendable actuellement. |
| Stock réservé | Quantité réservée pour commandes clients. |
| Stock en quarantaine | Quantité non vendable. |
| Stock périmé | Quantité non vendable. |
| Stock en transit | Quantité commandée mais non reçue. |
| Ventes historiques | Historique des sorties. |
| Consommation moyenne | Vitesse de consommation du produit. |
| Délai fournisseur | Délai de fabrication et livraison. |
| Délai transport | Maritime, aérien, express, terrestre. |
| Délai transit/douane | Délai estimé de dédouanement. |
| Délai interne | Réception, contrôle qualité, mise en stock. |
| Stock de sécurité | Stock minimum de protection. |
| Couverture cible | Nombre de jours de stock souhaité. |
| Seuil de réapprovisionnement | Point de commande. |

---

### 4.3.2 Horizon de prévision

L’horizon de prévision est la période sur laquelle le système projette le stock.

Exemples :

| Horizon | Usage |
|---|---|
| 30 jours | Analyse court terme. |
| 60 jours | Analyse opérationnelle. |
| 90 jours | Analyse de réapprovisionnement standard. |
| 180 jours | Analyse semi-annuelle. |
| 365 jours | Analyse annuelle et saisonnalité. |

Pour LABMEDIS, un horizon recommandé est :

```text
Horizon MRP par défaut : 180 jours
```

Cela permet d’anticiper les commandes internationales qui peuvent nécessiter 3 à 4 mois d’avance.

---

### 4.3.3 Lead time total

Le lead time total est le délai complet entre la décision de commander et la disponibilité du produit dans l’entrepôt LABMEDIS.

Il doit être décomposé en plusieurs étapes :

```text
Lead time total =
    Délai de fabrication
  + Délai de préparation fournisseur
  + Délai de transport international
  + Délai de transit / douane
  + Délai de réception entrepôt
  + Délai de contrôle qualité
  + Délai de mise en stock
```

Exemple :

| Étape | Délai |
|---|---:|
| Fabrication | 45 jours |
| Préparation fournisseur | 5 jours |
| Transport maritime | 40 jours |
| Transit / douane | 15 jours |
| Réception entrepôt | 3 jours |
| Contrôle qualité | 2 jours |
| Mise en stock | 2 jours |
| **Lead time total** | **112 jours** |

---

### 4.3.4 Modes de transport et délais

Le mode de transport influence fortement le MRP.

| Mode | Délai estimé | Coût estimé | Usage recommandé |
|---|---:|---:|---|
| Maritime | Long | Faible | Commandes volumineuses non urgentes. |
| Aérien | Moyen | Élevé | Produits critiques ou urgents. |
| Express | Court | Très élevé | Rupture imminente, petites quantités. |
| Terrestre | Variable | Moyen | Fournisseurs régionaux. |

Exemple fournisseur :

| Fournisseur | Pays | Mode probable |
|---|---|---|
| Continental Commodities | France | Maritime ou aérien |
| HORIBA ABX SAS | France | Aérien ou express |
| GALPHARMA | Tunisie | Maritime ou terrestre/aérien |
| IBERMA | Maroc | Maritime ou terrestre |
| B&B LIFE SCIENCE | Inde | Maritime ou aérien |
| BIORESEARCH | Suisse | Aérien |
| MAIA AFRICA SAS | Burkina Faso | Terrestre |
| DEO GRATIAS PHARMA | Togo | Terrestre local |

---

### 4.3.5 Stock disponible

Le stock disponible est la quantité réellement vendable.

Formule :

```text
Stock disponible =
    Stock physique
  - Stock réservé
  - Stock en quarantaine
  - Stock périmé
```

Le moteur MRP ne doit jamais se baser uniquement sur le stock physique.

---

### 4.3.6 Stock en transit

Le stock en transit correspond aux quantités déjà commandées mais non encore reçues.

Formule :

```text
Stock en transit =
    Quantités sur commandes fournisseurs validées
  + Quantités sur expéditions en cours
  - Quantités déjà reçues
```

Le moteur doit pouvoir distinguer :

| Statut | Effet sur MRP |
|---|---|
| Commande brouillon | Non inclus. |
| Commande validée | Inclus. |
| Commande expédiée | Inclus. |
| Commande en transit | Inclus. |
| Commande partiellement reçue | Inclus pour le reste. |
| Commande annulée | Exclu. |

---

### 4.3.7 Consommation moyenne

La consommation moyenne est la vitesse à laquelle un produit sort du stock.

Elle peut être calculée à partir :

- des ventes,
- des sorties de stock,
- des livraisons clients,
- des commandes clients confirmées.

Formule simple :

```text
Consommation moyenne journalière =
    Quantité vendue sur la période / Nombre de jours de la période
```

Exemple :

| Produit | Ventes 90 jours | Consommation journalière |
|---|---:|---:|
| France Lait 1er âge 400g | 1 800 | 20 unités/jour |
| ALLERGICA 10MG CPR B/30 | 540 | 6 unités/jour |
| Pommade Maïa 100 ml | 1 440 | 16 unités/jour |

---

## 4.4 Formules principales du moteur MRP

### 4.4.1 Couverture de stock actuelle

La couverture de stock indique combien de jours le stock disponible peut tenir.

Formule :

```text
Couverture de stock =
    Stock disponible / Consommation moyenne journalière
```

Exemple :

```text
Stock disponible = 1 200
Consommation moyenne = 20 unités/jour

Couverture = 1 200 / 20 = 60 jours
```

Interprétation :

| Couverture | Statut |
|---:|---|
| Inférieure au lead time | Risque élevé de rupture. |
| Entre lead time et lead time + 15 jours | Risque modéré. |
| Supérieure à lead time + 30 jours | Stock confortable. |
| Supérieure à couverture maximale | Surstock potentiel. |

---

### 4.4.2 Point de commande

Le point de commande est le seuil qui doit déclencher une commande.

Formule :

```text
Point de commande =
    Consommation moyenne journalière × Lead time total
  + Stock de sécurité
```

Exemple :

| Paramètre | Valeur |
|---|---:|
| Consommation moyenne | 20 unités/jour |
| Lead time total | 110 jours |
| Stock de sécurité | 300 unités |

Calcul :

```text
Point de commande = 20 × 110 + 300 = 2 500 unités
```

Si :

```text
Stock disponible + Stock en transit <= Point de commande
```

alors le système déclenche une suggestion de commande.

---

### 4.4.3 Stock cible

Le stock cible est le niveau de stock que LABMEDIS souhaite atteindre après réception de la commande.

Formule :

```text
Stock cible =
    Consommation moyenne journalière × Couverture cible
  + Stock de sécurité
```

Exemple :

| Paramètre | Valeur |
|---|---:|
| Consommation moyenne | 20 unités/jour |
| Couverture cible | 120 jours |
| Stock de sécurité | 300 unités |

Calcul :

```text
Stock cible = 20 × 120 + 300 = 2 700 unités
```

---

### 4.4.4 Besoin net

Le besoin net représente la quantité manquante projetée.

Formule :

```text
Besoin net =
    Stock cible
  - Stock disponible
  - Stock en transit
```

Si :

```text
Besoin net > 0
```

alors une commande est nécessaire.

Si :

```text
Besoin net <= 0
```

alors aucune commande n’est nécessaire.

Exemple :

| Élément | Quantité |
|---|---:|
| Stock cible | 2 700 |
| Stock disponible | 1 200 |
| Stock en transit | 500 |

Calcul :

```text
Besoin net = 2 700 - 1 200 - 500 = 1 000
```

Le système suggère de commander 1 000 unités.

---

### 4.4.5 Quantité suggérée

La quantité suggérée peut être ajustée selon plusieurs contraintes :

```text
Quantité suggérée =
    Max(Besoin net, Quantité minimale fournisseur)
```

Puis ajustée selon :

| Contrainte | Description |
|---|---|
| Conditionnement | Arrondir au carton complet. |
| Quantité minimale fournisseur | MOQ imposée par le fabricant. |
| Capacité financière | Budget disponible. |
| Capacité de stockage | Limites physiques de l’entrepôt. |
| Péremption | Éviter le surstock sur produits à rotation lente. |
| Mode de transport | Optimisation du remplissage conteneur. |

---

### 4.4.6 Arrondi au conditionnement

Les produits LABMEDIS sont souvent conditionnés en cartons.

Exemples :

| Produit | Conditionnement |
|---|---|
| France Lait 1er âge 400g | carton/12 |
| France Lait 1er âge 900g | carton/6 |
| Pommade Maïa 100 ml | carton/72 |
| ALLERGICA 10MG CPR B/30 | carton/54 |
| GRIPEX Adulte sans sucre B/12 | carton/48 |

Si le besoin net est de 1 000 unités pour `France Lait 1er âge 400g` :

```text
Conditionnement = 12 unités par carton

1 000 / 12 = 83,33 cartons
```

Le système doit arrondir au carton supérieur :

```text
84 cartons
```

Quantité finale :

```text
84 × 12 = 1 008 unités
```

---

### 4.4.7 Date limite de commande

La date limite de commande est la date maximale à laquelle il faut commander pour éviter la rupture.

Formule :

```text
Date limite de commande =
    Date de rupture estimée - Lead time total
```

Exemple :

| Paramètre | Valeur |
|---|---|
| Date du jour | 28/08/2026 |
| Couverture stock actuel | 60 jours |
| Date de rupture estimée | 27/10/2026 |
| Lead time total | 110 jours |
| Date limite de commande | 09/07/2026 |

Si la date limite de commande est déjà dépassée, le système doit marquer le produit comme **urgent**.

---

## 4.5 Workflow du moteur MRP

### 4.5.1 Workflow global

```text
Déclenchement du calcul MRP
        ↓
Récupération des produits actifs suivis MRP
        ↓
Récupération du stock disponible
        ↓
Récupération du stock réservé
        ↓
Récupération du stock en quarantaine
        ↓
Récupération du stock périmé
        ↓
Récupération du stock en transit
        ↓
Récupération des ventes historiques
        ↓
Calcul de la consommation moyenne
        ↓
Application de la saisonnalité éventuelle
        ↓
Récupération des délais fournisseurs
        ↓
Calcul du lead time total
        ↓
Calcul du point de commande
        ↓
Calcul du stock cible
        ↓
Calcul du besoin net
        ↓
Détection des risques de rupture
        ↓
Génération des suggestions de commande
        ↓
Classement par priorité
        ↓
Affichage dans le dashboard React
        ↓
Validation par responsable achats
        ↓
Transformation en commande fournisseur
```

---

### 4.5.2 Déclenchement du MRP

Le moteur peut être déclenché de plusieurs manières :

| Mode | Description |
|---|---|
| Automatique quotidien | Job Hangfire chaque jour. |
| Automatique hebdomadaire | Job Hangfire chaque lundi. |
| Manuel | Bouton “Lancer le calcul MRP” dans React. |
| Sur événement | Après réception importante, vente exceptionnelle ou rupture. |
| Simulation | Test sans génération de suggestion réelle. |

Recommandation :

```text
Job quotidien à 05h00 UTC
```

---

## 4.6 Calcul de la consommation historique

### 4.6.1 Sources de données

La consommation peut provenir de :

1. Commandes clients livrées.
2. Mouvements de stock de type vente.
3. Bons de livraison validés.
4. Factures.
5. Retours clients déduits.

Formule recommandée :

```text
Consommation nette =
    Sorties vente
  - Retours clients acceptés
```

---

### 4.6.2 Périodes recommandées

| Période | Usage |
|---|---|
| 30 jours | Analyse très court terme. |
| 60 jours | Analyse court terme. |
| 90 jours | Base recommandée pour MRP. |
| 180 jours | Analyse tendance. |
| 365 jours | Analyse saisonnalité. |

---

### 4.6.3 Moyenne simple

```text
Consommation journalière = Quantité vendue / Nombre de jours
```

Exemple :

```text
1 800 unités vendues sur 90 jours

1 800 / 90 = 20 unités/jour
```

---

### 4.6.4 Moyenne pondérée

Pour donner plus de poids aux ventes récentes :

| Période | Pondération |
|---|---:|
| 30 derniers jours | 50% |
| 31 à 60 jours | 30% |
| 61 à 90 jours | 20% |

Formule :

```text
Consommation pondérée =
    (Conso période 1 × 50%)
  + (Conso période 2 × 30%)
  + (Conso période 3 × 20%)
```

---

### 4.6.5 Gestion des ventes exceptionnelles

Certaines ventes peuvent fausser la prévision.

Exemple :

- grosse commande exceptionnelle CAMEG,
- appel d’offres,
- commande ponctuelle d’une clinique,
- livraison importante à LABOREX TOGO.

Le système doit permettre :

1. de marquer une vente comme exceptionnelle,
2. de l’exclure du calcul MRP,
3. de la réintégrer manuellement,
4. de lisser la consommation sur plusieurs mois.

---

## 4.7 Gestion des délais fournisseurs

### 4.7.1 Paramètres par fournisseur

Chaque fournisseur doit pouvoir avoir des délais configurables.

Exemple :

| Fournisseur | Fabrication | Transport maritime | Transport aérien | Transit |
|---|---:|---:|---:|---:|
| Continental Commodities | 45 j | 40 j | 7 j | 15 j |
| HORIBA ABX SAS | 20 j | 35 j | 5 j | 10 j |
| GALPHARMA | 30 j | 25 j | 5 j | 10 j |
| B&B LIFE SCIENCE | 60 j | 45 j | 10 j | 20 j |
| MAIA AFRICA SAS | 15 j | - | - | 5 j |
| DEO GRATIAS PHARMA | 7 j | - | - | 0 j |

---

### 4.7.2 Paramètres par produit

Certains produits peuvent avoir des délais spécifiques.

Exemple :

| Produit | Délai spécifique |
|---|---|
| Réactifs HORIBA | Délai technique, contrôle qualité particulier. |
| France Lait | Production par lot, conteneur. |
| GRIPEX | Produit saisonnier. |
| ABX PENTRA UREA CP | Produit technique de laboratoire. |

---

### 4.7.3 Délai interne LABMEDIS

Le délai interne doit inclure :

| Étape | Délai estimé |
|---|---:|
| Réception physique | 1 à 3 jours |
| Contrôle documentaire | 1 jour |
| Contrôle qualité | 1 à 3 jours |
| Mise en stock | 1 à 2 jours |
| Libération lot | 1 jour |

Valeur par défaut recommandée :

```text
Délai interne = 5 jours
```

---

## 4.8 Niveaux de priorité MRP

Le moteur doit classer les produits selon leur criticité.

### 4.8.1 Formule de criticité

```text
Jours avant rupture =
    Stock disponible / Consommation moyenne journalière
```

### 4.8.2 Classification recommandée

| Niveau | Condition | Action recommandée |
|---|---|---|
| Critique | Jours avant rupture <= Lead time total | Commander immédiatement. |
| Urgent | Jours avant rupture <= Lead time total + 15 jours | Créer suggestion prioritaire. |
| À surveiller | Jours avant rupture <= Lead time total + 30 jours | Surveiller dans le dashboard. |
| Normal | Jours avant rupture > Lead time total + 30 jours | Pas d’action immédiate. |
| Surstock | Couverture > couverture cible + 60 jours | Alerter sur surstock. |

---

## 4.9 Suggestions de commande

### 4.9.1 Contenu d’une suggestion

Chaque suggestion de commande doit contenir :

| Champ | Description |
|---|---|
| Produit | Produit concerné. |
| Fournisseur | Fournisseur recommandé. |
| Quantité suggérée | Quantité calculée. |
| Conditionnement | Quantité en cartons. |
| Stock disponible | Stock actuel. |
| Stock en transit | Stock déjà commandé. |
| Consommation moyenne | Vitesse de consommation. |
| Lead time total | Délai total estimé. |
| Date limite de commande | Date maximale pour commander. |
| Date de réception estimée | Date estimée après commande. |
| Mode de transport recommandé | Maritime, aérien, express. |
| Niveau de priorité | Critique, urgent, normal. |
| Coût estimé | Estimation du coût d’achat. |
| Statut | Suggestion, validée, convertie, rejetée. |

---

### 4.9.2 Exemple de suggestion

| Champ | Valeur |
|---|---|
| Produit | France Lait 1er âge 400g |
| Fournisseur | CONTINENTAL COMMODITIES |
| Stock disponible | 1 200 |
| Stock en transit | 500 |
| Consommation moyenne | 20 unités/jour |
| Lead time total | 110 jours |
| Point de commande | 2 500 |
| Stock cible | 2 700 |
| Besoin net | 1 000 |
| Conditionnement | carton/12 |
| Quantité suggérée | 1 008 unités |
| Cartons suggérés | 84 |
| Mode recommandé | Maritime |
| Priorité | Urgent |

---

## 4.10 Mode de transport recommandé

Le moteur peut recommander un mode de transport selon l’urgence.

### 4.10.1 Règle simple

```text
Si jours avant rupture < lead time maritime
alors recommander aérien ou express
sinon recommander maritime
```

### 4.10.2 Exemple

Produit : `ALLERGICA 10MG CPR B/30`

| Paramètre | Valeur |
|---|---:|
| Stock disponible | 120 |
| Consommation | 6 unités/jour |
| Jours avant rupture | 20 jours |
| Lead time maritime | 55 jours |
| Lead time aérien | 15 jours |

Décision :

```text
Maritime impossible car rupture avant réception.
Recommandation : aérien.
```

---

## 4.11 Gestion de la saisonnalité

Certains produits peuvent avoir une consommation saisonnière.

Exemples possibles :

| Produit | Saison possible |
|---|---|
| GRIPEX Adulte | Saison grippale. |
| GRIPEX Enfant | Saison grippale. |
| ALLERGICA | Périodes d’allergie. |
| Pommade Maïa | Périodes fortes, anti-moustiques. |
| Strick Out | Périodes de traitement insecticide. |
| France Lait | Consommation régulière mais sensible aux campagnes. |

Le système doit permettre :

1. d’activer un coefficient saisonnier par produit,
2. de définir des périodes hautes et basses,
3. d’appliquer un multiplicateur de consommation,
4. de comparer N-1 si historique disponible.

Formule :

```text
Consommation prévisionnelle =
    Consommation historique × Coefficient saisonnier
```

Exemple :

```text
Consommation historique = 20 unités/jour
Coefficient saisonnier = 1,30

Consommation prévisionnelle = 26 unités/jour
```

---

## 4.12 Gestion du surstock

Le MRP ne doit pas uniquement prévenir les ruptures. Il doit aussi éviter le surstock.

### 4.12.1 Détection du surstock

```text
Si couverture actuelle > couverture cible + seuil surstock
alors alerte surstock
```

Exemple :

| Paramètre | Valeur |
|---|---:|
| Couverture cible | 120 jours |
| Seuil surstock | 60 jours |
| Couverture actuelle | 220 jours |

Alerte :

```text
Surstock détecté : 220 jours de couverture.
```

---

### 4.12.2 Actions possibles

| Action | Description |
|---|---|
| Ne pas recommander commande | Bloquer suggestion automatique. |
| Proposer promotion | Accélérer rotation. |
| Proposer transfert | Déplacer vers un autre canal. |
| Proposer retour fournisseur | Si accord fournisseur. |
| Alerter direction | Décision commerciale ou financière. |

---

## 4.13 Intégration avec les achats

Le moteur MRP doit être connecté au module Achats.

### 4.13.1 Transformation d’une suggestion en commande

Workflow :

```text
Suggestion MRP
    ↓
Validation responsable achats
    ↓
Ajustement quantité si besoin
    ↓
Sélection fournisseur
    ↓
Sélection mode de transport
    ↓
Création commande fournisseur
    ↓
Statut suggestion : Convertie
```

---

### 4.13.2 Règles de conversion

| Règle | Description |
|---|---|
| Une suggestion peut être convertie en commande | Oui. |
| Plusieurs suggestions peuvent être regroupées | Oui, par fournisseur. |
| Une suggestion peut être rejetée | Oui, avec motif. |
| Une suggestion peut être modifiée | Oui, avant conversion. |
| Une suggestion convertie doit être liée à la commande | Oui, traçabilité. |
| Une commande issue du MRP doit conserver l’historique | Oui. |

---

## 4.14 Alertes et notifications

### 4.14.1 Types d’alertes MRP

| Alerte | Déclencheur |
|---|---|
| Risque critique | Rupture estimée avant réception possible. |
| Risque urgent | Point de commande atteint. |
| Produit à surveiller | Couverture proche du seuil. |
| Retard fournisseur | Commande non expédiée à la date prévue. |
| Retard transport | Expédition en retard. |
| Surstock | Couverture excessive. |
| Consommation anormale | Hausse ou baisse brutale des ventes. |
| Suggestion non traitée | Suggestion en attente depuis trop longtemps. |

---

### 4.14.2 Notifications temps réel

Le backend doit utiliser SignalR pour envoyer :

```text
MRP_CALCULATION_COMPLETED
LOW_STOCK_ALERT
CRITICAL_STOCK_ALERT
OVERSTOCK_ALERT
REORDER_SUGGESTION_CREATED
REORDER_SUGGESTION_CONVERTED
SUPPLIER_DELAY_ALERT
```

---

## 4.15 Impact sur le Frontend React

Le frontend doit fournir un espace dédié aux prévisions et au MRP.

---

### 4.15.1 Page Dashboard Prévisions

Cette page doit afficher :

| Indicateur | Description |
|---|---|
| Produits critiques | Produits avec risque de rupture. |
| Produits urgents | Produits à commander rapidement. |
| Produits à surveiller | Produits proches du seuil. |
| Produits en surstock | Produits avec couverture excessive. |
| Suggestions en attente | Suggestions non converties. |
| Commandes en transit | Commandes non reçues. |
| Valeur du stock | Valeur financière du stock. |
| Valeur à commander | Estimation des achats nécessaires. |

---

### 4.15.2 Tableau des produits à risque

Colonnes recommandées :

| Colonne | Description |
|---|---|
| Produit | Désignation. |
| Catégorie | Produit infantile, médicament, etc. |
| Fournisseur | Fournisseur principal. |
| Stock disponible | Quantité vendable. |
| Stock en transit | Quantité commandée. |
| Consommation/jour | Vitesse de sortie. |
| Couverture | Nombre de jours restants. |
| Lead time | Délai total. |
| Point de commande | Seuil de déclenchement. |
| Date limite commande | Date maximale. |
| Priorité | Critique, urgent, normal. |
| Action | Voir détail, créer suggestion, commander. |

---

### 4.15.3 Détail produit prévisionnel

La page détail doit afficher :

1. Informations produit.
2. Stock actuel.
3. Stock réservé.
4. Stock en quarantaine.
5. Stock en transit.
6. Ventes historiques.
7. Graphique de consommation.
8. Paramètres MRP.
9. Délais fournisseurs.
10. Suggestions précédentes.
11. Historique des décisions.

---

### 4.15.4 Graphiques recommandés

| Graphique | Usage |
|---|---|
| Courbe des ventes | Visualiser la consommation. |
| Courbe de projection stock | Visualiser la rupture future. |
| Barres par mois | Analyser la saisonnalité. |
| Jauge de couverture | Visualiser les jours restants. |
| Diagramme par fournisseur | Suivre les achats à prévoir. |
| Histogramme des écarts | Comparer prévision et réel. |

---

### 4.15.5 Simulation MRP

Le frontend doit permettre une simulation avant commande.

Paramètres modifiables :

- consommation moyenne,
- stock de sécurité,
- couverture cible,
- délai fournisseur,
- mode de transport,
- quantité suggérée,
- conditionnement.

Résultats :

- besoin net,
- quantité recommandée,
- date de réception estimée,
- coût estimé,
- impact sur couverture.

---

## 4.16 Impact sur le Backend .NET

Le backend doit implémenter les entités, services, jobs et endpoints nécessaires au moteur MRP.

---

## 4.16.1 Entités principales

Toutes les entités doivent hériter de `BaseEntity`.

### `ForecastParameter`

```csharp
public class ForecastParameter : BaseEntity
{
    public Guid ProductId { get; set; }
    public bool IsMtpEnabled { get; set; }
    public int ForecastHorizonDays { get; set; }
    public int SafetyStock { get; set; }
    public int TargetCoverageDays { get; set; }
    public int ReorderCoverageDays { get; set; }
    public int OverstockThresholdDays { get; set; }
    public ForecastMethod Method { get; set; }
    public decimal? SeasonalityFactor { get; set; }

    public Product Product { get; set; }
}
```

---

### `SupplierLeadTime`

```csharp
public class SupplierLeadTime : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Guid? ProductId { get; set; }
    public TransportMode TransportMode { get; set; }
    public int ManufacturingLeadTimeDays { get; set; }
    public int PreparationLeadTimeDays { get; set; }
    public int TransportLeadTimeDays { get; set; }
    public int CustomsLeadTimeDays { get; set; }
    public int InternalLeadTimeDays { get; set; }

    public Supplier Supplier { get; set; }
    public Product Product { get; set; }
}
```

---

### `ForecastCalculation`

```csharp
public class ForecastCalculation : BaseEntity
{
    public Guid ProductId { get; set; }
    public DateTime CalculationDate { get; set; }
    public int AvailableStock { get; set; }
    public int ReservedStock { get; set; }
    public int TransitStock { get; set; }
    public decimal AverageDailyConsumption { get; set; }
    public int LeadTimeDays { get; set; }
    public int SafetyStock { get; set; }
    public int ReorderPoint { get; set; }
    public int TargetStock { get; set; }
    public int NetRequirement { get; set; }
    public int CoverageDays { get; set; }
    public ForecastRiskLevel RiskLevel { get; set; }

    public Product Product { get; set; }
}
```

---

### `ReorderSuggestion`

```csharp
public class ReorderSuggestion : BaseEntity
{
    public Guid ForecastCalculationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid SupplierId { get; set; }
    public int SuggestedQuantity { get; set; }
    public int SuggestedCartonQuantity { get; set; }
    public TransportMode SuggestedTransportMode { get; set; }
    public DateTime SuggestedOrderDate { get; set; }
    public DateTime EstimatedReceptionDate { get; set; }
    public SuggestionStatus Status { get; set; }
    public string RejectionReason { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public ForecastCalculation ForecastCalculation { get; set; }
    public Product Product { get; set; }
    public Supplier Supplier { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
}
```

---

## 4.16.2 Enums recommandés

```csharp
public enum ForecastMethod
{
    Average30Days = 0,
    Average60Days = 1,
    Average90Days = 2,
    Weighted90Days = 3,
    Seasonal365Days = 4,
    Manual = 5
}
```

```csharp
public enum ForecastRiskLevel
{
    Normal = 0,
    Watch = 1,
    Urgent = 2,
    Critical = 3,
    Overstock = 4
}
```

```csharp
public enum SuggestionStatus
{
    Pending = 0,
    Validated = 1,
    Converted = 2,
    Rejected = 3,
    Expired = 4
}
```

---

## 4.16.3 Services recommandés

```csharp
IForecastParameterService
ISupplierLeadTimeService
IForecastCalculationService
IReorderSuggestionService
IMrpService
```

Le service principal doit respecter la règle d’architecture :

```csharp
public class MrpService : ForecastCalculationRepository, IMrpService
{
    private readonly ILoggerManager _logger;

    public MrpService(AppDbContext context, ILoggerManager logger) : base(context)
    {
        _logger = logger;
    }
}
```

---

## 4.16.4 Logique de calcul simplifiée

```csharp
public class MrpCalculationResult
{
    public int AvailableStock { get; set; }
    public int TransitStock { get; set; }
    public decimal AverageDailyConsumption { get; set; }
    public int LeadTimeDays { get; set; }
    public int SafetyStock { get; set; }
    public int ReorderPoint { get; set; }
    public int TargetStock { get; set; }
    public int NetRequirement { get; set; }
    public int CoverageDays { get; set; }
    public ForecastRiskLevel RiskLevel { get; set; }
}
```

```csharp
public MrpCalculationResult CalculateProductMrp(
    int availableStock,
    int transitStock,
    decimal averageDailyConsumption,
    int leadTimeDays,
    int safetyStock,
    int targetCoverageDays)
{
    int reorderPoint = Convert.ToInt32(Math.Ceiling(averageDailyConsumption * leadTimeDays)) + safetyStock;

    int targetStock = Convert.ToInt32(Math.Ceiling(averageDailyConsumption * targetCoverageDays)) + safetyStock;

    int netRequirement = targetStock - availableStock - transitStock;

    if (netRequirement < 0)
        netRequirement = 0;

    int coverageDays = averageDailyConsumption > 0
        ? Convert.ToInt32(Math.Floor(availableStock / averageDailyConsumption))
        : int.MaxValue;

    var riskLevel = ForecastRiskLevel.Normal;

    if (coverageDays <= leadTimeDays)
        riskLevel = ForecastRiskLevel.Critical;
    else if (coverageDays <= leadTimeDays + 15)
        riskLevel = ForecastRiskLevel.Urgent;
    else if (coverageDays <= leadTimeDays + 30)
        riskLevel = ForecastRiskLevel.Watch;

    return new MrpCalculationResult
    {
        AvailableStock = availableStock,
        TransitStock = transitStock,
        AverageDailyConsumption = averageDailyConsumption,
        LeadTimeDays = leadTimeDays,
        SafetyStock = safetyStock,
        ReorderPoint = reorderPoint,
        TargetStock = targetStock,
        NetRequirement = netRequirement,
        CoverageDays = coverageDays,
        RiskLevel = riskLevel
    };
}
```

---

## 4.16.5 Endpoints API recommandés

### Paramètres MRP

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/forecast/parameters` | Liste des paramètres produits. |
| GET | `/api/forecast/parameters/{productId}` | Paramètres d’un produit. |
| POST | `/api/forecast/parameters` | Créer paramètres. |
| PUT | `/api/forecast/parameters/{productId}` | Modifier paramètres. |

---

### Calcul MRP

| Méthode | Route | Description |
|---|---|---|
| POST | `/api/forecast/run` | Lancer le calcul MRP global. |
| POST | `/api/forecast/run/{productId}` | Lancer le calcul pour un produit. |
| GET | `/api/forecast/calculations` | Historique des calculs. |
| GET | `/api/forecast/calculations/{productId}` | Dernier calcul d’un produit. |

---

### Produits à risque

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/forecast/products-at-risk` | Produits en risque de rupture. |
| GET | `/api/forecast/critical-products` | Produits critiques. |
| GET | `/api/forecast/overstock-products` | Produits en surstock. |

---

### Suggestions

| Méthode | Route | Description |
|---|---|---|
| GET | `/api/reorder-suggestions` | Liste des suggestions. |
| GET | `/api/reorder-suggestions/{id}` | Détail d’une suggestion. |
| POST | `/api/reorder-suggestions` | Créer une suggestion manuelle. |
| POST | `/api/reorder-suggestions/{id}/validate` | Valider une suggestion. |
| POST | `/api/reorder-suggestions/{id}/reject` | Rejeter une suggestion. |
| POST | `/api/reorder-suggestions/{id}/convert-to-purchase-order` | Convertir en commande fournisseur. |

---

### Simulation

| Méthode | Route | Description |
|---|---|---|
| POST | `/api/forecast/simulate` | Simuler un besoin MRP. |
| POST | `/api/forecast/simulate/product/{productId}` | Simuler pour un produit. |

---

## 4.16.6 Exemple de contrôleur

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IMrpService _mrpService;
    private readonly ILoggerManager _logger;
    private readonly IUserService _userService;

    public ForecastController(
        IMrpService mrpService,
        ILoggerManager logger,
        IUserService userService)
    {
        _mrpService = mrpService;
        _logger = logger;
        _userService = userService;
    }

    [HttpGet("products-at-risk")]
    public async Task<IActionResult> GetProductsAtRisk()
    {
        var user = await _userService.GetCurrentUserAsync(User);

        _logger.LogInfo($"{user?.LastName} {user?.FirstName} ({user?.UserName}) | Début GetProductsAtRisk | {Request.Method} {Request.Path} IP: {Request.GetIp()} UserManager: {Request.GetUserAgentName()}");

        try
        {
            var result = await _mrpService.GetProductsAtRiskAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{user?.LastName} ... | Echec GetProductsAtRisk : {ex.Message} | IP: {Request.GetIp()}");

            return BadRequest(new
            {
                message = "Impossible de récupérer les produits à risque."
            });
        }
    }
}
```

---

## 4.17 Jobs Hangfire recommandés

### 4.17.1 Job quotidien MRP

```text
DailyMrpCalculationJob
```

Fréquence recommandée :

```text
Tous les jours à 05h00
```

Actions :

1. Récupérer les produits actifs suivis MRP.
2. Calculer les stocks disponibles.
3. Calculer les stocks en transit.
4. Calculer les consommations moyennes.
5. Appliquer les paramètres MRP.
6. Calculer les risques.
7. Générer les suggestions.
8. Envoyer les notifications SignalR.
9. Enregistrer l’historique.

---

### 4.17.2 Job de suivi des commandes en transit

```text
TransitDelayAlertJob
```

Actions :

1. Vérifier les commandes fournisseurs non reçues.
2. Comparer la date de réception estimée avec la date actuelle.
3. Détecter les retards.
4. Alerter les responsables achats/logistique.

---

### 4.17.3 Job de suivi des suggestions non traitées

```text
PendingSuggestionAlertJob
```

Actions :

1. Identifier les suggestions en attente depuis plus de X jours.
2. Envoyer une relance.
3. Marquer certaines suggestions comme expirées si nécessaire.

---

## 4.18 User Stories détaillées

### US-MRP-01 : Configurer les paramètres MRP d’un produit

**En tant que** responsable achats,  
**je veux** configurer les paramètres MRP d’un produit,  
**afin de** contrôler son réapprovisionnement.

**Critères d’acceptation :**

1. L’utilisateur peut activer ou désactiver le MRP sur un produit.
2. L’utilisateur peut définir le stock de sécurité.
3. L’utilisateur peut définir la couverture cible.
4. L’utilisateur peut définir l’horizon de prévision.
5. L’utilisateur peut choisir la méthode de calcul.
6. Les paramètres sont historisés.
7. Le moteur utilise ces paramètres lors du calcul.

---

### US-MRP-02 : Configurer les délais fournisseurs

**En tant que** responsable achats,  
**je veux** définir les délais fournisseurs par mode de transport,  
**afin de** calculer correctement la date limite de commande.

**Critères d’acceptation :**

1. Les délais peuvent être définis par fournisseur.
2. Les délais peuvent être surchargés par produit.
3. Les délais peuvent être définis par mode de transport.
4. Le système calcule automatiquement le lead time total.
5. Les délais sont modifiables.
6. Les modifications sont journalisées.

---

### US-MRP-03 : Lancer un calcul MRP manuel

**En tant que** responsable achats,  
**je veux** lancer un calcul MRP manuel,  
**afin de** mettre à jour les prévisions.

**Critères d’acceptation :**

1. Un bouton permet de lancer le calcul.
2. Le système affiche une progression.
3. Le calcul peut être global ou par produit.
4. Le résultat est affiché dans le dashboard.
5. Le calcul est historisé.
6. Une notification SignalR est envoyée à la fin.

---

### US-MRP-04 : Consulter les produits à risque

**En tant que** direction ou responsable achats,  
**je veux** voir les produits à risque de rupture,  
**afin de** prioriser les commandes.

**Critères d’acceptation :**

1. La liste affiche les produits critiques, urgents et à surveiller.
2. Le tableau peut être filtré par catégorie, fournisseur et priorité.
3. Le tableau affiche la couverture actuelle.
4. Le tableau affiche la date limite de commande.
5. Le tableau affiche la quantité suggérée.
6. L’utilisateur peut accéder au détail du produit.

---

### US-MRP-05 : Voir le détail prévisionnel d’un produit

**En tant que** responsable achats,  
**je veux** voir le détail prévisionnel d’un produit,  
**afin de** comprendre pourquoi une commande est suggérée.

**Critères d’acceptation :**

1. Le détail affiche le stock disponible.
2. Le détail affiche le stock en transit.
3. Le détail affiche la consommation moyenne.
4. Le détail affiche le lead time total.
5. Le détail affiche le point de commande.
6. Le détail affiche le besoin net.
7. Le détail affiche un graphique de projection.
8. Le détail affiche l’historique des suggestions.

---

### US-MRP-06 : Simuler une commande

**En tant que** responsable achats,  
**je veux** simuler une commande,  
**afin de** vérifier l’impact avant validation.

**Critères d’acceptation :**

1. L’utilisateur peut modifier la quantité.
2. L’utilisateur peut modifier le mode de transport.
3. L’utilisateur peut modifier les délais.
4. Le système recalcule la couverture future.
5. Le système affiche une estimation de coût.
6. La simulation n’enregistre pas de commande réelle.

---

### US-MRP-07 : Valider une suggestion de commande

**En tant que** responsable achats,  
**je veux** valider une suggestion de commande,  
**afin de** préparer la commande fournisseur.

**Critères d’acceptation :**

1. La suggestion doit être en statut `Pending`.
2. L’utilisateur peut ajuster la quantité.
3. L’utilisateur peut ajuster le fournisseur.
4. L’utilisateur peut ajuster le mode de transport.
5. La validation change le statut en `Validated`.
6. L’action est journalisée.

---

### US-MRP-08 : Convertir une suggestion en commande fournisseur

**En tant que** responsable achats,  
**je veux** convertir une suggestion en commande fournisseur,  
**afin de** lancer l’achat.

**Critères d’acceptation :**

1. La suggestion validée peut être convertie.
2. Le système crée une commande fournisseur.
3. La commande contient le produit et la quantité.
4. La commande est liée à la suggestion.
5. La suggestion passe au statut `Converted`.
6. La commande est visible dans le module Achats.

---

### US-MRP-09 : Rejeter une suggestion

**En tant que** responsable achats,  
**je veux** rejeter une suggestion,  
**afin de** ne pas commander un produit non nécessaire.

**Critères d’acceptation :**

1. Le rejet nécessite un motif.
2. La suggestion passe au statut `Rejected`.
3. Le motif est enregistré.
4. Le produit reste suivi par le MRP.
5. L’action est journalisée.

---

### US-MRP-10 : Recevoir une alerte de rupture critique

**En tant que** responsable achats ou direction,  
**je veux** recevoir une alerte lorsqu’un produit est critique,  
**afin de** prendre une décision rapide.

**Critères d’acceptation :**

1. L’alerte est envoyée via SignalR.
2. L’alerte peut aussi être envoyée par email si configurée.
3. L’alerte contient le produit, le stock, la couverture et la date limite.
4. L’utilisateur peut cliquer sur l’alerte pour ouvrir le détail.
5. L’alerte est historisée.

---

## 4.19 Exemple complet : France Lait 1er âge 400g

### Données

| Paramètre | Valeur |
|---|---:|
| Produit | France Lait 1er âge 400g |
| Fournisseur | CONTINENTAL COMMODITIES |
| Conditionnement | carton/12 |
| Stock disponible | 1 200 |
| Stock réservé | 150 |
| Stock en transit | 500 |
| Ventes 90 jours | 1 800 |
| Consommation moyenne | 20 unités/jour |
| Délai fabrication | 45 jours |
| Délai transport maritime | 40 jours |
| Délai transit/douane | 15 jours |
| Délai interne | 10 jours |
| Lead time total | 110 jours |
| Stock de sécurité | 300 |
| Couverture cible | 120 jours |

---

### Calcul du stock réellement disponible

```text
Stock physique = 1 200
Stock réservé = 150

Stock réellement disponible = 1 200 - 150 = 1 050
```

---

### Calcul couverture

```text
Couverture = 1 050 / 20 = 52,5 jours
```

---

### Calcul point de commande

```text
Point de commande = 20 × 110 + 300 = 2 500
```

---

### Stock pris en compte

```text
Stock disponible + Stock en transit = 1 050 + 500 = 1 550
```

---

### Déclenchement

```text
1 550 <= 2 500
```

Donc :

```text
Commande recommandée
```

---

### Stock cible

```text
Stock cible = 20 × 120 + 300 = 2 700
```

---

### Besoin net

```text
Besoin net = 2 700 - 1 550 = 1 150
```

---

### Arrondi conditionnement

```text
1 150 / 12 = 95,83 cartons
```

Arrondi :

```text
96 cartons
```

Quantité finale :

```text
96 × 12 = 1 152 unités
```

---

### Résultat

| Résultat | Valeur |
|---|---:|
| Couverture actuelle | 52,5 jours |
| Lead time total | 110 jours |
| Niveau de risque | Critique |
| Quantité suggérée | 1 152 unités |
| Conditionnement | 96 cartons |
| Mode recommandé | Aérien si urgence, maritime si encore possible |
| Action | Commander immédiatement |

---

## 4.20 Règles de validation métier

Le moteur doit respecter les règles suivantes :

| Règle | Description |
|---|---|
| Produit actif | Seuls les produits actifs sont calculés. |
| Produit suivi MRP | Le produit doit avoir le MRP activé. |
| Consommation nulle | Si aucune vente, ne pas suggérer commande automatique. |
| Stock négatif | Interdire les calculs avec stock négatif. |
| Délais manquants | Signaler les produits sans délais configurés. |
| Fournisseur manquant | Signaler les produits sans fournisseur principal. |
| Commande en cours | Ne pas proposer commande si besoin déjà couvert. |
| Surstock | Ne pas suggérer commande si couverture excessive. |
| Quantité minimale | Respecter le MOQ fournisseur si défini. |
| Conditionnement | Arrondir au carton supérieur si obligatoire. |

---

## 4.21 KPIs du module MRP

Le système doit fournir des indicateurs de performance :

| KPI | Description |
|---|---|
| Nombre de produits critiques | Produits en risque immédiat. |
| Nombre de produits urgents | Produits à commander rapidement. |
| Nombre de suggestions générées | Volume de suggestions MRP. |
| Suggestions converties | Suggestions transformées en commande. |
| Suggestions rejetées | Suggestions non retenues. |
| Taux de rupture | Nombre de ruptures réelles. |
| Taux de service | Capacité à servir les clients sans rupture. |
| Précision des prévisions | Écart entre prévision et consommation réelle. |
| Couverture moyenne | Nombre moyen de jours de stock. |
| Valeur du surstock | Valeur immobilisée inutilement. |
| Valeur à commander | Montant estimé des achats nécessaires. |
| Délai moyen fournisseur | Délai réel constaté. |

---

## 4.22 Points à valider avec LABMEDIS

Avant développement complet, les points suivants doivent être confirmés :

| Question | Impact |
|---|---|
| Quelle est la période de vente de référence recommandée ? | Calcul de consommation. |
| Faut-il utiliser 30, 60, 90 ou 180 jours ? | Précision MRP. |
| Faut-il une moyenne pondérée ? | Prévision plus fine. |
| Faut-il gérer la saisonnalité ? | Produits comme GRIPEX. |
| Faut-il un stock de sécurité par produit ou par catégorie ? | Paramétrage. |
| Faut-il une couverture cible différente par produit ? | Paramétrage. |
| Faut-il bloquer les commandes si surstock ? | Règle métier. |
| Faut-il suggérer automatiquement le mode de transport ? | Aide à la décision. |
| Faut-il gérer plusieurs fournisseurs pour un même produit ? | Flexibilité achat. |
| Faut-il gérer les quantités minimales fournisseur ? | Arrondi quantité. |
| Faut-il convertir plusieurs suggestions en une seule commande ? | Regroupement fournisseur. |
| Faut-il notifier par email en plus de SignalR ? | Alertes. |
| Faut-il exporter les suggestions en Excel ? | Reporting. |
| Faut-il comparer prévision et consommation réelle ? | Amélioration continue. |

---

## 4.23 Synthèse

Le module **Moteur d’Anticipation (MRP) & Prévisions** doit permettre à LABMEDIS de passer d’une gestion réactive à une gestion proactive des approvisionnements.

Il doit répondre automatiquement à :

```text
Quel produit commander ?
Quand commander ?
Quelle quantité commander ?
Chez quel fournisseur ?
Par quel mode de transport ?
Quel est le risque de rupture ?
Quel est l’impact sur le stock futur ?
```

Le module devra être intégré :

- au stock, pour connaître la disponibilité réelle,
- aux achats, pour transformer les suggestions en commandes,
- au transport, pour estimer les délais,
- aux ventes, pour analyser la consommation,
- au frontend React, pour fournir un dashboard décisionnel,
- au backend .NET, via des services robustes, des jobs Hangfire et des notifications SignalR.