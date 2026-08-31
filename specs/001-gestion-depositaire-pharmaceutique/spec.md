# Feature Specification: Système de Gestion LABMEDIS (Dépositaire Pharmaceutique)

**Feature Branch**: `001-gestion-depositaire-pharmaceutique`

**Created**: 2026-08-28

**Status**: Draft

**Input**: User description : "Lire tous les fichiers du dossier wiki (sans rien omettre) et rédiger une spécification complète du projet LABMEDIS à partir de leur contenu intégral."

**Source** : Intégralité de `wiki/LABMEDIS/` (00-vision, 01-features/FR-001 à FR-014, 02-workflows/WF-001 à WF-008, 03-data-model/ENT-001 à ENT-015, 04-api-contracts/API-001 à API-010, 05-ui-states/UI-001 à UI-006, 06-regles-metier/RG-001 à RG-009, 07-nfr, 08-architecture, 09-securite, 10-tests, 99-a-clarifier) et `wiki/_meta/` (disputes, glossaire, index, log, quality-gates).

## Contexte Métier

LABMEDIS remplace une gestion actuelle par Excel par un système exhaustif de contrôle des stocks, achats, ventes et traçabilité des lots pour un dépositaire pharmaceutique important/revend des produits de santé (médicaments, produits infantiles, réactifs de laboratoire, cosmétiques, compléments alimentaires, insecticides) au Togo, sous régime réglementaire UEMOA/CEDEAO et BPD (Bonnes Pratiques de Distribution), avec autorisation DPML (Direction de la Pharmacie et du Médicament de Lomé). Catalogue actuel : 138 références. Fournisseurs internationaux (France, Tunisie, Maroc, Inde, Suisse, Burkina Faso, Togo). Clients : répartiteurs, hôpitaux, cliniques, pharmacies, centrales d'achat.

## Clarifications

### Session 2026-08-28

- Q: Que doit faire le système lorsque deux commandes de vente tentent simultanément de réserver les dernières unités disponibles d'un même lot ? → A: Vérification optimiste — les deux confirmations sont acceptées en parallèle ; celle qui échouerait à trouver un stock suffisant au moment de la validation finale est rejetée avec l'erreur "stock insuffisant" (INSUFFICIENT_STOCK) et invitée à relancer avec la disponibilité à jour.
- Q: Quel volume de charge concurrente (utilisateurs actifs simultanés) le système doit-il supporter sans dégradation de performance ? → A: ~30 utilisateurs simultanés (équipe LABMEDIS interne, ~200 ventes/jour).
- Q: Quel niveau de disponibilité le système doit-il garantir pendant les heures d'ouverture de LABMEDIS ? → A: Disponible en continu (24/7), avec fenêtre de maintenance planifiée en dehors des heures ouvrées.
- Q: Pendant combien de temps les données de traçabilité (mouvements de lots, factures, journal d'audit) doivent-elles rester accessibles pour répondre à un rappel produit ou un contrôle réglementaire BPD ? → A: Durée illimitée — aucune donnée de traçabilité n'est jamais purgée du système.
- Q: En cas de perte de données (panne serveur, corruption), quelle est la perte de données maximale tolérable jusqu'à laquelle le système doit pouvoir restaurer à partir d'une sauvegarde ? → A: Quasi nulle, de l'ordre de quelques minutes (réplication continue).
- Q: Si le canal temps réel est indisponible pour un utilisateur hors ligne, le système doit-il garantir qu'il prenne connaissance des alertes critiques à sa prochaine connexion ? → A: Oui — les notifications critiques sont persistées et affichées dès la reconnexion, aucune n'est perdue.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Référentiel Produits, Fournisseurs et Clients (Priority: P1)

En tant qu'Admin, Direction ou Responsable Achats, je configure et maintiens le référentiel central (produits, fournisseurs, clients) qui sert de socle à toutes les opérations d'achat, de stock et de vente, afin qu'aucune transaction ne repose sur une saisie libre ou incohérente.

**Why this priority** : Toutes les autres fonctionnalités (achats, stock, pricing, ventes) dépendent de données de référence propres et contrôlées. Une erreur ici se propage à tout le système (mauvais pricing, mauvaise TVA, mauvais stock).

**Independent Test** : Peut être testé en créant un produit, un fournisseur et un client avec les règles de validation (unicité, listes contrôlées) et en vérifiant qu'ils sont immédiatement utilisables dans les formulaires d'achat/vente, indépendamment des autres modules.

**Acceptance Scenarios**:

1. **Given** un utilisateur Responsable Achats connecté et une catégorie "Produit infantile" existante, **When** il crée un produit avec désignation, catégorie et taux de TVA, **Then** le produit est créé et visible dans le catalogue.
2. **Given** un produit actif "France Lait 1er âge 400g" existant, **When** un autre produit avec la même désignation est soumis, **Then** la création est refusée avec un message de doublon.
3. **Given** un produit désactivé, **When** un commercial recherche ce produit dans le formulaire de commande de vente, **Then** le produit n'apparaît pas dans les résultats.
4. **Given** un fournisseur ou un client avec un nom déjà utilisé par une fiche active, **When** une nouvelle fiche est créée avec le même nom, **Then** la création est refusée.
5. **Given** un fichier Excel de 138 références produits, **When** l'import de masse est lancé, **Then** chaque ligne est validée individuellement, un rapport d'erreurs par ligne est produit, et l'import de 200 produits se termine en moins de 10 secondes.
6. **Given** un client dont l'encours dépasse son plafond configuré, **When** une nouvelle commande de vente est soumise pour ce client, **Then** le système alerte ou bloque la commande selon la configuration.
7. **Given** un client marqué inactif, **When** une nouvelle commande de vente lui est associée, **Then** la création est refusée.

---

### User Story 2 - Authentification, Rôles et Permissions (Priority: P1)

En tant qu'utilisateur du système, je me connecte de façon sécurisée et je n'accède qu'aux fonctionnalités et données autorisées par mon rôle métier, afin de garantir la séparation des responsabilités (ex. seul le Responsable Qualité libère un lot en quarantaine).

**Why this priority** : Aucune opération métier (achat, stock, vente, pricing) ne peut être exécutée en toute sécurité sans authentification fiable et sans contrôle d'accès granulaire — c'est un prérequis transverse à toutes les autres user stories.

**Independent Test** : Peut être testé en se connectant avec des comptes de rôles différents (ex. Commercial vs Responsable Qualité) et en vérifiant que chacun ne voit et n'exécute que les actions permises par son rôle, indépendamment des autres modules métier.

**Acceptance Scenarios**:

1. **Given** un utilisateur avec des identifiants valides, **When** il se connecte, **Then** il reçoit un jeton d'accès de courte durée et un jeton de rafraîchissement, ainsi que la liste de ses permissions.
2. **Given** un utilisateur qui saisit un mot de passe incorrect 5 fois de suite, **When** il tente une 6e connexion, **Then** son compte est verrouillé pendant 15 minutes.
3. **Given** un utilisateur avec le rôle Commercial (sans permission qualité), **When** il tente de libérer un lot en quarantaine, **Then** l'action est refusée.
4. **Given** un utilisateur désactivé par un Admin, **When** il tente d'utiliser un jeton déjà émis, **Then** l'accès est révoqué.
5. **Given** toute tentative de connexion (réussie ou échouée), **When** elle se produit, **Then** elle est journalisée avec IP et agent utilisateur.
6. **Given** un menu applicatif, **When** un utilisateur se connecte, **Then** seuls les modules autorisés par son rôle sont affichés.

---

### User Story 3 - Achat International (Commande Fournisseur) (Priority: P1)

En tant que Responsable Achats, je crée et pilote une commande d'achat international à travers un cycle de vie complet (de l'expression de besoin jusqu'à la clôture), avec figement du taux de change et validation Direction conditionnelle, afin de sécuriser financièrement chaque approvisionnement.

**Why this priority** : L'achat est le point d'entrée de toute la chaîne de valeur (stock, pricing, vente) ; une erreur de taux de change ou de validation impacte directement la rentabilité et la conformité financière.

**Independent Test** : Peut être testé en créant une commande d'achat, en la faisant progresser dans sa machine à états (validation, envoi, expédition, transit) et en vérifiant que le taux de change reste figé et que le seuil de validation Direction est respecté, indépendamment de la réception physique.

**Acceptance Scenarios**:

1. **Given** un besoin de réapprovisionnement identifié, **When** le Responsable Achats sélectionne un fournisseur actif, des produits et une devise, **Then** une commande est créée au statut Brouillon avec le taux de change du jour figé et non recalculable.
2. **Given** une commande dont le montant total dépasse le seuil de validation configuré, **When** elle est soumise, **Then** elle passe automatiquement au statut "En Attente de Validation" et une notification est envoyée à la Direction.
3. **Given** une commande sous le seuil de validation, **When** le Responsable Achats la valide, **Then** elle passe directement au statut Validée sans intervention de la Direction.
4. **Given** une commande à n'importe quel statut actif, **When** elle est annulée, **Then** un motif textuel est obligatoire, la transition est définitive, et l'historique des statuts est conservé.
5. **Given** une commande envoyée puis expédiée, **When** la date de livraison prévue est dépassée sans réception, **Then** une notification de retard est envoyée au Responsable Achats et à la Direction.
6. **Given** une réception partielle d'une commande, **When** elle est enregistrée, **Then** le statut passe à "Partiellement Reçue" et la commande reste ouverte pour réceptions ultérieures jusqu'à clôture.

---

### User Story 4 - Réception de Lots, Stockage et Traçabilité FEFO (Priority: P1)

En tant que Magasinier, je réceptionne la marchandise physique par lot (numéro de lot, date de péremption, quantités), je la range dans l'entrepôt, et le système garantit que toute sortie de stock respecte automatiquement l'ordre FEFO (premier périmé, premier sorti), afin de tracer 100% des mouvements et d'empêcher toute rupture de la chaîne de traçabilité pharmaceutique.

**Why this priority** : La traçabilité des lots et le respect du FEFO sont l'objectif produit n°1 et l'exigence réglementaire (BPD) la plus critique de LABMEDIS ; c'est le critère de succès principal du projet (0% d'erreur de traçabilité).

