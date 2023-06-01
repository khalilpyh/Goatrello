namespace Goatrello.Models;

public class LabelList
{
    public int Id { get; set; }
    public ICollection<Label> Labels { get; set; } = new HashSet<Label>();
}