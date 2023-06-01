namespace Goatrello.Models;

public class CustomFieldData
{
    public int CardId { get; set; }
    public Card Card { get; set; }
    public int CustomFieldId { get; set; }
    public CustomField CustomField { get; set; }
    public string Value { get; set; }
}