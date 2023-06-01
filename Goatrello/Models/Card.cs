using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Goatrello.Models;

public class Card
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    
    [Required(ErrorMessage = "You cannot leave the title blank.")]
    [StringLength(100, ErrorMessage = "Title cannot be more than 100 characters long.")]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }

	public int? UsersId { get; set; }
	public UserList? Users { get; set; }

    public int LabelsId { get; set; }
    public LabelList? Labels { get; set; }
    
    public ICollection<Checklist> Checklists { get; set; } = new HashSet<Checklist>();

    //Due Dates
    [Display(Name = "Due Date")]
	[DataType(DataType.DateTime)]
	[DisplayFormat(ApplyFormatInEditMode = false, DataFormatString = "{0:MM/dd/yy H:mm}")]
	public DateTime? DueDate { get; set; }
    public TimeSpan? Reminder { get; set; } = new TimeSpan();
    [Display(Name = "Due Date Complete")]
    public bool IsDueDateComplete { get; set; } = false;
    
    public ICollection<Attachment> Attachments { get; set; } = new HashSet<Attachment>();
    
    public ICollection<Link> Links { get; set; } = new HashSet<Link>();
    
    public ICollection<Activity> Activities { get; set; } = new HashSet<Activity>();
    
    public ICollection<CustomFieldData> CustomFieldDatas { get; set; } = new HashSet<CustomFieldData>();
    
    [Display(Name = "Archived")]
    public bool IsArchived { get; set; } = false;
}