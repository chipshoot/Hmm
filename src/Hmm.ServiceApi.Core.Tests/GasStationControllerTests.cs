using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class GasStationControllerTests
    {
        private readonly Mock<GasStationManager> _mockStationManager;
        private readonly GasStationController _controller;
        private readonly IMapper _mapper;
        private readonly List<GasStation> _stations;

        public GasStationControllerTests()
        {
            _mockStationManager = new Mock<GasStationManager>(
                Mock.Of<INoteSerializer<GasStation>>(),
                Mock.Of<IHmmValidator<GasStation>>(),
                Mock.Of<IHmmNoteManager>(),
                Mock.Of<IEntityLookup>(),
                Mock.Of<IAuthorProvider>());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutomobileMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _stations = GetSampleStations();
            SetupMockManager();

            _controller = new GasStationController(
                _mockStationManager.Object,
                _mapper,
                new Mock<ILogger<GasStationController>>().Object);

            // Setup controller context with ObjectModelValidator for TryValidateModel
            var objectValidator = new Mock<IObjectModelValidator>();
            objectValidator.Setup(o => o.Validate(
                It.IsAny<ActionContext>(),
                It.IsAny<ValidationStateDictionary>(),
                It.IsAny<string>(),
                It.IsAny<object>()));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ObjectValidator = objectValidator.Object;
        }

        #region Get stations

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfStations()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStations = Assert.IsType<PageList<GasStation>>(okResult.Value);
            Assert.Equal(3, returnStations.Count);
        }

        [Fact]
        public async Task Get_ReturnsOkWithEmptyList_WhenNoStationsFound()
        {
            // Arrange
            _mockStationManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(new List<GasStation>().AsQueryable(), 1, 20)));

            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStations = Assert.IsType<PageList<GasStation>>(okResult.Value);
            Assert.Equal(0, returnStations.Count);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            _mockStationManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Invalid("Database error"));

            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Database error", response.Errors);
        }

        #endregion

        #region Get active stations

        [Fact]
        public async Task GetActive_ReturnsOkResult_WithActiveStations()
        {
            // Arrange
            var activeStations = _stations.Where(s => s.IsActive).ToList();
            _mockStationManager
                .Setup(m => m.GetActiveStationsAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(activeStations.AsQueryable(), 1, 20)));

            // Act
            var result = await _controller.GetActive(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStations = Assert.IsType<PageList<GasStation>>(okResult.Value);
            Assert.Equal(2, returnStations.Count);
        }

        [Fact]
        public async Task GetActive_ReturnsOkWithEmptyList_WhenNoActiveStationsFound()
        {
            // Arrange
            _mockStationManager
                .Setup(m => m.GetActiveStationsAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(new List<GasStation>().AsQueryable(), 1, 20)));

            // Act
            var result = await _controller.GetActive(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStations = Assert.IsType<PageList<GasStation>>(okResult.Value);
            Assert.Equal(0, returnStations.Count);
        }

        [Fact]
        public async Task GetActive_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            _mockStationManager
                .Setup(m => m.GetActiveStationsAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Invalid("Database error"));

            // Act
            var result = await _controller.GetActive(new ResourceCollectionParameters());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Database error", response.Errors);
        }

        #endregion

        #region Get station by Id

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task GetById_ReturnsOkResult_WithStation(int stationId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(stationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStation = Assert.IsType<GasStation>(okResult.Value);
            Assert.Equal(stationId, returnStation.Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenStationNotFound()
        {
            // Arrange
            const int stationId = 999;
            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound($"Station {stationId} not found"));

            // Act
            var result = await _controller.Get(stationId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            const int stationId = 1;
            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ReturnsAsync(ProcessingResult<GasStation>.Invalid("Database error"));

            // Act
            var result = await _controller.Get(stationId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Get station by name

        [Fact]
        public async Task GetByName_ReturnsOkResult_WithStation()
        {
            // Arrange
            const string stationName = "Costco Gas";
            _mockStationManager
                .Setup(m => m.GetByNameAsync(stationName))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(_stations[0]));

            // Act
            var result = await _controller.GetByName(stationName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStation = Assert.IsType<GasStation>(okResult.Value);
            Assert.Equal(stationName, returnStation.Name);
        }

        [Fact]
        public async Task GetByName_ReturnsNotFound_WhenStationNotFound()
        {
            // Arrange
            const string stationName = "Unknown Station";
            _mockStationManager
                .Setup(m => m.GetByNameAsync(stationName))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound("Station not found"));

            // Act
            var result = await _controller.GetByName(stationName);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetByName_ReturnsBadRequest_WhenNameIsEmpty()
        {
            // Arrange
            // Act
            var result = await _controller.GetByName("");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Create station

        [Fact]
        public async Task Post_ReturnsOkResult_WithNewStation()
        {
            // Arrange
            var apiStation = new ApiGasStationForCreate
            {
                Name = "New Gas Station",
                Address = "123 New St",
                City = "Vancouver",
                Country = "Canada",
                State = "BC"
            };

            var createdStation = new GasStation
            {
                Id = 4,
                Name = "New Gas Station",
                Address = "123 New St",
                City = "Vancouver",
                Country = "Canada",
                State = "BC",
                IsActive = true
            };

            _mockStationManager
                .Setup(m => m.CreateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(createdStation));

            // Act
            var result = await _controller.Post(apiStation);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnStation = Assert.IsType<GasStation>(okResult.Value);
            Assert.Equal(4, returnStation.Id);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenApiStationIsNull()
        {
            // Arrange
            // Act
            var result = await _controller.Post(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Station data is required", response.Errors);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiStation = new ApiGasStationForCreate { Name = "Test", City = "Vancouver", Country = "Canada" };

            _mockStationManager
                .Setup(m => m.CreateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Invalid("Validation failed"));

            // Act
            var result = await _controller.Post(apiStation);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Validation failed", response.Errors);
        }

        [Fact]
        public async Task Post_ReturnsInternalServerError_OnException()
        {
            // Arrange
            var apiStation = new ApiGasStationForCreate { Name = "Test", City = "Vancouver", Country = "Canada" };

            _mockStationManager
                .Setup(m => m.CreateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Post(apiStation);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Update station

        [Fact]
        public async Task Put_ReturnsNoContent_WhenUpdateSuccessful()
        {
            // Arrange
            const int stationId = 1;
            var apiStation = new ApiGasStationForUpdate
            {
                Name = "Updated Station Name",
                Address = "456 Updated St"
            };

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(_stations[0]));

            // Act
            var result = await _controller.Put(stationId, apiStation);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_WhenApiStationIsNull()
        {
            // Arrange
            const int stationId = 1;

            // Act
            var result = await _controller.Put(stationId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Station data is required", response.Errors);
        }

        [Fact]
        public async Task Put_ReturnsNotFound_WhenStationNotFound()
        {
            // Arrange
            const int stationId = 999;
            var apiStation = new ApiGasStationForUpdate { Name = "Test" };

            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound("Not found"));

            // Act
            var result = await _controller.Put(stationId, apiStation);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int stationId = 1;
            var apiStation = new ApiGasStationForUpdate { Name = "Test" };

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Invalid("Update failed"));

            // Act
            var result = await _controller.Put(stationId, apiStation);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Patch station

        [Fact]
        public async Task Patch_ReturnsNoContent_WhenPatchSuccessful()
        {
            // Arrange
            const int stationId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasStationForUpdate>();
            patchDoc.Replace(s => s.Description, "Patched description");

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(_stations[0]));

            // Act
            var result = await _controller.Patch(stationId, patchDoc);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenPatchDocIsNull()
        {
            // Arrange
            const int stationId = 1;

            // Act
            var result = await _controller.Patch(stationId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Patch information is null or invalid id found", response.Errors);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<ApiGasStationForUpdate>();

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenStationNotFound()
        {
            // Arrange
            const int stationId = 999;
            var patchDoc = new JsonPatchDocument<ApiGasStationForUpdate>();

            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound("Not found"));

            // Act
            var result = await _controller.Patch(stationId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int stationId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasStationForUpdate>();
            patchDoc.Replace(s => s.Description, "Test");

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Invalid("Update failed"));

            // Act
            var result = await _controller.Patch(stationId, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Patch_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int stationId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasStationForUpdate>();
            patchDoc.Replace(s => s.Description, "Test");

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Patch(stationId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Delete station

        [Fact]
        public async Task Delete_ReturnsOk_WhenDeleteSuccessful()
        {
            // Arrange
            const int stationId = 1;

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(_stations[0]));

            // Act
            var result = await _controller.Delete(stationId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenStationNotFound()
        {
            // Arrange
            const int stationId = 999;

            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound("Station not found"));

            // Act
            var result = await _controller.Delete(stationId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenGetFails()
        {
            // Arrange
            const int stationId = 1;

            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ReturnsAsync(ProcessingResult<GasStation>.Invalid("Database error"));

            // Act
            var result = await _controller.Delete(stationId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_WhenUpdateFails()
        {
            // Arrange
            const int stationId = 1;

            _mockStationManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasStation>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasStation>.Invalid("Update failed"));

            // Act
            var result = await _controller.Delete(stationId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int stationId = 1;

            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(stationId))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Delete(stationId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Helper methods

        private void SetupMockManager()
        {
            _mockStationManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(_stations.AsQueryable(), 1, 20)));

            _mockStationManager
                .Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    var station = _stations.FirstOrDefault(s => s.Id == id);
                    return station != null
                        ? ProcessingResult<GasStation>.Ok(station)
                        : ProcessingResult<GasStation>.NotFound($"Station {id} not found");
                });
        }

        private static List<GasStation> GetSampleStations()
        {
            return
            [
                new GasStation
                {
                    Id = 1,
                    Name = "Costco Gas",
                    Address = "123 Main St",
                    City = "Vancouver",
                    Country = "Canada",
                    State = "BC",
                    ZipCode = "V5K 1A1",
                    Description = "Costco gas station",
                    IsActive = true
                },
                new GasStation
                {
                    Id = 2,
                    Name = "Petro Canada",
                    Address = "456 Oak Ave",
                    City = "Burnaby",
                    Country = "Canada",
                    State = "BC",
                    ZipCode = "V5H 2B2",
                    Description = "Petro Canada station",
                    IsActive = true
                },
                new GasStation
                {
                    Id = 3,
                    Name = "Closed Station",
                    Address = "789 Elm Blvd",
                    City = "Richmond",
                    Country = "Canada",
                    State = "BC",
                    ZipCode = "V6X 3C3",
                    Description = "Permanently closed",
                    IsActive = false
                }
            ];
        }

        #endregion
    }
}
