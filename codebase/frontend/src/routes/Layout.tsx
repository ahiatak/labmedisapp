import { NavLink, Outlet } from 'react-router-dom'
import { labels } from '../i18n/labels'
import { NotificationCenter } from '../components/NotificationCenter/NotificationCenter'
import { useAuth } from './AuthContext'

const navItems: Array<{ to: string; label: string }> = [
  { to: '/', label: labels.nav.dashboard },
  { to: '/products', label: labels.nav.products },
  { to: '/suppliers', label: labels.nav.suppliers },
  { to: '/customers', label: labels.nav.customers },
  { to: '/purchase-orders', label: labels.nav.purchaseOrders },
  { to: '/shipments', label: labels.nav.shipments },
  { to: '/stock/reception', label: labels.nav.stockReception },
  { to: '/warehouse', label: labels.nav.warehouse },
  { to: '/quality', label: labels.nav.quality },
  { to: '/pricing/simulator', label: labels.nav.pricing },
  { to: '/pricing/profiles', label: `${labels.nav.pricing} (profils)` },
  { to: '/sale-orders', label: labels.nav.saleOrders },
  { to: '/returns', label: labels.nav.returns },
  { to: '/inventory', label: labels.nav.inventory },
  { to: '/forecast', label: labels.nav.forecast },
  { to: '/reports', label: labels.nav.reports },
  { to: '/compliance', label: labels.nav.compliance },
  { to: '/admin/users', label: labels.nav.admin },
]

/**
 * Application shell. The nav is dynamic: it only lists modules the connected user's role
 * grants access to (FR-019) — permission checks are wired once real modules/permission
 * codes exist per user story.
 */
export function Layout() {
  const { user, logout } = useAuth()

  return (
    <div className="flex min-h-screen bg-gray-50 text-gray-900">
      <aside className="w-64 shrink-0 border-r border-gray-200 bg-white p-4">
        <h1 className="mb-6 text-lg font-semibold">{labels.appName}</h1>
        <nav className="flex flex-col gap-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `rounded px-3 py-2 text-sm ${isActive ? 'bg-gray-900 text-white' : 'hover:bg-gray-100'}`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-3">
          <span className="text-sm text-gray-500">
            {user ? `${user.firstName} ${user.lastName}` : ''}
          </span>
          <div className="flex items-center gap-3">
            {user && <NotificationCenter />}
            {user && (
              <button
                type="button"
                onClick={() => void logout()}
                className="text-sm text-gray-500 hover:text-gray-900"
              >
                {labels.actions.logout}
              </button>
            )}
          </div>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
