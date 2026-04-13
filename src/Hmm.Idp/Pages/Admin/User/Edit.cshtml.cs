using Duende.IdentityModel;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Hmm.Idp.Pages.Admin.User
{
    [Authorize(Roles = "Administrator")]
    [SecurityHeaders]
    public class EditModel : PageModel
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public EditModel(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<RoleOption> AllRoles { get; set; } = new();

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string SubjectId { get; set; }

            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password (leave blank to keep current)")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; } = true;

            public List<string> SelectedRoles { get; set; } = new();
        }

        public class RoleOption
        {
            public string Name { get; set; }
            public bool Selected { get; set; }
        }

        public async Task<IActionResult> OnGet(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userRepository.FindBySubjectIdAsync(id);
            if (user == null) return NotFound();

            var claims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);

            Input = new InputModel
            {
                SubjectId = user.Id,
                Username = user.UserName,
                FullName = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value,
                Email = user.Email,
                IsActive = user.IsActive,
                SelectedRoles = userRoles.ToList()
            };

            await PopulateRoles(userRoles);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateRoles(Input.SelectedRoles);
                return Page();
            }

            var user = await _userRepository.FindBySubjectIdAsync(Input.SubjectId);
            if (user == null) return NotFound();

            user.UserName = Input.Username;
            user.IsActive = Input.IsActive;
            if (!string.IsNullOrEmpty(Input.Email))
            {
                user.Email = Input.Email;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await PopulateRoles(Input.SelectedRoles);
                return Page();
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(Input.Password))
            {
                await _userRepository.SetPasswordAsync(user, Input.Password);
            }

            // Update name/email claims
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var claimsToRemove = existingClaims
                .Where(c => c.Type == JwtClaimTypes.Name || c.Type == JwtClaimTypes.Email)
                .ToList();
            if (claimsToRemove.Any())
            {
                await _userManager.RemoveClaimsAsync(user, claimsToRemove);
            }

            var newClaims = new List<Claim>();
            if (!string.IsNullOrEmpty(Input.FullName))
            {
                newClaims.Add(new Claim(JwtClaimTypes.Name, Input.FullName));
            }
            if (!string.IsNullOrEmpty(Input.Email))
            {
                newClaims.Add(new Claim(JwtClaimTypes.Email, Input.Email));
            }
            if (newClaims.Any())
            {
                await _userManager.AddClaimsAsync(user, newClaims);
            }

            // Reconcile roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var selected = Input.SelectedRoles ?? new List<string>();
            var toRemove = currentRoles.Except(selected, StringComparer.OrdinalIgnoreCase).ToList();
            var toAdd = selected.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (toRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, toRemove);
            }
            if (toAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, toAdd);
            }

            StatusMessage = "User has been updated successfully.";
            return RedirectToPage("./Index");
        }

        private async Task PopulateRoles(IEnumerable<string> selected)
        {
            var all = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            var selectedSet = new HashSet<string>(selected ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            AllRoles = all
                .Select(r => new RoleOption { Name = r.Name!, Selected = selectedSet.Contains(r.Name!) })
                .ToList();
        }
    }
}
