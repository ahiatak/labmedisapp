import os

def ensure_dir(path):
    os.makedirs(path, exist_ok=True)

base_dir = r"D:\workspace\LabMedisApp\wiki"

dirs = [
    r"LABMEDIS\06-regles-metier",
    r"LABMEDIS\03-data-model",
    r"LABMEDIS\01-features",
    r"LABMEDIS\02-workflows",
    r"LABMEDIS\04-api-contracts",
    r"LABMEDIS\05-ui-states",
    r"_meta"
]
for d in dirs:
    ensure_dir(os.path.join(base_dir, d))

def write_file(rel_path, content):
    full_path = os.path.join(base_dir, rel_path)
    with open(full_path, "w", encoding="utf-8") as f:
        f.write(content.strip())
    print(f"Written {rel_path} ({len(content.splitlines())} lines)")

# Data for 00-vision.md
write_file(r"LABMEDIS\00-vision.md", """
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
""")

# RGs
write_file(r"LABMEDIS\06-regles-metier\RG-001-fefo.md", """
# RG-001 : Algorithme FEFO (First Expired, First Out)

## 1. Définition et Application
La méthode [[FEFO]] DOIT être appliquée OBLIGATOIREMENT lors de toute sortie de stock.

## 2. Algorithme de sélection (Étapes)
1. Le système DOIT exclure tous les lots dont le statut est `Périmé`.
2. Le système DOIT exclure tous les lots en quarantaine ou non conformes.
3. Le système DOIT exclure les quantités déjà réservées sur le stock.
4. Le système DOIT trier les lots restants par Date de Péremption (ASCENDING).
5. En cas d'égalité, le système DOIT prioriser par emplacement de picking.
6. Le système DOIT allouer les quantités jusqu'à satisfaire la demande.

## 3. Gestion des quantités insuffisantes
- Si le stock est insuffisant, le système DOIT retourner une erreur stricte (ou statut partiel si autorisé).

## 4. Dérogation
La dérogation EST AUTORISÉE SI ET SEULEMENT SI :
- Le lot NE DOIT PAS être périmé.
- Le lot NE DOIT PAS être en quarantaine.
- La quantité dispo DOIT être suffisante.
- Le motif DOIT être saisi et tracé.

## 5. Statuts Lot
- Statuts proposables : UNIQUEMENT `Libéré`
- Statuts bloquants : `En réception`, `En quarantaine`, `Non conforme`, `Périmé`, `Détruit`, `En attente de libération`, `Suspecté falsifié`
""")

write_file(r"LABMEDIS\06-regles-metier\RG-002-cump-pmp.md", """
# RG-002 : Calcul PMP

## 1. Règle Générale
Le PMP DOIT être recalculé à CHAQUE RÉCEPTION de lot.

## 2. Formule
`PMP = Σ(quantité_lot × PRU_lot) / Σ(quantité_lot)`
Sur TOUS les lots disponibles d'un même produit.

## 3. Inclusion des lots
- Statut = `Libéré` ET quantité restante > 0.

## 4. Fixité du PRU
Le PRU d'un lot EST figé à la réception (JAMAIS recalculé a posteriori).

## 5. Pondération
Les lots bateau et avion ont des PRU différents, le PMP DOIT pondérer tous les lots disponibles.
""")

write_file(r"LABMEDIS\06-regles-metier\RG-003-conversion-devise.md", """
# RG-003 : Règles devises

## 1. EUR/XOF
- Taux FIXE à 655.957.
- Modifiable UNIQUEMENT par Admin avec action explicite.

## 2. USD/XOF
- Taux variable, saisi manuellement par Admin, historisé avec date d'application.

## 3. Taux figé sur commande
- Le `locked_exchange_rate_id` EST figé sur la commande et JAMAIS recalculé a posteriori.
- Date de figement : date d'émission de la commande.

## 4. Conversion en cascade
- Tous les calculs de pricing en CFA DOIVENT utiliser le taux figé du lot.
""")

