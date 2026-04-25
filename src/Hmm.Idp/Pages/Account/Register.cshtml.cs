using System.Text;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
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
                var (isValid, policyErrors) = _passwordPolicyService.ValidatePassword(Input.Password);
                if (!isValid)
                {
                    foreach (var error in policyErrors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    return Page();
                }

                var user = await _userRepository.CreateUserAsync(
                    Input.Username,
                    Input.Password,
                    email: Input.Email);

                // Generate a real Identity-issued token, base64url-encode it so that
                // the `+` / `/` / `=` chars survive the round-trip through the URL,
                // and build the callback link off the current request scheme/host so
                // we don't have to keep EmailSettings.ApplicationUrl in sync with the
                // IDP's own IssuerUri.
                var rawToken = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = user.Id, token = encodedToken },
                    protocol: Request.Scheme,
                    host: Request.Host.Value);

                var emailSent = await _emailService.SendVerificationEmailAsync(Input.Email, callbackUrl);

                if (emailSent)
                {
                    _logger.LogInformation("User created and verification email queued for {Email}", Input.Email);
                }
                else
                {
                    _logger.LogWarning("User created but verification email failed to send to {Email} — user can request a resend", Input.Email);
                }

                // Always land on the same confirmation-pending screen — never expose
                // whether the email succeeded (avoids account-enumeration via send-failure timing).
                return RedirectToPage("/Account/RegisterConfirmation", new { email = Input.Email });
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
