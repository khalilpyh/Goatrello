namespace Goatrello.Models;

public class Report
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsArchived { get; set; }
    public ICollection<ReportFilter> Filters { get; set; } = new HashSet<ReportFilter>();
    public ICollection<ReportOperation> Operations { get; set; } = new HashSet<ReportOperation>();
    public ICollection<ReportResult> Results { get; set; } = new HashSet<ReportResult>();
}