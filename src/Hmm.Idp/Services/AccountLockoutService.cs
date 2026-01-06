using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Hmm.Idp.Pages.Admin.User;

namespace Hmm.Idp.Services
{
    public class AccountLockoutService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LockoutOptions _lockoutOptions;
        private readonly ILogger<AccountLockoutService> _logger;

        public AccountLockoutService(
            UserManager<ApplicationUser> userManager,
            IOptions<LockoutOptions> lockoutOptions,
            ILogger<AccountLockoutService> logger)
        {
            _userManager = userManager;
            _lockoutOptions = lockoutOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Records a failed login attempt for a user.
        /// </summary>
        /// <param name="userName">The username that attempted to log in.</param>
        /// <returns>LockoutResult containing the status and details of the lockout.</returns>
        public async Task<LockoutResult> RecordFailedLoginAttemptAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                _logger.LogWarning("Failed login attempt for non-existent user: {Username}", userName);
                return new LockoutResult { IsLockedOut = false };
            }

            // Increment access failed count and update lockout details
            var result = await _userManager.AccessFailedAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update access failed count for user: {Username}, Errors: {Errors}",
                    userName, JsonSerializer.Serialize(result.Errors));
                return new LockoutResult { IsLockedOut = false };
            }

            // Check if the user is now locked out
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            if (isLockedOut)
            {
                var endDate = await _userManager.GetLockoutEndDateAsync(user);
                _logger.LogWarning("User {Username} has been locked out until {LockoutEnd} due to multiple failed login attempts",
                    userName, endDate);

                return new LockoutResult
                {
                    IsLockedOut = true,
                    LockoutEnd = endDate,
                    FailedLoginAttempts = await _userManager.GetAccessFailedCountAsync(user)
                };
            }

            return new LockoutResult
            {
                IsLockedOut = false,
                FailedLoginAttempts = await _userManager.GetAccessFailedCountAsync(user),
                RemainingAttempts = _lockoutOptions.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user)
            };
        }

        /// <summary>
        /// Resets a user's failed login attempts count.
        /// </summary>
        /// <param name="userName">The username to reset login attempts for.</param>
        public async Task ResetFailedLoginAttemptsAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return;
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            _logger.LogInformation("Reset failed login attempts for user: {Username}", userName);
        }

        /// <summary>
        /// Unlocks a user account that has been locked out.
        /// </summary>
        /// <param name="userName">The username to unlock.</param>
        /// <returns>True if the account was successfully unlocked, false otherwise.</returns>
        public async Task<bool> UnlockUserAccountAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return false;
            }

            // Set lockout end date to the past to unlock
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(-1));
            if (result.Succeeded)
            {
                // Also reset the access failed count
                await _userManager.ResetAccessFailedCountAsync(user);
                _logger.LogInformation("User account unlocked: {Username}", userName);
                return true;
            }

            _logger.LogError("Failed to unlock user account: {Username}, Errors: {Errors}",
                userName, JsonSerializer.Serialize(result.Errors));
            return false;
        }
    }

    public class LockoutResult
    {
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int FailedLoginAttempts { get; set; }
        public int RemainingAttempts { get; set; }
    }
}