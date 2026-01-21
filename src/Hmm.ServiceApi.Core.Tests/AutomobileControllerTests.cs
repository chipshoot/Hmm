using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class AutomobileControllerTests
    {
        private readonly Mock<IAutoEntityManager<AutomobileInfo>> _mockAutomobileManager;
        private readonly AutomobileController _controller;
        private readonly IMapper _mapper;
        private readonly List<AutomobileInfo> _automobiles;

        public AutomobileControllerTests()
        {
            _mockAutomobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutomobileMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _automobiles = GetSampleAutomobiles();
            SetupMockManager();

            _controller = new AutomobileController(
                _mockAutomobileManager.Object,
                _mapper,
                new Mock<ILogger<AutomobileController>>().Object);
        }

        #region Get automobiles

        [Fact]
        public async Task GetMobiles_ReturnsOkResult_WithListOfAutomobiles()
        {
            // Arrange
            // Act
            var result = await _controller.GetMobiles(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnAutomobiles = Assert.IsType<PageList<AutomobileInfo>>(okResult.Value);
            Assert.Equal(3, returnAutomobiles.Count);
        }

        [Fact]
        public async Task GetMobiles_ReturnsNotFound_WhenNoAutomobilesFound()
        {
            // Arrange
            _mockAutomobileManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<AutomobileInfo>>.Ok(
                    PageList<AutomobileInfo>.Create(new List<AutomobileInfo>().AsQueryable(), 1, 20)));

            // Act
            var result = await _controller.GetMobiles(new ResourceCollectionParameters());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetMobiles_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            _mockAutomobileManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<AutomobileInfo>>.Invalid("Database error"));

            // Act
            var result = await _controller.GetMobiles(new ResourceCollectionParameters());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Database error", response.Errors);
        }

        #endregion

        #region Get automobile by Id

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task GetAutomobileById_ReturnsOkResult_WithAutomobile(int autoId)
        {
            // Arrange
            // Act
            var result = await _controller.GetAutomobileById(autoId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnAutomobile = Assert.IsType<AutomobileInfo>(okResult.Value);
            Assert.Equal(autoId, returnAutomobile.Id);
        }

        [Fact]
        public async Task GetAutomobileById_ReturnsNotFound_WhenAutomobileNotFound()
        {
            // Arrange
            const int autoId = 999;
            _mockAutomobileManager
                .Setup(m => m.GetEntityByIdAsync(autoId))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound($"Automobile {autoId} not found"));

            // Act
            var result = await _controller.GetAutomobileById(autoId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetAutomobileById_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            const int autoId = 1;
            _mockAutomobileManager
                .Setup(m => m.GetEntityByIdAsync(autoId))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Invalid("Database error"));

            // Act
            var result = await _controller.GetAutomobileById(autoId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Create automobile

        [Fact]
        public async Task CreateAutomobile_ReturnsOkResult_WithNewAutomobile()
        {
            // Arrange
            var apiCar = new ApiAutomobileForCreate
            {
                Maker = "Toyota",
                Brand = "Toyota",
                Model = "Camry",
                Year = 2023,
                Plate = "ABC123",
                VIN = "1HGBH41JXMN109186"
            };

            var createdCar = new AutomobileInfo
            {
                Id = 4,
                Maker = "Toyota",
                Brand = "Toyota",
                Model = "Camry",
                Year = 2023,
                Plate = "ABC123",
                VIN = "1HGBH41JXMN109186"
            };

            _mockAutomobileManager
                .Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Ok(createdCar));

            // Act
            var result = await _controller.CreateAutomobile(apiCar);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnAutomobile = Assert.IsType<AutomobileInfo>(okResult.Value);
            Assert.Equal(4, returnAutomobile.Id);
        }

        [Fact]
        public async Task CreateAutomobile_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiCar = new ApiAutomobileForCreate { Maker = "Test" };

            _mockAutomobileManager
                .Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Invalid("Validation failed"));

            // Act
            var result = await _controller.CreateAutomobile(apiCar);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Validation failed", response.Errors);
        }

        #endregion

        #region Update automobile

        [Fact]
        public async Task UpdateAutomobile_ReturnsNoContent_WhenUpdateSuccessful()
        {
            // Arrange
            const int autoId = 1;
            var apiCar = new ApiAutomobileForUpdate
            {
                Color = "Blue",
                Plate = "XYZ789",
                MeterReading = 60000,
                Notes = "Updated notes"
            };

            _mockAutomobileManager
                .Setup(m => m.UpdateAsync(It.IsAny<AutomobileInfo>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Ok(_automobiles[0]));

            // Act
            var result = await _controller.UpdateAutomobile(autoId, apiCar);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateAutomobile_ReturnsBadRequest_WhenApiCarIsNull()
        {
            // Arrange
            const int autoId = 1;

            // Act
            var result = await _controller.UpdateAutomobile(autoId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Automobile data is required", response.Errors);
        }

        [Fact]
        public async Task UpdateAutomobile_ReturnsNotFound_WhenAutomobileNotFound()
        {
            // Arrange
            const int autoId = 999;
            var apiCar = new ApiAutomobileForUpdate { Plate = "TEST123" };

            _mockAutomobileManager
                .Setup(m => m.GetEntityByIdAsync(autoId))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound("Not found"));

            // Act
            var result = await _controller.UpdateAutomobile(autoId, apiCar);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateAutomobile_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int autoId = 1;
            var apiCar = new ApiAutomobileForUpdate { Plate = "TEST123" };

            _mockAutomobileManager
                .Setup(m => m.UpdateAsync(It.IsAny<AutomobileInfo>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Invalid("Update failed"));

            // Act
            var result = await _controller.UpdateAutomobile(autoId, apiCar);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Patch automobile

        [Fact]
        public async Task Patch_ReturnsNoContent_WhenPatchSuccessful()
        {
            // Arrange
            const int autoId = 1;
            var patchDoc = new JsonPatchDocument<ApiAutomobileForUpdate>();
            patchDoc.Replace(a => a.Color, "Updated Color");

            _mockAutomobileManager
                .Setup(m => m.UpdateAsync(It.IsAny<AutomobileInfo>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Ok(_automobiles[0]));

            // Act
            var result = await _controller.Patch(autoId, patchDoc);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenPatchDocIsNull()
        {
            // Arrange
            const int autoId = 1;

            // Act
            var result = await _controller.Patch(autoId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Patch information is null or invalid id found", response.Errors);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<ApiAutomobileForUpdate>();

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenAutomobileNotFound()
        {
            // Arrange
            const int autoId = 999;
            var patchDoc = new JsonPatchDocument<ApiAutomobileForUpdate>();

            _mockAutomobileManager
                .Setup(m => m.GetEntityByIdAsync(autoId))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound("Not found"));

            // Act
            var result = await _controller.Patch(autoId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int autoId = 1;
            var patchDoc = new JsonPatchDocument<ApiAutomobileForUpdate>();
            patchDoc.Replace(a => a.Color, "Test");

            _mockAutomobileManager
                .Setup(m => m.UpdateAsync(It.IsAny<AutomobileInfo>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Patch(autoId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Helper methods

        private void SetupMockManager()
        {
            _mockAutomobileManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<AutomobileInfo>>.Ok(
                    PageList<AutomobileInfo>.Create(_automobiles.AsQueryable(), 1, 20)));

            _mockAutomobileManager
                .Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    var auto = _automobiles.FirstOrDefault(a => a.Id == id);
                    return auto != null
                        ? ProcessingResult<AutomobileInfo>.Ok(auto)
                        : ProcessingResult<AutomobileInfo>.NotFound($"Automobile {id} not found");
                });
        }

        private static List<AutomobileInfo> GetSampleAutomobiles()
        {
            return
            [
                new AutomobileInfo
                {
                    Id = 1,
                    Maker = "Toyota",
                    Brand = "Toyota",
                    Model = "Corolla",
                    Year = 2020,
                    Plate = "ABC123",
                    VIN = "1HGBH41JXMN109186",
                    EngineType = FuelEngineType.Gasoline,
                    FuelType = FuelGrade.Regular
                },
                new AutomobileInfo
                {
                    Id = 2,
                    Maker = "Honda",
                    Brand = "Honda",
                    Model = "Civic",
                    Year = 2021,
                    Plate = "DEF456",
                    VIN = "2HGBH41JXMN109187",
                    EngineType = FuelEngineType.Gasoline,
                    FuelType = FuelGrade.Regular
                },
                new AutomobileInfo
                {
                    Id = 3,
                    Maker = "Ford",
                    Brand = "Ford",
                    Model = "Mustang",
                    Year = 2022,
                    Plate = "GHI789",
                    VIN = "3HGBH41JXMN109188",
                    EngineType = FuelEngineType.Gasoline,
                    FuelType = FuelGrade.Premium
                }
            ];
        }

        #endregion
    }
}
