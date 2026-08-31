# Contrat API — Prévision (MRP) & Réapprovisionnement (US10)

Toutes les routes `[Authorize]`, préfixées `api/forecast`, permission `Forecast.*`.

| Route | Description |
|---|---|
| `GET /api/forecast/suggestions?status=` | Liste des suggestions (EnAttente \| Converti \| Rejeté — FR-065) |
| `POST /api/forecast/suggestions/{id}/convert` | Convertit en commande d'achat (`Brouillon`, pré-remplie) — délègue à `POST /api/purchase-orders` |
| `POST /api/forecast/suggestions/{id}/reject` | Clôture la suggestion sans action |
| `POST /api/forecast/run` | Déclenche manuellement le calcul (normalement exécuté par `StockForecastJob` Hangfire quotidien, FR-063) — usage admin/diagnostic |
| `GET /api/forecast/products/{id}/parameters` | Paramètres MRP du produit (`safetyStockDays`, `consumptionWindowDays`, consommation manuelle si absente d'historique — FR-066) |
| `PUT /api/forecast/products/{id}/parameters` | Mise à jour des paramètres |

Réponse `GET /api/forecast/suggestions` (extrait) :
```json
[{ "productId": "guid", "suggestionDate": "date", "orderDeadline": "date", "suggestedQuantity": "int", "status": "string", "criticality": "OK|Surveiller|Urgent|Critique" }]
```

---

**Traçabilité** : FR-063 à FR-067, SC-012.