write_file(r"LABMEDIS\06-regles-metier\RG-004-cascade-prix.md", """
# RG-004 : Formule complète de cascade de prix

## 1. Formule
```text
PA (Euro) → PA (CFA) = PA_Euro × taux_change
PA (CFA) × Coeff_Commission × Coeff_Freight × Coeff_Transit × Coeff_FraisTransfert = PR (CFA)
PR (CFA) × Coeff_Marge = PV HT Calculé
PV HT Appliqué = ajustement manuel possible
Écart = PV HT Calculé - PV HT Appliqué → conservé JAMAIS écrasé
```

## 2. Valeurs de validation (France Lait)
- PA 3.41€, taux ~656
- Coeff_Commission = 1.25, Coeff_Freight = 1.03, Coeff_Transit = 1.09, Coeff_FraisTransfert = 1.07
- Résultat PR = 3359 CFA, PV HT = 3695 CFA

## 3. Contraintes
- TOUS les coefficients DOIVENT être en table PricingProfile, JAMAIS hardcodés.
- Variables par TransportMode (Maritime/Aérien/Express/Terrestre) ET CategoryId.
- Arrondi CFA OBLIGATOIRE (Math.Round(value, 0, MidpointRounding.AwayFromZero)).
""")

write_file(r"LABMEDIS\06-regles-metier\RG-005-soft-delete.md", """
# RG-005 : Soft Delete

## 1. Règle
- `IsDeleted = true` sur TOUTE suppression.
- JAMAIS de DELETE physique en base (zéro cascade delete physique).

## 2. Index unique
- Les index uniques DOIVENT être partiels : `WHERE deleted_at IS NULL`.

## 3. Entités concernées
- TOUTES les tables métier sauf tables append-only (user_password_history, notification_reads).
""")

write_file(r"LABMEDIS\06-regles-metier\RG-006-unicite-lot.md", """
# RG-006 : Unicité du lot

## 1. Unicité
- Le numéro de lot DOIT être unique par couple (fournisseur_id, produit_id).
- Deux lots NE PEUVENT PAS partager le même numéro pour le même produit du même fournisseur.

## 2. Quantité
- Quantité reçue = en unités réelles (JAMAIS calculée depuis carton × nb/carton).

## 3. Emplacements
- Un lot PEUT être stocké sur PLUSIEURS emplacements (`stock_lot_locations`).
- Un emplacement PEUT contenir PLUSIEURS lots différents.
""")

write_file(r"LABMEDIS\06-regles-metier\RG-007-tva.md", """
# RG-007 : Gestion de la TVA

## 1. Catégories
| Catégorie | TVA | Base légale |
|---|---|---|
| Produit infantile | 18% | Hors liste UEMOA |
| Cosmétique | 18% | Hors liste UEMOA |
| Complément alimentaire | 18% | Hors liste UEMOA |
| Insecticide | 18% | À confirmer |
| Médicament | 0% | Directive UEMOA 06/2002 |
| Réactif de laboratoire | Flag IsTaxable | Variable |

## 2. Règles
- La TVA DOIT être configurable par produit (jamais déduite automatiquement de la catégorie seule).
- `PV TTC = PV HT × (1 + taux_TVA)`.
""")

write_file(r"LABMEDIS\06-regles-metier\RG-008-quarantaine.md", """
# RG-008 : Quarantaine

## 1. Statuts
`En réception` → `En quarantaine` | `Libéré` | `Non conforme` | `Périmé` | `Détruit`

## 2. Règles
- Vente INTERDITE si statut ≠ `Libéré`.
- Libération : rôle Responsable Qualité UNIQUEMENT, action journalisée.
- Mise en quarantaine : motif OBLIGATOIRE, emplacement quarantaine OBLIGATOIRE.
- `En attente de libération` : statut dédié pour lots non encore libérés par fabricant/importateur (BPD UEMOA).
- `Suspecté falsifié` : statut dédié, autorité compétente à notifier.
""")

write_file(r"LABMEDIS\06-regles-metier\RG-009-arrondi-cfa.md", """
# RG-009 : Arrondi CFA

## 1. Règle
- Devise CFA (XOF) : ZÉRO décimale.
- Arrondi : `Math.Round(value, 0, MidpointRounding.AwayFromZero)`.

## 2. Application
- Extension method C# : `public static decimal ToCfaRounded(this decimal value) { return Math.Round(value, 0, MidpointRounding.AwayFromZero); }`
- Sur PA_CFA, PR_CFA, PV_HT, PV_TTC.

## 3. Contrainte
- Les calculs intermédiaires DOIVENT conserver la précision decimal avant arrondi final.
""")

print("Done RG")
