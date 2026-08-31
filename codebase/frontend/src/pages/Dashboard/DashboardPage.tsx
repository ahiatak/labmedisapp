import { useCallback, useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { startNotificationHub, getNotificationHubConnection } from '../../services/signalrClient'
import { labels } from '../../i18n/labels'
import { formatCfa } from '../../i18n/format'

interface DirectionDashboard {
  totalRevenueCfa: string
  totalMarginCfa: string
  stockValueCfa: string
  stockoutProductCount: number
}

/**
 * Tableau de bord Direction (US11 — contracts/reporting.md, FR-068/FR-075). Refetches on
 * "dashboardRefresh" SignalR events pushed by NotificationHub (US12) — no polling, per
 * Principle IX of the constitution.
 */
export function DashboardPage() {
  const [dashboard, setDashboard] = useState<DirectionDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)

  const loadDashboard = useCallback(async () => {
    try {
      const response = await apiClient.get<DirectionDashboard>('/api/reports/dashboard/direction')
      setDashboard(response.data)
      setError(null)
    } catch {
      setError(labels.states.error)
    }
  }, [])

  useEffect(() => {
    void loadDashboard()

    let cancelled = false
    void startNotificationHub().then(() => {
      if (cancelled) {
        return
      }
      getNotificationHubConnection().on('dashboardRefresh', () => void loadDashboard())
    })

    return () => {
      cancelled = true
      getNotificationHubConnection().off('dashboardRefresh')
    }
  }, [loadDashboard])

  if (error) {
    return <p className="text-sm text-red-600">{error}</p>
  }

  if (!dashboard) {
    return <p className="text-sm text-gray-500">{labels.states.loading}</p>
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.dashboard}</h1>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
        <div className="rounded border border-gray-200 bg-white p-4">
          <p className="text-xs text-gray-500">Chiffre d'affaires</p>
          <p className="text-2xl font-semibold">{formatCfa(Number(dashboard.totalRevenueCfa))}</p>
        </div>
        <div className="rounded border border-gray-200 bg-white p-4">
          <p className="text-xs text-gray-500">Marge</p>
          <p className="text-2xl font-semibold">{formatCfa(Number(dashboard.totalMarginCfa))}</p>
        </div>
        <div className="rounded border border-gray-200 bg-white p-4">
          <p className="text-xs text-gray-500">Valeur de stock</p>
          <p className="text-2xl font-semibold">{formatCfa(Number(dashboard.stockValueCfa))}</p>
        </div>
        <div className="rounded border border-gray-200 bg-white p-4">
          <p className="text-xs text-gray-500">Produits en rupture</p>
          <p className="text-2xl font-semibold">{dashboard.stockoutProductCount}</p>
        </div>
      </div>
    </div>
  )
}
