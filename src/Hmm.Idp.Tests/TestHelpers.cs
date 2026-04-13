using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Hmm.Idp.Data;
using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Hmm.Idp.Tests;

/// <summary>
/// Shared test infrastructure for Hmm.Idp unit tests — builds in-memory Duende
/// configuration contexts and mockable ASP.NET Identity managers without spinning
/// up the full DI container.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Creates a fresh in-memory <see cref="ConfigurationDbContext"/> (Duende) with a
    /// unique database name per call. Duende's OnModelCreating pulls
    /// <see cref="ConfigurationStoreOptions"/> from the application service provider, so
    /// we wire a minimal one via <see cref="DbContextOptionsBuilder.UseApplicationServiceProvider"/>.
    /// </summary>
    public static ConfigurationDbContext CreateConfigurationDbContext(string? dbName = null)
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton(new ConfigurationStoreOptions())
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .UseApplicationServiceProvider(serviceProvider)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                .InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ConfigurationDbContext(options);
    }

    /// <summary>
    /// Builds a <see cref="Mock{T}"/> of <see cref="UserManager{ApplicationUser}"/>
    /// with the mandatory constructor parameters satisfied.
    /// Configure behaviour via the returned mock's Setup calls.
    /// </summary>
    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
        return mgr;
    }

    /// <summary>
    /// Builds a <see cref="Mock{T}"/> of <see cref="RoleManager{ApplicationRole}"/>
    /// with the mandatory constructor parameters satisfied.
    /// </summary>
    public static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        return new Mock<RoleManager<ApplicationRole>>(
            store.Object,
            new IRoleValidator<ApplicationRole>[0],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!);
    }

    /// <summary>
    /// Creates a fresh in-memory <see cref="ApplicationDbContext"/> (the real Identity
    /// context) so Identity entities can be queried with EF Core async operators.
    /// </summary>
    public static ApplicationDbContext CreateApplicationDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
