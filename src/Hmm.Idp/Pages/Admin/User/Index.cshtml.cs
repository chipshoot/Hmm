using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "Administrator")]
    [SecurityHeaders]
    public class IndexModel : PageModel
    {
        private const int DefaultPageSize = 25;

        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
        }

        public IList<UserRow> Users { get; private set; } = new List<UserRow>();

        [BindProperty(SupportsGet = true)]
        public string Query { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        public int PageSize { get; } = DefaultPageSize;
        public int TotalUsers { get; private set; }
        public int TotalPages => (int)Math.Ceiling(TotalUsers / (double)PageSize);

        public async Task OnGet()
        {
            var (users, total) = await _userRepository.SearchUsersAsync(Query, Page, PageSize);
            TotalUsers = total;

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var lockedOut = await _userManager.IsLockedOutAsync(user);
                Users.Add(new UserRow
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = string.Join(", ", roles),
                    IsActive = user.IsActive,
                    IsLockedOut = lockedOut,
                    LastLoginAt = user.LastLoginAt
                });
            }
        }

        public class UserRow
        {
            public string Id { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
            public string Roles { get; set; }
            public bool IsActive { get; set; }
            public bool IsLockedOut { get; set; }
            public DateTime? LastLoginAt { get; set; }
        }
    }
}
