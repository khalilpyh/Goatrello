namespace Goatrello.Models;
using System.ComponentModel.DataAnnotations;

public enum FieldDataType
{
    Int,
    Float,
    String
}

public class CustomField
{
    public int Id { get; set; }

    [Required(ErrorMessage = "You cannot leave the Name blank.")]
    [StringLength(30, ErrorMessage = "Title cannot be more than 30 characters long.")]
    public string Name { get; set; }

    [Display(Name = "Type")]
    public FieldDataType FieldDataType { get; set; }
}