using System.ComponentModel.DataAnnotations;
using System.Text;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Hmm.Idp.Pages.Account
{
    /// <summary>
    /// Lets a user request another verification email — used when the original
    /// link was missed, the token expired, or SMTP failed at registration time.
    /// </summary>
    public class ResendConfirmationModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ResendConfirmationModel> _logger;

        public ResendConfirmationModel(
            IApplicationUserRepository userRepository,
            IEmailService emailService,
            ILogger<ResendConfirmationModel> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool Submitted { get; private set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public IActionResult OnGet([FromQuery] string email)
        {
            // Pre-fill the email if one was passed via the link in
            // RegisterConfirmation — saves the user retyping it.
            if (!string.IsNullOrWhiteSpace(email))
            {
                Input.Email = email;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // We always render the same "if such an account exists, we sent a link"
            // message, regardless of whether the email is on file or already verified.
            // This avoids account enumeration via the resend endpoint.
            var user = await _userRepository.FindByEmailAsync(Input.Email);
            if (user is { EmailConfirmed: false })
            {
                try
                {
                    var rawToken = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { userId = user.Id, token = encodedToken },
                        protocol: Request.Scheme,
                        host: Request.Host.Value);

                    var sent = await _emailService.SendVerificationEmailAsync(Input.Email, callbackUrl);
                    if (!sent)
                    {
                        _logger.LogWarning("Resend confirmation: SMTP failed for {Email}", Input.Email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Resend confirmation failed for {Email}", Input.Email);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Resend confirmation requested for {Email} — no unconfirmed account on file",
                    Input.Email);
            }

            Submitted = true;
            return Page();
        }
    }
}
