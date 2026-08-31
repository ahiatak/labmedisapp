import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { PermissionGate } from '../../routes/PermissionGate'

interface Supplier {
  id: string
  name: string
  country: string
  defaultCurrencyCode: string | null
  isActive: boolean
}

interface Lookup {
  id: string
  name: string
}

/** Fournisseurs (US1 — contracts/products-referentiel.md, FR-007). */
export function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<Supplier[]>([])
  const [currencies, setCurrencies] = useState<Lookup[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [country, setCountry] = useState('')
  const [defaultCurrencyId, setDefaultCurrencyId] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const loadSuppliers = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await apiClient.get<Supplier[]>('/api/suppliers')
      setSuppliers(response.data)
    } catch {
      setError(labels.states.error)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadSuppliers()
    apiClient
      .get<Lookup[]>('/api/currencies')
      .then((response) => setCurrencies(response.data))
      .catch(() => setCurrencies([]))
  }, [loadSuppliers])

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    if (!defaultCurrencyId) {
      setFormError('Sélectionnez une devise.')
      return
    }

    setIsSubmitting(true)
    try {
      await apiClient.post('/api/suppliers', { name, country, defaultCurrencyId })
      setName('')
      setCountry('')
      await loadSuppliers()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        setFormError('Un fournisseur avec ce nom existe déjà.')
      } else {
        setFormError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleDeactivate(id: string) {
    await apiClient.delete(`/api/suppliers/${id}`)
    await loadSuppliers()
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.suppliers}</h1>

      <PermissionGate permission="Suppliers.Create">
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
          <input
            required
            placeholder="Nom"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2"
          />
          <input
            required
            placeholder="Pays"
            value={country}
            onChange={(e) => setCountry(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          />
          <select
            required
            value={defaultCurrencyId}
            onChange={(e) => setDefaultCurrencyId(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">Devise…</option>
            {currencies.map((currency) => (
              <option key={currency.id} value={currency.id}>
                {currency.name}
              </option>
            ))}
          </select>
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50"
          >
            {labels.actions.create}
          </button>
          {formError && <p className="text-sm text-red-600 sm:col-span-4">{formError}</p>}
        </form>
      </PermissionGate>

      {isLoading && <p className="text-sm text-gray-500">{labels.states.loading}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!isLoading && !error && (
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">Nom</th>
              <th className="px-3 py-2">Pays</th>
              <th className="px-3 py-2">Devise</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {suppliers.length === 0 && (
              <tr>
                <td colSpan={5} className="px-3 py-4 text-center text-gray-500">
                  {labels.states.empty}
                </td>
              </tr>
            )}
            {suppliers.map((supplier) => (
              <tr key={supplier.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{supplier.name}</td>
                <td className="px-3 py-2">{supplier.country}</td>
                <td className="px-3 py-2">{supplier.defaultCurrencyCode}</td>
                <td className="px-3 py-2">{supplier.isActive ? 'Actif' : 'Inactif'}</td>
                <td className="px-3 py-2 text-right">
                  <PermissionGate permission="Suppliers.Delete">
                    {supplier.isActive && (
                      <button
                        type="button"
                        onClick={() => void handleDeactivate(supplier.id)}
                        className="text-sm text-red-600 hover:underline"
                      >
                        {labels.actions.delete}
                      </button>
                    )}
                  </PermissionGate>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
