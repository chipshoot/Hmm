using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;

namespace Hmm.ServiceApi.Core.Tests
{
    public class NominatimGeocodingServiceTests
    {
        private readonly GeocodingSettings _settings;
        private readonly Mock<ILogger<NominatimGeocodingService>> _mockLogger;

        public NominatimGeocodingServiceTests()
        {
            _settings = new GeocodingSettings
            {
                Provider = "Nominatim",
                BaseUrl = "https://nominatim.openstreetmap.org",
                UserAgent = "HmmApp/1.0"
            };
            _mockLogger = new Mock<ILogger<NominatimGeocodingService>>();
        }

        #region Validation Tests

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        [InlineData(-100)]
        [InlineData(200)]
        public async Task ReverseGeocodeAsync_WithInvalidLatitude_ReturnsInvalid(double latitude)
        {
            // Arrange
            var service = CreateService(CreateMockHandler("{}"));

            // Act
            var result = await service.ReverseGeocodeAsync(latitude, 0);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Latitude", result.ErrorMessage);
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        [InlineData(-200)]
        [InlineData(300)]
        public async Task ReverseGeocodeAsync_WithInvalidLongitude_ReturnsInvalid(double longitude)
        {
            // Arrange
            var service = CreateService(CreateMockHandler("{}"));

            // Act
            var result = await service.ReverseGeocodeAsync(0, longitude);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Longitude", result.ErrorMessage);
        }

        [Theory]
        [InlineData(-90, -180)]
        [InlineData(90, 180)]
        [InlineData(0, 0)]
        [InlineData(49.2827, -123.1207)]
        public async Task ReverseGeocodeAsync_WithValidCoordinates_DoesNotReturnValidationError(double latitude, double longitude)
        {
            // Arrange
            var json = CreateSuccessResponse("Test St", "Vancouver", "BC", "Canada", "V5K 1A1", "Test St, Vancouver");
            var service = CreateService(CreateMockHandler(json));

            // Act
            var result = await service.ReverseGeocodeAsync(latitude, longitude);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Success Response Tests

        [Fact]
        public async Task ReverseGeocodeAsync_WithValidResponse_ReturnsGeoAddress()
        {
            // Arrange
            var json = CreateSuccessResponse("123 Main St", "Vancouver", "British Columbia", "Canada", "V6B 1A1",
                "123 Main St, Vancouver, BC, Canada");
            var service = CreateService(CreateMockHandler(json));

            // Act
            var result = await service.ReverseGeocodeAsync(49.2827, -123.1207);

            // Assert
            Assert.True(result.Success);
            var address = result.Value;
            Assert.Equal("123 Main St", address.Street);
            Assert.Equal("Vancouver", address.City);
            Assert.Equal("British Columbia", address.State);
            Assert.Equal("Canada", address.Country);
            Assert.Equal("V6B 1A1", address.ZipCode);
            Assert.Equal("123 Main St, Vancouver, BC, Canada", address.FormattedAddress);
            Assert.Equal(49.2827, address.Latitude);
            Assert.Equal(-123.1207, address.Longitude);
        }

        [Fact]
        public async Task ReverseGeocodeAsync_WithTownInsteadOfCity_ReturnsCityFromTown()
        {
            // Arrange - use "town" instead of "city" in the response
            var json = """
                {
                    "display_name": "Small Town, Canada",
                    "address": {
                        "road": "Rural Rd",
                        "town": "Smallville",
                        "province": "Ontario",
                        "country": "Canada",
                        "postcode": "K0A 1A0"
                    }
                }
                """;
            var service = CreateService(CreateMockHandler(json));

            // Act
            var result = await service.ReverseGeocodeAsync(45.0, -75.0);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Smallville", result.Value.City);
            Assert.Equal("Ontario", result.Value.State);
        }

        #endregion

        #region Error Response Tests

        [Fact]
        public async Task ReverseGeocodeAsync_WithErrorResponse_ReturnsNotFound()
        {
            // Arrange
            var json = """{"error": "Unable to geocode"}""";
            var service = CreateService(CreateMockHandler(json));

            // Act
            var result = await service.ReverseGeocodeAsync(0, 0);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("Unable to geocode", result.ErrorMessage);
        }

        [Fact]
        public async Task ReverseGeocodeAsync_WithHttpError_ReturnsFail()
        {
            // Arrange
            var handler = CreateMockHandler("Server Error", HttpStatusCode.InternalServerError);
            var service = CreateService(handler);

            // Act
            var result = await service.ReverseGeocodeAsync(49.2827, -123.1207);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("unavailable", result.ErrorMessage);
        }

        [Fact]
        public async Task ReverseGeocodeAsync_WithInvalidJson_ReturnsFail()
        {
            // Arrange
            var service = CreateService(CreateMockHandler("not valid json {{{"));

            // Act
            var result = await service.ReverseGeocodeAsync(49.2827, -123.1207);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("parse", result.ErrorMessage);
        }

        #endregion

        #region Helper Methods

        private NominatimGeocodingService CreateService(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var options = Options.Create(_settings);
            return new NominatimGeocodingService(httpClient, options, _mockLogger.Object);
        }

        private static MockHttpMessageHandler CreateMockHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new MockHttpMessageHandler(responseContent, statusCode);
        }

        private static string CreateSuccessResponse(string street, string city, string state, string country, string postcode, string displayName)
        {
            return $$"""
                {
                    "display_name": "{{displayName}}",
                    "address": {
                        "road": "{{street}}",
                        "city": "{{city}}",
                        "state": "{{state}}",
                        "country": "{{country}}",
                        "postcode": "{{postcode}}"
                    }
                }
                """;
        }

        #endregion
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseContent = responseContent;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
