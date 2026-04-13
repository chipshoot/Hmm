using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly PasswordPolicyService _passwordPolicyService;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            IApplicationUserRepository userRepository,
            PasswordPolicyService passwordPolicyService,
            IEmailService emailService,
            ILogger<RegisterModel> logger)
        {
            _userRepository = userRepository;
            _passwordPolicyService = passwordPolicyService;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
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
                    email: Input.Email);

                // Send email verification link
                var emailSent = await _emailService.SendVerificationEmailAsync(
                    Input.Email,
                    user.Id,
                    "VerificationToken"); // In a real implementation, generate a proper token

                if (emailSent)
                {
                    _logger.LogInformation("User created successfully and verification email sent to {Email}", Input.Email);
                    TempData["SuccessMessage"] = "Registration successful. Please check your email to verify your account.";
                    return RedirectToPage("/Account/Login");
                }
                else
                {
                    _logger.LogWarning("User created but failed to send verification email to {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Registration successful but we couldn't send the verification email. Please contact support.");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                return Page();
            }
        }
    }
}