namespace LABMEDIS.Service.DTOs.Responses;

public class DirectionDashboardResponse
{
    public string TotalRevenueCfa { get; set; } = "0";

    public string TotalMarginCfa { get; set; } = "0";

    public string StockValueCfa { get; set; } = "0";

    public int StockoutProductCount { get; set; }
}

public class StockReportResponse
{
    public int TotalAvailable { get; set; }

    public int TotalReserved { get; set; }

    public int TotalQuarantine { get; set; }

    public int TotalExpired { get; set; }

    public int SlowMovingProductCount { get; set; }
}

public class ExpiringLotReportLine
{
    public Guid LotId { get; set; }

    public string? ProductDesignation { get; set; }

    public string InternalLotNumber { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public int RemainingQuantity { get; set; }
}

public class SlowMovingProductReportLine
{
    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public DateTime? LastMovementAt { get; set; }

    public int DaysSinceLastMovement { get; set; }
}

public class SalesReportLine
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string RevenueCfa { get; set; } = "0";
}

public class SalesReportResponse
{
    public string TotalRevenueCfa { get; set; } = "0";

    public string ReturnRatePercent { get; set; } = "0";

    public List<SalesReportLine> ByCustomer { get; set; } = [];

    public List<SalesReportLine> ByProduct { get; set; } = [];
}

public class PricingReportLine
{
    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public string TheoreticalMarginCfa { get; set; } = "0";

    public string RealMarginCfa { get; set; } = "0";

    public string PriceGapCfa { get; set; } = "0";
}

public class QualityReportResponse
{
    public int QuarantineCount { get; set; }

    public int NonConformeCount { get; set; }

    public List<StockLotResponse> Lots { get; set; } = [];
}
