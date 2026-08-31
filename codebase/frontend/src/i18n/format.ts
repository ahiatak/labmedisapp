/**
 * Formatting helpers enforcing the constitution's French/CFA presentation rules (FR-090:
 * 100% French UI, dates JJ/MM/AAAA; FR-088: every amount shown in its origin currency AND
 * its XOF equivalent).
 */

const DATE_FORMATTER = new Intl.DateTimeFormat('fr-FR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

/** Formats an ISO date string or Date as JJ/MM/AAAA. */
export function formatDateFr(value: string | Date): string {
  const date = typeof value === 'string' ? new Date(value) : value
  return DATE_FORMATTER.format(date)
}

const CFA_FORMATTER = new Intl.NumberFormat('fr-FR', {
  maximumFractionDigits: 0,
  minimumFractionDigits: 0,
})

/** Formats a XOF amount with thousands separators and zero decimals (RG-009 — arrondi CFA). */
export function formatCfa(amount: number): string {
  return `${CFA_FORMATTER.format(Math.round(amount))} XOF`
}

const CURRENCY_FORMATTERS: Record<string, Intl.NumberFormat> = {
  EUR: new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' }),
  USD: new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'USD' }),
}

/**
 * Formats a monetary amount in its origin currency, alongside its XOF equivalent when
 * provided — satisfies FR-088 ("devise d'origine ET équivalent XOF") wherever a financial
 * amount is displayed (invoices, purchase orders, price history, reports).
 */
export function formatDualCurrency(
  amount: number,
  currencyCode: string,
  xofEquivalent?: number
): string {
  const origin =
    currencyCode === 'XOF'
      ? formatCfa(amount)
      : (CURRENCY_FORMATTERS[currencyCode]?.format(amount) ?? `${amount} ${currencyCode}`)

  if (currencyCode === 'XOF' || xofEquivalent === undefined) {
    return origin
  }

  return `${origin} (${formatCfa(xofEquivalent)})`
}

/** Strips everything but digits — used while typing into a CFA-masked input field. */
export function unmaskCfaInput(rawValue: string): string {
  return rawValue.replace(/[^\d]/g, '')
}

/** Applies thousands-separator masking as the user types a CFA amount. */
export function maskCfaInput(rawValue: string): string {
  const digitsOnly = unmaskCfaInput(rawValue)
  if (!digitsOnly) {
    return ''
  }
  return CFA_FORMATTER.format(Number.parseInt(digitsOnly, 10))
}
