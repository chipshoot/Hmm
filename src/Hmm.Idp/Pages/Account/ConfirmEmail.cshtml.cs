using System.Text;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Hmm.Idp.Pages.Account
{
    /// <summary>
    /// Lands the user when they click the verification link emailed at
    /// registration time (POST /api/account/register from the mobile app, or
    /// the admin-driven /Admin/User/Create flow). Validates the
    /// base64url-encoded token and flips <c>EmailConfirmed = true</c>.
    /// </summary>
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly ILogger<ConfirmEmailModel> _logger;
        private readonly EmailSettings _emailSettings;

        public ConfirmEmailModel(
            IApplicationUserRepository userRepository,
            IOptions<EmailSettings> emailSettings,
            ILogger<ConfirmEmailModel> logger)
        {
            _userRepository = userRepository;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public bool Success { get; private set; }
        public string Message { get; private set; } = string.Empty;

        /// <summary>
        /// True when the verification link was issued to a user who registered
        /// via the mobile API (source=mobile). The view suppresses the
        /// "Sign in" CTA in that case — mobile users should switch back to
        /// the installed app, not log into a web page.
        /// </summary>
        public bool IsFromMobile { get; private set; }

        /// <summary>
        /// Where the "Sign in" CTA points after successful verification, when
        /// shown (i.e. when the user did NOT register via the mobile API).
        /// Configured via <c>EmailSettings.PostVerificationUrl</c>; defaults
        /// to the consumer-facing site so end users don't land on the IDP's
        /// identity-management Razor pages.
        /// </summary>
        public string SignInUrl => _emailSettings.PostVerificationUrl;

        public async Task<IActionResult> OnGetAsync(string userId, string token, string? source = null)
        {
            IsFromMobile = string.Equals(source, "mobile", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                Success = false;
                Message = "The confirmation link is missing required information.";
                return Page();
            }

            var user = await _userRepository.FindBySubjectIdAsync(userId);
            if (user is null)
            {
                // Don't reveal whether the userId matched — generic failure.
                _logger.LogWarning("Email confirmation attempted for unknown userId={UserId}", userId);
                Success = false;
                Message = "This confirmation link is no longer valid.";
                return Page();
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                _logger.LogWarning("Email confirmation token failed to decode for userId={UserId}", userId);
                Success = false;
                Message = "This confirmation link is malformed.";
                return Page();
            }

            var result = await _userRepository.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed for userId={UserId}", userId);
                Success = true;
                Message = "Your email has been verified. You can now sign in.";
                return Page();
            }

            _logger.LogWarning(
                "Email confirmation failed for userId={UserId}: {Errors}",
                userId,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            Success = false;
            Message = "This confirmation link is invalid or has expired. You can request a new one below.";
            return Page();
        }
    }
}
