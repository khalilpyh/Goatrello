using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Goatrello.Models;

public class User : IdentityUser<int>
{
    [Display(Name = "First Name")]
    [Required(ErrorMessage = "You cannot leave the first name blank.")]
    [StringLength(50, ErrorMessage = "First name cannot be more than 50 characters long.")]
    public string FirstName { get; set; } = null!;

    [Display(Name = "Last Name")]
    [Required(ErrorMessage = "You cannot leave the last name blank.")]
    [StringLength(100, ErrorMessage = "Last name cannot be more than 100 characters long.")]
    public string LastName { get; set; } = null!;

    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;

    [Display(Name = "Display Name")]
    public string DisplayName => $"{this.FirstName} {this.LastName}";
    
    public Guid? APIKey { get; set; }
    
    public bool IsSiteAdmin { get; set; }
}