import axios from 'axios'
import { useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'

interface Attachment {
  id: string
  documentType: string
  fileReference: string
  uploadedAt: string
}

interface RecallCustomer {
  customerId: string
  name: string
  address: string | null
}

interface LotTraceability {
  stockLotId: string
  internalLotNumber: string
  customers: RecallCustomer[]
}

const DOCUMENT_TYPES = ['Facture', 'Douane', 'Certificat', 'AutorisationDpml']

/** Conformité / Rappel de Lot (US13 — contracts, FR-080 à FR-083). */
export function CompliancePage() {
  const [lotId, setLotId] = useState('')
  const [attachments, setAttachments] = useState<Attachment[]>([])
  const [traceability, setTraceability] = useState<LotTraceability | null>(null)
  const [documentType, setDocumentType] = useState(DOCUMENT_TYPES[2])
  const [fileReference, setFileReference] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function loadLot() {
    if (!lotId) {
      return
    }
    setError(null)
    try {
      const [attachmentsResponse, traceabilityResponse] = await Promise.all([
        apiClient.get<Attachment[]>('/api/compliance/attachments', { params: { attachableType: 'StockLot', attachableId: lotId } }),
        apiClient.get<LotTraceability>(`/api/compliance/lots/${lotId}/traceability`),
      ])
      setAttachments(attachmentsResponse.data)
      setTraceability(traceabilityResponse.data)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      } else {
        setError(labels.states.error)
      }
      setTraceability(null)
    }
  }

  async function handleAttach(event: FormEvent) {
    event.preventDefault()
    setError(null)
    try {
      await apiClient.post('/api/compliance/attachments', {
        attachableType: 'StockLot',
        attachableId: lotId,
        documentType,
        fileReference,
      })
      setFileReference('')
      await loadLot()
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.data?.message) {
        setError(err.response.data.message)
      }
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.compliance}</h1>

      <div className="flex gap-2">
        <input
          placeholder="Identifiant du lot"
          value={lotId}
          onChange={(e) => setLotId(e.target.value)}
          className="w-96 rounded border border-gray-300 px-3 py-2 text-sm"
        />
        <button type="button" onClick={() => void loadLot()} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
          Charger
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {traceability && (
        <div className="space-y-6">
          <section className="rounded border border-gray-200 bg-white p-4">
            <h2 className="mb-2 text-sm font-semibold text-gray-700">Pièces justificatives — Lot {traceability.internalLotNumber}</h2>
            <form onSubmit={(e) => void handleAttach(e)} className="mb-4 flex gap-2">
              <select value={documentType} onChange={(e) => setDocumentType(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
                {DOCUMENT_TYPES.map((type) => (
                  <option key={type} value={type}>{type}</option>
                ))}
              </select>
              <input
                required
                placeholder="Référence du fichier"
                value={fileReference}
                onChange={(e) => setFileReference(e.target.value)}
                className="flex-1 rounded border border-gray-300 px-3 py-2 text-sm"
              />
              <button type="submit" className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800">
                Rattacher
              </button>
            </form>
            <ul className="space-y-1 text-sm">
              {attachments.length === 0 && <li className="text-gray-500">Aucune pièce jointe.</li>}
              {attachments.map((attachment) => (
                <li key={attachment.id}>
                  {attachment.documentType} — {attachment.fileReference}
                </li>
              ))}
            </ul>
          </section>

          <section className="rounded border border-gray-200 bg-white p-4">
            <h2 className="mb-2 text-sm font-semibold text-gray-700">Rappel produit — Clients ayant reçu ce lot</h2>
            <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-3 py-2">Client</th>
                  <th className="px-3 py-2">Adresse</th>
                </tr>
              </thead>
              <tbody>
                {traceability.customers.length === 0 && (
                  <tr>
                    <td colSpan={2} className="px-3 py-4 text-center text-gray-500">Aucun client identifié pour ce lot.</td>
                  </tr>
                )}
                {traceability.customers.map((customer) => (
                  <tr key={customer.customerId} className="border-t border-gray-100">
                    <td className="px-3 py-2">{customer.name}</td>
                    <td className="px-3 py-2">{customer.address ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        </div>
      )}
    </div>
  )
}
