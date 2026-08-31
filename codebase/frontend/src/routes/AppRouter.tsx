import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { LoginPage } from '../pages/Auth/LoginPage'
import { ProductsPage } from '../pages/Products/ProductsPage'
import { SuppliersPage } from '../pages/Suppliers/SuppliersPage'
import { CustomersPage } from '../pages/Customers/CustomersPage'
import { AdminUsersPage } from '../pages/Admin/Users/AdminUsersPage'
import { PurchaseOrdersPage } from '../pages/PurchaseOrders/PurchaseOrdersPage'
import { QualityPage } from '../pages/Quality/QualityPage'
import { PricingSimulatorPage } from '../pages/Pricing/Simulator/PricingSimulatorPage'
import { PricingProfilesPage } from '../pages/Pricing/Profiles/PricingProfilesPage'
import { SaleOrdersPage } from '../pages/SaleOrders/SaleOrdersPage'
import { ReturnsPage } from '../pages/Returns/ReturnsPage'
import { InventoryPage } from '../pages/Inventory/InventoryPage'
import { ForecastPage } from '../pages/Forecast/ForecastPage'
import { DashboardPage } from '../pages/Dashboard/DashboardPage'
import { ReportsPage } from '../pages/Reports/ReportsPage'
import { CompliancePage } from '../pages/Compliance/CompliancePage'
import { ShipmentsPage } from '../pages/Shipments/ShipmentsPage'
import { StockReceptionPage } from '../pages/StockReception/StockReceptionPage'
import { WarehousePage } from '../pages/Warehouse/WarehousePage'
import { Layout } from './Layout'
import { ProtectedRoute } from './ProtectedRoute'

/** Top-level route table — every module page for the 13 user stories (see specs/001-gestion-depositaire-pharmaceutique). */
export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        <Route
          element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<DashboardPage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/suppliers" element={<SuppliersPage />} />
          <Route path="/customers" element={<CustomersPage />} />
          <Route path="/admin/users" element={<AdminUsersPage />} />
          <Route path="/purchase-orders" element={<PurchaseOrdersPage />} />
          <Route path="/shipments" element={<ShipmentsPage />} />
          <Route path="/stock/reception" element={<StockReceptionPage />} />
          <Route path="/warehouse" element={<WarehousePage />} />
          <Route path="/quality" element={<QualityPage />} />
          <Route path="/pricing/simulator" element={<PricingSimulatorPage />} />
          <Route path="/pricing/profiles" element={<PricingProfilesPage />} />
          <Route path="/sale-orders" element={<SaleOrdersPage />} />
          <Route path="/returns" element={<ReturnsPage />} />
          <Route path="/inventory" element={<InventoryPage />} />
          <Route path="/forecast" element={<ForecastPage />} />
          <Route path="/reports" element={<ReportsPage />} />
          <Route path="/compliance" element={<CompliancePage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
