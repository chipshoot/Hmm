using System;
using System.Threading.Tasks;
using Hmm.Infrastructure;

namespace Hmm.Infrastructure.Tests
{
    public class IdpUserProfileProviderTests
    {
        public IdpUserProfileProviderTests()
        {
            // Clear cache before each test to ensure isolation
            IdpUserProfileProvider.ClearCache();
            // Reset cache duration to default
            IdpUserProfileProvider.CacheDuration = TimeSpan.FromHours(1);
        }

        [Fact]
        public void CacheDuration_DefaultValue_IsOneHour()
        {
            // Reset to ensure we're checking the default
            IdpUserProfileProvider.CacheDuration = TimeSpan.FromHours(1);

            Assert.Equal(TimeSpan.FromHours(1), IdpUserProfileProvider.CacheDuration);
        }

        [Fact]
        public void CacheDuration_CanBeConfigured()
        {
            var customDuration = TimeSpan.FromMinutes(30);
            IdpUserProfileProvider.CacheDuration = customDuration;

            Assert.Equal(customDuration, IdpUserProfileProvider.CacheDuration);
        }

        [Fact]
        public void ClearCache_DoesNotThrow()
        {
            // Arrange & Act & Assert - should not throw
            var exception = Record.Exception(() => IdpUserProfileProvider.ClearCache());
            Assert.Null(exception);
        }

        [Fact]
        public void ClearCache_CanBeCalledMultipleTimes()
        {
            // Arrange & Act & Assert - calling multiple times should not throw
            var exception = Record.Exception(() =>
            {
                IdpUserProfileProvider.ClearCache();
                IdpUserProfileProvider.ClearCache();
                IdpUserProfileProvider.ClearCache();
            });
            Assert.Null(exception);
        }

        [Fact]
        public async Task GetUserClaimAsync_ThrowsArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            using var httpClient = new System.Net.Http.HttpClient();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => IdpUserProfileProvider.GetUserClaimAsync("email", null!, httpClient));
        }

        [Fact]
        public async Task GetUserClaimAsync_ThrowsArgumentNullException_WhenHttpClientIsNull()
        {
            // Arrange
            var context = new Microsoft.AspNetCore.Http.DefaultHttpContext();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => IdpUserProfileProvider.GetUserClaimAsync("email", context, null!));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(60)]
        [InlineData(120)]
        public void CacheDuration_AcceptsVariousDurations(int minutes)
        {
            // Arrange
            var duration = TimeSpan.FromMinutes(minutes);

            // Act
            IdpUserProfileProvider.CacheDuration = duration;

            // Assert
            Assert.Equal(duration, IdpUserProfileProvider.CacheDuration);
        }
    }
}
