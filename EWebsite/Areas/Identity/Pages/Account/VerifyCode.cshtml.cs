using EWebsite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EWebsite.Areas.Identity.Pages.Account
{
    public class VerifyCodeModel : PageModel
    {
        private readonly IResetCodeStore _codeStore;
        private readonly UserManager<IdentityUser> _userManager;

        public VerifyCodeModel(IResetCodeStore codeStore, UserManager<IdentityUser> userManager)
        {
            _codeStore = codeStore;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required, StringLength(6, MinimumLength = 6, ErrorMessage = "Enter the 6-digit code.")]
        public string Code { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var ok = await _codeStore.ValidateAsync(Email, Code);
            if (!ok)
            {
                ModelState.AddModelError("Code", "Invalid or expired code.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                // Do not reveal anything; go to the same end state
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Generate the official Identity password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Encode for query string (Identity ResetPassword expects 'code' param)
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Redirect into the existing ResetPassword page
            return RedirectToPage("./ResetPassword", new { email = Email, code = encoded });
        }
    }
}