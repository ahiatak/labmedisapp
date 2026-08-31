import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatCfa, formatDateFr } from '../../i18n/format'
import { PermissionGate } from '../../routes/PermissionGate'

interface Lookup {
  id: string
  name: string
}

interface OrderLine {
  productId: string
  productDesignation: string | null
  quantity: number
  allocatedInternalLotNumber: string | null
}

interface SaleOrder {
  id: string
  orderNumber: string
  customerName: string | null
  status: string
  orderDate: string
  totalTtc: string
  lines: OrderLine[]
}

const STATUS_LABELS: Record<string, string> = {
  Brouillon: 'Brouillon',
  Confirmee: 'Confirmée',
  Livree: 'Livrée',
  Facturee: 'Facturée',
  Annulee: 'Annulée',
}

/** Commandes de Vente (US7 — contracts/sales.md, FR-054 à FR-059). */
export function SaleOrdersPage() {
  const [orders, setOrders] = useState<SaleOrder[]>([])
  const [customers, setCustomers] = useState<Lookup[]>([])
  const [currencies, setCurrencies] = useState<Lookup[]>([])
  const [products, setProducts] = useState<Lookup[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const [customerId, setCustomerId] = useState('')
  const [currencyId, setCurrencyId] = useState('')
  const [lineProductId, setLineProductId] = useState('')
  const [lineQuantity, setLineQuantity] = useState('1')
  const [lines, setLines] = useState<{ productId: string; designation?: string; quantity: number }[]>([])
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const loadOrders = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await apiClient.get<SaleOrder[]>('/api/sale-orders')
      setOrders(response.data)
    } catch {
      setError(labels.states.error)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadOrders()
    apiClient.get<{ id: string; name: string }[]>('/api/customers').then((r) => setCustomers(r.data)).catch(() => setCustomers([]))
    apiClient.get<Lookup[]>('/api/currencies').then((r) => setCurrencies(r.data)).catch(() => setCurrencies([]))
    apiClient
      .get<{ items: Lookup[] }>('/api/products', { params: { selectableOnly: true, pageSize: 200 } })
      .then((r) => setProducts(r.data.items))
      .catch(() => setProducts([]))
  }, [loadOrders])

  function addLine() {
    if (!lineProductId) {
      return
    }
    setLines((prev) => [
      ...prev,
      { productId: lineProductId, designation: products.find((p) => p.id === lineProductId)?.name, quantity: Number.parseInt(lineQuantity, 10) || 1 },
    ])
    setLineProductId('')
    setLineQuantity('1')
  }

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    if (!customerId || !currencyId || lines.length === 0) {
      setFormError('Client, devise et au moins une ligne sont requis.')
      return
    }

    setIsSubmitting(true)
    try {
      await apiClient.post('/api/sale-orders', {
        customerId,
        currencyId,
        lines: lines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
      })
      setLines([])
      setCustomerId('')
      await loadOrders()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setFormError(err.response.data.message)
      } else {
        setFormError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function runAction(action: () => Promise<unknown>) {
    setActionError(null)
    try {
      await action()
      await loadOrders()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setActionError(err.response.data.message)
      } else {
        setActionError(labels.states.error)
      }
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.saleOrders}</h1>

      <PermissionGate permission="Sales.Create">
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="space-y-3 rounded border border-gray-200 bg-white p-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <select required value={customerId} onChange={(e) => setCustomerId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">Client…</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            <select required value={currencyId} onChange={(e) => setCurrencyId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">Devise…</option>
              {currencies.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-1 gap-3 border-t border-gray-100 pt-3 sm:grid-cols-4">
            <select value={lineProductId} onChange={(e) => setLineProductId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2">
              <option value="">Produit…</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
            <input type="number" min={1} value={lineQuantity} onChange={(e) => setLineQuantity(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
            <button type="button" onClick={addLine} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
              Ajouter la ligne
            </button>
          </div>

          {lines.length > 0 && (
            <ul className="divide-y divide-gray-100 text-sm">
              {lines.map((line, index) => (
                <li key={`${line.productId}-${index}`}>{line.designation} — {line.quantity}</li>
              ))}
            </ul>
          )}

          <button type="submit" disabled={isSubmitting} className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50">
            {labels.actions.create}
          </button>
          {formError && <p className="text-sm text-red-600">{formError}</p>}
        </form>
      </PermissionGate>

      {actionError && <p className="text-sm text-red-600">{actionError}</p>}
      {isLoading && <p className="text-sm text-gray-500">{labels.states.loading}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!isLoading && !error && (
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">N° Commande</th>
              <th className="px-3 py-2">Client</th>
              <th className="px-3 py-2">Date</th>
              <th className="px-3 py-2">Total TTC</th>
              <th className="px-3 py-2">Lot(s)</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {orders.length === 0 && (
              <tr>
                <td colSpan={7} className="px-3 py-4 text-center text-gray-500">{labels.states.empty}</td>
              </tr>
            )}
            {orders.map((order) => (
              <tr key={order.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{order.orderNumber}</td>
                <td className="px-3 py-2">{order.customerName}</td>
                <td className="px-3 py-2">{formatDateFr(order.orderDate)}</td>
                <td className="px-3 py-2">{formatCfa(Number(order.totalTtc))}</td>
                <td className="px-3 py-2">{order.lines.map((l) => l.allocatedInternalLotNumber).filter(Boolean).join(', ') || '—'}</td>
                <td className="px-3 py-2">
                  <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{STATUS_LABELS[order.status] ?? order.status}</span>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="flex justify-end gap-3">
                    {order.status === 'Brouillon' && (
                      <PermissionGate permission="Sales.Create">
                        <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/sale-orders/${order.id}/confirm`))} className="text-gray-700 hover:underline">
                          Confirmer
                        </button>
                      </PermissionGate>
                    )}
                    {order.status === 'Confirmee' && (
                      <PermissionGate permission="Sales.Deliver">
                        <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/sale-orders/${order.id}/deliver`))} className="text-gray-700 hover:underline">
                          Livrer
                        </button>
                      </PermissionGate>
                    )}
                    {order.status === 'Livree' && (
                      <PermissionGate permission="Sales.Invoice">
                        <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/sale-orders/${order.id}/invoice`))} className="text-gray-700 hover:underline">
                          Facturer
                        </button>
                      </PermissionGate>
                    )}
                    {order.status === 'Facturee' && (
                      <a href={`${apiClient.defaults.baseURL}/api/sale-orders/${order.id}/invoice/pdf`} target="_blank" rel="noreferrer" className="text-gray-700 hover:underline">
                        PDF
                      </a>
                    )}
                    {(order.status === 'Brouillon' || order.status === 'Confirmee') && (
                      <PermissionGate permission="Sales.Create">
                        <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/sale-orders/${order.id}/cancel`))} className="text-red-600 hover:underline">
                          Annuler
                        </button>
                      </PermissionGate>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
