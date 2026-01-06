using Duende.IdentityModel;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "admin")]
    [SecurityHeaders]
    public class EditModel : PageModel
    {
        private readonly UserManagementService _userService;

        public EditModel(UserManagementService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string SubjectId { get; set; }

            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password (leave blank to keep current)")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userService.GetUserBySubjectId(id);
            if (user == null)
            {
                return NotFound();
            }

            // Map user data to input model
            Input = new InputModel
            {
                SubjectId = user.SubjectId,
                Username = user.UserName,
                // Password is not populated for security reasons
                FullName = user.GetClaims().FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value,
                Email = user.GetClaims().FirstOrDefault(c => c.Type == JwtClaimTypes.Email)?.Value
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userService.GetUserBySubjectId(Input.SubjectId);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Update user properties
                user.UserName = Input.Username;

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(Input.Password))
                {
                    user.Password = Input.Password;
                }

                // Update claims
                // First remove existing name and email claims
                var claimsToRemove = user.GetClaims()
                    .Where(c => c.Type == JwtClaimTypes.Name || c.Type == JwtClaimTypes.Email)
                    .ToList();

                foreach (var claim in claimsToRemove)
                {
                    user.RemoveClaim(claim);
                }

                // Add updated claims if provided
                if (!string.IsNullOrEmpty(Input.FullName))
                {
                    user.AddClaim(new System.Security.Claims.Claim(JwtClaimTypes.Name, Input.FullName));
                }

                if (!string.IsNullOrEmpty(Input.Email))
                {
                    user.AddClaim(new System.Security.Claims.Claim(JwtClaimTypes.Email, Input.Email));
                }

                // Save the updated user
                if (await _userService.UpdateUser(user))
                {
                    StatusMessage = "User has been updated successfully.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to update user.");
                    return Page();
                }
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}