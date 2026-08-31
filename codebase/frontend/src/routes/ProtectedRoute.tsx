import type { ReactElement } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'

interface ProtectedRouteProps {
  children: ReactElement
}

/** Redirects to /login when no authenticated user is present (FR-012). */
export function ProtectedRoute({ children }: ProtectedRouteProps): ReactElement | null {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return null
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  return children
}
