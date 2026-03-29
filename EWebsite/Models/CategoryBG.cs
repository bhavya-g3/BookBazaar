using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EWebsite.Models
{
    public class CategoryBG
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Display Name")]
        
        public string Name { get; set; }

        [DisplayName("Display Order")]
        [Range(1, 100, ErrorMessage = "Between 1 to 100 only!")]
        public int DisplayOrder { get; set; }
    }
}