import axios from 'axios'
import { useCallback, useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatDateFr } from '../../i18n/format'
import { PermissionGate } from '../../routes/PermissionGate'

interface ReorderSuggestion {
  id: string
  productDesignation: string | null
  suggestionDate: string
  orderDeadline: string
  suggestedQuantity: number
  status: string
}

const STATUS_LABELS: Record<string, string> = {
  EnAttente: 'En attente',
  Converti: 'Convertie',
  Rejete: 'Rejetée',
}

/** Suggestions de Réapprovisionnement (US10 — contracts/forecast.md, FR-063 à FR-067). */
export function ForecastPage() {
  const [suggestions, setSuggestions] = useState<ReorderSuggestion[]>([])
  const [error, setError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const loadSuggestions = useCallback(async () => {
    setError(null)
    try {
      const response = await apiClient.get<ReorderSuggestion[]>('/api/forecast/suggestions', { params: { status: 'EnAttente' } })
      setSuggestions(response.data)
    } catch {
      setError(labels.states.error)
    }
  }, [])

  useEffect(() => {
    void loadSuggestions()
  }, [loadSuggestions])

  async function runAction(action: () => Promise<unknown>) {
    setActionError(null)
    try {
      await action()
      await loadSuggestions()
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
      <h1 className="text-xl font-semibold">{labels.nav.forecast}</h1>
      {error && <p className="text-sm text-red-600">{error}</p>}
      {actionError && <p className="text-sm text-red-600">{actionError}</p>}

      <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
        <thead className="bg-gray-100 text-left">
          <tr>
            <th className="px-3 py-2">Produit</th>
            <th className="px-3 py-2">Date suggestion</th>
            <th className="px-3 py-2">Date limite commande</th>
            <th className="px-3 py-2">Quantité suggérée</th>
            <th className="px-3 py-2">Statut</th>
            <th className="px-3 py-2" />
          </tr>
        </thead>
        <tbody>
          {suggestions.length === 0 && (
            <tr>
              <td colSpan={6} className="px-3 py-4 text-center text-gray-500">Aucune suggestion en attente.</td>
            </tr>
          )}
          {suggestions.map((suggestion) => (
            <tr key={suggestion.id} className="border-t border-gray-100">
              <td className="px-3 py-2">{suggestion.productDesignation}</td>
              <td className="px-3 py-2">{formatDateFr(suggestion.suggestionDate)}</td>
              <td className="px-3 py-2">{formatDateFr(suggestion.orderDeadline)}</td>
              <td className="px-3 py-2">{suggestion.suggestedQuantity}</td>
              <td className="px-3 py-2">
                <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{STATUS_LABELS[suggestion.status] ?? suggestion.status}</span>
              </td>
              <td className="px-3 py-2 text-right">
                <PermissionGate permission="Forecast.Convert">
                  <div className="flex justify-end gap-3">
                    <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/forecast/suggestions/${suggestion.id}/convert`))} className="text-gray-700 hover:underline">
                      Convertir en commande
                    </button>
                    <button type="button" onClick={() => void runAction(() => apiClient.post(`/api/forecast/suggestions/${suggestion.id}/reject`))} className="text-red-600 hover:underline">
                      Rejeter
                    </button>
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
