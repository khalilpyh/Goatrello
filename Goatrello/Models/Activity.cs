using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Activity
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    
    [Required(ErrorMessage = "Author is a required field")]
    public User Author { get; set; }= null!;
    public int AuthorId { get; set; }
    
    [Required(ErrorMessage = "You cannot leave the created date blank.")]
    public DateTime Created { get; set; } = new DateTime();

    [Display(Name = "Edited")]
    public bool IsEdited { get; set; } = false;

    //records indicate that an activity is readonly (not a comment)
    [Display(Name = "Record")]
    public bool IsRecord { get; set; } = true;

    public string Content { get; set; } = null!;
}