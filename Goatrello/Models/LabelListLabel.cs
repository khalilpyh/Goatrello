namespace Goatrello.Models;

public class LabelListLabel
{
    public int ListId { get; set; }
    public LabelList List { get; set; } = null!;
    public int LabelId { get; set; }
    public Label Label { get; set; } = null!;
}