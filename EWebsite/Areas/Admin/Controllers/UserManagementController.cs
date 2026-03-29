using EWebsite.Data;
using EWebsite.Helper_Files;
using EWebsite.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Role_Admin)]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly AdminSettings _adminSettings;

        public UserManagementController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db,
            IOptions<AdminSettings> adminOptions)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _adminSettings = adminOptions.Value;
        }

        private bool IsProtectedAdmin(string? email, string? userName)
        {
            var key = _adminSettings.PrimaryAdminIdentifier;
            if (string.IsNullOrWhiteSpace(key)) return false;

            return string.Equals(email, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(userName, key, StringComparison.OrdinalIgnoreCase);
        }

        // GET: /Admin/UserManagement/UserManage
        public async Task<IActionResult> UserManage()
        {
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync();

            var userRoleLinks = await _db.UserRoles.ToListAsync();
            var allRoles = await _db.Roles.ToListAsync();

            var roleNameById = allRoles.ToDictionary(
                r => r.Id,
                r => r.Name,
                StringComparer.Ordinal);

            var userRolesMap = userRoleLinks
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ur => roleNameById.TryGetValue(ur.RoleId, out var name) ? name : null)
                          .Where(n => !string.IsNullOrEmpty(n))
                          .ToList(),
                    StringComparer.Ordinal);

            var vms = users.Select(u =>
            {
                userRolesMap.TryGetValue(u.Id, out var rolesForUser);
                rolesForUser ??= new List<string>();

                var rolesDisplay = rolesForUser.Any()
                    ? string.Join(", ", rolesForUser)
                    : "Customer";

                var isAdmin = rolesForUser.Contains(Roles.Role_Admin, StringComparer.OrdinalIgnoreCase);

                var isProtected = IsProtectedAdmin(u.Email, u.UserName);

                return new UserListItemVM
                {
                    Id = u.Id,
                    Email = u.Email,
                    UserName = u.UserName,
                    RolesDisplay = rolesDisplay,
                    IsAdmin = isAdmin,
                    IsProtected = isProtected
                };
            })
            .OrderBy(vm => vm.Email)
            .ToList();

            return View(vms);
        }

        // GET: /Admin/UserManagement/Details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var ship = await _db.BGShippingAddresses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdentityUserId == id);

            var vm = new UserDetailsVM
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                RoleNames = roles.ToList(),
                Name = ship?.Name,
                Phone = ship?.Phone,
                StreetAddress = ship?.StreetAddress,
                City = ship?.City,
                State = ship?.State,
                PostalCode = ship?.PostalCode,
                IsProtected = IsProtectedAdmin(user.Email, user.UserName)
            };

            return View(vm);
        }

        // GET: /Admin/UserManagement/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserVM());
        }

        // POST: /Admin/UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email is required.");

            if (string.IsNullOrWhiteSpace(model.UserName))
                ModelState.AddModelError(nameof(model.UserName), "User name is required.");

            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Password is required.");

            if (model.Password != model.ConfirmPassword)
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Passwords do not match.");

            if (!ModelState.IsValid)
                return View(model);

            if (!await _roleManager.RoleExistsAsync(Roles.Role_Customer))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Role_Customer));

            if (model.MakeAdmin && !await _roleManager.RoleExistsAsync(Roles.Role_Admin))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Role_Admin));

            var emailExists = await _userManager.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email already exists.");
                return View(model);
            }
            var usernameExists = await _userManager.Users.AnyAsync(u => u.UserName == model.UserName);
            if (usernameExists)
            {
                ModelState.AddModelError(nameof(model.UserName), "Username already exists.");
                return View(model);
            }
            var newUser = new IdentityUser
            {
                Email = model.Email,
                UserName = model.UserName,
                EmailConfirmed=true
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(model);
            }

            if (model.MakeAdmin)
            {
                await _userManager.AddToRoleAsync(newUser, Roles.Role_Admin);
            }
            else
            {
                await _userManager.AddToRoleAsync(newUser, Roles.Role_Customer);
            }

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Details), new {id=newUser.Id});
        }

        // POST: /Admin/UserManagement/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["error"] = "Invalid user.";
                return RedirectToAction(nameof(UserManage));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["error"] = "User not found.";
                return RedirectToAction(nameof(UserManage));
            }

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == userId)
            {
                TempData["error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(UserManage));
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Role_Admin);
            if (isAdmin)
            {
                var totalAdmins = await (from ur in _db.UserRoles
                                         join r in _db.Roles on ur.RoleId equals r.Id
                                         where r.Name == Roles.Role_Admin
                                         select ur.UserId)
                                         .Distinct()
                                         .CountAsync();

                if (totalAdmins <= 1)
                {
                    TempData["error"] = "You cannot delete the last remaining admin.";
                    return RedirectToAction(nameof(UserManage));
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["success"] = "User deleted successfully.";
            }
            else
            {
                TempData["error"] = string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(UserManage));
        }

        // POST: /Admin/UserManagement/PromoteToAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!await _roleManager.RoleExistsAsync(Roles.Role_Admin))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Role_Admin));

            if (!await _roleManager.RoleExistsAsync(Roles.Role_Customer))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Role_Customer));

            var result = await _userManager.AddToRoleAsync(user, Roles.Role_Admin);
            await _userManager.RemoveFromRoleAsync(user, Roles.Role_Customer);

            if (!result.Succeeded)
            {
                TempData["error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            TempData["success"] = "User promoted to Admin.";
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // POST: /Admin/UserManagement/DemoteToCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteToCustomer(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == userId)
            {
                TempData["error"] = "You cannot demote your own account.";
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!await _roleManager.RoleExistsAsync(Roles.Role_Customer))
                await _roleManager.CreateAsync(new IdentityRole(Roles.Role_Customer));

            var adminsCount = await (from ur in _db.UserRoles
                                     join r in _db.Roles on ur.RoleId equals r.Id
                                     where r.Name == Roles.Role_Admin
                                     select ur.UserId)
                                     .Distinct()
                                     .CountAsync();

            var isUserAdmin = await _userManager.IsInRoleAsync(user, Roles.Role_Admin);
            if (isUserAdmin && adminsCount <= 1)
            {
                TempData["error"] = "Cannot demote the last admin.";
                return RedirectToAction(nameof(Details), new { id = userId });
            }

            await _userManager.RemoveFromRoleAsync(user, Roles.Role_Admin);
            await _userManager.AddToRoleAsync(user, Roles.Role_Customer);

            TempData["success"] = "User demoted to Customer.";
            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}