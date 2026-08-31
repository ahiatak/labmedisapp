import axios from 'axios'
import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { apiClient } from '../../services/apiClient'
import { useAuth } from '../../routes/AuthContext'
import { labels } from '../../i18n/labels'

interface LoginResponse {
  accessToken: string
  refreshToken: string
}

/** Login screen (contracts/auth.md — POST /api/auth/login). FR-012 à FR-014. */
export function LoginPage() {
  const { isAuthenticated, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  if (isAuthenticated) {
    const redirectTo = (location.state as { from?: string } | null)?.from ?? '/'
    return <Navigate to={redirectTo} replace />
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const response = await apiClient.post<LoginResponse>('/api/auth/login', { email, password })
      await login(response.data.accessToken, response.data.refreshToken)
      navigate('/', { replace: true })
    } catch (err) {
      if (axios.isAxiosError(err)) {
        if (err.response?.status === 423) {
          setError('Ce compte est temporairement verrouillé suite à plusieurs échecs de connexion.')
        } else if (err.response?.status === 401) {
          setError('Identifiants invalides.')
        } else {
          setError(labels.states.error)
        }
      } else {
        setError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <form
        onSubmit={(event) => void handleSubmit(event)}
        className="w-full max-w-sm rounded-lg border border-gray-200 bg-white p-8 shadow-sm"
      >
        <h1 className="mb-6 text-center text-xl font-semibold text-gray-900">{labels.appName}</h1>

        <label className="mb-1 block text-sm font-medium text-gray-700" htmlFor="email">
          Email
        </label>
        <input
          id="email"
          type="email"
          required
          autoComplete="username"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          className="mb-4 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        <label className="mb-1 block text-sm font-medium text-gray-700" htmlFor="password">
          Mot de passe
        </label>
        <input
          id="password"
          type="password"
          required
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          className="mb-4 w-full rounded border border-gray-300 px-3 py-2 text-sm"
        />

        {error && <p className="mb-4 text-sm text-red-600">{error}</p>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50"
        >
          {isSubmitting ? labels.states.loading : 'Se connecter'}
        </button>
      </form>
    </div>
  )
}
