import axios from 'axios'
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { apiClient } from '../../services/apiClient'
import { labels } from '../../i18n/labels'
import { PermissionGate } from '../../routes/PermissionGate'

interface Product {
  id: string
  designation: string
  categoryId: string
  categoryName: string | null
  codeCip: string | null
  vatRate: string
  isActive: boolean
}

interface Lookup {
  id: string
  name: string
}

interface ImportRowError {
  rowNumber: number
  message: string
}

interface ImportReport {
  totalRows: number
  successCount: number
  errors: ImportRowError[]
}

/** Catalogue Produits (US1 — contracts/products-referentiel.md, FR-001 à FR-006). */
export function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [categories, setCategories] = useState<Lookup[]>([])
  const [search, setSearch] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [designation, setDesignation] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [vatRate, setVatRate] = useState('0.18')
  const [codeCip, setCodeCip] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const [importReport, setImportReport] = useState<ImportReport | null>(null)
  const [isImporting, setIsImporting] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const loadProducts = useCallback(async (searchTerm: string) => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await apiClient.get<{ items: Product[] }>('/api/products', {
        params: searchTerm ? { search: searchTerm } : undefined,
      })
      setProducts(response.data.items)
    } catch {
      setError(labels.states.error)
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadProducts('')
    apiClient
      .get<Lookup[]>('/api/referentiel/categories')
      .then((response) => setCategories(response.data))
      .catch(() => setCategories([]))
  }, [loadProducts])

  async function handleSearchSubmit(event: FormEvent) {
    event.preventDefault()
    await loadProducts(search)
  }

  async function handleCreateSubmit(event: FormEvent) {
    event.preventDefault()
    setFormError(null)

    if (!categoryId) {
      setFormError('Sélectionnez une catégorie.')
      return
    }

    setIsSubmitting(true)
    try {
      await apiClient.post('/api/products', { designation, categoryId, vatRate, codeCip: codeCip || null })
      setDesignation('')
      setCodeCip('')
      await loadProducts(search)
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        setFormError('Un produit avec cette désignation existe déjà.')
      } else if (axios.isAxiosError(err) && err.response?.data?.message) {
        setFormError(err.response.data.message)
      } else {
        setFormError(labels.states.error)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleDeactivate(id: string) {
    await apiClient.delete(`/api/products/${id}`)
    await loadProducts(search)
  }

  async function handleImport() {
    const file = fileInputRef.current?.files?.[0]
    if (!file) {
      return
    }

    setIsImporting(true)
    setImportReport(null)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const response = await apiClient.post<ImportReport>('/api/products/import', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      setImportReport(response.data)
      await loadProducts(search)
    } catch {
      setError("L'import du catalogue a échoué.")
    } finally {
      setIsImporting(false)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">{labels.nav.products}</h1>

      <PermissionGate permission="Products.Create">
        <form onSubmit={(e) => void handleCreateSubmit(e)} className="grid grid-cols-1 gap-3 rounded border border-gray-200 bg-white p-4 sm:grid-cols-4">
          <input
            required
            placeholder="Désignation"
            value={designation}
            onChange={(e) => setDesignation(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm sm:col-span-2"
          />
          <select
            required
            value={categoryId}
            onChange={(e) => setCategoryId(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          >
            <option value="">Catégorie…</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
          <input
            placeholder="Code CIP"
            value={codeCip}
            onChange={(e) => setCodeCip(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          />
          <input
            placeholder="Taux TVA (ex: 0.18)"
            value={vatRate}
            onChange={(e) => setVatRate(e.target.value)}
            className="rounded border border-gray-300 px-3 py-2 text-sm"
          />
          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded bg-gray-900 px-3 py-2 text-sm font-medium text-white hover:bg-gray-800 disabled:opacity-50"
          >
            {labels.actions.create}
          </button>
          {formError && <p className="text-sm text-red-600 sm:col-span-4">{formError}</p>}
        </form>
      </PermissionGate>

      <PermissionGate permission="Products.Create">
        <div className="flex items-center gap-3 rounded border border-gray-200 bg-white p-4">
          <input ref={fileInputRef} type="file" accept=".xlsx" className="text-sm" />
          <button
            type="button"
            onClick={() => void handleImport()}
            disabled={isImporting}
            className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100 disabled:opacity-50"
          >
            {isImporting ? labels.states.loading : 'Importer un catalogue Excel'}
          </button>
          {importReport && (
            <span className="text-sm text-gray-600">
              {importReport.successCount}/{importReport.totalRows} lignes importées
              {importReport.errors.length > 0 && ` — ${importReport.errors.length} erreur(s)`}
            </span>
          )}
        </div>
      </PermissionGate>
      {importReport && importReport.errors.length > 0 && (
        <ul className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {importReport.errors.map((rowError) => (
            <li key={rowError.rowNumber}>
              Ligne {rowError.rowNumber} : {rowError.message}
            </li>
          ))}
        </ul>
      )}

      <form onSubmit={(e) => void handleSearchSubmit(e)} className="flex gap-2">
        <input
          placeholder="Rechercher…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-64 rounded border border-gray-300 px-3 py-2 text-sm"
        />
        <button type="submit" className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-100">
          Rechercher
        </button>
      </form>

      {isLoading && <p className="text-sm text-gray-500">{labels.states.loading}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!isLoading && !error && (
        <table className="w-full border-collapse overflow-hidden rounded border border-gray-200 bg-white text-sm">
          <thead className="bg-gray-100 text-left">
            <tr>
              <th className="px-3 py-2">Désignation</th>
              <th className="px-3 py-2">Catégorie</th>
              <th className="px-3 py-2">Code CIP</th>
              <th className="px-3 py-2">TVA</th>
              <th className="px-3 py-2">Statut</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {products.length === 0 && (
              <tr>
                <td colSpan={6} className="px-3 py-4 text-center text-gray-500">
                  {labels.states.empty}
                </td>
              </tr>
            )}
            {products.map((product) => (
              <tr key={product.id} className="border-t border-gray-100">
                <td className="px-3 py-2">{product.designation}</td>
                <td className="px-3 py-2">{product.categoryName}</td>
                <td className="px-3 py-2">{product.codeCip}</td>
                <td className="px-3 py-2">{product.vatRate}</td>
                <td className="px-3 py-2">{product.isActive ? 'Actif' : 'Inactif'}</td>
                <td className="px-3 py-2 text-right">
                  <PermissionGate permission="Products.Delete">
                    {product.isActive && (
                      <button
                        type="button"
                        onClick={() => void handleDeactivate(product.id)}
                        className="text-sm text-red-600 hover:underline"
                      >
                        {labels.actions.delete}
                      </button>
                    )}
                  </PermissionGate>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
