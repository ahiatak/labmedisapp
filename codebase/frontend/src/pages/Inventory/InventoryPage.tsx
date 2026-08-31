import axios from 'axios'
import { useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { PermissionGate } from '../../routes/PermissionGate'

interface InventoryCount {
  id: string
  stockLotId: string
  internalLotNumber: string | null
  productDesignation: string | null
  systemQuantity: number
  countedQuantity: number | null
  variance: number | null
}

interface InventorySession {
  id: string
  sessionNumber: string
  perimeter: string
  status: string
  counts: InventoryCount[]
}

/** Inventaire (US9 — contracts/stock.md, FR-044). */
export function InventoryPage() {
  const [perimeter, setPerimeter] = useState('')
  const [session, setSession] = useState<InventorySession | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [adjustmentReason, setAdjustmentReason] = useState('')

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    try {
      const response = await apiClient.post<InventorySession>('/api/stock/inventory-sessions', { perimeter })
      setSession(response.data)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError(labels.states.error)
      }
    }
  }

  async function updateCount(stockLotId: string, countedQuantity: string) {
    if (!session) {
      return
    }
    const response = await apiClient.post<InventoryCount>(`/api/stock/inventory-sessions/${session.id}/counts`, {
      stockLotId,
      countedQuantity: Number.parseInt(countedQuantity, 10) || 0,
    })
    setSession((prev) => (prev ? { ...prev, counts: prev.counts.map((c) => (c.stockLotId === stockLotId ? response.data : c)) } : prev))
  }

  async function handleValidate() {
    if (!session) {
      return
    }
    setError(null)
    try {
      const response = await apiClient.post<InventorySession>(`/api/stock/inventory-sessions/${session.id}/validate`, { adjustmentReason: adjustmentReason || null })
      setSession(response.data)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      }
    }
  }

  const hasVariance = session?.counts.some((c) => c.variance !== null && c.variance !== 0) ?? false

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.inventory}</h1>

      {!session && (
        <PermissionGate permission="Inventory.Manage">
          <form onSubmit={(e) => void handleCreateSubmit(e)} className="flex gap-2 rounded border border-gray-200 bg-white p-4">
            <input
              required
              placeholder="Périmètre (code d'emplacement ou préfixe de zone)"
              value={perimeter}
              onChange={(e) => setPerimeter(e.target.value)}
              className="flex-1 rounded border border-gray-300 px-3 py-2 text-sm"
            />
            <button type="submit" className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800">
              Créer la session (gèle le périmètre)
            </button>
          </form>
        </PermissionGate>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}

      {session && (
        <div className="space-y-4 rounded border border-gray-200 bg-white p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">{session.sessionNumber} — {session.perimeter}</p>
              <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{session.status}</span>
            </div>
            <button type="button" onClick={() => setSession(null)} className="text-sm text-gray-500 hover:underline">
              Nouvelle session
            </button>
          </div>

          <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 text-sm">
            <thead className="bg-gray-100 text-left">
              <tr>
                <th className="px-3 py-2">Produit</th>
                <th className="px-3 py-2">N° lot</th>
                <th className="px-3 py-2">Qté système</th>
                <th className="px-3 py-2">Qté comptée</th>
                <th className="px-3 py-2">Écart</th>
              </tr>
            </thead>
            <tbody>
              {session.counts.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-gray-500">Aucun lot dans ce périmètre.</td>
                </tr>
              )}
              {session.counts.map((count) => (
                <tr key={count.id} className="border-t border-gray-100">
                  <td className="px-3 py-2">{count.productDesignation}</td>
                  <td className="px-3 py-2">{count.internalLotNumber}</td>
                  <td className="px-3 py-2">{count.systemQuantity}</td>
                  <td className="px-3 py-2">
                    <input
                      type="number"
                      defaultValue={count.countedQuantity ?? ''}
                      onBlur={(e) => void updateCount(count.stockLotId, e.target.value)}
                      className="w-24 rounded border border-gray-300 px-2 py-1 text-sm"
                    />
                  </td>
                  <td className={`px-3 py-2 ${count.variance ? 'font-semibold text-orange-600' : ''}`}>{count.variance ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>

          {session.status === 'EnComptage' && (
            <PermissionGate permission="Inventory.Validate">
              <div className="flex items-center gap-2">
                {hasVariance && (
                  <input
                    placeholder="Motif d'ajustement (obligatoire, écarts constatés)"
                    value={adjustmentReason}
                    onChange={(e) => setAdjustmentReason(e.target.value)}
                    className="flex-1 rounded border border-gray-300 px-3 py-2 text-sm"
                  />
                )}
                <button type="button" onClick={() => void handleValidate()} className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800">
                  Valider et clôturer
                </button>
              </div>
            </PermissionGate>
          )}
        </div>
      )}
    </div>
  )
}
