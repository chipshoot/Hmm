using AutoMapper;
using Hmm.ServiceApi.Areas.UtilityService.Controllers;
using Hmm.ServiceApi.Areas.UtilityService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Utility;
using Hmm.Utility.Services;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class GeocodingControllerTests
    {
        private readonly Mock<IGeocodingService> _mockGeocodingService;
        private readonly GeocodingController _controller;
        private readonly IMapper _mapper;

        public GeocodingControllerTests()
        {
            _mockGeocodingService = new Mock<IGeocodingService>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UtilityServiceMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _controller = new GeocodingController(
                _mockGeocodingService.Object,
                _mapper,
                new Mock<ILogger<GeocodingController>>().Object);
        }

        [Fact]
        public async Task ReverseGeocode_WithValidCoordinates_ReturnsOkWithAddress()
        {
            // Arrange
            var geoAddress = new GeoAddress
            {
                Street = "123 Main St",
                City = "Vancouver",
                State = "BC",
                Country = "Canada",
                ZipCode = "V6B 1A1",
                FormattedAddress = "123 Main St, Vancouver, BC, Canada",
                Latitude = 49.2827,
                Longitude = -123.1207
            };

            _mockGeocodingService
                .Setup(s => s.ReverseGeocodeAsync(49.2827, -123.1207))
                .ReturnsAsync(ProcessingResult<GeoAddress>.Ok(geoAddress));

            // Act
            var result = await _controller.ReverseGeocode(49.2827, -123.1207);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiAddress = Assert.IsType<ApiGeoAddress>(okResult.Value);
            Assert.Equal("123 Main St", apiAddress.Street);
            Assert.Equal("Vancouver", apiAddress.City);
            Assert.Equal("Canada", apiAddress.Country);
            Assert.Equal(49.2827, apiAddress.Latitude);
            Assert.Equal(-123.1207, apiAddress.Longitude);
        }

        [Fact]
        public async Task ReverseGeocode_WhenLocationNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockGeocodingService
                .Setup(s => s.ReverseGeocodeAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(ProcessingResult<GeoAddress>.NotFound("Location not found"));

            // Act
            var result = await _controller.ReverseGeocode(0, 0);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(notFoundResult.Value);
            Assert.Contains("Location not found", response.Errors);
        }

        [Fact]
        public async Task ReverseGeocode_WhenServiceFails_ReturnsBadRequest()
        {
            // Arrange
            _mockGeocodingService
                .Setup(s => s.ReverseGeocodeAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(ProcessingResult<GeoAddress>.Fail("Geocoding service unavailable"));

            // Act
            var result = await _controller.ReverseGeocode(49.2827, -123.1207);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Geocoding service unavailable", response.Errors);
        }

        [Fact]
        public async Task ReverseGeocode_WhenValidationFails_ReturnsBadRequest()
        {
            // Arrange
            _mockGeocodingService
                .Setup(s => s.ReverseGeocodeAsync(It.IsAny<double>(), It.IsAny<double>()))
                .ReturnsAsync(ProcessingResult<GeoAddress>.Invalid("Latitude must be between -90 and 90"));

            // Act
            var result = await _controller.ReverseGeocode(999, 0);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Latitude", response.Errors.First());
        }

        [Fact]
        public async Task ReverseGeocode_MapsAllFieldsCorrectly()
        {
            // Arrange
            var geoAddress = new GeoAddress
            {
                Street = "456 Oak Ave",
                City = "Burnaby",
                State = "British Columbia",
                Country = "Canada",
                ZipCode = "V5H 2B2",
                FormattedAddress = "456 Oak Ave, Burnaby, BC, Canada",
                Latitude = 49.2488,
                Longitude = -122.9805
            };

            _mockGeocodingService
                .Setup(s => s.ReverseGeocodeAsync(49.2488, -122.9805))
                .ReturnsAsync(ProcessingResult<GeoAddress>.Ok(geoAddress));

            // Act
            var result = await _controller.ReverseGeocode(49.2488, -122.9805);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiAddress = Assert.IsType<ApiGeoAddress>(okResult.Value);
            Assert.Equal("456 Oak Ave", apiAddress.Street);
            Assert.Equal("Burnaby", apiAddress.City);
            Assert.Equal("British Columbia", apiAddress.State);
            Assert.Equal("Canada", apiAddress.Country);
            Assert.Equal("V5H 2B2", apiAddress.ZipCode);
            Assert.Equal("456 Oak Ave, Burnaby, BC, Canada", apiAddress.FormattedAddress);
            Assert.Equal(49.2488, apiAddress.Latitude);
            Assert.Equal(-122.9805, apiAddress.Longitude);
        }
    }
}
