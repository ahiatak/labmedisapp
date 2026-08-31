import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { apiClient, clearTokens, getAccessToken, getRefreshToken, setTokens } from '../services/apiClient'
import { AuthContext, type AuthUser } from './AuthContext'

interface MeResponse {
  id: string
  firstName: string
  lastName: string
  userName: string
  roles: string[]
  permissions: string[]
}

function toAuthUser(me: MeResponse): AuthUser {
  return {
    id: me.id,
    firstName: me.firstName,
    lastName: me.lastName,
    roles: me.roles,
    permissions: me.permissions,
  }
}

/** Real auth provider — wires GET /api/auth/me and POST /api/auth/logout (contracts/auth.md, US2). */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function bootstrap() {
      if (!getAccessToken()) {
        setIsLoading(false)
        return
      }

      try {
        const response = await apiClient.get<MeResponse>('/api/auth/me')
        if (!cancelled) {
          setUser(toAuthUser(response.data))
        }
      } catch {
        clearTokens()
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    void bootstrap()
    return () => {
      cancelled = true
    }
  }, [])

  const login = useCallback(async (accessToken: string, refreshToken: string) => {
    setTokens(accessToken, refreshToken)
    const response = await apiClient.get<MeResponse>('/api/auth/me')
    setUser(toAuthUser(response.data))
  }, [])

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken()
    try {
      if (refreshToken) {
        await apiClient.post('/api/auth/logout', { refreshToken })
      }
    } catch {
      // Best-effort revocation — the client-side session is cleared regardless.
    } finally {
      clearTokens()
      setUser(null)
    }
  }, [])

  const hasPermission = useCallback(
    (permission: string) => user?.permissions.includes(permission) ?? false,
    [user]
  )

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      hasPermission,
      login,
      logout,
    }),
    [user, isLoading, hasPermission, login, logout]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
