using System.Text;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Hmm.Idp.Pages.Account
{
    /// <summary>
    /// Lands the user when they click the verification link emailed by
    /// <see cref="RegisterModel"/>. Validates the (base64url-encoded) token
    /// and flips <c>EmailConfirmed = true</c>.
    /// </summary>
    public class ConfirmEmailModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(
            IApplicationUserRepository userRepository,
            ILogger<ConfirmEmailModel> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public bool Success { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string userId, string token)
        {
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
