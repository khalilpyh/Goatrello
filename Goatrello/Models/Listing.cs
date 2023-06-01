using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Listing
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; } = null!;
    
    [Required(ErrorMessage = "You cannot leave the Title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Title { get; set; }
    
    public ICollection<Card> Cards { get; set; } = new HashSet<Card>();
    
    //List Permissions
    public UserList Observers { get; set; } = null!;
    public UserList HiddenFrom { get; set; } = null!;

    [Display(Name = "Make it Private?")]
    public bool IsPrivate { get; set; } = false;

    //Each Board *should* have only one Template Listing
    [Display(Name = "Make it as Template?")]
    public bool IsTemplate { get; set; } = false;
    
    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}