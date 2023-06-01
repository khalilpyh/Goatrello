using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Label
{
    public int Id { get; set; }

    [Required(ErrorMessage = "You cannot leave the title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Title { get; set; } = null!;
    
    [Required(ErrorMessage = "Color is required")]
    [StringLength(6, ErrorMessage = "Color must be provided as hexidecimal value", MinimumLength = 6)]
    public string Color { get; set; } = null!;

    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}