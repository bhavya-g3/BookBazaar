using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace EWebsite.Models
{
    public class ShippingAddress
    {
        public int Id { get; set; }

        [Required]
        public string IdentityUserId { get; set; }

        [ForeignKey(nameof(IdentityUserId))]
        public IdentityUser IdentityUser { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; }

        [Required, StringLength(200)]
        public string StreetAddress { get; set; }

        [Required, StringLength(100)]
        public string City { get; set; }

        [Required, StringLength(100)]
        public string State { get; set; }

        [Required, StringLength(20)]
        public string PostalCode { get; set; }

        public bool IsDefault { get; set; }
    }
}