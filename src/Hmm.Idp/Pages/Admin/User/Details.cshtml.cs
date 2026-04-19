using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "Administrator")]
    [SecurityHeaders]
    public class DetailsModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public DetailsModel(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _emailService = emailService;
        }

        public ApplicationUser User { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
        public IList<Claim> Claims { get; set; } = new List<Claim>();
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userRepository.FindBySubjectIdAsync(id);
            if (user == null) return NotFound();

            User = user;
            Roles = await _userManager.GetRolesAsync(user);
            Claims = await _userManager.GetClaimsAsync(user);
            IsLockedOut = await _userManager.IsLockedOutAsync(user);
            LockoutEnd = user.LockoutEnd;
            return Page();
        }

        public async Task<IActionResult> OnPostUnlockAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userRepository.FindBySubjectIdAsync(id);
            if (user == null) return NotFound();

            await _userRepository.UnlockUserAsync(user);
            StatusMessage = "User has been unlocked.";
            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostResendVerificationAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userRepository.FindBySubjectIdAsync(id);
            if (user == null) return NotFound();

            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.Id, "VerificationToken");
                StatusMessage = "Verification email sent.";
            }
            else
            {
                StatusMessage = "User has no email on file.";
            }
            return RedirectToPage("./Details", new { id });
        }
    }
}
