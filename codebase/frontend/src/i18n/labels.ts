/**
 * Centralized French UI strings for elements shared across pages (navigation, generic
 * actions). Page-specific copy lives alongside each page as it is implemented — this file
 * only holds the vocabulary the app shell (layout, nav, common buttons) needs.
 */
export const labels = {
  appName: 'LABMEDIS',
  nav: {
    dashboard: 'Tableau de bord',
    products: 'Produits',
    suppliers: 'Fournisseurs',
    customers: 'Clients',
    purchaseOrders: "Commandes d'achat",
    shipments: 'Expéditions',
    stockReception: 'Réception stock',
    warehouse: 'Entrepôt',
    quality: 'Contrôle qualité',
    pricing: 'Tarification',
    saleOrders: 'Commandes de vente',
    returns: 'Retours clients',
    inventory: 'Inventaire',
    forecast: 'Réapprovisionnement',
    reports: 'Rapports',
    compliance: 'Conformité',
    admin: 'Administration',
  },
  actions: {
    create: 'Créer',
    edit: 'Modifier',
    save: 'Enregistrer',
    cancel: 'Annuler',
    delete: 'Désactiver',
    confirm: 'Confirmer',
    logout: 'Déconnexion',
  },
  states: {
    loading: 'Chargement…',
    empty: 'Aucune donnée',
    error: "Une erreur est survenue. Veuillez réessayer.",
    retry: 'Réessayer',
  },
} as const
