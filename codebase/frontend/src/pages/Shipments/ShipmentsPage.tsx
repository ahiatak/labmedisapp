import axios from 'axios'
import { useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { PermissionGate } from '../../routes/PermissionGate'

interface Shipment {
  id: string
  shipmentNumber: string
  status: string
  transportMode: string
  carrier: string | null
  transportReference: string | null
  importAuthorizationRef: string | null
}

interface TimelineEntry {
  status: string
  occurredAt: string
  notes: string | null
}

const TRANSPORT_MODES = ['Maritime', 'Aerien', 'Express', 'Terrestre']
const SHIPMENT_STATUSES = ['Creee', 'Expediee', 'ArriveePort', 'EnDouane', 'Dedouanee', 'Livree']

/** Expéditions / Logistique (US3 — contracts/shipments.md, FR-025 à FR-028). */
export function ShipmentsPage() {
  const [carrier, setCarrier] = useState('')
  const [transportMode, setTransportMode] = useState(TRANSPORT_MODES[0])
  const [transportReference, setTransportReference] = useState('')
  const [importAuthorizationRef, setImportAuthorizationRef] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [shipment, setShipment] = useState<Shipment | null>(null)
  const [timeline, setTimeline] = useState<TimelineEntry[]>([])
  const [eventStatus, setEventStatus] = useState(SHIPMENT_STATUSES[1])
  const [lookupId, setLookupId] = useState('')

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)
    setIsSubmitting(true)
    try {
      const response = await apiClient.post<Shipment>('/api/shipments', {
        transportMode,
        carrier: carrier || null,
        transportReference: transportReference || null,
        importAuthorizationRef: importAuthorizationRef || null,
        purchaseOrderLineIds: [],
      })
      setShipment(response.data)
      setTimeline([])
      setCarrier('')
      setTransportReference('')
      setImportAuthorizationRef('')
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setFormError(err.response.data.message)
      } else {
        setFormError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function loadShipment(id: string) {
    setFormError(null)
    try {
      const [shipmentResponse, timelineResponse] = await Promise.all([
        apiClient.get<Shipment>(`/api/shipments/${id}`),
        apiClient.get<TimelineEntry[]>(`/api/shipments/${id}/timeline`),
      ])
      setShipment(shipmentResponse.data)
      setTimeline(timelineResponse.data)
    } catch {
      setFormError('Expédition introuvable.')
    }
  }

  async function addEvent() {
    if (!shipment) {
      return
    }
    try {
      const response = await apiClient.post<Shipment>(`/api/shipments/${shipment.id}/events`, { status: eventStatus })
      setShipment(response.data)
      await loadShipment(shipment.id)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setFormError(err.response.data.message)
      }
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.shipments}</h1>

      <PermissionGate permission="Shipments.Create">
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
          <select value={transportMode} onChange={(e) => setTransportMode(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
            {TRANSPORT_MODES.map((mode) => (
              <option key={mode} value={mode}>{mode}</option>
            ))}
          </select>
          <input placeholder="Transporteur" value={carrier} onChange={(e) => setCarrier(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
          <input placeholder="Référence de suivi" value={transportReference} onChange={(e) => setTransportReference(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
          <input placeholder="Réf. autorisation DPML (si médicament)" value={importAuthorizationRef} onChange={(e) => setImportAuthorizationRef(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm" />
          <button type="submit" disabled={isSubmitting} className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50 sm:col-span-4">
            {labels.actions.create}
          </button>
          {formError && <p className="text-sm text-red-600 sm:col-span-4">{formError}</p>}
        </form>
      </PermissionGate>

      <div className="flex gap-2">
        <input placeholder="Rechercher par identifiant d'expédition…" value={lookupId} onChange={(e) => setLookupId(e.target.value)} className="w-96 rounded border border-gray-300 px-3 py-2 text-sm" />
        <button type="button" onClick={() => void loadShipment(lookupId)} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
          Charger
        </button>
      </div>

      {shipment && (
        <div className="space-y-4 rounded border border-gray-200 bg-white p-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-medium">{shipment.shipmentNumber}</p>
              <p className="text-sm text-gray-500">{shipment.transportMode} — {shipment.carrier ?? '—'}</p>
            </div>
            <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium">{shipment.status}</span>
          </div>

          <PermissionGate permission="Shipments.Update">
            <div className="flex items-center gap-2">
              <select value={eventStatus} onChange={(e) => setEventStatus(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
                {SHIPMENT_STATUSES.map((status) => (
                  <option key={status} value={status}>{status}</option>
                ))}
              </select>
              <button type="button" onClick={() => void addEvent()} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
                Enregistrer l'événement
              </button>
            </div>
          </PermissionGate>

          <div>
            <h2 className="mb-2 text-sm font-semibold text-gray-700">Historique</h2>
            <ul className="space-y-1 text-sm">
              {timeline.length === 0 && <li className="text-gray-500">{labels.states.empty}</li>}
              {timeline.map((entry, index) => (
                <li key={index}>
                  {entry.status} — {new Date(entry.occurredAt).toLocaleString('fr-FR')}
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}
