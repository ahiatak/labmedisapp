import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatDateFr } from '../../i18n/format'
import { PermissionGate } from '../../routes/PermissionGate'

interface Lookup {
  id: string
  name: string
}

interface Packaging {
  id: string
  packagingType: string
  quantityPerPackage: number
}

interface OrderLine {
  productId: string
  productDesignation?: string
  quantity: number
  unitPriceForeign: string
  packagingId: string
}

interface PurchaseOrder {
  id: string
  orderNumber: string
  supplierName: string | null
  currencyCode: string | null
  status: string
  orderDate: string
  totalForeign: string
  totalCfa: string
  cancellationReason: string | null
}

const STATUS_LABELS: Record<string, string> = {
  Brouillon: 'Brouillon',
  EnAttenteValidation: 'En attente de validation',
  Validee: 'Validée',
  Envoyee: 'Envoyée',
  EnFabrication: 'En fabrication',
  PreteAExpedier: 'Prête à expédier',
  Expediee: 'Expédiée',
  EnTransit: 'En transit',
  PartiellementRecue: 'Partiellement reçue',
  Recue: 'Reçue',
  Close: 'Clôturée',
  Annulee: 'Annulée',
}

const TRANSPORT_MODES = ['Maritime', 'Aerien', 'Express', 'Terrestre']

