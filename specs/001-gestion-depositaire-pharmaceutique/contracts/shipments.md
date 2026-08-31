# Contrat API — Expéditions / Logistique (US3)

Toutes les routes `[Authorize]`, préfixées `api/shipments`, permission `Shipments.*`.

| Route | Description |
|---|---|
| `POST /api/shipments` | Création (mode transport, transporteur, référence suivi, régime douanier — FR-025). `importAuthorizationRef` requis si la commande liée contient un médicament (FR-028, `400 DPML_REF_REQUIRED`). |
| `GET /api/shipments/{id}` | Détail |
| `POST /api/shipments/{id}/costs` | Ajout d'un frais logistique (FR-026) : `{ "costType": "Freight|Transit|Douane|Commission|Transfert|Assurance|Manutention", "amount": "string", "allocationKey": "Valeur|Quantité|Volume" }` |
| `POST /api/shipments/{id}/events` | Événement de suivi (Expédié → Arrivé port → En douane → Dédouané → Livré) — déclenche `shipment:arrived` (SignalR, voir `notifications.md`) |
| `GET /api/shipments/{id}/timeline` | Historique chronologique des événements et dates estimées/réelles |

Réponse `POST /api/shipments` (`201`) :
```json
{ "id": "guid", "shipmentNumber": "string", "status": "string" }
```

---

**Traçabilité** : FR-025 à FR-028.
