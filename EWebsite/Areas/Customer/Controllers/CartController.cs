using EWebsite.Data;
using EWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace EWebsite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET /Customer/Cart
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var lines = await _db.BGShoppingCart
                .Include(c => c.ProductBG)
                .Where(c => c.IdentityUserId == user.Id)
                .ToListAsync();

            // Remove cart rows where product was deleted
            var removed = false;
            foreach (var l in lines.Where(l => l.ProductBG == null).ToList())
            {
                _db.BGShoppingCart.Remove(l);
                removed = true;
            }

            // Quantity rules check
            var changed = false;
            foreach (var l in lines.Where(l => l.ProductBG != null))
            {
                var max = l.ProductBG.MaxQuantityPerOrder;

                if (max.HasValue && max.Value <= 0)
                {
                    _db.BGShoppingCart.Remove(l);
                    changed = true;
                    continue;
                }

                if (max.HasValue && l.Count > max.Value)
                {
                    l.Count = max.Value;
                    changed = true;
                }
            }

            if (removed || changed)
            {
                await _db.SaveChangesAsync();

                if (changed)
                {
                    TempData["Warning"] = "Some item quantities were adjusted to the latest per-order limits.";
                }
            }

            var vm = new CartIndexVM
            {
                Lines = lines
                    .Where(l => l.ProductBG != null)
                    .Select(l => new CartLineVM
                    {
                        ProductId = l.ProductID,
                        Title = l.ProductBG.Title,
                        ImageURL = string.IsNullOrWhiteSpace(l.ProductBG.ImageURL)
                                    ? "/images/product/no-image.png"
                                    : l.ProductBG.ImageURL,
                        UnitPrice = UnitPriceFor(l.ProductBG, l.Count),
                        Count = l.Count,
                        MaxQuantityPerOrder = l.ProductBG.MaxQuantityPerOrder
                    })
                    .ToList()
            };

            vm.Total = vm.Lines.Sum(x => x.UnitPrice * x.Count);

            return View(vm);
        }


        // POST /Customer/Cart/AddToCart
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int count = 1, string? returnUrl = null)
        {
            if (count < 1) count = 1;

            var product = await _db.Set<ProductBG>().FirstOrDefaultAsync(p => p.Id == productId);
            var user = await _userManager.GetUserAsync(User);

            var existing = await _db.BGShoppingCart
                .FirstOrDefaultAsync(c => c.IdentityUserId == user.Id && c.ProductID == productId);

            int? max = product.MaxQuantityPerOrder;

            if (existing == null)
            {
                if (max.HasValue && count > max.Value)
                {
                    count = max.Value;
                    TempData["Warning"] = $"Max allowed per order is {max.Value}. Quantity adjusted.";
                }

                _db.BGShoppingCart.Add(new ShoppingCart
                {
                    IdentityUserId = user.Id,
                    ProductID = productId,
                    Count = count
                });
            }
            else
            {
                if (max.HasValue)
                {
                    int allowedToAdd = Math.Max(0, max.Value - existing.Count);

                    if (allowedToAdd <= 0)
                    {
                        TempData["Warning"] = $"You already have the maximum allowed quantity ({max.Value}) for this product.";
                    }
                    else
                    {
                        int toAdd = Math.Min(count, allowedToAdd);

                        if (toAdd < count)
                        {
                            TempData["Warning"] = $"Max allowed per order is {max.Value}. Added {toAdd} instead of {count}.";
                        }

                        existing.Count += toAdd;
                    }
                }
                else
                {
                    existing.Count += count;
                }
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Product added to cart.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }


        // POST /Customer/Cart/Remove
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var user = await _userManager.GetUserAsync(User);

            var line = await _db.BGShoppingCart
                .FirstOrDefaultAsync(c => c.IdentityUserId == user.Id && c.ProductID == productId);

            if (line != null)
            {
                _db.BGShoppingCart.Remove(line);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        private double UnitPriceFor(ProductBG product, int qty)
        {
            if (qty >= 100) return product.Price100;
            if (qty >= 50) return product.Price50;
            return product.Price;
        }


        // ============================
        //   VIEW MODELS
        // ============================

        public class CartIndexVM
        {
            public List<CartLineVM> Lines { get; set; } = new();
            public double Total { get; set; }
        }

        public class CartLineVM
        {
            public int ProductId { get; set; }
            public string Title { get; set; }
            public string ImageURL { get; set; }
            public double UnitPrice { get; set; }
            public int Count { get; set; }
            public double Subtotal => UnitPrice * Count;
            public int? MaxQuantityPerOrder { get; set; }
        }
    }
}