**Independent Test** : Peut être testé en réceptionnant un lot avec numéro et date de péremption, en le plaçant dans un emplacement, puis en demandant une sortie de stock et en vérifiant que le lot dont la péremption est la plus proche est proposé en premier, indépendamment des modules achat/vente qui déclenchent l'opération.

**Acceptance Scenarios**:

1. **Given** l'arrivée physique d'une marchandise liée à une commande d'achat, **When** le magasinier saisit les documents (bon de livraison, packing list), **Then** un contrôle documentaire est requis avant toute mise en stock.
2. **Given** une réception, **When** un lot est enregistré, **Then** le numéro de lot, la date de péremption, et la quantité reçue (en unités et en cartons) sont obligatoires et le lot est créé au statut "En réception" ou "En quarantaine" selon le choix du magasinier.
3. **Given** un lot dont la date de péremption est à moins de 30 jours à la réception, **When** il est enregistré, **Then** le système le bloque automatiquement (seuils spécifiques : 90 jours médicament, 120 jours produit infantile, 60 jours réactif de laboratoire, 90 jours cosmétique/complément).
4. **Given** un écart entre quantité commandée et quantité reçue (manquant, excédent, non commandé, endommagé), **When** il est détecté, **Then** un constat d'écart est enregistré avant mise en stock.
5. **Given** un lot réceptionné, **When** il est enregistré, **Then** le prix de revient unitaire (PRU) du lot est calculé et figé définitivement (jamais recalculé a posteriori), et le prix moyen pondéré (PMP) du produit est recalculé après réception.
6. **Given** une commande client à préparer, **When** le système propose une allocation de stock, **Then** il sélectionne automatiquement le lot dont la date de péremption est la plus proche parmi les lots au statut "Libéré" ayant une quantité disponible suffisante.
7. **Given** une proposition FEFO, **When** un magasinier choisit manuellement un lot différent du premier proposé, **Then** un motif non vide est obligatoire, un avertissement visible est affiché, et l'action est journalisée.
8. **Given** le stock total disponible d'un produit inférieur à la quantité demandée, **When** une allocation est tentée, **Then** le système refuse l'opération et indique la quantité disponible.
9. **Given** un lot dont la date de péremption est dépassée, **When** le job quotidien de vérification s'exécute, **Then** le statut du lot passe automatiquement à "Périmé" et toute vente de ce lot est bloquée.
10. **Given** un lot stocké, **When** il est déplacé, transféré ou ajusté, **Then** un mouvement de stock est enregistré avec type, quantité, emplacement source/destination et utilisateur responsable.

---

### User Story 5 - Contrôle Qualité et Quarantaine des Lots (Priority: P1)

En tant que Responsable Qualité, je suis seul habilité à libérer un lot en quarantaine ou à le déclarer non conforme, afin de garantir qu'aucun produit non contrôlé n'atteigne un client.

**Why this priority** : Objectif produit explicite — "empêcher toute vente à perte ou avec un lot non conforme" — et exigence réglementaire BPD/UEMOA non négociable ; un manquement ici constitue un risque sanitaire et légal direct.

**Independent Test** : Peut être testé en plaçant un lot en quarantaine avec un motif, en tentant de le vendre (doit être bloqué), puis en le libérant avec le rôle Responsable Qualité uniquement, indépendamment du flux de réception qui l'a créé.

**Acceptance Scenarios**:

