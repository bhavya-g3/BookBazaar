using EWebsite.Data;
using EWebsite.Helper_Files;
using EWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult CatView()
        {
            List<CategoryBG> list = _db.BGCategories.ToList();
            return View(list);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CategoryBG obj)
        {
            if (!ModelState.IsValid) return View();

            _db.BGCategories.Add(obj);
            _db.SaveChanges();
            TempData["success"] = "Category created successfully";
            return RedirectToAction("CatView");
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            CategoryBG? categoryFromDb = _db.BGCategories.Find(id);
            if (categoryFromDb == null) return NotFound();

            return View(categoryFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CategoryBG obj)
        {
            if (!ModelState.IsValid) return View();

            _db.BGCategories.Update(obj);
            _db.SaveChanges();
            TempData["success"] = "Category edited successfully";
            return RedirectToAction("CatView");
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            CategoryBG? categoryFromDb = _db.BGCategories.Find(id);
            if (categoryFromDb == null) return NotFound();

            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            CategoryBG? categoryFromDb = _db.BGCategories.Find(id);
            if (categoryFromDb == null) return NotFound();

            _db.BGCategories.Remove(categoryFromDb);
            _db.SaveChanges();
            TempData["success"] = "Category deleted successfully";
            return RedirectToAction("CatView");
        }
    }
}