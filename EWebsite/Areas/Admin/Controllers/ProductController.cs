using EWebsite.Data;
using EWebsite.Helper_Files;
using EWebsite.Models;
using EWebsite.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;

namespace EWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public IActionResult ProdView()
        {
            List<ProductBG> list = _db.BGProducts.ToList();
            return View(list);
        }

        // ---------------- CREATE ---------------------

        public IActionResult Create()
        {
            var categoryDropDown = _db.BGCategories.Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            var productVM = new ProductVM
            {
                CategoryList = categoryDropDown,
                ProductBG = new ProductBG()
            };

            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductVM obj, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                obj.CategoryList = _db.BGCategories.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(obj);
            }

            if (file != null && file.Length > 0)
            {
                string wwwRootPath = _env.WebRootPath;
                string folder = Path.Combine(wwwRootPath, "images", "product");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string physicalPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                obj.ProductBG.ImageURL = "/images/product/" + fileName;
            }

            _db.BGProducts.Add(obj.ProductBG);
            _db.SaveChanges();

            TempData["success"] = "Product created successfully";
            return RedirectToAction("ProdView");
        }

        // ---------------- EDIT ---------------------

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var productFromDb = _db.BGProducts.Find(id);
            if (productFromDb == null) return NotFound();

            var categoryDropDown = _db.BGCategories.Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });

            var productVM = new ProductVM
            {
                ProductBG = productFromDb,
                CategoryList = categoryDropDown
            };

            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM obj, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                obj.CategoryList = _db.BGCategories.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
                return View(obj);
            }

            var productFromDb = _db.BGProducts.Find(obj.ProductBG.Id);
            if (productFromDb == null) return NotFound();

            // Update fields
            productFromDb.Title = obj.ProductBG.Title;
            productFromDb.Description = obj.ProductBG.Description;
            productFromDb.Author = obj.ProductBG.Author;
            productFromDb.ISBN = obj.ProductBG.ISBN;
            productFromDb.ListPrice = obj.ProductBG.ListPrice;
            productFromDb.Price = obj.ProductBG.Price;
            productFromDb.Price50 = obj.ProductBG.Price50;
            productFromDb.Price100 = obj.ProductBG.Price100;
            productFromDb.CategoryID = obj.ProductBG.CategoryID;
            productFromDb.MaxQuantityPerOrder = obj.ProductBG.MaxQuantityPerOrder;

            if (file != null && file.Length > 0)
            {
                string wwwRootPath = _env.WebRootPath;
                string folder = Path.Combine(wwwRootPath, "images", "product");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (!string.IsNullOrWhiteSpace(productFromDb.ImageURL))
                {
                    string oldImagePhysical = Path.Combine(
                        wwwRootPath,
                        productFromDb.ImageURL.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                    );

                    if (System.IO.File.Exists(oldImagePhysical)){ System.IO.File.Delete(oldImagePhysical); }
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string physicalPath = Path.Combine(folder, fileName);

                using (var fs = new FileStream(physicalPath, FileMode.Create))
                {
                    file.CopyTo(fs);
                }

                productFromDb.ImageURL = "/images/product/" + fileName;
            }

            _db.SaveChanges();
            TempData["success"] = "Product edited successfully";
            return RedirectToAction("ProdView");
        }

        // ---------------- DELETE ---------------------

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var productFromDb = _db.BGProducts.Find(id);
            if (productFromDb == null) return NotFound();

            return View(productFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var productFromDb = _db.BGProducts.Find(id);
            if (productFromDb == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(productFromDb.ImageURL))
            {
                string wwwRootPath = _env.WebRootPath;

                var imagePhysical = Path.Combine(
                    wwwRootPath,
                    productFromDb.ImageURL.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(imagePhysical))
                    System.IO.File.Delete(imagePhysical);
            }

            _db.BGProducts.Remove(productFromDb);
            _db.SaveChanges();

            TempData["success"] = "Product deleted successfully";
            return RedirectToAction("ProdView");
        }
    }
}