1. **Given** un lot dont le contrôle qualité échoue, **When** il est mis en quarantaine, **Then** un motif et un emplacement de quarantaine dédié sont obligatoires.
2. **Given** un lot au statut différent de "Libéré" (quarantaine, non conforme, en attente de libération, périmé, détruit, suspecté falsifié), **When** une tentative de vente est effectuée, **Then** elle est refusée.
3. **Given** un lot en quarantaine, **When** un utilisateur sans le rôle Responsable Qualité tente de le libérer, **Then** l'action est refusée.
4. **Given** un lot libéré par le Responsable Qualité, **When** la libération est effectuée, **Then** l'action est journalisée avec l'identité de l'utilisateur et l'horodatage.
5. **Given** un lot suspecté falsifié, **When** ce statut est déclenché (depuis n'importe quel statut actif), **Then** une alerte de notification de l'autorité compétente est requise.
6. **Given** un lot devant être détruit, **When** la destruction est enregistrée, **Then** un document de destruction est obligatoire et le lot sort définitivement du stock physique disponible.

---

### User Story 6 - Tarification et Moteur de Calcul du Prix de Revient (Priority: P1)

En tant qu'Admin ou Direction, je configure des profils de coefficients de pricing (par mode de transport et catégorie) et je simule/applique un prix de vente calculé automatiquement en cascade depuis le prix d'achat, afin de garantir une marge maîtrisée et traçable sur chaque produit.

**Why this priority** : Objectif produit explicite — "calculer le prix de revient de manière automatique" — c'est le cœur financier du système ; toute erreur fausse directement la marge et la rentabilité de l'entreprise.

**Independent Test** : Peut être testé en simulant un calcul de prix à partir d'un prix d'achat en devise étrangère, d'un taux de change et d'un profil de coefficients, et en vérifiant que le résultat (prix de revient, prix de vente calculé, écart, TVA) est correct sans dépendre d'une commande réelle.

**Acceptance Scenarios**:

1. **Given** un prix d'achat en devise étrangère, un taux de change figé et un profil de coefficients (commission, fret, transit, frais de transfert, marge), **When** une simulation de prix est demandée, **Then** le système calcule successivement le prix d'achat en CFA, le prix de revient en CFA, et le prix de vente HT calculé, chaque étape intermédiaire conservant une précision complète et l'arrondi CFA (zéro décimale) n'étant appliqué qu'au résultat final.
2. **Given** un ensemble de coefficients de pricing, **When** ils sont consultés ou modifiés, **Then** ils proviennent exclusivement d'une configuration en base de données, jamais d'une valeur figée dans le code.
3. **Given** un prix de vente HT calculé, **When** la Direction ou l'Admin ajuste manuellement le prix de vente appliqué, **Then** l'écart entre le prix calculé et le prix appliqué est conservé et n'est jamais écrasé ni remis à zéro automatiquement.
4. **Given** toute modification du prix de vente d'un produit, **When** elle est enregistrée, **Then** une nouvelle ligne d'historique de prix est créée (jamais de modification de l'ancienne ligne).
5. **Given** un prix de vente HT appliqué et un taux de TVA configuré par produit, **When** le prix TTC est calculé, **Then** il est égal au prix HT multiplié par (1 + taux de TVA).
6. **Given** aucun profil de coefficients actif pour la combinaison catégorie/mode de transport demandée, **When** une simulation est lancée, **Then** le système renvoie une erreur explicite et propose le profil global si disponible.
7. **Given** un utilisateur sans permission de modification du pricing, **When** il tente de modifier un profil de coefficients ou d'appliquer un nouveau prix, **Then** l'action est refusée (réservée à Admin et Direction).

---

### User Story 7 - Ventes et Facturation avec Traçabilité des Lots (Priority: P1)

En tant que Commercial, je crée une commande de vente pour un client, le système propose automatiquement l'allocation FEFO des lots disponibles, réserve le stock à la confirmation, puis génère un bon de livraison et une facture référençant précisément chaque lot vendu, afin de garantir la traçabilité complète de chaque vente jusqu'au lot d'origine.

**Why this priority** : C'est l'aboutissement de la chaîne de valeur commerciale et une exigence réglementaire directe (BPD) — chaque facture doit permettre de retrouver le lot vendu à tout moment (rappel produit).

**Independent Test** : Peut être testé en créant une commande de vente, en la confirmant (ce qui réserve le stock), en la livrant et en la facturant, puis en vérifiant que le numéro de lot apparaît sur la facture PDF générée — indépendamment du contrôle qualité ou de l'achat qui a produit le lot.

**Acceptance Scenarios**:

1. **Given** une commande de vente en cours de saisie, **When** des lignes produit/quantité sont ajoutées, **Then** le système vérifie la disponibilité de stock en temps réel et propose automatiquement l'allocation FEFO.
2. **Given** une commande de vente confirmée, **When** la confirmation est enregistrée, **Then** une réservation de stock est créée immédiatement pour chaque lot alloué (réduisant le stock disponible sans réduire le stock physique).
3. **Given** une commande de vente annulée après confirmation, **When** l'annulation est enregistrée, **Then** toute réservation de stock associée est libérée immédiatement.
4. **Given** une commande de vente livrée, **When** le bon de livraison est généré, **Then** il est un document distinct de la facture et le stock physique est décrémenté à ce moment.
5. **Given** une facture générée, **When** elle est consultée ou exportée en PDF, **Then** chaque ligne de facture référence explicitement le lot vendu (numéro de lot visible sur le document).
6. **Given** une commande de vente, **When** elle progresse dans son cycle de vie, **Then** elle suit les statuts Brouillon → Confirmée → Livrée → Facturée, ou peut être Annulée à un stade antérieur à la livraison.
7. **Given** une facture émise, **When** la devise du client est XOF ou EUR, **Then** le document reflète la devise choisie avec le formatage approprié.
8. **Given** deux commandes de vente confirmées simultanément sur les mêmes dernières unités disponibles d'un lot, **When** les deux confirmations sont traitées en parallèle, **Then** celle qui ne trouve plus de stock suffisant au moment de la validation finale est rejetée avec l'erreur "stock insuffisant" et invitée à relancer avec la disponibilité à jour, sans qu'aucune survente ne se produise.

---

### User Story 8 - Retours Clients et Avoirs (Priority: P2)

En tant que Commercial ou Responsable Qualité, je traite un retour de marchandise d'un client, je décide de sa disposition (remise en stock, quarantaine ou destruction), et je génère un avoir correspondant, afin de refléter fidèlement l'impact financier et physique du retour.

**Why this priority** : Fonctionnalité de support nécessaire à la continuité commerciale, mais dépendante de l'existence préalable de ventes et de lots — moins critique que le flux principal achat-stock-vente.

**Independent Test** : Peut être testé en initiant un retour sur une commande de vente déjà livrée, en vérifiant la validité du lot et le délai de retour, en choisissant une disposition, et en confirmant qu'un avoir est généré — indépendamment des autres user stories une fois qu'une vente existe.

**Acceptance Scenarios**:

1. **Given** une demande de retour client, **When** elle est initiée, **Then** le lot d'origine et le délai de retour sont vérifiés avant toute décision.
2. **Given** un retour validé, **When** la disposition "Remise en stock" est choisie, **Then** la quantité retourne au stock disponible.
3. **Given** un retour validé, **When** la disposition "Quarantaine" est choisie, **Then** la marchandise est placée en zone de quarantaine dédiée avec motif obligatoire.
4. **Given** un retour validé, **When** la disposition "Destruction" est choisie, **Then** la marchandise sort définitivement du stock.
5. **Given** un retour traité (quelle que soit la disposition), **When** le traitement est finalisé, **Then** un avoir est généré et lié au retour.

---

### User Story 9 - Inventaire Physique et Ajustements de Stock (Priority: P2)

En tant que Magasinier ou Responsable Logistique, je réalise une session d'inventaire physique sur un périmètre défini, je compare le comptage physique au stock système, et je valide les ajustements nécessaires avec un motif obligatoire, afin de maintenir l'exactitude du stock dans la durée.

**Why this priority** : Fonctionnalité de contrôle périodique qui consolide la fiabilité du stock déjà géré au quotidien par les flux d'achat/vente — importante mais non bloquante pour les opérations courantes.

**Independent Test** : Peut être testé en créant une session d'inventaire sur un périmètre, en gelant les mouvements de cette zone, en saisissant un comptage, en calculant les écarts et en validant les ajustements — indépendamment des flux d'achat/vente en cours ailleurs dans l'entrepôt.

**Acceptance Scenarios**:

1. **Given** une session d'inventaire créée sur un périmètre (zone, emplacement), **When** elle démarre, **Then** les mouvements de stock sur ce périmètre sont gelés pour la durée du comptage.
2. **Given** un comptage physique saisi, **When** il est comparé au stock système, **Then** les écarts sont calculés automatiquement par lot/emplacement.
3. **Given** des écarts d'inventaire détectés, **When** le responsable valide la session, **Then** des ajustements de stock sont créés automatiquement avec un motif obligatoire pour chaque ajustement.
4. **Given** une session non validée par le responsable, **When** un écart est jugé anormal, **Then** un re-comptage peut être demandé avant clôture.
5. **Given** une session clôturée, **When** elle est terminée, **Then** son historique complet (comptages, écarts, ajustements) est conservé de façon permanente.

---

### User Story 10 - Prévision des Besoins (MRP) et Réapprovisionnement (Priority: P2)

En tant que Responsable Achats, je reçois chaque jour des suggestions automatiques de réapprovisionnement basées sur la consommation historique et les délais fournisseurs, afin d'anticiper les ruptures de stock avant qu'elles ne surviennent.

**Why this priority** : Objectif produit explicite — "notifier la direction pour les commandes de réapprovisionnement" — améliore la performance opérationnelle mais n'est pas bloquant : les commandes d'achat manuelles restent possibles sans le MRP.

**Independent Test** : Peut être testé en déclenchant le calcul de prévision pour un produit avec un historique de consommation connu, et en vérifiant qu'une suggestion de commande est créée avec la bonne date limite et la bonne quantité — indépendamment de la création effective d'une commande d'achat.

**Acceptance Scenarios**:

1. **Given** l'historique de consommation d'un produit sur les 90 derniers jours, **When** le calcul quotidien automatique s'exécute, **Then** le point de commande est calculé comme (consommation moyenne journalière × délai total) + stock de sécurité.
2. **Given** un stock disponible et en transit inférieur au point de commande calculé, **When** le calcul détecte cette situation, **Then** une suggestion de réapprovisionnement est créée avec une date limite de commande et une notification est envoyée en temps réel.
3. **Given** une suggestion de réapprovisionnement en attente, **When** le Responsable Achats la traite, **Then** il peut soit la convertir directement en commande d'achat, soit la rejeter.
4. **Given** un produit nouvellement créé sans historique de consommation, **When** une prévision est nécessaire, **Then** une consommation estimée peut être saisie manuellement.
5. **Given** l'état d'un produit vis-à-vis de son point de commande, **When** il est consulté, **Then** il affiche un statut parmi OK, Surveiller, Urgent, Critique.

---

### User Story 11 - Reporting et Tableaux de Bord (Priority: P2)

En tant que Direction, Responsable Achats, Responsable Stock, Commercial ou Responsable Qualité, je consulte des tableaux de bord et rapports adaptés à mon rôle (chiffre d'affaires, marge, valeur de stock, ruptures, péremptions, rotation, qualité), afin de piloter l'activité avec des données fiables et à jour.

**Why this priority** : Valeur de pilotage stratégique importante mais consommant des données déjà produites par les autres user stories — n'est utile qu'une fois les flux transactionnels alimentés.

**Independent Test** : Peut être testé en interrogeant chaque rapport (direction, stock, ventes, péremptions, pricing, qualité) avec des données existantes et en vérifiant l'exactitude des agrégations et la disponibilité d'un export — indépendamment des interactions transactionnelles en cours.

**Acceptance Scenarios**:

1. **Given** des données transactionnelles existantes, **When** la Direction consulte son tableau de bord, **Then** elle voit le chiffre d'affaires, la marge, la valeur de stock et les ruptures.
2. **Given** des lots proches de la péremption, **When** un rapport de stock est demandé avec un paramètre de nombre de jours, **Then** la liste des lots concernés est retournée.
3. **Given** des ventes historiques, **When** un rapport de ventes est demandé, **Then** le chiffre d'affaires par client et par produit ainsi que les taux de retour/service sont calculés.
4. **Given** un produit avec un prix de vente calculé et un prix de vente appliqué différents, **When** le rapport de pricing est consulté, **Then** la marge théorique, la marge réelle et l'écart sont affichés.
5. **Given** un rapport quelconque du système, **When** un export est demandé, **Then** il est disponible aux formats PDF et Excel.
6. **Given** un tableau de bord affiché, **When** un événement métier survient (rupture, péremption, retard), **Then** le tableau de bord se met à jour sans rechargement manuel de la page.

---

### User Story 12 - Notifications Temps Réel (Priority: P2)

En tant qu'utilisateur concerné (Responsable Achats, Direction, Responsable Qualité, Magasinier), je reçois des alertes en temps réel sur les événements critiques (stock faible, péremption proche, retard de livraison, suggestion MRP), sans avoir à rafraîchir ou interroger le système manuellement.

**Why this priority** : Améliore la réactivité opérationnelle sur des événements déjà détectés par les autres modules ; dépend de l'existence de ces modules pour avoir une source d'événements à notifier.

**Independent Test** : Peut être testé en déclenchant un événement (ex. franchissement d'un seuil de péremption) et en vérifiant qu'une notification apparaît en temps réel pour le ou les rôles concernés, avec un état lu/non lu propre à chaque utilisateur — indépendamment du canal d'origine de l'événement.

**Acceptance Scenarios**:

1. **Given** un événement métier déclencheur (stock faible, rupture, péremption proche à J-30/60/90/120, retard de livraison, réception en attente, quarantaine prolongée, suggestion MRP, expiration de licence DPML), **When** il survient, **Then** une notification est émise en temps réel sans mécanisme d'interrogation périodique (pas de polling).
2. **Given** une notification émise, **When** elle est destinée à un rôle spécifique, **Then** seuls les utilisateurs ayant ce rôle ou cette permission la reçoivent.
3. **Given** une notification reçue par un utilisateur, **When** il la consulte, **Then** son état passe de non lu à lu, indépendamment de l'état de cette même notification pour un autre utilisateur.
4. **Given** un événement critique configuré pour l'envoi d'email ou de SMS en plus du canal temps réel, **When** il survient, **Then** le message est également transmis par le canal secondaire configuré.
5. **Given** un utilisateur déconnecté du canal temps réel au moment où une notification est émise, **When** il se reconnecte, **Then** il retrouve intégralement cette notification (aucune alerte critique n'est perdue faute de connexion au moment de l'émission).

---

### User Story 13 - Gestion Documentaire et Conformité Réglementaire (Priority: P3)

En tant que Responsable Qualité ou Admin, je rattache les pièces justificatives réglementaires à chaque lot et expédition (factures, documents douaniers, certificats, autorisation DPML), et je peux retrouver instantanément tous les clients ayant reçu un lot donné en cas de rappel produit, afin de répondre aux exigences BPD UEMOA/CEDEAO.

**Why this priority** : Complète la conformité réglementaire déjà largement portée par les user stories de traçabilité (US4, US5, US7) ; ajoute la dimension documentaire et le scénario de rappel, moins fréquent mais critique quand il survient.

**Independent Test** : Peut être testé en attachant un document à un lot ou une expédition, puis en simulant un rappel de lot et en vérifiant que la liste de tous les clients ayant reçu ce lot (via les factures) est produite correctement — indépendamment des autres user stories une fois des ventes tracées existantes.

**Acceptance Scenarios**:

1. **Given** un lot ou une expédition, **When** une pièce jointe réglementaire (facture, document douanier, certificat) est ajoutée, **Then** elle est rattachée et consultable ultérieurement.
2. **Given** un lot déclaré "Suspecté falsifié" ou nécessitant un rappel, **When** l'investigation est lancée, **Then** le système permet de remonter la chaîne complète vente → lot → expédition → achat → fournisseur.
3. **Given** un lot à rappeler, **When** la recherche est effectuée, **Then** tous les clients ayant reçu ce lot sont identifiés à partir des lignes de facture.
4. **Given** une expédition de médicaments, **When** elle est enregistrée, **Then** une référence d'autorisation d'importation DPML est requise (optionnelle pour les autres catégories de produits).

---

### Edge Cases

- Que se passe-t-il quand le stock total disponible (tous lots "Libéré" confondus) est inférieur à la quantité demandée sur une commande de vente ? → Le système DOIT refuser l'allocation avec le détail de la quantité disponible, sans jamais allouer partiellement sans confirmation explicite.
- Que se passe-t-il quand tous les lots d'un produit sont périmés ou bloqués (aucun lot "Libéré" disponible) ? → Le système DOIT refuser l'allocation avec un message dédié.
- Que se passe-t-il quand le premier lot FEFO périme dans moins de 30 jours ? → L'allocation est autorisée mais un avertissement explicite est renvoyé.
- Que se passe-t-il quand un magasinier choisit un lot non-FEFO sans motif ? → L'action DOIT être bloquée.
- Que se passe-t-il quand aucun profil de pricing actif n'existe pour la combinaison catégorie/mode de transport demandée ? → Le système propose le profil global (catégorie non spécifiée) s'il existe, sinon renvoie une erreur explicite.
- Que se passe-t-il quand aucun taux de change actif n'existe pour la devise et la date de la commande ? → La création de la commande DOIT être bloquée avec une erreur explicite.
- Que se passe-t-il quand un produit inactif ou un fournisseur/client inactif est référencé dans une nouvelle transaction ? → La sélection DOIT être empêchée dans les formulaires concernés.
- Que se passe-t-il quand une désignation de produit soft-supprimée est réutilisée pour un nouveau produit ? → La création DOIT être autorisée (le produit supprimé ne bloque pas la réutilisation du nom).
- Que se passe-t-il quand l'encours d'un client dépasse son plafond configuré ? → Le système DOIT alerter et/ou bloquer selon la configuration, avant confirmation de toute nouvelle commande.
- Que se passe-t-il quand un utilisateur atteint 5 échecs de connexion consécutifs ? → Le compte DOIT être verrouillé 15 minutes.
- Que se passe-t-il quand le taux de change EUR/XOF est modifié ? → Seul l'Admin peut le faire, via une action explicite et journalisée ; les commandes déjà figées ne sont jamais recalculées.
- Que se passe-t-il quand un lot est suspecté falsifié ? → Le statut DOIT pouvoir être déclenché depuis n'importe quel statut actif, et l'autorité compétente DOIT être notifiée.
- Que se passe-t-il quand une commande d'achat est annulée sans motif ? → L'annulation DOIT être refusée tant qu'aucun motif n'est saisi.
- Que se passe-t-il quand un écart entre prix de vente calculé et prix de vente appliqué existe et qu'une nouvelle simulation est lancée ? → L'écart précédemment enregistré ne DOIT jamais être écrasé automatiquement.
- Que se passe-t-il quand deux commandes de vente sont confirmées au même instant sur les mêmes dernières unités disponibles d'un lot ? → Les deux confirmations sont traitées en parallèle ; celle qui ne trouve plus de stock suffisant au moment de la validation finale est rejetée avec l'erreur "stock insuffisant", sans qu'aucune survente ne se produise (voir FR-091).

## Requirements *(mandatory)*

### Functional Requirements

#### Référentiel & Configuration (US1)

- **FR-001**: Le système DOIT permettre la création, modification, consultation et désactivation de fiches produit avec désignation, catégorie, forme pharmaceutique, dosage, code CIP, mode de transport par défaut, délais de fabrication/livraison, stock de sécurité, taux de TVA et indicateur d'assujettissement à la TVA.
- **FR-002**: Le système DOIT garantir l'unicité de la désignation produit et du code CIP parmi les seuls produits actifs (deux produits inactifs/supprimés peuvent partager la même désignation).
- **FR-003**: Le système DOIT restreindre la catégorie, la forme pharmaceutique et la classe thérapeutique à des listes contrôlées (référentiels), sans saisie libre.
- **FR-004**: Le système DOIT permettre à un produit d'avoir plusieurs conditionnements (unité, carton, palette, colis express) et plusieurs fournisseurs habituels ordonnés par priorité.
- **FR-005**: Le système DOIT masquer tout produit désactivé des listes de sélection des formulaires d'achat, de vente et de réception.
- **FR-006**: Le système DOIT permettre l'import en masse d'un catalogue produits depuis un fichier Excel, avec validation ligne par ligne et rapport d'erreurs, traitant au moins 200 lignes en moins de 10 secondes.
- **FR-007**: Le système DOIT permettre la gestion des fiches fournisseur (nom, adresse, pays, devise par défaut, délais moyens de fabrication/livraison) avec unicité du nom parmi les fournisseurs actifs.
- **FR-008**: Le système DOIT permettre la gestion des fiches client (nom, type, adresse, délai de paiement, plafond d'encours) avec unicité du nom parmi les clients actifs.
- **FR-009**: Le système DOIT calculer l'encours d'un client comme la somme de ses factures non soldées, et alerter ou bloquer toute nouvelle commande dépassant le plafond configuré.
- **FR-010**: Le système NE DOIT PAS permettre l'enregistrement d'une nouvelle commande de vente pour un client marqué inactif.
- **FR-011**: Le système DOIT permettre la définition de tarifs négociés par client et par produit, sur des périodes de validité qui ne peuvent pas se chevaucher pour un même couple client/produit.

#### Authentification, Rôles et Permissions (US2)

- **FR-012**: Le système DOIT authentifier les utilisateurs par identifiant/mot de passe et délivrer un jeton d'accès de courte durée (15-30 minutes) accompagné d'un jeton de rafraîchissement (7-30 jours).
- **FR-013**: Le système DOIT exiger des mots de passe d'au moins 8 caractères combinant majuscules, minuscules, chiffres et caractères spéciaux.
- **FR-014**: Le système DOIT verrouiller un compte pendant 15 minutes après 5 tentatives de connexion échouées consécutives.
- **FR-015**: Le système DOIT gérer un modèle de rôles et permissions granulaires (format Module.Action) couvrant au minimum les profils Admin, Direction, Responsable Achats, Logistique, Magasinier, Responsable Qualité, Commercial, Comptable, Préparateur et Lecture Seule.
- **FR-016**: Le système DOIT restreindre chaque action métier sensible (ex. libération de lot en quarantaine, modification des coefficients de pricing) au(x) rôle(s) explicitement autorisé(s).
- **FR-017**: Le système DOIT journaliser toute tentative de connexion (réussie ou échouée) avec horodatage, adresse IP et agent utilisateur.
- **FR-018**: Le système DOIT révoquer l'accès (jetons) d'un utilisateur désactivé.
- **FR-019**: Le système DOIT n'afficher, dans les menus applicatifs, que les modules et actions autorisés par les permissions de l'utilisateur connecté.

#### Achats & Logistique (US3)

- **FR-020**: Le système DOIT permettre la création d'une commande d'achat associée à un fournisseur actif, une devise, un taux de change et des lignes produit/quantité/prix.
- **FR-021**: Le système DOIT figer le taux de change au moment de la création de la commande d'achat et ne jamais le recalculer, y compris lors des réceptions ultérieures.
- **FR-022**: Le système DOIT faire progresser chaque commande d'achat à travers une machine à états complète (Brouillon, En Attente de Validation, Validée, Envoyée, En Fabrication, Prête à Expédier, Expédiée, En Transit, Partiellement Reçue, Reçue, Close, Annulée) avec conservation de l'historique complet des transitions.
- **FR-023**: Le système DOIT router automatiquement toute commande dont le montant dépasse un seuil configurable vers une validation obligatoire par la Direction.
- **FR-024**: Le système DOIT exiger un motif textuel non vide pour toute annulation de commande d'achat, et rendre cette transition définitive (irréversible).
- **FR-025**: Le système DOIT permettre le suivi logistique d'une expédition (mode de transport, transporteur, références de suivi, régime douanier, dates estimées/réelles) pouvant regrouper une ou plusieurs commandes d'achat.
- **FR-026**: Le système DOIT permettre la répartition des frais logistiques (fret, transit, douane, commission, transfert, assurance, manutention) sur les lignes d'expédition selon une clé configurable (valeur, quantité ou volume).
- **FR-027**: Le système DOIT notifier en temps réel le Responsable Achats et la Direction lorsqu'une commande d'achat dépasse sa date de livraison prévue sans réception complète.
- **FR-028**: Le système DOIT permettre l'enregistrement d'une référence d'autorisation d'importation DPML sur une expédition, obligatoire pour les expéditions de médicaments.

#### Réception, Stock et Traçabilité (US4, US5)

- **FR-029**: Le système DOIT exiger, pour chaque lot réceptionné, un numéro de lot, une date de péremption et une quantité reçue (en unités et en cartons).
- **FR-030**: Le système DOIT garantir l'unicité du numéro de lot fournisseur par couple fournisseur/produit, et l'unicité globale du numéro de lot interne.
- **FR-031**: Le système DOIT bloquer automatiquement tout lot dont la date de péremption à réception est inférieure au seuil applicable à sa catégorie (30 jours seuil générique de blocage strict ; seuils d'alerte différenciés : 60 jours réactifs de laboratoire, 90 jours médicaments/cosmétiques/compléments, 120 jours produits infantiles).
- **FR-032**: Le système DOIT calculer le prix de revient unitaire (PRU) de chaque lot à la réception et le figer définitivement, sans jamais le recalculer a posteriori.
- **FR-033**: Le système DOIT recalculer le prix moyen pondéré (PMP/CUMP) d'un produit après chaque réception de lot, en pondérant tous les lots au statut Libéré disponibles.
- **FR-034**: Le système DOIT organiser l'entreposage en zones/allées/racks/niveaux/positions, avec des types d'emplacement dédiés (réception, quarantaine, stockage, picking, réserve, chaîne du froid, produits périmés, produits détruits, transit).
- **FR-035**: Le système DOIT calculer le stock disponible d'un produit comme : stock physique − stock réservé − stock en quarantaine − stock périmé.
- **FR-036**: Le système DOIT, pour toute sortie de stock (vente, transfert, picking), sélectionner automatiquement en priorité le lot au statut Libéré dont la date de péremption est la plus proche (règle FEFO), en excluant tout lot périmé ou non libéré.
- **FR-037**: Le système DOIT autoriser une dérogation manuelle à l'ordre FEFO uniquement si le lot choisi n'est pas périmé, est au statut Libéré, dispose d'une quantité suffisante, et si un motif non vide est saisi ; cette dérogation DOIT être journalisée.
- **FR-038**: Le système DOIT créer un mouvement de stock traçable (type, quantité, date, utilisateur, emplacement source/destination, document d'origine) pour toute entrée, sortie, transfert ou ajustement de stock.
- **FR-039**: Le système DOIT permettre à un même lot d'être réparti sur plusieurs emplacements simultanément, et à un même emplacement de contenir plusieurs lots différents.
- **FR-040**: Le système DOIT faire progresser le statut qualité de chaque lot selon une machine à états contrôlée (En réception, En quarantaine, Libéré, Non conforme, Périmé, Détruit, En attente de libération, Suspecté falsifié).
- **FR-041**: Le système DOIT interdire la vente de tout lot dont le statut qualité n'est pas Libéré.
- **FR-042**: Le système DOIT restreindre la libération d'un lot en quarantaine au seul rôle Responsable Qualité, et exiger un motif pour toute mise en quarantaine ou déclaration de non-conformité.
- **FR-043**: Le système DOIT faire passer automatiquement un lot au statut Périmé dès que sa date de péremption est dépassée, et bloquer toute vente de ce lot.
- **FR-044**: Le système DOIT permettre la réalisation de sessions d'inventaire physique par périmètre, avec gel des mouvements pendant le comptage, calcul automatique des écarts, et création d'ajustements de stock motivés après validation.

#### Tarification (US6)

- **FR-045**: Le système DOIT calculer le prix de revient en appliquant successivement au prix d'achat converti en CFA les coefficients de commission, de fret, de transit et de frais de transfert issus d'un profil de coefficients configuré (jamais codés en dur).
- **FR-046**: Le système DOIT calculer le prix de vente HT proposé en appliquant un coefficient de marge cible au prix de revient.
- **FR-047**: Le système DOIT permettre à un profil de coefficients de varier selon le mode de transport et, optionnellement, selon la catégorie de produit (une valeur non spécifiée s'appliquant globalement).
- **FR-048**: Le système DOIT conserver la précision complète des calculs intermédiaires de pricing et n'appliquer l'arrondi de la devise CFA (zéro décimale) qu'au résultat final affiché ou stocké.
- **FR-049**: Le système DOIT permettre à la Direction ou à l'Admin d'ajuster manuellement le prix de vente appliqué, et calculer/conserver l'écart avec le prix de vente calculé sans jamais l'écraser automatiquement.
- **FR-050**: Le système DOIT créer une nouvelle entrée d'historique à chaque modification de prix de vente, sans jamais modifier une entrée existante.
- **FR-051**: Le système DOIT calculer le prix TTC comme le prix HT appliqué multiplié par (1 + taux de TVA du produit).
- **FR-052**: Le système DOIT restreindre la modification des profils de coefficients et l'application de nouveaux prix aux seuls rôles Admin et Direction.
- **FR-053**: Le système DOIT permettre de simuler un calcul de prix (prix d'achat, taux de change, profil) sans l'appliquer, et retourner une erreur explicite si aucun profil de coefficients ou aucun taux de change applicable n'est disponible.

#### Ventes, Facturation, Retours (US7, US8)

- **FR-054**: Le système DOIT vérifier la disponibilité du stock en temps réel lors de la saisie d'une commande de vente et proposer automatiquement l'allocation FEFO correspondante.
- **FR-055**: Le système DOIT créer une réservation de stock pour chaque lot alloué dès la confirmation d'une commande de vente, et la libérer immédiatement en cas d'annulation. En cas de confirmations concurrentes sur la même quantité disponible d'un lot, voir FR-091 pour la règle d'arbitrage.
- **FR-056**: Le système DOIT faire progresser une commande de vente selon les statuts Brouillon → Confirmée → Livrée → Facturée, avec possibilité d'Annulation avant livraison.
- **FR-057**: Le système DOIT produire un bon de livraison distinct de la facture, et décrémenter le stock physique au moment de la livraison (et non à la confirmation).
- **FR-058**: Le système DOIT faire apparaître le numéro de lot de chaque ligne vendue sur la facture, y compris dans son export PDF.
- **FR-059**: Le système DOIT permettre l'émission de factures en devise XOF ou EUR selon la préférence du client.
- **FR-060**: Le système DOIT permettre l'initiation d'un retour client rattaché à une commande de vente livrée, avec vérification du lot d'origine et du délai de retour avant décision.
- **FR-061**: Le système DOIT permettre de disposer un retour selon trois issues : remise en stock disponible, mise en quarantaine (motif obligatoire), ou destruction définitive.
- **FR-062**: Le système DOIT générer un avoir pour tout retour client traité, quelle que soit sa disposition.

#### Prévision (MRP) & Réapprovisionnement (US10)

- **FR-063**: Le système DOIT calculer quotidiennement, pour chaque produit actif, un point de commande égal à (consommation moyenne journalière sur une fenêtre glissante de 90 jours × délai total de fabrication et livraison) + stock de sécurité.
- **FR-064**: Le système DOIT créer automatiquement une suggestion de réapprovisionnement, avec une date limite de commande, lorsque le stock disponible et en transit d'un produit passe sous son point de commande.
- **FR-065**: Le système DOIT permettre au Responsable Achats de convertir une suggestion en commande d'achat ou de la rejeter.
- **FR-066**: Le système DOIT permettre la saisie manuelle d'une consommation estimée pour tout produit sans historique de consommation suffisant.
- **FR-067**: Le système DOIT afficher un statut de criticité (OK, Surveiller, Urgent, Critique) pour chaque produit en fonction de son nombre de jours de couverture de stock restant.

#### Reporting & Tableaux de Bord (US11)

- **FR-068**: Le système DOIT fournir un tableau de bord Direction présentant le chiffre d'affaires, la marge, la valeur de stock et les ruptures.
- **FR-069**: Le système DOIT fournir un rapport listant les lots proches de la péremption sur une fenêtre de jours paramétrable.
- **FR-070**: Le système DOIT fournir un rapport de rotation de stock permettant d'identifier les produits à rotation lente.
- **FR-071**: Le système DOIT fournir un rapport de ventes par client et par produit, incluant les taux de retour et de service.
- **FR-072**: Le système DOIT fournir un rapport de pricing comparant marge théorique et marge réelle, avec l'écart de prix de vente.
- **FR-073**: Le système DOIT fournir un rapport qualité listant les lots en quarantaine ou non conformes.
- **FR-074**: Le système DOIT permettre l'export de tout rapport aux formats PDF et Excel.
- **FR-075**: Le système DOIT mettre à jour les tableaux de bord affichés en temps réel lors de la survenue d'événements métier pertinents, sans action manuelle de rafraîchissement.

#### Notifications (US12)

- **FR-076**: Le système DOIT émettre des notifications en temps réel (sans mécanisme d'interrogation périodique) pour les événements : stock faible, rupture de stock, péremption proche (J-30/60/90/120 selon catégorie), retard de livraison, réception en attente, quarantaine prolongée, suggestion MRP créée, expiration de licence DPML.
- **FR-077**: Le système DOIT cibler chaque notification aux seuls utilisateurs disposant du rôle ou de la permission concernée par l'événement.
- **FR-078**: Le système DOIT maintenir un état lu/non lu de chaque notification, propre à chaque utilisateur destinataire.
- **FR-094**: Le système DOIT persister toute notification émise indépendamment de la connexion du destinataire au canal temps réel, et la lui présenter intégralement (aucune perte) dès sa prochaine connexion.
- **FR-079**: Le système DOIT pouvoir relayer une notification critique par email et/ou SMS en complément du canal temps réel, selon la configuration de l'événement.

#### Conformité & Documentation Réglementaire (US13)

- **FR-080**: Le système DOIT permettre de rattacher des pièces justificatives (facture, documents douaniers, certificats) à un lot ou à une expédition.
- **FR-081**: Le système DOIT permettre de retracer la chaîne complète vente → lot → expédition → achat → fournisseur pour tout lot donné.
- **FR-082**: Le système DOIT permettre d'identifier, à partir d'un numéro de lot, l'ensemble des clients l'ayant reçu, en cas de rappel produit.
- **FR-083**: Le système DOIT déclencher une alerte dédiée et permettre la notification d'une autorité compétente lorsqu'un lot est déclaré "Suspecté falsifié".

#### Exigences Transverses (Toutes User Stories)

- **FR-084**: Le système NE DOIT JAMAIS supprimer physiquement une donnée métier ; toute suppression DOIT être une suppression logique préservant l'historique.
- **FR-085**: Le système DOIT gérer les devises EUR, USD et XOF, avec un taux EUR/XOF fixe (655,957) modifiable uniquement par un Admin via une action explicite et journalisée, et un taux USD/XOF variable, saisi manuellement et historisé.
- **FR-086**: Le système DOIT figer le taux de change applicable au moment de la création de chaque transaction concernée (commande d'achat) et ne jamais le recalculer rétroactivement.
- **FR-087**: Le système DOIT appliquer un taux de TVA configurable individuellement par produit (jamais déduit automatiquement de la seule catégorie), avec 0% pour les médicaments (directive UEMOA) et 18% par défaut pour les autres catégories sauf configuration contraire.
- **FR-088**: Le système DOIT afficher tout montant financier dans sa devise d'origine ainsi qu'en équivalent XOF.
- **FR-089**: Le système DOIT journaliser de façon exhaustive toute action sensible (création, modification, changement de statut, suppression logique) avec l'identité de l'utilisateur, l'horodatage, et le contexte de la requête.
- **FR-090**: Le système DOIT présenter son interface intégralement en français, avec les dates au format JJ/MM/AAAA.
- **FR-091**: Lorsque plusieurs confirmations de commande de vente entrent en concurrence sur la même quantité disponible d'un lot, le système DOIT accepter les tentatives en parallèle (vérification optimiste) et rejeter, avec l'erreur "stock insuffisant", uniquement celle(s) qui ne trouve(nt) plus de stock suffisant au moment de la validation finale ; aucune survente n'est autorisée.
- **FR-092**: Le système DOIT conserver indéfiniment (sans purge ni archivage hors ligne qui les rendrait inaccessibles) toutes les données de traçabilité — mouvements de stock, lots, factures, retours et journal d'audit — afin de pouvoir répondre à tout moment à un rappel produit ou un contrôle réglementaire BPD.
- **FR-093**: Le système DOIT être sauvegardé (ou répliqué) à une fréquence garantissant qu'en cas d'incident, la perte de données transactionnelles et de traçabilité soit limitée à quelques minutes au maximum.

### Key Entities *(include if feature involves data)*

- **Produit** : article du catalogue LABMEDIS (désignation, catégorie, forme pharmaceutique, dosage, code CIP, TVA, assujettissement, délais et stock de sécurité pour la prévision) ; relié à des conditionnements, des fournisseurs habituels, des lots de stock et un historique de prix.
- **Catégorie / Classe Thérapeutique / Forme Pharmaceutique** : listes de référence contrôlées qualifiant un produit.
- **Conditionnement Produit** : unité de vente/achat d'un produit (unité, carton, palette, colis express) avec quantité par conditionnement.
- **Fournisseur** : partenaire d'approvisionnement international (nom, pays, devise par défaut, délais moyens) relié aux commandes d'achat et aux produits qu'il fournit habituellement.
- **Client** : destinataire commercial (répartiteur, hôpital, clinique, pharmacie, centrale d'achat) avec conditions de paiement, plafond d'encours et tarifs négociés éventuels.
- **Commande d'Achat** : engagement d'achat auprès d'un fournisseur, en devise et taux de change figés, suivie via une machine à états jusqu'à sa clôture ; composée de lignes produit/quantité/prix.
- **Expédition** : transport physique d'une ou plusieurs commandes d'achat, avec mode de transport, transporteur, régime douanier, dates et coûts logistiques répartis sur ses lignes.
- **Lot de Stock** : unité de traçabilité pharmaceutique immuable après création (sauf quantité restante et statut qualité) — numéro fournisseur, numéro interne, date de péremption, quantités initiale/restante/réservée, coût unitaire figé, statut qualité, historique de libération.
- **Emplacement de Stockage** : position physique dans un entrepôt (zone/allée/rack/niveau/position) typée (réception, quarantaine, stockage, picking, réserve, chaîne du froid, périmés, détruits, transit), pouvant héberger plusieurs lots.
- **Mouvement de Stock** : trace de toute variation de quantité d'un lot (réception, mise en stock, transfert, vente, retour, ajustement, destruction, perte, échantillon, quarantaine, libération) avec utilisateur, date, quantité et document d'origine.
- **Session d'Inventaire** : opération de comptage physique périodique sur un périmètre, avec comptages, écarts calculés et ajustements validés.
- **Profil de Pricing** : ensemble de coefficients (commission, fret, transit, frais de transfert, marge) applicables selon le mode de transport et, optionnellement, la catégorie de produit.
- **Historique de Prix (Produit)** : ligne immuable capturant, à une date donnée, le PMP courant, le prix de vente calculé, le prix de vente appliqué, l'écart entre les deux et le taux de TVA.
- **Commande de Vente** : engagement de vente à un client, en devise choisie, avec lignes produit/quantité, statuts de cycle de vie et allocation FEFO des lots.
- **Livraison / Bon de Livraison** : document matérialisant la sortie physique de marchandise pour une commande de vente.
- **Facture** : document commercial référençant, ligne par ligne, le lot vendu, la quantité, le prix et la TVA appliquée ; exportable en PDF.
- **Retour Client / Avoir** : demande de reprise de marchandise livrée, avec disposition (stock, quarantaine, destruction) et document d'avoir associé.
- **Paramètre de Prévision (MRP)** : configuration par produit (stock de sécurité en jours, fenêtre de consommation) utilisée par le calcul du point de commande.
- **Délai Fournisseur** : délai de fabrication et de transport propre à un couple produit/fournisseur, historisé par date d'effet.
- **Calcul de Prévision** : résultat quotidien du moteur MRP pour un produit (consommation moyenne, point de commande, jours de couverture restants, statut de criticité).
- **Suggestion de Réapprovisionnement** : proposition de commande générée par le MRP, avec quantité suggérée, date limite, et statut (en attente, convertie, rejetée).
- **Utilisateur** : compte d'accès individuel avec identité, statut actif/inactif, historique de connexion et de mots de passe.
- **Rôle / Permission** : regroupement de droits d'accès nommés par module et action, attribué à un ou plusieurs utilisateurs, avec exceptions individuelles possibles.
- **Notification** : message d'alerte lié à un événement métier, ciblé par rôle/permission, avec état lu/non lu par destinataire.
- **Devise / Taux de Change** : référentiel des devises supportées (EUR, USD, XOF) et de leurs taux, historisés et figés au besoin sur les transactions.
- **Journal d'Audit** : trace exhaustive et immuable des actions sensibles effectuées dans le système.
- **Pièce Jointe Réglementaire** : document (facture, douane, certificat, autorisation DPML) rattaché à un lot ou une expédition pour la conformité BPD.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% des mouvements de lots (réception, transfert, vente, retour, ajustement, destruction) sont tracés et consultables a posteriori, sans aucune perte d'historique.
- **SC-002**: 0% des ventes ou sorties de stock ne respectent pas la règle FEFO sans dérogation motivée et journalisée.
- **SC-003**: Le catalogue produit répond aux utilisateurs en moins de 500 millisecondes (P95) lors de la recherche ou de la consultation.
- **SC-004**: L'import d'un catalogue de 200 produits se termine en moins de 10 secondes, avec un rapport d'erreurs exploitable pour toute ligne invalide.
- **SC-005**: Une notification temps réel liée à un événement métier (stock faible, péremption, retard) atteint son ou ses destinataires en moins d'une seconde après la survenue de l'événement.
- **SC-006**: 100% des actions sensibles (validation, changement de statut, suppression logique, libération de lot) sont retrouvables dans le journal d'audit avec utilisateur, date et contexte.
- **SC-007**: 0% de suppression physique de donnée métier n'est possible via l'application — toute suppression reste réversible et consultable dans l'historique.
- **SC-008**: 100% des scénarios de calcul de prix (cascade PA→PR→PV, TVA, écart) sont validés et reproductibles avant mise en production, sans coefficient codé en dur.
- **SC-009**: 0% des lots dont le statut n'est pas "Libéré" ne peuvent être vendus, sans exception.
- **SC-010**: 100% des factures générées font apparaître le numéro de lot de chaque ligne vendue.
- **SC-011**: Les utilisateurs de chaque rôle métier (Admin, Direction, Achats, Logistique, Magasinier, Qualité, Commercial, Comptable, Préparateur) ne voient et n'exécutent que les actions couvertes par leurs permissions, vérifié sur 100% des modules.
- **SC-012**: Le système propose une suggestion de réapprovisionnement pour tout produit franchissant son point de commande, au plus tard lors du calcul quotidien suivant le franchissement.
- **SC-013**: 0% de survente ne se produit même lors de confirmations de commande concurrentes sur les mêmes dernières unités d'un lot ; toute confirmation en conflit est rejetée avec une erreur explicite plutôt que de dépasser la quantité réellement disponible.
- **SC-014**: Le système maintient la latence catalogue (SC-003) et les temps de réponse des opérations transactionnelles courantes (achat, réception, vente) avec au moins 30 utilisateurs actifs simultanés, sans dégradation perceptible par les utilisateurs.
- **SC-015**: Le système reste disponible en continu (24 heures sur 24, 7 jours sur 7), toute interruption de service étant limitée aux fenêtres de maintenance planifiées en dehors des heures ouvrées de LABMEDIS.
- **SC-016**: En cas d'incident nécessitant une restauration à partir d'une sauvegarde, la perte de données constatée n'excède pas quelques minutes.

## Assumptions

*Les points suivants correspondent à des ambiguïtés identifiées dans les sources (`wiki/LABMEDIS/99-a-clarifier.md`) pour lesquelles une hypothèse par défaut est déjà documentée et retenue comme base de cette spécification. Ils devront être confirmés avec le porteur métier LABMEDIS avant ou pendant le premier sprint, mais ne bloquent pas la spécification.*

- Le taux de change USD/XOF est figé à la date de création de la commande d'achat (et non à la réception ou au paiement fournisseur).
- L'assujettissement à la TVA des réactifs de laboratoire est déterminé par un indicateur configurable individuellement par produit (`IsTaxable`), en attendant la liste exhaustive des références taxables/exonérées de la part de LABMEDIS.
- Le prix moyen pondéré (PMP/CUMP) est recalculé uniquement à chaque réception de lot, pas à chaque mouvement de sortie de stock, pour des raisons de performance.
- Les coefficients de pricing (commission, fret, transit, transfert, marge) peuvent varier par catégorie de produit et par mode de transport ; en l'absence de tableau complet fourni par LABMEDIS, un profil global (catégorie non spécifiée) sert de repli.
- La traçabilité est gérée au niveau du lot (et non au niveau de l'unité individuelle/numéro de série), conforme aux pratiques standards d'un dépositaire pharmaceutique de distribution.
- Un portail applicatif dédié aux répartiteurs (accès direct pour passer des commandes) est explicitement hors périmètre de cette version ; l'accès reste interne à LABMEDIS uniquement.
- La gestion de chaîne du froid (stockage +2/+8°C) est prévue comme un type d'emplacement disponible dans le modèle, mais son activation opérationnelle (capteurs, alertes température) reste à confirmer.
- L'export vers un logiciel comptable externe (Sage, QuickBooks, etc.) est hors périmètre de cette version ; seul un export Excel/CSV des factures et avoirs est requis.
- L'Incoterm sur une commande d'achat est une information optionnelle, affichée sur le document de commande si renseignée.
- La référence d'autorisation d'importation DPML est optionnelle sur une expédition, sauf pour les expéditions contenant des médicaments où elle devient obligatoire.
- Le format du numéro de lot interne suit la structure `{code_produit}-{AAAAMMJJ}-{NNN}` par défaut, à valider avec LABMEDIS.
- Les utilisateurs disposent d'une connectivité internet stable pour l'utilisation du système et de ses notifications temps réel.
- L'export vers un système comptable, la gestion de la paie et les ressources humaines sont explicitement exclus du périmètre (voir vision produit, section Périmètre).
- Le référentiel initial (produits, fournisseurs, clients) est repris depuis les données existantes (138 références produits, 8 fournisseurs, 12 clients connus) via l'import de masse plutôt que ressaisi manuellement.
