import { useCallback, useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { startNotificationHub, getNotificationHubConnection } from '../../services/signalrClient'

interface Notification {
  id: string
  eventType: string
  payload: string
  isRead: boolean
  createdAt: string
}

const EVENT_LABELS: Record<string, string> = {
  'stock:low': 'Stock faible',
  'stock:outOfStock': 'Rupture de stock',
  'lot:expiringSoon': 'Péremption proche',
  'order:pendingApproval': 'Commande en attente de validation',
  'order:lateDelivery': 'Retard de livraison',
  'shipment:arrived': 'Expédition arrivée',
  'mrp:suggestion': 'Suggestion de réapprovisionnement',
  'quarantine:prolonged': 'Quarantaine prolongée',
  'dpml:expiringSoon': 'Licence DPML proche expiration',
  'lot:suspectedFalsified': 'Lot suspecté falsifié',
}

/**
 * Centre de notifications (US12 — contracts/notifications.md, FR-076 à FR-078, FR-094).
 * Loads persisted notifications on mount (survives having been offline at emission time),
 * then stays live via the shared SignalR connection — no polling (Principle IX).
 */
export function NotificationCenter() {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [isOpen, setIsOpen] = useState(false)

  const loadNotifications = useCallback(async () => {
    try {
      const response = await apiClient.get<Notification[]>('/api/notifications')
      setNotifications(response.data)
    } catch {
      // Best-effort — the badge simply stays at its last known count on failure.
    }
  }, [])

  useEffect(() => {
    void loadNotifications()

    let cancelled = false
    void startNotificationHub().then(() => {
      if (cancelled) {
        return
      }
      getNotificationHubConnection().on('notification:new', () => void loadNotifications())
    })

    return () => {
      cancelled = true
      getNotificationHubConnection().off('notification:new')
    }
  }, [loadNotifications])

  const unreadCount = notifications.filter((n) => !n.isRead).length

  async function markRead(id: string) {
    await apiClient.post(`/api/notifications/${id}/read`)
    setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)))
  }

  async function markAllRead() {
    await apiClient.post('/api/notifications/mark-all-read')
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className="relative rounded p-2 text-sm text-gray-500 hover:bg-gray-100"
        aria-label="Notifications"
      >
        🔔
        {unreadCount > 0 && (
          <span className="absolute -right-1 -top-1 flex h-4 w-4 items-center justify-center rounded-full bg-red-600 text-[10px] font-semibold text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 z-10 mt-2 w-80 rounded border border-gray-200 bg-white shadow-lg">
          <div className="flex items-center justify-between border-b border-gray-100 px-3 py-2">
            <span className="text-sm font-semibold">Notifications</span>
            {unreadCount > 0 && (
              <button type="button" onClick={() => void markAllRead()} className="text-xs text-gray-500 hover:underline">
                Tout marquer comme lu
              </button>
            )}
          </div>
          <ul className="max-h-96 overflow-y-auto">
            {notifications.length === 0 && <li className="px-3 py-4 text-center text-sm text-gray-500">Aucune notification</li>}
            {notifications.map((notification) => (
              <li
                key={notification.id}
                className={`cursor-pointer border-b border-gray-50 px-3 py-2 text-sm hover:bg-gray-50 ${notification.isRead ? 'text-gray-400' : 'font-medium text-gray-900'}`}
                onClick={() => !notification.isRead && void markRead(notification.id)}
              >
                <p>{EVENT_LABELS[notification.eventType] ?? notification.eventType}</p>
                <p className="text-xs text-gray-400">{new Date(notification.createdAt).toLocaleString('fr-FR')}</p>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
