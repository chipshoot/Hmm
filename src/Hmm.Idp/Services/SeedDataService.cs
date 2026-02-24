using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;


namespace Hmm.Idp.Services;

public class SeedDataService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<SeedDataService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedUsersAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "Administrator", "User", "ApiClient" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new ApplicationRole { Name = role });
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {Role}", role);
                }
                else
                {
                    _logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        // Seed admin user
        await CreateUserIfNotExistsAsync(new SeedUserInfo
        {
            UserName = "admin@hmm.local",
            Email = "admin@hmm.local",
            Password = "Admin@12345678#",
            FirstName = "System",
            LastName = "Administrator",
            Roles = ["Administrator"],
            Claims =
            [
                new Claim("name", "System Administrator"),
                new Claim("given_name", "System"),
                new Claim("family_name", "Administrator"),
                new Claim("email", "admin@hmm.local"),
                new Claim("email_verified", "true"),
                new Claim("role", "Administrator")
            ]
        });

        // Seed test user for functional testing
        await CreateUserIfNotExistsAsync(new SeedUserInfo
        {
            UserName = "testuser@hmm.local",
            Email = "testuser@hmm.local",
            Password = "TestPassword123#",
            FirstName = "Test",
            LastName = "User",
            Roles = ["User"],
            Claims =
            [
                new Claim("name", "Test User"),
                new Claim("given_name", "Test"),
                new Claim("family_name", "User"),
                new Claim("email", "testuser@hmm.local"),
                new Claim("email_verified", "true"),
                new Claim("role", "User")
            ]
        });

        // Seed Alice (from TestUsers)
        await CreateUserIfNotExistsAsync(new SeedUserInfo
        {
            UserName = "alice",
            Email = "alicesmith@email.com",
            Password = "Alice@12345678#",
            FirstName = "Alice",
            LastName = "Smith",
            Roles = ["User"],
            Claims =
            [
                new Claim("name", "Alice Smith"),
                new Claim("given_name", "Alice"),
                new Claim("family_name", "Smith"),
                new Claim("email", "alicesmith@email.com"),
                new Claim("email_verified", "true"),
                new Claim("website", "http://alice.com"),
                new Claim("role", "User")
            ]
        });

        // Seed Bob (from TestUsers)
        await CreateUserIfNotExistsAsync(new SeedUserInfo
        {
            UserName = "bob",
            Email = "bobsmith@email.com",
            Password = "Bob@123456789#",
            FirstName = "Bob",
            LastName = "Smith",
            Roles = ["User"],
            Claims =
            [
                new Claim("name", "Bob Smith"),
                new Claim("given_name", "Bob"),
                new Claim("family_name", "Smith"),
                new Claim("email", "bobsmith@email.com"),
                new Claim("email_verified", "true"),
                new Claim("website", "http://bob.com"),
                new Claim("role", "User")
            ]
        });

        // Seed API client user (for ServiceApi)
        await CreateUserIfNotExistsAsync(new SeedUserInfo
        {
            UserName = "serviceapi@hmm.local",
            Email = "serviceapi@hmm.local",
            Password = "ServiceApi@123#",
            FirstName = "Service",
            LastName = "API",
            Roles = ["ApiClient"],
            Claims =
            [
                new Claim("name", "Service API"),
                new Claim("given_name", "Service"),
                new Claim("family_name", "API"),
                new Claim("email", "serviceapi@hmm.local"),
                new Claim("email_verified", "true"),
                new Claim("role", "ApiClient")
            ]
        });
    }

    private async Task CreateUserIfNotExistsAsync(SeedUserInfo userInfo)
    {
        var existingUser = await _userManager.FindByNameAsync(userInfo.UserName);
        if (existingUser != null)
        {
            _logger.LogInformation("User {UserName} already exists, skipping", userInfo.UserName);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = userInfo.UserName,
            Email = userInfo.Email,
            EmailConfirmed = true,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, userInfo.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create user {UserName}: {Errors}",
                userInfo.UserName,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation("Created user: {UserName}", userInfo.UserName);

        // Add roles
        foreach (var role in userInfo.Roles)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (roleResult.Succeeded)
            {
                _logger.LogInformation("Added user {UserName} to role {Role}", userInfo.UserName, role);
            }
        }

        // Add claims
        if (userInfo.Claims.Count > 0)
        {
            var claimResult = await _userManager.AddClaimsAsync(user, userInfo.Claims);
            if (claimResult.Succeeded)
            {
                _logger.LogInformation("Added {Count} claims to user {UserName}", userInfo.Claims.Count, userInfo.UserName);
            }
        }
    }

    private class SeedUserInfo
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
        public List<Claim> Claims { get; set; } = [];
    }
}
