using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using Hmm.Idp.Pages.Admin.User;

namespace Hmm.Idp.Services
{
    public class TwoFactorAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<TwoFactorAuthService> _logger;

        public TwoFactorAuthService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<TwoFactorAuthService> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Generates and sends a 2FA code via email
        /// </summary>
        /// <param name="userName">The username</param>
        /// <returns>True if code was generated and sent successfully</returns>
        public async Task<bool> GenerateAndSendTwoFactorCodeAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null || !user.EmailConfirmed)
            {
                _logger.LogWarning("Failed to generate 2FA code for non-existent or unconfirmed user: {Username}", userName);
                return false;
            }

            // Generate a random 6-digit code
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

            // Send code via email
            var subject = "Your Two-Factor Authentication Code";
            var message = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #f8f9fa; padding: 20px; text-align: center; }}
                        .content {{ padding: 20px; }}
                        .code {{ font-size: 24px; font-weight: bold; text-align: center; 
                                letter-spacing: 5px; margin: 20px 0; padding: 10px; 
                                background-color: #f0f0f0; border-radius: 4px; }}
                        .footer {{ margin-top: 20px; font-size: 12px; color: #6c757d; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Your Authentication Code</h2>
                        </div>
                        <div class='content'>
                            <p>You recently attempted to sign in to your account. To complete the sign-in process, please use the following code:</p>
                            <div class='code'>{code}</div>
                            <p>This code will expire in 5 minutes.</p>
                            <p>If you did not request this code, please ignore this email and consider changing your password.</p>
                        </div>
                        <div class='footer'>
                            <p>This is an automated message. Please do not reply to this email.</p>
                        </div>
                    </div>
                </body>
                </html>";

            var result = await _emailService.SendEmailAsync(user.Email, subject, message);

            if (result)
            {
                _logger.LogInformation("2FA code sent to user: {Username}", userName);
                return true;
            }

            _logger.LogError("Failed to send 2FA code to user: {Username}", userName);
            return false;
        }

        /// <summary>
        /// Verifies the 2FA code for a user
        /// </summary>
        /// <param name="userName">The username</param>
        /// <param name="code">The code to verify</param>
        /// <returns>True if the code is valid</returns>
        public async Task<bool> VerifyTwoFactorCodeAsync(string userName, string code)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, code);

            if (result)
            {
                _logger.LogInformation("2FA code verified for user: {Username}", userName);
            }
            else
            {
                _logger.LogWarning("Invalid 2FA code attempt for user: {Username}", userName);
            }

            return result;
        }

        /// <summary>
        /// Enables two-factor authentication for a user
        /// </summary>
        /// <param name="userName">The username</param>
        /// <returns>True if 2FA was enabled successfully</returns>
        public async Task<bool> EnableTwoFactorAuthenticationAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);

            if (result.Succeeded)
            {
                _logger.LogInformation("2FA enabled for user: {Username}", userName);
                return true;
            }

            _logger.LogError("Failed to enable 2FA for user: {Username}", userName);
            return false;
        }

        /// <summary>
        /// Disables two-factor authentication for a user
        /// </summary>
        /// <param name="userName">The username</param>
        /// <returns>True if 2FA was disabled successfully</returns>
        public async Task<bool> DisableTwoFactorAuthenticationAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);

            if (result.Succeeded)
            {
                _logger.LogInformation("2FA disabled for user: {Username}", userName);
                return true;
            }

            _logger.LogError("Failed to disable 2FA for user: {Username}", userName);
            return false;
        }

        /// <summary>
        /// Checks if a user has two-factor authentication enabled
        /// </summary>
        /// <param name="userName">The username</param>
        /// <returns>True if 2FA is enabled</returns>
        public async Task<bool> IsTwoFactorEnabledAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return false;
            }

            return await _userManager.GetTwoFactorEnabledAsync(user);
        }

        /// <summary>
        /// Generates recovery codes for a user
        /// </summary>
        /// <param name="userName">The username</param>
        /// <returns>A list of recovery codes, or null if failed</returns>
        public async Task<IEnumerable<string>> GenerateRecoveryCodesAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return null;
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            if (recoveryCodes != null)
            {
                _logger.LogInformation("Recovery codes generated for user: {Username}", userName);
                return recoveryCodes;
            }

            _logger.LogError("Failed to generate recovery codes for user: {Username}", userName);
            return null;
        }
    }
}