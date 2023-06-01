using Goatrello.Services;
using Newtonsoft.Json.Linq;

namespace Goatrello.Models;

public class ReportOperation
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report Report { get; set; }
    public string Name { get; set; }
    public ReportOperationType Type { get; set; } 
    public string Values { get; set; }
}