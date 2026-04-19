using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "Administrator")]
    [SecurityHeaders]
    public class DeleteModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;

        public DeleteModel(IApplicationUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public ApplicationUser User { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            User = await _userRepository.FindBySubjectIdAsync(id);
            if (User == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var success = await _userRepository.DeleteUserAsync(id);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Failed to delete user.");
                User = await _userRepository.FindBySubjectIdAsync(id);
                return Page();
            }

            return RedirectToPage("./Index");
        }
    }
}
