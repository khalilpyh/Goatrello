using Newtonsoft.Json.Linq;

namespace Goatrello.Models;

public class ReportResult
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    public DateTime Created { get; set; }
    public string Result { get; set; }
}