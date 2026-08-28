# 00 Vision et Périmètre du Produit

## 1. Résumé Exécutif
LABMEDIS est une application métier destinée à remplacer la gestion actuelle par Excel. Le système DOIT fournir un contrôle exhaustif sur les stocks, les achats, les ventes et la traçabilité des lots.
Les informations contenues dans ce document SONT OBLIGATOIRES et NE DOIVENT PAS être ignorées.

## 2. Acteurs
- **Admin** : DOIT avoir tous les droits de configuration.
- **Direction** : DOIT valider les commandes critiques.
- **Responsable Achats** : DOIT gérer les fournisseurs et le [[CUMP/PMP]].
- **Logistique/Magasinier** : DOIT réceptionner et mettre en stock.
- **Responsable Qualité** : DOIT être le seul à libérer les lots en quarantaine.
- **Commercial** : DOIT gérer les commandes de vente.
- **Comptable** : DOIT vérifier la facturation.
- **Préparateur** : DOIT préparer les commandes (Picking).

## 3. Modèle d'Affaires
Le modèle EST basé sur l'achat de produits de santé (importation), leur dédouanement et leur revente.
Le [[Landing Cost]] DOIT être calculé précisément à chaque réception.

## 4. Objectifs (7)
1. Le système DOIT tracer 100% des mouvements de lots.
2. Le système DOIT automatiser la sortie via [[FEFO]].
3. Le système DOIT calculer le prix de revient de manière automatique.
4. Le système DOIT gérer la péremption stricte.
5. Le système DOIT empêcher toute vente à perte ou avec un lot non conforme.
6. Le système DOIT notifier la direction pour les commandes de réapprovisionnement.
7. Le système DOIT interdire la suppression physique des données (Soft Delete).

## 5. Périmètre
- **Inclus** : Achats, Ventes, Stocks, Lots, MRP, Pricing, Reporting, Auth/RBAC.
- **Exclus** : Export vers logiciel comptable (v1), Ressources Humaines, Paie.

## 6. Critères de Succès
- 0% d'erreurs sur la traçabilité des numéros de lots.
- Couverture totale des tests sur le moteur de pricing.
- Latence inférieure à 500ms sur le catalogue produit.
- Adhésion des utilisateurs.

## 7. Pré-mortem
Si le projet échoue, CE SERA parce que :
- Les utilisateurs refusent de saisir les numéros de lots ou scannent mal.
- La règle FEFO est contournée sans trace.
Le système DOIT donc rendre le contournement impossible et exiger un motif pour toute dérogation.

## 8. Stack Technique
- Backend : C# .NET 8, PostgreSQL, Entity Framework Core, Hangfire (MRP), SignalR (Notifs).
- Frontend : React, TypeScript, TailwindCSS.