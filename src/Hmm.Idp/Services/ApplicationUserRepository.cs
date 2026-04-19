using Duende.IdentityModel;
using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Hmm.Idp.Services
{
    /// <summary>
    /// Repository abstraction over ASP.NET Identity's <see cref="UserManager{TUser}"/> and
    /// <see cref="RoleManager{TRole}"/>. Exposed as an interface so callers can be unit tested
    /// with Moq without constructing real Identity infrastructure.
    /// </summary>
    public interface IApplicationUserRepository
    {
        Task<bool> ValidateCredentialsAsync(string userName, string password);
        Task<ApplicationUser> FindBySubjectIdAsync(string subjectId);
        Task<ApplicationUser> FindByUserNameAsync(string userName);
        Task<ApplicationUser> FindByEmailAsync(string email);
        Task<ApplicationUser> FindByExternalProviderAsync(string provider, string userId);
        Task<ApplicationUser> AutoProvisionUserAsync(string provider, string userId, List<Claim> claims);
        Task<ApplicationUser> CreateUserAsync(string userName, string password, string firstName = null, string lastName = null, string email = null);
        Task<IEnumerable<Claim>> GetClaimsBySubjectIdAsync(string subjectId);
        Task<IEnumerable<ApplicationUser>> GetUsersAsync();
        Task<bool> DeleteUserAsync(string subjectId);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<bool> SetPasswordAsync(ApplicationUser user, string newPassword);
        Task<(IReadOnlyList<ApplicationUser> Users, int Total)> SearchUsersAsync(string query, int page, int pageSize);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
        Task<bool> AddUserToRoleAsync(ApplicationUser user, string role);
        Task<bool> RemoveUserFromRoleAsync(ApplicationUser user, string role);
        Task UnlockUserAsync(ApplicationUser user);
        Task<bool> IsLockedOutAsync(ApplicationUser user);
        Task<IList<Claim>> GetClaimsAsync(ApplicationUser user);
    }

    public class ApplicationUserRepository : IApplicationUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public ApplicationUserRepository(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<bool> ValidateCredentialsAsync(string userName, string password)
        {
            var user = await FindByUserNameAsync(userName);

            if (user is { IsActive: true })
            {
                return await _userManager.CheckPasswordAsync(user, password);
            }

            return false;
        }

        public async Task<ApplicationUser> FindBySubjectIdAsync(string subjectId)
        {
            return await _userManager.FindByIdAsync(subjectId);
        }

        public async Task<ApplicationUser> FindByUserNameAsync(string userName)
        {
            return await _userManager.FindByNameAsync(userName);
        }

        public async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<ApplicationUser> FindByExternalProviderAsync(string provider, string userId)
        {
            return await _userManager.Users
                .FirstOrDefaultAsync(x =>
                    x.ProviderName == provider &&
                    x.ProviderSubjectId == userId);
        }

        public async Task<ApplicationUser> AutoProvisionUserAsync(string provider, string userId, List<Claim> claims)
        {
            // Create a list of claims that we want to transfer into our store
            var filtered = new List<Claim>();

            foreach (var claim in claims)
            {
                // If the external system sends a display name - translate that to the standard OIDC name claim
                if (claim.Type == ClaimTypes.Name)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, claim.Value));
                }
                // If the JWT handler has an outbound mapping to an OIDC claim use that
                else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.TryGetValue(claim.Type, out var value))
                {
                    filtered.Add(new Claim(value, claim.Value));
                }
                // Copy the claim as-is
                else
                {
                    filtered.Add(claim);
                }
            }

            // If no display name was provided, try to construct by first and/or last name
            if (filtered.All(x => x.Type != JwtClaimTypes.Name))
            {
                var first = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
                var last = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;

                if (first != null && last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                }
                else if (first != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first));
                }
                else if (last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, last));
                }
            }

            // Create unique identifier
            var sub = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

            // Check if a display name is available, otherwise fallback to subject id
            var name = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? sub;
            var email = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Email)?.Value;
            var firstName = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value;
            var lastName = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value;

            // Create new user
            var user = new ApplicationUser
            {
                Id = sub,
                UserName = name,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                ProviderName = provider,
                ProviderSubjectId = userId
            };

            // Add user
            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            // Add claims
            if (filtered.Any())
            {
                result = await _userManager.AddClaimsAsync(user, filtered);

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
            }

            return user;
        }

        public async Task<ApplicationUser> CreateUserAsync(string userName, string password, string firstName = null, string lastName = null, string email = null)
        {
            // Check if username exists
            var existingUser = await FindByUserNameAsync(userName);
            if (existingUser != null)
            {
                throw new Exception("Username already exists");
            }

            // Check if email exists (RequireUniqueEmail is enabled)
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingEmail = await FindByEmailAsync(email);
                if (existingEmail != null)
                {
                    throw new Exception("An account with this email address already exists");
                }
            }

            // Create unique identifier
            var sub = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

            // Create new user
            var user = new ApplicationUser
            {
                Id = sub,
                UserName = userName,
                Email = email,
                EmailConfirmed = !string.IsNullOrWhiteSpace(email),
                FirstName = firstName,
                LastName = lastName
            };

            // Add user
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors.First().Description);
            }

            // Add claims
            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            {
                claims.Add(new Claim(JwtClaimTypes.Name, $"{firstName} {lastName}"));
                claims.Add(new Claim(JwtClaimTypes.GivenName, firstName));
                claims.Add(new Claim(JwtClaimTypes.FamilyName, lastName));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(JwtClaimTypes.Email, email));
                claims.Add(new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean));
            }

            if (claims.Any())
            {
                result = await _userManager.AddClaimsAsync(user, claims);

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
            }

            return user;
        }

        public async Task<IEnumerable<Claim>> GetClaimsBySubjectIdAsync(string subjectId)
        {
            var user = await FindBySubjectIdAsync(subjectId);

            if (user == null)
            {
                return Enumerable.Empty<Claim>();
            }

            // Get user claims
            var claims = await _userManager.GetClaimsAsync(user);

            // Get role claims
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, role));
            }

            // Add standard claims for IdentityServer
            if (!claims.Any(c => c.Type == JwtClaimTypes.Subject))
            {
                claims.Add(new Claim(JwtClaimTypes.Subject, user.Id));
            }

            if (!claims.Any(c => c.Type == JwtClaimTypes.Name) && user.UserName != null)
            {
                claims.Add(new Claim(JwtClaimTypes.Name, user.UserName));
            }

            if (!claims.Any(c => c.Type == JwtClaimTypes.Email) && user.Email != null)
            {
                claims.Add(new Claim(JwtClaimTypes.Email, user.Email));
            }

            return claims;
        }

        public async Task<IEnumerable<ApplicationUser>> GetUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<bool> DeleteUserAsync(string subjectId)
        {
            var user = await FindBySubjectIdAsync(subjectId);

            if (user == null)
            {
                return false;
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserAsync(ApplicationUser user)
        {
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> SetPasswordAsync(ApplicationUser user, string newPassword)
        {
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, newPassword);
            return result.Succeeded;
        }

        public async Task<(IReadOnlyList<ApplicationUser> Users, int Total)> SearchUsersAsync(
            string query, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 25;

            var users = _userManager.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim();
                users = users.Where(u =>
                    (u.UserName != null && u.UserName.Contains(q)) ||
                    (u.Email != null && u.Email.Contains(q)));
            }

            var total = await users.CountAsync();
            var pageItems = await users
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pageItems, total);
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> AddUserToRoleAsync(ApplicationUser user, string role)
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> RemoveUserFromRoleAsync(ApplicationUser user, string role)
        {
            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task UnlockUserAsync(ApplicationUser user)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        public async Task<bool> IsLockedOutAsync(ApplicationUser user)
        {
            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task<IList<Claim>> GetClaimsAsync(ApplicationUser user)
        {
            return await _userManager.GetClaimsAsync(user);
        }
    }
}
