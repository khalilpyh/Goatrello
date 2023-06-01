using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;



public class Attachment
{
    public int Id { get; set; }
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    
    public int ServerFileId { get; set; }
    [Required(ErrorMessage = "Attachments cannot be blank")]
    public ServerFile ServerFile { get; set; } = null!;
    
    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}