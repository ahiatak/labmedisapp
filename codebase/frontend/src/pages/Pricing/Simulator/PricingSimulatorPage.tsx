import axios from 'axios'
import { useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../../services/apiClient'
import { labels } from '../../../i18n/labels'
import { formatCfa } from '../../../i18n/format'

interface Lookup {
  id: string
  name: string
}

interface SimulationResult {
  purchasePriceCfa: string
  landingCostCfa: string
  targetPriceHtCfa: string
  targetPriceTtcCfa: string
}

/** Simulateur de Pricing (US6 — contracts/pricing.md, RG-004). */
export function PricingSimulatorPage() {
  const [profiles, setProfiles] = useState<Lookup[]>([])
  const [pricingProfileId, setPricingProfileId] = useState('')
  const [purchasePriceForeign, setPurchasePriceForeign] = useState('')
  const [exchangeRate, setExchangeRate] = useState('655.957')
  const [vatRate, setVatRate] = useState('0.18')
  const [result, setResult] = useState<SimulationResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    apiClient.get<Lookup[]>('/api/pricing/profiles').then((r) => setProfiles(r.data)).catch(() => setProfiles([]))
  }, [])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      const response = await apiClient.post<SimulationResult>('/api/pricing/simulate', {
        purchasePriceForeign,
        exchangeRate,
        pricingProfileId,
        vatRate,
      })
      setResult(response.data)
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
      <h1 className="text-xl font-semibold">{labels.nav.pricing} — Simulateur</h1>

      <form onSubmit={(e) => void handleSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
        <select required value={pricingProfileId} onChange={(e) => setPricingProfileId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
          <option value="">Profil de pricing…</option>
          {profiles.map((p) => (
            <option key={p.id} value={p.id}>{p.name}</option>
          ))}
        </select>
        <input required placeholder="Prix d'achat (devise)" value={purchasePriceForeign} onChange={(e) => setPurchasePriceForeign(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <input required placeholder="Taux de change" value={exchangeRate} onChange={(e) => setExchangeRate(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <input required placeholder="Taux TVA" value={vatRate} onChange={(e) => setVatRate(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <button type="submit" disabled={isSubmitting} className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50 sm:col-span-4">
          Simuler
        </button>
        {error && <p className="text-sm text-red-600 sm:col-span-4">{error}</p>}
      </form>

      {result && (
        <div className="grid grid-cols-1 gap-4 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
          <div>
            <p className="text-xs text-gray-500">Prix d'achat CFA</p>
            <p className="text-lg font-semibold">{formatCfa(Number(result.purchasePriceCfa))}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Prix de revient (PR)</p>
            <p className="text-lg font-semibold">{formatCfa(Number(result.landingCostCfa))}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">PV HT calculé</p>
            <p className="text-lg font-semibold">{formatCfa(Number(result.targetPriceHtCfa))}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">PV TTC calculé</p>
            <p className="text-lg font-semibold">{formatCfa(Number(result.targetPriceTtcCfa))}</p>
          </div>
        </div>
      )}
    </div>
  )
}
