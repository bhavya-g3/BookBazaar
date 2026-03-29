using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EWebsite.ViewModel
{
    public class ShippingVM
    {
        public int? SelectedAddressId { get; set; }

        public List<SelectListItem> AddressOptions { get; set; } = new();

        public bool IsDefault { get; set; }

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
    }
}