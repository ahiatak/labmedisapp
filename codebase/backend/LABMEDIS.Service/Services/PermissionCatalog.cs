namespace LABMEDIS.Service.Services;

/// <summary>
/// Static catalogue of every "Module.Action" permission code known to the application
/// (FR-015) and the default permission set granted to each of the 10 built-in LABMEDIS
/// roles. Seeded into the database by PermissionService/RoleService at startup; the
/// database rows (not this class) are the source of truth once an Admin edits role
/// assignments through the UI.
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<(string Code, string Description)> All =
    [
        ("Products.Read", "Consulter le catalogue produits"),
        ("Products.Create", "Créer un produit"),
        ("Products.Update", "Modifier un produit"),
        ("Products.Delete", "Désactiver un produit"),
        ("Suppliers.Read", "Consulter les fournisseurs"),
        ("Suppliers.Create", "Créer un fournisseur"),
        ("Suppliers.Update", "Modifier un fournisseur"),
        ("Suppliers.Delete", "Désactiver un fournisseur"),
        ("Customers.Read", "Consulter les clients"),
        ("Customers.Create", "Créer un client"),
        ("Customers.Update", "Modifier un client"),
        ("Customers.Delete", "Désactiver un client"),
        ("Users.Read", "Consulter les comptes utilisateurs"),
        ("Users.Create", "Créer un compte utilisateur"),
        ("Users.Update", "Modifier un compte utilisateur"),
        ("Users.Delete", "Désactiver un compte utilisateur"),
        ("Roles.Read", "Consulter les rôles"),
        ("Roles.Update", "Modifier les permissions d'un rôle"),
        ("PurchaseOrders.Read", "Consulter les commandes d'achat"),
        ("PurchaseOrders.Create", "Créer une commande d'achat"),
        ("PurchaseOrders.Update", "Modifier une commande d'achat"),
        ("PurchaseOrders.Validate", "Valider une commande d'achat (Direction)"),
        ("Shipments.Read", "Consulter les expéditions"),
        ("Shipments.Create", "Créer une expédition"),
        ("Shipments.Update", "Modifier une expédition"),
        ("Stock.Read", "Consulter le stock"),
        ("Stock.Receive", "Réceptionner un lot en stock"),
        ("Stock.Move", "Mouvementer/allouer/mettre en quarantaine un lot"),
        ("Quality.Read", "Consulter le statut qualité des lots"),
        ("Quality.Release", "Libérer/rejeter un lot en quarantaine"),
        ("Pricing.Read", "Consulter les prix"),
        ("Pricing.Update", "Modifier les profils de pricing et appliquer un prix"),
        ("Sales.Read", "Consulter les commandes de vente"),
        ("Sales.Create", "Créer/confirmer/annuler une commande de vente"),
        ("Sales.Deliver", "Livrer une commande de vente"),
        ("Sales.Invoice", "Facturer une commande de vente (Comptable/Direction)"),
        ("Returns.Read", "Consulter les retours clients"),
        ("Returns.Create", "Initier un retour client"),
        ("Inventory.Read", "Consulter les sessions d'inventaire"),
        ("Inventory.Manage", "Créer une session d'inventaire et saisir les comptages"),
        ("Inventory.Validate", "Valider les écarts d'inventaire"),
        ("Forecast.Read", "Consulter les suggestions de réapprovisionnement"),
        ("Forecast.Convert", "Convertir/rejeter une suggestion de réapprovisionnement"),
        ("Reports.Read", "Consulter les rapports et tableaux de bord"),
        ("Notifications.Read", "Consulter les notifications"),
        ("Compliance.Read", "Consulter la conformité documentaire"),
        ("Compliance.Manage", "Rattacher des pièces justificatives / traiter un rappel")
    ];

    public static readonly IReadOnlyDictionary<string, string[]> DefaultRolePermissions = new Dictionary<string, string[]>
    {
        ["Admin"] = All.Select(p => p.Code).ToArray(),
        ["Direction"] = All.Where(p => p.Code.EndsWith(".Read", StringComparison.Ordinal))
            .Select(p => p.Code)
            .Concat(["PurchaseOrders.Validate", "Pricing.Update", "Users.Read", "Roles.Read", "Sales.Invoice"])
            .ToArray(),
        ["ResponsableAchats"] = ["Products.Read", "Suppliers.Read", "Suppliers.Create", "Suppliers.Update",
            "PurchaseOrders.Read", "PurchaseOrders.Create", "PurchaseOrders.Update", "PurchaseOrders.Validate",
            "Shipments.Read", "Shipments.Create", "Shipments.Update", "Stock.Read", "Forecast.Read", "Forecast.Convert"],
        ["Logistique"] = ["Shipments.Read", "Shipments.Create", "Shipments.Update", "Stock.Read", "Stock.Receive", "Stock.Move",
            "Inventory.Read", "Inventory.Manage", "Inventory.Validate"],
        ["Magasinier"] = ["Stock.Read", "Stock.Receive", "Stock.Move", "Inventory.Read", "Inventory.Manage"],
        ["ResponsableQualite"] = ["Quality.Read", "Quality.Release", "Stock.Read", "Compliance.Read", "Compliance.Manage", "Returns.Read", "Returns.Create"],
        ["Commercial"] = ["Customers.Read", "Customers.Create", "Customers.Update", "Sales.Read",
            "Sales.Create", "Sales.Deliver",
            "Returns.Read", "Returns.Create", "Pricing.Read", "Products.Read"],
        ["Comptable"] = ["Customers.Read", "Sales.Read", "Sales.Invoice", "Returns.Read", "Reports.Read", "Pricing.Read"],
        ["Preparateur"] = ["Stock.Read", "Sales.Read"],
        ["LectureSeule"] = All.Where(p => p.Code.EndsWith(".Read", StringComparison.Ordinal)).Select(p => p.Code).ToArray()
    };
}
