using Hmm.Idp.Data;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Hmm.Idp.Tests;

/// <summary>
/// Focused tests for the non-trivial query logic in <see cref="ApplicationUserRepository.SearchUsersAsync"/>.
/// The other repository methods are thin wrappers around <see cref="UserManager{TUser}"/> and are
/// exercised transitively by the page model tests.
/// </summary>
public class ApplicationUserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly ApplicationUserRepository _repository;

    public ApplicationUserRepositoryTests()
    {
        _dbContext = TestHelpers.CreateApplicationDbContext();
        _mockUserManager = TestHelpers.CreateMockUserManager();

        // Point UserManager.Users at the real EF InMemory DbSet so async operators
        // (CountAsync / ToListAsync) work. The Users property is virtual on UserManager<T>.
        _mockUserManager.Setup(m => m.Users).Returns(() => _dbContext.Users);

        var mockRoleManager = TestHelpers.CreateMockRoleManager();
        _repository = new ApplicationUserRepository(_mockUserManager.Object, mockRoleManager.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task SeedUsersAsync(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = $"user-{i:D3}",
                UserName = $"user{i:D3}",
                NormalizedUserName = $"USER{i:D3}",
                Email = $"user{i:D3}@example.com",
                NormalizedEmail = $"USER{i:D3}@EXAMPLE.COM",
                IsActive = true
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchUsersAsync_ReturnsAllUsers_WhenNoQuery()
    {
        await SeedUsersAsync(5);

        var (users, total) = await _repository.SearchUsersAsync(query: null!, page: 1, pageSize: 25);

        Assert.Equal(5, total);
        Assert.Equal(5, users.Count);
    }

    [Fact]
    public async Task SearchUsersAsync_FiltersByUserName()
    {
        await SeedUsersAsync(5);
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = "alice-1",
            UserName = "alice",
            NormalizedUserName = "ALICE",
            Email = "alice@company.com",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var (users, total) = await _repository.SearchUsersAsync(query: "alice", page: 1, pageSize: 25);

        Assert.Equal(1, total);
        Assert.Single(users);
        Assert.Equal("alice", users[0].UserName);
    }

    [Fact]
    public async Task SearchUsersAsync_FiltersByEmail()
    {
        await SeedUsersAsync(5);
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = "bob-1",
            UserName = "bob",
            Email = "special@company.com",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var (users, total) = await _repository.SearchUsersAsync(query: "special", page: 1, pageSize: 25);

        Assert.Equal(1, total);
        Assert.Equal("bob", users[0].UserName);
    }

    [Fact]
    public async Task SearchUsersAsync_PaginatesResults()
    {
        await SeedUsersAsync(30);

        var (page1, total1) = await _repository.SearchUsersAsync(null!, page: 1, pageSize: 10);
        var (page2, total2) = await _repository.SearchUsersAsync(null!, page: 2, pageSize: 10);
        var (page3, total3) = await _repository.SearchUsersAsync(null!, page: 3, pageSize: 10);
        var (page4, total4) = await _repository.SearchUsersAsync(null!, page: 4, pageSize: 10);

        Assert.Equal(30, total1);
        Assert.Equal(30, total2);
        Assert.Equal(30, total3);
        Assert.Equal(30, total4);
        Assert.Equal(10, page1.Count);
        Assert.Equal(10, page2.Count);
        Assert.Equal(10, page3.Count);
        Assert.Empty(page4);

        // Ensure the pages are disjoint and ordered
        Assert.Equal("user001", page1[0].UserName);
        Assert.Equal("user011", page2[0].UserName);
        Assert.Equal("user021", page3[0].UserName);
    }

    [Fact]
    public async Task SearchUsersAsync_NormalizesInvalidPageAndPageSize()
    {
        await SeedUsersAsync(3);

        // Invalid page (-5) should normalize to 1, pageSize (0) should normalize to 25
        var (users, total) = await _repository.SearchUsersAsync(null!, page: -5, pageSize: 0);

        Assert.Equal(3, total);
        Assert.Equal(3, users.Count);
    }

    [Fact]
    public async Task SearchUsersAsync_ReturnsEmpty_WhenQueryMatchesNothing()
    {
        await SeedUsersAsync(5);

        var (users, total) = await _repository.SearchUsersAsync(query: "nonexistent", page: 1, pageSize: 25);

        Assert.Equal(0, total);
        Assert.Empty(users);
    }

    [Fact]
    public async Task SearchUsersAsync_TrimsWhitespaceQuery()
    {
        await SeedUsersAsync(5);

        var (users, total) = await _repository.SearchUsersAsync(query: "  user001  ", page: 1, pageSize: 25);

        Assert.Equal(1, total);
        Assert.Equal("user001", users[0].UserName);
    }
}
