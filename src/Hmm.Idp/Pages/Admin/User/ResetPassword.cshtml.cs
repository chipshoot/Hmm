using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "Administrator")]
    [SecurityHeaders]
    public class ResetPasswordModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly PasswordPolicyService _passwordPolicyService;

        public ResetPasswordModel(
            IApplicationUserRepository userRepository,
            PasswordPolicyService passwordPolicyService)
        {
            _userRepository = userRepository;
            _passwordPolicyService = passwordPolicyService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string UserName { get; set; }

        public class InputModel
        {
            [Required]
            public string SubjectId { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 12)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userRepository.FindBySubjectIdAsync(id);
            if (user == null) return NotFound();

            Input = new InputModel { SubjectId = user.Id };
            UserName = user.UserName;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var u = await _userRepository.FindBySubjectIdAsync(Input.SubjectId);
                UserName = u?.UserName;
                return Page();
            }

            var (isValid, errors) = _passwordPolicyService.ValidatePassword(Input.NewPassword);
            if (!isValid)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                var u = await _userRepository.FindBySubjectIdAsync(Input.SubjectId);
                UserName = u?.UserName;
                return Page();
            }

            var user = await _userRepository.FindBySubjectIdAsync(Input.SubjectId);
            if (user == null) return NotFound();

            var success = await _userRepository.SetPasswordAsync(user, Input.NewPassword);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to reset password.");
                UserName = user.UserName;
                return Page();
            }

            TempData["StatusMessage"] = $"Password reset for {user.UserName}.";
            return RedirectToPage("./Details", new { id = user.Id });
        }
    }
}
