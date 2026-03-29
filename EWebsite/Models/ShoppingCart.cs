using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EWebsite.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }

        public int ProductID { get; set; }

        [ForeignKey(nameof(ProductID))]
        [ValidateNever]
        public ProductBG ProductBG { get; set; }

        [Range(1, 1000, ErrorMessage = "Please enter a value between 1 and 1000")]
        public int Count { get; set; } = 1;

        public string? IdentityUserId { get; set; }

        [ForeignKey(nameof(IdentityUserId))]
        [ValidateNever]
        public IdentityUser IdentityUser { get; set; }
    }
}