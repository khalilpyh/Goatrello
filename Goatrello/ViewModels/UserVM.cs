using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace Goatrello.ViewModels
{
    public class UserVM
    {
        public int Id { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Display(Name = "Formal Name")]
        public string DisplayName { get; set; }

        [Display(Name = "Admin Status")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Archive Status")]
        public bool IsArchived { get; set; }
    }
}
