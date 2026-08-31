import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../../services/apiClient'
import { labels } from '../../../i18n/labels'

interface UserRow {
  id: string
  email: string
  firstName: string
  lastName: string
  isActive: boolean
  roles: string[]
}

interface RoleRow {
  id: string
  name: string
  isSystem: boolean
  permissions: string[]
}

/** Administration Utilisateurs/Rôles (US2 — FR-012 à FR-019). */
export function AdminUsersPage() {
  const [users, setUsers] = useState<UserRow[]>([])
  const [roles, setRoles] = useState<RoleRow[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [email, setEmail] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [password, setPassword] = useState('')
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const loadAll = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const [usersResponse, rolesResponse] = await Promise.all([
        apiClient.get<UserRow[]>('/api/users'),
        apiClient.get<RoleRow[]>('/api/roles'),
      ])
      setUsers(usersResponse.data)
      setRoles(rolesResponse.data)
    } catch {
      setError(labels.states.error)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadAll()
  }, [loadAll])

  function toggleRole(roleName: string) {
    setSelectedRoles((prev) => (prev.includes(roleName) ? prev.filter((r) => r !== roleName) : [...prev, roleName]))
  }

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)
    setIsSubmitting(true)
    try {
      await apiClient.post('/api/users', { email, firstName, lastName, password, roles: selectedRoles })
      setEmail('')
      setFirstName('')
      setLastName('')
      setPassword('')
      setSelectedRoles([])
      await loadAll()
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

  async function handleDeactivate(id: string) {
    await apiClient.delete(`/api/users/${id}`)
    await loadAll()
  }

  if (isLoading) {
    return <p className="text-sm text-gray-500">{labels.states.loading}</p>
  }

  if (error) {
    return <p className="text-sm text-red-600">{error}</p>
  }

  return (
    <div className="space-y-8">
      <h1 className="text-xl font-semibold">{labels.nav.admin}</h1>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-gray-700">Nouvel utilisateur</h2>
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
          <input required type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
          <input required placeholder="Prénom" value={firstName} onChange={(e) => setFirstName(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
          <input required placeholder="Nom" value={lastName} onChange={(e) => setLastName(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
          <input required type="password" placeholder="Mot de passe temporaire" value={password} onChange={(e) => setPassword(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />

          <div className="flex flex-wrap gap-3 sm:col-span-4">
            {roles.map((role) => (
              <label key={role.id} className="flex items-center gap-1 text-sm">
                <input type="checkbox" checked={selectedRoles.includes(role.name)} onChange={() => toggleRole(role.name)} />
                {role.name}
              </label>
            ))}
          </div>

          <button type="submit" disabled={isSubmitting} className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50 sm:col-span-4">
            {labels.actions.create}
          </button>
          {formError && <p className="text-sm text-red-600 sm:col-span-4">{formError}</p>}
        </form>
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-gray-700">Utilisateurs</h2>
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">Nom</th>
              <th className="px-3 py-2">Email</th>
              <th className="px-3 py-2">Rôles</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{user.firstName} {user.lastName}</td>
                <td className="px-3 py-2">{user.email}</td>
                <td className="px-3 py-2">{user.roles.join(', ')}</td>
                <td className="px-3 py-2">{user.isActive ? 'Actif' : 'Inactif'}</td>
                <td className="px-3 py-2 text-right">
                  {user.isActive && (
                    <button type="button" onClick={() => void handleDeactivate(user.id)} className="text-sm text-red-600 hover:underline">
                      {labels.actions.delete}
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-gray-700">Rôles ({roles.length})</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {roles.map((role) => (
            <div key={role.id} className="rounded border border-gray-200 bg-white p-4">
              <p className="mb-2 font-medium">
                {role.name} {role.isSystem && <span className="text-xs text-gray-400">(système)</span>}
              </p>
              <p className="text-xs text-gray-500">{role.permissions.length} permission(s)</p>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
