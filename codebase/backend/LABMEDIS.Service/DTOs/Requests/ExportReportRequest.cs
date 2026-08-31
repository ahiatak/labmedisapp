namespace LABMEDIS.Service.DTOs.Requests;

public class ExportReportRequest
{
    /// <summary>stock|sales|pricing|quality.</summary>
    public string ReportType { get; set; } = "stock";

    /// <summary>Pdf|Excel.</summary>
    public string Format { get; set; } = "Excel";
}
