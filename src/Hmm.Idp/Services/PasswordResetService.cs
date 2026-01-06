using Microsoft.AspNetCore.Identity;
using Hmm.Idp.Pages.Admin.User;

namespace Hmm.Idp.Services
{
    public class PasswordResetService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly PasswordPolicyService _passwordPolicyService;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            PasswordPolicyService passwordPolicyService,
            ILogger<PasswordResetService> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _passwordPolicyService = passwordPolicyService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates the password reset process by generating a token and sending an email
        /// </summary>
        /// <param name="email">The user's email address</param>
        /// <returns>True if the process was initiated successfully, false otherwise</returns>
        public async Task<bool> InitiatePasswordResetAsync(string email)
        {
            // Find the user by email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist for security reasons
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                return true; // Return true to avoid revealing user existence
            }

            // Generate reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Send password reset email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(email, user.Id, token);

            if (emailSent)
            {
                _logger.LogInformation("Password reset email sent to user: {Email}", email);
                return true;
            }

            _logger.LogError("Failed to send password reset email to user: {Email}", email);
            return false;
        }

        /// <summary>
        /// Validates a password reset token
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="token">The password reset token</param>
        /// <returns>True if the token is valid, false otherwise</returns>
        public async Task<bool> ValidatePasswordResetTokenAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Token validation is performed by checking if it can be used to reset the password
            // We'll use a dummy password that will never be set
            try
            {
                var result = await _userManager.VerifyUserTokenAsync(
                    user,
                    _userManager.Options.Tokens.PasswordResetTokenProvider,
                    UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
                    token);

                if (result)
                {
                    _logger.LogInformation("Password reset token validated for user: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("Invalid password reset token for user: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating password reset token for user: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Resets a user's password using a reset token
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="token">The password reset token</param>
        /// <param name="newPassword">The new password</param>
        /// <returns>A tuple indicating success and any error messages</returns>
        public async Task<(bool Succeeded, List<string> Errors)> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, new List<string> { "User not found." });
            }

            // Validate the new password against password policy
            var (isValid, policyErrors) = _passwordPolicyService.ValidatePassword(newPassword);
            if (!isValid)
            {
                _logger.LogWarning("Password reset failed for user {UserId} due to policy violations", userId);
                return (false, policyErrors);
            }

            // Reset the password
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user: {UserId}", userId);

                // Reset failed login attempts count if the password reset is successful
                await _userManager.ResetAccessFailedCountAsync(user);

                // Notify the user about the password change
                await _emailService.SendEmailAsync(user.Email, "Your Password Has Been Changed",
                    $@"<html>
                      <body>
                        <h2>Password Change Confirmation</h2>
                        <p>This is to confirm that your password was just changed. If you did not perform this action, please contact support immediately.</p>
                      </body>
                      </html>");

                return (true, new List<string>());
            }

            _logger.LogWarning("Password reset failed for user: {UserId}", userId);
            return (false, result.Errors.Select(e => e.Description).ToList());
        }

        /// <summary>
        /// Changes a user's password (when the user knows their current password)
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="currentPassword">The current password</param>
        /// <param name="newPassword">The new password</param>
        /// <returns>A tuple indicating success and any error messages</returns>
        public async Task<(bool Succeeded, List<string> Errors)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, new List<string> { "User not found." });
            }

            // Validate the current password
            if (!await _userManager.CheckPasswordAsync(user, currentPassword))
            {
                _logger.LogWarning("Change password failed for user {UserId} due to incorrect current password", userId);
                return (false, new List<string> { "Current password is incorrect." });
            }

            // Validate the new password against password policy
            var (isValid, policyErrors) = _passwordPolicyService.ValidatePassword(newPassword);
            if (!isValid)
            {
                _logger.LogWarning("Change password failed for user {UserId} due to policy violations", userId);
                return (false, policyErrors);
            }

            // Check if the new password is the same as the current one
            if (currentPassword == newPassword)
            {
                return (false, new List<string> { "New password must be different from the current password." });
            }

            // Change the password
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

                // Notify the user about the password change
                await _emailService.SendEmailAsync(user.Email, "Your Password Has Been Changed",
                    $@"<html>
                      <body>
                        <h2>Password Change Confirmation</h2>
                        <p>This is to confirm that your password was just changed. If you did not perform this action, please contact support immediately.</p>
                      </body>
                      </html>");

                return (true, new List<string>());
            }

            _logger.LogWarning("Password change failed for user: {UserId}", userId);
            return (false, result.Errors.Select(e => e.Description).ToList());
        }
    }
}