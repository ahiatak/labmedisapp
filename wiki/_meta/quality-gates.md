---
id: "LABMEDIS-META-QG"
projet: "LABMEDIS"
type: "meta"
titre: "Quality Gates"
priorite: "Critique"
statut: "validé"
source_raw: []
date_creation: "2026-08-28"
date_maj: "2026-08-28"
tags: ["#labmedis", "#meta", "#quality"]
---

# Quality Gates (Critères d'Acceptation)

## 1. Structure de l'EKB
- Chaque fichier DOIT comporter le frontmatter YAML exact.
- Chaque fichier DOIT appartenir à un domaine spécifique (01-features, 02-workflows, etc.).

## 2. Syntaxe et Contenu
- Le langage DOIT être IMPÉRATIF (DOIT, NE DOIT PAS, EST REQUIS, EST INTERDIT).
- Les mots "devrait", "pourrait", "idéalement", "généralement", "etc.", "idem" SONT INTERDITS.
- Chaque concept DOIT résider dans son propre fichier.
- Les wikilinks DOIVENT être utilisés pour référencer d'autres entités.

## 3. Architecture Backend
- L'architecture 3 couches (Core, Service, Api) EST REQUISE.
- Les suppressions physiques SONT INTERDITES (Soft delete `IsDeleted = true` EXCLUSIVEMENT).
- NLog EST REQUIS pour le logging.
- Hangfire EST REQUIS pour les tâches de fond.

## 4. Règles de Prix
- L'écart entre PV HT calculé et PV HT appliqué DOIT être conservé et NE DOIT JAMAIS être écrasé.
- Les taux de change DOIVENT respecter les règles (EUR/XOF fixe 655.957).
