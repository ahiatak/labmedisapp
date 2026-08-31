import { NavLink, Outlet } from 'react-router-dom'
import { labels } from '../i18n/labels'
import { NotificationCenter } from '../components/NotificationCenter/NotificationCenter'
import { useAuth } from './AuthContext'

interface NavItem {
  to: string
  label: string
  permission?: string
}

const navItems: NavItem[] = [
  { to: '/', label: labels.nav.dashboard },
  { to: '/products', label: labels.nav.products, permission: 'Products.Read' },
  { to: '/suppliers', label: labels.nav.suppliers, permission: 'Suppliers.Read' },
  { to: '/customers', label: labels.nav.customers, permission: 'Customers.Read' },
  { to: '/purchase-orders', label: labels.nav.purchaseOrders, permission: 'PurchaseOrders.Read' },
  { to: '/shipments', label: labels.nav.shipments, permission: 'Shipments.Read' },
  { to: '/stock/reception', label: labels.nav.stockReception, permission: 'Stock.Receive' },
  { to: '/warehouse', label: labels.nav.warehouse, permission: 'Stock.Read' },
  { to: '/quality', label: labels.nav.quality, permission: 'Quality.Read' },
  { to: '/pricing/simulator', label: labels.nav.pricing, permission: 'Pricing.Read' },
  { to: '/pricing/profiles', label: `${labels.nav.pricing} (profils)`, permission: 'Pricing.Update' },
  { to: '/sale-orders', label: labels.nav.saleOrders, permission: 'Sales.Read' },
  { to: '/returns', label: labels.nav.returns, permission: 'Returns.Read' },
  { to: '/inventory', label: labels.nav.inventory, permission: 'Inventory.Read' },
  { to: '/forecast', label: labels.nav.forecast, permission: 'Forecast.Read' },
  { to: '/reports', label: labels.nav.reports, permission: 'Reports.Read' },
  { to: '/compliance', label: labels.nav.compliance, permission: 'Compliance.Read' },
  { to: '/admin/users', label: labels.nav.admin, permission: 'Users.Read' },
]

/**
 * Application shell. The nav is dynamic: it only lists modules the connected user's role
 * grants access to (FR-019).
 */
export function Layout() {
  const { user, logout, hasPermission } = useAuth()

  const visibleNavItems = navItems.filter((item) => {
    if (!item.permission) return true
    return hasPermission(item.permission) || user?.roles.includes('Admin')
  })

  return (
    <div className="flex min-h-screen bg-gray-50 text-gray-900">
      <aside className="w-64 shrink-0 border-r border-gray-200 bg-white p-4">
        <h1 className="mb-6 text-lg font-semibold">{labels.appName}</h1>
        <nav className="flex flex-col gap-1">
          {visibleNavItems.map((item) => (
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
