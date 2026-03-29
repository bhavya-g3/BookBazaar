//Path : Areas/Customer/Controllers/CheckoutController.cs
using EWebsite.Data;
using EWebsite.Models;
using EWebsite.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EWebsite.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---------------------------------------------------------
        // GET: /Customer/Checkout/Shipping
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Shipping()
        {
            var user = await _userManager.GetUserAsync(User);

            var addresses = await _db.BGShippingAddresses
                .Where(sa => sa.IdentityUserId == user.Id)
                .OrderByDescending(sa => sa.Id)
                .ThenBy(sa => sa.IsDefault)
                .ToListAsync();

            var vm = new ShippingVM();

            foreach (var a in addresses)
            {
                vm.AddressOptions.Add(new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Name}, {a.StreetAddress}, {a.City} ({a.PostalCode})" + (a.IsDefault ? " - Default" : ""),
                    Selected = a.IsDefault
                });
            }

            vm.AddressOptions.Add(new SelectListItem
            {
                Value = "-1",
                Text = "+ Add new address",
                Selected = addresses.Any() == false
            });

            if (addresses.Any())
            {
                var def = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.First();
                vm.SelectedAddressId = def.Id;

                vm.Name = def.Name;
                vm.Phone = def.Phone;
                vm.StreetAddress = def.StreetAddress;
                vm.City = def.City;
                vm.State = def.State;
                vm.PostalCode = def.PostalCode;
                vm.IsDefault = def.IsDefault;
            }
            else
            {
                vm.SelectedAddressId = -1;
            }

            return View(vm);
        }

        // ---------------------------------------------------------
        // POST: /Customer/Checkout/Shipping
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(ShippingVM vm)
        {
            var user = await _userManager.GetUserAsync(User);
            bool addingNew = vm.SelectedAddressId.HasValue && vm.SelectedAddressId.Value == -1;

            if (addingNew)
            {
                if (!ModelState.IsValid)
                {
                    await PopulateAddressDropdown(user.Id, vm);
                    return View(vm);
                }

                var newAddress = new ShippingAddress
                {
                    IdentityUserId = user.Id,
                    Name = vm.Name,
                    Phone = vm.Phone,
                    StreetAddress = vm.StreetAddress,
                    City = vm.City,
                    State = vm.State,
                    PostalCode = vm.PostalCode,
                    IsDefault = false
                };

                _db.BGShippingAddresses.Add(newAddress);
                await _db.SaveChangesAsync();

                if (vm.IsDefault)
                {
                    await SetDefaultAsync(user.Id, newAddress.Id);
                }

                TempData["Success"] = "New address saved.";
                TempData["SelectedShippingAddressId"] = newAddress.Id;

                return RedirectToAction(nameof(Shipping));
            }
            else
            {
                if (!vm.SelectedAddressId.HasValue)
                {
                    await PopulateAddressDropdown(user.Id, vm);
                    ModelState.AddModelError("", "Please select an address or choose Add new.");
                    return View(vm);
                }

                var selected = await _db.BGShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.Id == vm.SelectedAddressId.Value && sa.IdentityUserId == user.Id);

                if (selected == null)
                {
                    await PopulateAddressDropdown(user.Id, vm);
                    ModelState.AddModelError("", "Selected address not found.");
                    return View(vm);
                }

                TempData["SelectedShippingAddressId"] = selected.Id;

                return RedirectToAction(nameof(Review));
            }
        }

        // ---------------------------------------------------------
        // Helper: Populate dropdown
        // ---------------------------------------------------------
        private async Task PopulateAddressDropdown(string userId, ShippingVM vm)
        {
            var addresses = await _db.BGShippingAddresses
                .Where(sa => sa.IdentityUserId == userId)
                .OrderByDescending(sa => sa.Id)
                .ThenBy(sa => sa.IsDefault)
                .ToListAsync();

            vm.AddressOptions = addresses.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Name}, {a.StreetAddress}, {a.City} ({a.PostalCode})" + (a.IsDefault ? " - Default" : ""),
                Selected = vm.SelectedAddressId.HasValue && vm.SelectedAddressId.Value == a.Id
            }).ToList();

            vm.AddressOptions.Add(new SelectListItem { Value = "-1", Text = "+ Add new address" });
        }

        // ---------------------------------------------------------
        // Helper: Set default address
        // ---------------------------------------------------------
        private async Task SetDefaultAsync(string userId, int addressId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            // clear any existing default for this user
            await _db.BGShippingAddresses
                .Where(a => a.IdentityUserId == userId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

            // set the specified address as default
            await _db.BGShippingAddresses
                .Where(a => a.IdentityUserId == userId && a.Id == addressId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, true));

            await tx.CommitAsync();
        }

        // ---------------------------------------------------------
        // GET: /Customer/Checkout/Review
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Review()
        {
            var user = await _userManager.GetUserAsync(User);

            var shipping = await _db.BGShippingAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(sa => sa.IdentityUserId == user.Id);

            var cartlines = await _db.BGShoppingCart
                .Include(c => c.ProductBG)
                .Where(c => c.IdentityUserId == user.Id)
                .ToListAsync();

            var total = cartlines.Sum(l =>
            {
                var price = l.Count >= 100 ? l.ProductBG.Price100 : l.Count >= 50 ? l.ProductBG.Price50 : l.ProductBG.Price;
                return price * l.Count;
            });
            ViewBag.Total = total;
            ViewBag.Shipping = shipping;
            ViewData["Title"] = "Order Review (Stub)";
            return View();
        }

        // GET: /Customer/Checkout/EditAddress/5
        [HttpGet]
        public async Task<IActionResult> EditAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var addr = await _db.BGShippingAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.IdentityUserId == user.Id);

            if (addr == null)
            {
                TempData["Error"] = "Address not found.";
                return RedirectToAction(nameof(Shipping));
            }

            var vm = new ShippingVM
            {
                SelectedAddressId = id,
                Name = addr.Name,
                Phone = addr.Phone,
                StreetAddress = addr.StreetAddress,
                City = addr.City,
                State = addr.State,
                PostalCode = addr.PostalCode,
                IsDefault = addr.IsDefault
            };

            ViewData["Title"] = "Edit Address";
            return View("EditAddress", vm);
        }

        // POST: /Customer/Checkout/EditAddress/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(int id, ShippingVM vm)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Address";
                return View("EditAddress", vm);
            }

            var addr = await _db.BGShippingAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.IdentityUserId == user.Id);

            if (addr == null)
            {
                TempData["Error"] = "Address not found.";
                return RedirectToAction(nameof(Shipping));
            }

            addr.Name = vm.Name;
            addr.Phone = vm.Phone;
            addr.StreetAddress = vm.StreetAddress;
            addr.City = vm.City;
            addr.State = vm.State;
            addr.PostalCode = vm.PostalCode;

            if (!vm.IsDefault)
            {
                addr.IsDefault = false;
            }

            await _db.SaveChangesAsync();

            if (vm.IsDefault)
            {
                await SetDefaultAsync(user.Id, addr.Id);
            }

            TempData["Success"] = "Address updated.";
            TempData["SelectedShippingAddressId"] = addr.Id;

            return RedirectToAction(nameof(Shipping));
        }

        // ---------------------------------------------------------
        // GET: /Customer/Checkout/DeleteAddress/5
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> DeleteAddress(int SelectedAddressId)
        {
            var userId = _userManager.GetUserId(User);

            var address = await _db.BGShippingAddresses
                .FirstOrDefaultAsync(a => a.Id == SelectedAddressId && a.IdentityUserId == userId);

            if (address == null)
            {
                TempData["error"] = "Address not found.";
                return RedirectToAction("Shipping", new { area = "Customer" });
            }

            return View("DeleteAddress", address);
        }

        // ---------------------------------------------------------
        // POST: DeleteAddress
        // ---------------------------------------------------------
        [HttpPost, ActionName("DeleteAddress")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddressPOST(int id)
        {
            var userId = _userManager.GetUserId(User);

            var address = await _db.BGShippingAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.IdentityUserId == userId);

            if (address == null)
            {
                TempData["error"] = "Address not found.";
                return RedirectToAction("Shipping", new { area = "Customer" });
            }

            _db.BGShippingAddresses.Remove(address);
            await _db.SaveChangesAsync();

            TempData["success"] = "Address deleted.";
            return RedirectToAction("Shipping", new { area = "Customer" });
        }

    }
}