import axios from 'axios'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { formatDateFr } from '../../i18n/format'
import { PermissionGate } from '../../routes/PermissionGate'

interface Warehouse {
  id: string
  name: string
}

interface StorageLocation {
  id: string
  code: string
  warehouseId: string
  locationType: string
  isLocked: boolean
}

interface StockLot {
  id: string
  productDesignation: string | null
  internalLotNumber: string
  expiryDate: string
  remainingQuantity: number
  availableQuantity: number
  qualityStatus: string
}

const LOCATION_TYPES = ['Reception', 'Quarantaine', 'Stockage', 'Picking', 'Reserve', 'ChaineDuFroid', 'ProduitsPerimes', 'ProduitsDetruits', 'Transit']

/** Entrepôt/Emplacements (US4 — FR-034). */
export function WarehousePage() {
  const [warehouses, setWarehouses] = useState<Warehouse[]>([])
  const [locations, setLocations] = useState<StorageLocation[]>([])
  const [lots, setLots] = useState<StockLot[]>([])
  const [warehouseName, setWarehouseName] = useState('')
  const [locationCode, setLocationCode] = useState('')
  const [locationWarehouseId, setLocationWarehouseId] = useState('')
  const [locationType, setLocationType] = useState(LOCATION_TYPES[2])
  const [formError, setFormError] = useState<string | null>(null)

  const loadAll = useCallback(async () => {
    const [warehousesResponse, locationsResponse, lotsResponse] = await Promise.all([
      apiClient.get<Warehouse[]>('/api/warehouses'),
      apiClient.get<StorageLocation[]>('/api/warehouses/locations'),
      apiClient.get<StockLot[]>('/api/stock/lots'),
    ])
    setWarehouses(warehousesResponse.data)
    setLocations(locationsResponse.data)
    setLots(lotsResponse.data)
  }, [])

  useEffect(() => {
    void loadAll()
  }, [loadAll])

  async function handleCreateWarehouse(event: FormEvent) {
    event.preventDefault()
    await apiClient.post('/api/warehouses', { name: warehouseName })
    setWarehouseName('')
    await loadAll()
  }

  async function handleCreateLocation(event: FormEvent) {
    event.preventDefault()
    setFormError(null)
    try {
      await apiClient.post('/api/warehouses/locations', { code: locationCode, warehouseId: locationWarehouseId, locationType })
      setLocationCode('')
      await loadAll()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setFormError(err.response.data.message)
      }
    }
  }

  return (
    <div className="space-y-8">
      <h1 className="text-xl font-semibold">{labels.nav.warehouse}</h1>

      <PermissionGate permission="Stock.Move">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <form onSubmit={(e) => void handleCreateWarehouse(e)} className="space-y-2 rounded border border-gray-200 bg-white p-4">
            <h2 className="text-sm font-semibold text-gray-700">Nouvel entrepôt</h2>
            <input required placeholder="Nom" value={warehouseName} onChange={(e) => setWarehouseName(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            <button type="submit" className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800">
              {labels.actions.create}
            </button>
          </form>

          <form onSubmit={(e) => void handleCreateLocation(e)} className="space-y-2 rounded border border-gray-200 bg-white p-4">
            <h2 className="text-sm font-semibold text-gray-700">Nouvel emplacement</h2>
            <select required value={locationWarehouseId} onChange={(e) => setLocationWarehouseId(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2 text-sm">
              <option value="">Entrepôt…</option>
              {warehouses.map((w) => (
                <option key={w.id} value={w.id}>{w.name}</option>
              ))}
            </select>
            <input required placeholder="Code (ZONE-ALLÉE-RACK-NIVEAU-POSITION)" value={locationCode} onChange={(e) => setLocationCode(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            <select value={locationType} onChange={(e) => setLocationType(e.target.value)} className="w-full rounded border border-gray-300 px-3 py-2 text-sm">
              {LOCATION_TYPES.map((type) => (
                <option key={type} value={type}>{type}</option>
              ))}
            </select>
            <button type="submit" className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800">
              {labels.actions.create}
            </button>
            {formError && <p className="text-sm text-red-600">{formError}</p>}
          </form>
        </div>
      </PermissionGate>

      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-gray-700">Emplacements ({locations.length})</h2>
        <div className="flex flex-wrap gap-2 text-sm">
          {locations.map((loc) => (
            <span key={loc.id} className="rounded border border-gray-200 bg-white px-2 py-1">
              {loc.code} <span className="text-gray-400">({loc.locationType})</span>
            </span>
          ))}
        </div>
      </section>

      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-gray-700">Stock par lot</h2>
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">Produit</th>
              <th className="px-3 py-2">N° lot interne</th>
              <th className="px-3 py-2">Péremption</th>
              <th className="px-3 py-2">Disponible</th>
              <th className="px-3 py-2">Statut</th>
            </tr>
          </thead>
          <tbody>
            {lots.length === 0 && (
              <tr>
                <td colSpan={5} className="px-3 py-4 text-center text-gray-500">{labels.states.empty}</td>
              </tr>
            )}
            {lots.map((lot) => (
              <tr key={lot.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{lot.productDesignation}</td>
                <td className="px-3 py-2">{lot.internalLotNumber}</td>
                <td className="px-3 py-2">{formatDateFr(lot.expiryDate)}</td>
                <td className="px-3 py-2">{lot.availableQuantity}/{lot.remainingQuantity}</td>
                <td className="px-3 py-2">
                  <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{lot.qualityStatus}</span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  )
}
