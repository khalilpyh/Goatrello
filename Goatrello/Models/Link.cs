using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Link
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;

    [Required(ErrorMessage = "Link cannot be blank")]
    public Uri Uri { get; set; } = null!;
    
    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}