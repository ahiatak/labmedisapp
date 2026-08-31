import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatDateFr } from '../../i18n/format'
import { PermissionGate } from '../../routes/PermissionGate'
import { useAuth } from '../../routes/AuthContext'

interface StockLot {
  id: string
  productDesignation: string | null
  internalLotNumber: string
  expiryDate: string
  remainingQuantity: number
  qualityStatus: string
  quarantineReason: string | null
}

const QUARANTINE_STATUSES = ['EnQuarantaine', 'EnReception', 'EnAttenteLiberation']

/** Contrôle Qualité (US5 — contracts/stock.md, FR-040 à FR-042). */
export function QualityPage() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('Admin') ?? false
  const [lots, setLots] = useState<StockLot[]>([])
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const loadLots = useCallback(async () => {
    setError(null)
    try {
      const response = await apiClient.get<StockLot[]>('/api/stock/lots')
      setLots(response.data.filter((lot) => QUARANTINE_STATUSES.includes(lot.qualityStatus)))
    } catch {
      setError(labels.states.error)
    }
  }, [])

  useEffect(() => {
    void loadLots()
  }, [loadLots])

  async function runAction(action: () => Promise<unknown>) {
    setActionError(null)
    try {
      await action()
      await loadLots()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setActionError(err.response.data.message)
      } else {
        setActionError(labels.states.error)
      }
    }
  }

  async function handleRelease(id: string) {
    await runAction(() => apiClient.post(`/api/stock/lots/${id}/release`))
  }

  async function handleReject(id: string) {
    const reason = window.prompt('Motif de non-conformité (obligatoire) :')
    if (!reason) {
      return
    }
    await runAction(() => apiClient.post(`/api/stock/lots/${id}/non-conforme`, { reason }))
  }

  async function handleDestroy(id: string) {
    const ref = window.prompt('Référence du document de destruction (obligatoire) :')
    if (!ref) {
      return
    }
    await runAction(() => apiClient.post(`/api/stock/lots/${id}/destroy`, { destructionDocumentRef: ref }))
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.quality}</h1>
      {error && <p className="text-sm text-red-600">{error}</p>}
      {actionError && <p className="text-sm text-red-600">{actionError}</p>}

      <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
        <thead className="bg-gray-100 text-left">
          <tr>
            <th className="px-3 py-2">Produit</th>
            <th className="px-3 py-2">N° lot</th>
            <th className="px-3 py-2">Péremption</th>
            <th className="px-3 py-2">Quantité</th>
            <th className="px-3 py-2">Statut</th>
            <th className="px-3 py-2">Motif</th>
            <th className="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          {lots.length === 0 && (
            <tr>
              <td colSpan={7} className="px-3 py-4 text-center text-gray-500">Aucun lot en attente de décision qualité.</td>
            </tr>
          )}
          {lots.map((lot) => (
            <tr key={lot.id} className="border-t border-gray-100">
              <td className="px-3 py-2">{lot.productDesignation}</td>
              <td className="px-3 py-2">{lot.internalLotNumber}</td>
              <td className="px-3 py-2">{formatDateFr(lot.expiryDate)}</td>
              <td className="px-3 py-2">{lot.remainingQuantity}</td>
              <td className="px-3 py-2">
                <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{lot.qualityStatus}</span>
              </td>
              <td className="px-3 py-2">{lot.quarantineReason ?? '—'}</td>
              <td className="px-3 py-2 text-right">
                <PermissionGate permission="Quality.Release">
                  <div className="flex justify-end gap-3">
                    <button type="button" onClick={() => void handleRelease(lot.id)} className="text-green-700 hover:underline">
                      Libérer
                    </button>
                    <button type="button" onClick={() => void handleReject(lot.id)} className="text-orange-700 hover:underline">
                      Rejeter
                    </button>
                    {isAdmin && (
                      <button type="button" onClick={() => void handleDestroy(lot.id)} className="text-red-600 hover:underline">
                        Détruire
                      </button>
                    )}
                  </div>
                </PermissionGate>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
