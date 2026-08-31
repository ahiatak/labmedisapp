import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatCfa } from '../../i18n/format'

interface OrderLine {
  id: string
  productDesignation: string | null
  quantity: number
}

interface SaleOrder {
  id: string
  orderNumber: string
  status: string
  lines: OrderLine[]
}

interface CustomerReturn {
  id: string
  returnNumber: string
  status: string
  reason: string
  creditNoteNumber: string | null
  creditNoteAmount: string | null
}

const RETURNABLE_STATUSES = ['Livree', 'Facturee']
const DISPOSITIONS = [
  { value: 'RemiseEnStock', label: 'Remise en stock' },
  { value: 'Quarantaine', label: 'Quarantaine' },
  { value: 'Destruction', label: 'Destruction' },
]

/** Retours Clients (US8 — contracts/sales.md, FR-060 à FR-062). */
export function ReturnsPage() {
  const [orders, setOrders] = useState<SaleOrder[]>([])
  const [selectedOrder, setSelectedOrder] = useState<SaleOrder | null>(null)
  const [returns, setReturns] = useState<CustomerReturn[]>([])

  const [lineId, setLineId] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [disposition, setDisposition] = useState(DISPOSITIONS[0].value)
  const [motif, setMotif] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [lastCreditNote, setLastCreditNote] = useState<CustomerReturn | null>(null)

  const loadOrders = useCallback(async () => {
    const response = await apiClient.get<SaleOrder[]>('/api/sale-orders')
    setOrders(response.data.filter((o) => RETURNABLE_STATUSES.includes(o.status)))
  }, [])

  useEffect(() => {
    void loadOrders()
  }, [loadOrders])

  async function selectOrder(order: SaleOrder) {
    setSelectedOrder(order)
    setLastCreditNote(null)
    setError(null)
    const response = await apiClient.get<CustomerReturn[]>(`/api/sale-orders/${order.id}/returns`)
    setReturns(response.data)
  }

  async function handleSubmit() {
    if (!selectedOrder || !lineId) {
      return
    }

    setError(null)
    try {
      const response = await apiClient.post<CustomerReturn>(`/api/sale-orders/${selectedOrder.id}/returns`, {
        saleOrderLineId: lineId,
        quantity: Number.parseInt(quantity, 10),
        disposition,
        motif: motif || null,
      })
      setLastCreditNote(response.data)
      setMotif('')
      await selectOrder(selectedOrder)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError(labels.states.error)
      }
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.returns}</h1>

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
                <td colSpan={3} className="px-3 py-4 text-center text-gray-500">Aucune commande livrée éligible à un retour.</td>
              </tr>
            )}
            {orders.map((order) => (
              <tr key={order.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{order.orderNumber}</td>
                <td className="px-3 py-2">{order.status}</td>
                <td className="px-3 py-2 text-right">
                  <button type="button" onClick={() => void selectOrder(order)} className="text-gray-700 hover:underline">
                    Initier un retour
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
              Retour à la liste
            </button>
          </div>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
            <select value={lineId} onChange={(e) => setLineId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2">
              <option value="">Ligne concernée…</option>
              {selectedOrder.lines.map((line) => (
                <option key={line.id} value={line.id}>{line.productDesignation} (qté livrée : {line.quantity})</option>
              ))}
            </select>
            <input type="number" min={1} value={quantity} onChange={(e) => setQuantity(e.target.value)} placeholder="Quantité" className="rounded border border-gray-300 px-3 py-2 text-sm" />
            <select value={disposition} onChange={(e) => setDisposition(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
              {DISPOSITIONS.map((d) => (
                <option key={d.value} value={d.value}>{d.label}</option>
              ))}
            </select>
            <input
              value={motif}
              onChange={(e) => setMotif(e.target.value)}
              placeholder={disposition === 'Quarantaine' ? 'Motif (obligatoire)' : 'Motif (optionnel)'}
              className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-3"
            />
            <button type="button" onClick={() => void handleSubmit()} className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800">
              Confirmer le retour
            </button>
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}
          {lastCreditNote?.creditNoteNumber && (
            <p className="rounded bg-green-50 p-3 text-sm text-green-700">
              Avoir généré : {lastCreditNote.creditNoteNumber} — {formatCfa(Number(lastCreditNote.creditNoteAmount))}
            </p>
          )}

          <div>
            <h3 className="mb-2 text-sm font-semibold text-gray-700">Retours déjà traités</h3>
            <ul className="space-y-1 text-sm">
              {returns.length === 0 && <li className="text-gray-500">{labels.states.empty}</li>}
              {returns.map((r) => (
                <li key={r.id}>
                  {r.returnNumber} — {r.reason} {r.creditNoteNumber && `— Avoir ${r.creditNoteNumber} (${formatCfa(Number(r.creditNoteAmount))})`}
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}
