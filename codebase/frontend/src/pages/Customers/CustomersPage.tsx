import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatCfa } from '../../i18n/format'
import { PermissionGate } from '../../routes/PermissionGate'

interface Customer {
  id: string
  name: string
  type: string
  paymentDays: number
  creditLimit: string | null
  isActive: boolean
}

const CUSTOMER_TYPES = ['Répartiteur', 'Hôpital', 'Clinique', 'Pharmacie', 'CentraleAchat', 'Autre']

/** Clients (US1 — contracts/products-referentiel.md, FR-008/FR-009). */
export function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [name, setName] = useState('')
  const [type, setType] = useState(CUSTOMER_TYPES[0])
  const [paymentDays, setPaymentDays] = useState('30')
  const [creditLimit, setCreditLimit] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [balances, setBalances] = useState<Record<string, string>>({})

  const loadCustomers = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await apiClient.get<Customer[]>('/api/customers')
      setCustomers(response.data)
    } catch {
      setError(labels.states.error)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadCustomers()
  }, [loadCustomers])

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)
    setIsSubmitting(true)
    try {
      await apiClient.post('/api/customers', {
        name,
        type,
        paymentDays: Number.parseInt(paymentDays, 10),
        creditLimit: creditLimit || null,
      })
      setName('')
      setCreditLimit('')
      await loadCustomers()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        setFormError('Un client avec ce nom existe déjà.')
      } else {
        setFormError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleDeactivate(id: string) {
    await apiClient.delete(`/api/customers/${id}`)
    await loadCustomers()
  }

  async function loadOutstandingBalance(id: string) {
    const response = await apiClient.get<{ outstandingBalance: string }>(`/api/customers/${id}/outstanding-balance`)
    setBalances((prev) => ({ ...prev, [id]: response.data.outstandingBalance }))
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.customers}</h1>

      <PermissionGate permission="Customers.Create">
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-5">
          <input
            required
            placeholder="Nom"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2"
          />
          <select value={type} onChange={(e) => setType(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
            {CUSTOMER_TYPES.map((customerType) => (
              <option key={customerType} value={customerType}>
                {customerType}
              </option>
            ))}
          </select>
          <input
            type="number"
            min={0}
            placeholder="Délai paiement (j)"
            value={paymentDays}
            onChange={(e) => setPaymentDays(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          />
          <input
            placeholder="Plafond encours (XOF)"
            value={creditLimit}
            onChange={(e) => setCreditLimit(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          />
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50 sm:col-span-5"
          >
            {labels.actions.create}
          </button>
          {formError && <p className="text-sm text-red-600 sm:col-span-5">{formError}</p>}
        </form>
      </PermissionGate>

      {isLoading && <p className="text-sm text-gray-500">{labels.states.loading}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!isLoading && !error && (
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">Nom</th>
              <th className="px-3 py-2">Type</th>
              <th className="px-3 py-2">Délai (j)</th>
              <th className="px-3 py-2">Plafond</th>
              <th className="px-3 py-2">Encours</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {customers.length === 0 && (
              <tr>
                <td colSpan={7} className="px-3 py-4 text-center text-gray-500">
                  {labels.states.empty}
                </td>
              </tr>
            )}
            {customers.map((customer) => (
              <tr key={customer.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{customer.name}</td>
                <td className="px-3 py-2">{customer.type}</td>
                <td className="px-3 py-2">{customer.paymentDays}</td>
                <td className="px-3 py-2">{customer.creditLimit ? formatCfa(Number(customer.creditLimit)) : '—'}</td>
                <td className="px-3 py-2">
                  {balances[customer.id] ? (
                    formatCfa(Number(balances[customer.id]))
                  ) : (
                    <button type="button" onClick={() => void loadOutstandingBalance(customer.id)} className="text-gray-500 hover:underline">
                      Voir
                    </button>
                  )}
                </td>
                <td className="px-3 py-2">{customer.isActive ? 'Actif' : 'Inactif'}</td>
                <td className="px-3 py-2 text-right">
                  <PermissionGate permission="Customers.Delete">
                    {customer.isActive && (
                      <button
                        type="button"
                        onClick={() => void handleDeactivate(customer.id)}
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
