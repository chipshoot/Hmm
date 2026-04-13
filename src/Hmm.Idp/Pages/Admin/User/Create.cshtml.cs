// Pages/UserManagement/Create.cshtml.cs
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "Administrator")]
    [SecurityHeaders]
    public class CreateModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly PasswordPolicyService _passwordPolicyService;
        private readonly IEmailService _emailService;

        public CreateModel(
            IApplicationUserRepository userRepository,
            PasswordPolicyService passwordPolicyService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _passwordPolicyService = passwordPolicyService;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Enable Two-Factor Authentication")]
            public bool EnableTwoFactorAuth { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Validate password against policy
                var (isValid, policyErrors) = _passwordPolicyService.ValidatePassword(Input.Password);
                if (!isValid)
                {
                    foreach (var error in policyErrors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    return Page();
                }

                // Create the user
                var user = await _userRepository.CreateUserAsync(
                    Input.Username,
                    Input.Password,
                    firstName: Input.FirstName,
                    lastName: Input.LastName,
                    email: Input.Email);

                // Send email verification link
                await _emailService.SendVerificationEmailAsync(
                    Input.Email,
                    user.Id,
                    "VerificationToken"); // In a real implementation, generate a proper token

                TempData["SuccessMessage"] = "User created successfully. A verification email has been sent.";
                return RedirectToPage("./Index");
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
