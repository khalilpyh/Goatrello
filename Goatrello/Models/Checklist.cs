using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Checklist
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    
    [Required(ErrorMessage = "You cannot leave the title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Title { get; set; } = null!;

    [Display(Name = "Items")]
    [BsonElement("Items")]
    public ICollection<ChecklistItem>? ChecklistItems { get; set; } = new HashSet<ChecklistItem>();

    [Display(Name = "Show Hidden")]
    public bool IsShowHidden { get; set; } = false;
    
    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}