import { useEffect, useState } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatCfa, formatDateFr } from '../../i18n/format'

interface StockReport {
  totalAvailable: number
  totalReserved: number
  totalQuarantine: number
  totalExpired: number
  slowMovingProductCount: number
}

interface ExpiringLot {
  lotId: string
  productDesignation: string | null
  internalLotNumber: string
  expiryDate: string
  remainingQuantity: number
}

interface SalesReportLine {
  id: string
  name: string | null
  revenueCfa: string
}

interface SalesReport {
  totalRevenueCfa: string
  returnRatePercent: string
  byCustomer: SalesReportLine[]
  byProduct: SalesReportLine[]
}

interface PricingReportLine {
  productId: string
  productDesignation: string | null
  theoreticalMarginCfa: string
  realMarginCfa: string
  priceGapCfa: string
}

interface QualityReport {
  quarantineCount: number
  nonConformeCount: number
}

const REPORT_TYPES = ['stock', 'sales', 'pricing', 'quality'] as const
type ReportType = (typeof REPORT_TYPES)[number]

const REPORT_LABELS: Record<ReportType, string> = {
  stock: 'Stock',
  sales: 'Ventes',
  pricing: 'Pricing',
  quality: 'Qualité',
}

/** Rapports (US11 — contracts/reporting.md, FR-069 à FR-074). */
export function ReportsPage() {
  const [activeTab, setActiveTab] = useState<ReportType>('stock')
  const [stockReport, setStockReport] = useState<StockReport | null>(null)
  const [expiringLots, setExpiringLots] = useState<ExpiringLot[]>([])
  const [salesReport, setSalesReport] = useState<SalesReport | null>(null)
  const [pricingReport, setPricingReport] = useState<PricingReportLine[]>([])
  const [qualityReport, setQualityReport] = useState<QualityReport | null>(null)

  useEffect(() => {
    apiClient.get<StockReport>('/api/reports/stock').then((r) => setStockReport(r.data)).catch(() => setStockReport(null))
    apiClient.get<ExpiringLot[]>('/api/reports/lots/expiring', { params: { days: 90 } }).then((r) => setExpiringLots(r.data)).catch(() => setExpiringLots([]))
    apiClient.get<SalesReport>('/api/reports/sales').then((r) => setSalesReport(r.data)).catch(() => setSalesReport(null))
    apiClient.get<PricingReportLine[]>('/api/reports/pricing').then((r) => setPricingReport(r.data)).catch(() => setPricingReport([]))
    apiClient.get<QualityReport>('/api/reports/quality').then((r) => setQualityReport(r.data)).catch(() => setQualityReport(null))
  }, [])

  async function handleExport(format: 'Pdf' | 'Excel') {
    const response = await apiClient.post('/api/reports/export', { reportType: activeTab, format }, { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([response.data as BlobPart]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', `rapport-${activeTab}.${format === 'Pdf' ? 'pdf' : 'xlsx'}`)
    document.body.appendChild(link)
    link.click()
    link.remove()
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.reports}</h1>

      <div className="flex items-center justify-between">
        <div className="flex gap-2">
          {REPORT_TYPES.map((type) => (
            <button
              key={type}
              type="button"
              onClick={() => setActiveTab(type)}
              className={`rounded px-3 py-2 text-sm ${activeTab === type ? 'bg-gray-900 text-white' : 'border border-gray-300 hover:bg-gray-100'}`}
            >
              {REPORT_LABELS[type]}
            </button>
          ))}
        </div>
        <div className="flex gap-2">
          <button type="button" onClick={() => void handleExport('Excel')} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
            Export Excel
          </button>
          <button type="button" onClick={() => void handleExport('Pdf')} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
            Export PDF
          </button>
        </div>
      </div>

      {activeTab === 'stock' && stockReport && (
        <div className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
            <div className="rounded border border-gray-200 bg-white p-4">
              <p className="text-xs text-gray-500">Disponible</p>
              <p className="text-xl font-semibold">{stockReport.totalAvailable}</p>
            </div>
            <div className="rounded border border-gray-200 bg-white p-4">
              <p className="text-xs text-gray-500">Réservé</p>
              <p className="text-xl font-semibold">{stockReport.totalReserved}</p>
            </div>
            <div className="rounded border border-gray-200 bg-white p-4">
              <p className="text-xs text-gray-500">Quarantaine</p>
              <p className="text-xl font-semibold">{stockReport.totalQuarantine}</p>
            </div>
            <div className="rounded border border-gray-200 bg-white p-4">
              <p className="text-xs text-gray-500">Périmé</p>
              <p className="text-xl font-semibold">{stockReport.totalExpired}</p>
            </div>
          </div>

          <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
            <thead className="bg-gray-100 text-left">
              <tr>
                <th className="px-3 py-2">Produit</th>
                <th className="px-3 py-2">N° lot</th>
                <th className="px-3 py-2">Péremption</th>
                <th className="px-3 py-2">Quantité</th>
              </tr>
            </thead>
            <tbody>
              {expiringLots.map((lot) => (
                <tr key={lot.lotId} className="border-t border-gray-100">
                  <td className="px-3 py-2">{lot.productDesignation}</td>
                  <td className="px-3 py-2">{lot.internalLotNumber}</td>
                  <td className="px-3 py-2">{formatDateFr(lot.expiryDate)}</td>
                  <td className="px-3 py-2">{lot.remainingQuantity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeTab === 'sales' && salesReport && (
        <div className="space-y-4">
          <p className="text-sm text-gray-600">Taux de retour : {salesReport.returnRatePercent}%</p>
          <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
            <thead className="bg-gray-100 text-left">
              <tr>
                <th className="px-3 py-2">Client</th>
                <th className="px-3 py-2">CA</th>
              </tr>
            </thead>
            <tbody>
              {salesReport.byCustomer.map((line) => (
                <tr key={line.id} className="border-t border-gray-100">
                  <td className="px-3 py-2">{line.name}</td>
                  <td className="px-3 py-2">{formatCfa(Number(line.revenueCfa))}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeTab === 'pricing' && (
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">Produit</th>
              <th className="px-3 py-2">Marge théorique</th>
              <th className="px-3 py-2">Marge réelle</th>
              <th className="px-3 py-2">Écart PV</th>
            </tr>
          </thead>
          <tbody>
            {pricingReport.map((line) => (
              <tr key={line.productId} className="border-t border-gray-100">
                <td className="px-3 py-2">{line.productDesignation}</td>
                <td className="px-3 py-2">{formatCfa(Number(line.theoreticalMarginCfa))}</td>
                <td className="px-3 py-2">{formatCfa(Number(line.realMarginCfa))}</td>
                <td className="px-3 py-2">{formatCfa(Number(line.priceGapCfa))}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {activeTab === 'quality' && qualityReport && (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div className="rounded border border-gray-200 bg-white p-4">
            <p className="text-xs text-gray-500">Lots en quarantaine</p>
            <p className="text-2xl font-semibold">{qualityReport.quarantineCount}</p>
          </div>
          <div className="rounded border border-gray-200 bg-white p-4">
            <p className="text-xs text-gray-500">Lots non conformes</p>
            <p className="text-2xl font-semibold">{qualityReport.nonConformeCount}</p>
          </div>
        </div>
      )}
    </div>
  )
}
