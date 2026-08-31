import { createContext, useContext } from 'react'

export interface AuthUser {
  id: string
  firstName: string
  lastName: string
  roles: string[]
  permissions: string[]
}

export interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  hasPermission: (permission: string) => boolean
  login: (accessToken: string, refreshToken: string) => Promise<void>
  logout: () => Promise<void>
}

/**
 * Placeholder default — replaced by the real provider wired to POST /api/auth/login and
 * GET /api/auth/me (see contracts/auth.md) when US2 (Authentification, Rôles et Permissions)
 * is implemented. Kept here so routing/permission plumbing can be built and typed now.
 */
export const AuthContext = createContext<AuthContextValue>({
  user: null,
  isAuthenticated: false,
  isLoading: false,
  hasPermission: () => false,
  login: async () => {
    throw new Error('AuthProvider not yet implemented (see user story US2).')
  },
  logout: async () => {},
})

export function useAuth(): AuthContextValue {
  return useContext(AuthContext)
}
