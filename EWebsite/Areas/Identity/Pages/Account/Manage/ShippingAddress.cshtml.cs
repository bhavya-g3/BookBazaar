using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using EWebsite.Data;
using EWebsite.Models;

namespace EWebsite.Areas.Identity.Pages.Account.Manage
{
    public class ShippingAddressModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ShippingAddressModel(UserManager<IdentityUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required, StringLength(100)]
            public string Name { get; set; }

            [Required, Phone]
            public string Phone { get; set; }

            [Required]
            public string StreetAddress { get; set; }

            [Required]
            public string City { get; set; }

            [Required]
            public string State { get; set; }

            [Required]
            public string PostalCode { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var saved = await _db.BGShippingAddresses
                .FirstOrDefaultAsync(x => x.IdentityUserId == user.Id);

            Input = new InputModel
            {
                Name = saved?.Name,
                Phone = saved?.Phone,
                StreetAddress = saved?.StreetAddress,
                City = saved?.City,
                State = saved?.State,
                PostalCode = saved?.PostalCode
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.GetUserAsync(User);

            var saved = await _db.BGShippingAddresses
                .FirstOrDefaultAsync(x => x.IdentityUserId == user.Id);

            if (saved == null)
            {
                saved = new ShippingAddress
                {
                    IdentityUserId = user.Id,
                    Name = Input.Name,
                    Phone = Input.Phone,
                    StreetAddress = Input.StreetAddress,
                    City = Input.City,
                    State = Input.State,
                    PostalCode = Input.PostalCode
                };

                _db.BGShippingAddresses.Add(saved);
            }
            else
            {
                saved.Name = Input.Name;
                saved.Phone = Input.Phone;
                saved.StreetAddress = Input.StreetAddress;
                saved.City = Input.City;
                saved.State = Input.State;
                saved.PostalCode = Input.PostalCode;

                _db.BGShippingAddresses.Update(saved);
            }

            await _db.SaveChangesAsync();

            StatusMessage = "Shipping address updated.";
            return RedirectToPage();
        }
    }
}