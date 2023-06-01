using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Board
{
    public int Id { get; set; }

    [Display(Name = "Title")]
    [Required(ErrorMessage = "You cannot leave the title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Title { get; set; } = null!;
    
    public ICollection<Listing> Listings { get; set; } = new HashSet<Listing>();

    public int LabelsId { get; set; }
    public LabelList Labels { get; set; } = null!;
    
    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
    
    //Board Permissions
    public int AdministratorsId { get; set; }
    public UserList Administrators { get; set; } = null!;
    public int MembersId { get; set; }
    public UserList Members { get; set; } = null!;
    public int ObserversId { get; set; }
    public UserList Observers { get; set; } = null!;

    [Display(Name = "Hidden From")]
    public int HiddenFromId { get; set; }

	[Display(Name = "Hidden From")]
	public UserList HiddenFrom { get; set; } = null!;
    
    [Display(Name = "Make it Private?")]
    public bool IsPrivate { get; set; } = false;
}