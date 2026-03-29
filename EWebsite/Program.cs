using EWebsite;
using EWebsite.Data;
using EWebsite.Helper_Files;
using EWebsite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------
// Database
// -------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// -------------------------------------------------------------
// Identity
// -------------------------------------------------------------
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// -------------------------------------------------------------
// Cookie settings (login / logout / denied)
// -------------------------------------------------------------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// -------------------------------------------------------------
// Razor + MVC
// -------------------------------------------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// -------------------------------------------------------------
// Custom Services + Options
// -------------------------------------------------------------
builder.Services.Configure<FileResetCodeStore.Options>(builder.Configuration.GetSection("ResetLog"));
builder.Services.AddSingleton<IResetCodeStore, FileResetCodeStore>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("Admin"));

var app = builder.Build();

// -------------------------------------------------------------
// Middleware
// -------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

// -------------------------------------------------------------
// Seed Admin & Roles
// -------------------------------------------------------------
await SeedAdminAsync(app);

app.Run();


// =================================================================
// ADMIN SEEDING LOGIC
// =================================================================

async Task SeedAdminAsync(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminSettings>>();

    string[] roles = new[] { Roles.Role_Admin, Roles.Role_Customer };

    // Create roles if not exist
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Admin credentials from appsettings.json OR fallback
    var adminEmail = adminOptions.Value.PrimaryAdminIdentifier ?? "admin.admin@gmail.com";
    var adminPassword = "Aa1234#";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new Exception("Failed to create default admin: " + errors);
        }
    }

    // Add admin to Admin Role
    if (!await userManager.IsInRoleAsync(adminUser, Roles.Role_Admin))
    {
        await userManager.AddToRoleAsync(adminUser, Roles.Role_Admin);
    }
}