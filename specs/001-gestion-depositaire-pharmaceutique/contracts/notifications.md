# Contrat API — Notifications Temps Réel (US12, US13)

## Hub SignalR — `/hubs/notifications`
**Auth**: JWT (query string ou header au handshake, `[Authorize]` sur le Hub). Backplane Redis pour la scalabilité multi-instance (Principe IX). **Zéro polling** — le frontend s'abonne une fois à la connexion.

Événements serveur → client (FR-076) :
| Événement | Déclencheur | Payload minimal |
|---|---|---|
| `stock:low` | Stock dispo < seuil produit | `{ productId, currentQty, threshold }` |
| `stock:outOfStock` | Stock dispo = 0 | `{ productId }` |
| `lot:expiringSoon` | Péremption à J-30/60/90/120 selon catégorie | `{ lotId, productId, expiryDate, daysRemaining }` |
| `order:pendingApproval` | PO en attente validation Direction | `{ purchaseOrderId }` |
| `order:lateDelivery` | PO en retard | `{ purchaseOrderId, expectedDate }` |
| `shipment:arrived` | Événement expédition "Arrivé" | `{ shipmentId }` |
| `mrp:suggestion` | Suggestion MRP créée | `{ reorderSuggestionId, productId }` |
| `quarantine:prolonged` | Lot en quarantaine au-delà d'un seuil | `{ lotId }` |
| `dpml:expiringSoon` | Licence DPML proche expiration | `{ shipmentId }` |
| `lot:suspectedFalsified` | Lot suspecté falsifié | `{ lotId }` |

Ciblage : chaque événement est envoyé uniquement aux connexions dont l'utilisateur possède le rôle/permission concerné (FR-077) — implémenté via des groupes SignalR nommés par rôle/permission.

## REST — `api/notifications`

| Route | Description |
|---|---|
| `GET /api/notifications?unreadOnly=` | Liste des notifications persistées de l'utilisateur courant (FR-094 — garantit la récupération même si hors ligne au moment de l'émission) |
| `POST /api/notifications/{id}/read` | Marque comme lue pour l'utilisateur courant uniquement (état par utilisateur, FR-078) |
| `POST /api/notifications/mark-all-read` | Marque tout comme lu |

Canaux secondaires (FR-079) : les événements marqués `critique` dans la configuration déclenchent en plus un envoi via `INotificationService` (FluentEmail / Twilio SMS), de façon asynchrone (ne bloque pas l'émission SignalR).

---

**Traçabilité** : FR-076 à FR-079, FR-094, SC-005.
