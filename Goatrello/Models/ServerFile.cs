using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class ServerFile
{
    public int Id { get; set; }

    [Display(Name = "Title")]
    [Required(ErrorMessage = "You cannot leave the title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Title { get; set; } = null!;

    [Display(Name = "ServerFile Type")]
    [StringLength(20, ErrorMessage = "ServerFile type cannot be more than 20 characters long.")]
    public string? FileType { get; set; }
    
    [Display(Name = "Thumbnail Id")]
    public Guid? ThumbnailGuid { get; set; }
    
    [Display(Name = "File Id")]
    public Guid FileGuid { get; set; } = new Guid();

    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
    
    [Display(Name = "Created By")]
    public int UserId { get; set; }

    public User User { get; set; } = null!;
}