/** Commandes d'Achat (US3 — contracts/purchase-orders.md, FR-020 à FR-024). */
export function PurchaseOrdersPage() {
  const [orders, setOrders] = useState<PurchaseOrder[]>([])
  const [suppliers, setSuppliers] = useState<Lookup[]>([])
  const [currencies, setCurrencies] = useState<Lookup[]>([])
  const [products, setProducts] = useState<Lookup[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const [supplierId, setSupplierId] = useState('')
  const [currencyId, setCurrencyId] = useState('')
  const [transportMode, setTransportMode] = useState(TRANSPORT_MODES[0])
  const [lines, setLines] = useState<OrderLine[]>([])
  const [lineProductId, setLineProductId] = useState('')
  const [lineQuantity, setLineQuantity] = useState('1')
  const [lineUnitPrice, setLineUnitPrice] = useState('0')
  const [linePackagings, setLinePackagings] = useState<Packaging[]>([])
  const [linePackagingId, setLinePackagingId] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const loadOrders = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await apiClient.get<PurchaseOrder[]>('/api/purchase-orders')
      setOrders(response.data)
    } catch {
      setError(labels.states.error)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadOrders()
    apiClient.get<{ id: string; name: string }[]>('/api/suppliers').then((r) => setSuppliers(r.data)).catch(() => setSuppliers([]))
    apiClient.get<Lookup[]>('/api/currencies').then((r) => setCurrencies(r.data)).catch(() => setCurrencies([]))
    apiClient
      .get<{ items: Lookup[] }>('/api/products', { params: { selectableOnly: true, pageSize: 200 } })
      .then((r) => setProducts(r.data.items))
      .catch(() => setProducts([]))
  }, [loadOrders])

  useEffect(() => {
    if (!lineProductId) {
      setLinePackagings([])
      setLinePackagingId('')
      return
    }

    apiClient
      .get<{ packagings: Packaging[] }>(`/api/products/${lineProductId}`)
      .then((r) => {
        setLinePackagings(r.data.packagings)
        setLinePackagingId(r.data.packagings[0]?.id ?? '')
      })
      .catch(() => setLinePackagings([]))
  }, [lineProductId])

  function addLine() {
    if (!lineProductId || !linePackagingId) {
      setFormError('Sélectionnez un produit avec un conditionnement configuré.')
      return
    }

    setFormError(null)
    setLines((prev) => [
      ...prev,
      {
        productId: lineProductId,
        productDesignation: products.find((p) => p.id === lineProductId)?.name,
        quantity: Number.parseInt(lineQuantity, 10) || 1,
        unitPriceForeign: lineUnitPrice,
        packagingId: linePackagingId,
      },
    ])
    setLineProductId('')
    setLineQuantity('1')
    setLineUnitPrice('0')
  }

  function removeLine(index: number) {
    setLines((prev) => prev.filter((_, i) => i !== index))
  }

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    if (!supplierId || !currencyId || lines.length === 0) {
      setFormError('Fournisseur, devise et au moins une ligne sont requis.')
      return
    }

    setIsSubmitting(true)
    try {
      await apiClient.post('/api/purchase-orders', {
        supplierId,
        currencyId,
        transportMode,
        lines: lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          unitPriceForeign: l.unitPriceForeign,
          packagingId: l.packagingId,
        })),
      })
      setLines([])
      setSupplierId('')
      setCurrencyId('')
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

  async function handleCancel(id: string) {
    const reason = window.prompt("Motif d'annulation (obligatoire) :")
    if (reason === null) {
      return
    }
    await runAction(() => apiClient.post(`/api/purchase-orders/${id}/cancel`, { reason }))
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.purchaseOrders}</h1>

      <PermissionGate permission="PurchaseOrders.Create">
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="space-y-3 rounded border border-gray-200 bg-white p-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <select required value={supplierId} onChange={(e) => setSupplierId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">Fournisseur…</option>
              {suppliers.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <select required value={currencyId} onChange={(e) => setCurrencyId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">Devise…</option>
              {currencies.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            <select value={transportMode} onChange={(e) => setTransportMode(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
              {TRANSPORT_MODES.map((mode) => (
                <option key={mode} value={mode}>{mode}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-1 gap-3 border-t border-gray-100 pt-3 sm:grid-cols-5">
            <select value={lineProductId} onChange={(e) => setLineProductId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2">
              <option value="">Produit…</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
            <input type="number" min={1} value={lineQuantity} onChange={(e) => setLineQuantity(e.target.value)} placeholder="Quantité" className="rounded border border-gray-300 px-3 py-2 text-sm" />
            <input value={lineUnitPrice} onChange={(e) => setLineUnitPrice(e.target.value)} placeholder="Prix unitaire (devise)" className="rounded border border-gray-300 px-3 py-2 text-sm" />
            <select value={linePackagingId} onChange={(e) => setLinePackagingId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" disabled={linePackagings.length === 0}>
              <option value="">Conditionnement…</option>
              {linePackagings.map((p) => (
                <option key={p.id} value={p.id}>{p.packagingType} ({p.quantityPerPackage})</option>
              ))}
            </select>
            <button type="button" onClick={addLine} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100 sm:col-span-5">
              Ajouter la ligne
            </button>
          </div>

          {lines.length > 0 && (
            <ul className="divide-y divide-gray-100 text-sm">
              {lines.map((line, index) => (
                <li key={`${line.productId}-${index}`} className="flex items-center justify-between py-1">
                  <span>{line.productDesignation} — {line.quantity} × {line.unitPriceForeign}</span>
                  <button type="button" onClick={() => removeLine(index)} className="text-red-600 hover:underline">Retirer</button>
                </li>
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
              <th className="px-3 py-2">Fournisseur</th>
              <th className="px-3 py-2">Date</th>
              <th className="px-3 py-2">Total</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {orders.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-4 text-center text-gray-500">{labels.states.empty}</td>
              </tr>
            )}
            {orders.map((order) => (
              <tr key={order.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{order.orderNumber}</td>
                <td className="px-3 py-2">{order.supplierName}</td>
                <td className="px-3 py-2">{formatDateFr(order.orderDate)}</td>
                <td className="px-3 py-2">{order.totalForeign} {order.currencyCode}</td>
                <td className="px-3 py-2">
                  <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{STATUS_LABELS[order.status] ?? order.status}</span>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="flex justify-end gap-3">
                    {order.status === 'Brouillon' && (
                      <PermissionGate permission="PurchaseOrders.Create">
                        <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/purchase-orders/${order.id}/submit`))} className="text-gray-700 hover:underline">
                          Soumettre
                        </button>
                      </PermissionGate>
                    )}
                    {order.status === 'EnAttenteValidation' && (
                      <PermissionGate permission="PurchaseOrders.Validate">
                        <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/purchase-orders/${order.id}/validate`))} className="text-gray-700 hover:underline">
                          Valider
                        </button>
                      </PermissionGate>
                    )}
                    {order.status !== 'Annulee' && order.status !== 'Recue' && order.status !== 'Close' && (
                      <PermissionGate permission="PurchaseOrders.Create">
                        <button type="button" onClick={() => void handleCancel(order.id)} className="text-red-600 hover:underline">
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
