using EWebsite.Data;
using EWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EWebsite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // ---------------------------------------------------------
        // Home Page
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            var BGProducts = _db.BGProducts
                                .OrderByDescending(p => p.Id)
                                .ToList();

            return View(BGProducts);
        }

        // ---------------------------------------------------------
        // Product Details Page
        // ---------------------------------------------------------
        public IActionResult Details(int id)
        {
            var product = _db.BGProducts
                             .Include(p => p.CategoryBG)
                             .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // ---------------------------------------------------------
        // Privacy Page
        // ---------------------------------------------------------
        public IActionResult Privacy()
        {
            return View();
        }

        // ---------------------------------------------------------
        // Error Page
        // ---------------------------------------------------------
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}