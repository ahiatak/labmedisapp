# Contrat API — Reporting & Tableaux de Bord (US11)

Toutes les routes `[Authorize]`, préfixées `api/reports`, permission `Reports.Read` (le contenu retourné est filtré selon le rôle — ex. Comptable ne voit pas les mêmes agrégats que Magasinier).

| Route | Description |
|---|---|
| `GET /api/reports/dashboard/direction` | CA, marge, valeur de stock, ruptures (FR-068) |
| `GET /api/reports/stock` | Vue globale disponible/réservé/quarantaine/périmé + rotation lente (FR-070) |
| `GET /api/reports/lots/expiring?days=` | Lots proches péremption sur fenêtre paramétrable (FR-069) |
| `GET /api/reports/lots/slow-moving` | Produits à rotation lente |
| `GET /api/reports/sales` | CA par client/produit, taux de retour/service (FR-071) |
| `GET /api/reports/pricing` | Marge théorique vs réelle, écart PV (FR-072) |
| `GET /api/reports/quality` | Lots en quarantaine/non conformes (FR-073) |
| `POST /api/reports/export` | `{ "reportType": "string", "format": "Pdf|Excel", "filters": {} }` → fichier généré (FR-074) |

Tous les tableaux de bord sont complétés côté frontend par un abonnement SignalR (`DashboardHub`, voir `notifications.md`) pour la mise à jour temps réel sans re-fetch manuel (FR-075).

---

**Traçabilité** : FR-068 à FR-075, SC-005.
