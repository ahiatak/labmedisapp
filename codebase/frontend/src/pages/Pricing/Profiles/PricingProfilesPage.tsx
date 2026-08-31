import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../../services/apiClient'
import { labels } from '../../../i18n/labels'

interface Lookup {
  id: string
  name: string
}

interface PricingProfile {
  id: string
  name: string
  categoryId: string | null
  transportMode: string
  commissionCoeff: string
  freightCoeff: string
  transitCoeff: string
  transferFeeCoeff: string
  targetMarginCoeff: string
  isActive: boolean
}

const TRANSPORT_MODES = ['Maritime', 'Aerien', 'Express', 'Terrestre']

/** Gestion des Profils de Pricing (US6 — Admin/Direction uniquement, FR-052). */
export function PricingProfilesPage() {
  const [profiles, setProfiles] = useState<PricingProfile[]>([])
  const [categories, setCategories] = useState<Lookup[]>([])

  const [name, setName] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [transportMode, setTransportMode] = useState(TRANSPORT_MODES[0])
  const [commissionCoeff, setCommissionCoeff] = useState('1')
  const [freightCoeff, setFreightCoeff] = useState('1')
  const [transitCoeff, setTransitCoeff] = useState('1')
  const [transferFeeCoeff, setTransferFeeCoeff] = useState('1')
  const [targetMarginCoeff, setTargetMarginCoeff] = useState('1.3')

  const loadProfiles = useCallback(async () => {
    const response = await apiClient.get<PricingProfile[]>('/api/pricing/profiles')
    setProfiles(response.data)
  }, [])

  useEffect(() => {
    void loadProfiles()
    apiClient.get<Lookup[]>('/api/referentiel/categories').then((r) => setCategories(r.data)).catch(() => setCategories([]))
  }, [loadProfiles])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    await apiClient.post('/api/pricing/profiles', {
      name,
      categoryId: categoryId || null,
      transportMode,
      commissionCoeff,
      freightCoeff,
      transitCoeff,
      transferFeeCoeff,
      targetMarginCoeff,
    })
    setName('')
    await loadProfiles()
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.pricing} — Profils</h1>

      <form onSubmit={(e) => void handleSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
        <input required placeholder="Nom du profil" value={name} onChange={(e) => setName(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2" />
        <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
          <option value="">Toutes catégories</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <select value={transportMode} onChange={(e) => setTransportMode(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
          {TRANSPORT_MODES.map((mode) => (
            <option key={mode} value={mode}>{mode}</option>
          ))}
        </select>
        <input placeholder="Coeff. Commission" value={commissionCoeff} onChange={(e) => setCommissionCoeff(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <input placeholder="Coeff. Fret" value={freightCoeff} onChange={(e) => setFreightCoeff(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <input placeholder="Coeff. Transit" value={transitCoeff} onChange={(e) => setTransitCoeff(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <input placeholder="Coeff. Transfert" value={transferFeeCoeff} onChange={(e) => setTransferFeeCoeff(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <input placeholder="Coeff. Marge cible" value={targetMarginCoeff} onChange={(e) => setTargetMarginCoeff(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
        <button type="submit" className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 sm:col-span-4">
          {labels.actions.create}
        </button>
      </form>

      <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
        <thead className="bg-gray-100 text-left">
          <tr>
            <th className="px-3 py-2">Nom</th>
            <th className="px-3 py-2">Transport</th>
            <th className="px-3 py-2">Commission</th>
            <th className="px-3 py-2">Fret</th>
            <th className="px-3 py-2">Transit</th>
            <th className="px-3 py-2">Transfert</th>
            <th className="px-3 py-2">Marge</th>
          </tr>
        </thead>
        <tbody>
          {profiles.length === 0 && (
            <tr>
              <td colSpan={7} className="px-3 py-4 text-center text-gray-500">{labels.states.empty}</td>
            </tr>
          )}
          {profiles.map((profile) => (
            <tr key={profile.id} className="border-t border-gray-100">
              <td className="px-3 py-2">{profile.name}</td>
              <td className="px-3 py-2">{profile.transportMode}</td>
              <td className="px-3 py-2">{profile.commissionCoeff}</td>
              <td className="px-3 py-2">{profile.freightCoeff}</td>
              <td className="px-3 py-2">{profile.transitCoeff}</td>
              <td className="px-3 py-2">{profile.transferFeeCoeff}</td>
              <td className="px-3 py-2">{profile.targetMarginCoeff}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
