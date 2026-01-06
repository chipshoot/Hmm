using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "admin")]
    [SecurityHeaders]
    public class IndexModel : PageModel
    {
        private readonly UserManagementService _userService;

        public IndexModel(UserManagementService userService)
        {
            _userService = userService;
        }

        public IEnumerable<ApplicationUser> Users { get; private set; }

        public async Task OnGet()
        {
            Users = await _userService.GetAllUsers();
        }
    }
}