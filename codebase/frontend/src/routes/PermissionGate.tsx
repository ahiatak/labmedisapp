import type { ReactNode } from 'react'
import { useAuth } from './AuthContext'

interface PermissionGateProps {
  /** Permission code, format Module.Action (e.g. "Quality.Release") — see FR-016/FR-019. */
  permission: string
  children: ReactNode
  fallback?: ReactNode
}

/** Renders its children only if the current user holds the required permission. */
export function PermissionGate({ permission, children, fallback = null }: PermissionGateProps) {
  const { hasPermission } = useAuth()
  return hasPermission(permission) ? children : fallback
}
