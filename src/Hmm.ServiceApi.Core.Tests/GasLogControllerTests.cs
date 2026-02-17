using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.Services;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class GasLogControllerTests
    {
        private readonly Mock<IGasLogManager> _mockGasLogManager;
        private readonly Mock<IAutoEntityManager<AutomobileInfo>> _mockAutoManager;
        private readonly Mock<IAutoEntityManager<GasDiscount>> _mockDiscountManager;
        private readonly Mock<IPropertyMappingService> _mockPropertyMappingService;
        private readonly Mock<IPropertyCheckService> _mockPropertyCheckService;
        private readonly GasLogController _controller;
        private readonly IMapper _mapper;
        private readonly List<GasLog> _gasLogs;
        private readonly List<AutomobileInfo> _automobiles;

        public GasLogControllerTests()
        {
            _mockGasLogManager = new Mock<IGasLogManager>();
            _mockAutoManager = new Mock<IAutoEntityManager<AutomobileInfo>>();
            _mockDiscountManager = new Mock<IAutoEntityManager<GasDiscount>>();
            _mockPropertyMappingService = new Mock<IPropertyMappingService>();
            _mockPropertyCheckService = new Mock<IPropertyCheckService>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutomobileMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _automobiles = GetSampleAutomobiles();
            _gasLogs = GetSampleGasLogs();
            SetupMocks();

            _controller = new GasLogController(
                _mockGasLogManager.Object,
                _mapper,
                _mockAutoManager.Object,
                _mockDiscountManager.Object,
                _mockPropertyMappingService.Object,
                _mockPropertyCheckService.Object,
                new Mock<ILogger<GasLogController>>().Object);

            // Setup controller context
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Get gas logs

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfGasLogs()
        {
            // Arrange
            const int autoId = 1;

            // Act
            var result = await _controller.Get(autoId, new GasLogResourceParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnGasLogs = Assert.IsType<PageList<GasLog>>(okResult.Value);
            Assert.Equal(2, returnGasLogs.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNoGasLogsFound()
        {
            // Arrange
            const int autoId = 1;
            _mockGasLogManager
                .Setup(m => m.GetGasLogsAsync(autoId, It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasLog>>.Ok(
                    PageList<GasLog>.Create(new List<GasLog>().AsQueryable(), 1, 20)));

            // Act
            var result = await _controller.Get(autoId, new GasLogResourceParameters());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            const int autoId = 1;
            _mockGasLogManager
                .Setup(m => m.GetGasLogsAsync(autoId, It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasLog>>.Invalid("Database error"));

            // Act
            var result = await _controller.Get(autoId, new GasLogResourceParameters());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Database error", response.Errors);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenOrderByInvalid()
        {
            // Arrange
            const int autoId = 1;
            var resourceParams = new GasLogResourceParameters { OrderBy = "InvalidField" };

            _mockPropertyMappingService
                .Setup(m => m.ValidMappingExistsFor<ApiGasLog, GasLog>(It.IsAny<string>()))
                .Returns(ProcessingResult<bool>.Invalid("Invalid order by field"));

            // Act
            var result = await _controller.Get(autoId, resourceParams);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenFieldsInvalid()
        {
            // Arrange
            const int autoId = 1;
            var resourceParams = new GasLogResourceParameters { Fields = "InvalidField" };

            _mockPropertyCheckService
                .Setup(m => m.TypeHasProperties<ApiGasLog>(It.IsAny<string>()))
                .Returns(ProcessingResult<bool>.Invalid("Invalid field specified"));

            // Act
            var result = await _controller.Get(autoId, resourceParams);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Get gas log by Id

        [Fact]
        public async Task GetById_ReturnsOkResult_WithGasLog()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 1;

            // Act
            var result = await _controller.Get(autoId, logId, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnGasLog = Assert.IsType<GasLog>(okResult.Value);
            Assert.Equal(logId, returnGasLog.Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenGasLogNotFound()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 999;

            _mockGasLogManager
                .Setup(m => m.GetEntityByIdAsync(logId))
                .ReturnsAsync(ProcessingResult<GasLog>.NotFound("Gas log not found"));

            // Act
            var result = await _controller.Get(autoId, logId, null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenGasLogBelongsToDifferentAuto()
        {
            // Arrange
            const int autoId = 2;
            const int logId = 1;

            // Act
            var result = await _controller.Get(autoId, logId, null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsBadRequest_WhenFieldsInvalid()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 1;

            _mockPropertyCheckService
                .Setup(m => m.TypeHasProperties<ApiGasLog>(It.IsAny<string>()))
                .Returns(ProcessingResult<bool>.Invalid("Invalid field"));

            // Act
            var result = await _controller.Get(autoId, logId, "invalidField");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Create gas log

        [Fact]
        public async Task Post_ReturnsOkResult_WithNewGasLog()
        {
            // Arrange
            const int autoId = 1;
            var apiGasLog = new ApiGasLogForCreation
            {
                AutomobileId = autoId,
                Date = DateTime.UtcNow,
                Odometer = 50000,
                Distance = 400,
                Fuel = 40,
                FuelGrade = "Regular",
                TotalPrice = 60,
                UnitPrice = 1.5m
            };

            var createdGasLog = new GasLog
            {
                Id = 3,
                AutomobileId = _automobiles[0].Id,
                Date = apiGasLog.Date,
                Odometer = new Dimension(50000, DimensionUnit.Kilometre),
                Distance = new Dimension(400, DimensionUnit.Kilometre),
                Fuel = new Volume(40, VolumeUnit.Gallon),
                TotalPrice = new Money((decimal)60, CurrencyCodeType.Cad),
                UnitPrice = new Money((decimal)1.5, CurrencyCodeType.Cad)
            };

            _mockGasLogManager
                .Setup(m => m.CreateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Ok(createdGasLog));

            // Act
            var result = await _controller.Post(autoId, apiGasLog);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnGasLog = Assert.IsType<GasLog>(okResult.Value);
            Assert.Equal(3, returnGasLog.Id);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenApiGasLogIsNull()
        {
            // Arrange
            const int autoId = 1;

            // Act
            var result = await _controller.Post(autoId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Null gas log found", response.Errors);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenAutomobileNotFound()
        {
            // Arrange
            const int autoId = 999;
            var apiGasLog = new ApiGasLogForCreation
            {
                Date = DateTime.UtcNow,
                FuelGrade = "Regular"
            };

            _mockAutoManager
                .Setup(m => m.GetEntityByIdAsync(autoId))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound("Automobile not found"));

            // Act
            var result = await _controller.Post(autoId, apiGasLog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            const int autoId = 1;
            var apiGasLog = new ApiGasLogForCreation
            {
                Date = DateTime.UtcNow,
                FuelGrade = "Regular"
            };

            _mockGasLogManager
                .Setup(m => m.CreateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Invalid("Validation failed"));

            // Act
            var result = await _controller.Post(autoId, apiGasLog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Post_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int autoId = 1;
            var apiGasLog = new ApiGasLogForCreation
            {
                Date = DateTime.UtcNow,
                FuelGrade = "Regular"
            };

            _mockGasLogManager
                .Setup(m => m.CreateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Post(autoId, apiGasLog);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region History log

        [Fact]
        public async Task HistoryLog_ReturnsOkResult_WithNewGasLog()
        {
            // Arrange
            const int autoId = 1;
            var apiGasLog = new ApiGasLogForCreation
            {
                AutomobileId = autoId,
                Date = DateTime.UtcNow.AddDays(-30),
                Odometer = 49000,
                Distance = 350,
                Fuel = 35,
                FuelGrade = "Regular",
                TotalPrice = 55,
                UnitPrice = 1.57m
            };

            var createdGasLog = new GasLog
            {
                Id = 4,
                AutomobileId = _automobiles[0].Id,
                Date = apiGasLog.Date,
                Odometer = new Dimension(49000, DimensionUnit.Kilometre),
                Distance = new Dimension(350, DimensionUnit.Kilometre),
                Fuel = new Volume(35, VolumeUnit.Gallon),
                TotalPrice = new Money((decimal)55, CurrencyCodeType.Cad),
                UnitPrice = new Money((decimal)1.57, CurrencyCodeType.Cad)
            };

            _mockGasLogManager
                .Setup(m => m.LogHistoryAsync(It.IsAny<GasLog>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Ok(createdGasLog));

            // Act
            var result = await _controller.HistoryLog(autoId, apiGasLog);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnGasLog = Assert.IsType<GasLog>(okResult.Value);
            Assert.Equal(4, returnGasLog.Id);
        }

        [Fact]
        public async Task HistoryLog_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            const int autoId = 1;
            var apiGasLog = new ApiGasLogForCreation
            {
                AutomobileId = autoId,
                Date = DateTime.UtcNow.AddDays(-30),
                Odometer = 49000,
                Fuel = 35,
                FuelGrade = "Regular",
                TotalPrice = 55,
                UnitPrice = 1.57m
            };

            _mockGasLogManager
                .Setup(m => m.LogHistoryAsync(It.IsAny<GasLog>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Invalid("Validation failed"));

            // Act
            var result = await _controller.HistoryLog(autoId, apiGasLog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task HistoryLog_ReturnsBadRequest_WhenAutomobileNotFound()
        {
            // Arrange
            const int autoId = 999;
            var apiGasLog = new ApiGasLogForCreation
            {
                Date = DateTime.UtcNow.AddDays(-30),
                FuelGrade = "Regular"
            };

            _mockAutoManager
                .Setup(m => m.GetEntityByIdAsync(autoId))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.NotFound("Automobile not found"));

            // Act
            var result = await _controller.HistoryLog(autoId, apiGasLog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task HistoryLog_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int autoId = 1;
            var apiGasLog = new ApiGasLogForCreation
            {
                Date = DateTime.UtcNow.AddDays(-30),
                FuelGrade = "Regular"
            };

            _mockGasLogManager
                .Setup(m => m.LogHistoryAsync(It.IsAny<GasLog>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.HistoryLog(autoId, apiGasLog);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task HistoryLog_ReturnsBadRequest_WhenApiGasLogIsNull()
        {
            // Arrange
            const int autoId = 1;

            // Act
            var result = await _controller.HistoryLog(autoId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Update gas log

        [Fact]
        public async Task Put_ReturnsOkResult_WithUpdatedGasLog()
        {
            // Arrange
            const int logId = 1;
            var apiGasLog = new ApiGasLogForUpdate
            {
                Comment = "Updated comment"
            };

            var updatedGasLog = _gasLogs[0];
            updatedGasLog.Comment = "Updated comment";

            _mockGasLogManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Ok(updatedGasLog));

            // Act
            var result = await _controller.Put(logId, apiGasLog);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnApiLog = Assert.IsType<ApiGasLog>(okResult.Value);
            Assert.NotNull(returnApiLog);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_WhenApiGasLogIsNull()
        {
            // Arrange
            const int logId = 1;

            // Act
            var result = await _controller.Put(logId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Put_ReturnsNotFound_WhenGasLogNotFound()
        {
            // Arrange
            const int logId = 999;
            var apiGasLog = new ApiGasLogForUpdate { Comment = "Test" };

            _mockGasLogManager
                .Setup(m => m.GetEntityByIdAsync(logId))
                .ReturnsAsync(ProcessingResult<GasLog>.NotFound("Gas log not found"));

            // Act
            var result = await _controller.Put(logId, apiGasLog);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int logId = 1;
            var apiGasLog = new ApiGasLogForUpdate { Comment = "Test" };

            _mockGasLogManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Invalid("Update failed"));

            // Act
            var result = await _controller.Put(logId, apiGasLog);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Patch gas log

        [Fact]
        public async Task Patch_ReturnsNoContent_WhenPatchSuccessful()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasLogForUpdate>();
            patchDoc.Replace(g => g.Comment, "Patched comment");

            // Create a mock mapper that handles the complex type conversions
            var mockMapper = new Mock<IMapper>();
            var apiGasLogForUpdate = new ApiGasLogForUpdate { Comment = "Original comment" };
            mockMapper.Setup(m => m.Map<ApiGasLogForUpdate>(It.IsAny<GasLog>())).Returns(apiGasLogForUpdate);
            mockMapper.Setup(m => m.Map(It.IsAny<ApiGasLogForUpdate>(), It.IsAny<GasLog>()));

            var controller = new GasLogController(
                _mockGasLogManager.Object,
                mockMapper.Object,
                _mockAutoManager.Object,
                _mockDiscountManager.Object,
                _mockPropertyMappingService.Object,
                _mockPropertyCheckService.Object,
                new Mock<ILogger<GasLogController>>().Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            _mockGasLogManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Ok(_gasLogs[0]));

            // Act
            var result = await controller.Patch(autoId, logId, patchDoc);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenPatchDocIsNull()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 1;

            // Act
            var result = await _controller.Patch(autoId, logId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            const int autoId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasLogForUpdate>();

            // Act
            var result = await _controller.Patch(autoId, 0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenGasLogNotFound()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 999;
            var patchDoc = new JsonPatchDocument<ApiGasLogForUpdate>();

            _mockGasLogManager
                .Setup(m => m.GetEntityByIdAsync(logId))
                .ReturnsAsync(ProcessingResult<GasLog>.NotFound("Gas log not found"));

            // Act
            var result = await _controller.Patch(autoId, logId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenGasLogBelongsToDifferentAuto()
        {
            // Arrange
            const int autoId = 2;
            const int logId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasLogForUpdate>();

            // Act
            var result = await _controller.Patch(autoId, logId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int autoId = 1;
            const int logId = 1;
            var patchDoc = new JsonPatchDocument<ApiGasLogForUpdate>();
            patchDoc.Replace(g => g.Comment, "Test");

            _mockGasLogManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Patch(autoId, logId, patchDoc);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Delete gas log

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenDeleteSuccessful()
        {
            // Arrange
            const int logId = 1;

            _mockGasLogManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Ok(_gasLogs[0]));

            // Act
            var result = await _controller.Delete(logId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenGasLogNotFound()
        {
            // Arrange
            const int logId = 999;

            _mockGasLogManager
                .Setup(m => m.GetEntityByIdAsync(logId))
                .ReturnsAsync(ProcessingResult<GasLog>.NotFound("Gas log not found"));

            // Act
            var result = await _controller.Delete(logId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("999", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenGetFails()
        {
            // Arrange
            const int logId = 1;

            _mockGasLogManager
                .Setup(m => m.GetEntityByIdAsync(logId))
                .ReturnsAsync(ProcessingResult<GasLog>.Invalid("Database error"));

            // Act
            var result = await _controller.Delete(logId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_WhenUpdateFails()
        {
            // Arrange
            const int logId = 1;

            _mockGasLogManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasLog>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasLog>.Invalid("Update failed"));

            // Act
            var result = await _controller.Delete(logId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Helper methods

        private void SetupMocks()
        {
            // Setup property services
            _mockPropertyMappingService
                .Setup(m => m.ValidMappingExistsFor<ApiGasLog, GasLog>(It.IsAny<string>()))
                .Returns(ProcessingResult<bool>.Ok(true));

            _mockPropertyMappingService
                .Setup(m => m.GetPropertyMapping<ApiGasLog, GasLog>())
                .Returns(ProcessingResult<Dictionary<string, PropertyMappingValue>>.Ok(
                    new Dictionary<string, PropertyMappingValue>()));

            _mockPropertyCheckService
                .Setup(m => m.TypeHasProperties<ApiGasLog>(It.IsAny<string>()))
                .Returns(ProcessingResult<bool>.Ok(true));

            // Setup automobile manager
            _mockAutoManager
                .Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    var auto = _automobiles.FirstOrDefault(a => a.Id == id);
                    return auto != null
                        ? ProcessingResult<AutomobileInfo>.Ok(auto)
                        : ProcessingResult<AutomobileInfo>.NotFound($"Automobile {id} not found");
                });

            // Setup gas log manager
            _mockGasLogManager
                .Setup(m => m.GetGasLogsAsync(It.IsAny<int>(), It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((int autoId, ResourceCollectionParameters _) =>
                {
                    var logs = _gasLogs.Where(g => g.AutomobileId == autoId).ToList();
                    return ProcessingResult<PageList<GasLog>>.Ok(
                        PageList<GasLog>.Create(logs.AsQueryable(), 1, 20));
                });

            _mockGasLogManager
                .Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    var log = _gasLogs.FirstOrDefault(g => g.Id == id);
                    return log != null
                        ? ProcessingResult<GasLog>.Ok(log)
                        : ProcessingResult<GasLog>.NotFound($"Gas log {id} not found");
                });
        }

        private List<AutomobileInfo> GetSampleAutomobiles()
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
                }
            ];
        }

        private List<GasLog> GetSampleGasLogs()
        {
            return
            [
                new GasLog
                {
                    Id = 1,
                    AutomobileId = _automobiles[0].Id,
                    Date = DateTime.UtcNow.AddDays(-7),
                    Odometer = new Dimension(50000, DimensionUnit.Kilometre),
                    Distance = new Dimension(400, DimensionUnit.Kilometre),
                    Fuel = new Volume(40, VolumeUnit.Gallon),
                    FuelGrade = FuelGrade.Regular,
                    TotalPrice = new Money((decimal)60, CurrencyCodeType.Cad),
                    UnitPrice = new Money((decimal)1.5, CurrencyCodeType.Cad),
                    IsFullTank = true
                },
                new GasLog
                {
                    Id = 2,
                    AutomobileId = _automobiles[0].Id,
                    Date = DateTime.UtcNow.AddDays(-14),
                    Odometer = new Dimension(49600, DimensionUnit.Kilometre),
                    Distance = new Dimension(380, DimensionUnit.Kilometre),
                    Fuel = new Volume(38, VolumeUnit.Gallon),
                    FuelGrade = FuelGrade.Regular,
                    TotalPrice = new Money((decimal)57, CurrencyCodeType.Cad),
                    UnitPrice = new Money((decimal)1.5, CurrencyCodeType.Cad),
                    IsFullTank = true
                }
            ];
        }

        #endregion
    }
}
