import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'

interface OrderLine {
  id: string
  productId: string
  productDesignation: string | null
  quantity: number
}

interface PurchaseOrder {
  id: string
  orderNumber: string
  status: string
  lines: OrderLine[]
}

interface StorageLocationOption {
  id: string
  code: string
}

interface LineDraft {
  lotNumber: string
  expiryDate: string
  quantityReceived: string
  storageLocationId: string
  qualityStatus: string
}

const RECEIVABLE_STATUSES = ['Envoyee', 'EnFabrication', 'PreteAExpedier', 'Expediee', 'EnTransit', 'PartiellementRecue']

/** Réception Stock (US4 — contracts/purchase-orders.md POST .../receive, FR-029 à FR-033). */
export function StockReceptionPage() {
  const [orders, setOrders] = useState<PurchaseOrder[]>([])
  const [locations, setLocations] = useState<StorageLocationOption[]>([])
  const [selectedOrder, setSelectedOrder] = useState<PurchaseOrder | null>(null)
  const [drafts, setDrafts] = useState<Record<string, LineDraft>>({})
  const [error, setError] = useState<string | null>(null)
  const [report, setReport] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const loadOrders = useCallback(async () => {
    const response = await apiClient.get<PurchaseOrder[]>('/api/purchase-orders')
    setOrders(response.data.filter((o) => RECEIVABLE_STATUSES.includes(o.status)))
  }, [])

  useEffect(() => {
    void loadOrders()
    apiClient.get<StorageLocationOption[]>('/api/warehouses/locations').then((r) => setLocations(r.data)).catch(() => setLocations([]))
  }, [loadOrders])

  function selectOrder(order: PurchaseOrder) {
    setSelectedOrder(order)
    setReport(null)
    setError(null)
    setDrafts(
      Object.fromEntries(
        order.lines.map((line) => [
          line.id,
          { lotNumber: '', expiryDate: '', quantityReceived: String(line.quantity), storageLocationId: '', qualityStatus: 'EnAttenteLiberation' },
        ])
      )
    )
  }

  function updateDraft(lineId: string, patch: Partial<LineDraft>) {
    setDrafts((prev) => ({ ...prev, [lineId]: { ...prev[lineId], ...patch } }))
  }

  async function handleSubmit() {
    if (!selectedOrder) {
      return
    }

    setError(null)
    setIsSubmitting(true)
    try {
      const payload = selectedOrder.lines.map((line) => {
        const draft = drafts[line.id]
        return {
          lineId: line.id,
          lotNumber: draft.lotNumber,
          expiryDate: draft.expiryDate,
          quantityReceived: Number.parseInt(draft.quantityReceived, 10),
          storageLocationId: draft.storageLocationId,
          qualityStatus: draft.qualityStatus,
        }
      })

      await apiClient.post(`/api/purchase-orders/${selectedOrder.id}/receive`, payload)
      setReport(`Réception enregistrée pour la commande ${selectedOrder.orderNumber}.`)
      setSelectedOrder(null)
      await loadOrders()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.stockReception}</h1>
      {report && <p className="rounded bg-green-50 p-3 text-sm text-green-700">{report}</p>}

      {!selectedOrder && (
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">N° Commande</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {orders.length === 0 && (
              <tr>
                <td colSpan={3} className="px-3 py-4 text-center text-gray-500">Aucune commande en attente de réception.</td>
              </tr>
            )}
            {orders.map((order) => (
              <tr key={order.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{order.orderNumber}</td>
                <td className="px-3 py-2">{order.status}</td>
                <td className="px-3 py-2 text-right">
                  <button type="button" onClick={() => selectOrder(order)} className="text-gray-700 hover:underline">
                    Réceptionner
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {selectedOrder && (
        <div className="space-y-4 rounded border border-gray-200 bg-white p-4">
          <div className="flex items-center justify-between">
            <h2 className="font-medium">{selectedOrder.orderNumber}</h2>
            <button type="button" onClick={() => setSelectedOrder(null)} className="text-sm text-gray-500 hover:underline">
              Annuler
            </button>
          </div>

          {selectedOrder.lines.map((line) => {
            const draft = drafts[line.id]
            return (
              <div key={line.id} className="grid grid-cols-1 gap-2 border-t border-gray-100 pt-3 sm:grid-cols-5">
                <span className="text-sm sm:col-span-5">{line.productDesignation} — quantité commandée : {line.quantity}</span>
                <input
                  placeholder="N° lot fournisseur"
                  value={draft.lotNumber}
                  onChange={(e) => updateDraft(line.id, { lotNumber: e.target.value })}
                  className="rounded border border-gray-300 px-3 py-2 text-sm"
                />
                <input
                  type="date"
                  value={draft.expiryDate}
                  onChange={(e) => updateDraft(line.id, { expiryDate: e.target.value })}
                  className="rounded border border-gray-300 px-3 py-2 text-sm"
                />
                <input
                  type="number"
                  min={1}
                  value={draft.quantityReceived}
                  onChange={(e) => updateDraft(line.id, { quantityReceived: e.target.value })}
                  className="rounded border border-gray-300 px-3 py-2 text-sm"
                />
                <select
                  value={draft.storageLocationId}
                  onChange={(e) => updateDraft(line.id, { storageLocationId: e.target.value })}
                  className="rounded border border-gray-300 px-3 py-2 text-sm"
                >
                  <option value="">Emplacement…</option>
                  {locations.map((loc) => (
                    <option key={loc.id} value={loc.id}>{loc.code}</option>
                  ))}
                </select>
                <select
                  value={draft.qualityStatus}
                  onChange={(e) => updateDraft(line.id, { qualityStatus: e.target.value })}
                  className="rounded border border-gray-300 px-3 py-2 text-sm"
                >
                  <option value="EnAttenteLiberation">En attente de libération</option>
                  <option value="EnQuarantaine">En quarantaine</option>
                </select>
              </div>
            )
          })}

          {error && <p className="text-sm text-red-600">{error}</p>}

          <button
            type="button"
            onClick={() => void handleSubmit()}
            disabled={isSubmitting}
            className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50"
          >
            {isSubmitting ? labels.states.loading : 'Confirmer la réception'}
          </button>
        </div>
      )}
    </div>
  )
}
