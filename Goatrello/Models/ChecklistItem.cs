using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class ChecklistItem
{
    public int Id { get; set; }
    public int ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
        
    [Required(ErrorMessage = "You cannot leave the title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Item { get; set; } = null!;
    
    [Display(Name = "Checked")]
    public bool IsChecked { get; set; } = false;

    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}