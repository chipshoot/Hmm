using AutoMapper;
using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Controllers;
using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class GasDiscountControllerTests
    {
        private readonly Mock<IAutoEntityManager<GasDiscount>> _mockDiscountManager;
        private readonly GasDiscountController _controller;
        private readonly IMapper _mapper;
        private readonly List<GasDiscount> _discounts;

        public GasDiscountControllerTests()
        {
            _mockDiscountManager = new Mock<IAutoEntityManager<GasDiscount>>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutomobileMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _discounts = GetSampleDiscounts();
            SetupMockManager();

            _controller = new GasDiscountController(
                _mockDiscountManager.Object,
                _mapper,
                new Mock<ILogger<GasDiscountController>>().Object);

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

        #region Get discounts

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfDiscounts()
        {
            // Arrange
            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDiscounts = Assert.IsType<PageList<GasDiscount>>(okResult.Value);
            Assert.Equal(3, returnDiscounts.Count);
        }

        [Fact]
        public async Task Get_ReturnsNotFound_WhenNoDiscountsFound()
        {
            // Arrange
            _mockDiscountManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(
                    PageList<GasDiscount>.Create(new List<GasDiscount>().AsQueryable(), 1, 20)));

            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            _mockDiscountManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Invalid("Database error"));

            // Act
            var result = await _controller.Get(new ResourceCollectionParameters());

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Database error", response.Errors);
        }

        #endregion

        #region Get discount by Id

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task GetById_ReturnsOkResult_WithDiscount(int discountId)
        {
            // Arrange
            // Act
            var result = await _controller.Get(discountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDiscount = Assert.IsType<GasDiscount>(okResult.Value);
            Assert.Equal(discountId, returnDiscount.Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenDiscountNotFound()
        {
            // Arrange
            const int discountId = 999;
            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ReturnsAsync(ProcessingResult<GasDiscount>.NotFound($"Discount {discountId} not found"));

            // Act
            var result = await _controller.Get(discountId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsBadRequest_WhenManagerFails()
        {
            // Arrange
            const int discountId = 1;
            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Invalid("Database error"));

            // Act
            var result = await _controller.Get(discountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Create discount

        [Fact]
        public async Task Post_ReturnsOkResult_WithNewDiscount()
        {
            // Arrange
            var apiDiscount = new ApiDiscountForCreate
            {
                Program = "New Discount Program",
                Amount = 0.10m,
                DiscountType = "PerLiter",
                Comment = "Test discount"
            };

            var createdDiscount = new GasDiscount
            {
                Id = 4,
                Program = "New Discount Program",
                Amount = new Money(0.10m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                Comment = "Test discount",
                IsActive = true
            };

            _mockDiscountManager
                .Setup(m => m.CreateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Ok(createdDiscount));

            // Act
            var result = await _controller.Post(apiDiscount);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDiscount = Assert.IsType<GasDiscount>(okResult.Value);
            Assert.Equal(4, returnDiscount.Id);
        }

        [Fact]
        public async Task Post_ReturnsBadRequest_WhenCreationFails()
        {
            // Arrange
            var apiDiscount = new ApiDiscountForCreate { Program = "Test" };

            _mockDiscountManager
                .Setup(m => m.CreateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Invalid("Validation failed"));

            // Act
            var result = await _controller.Post(apiDiscount);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Validation failed", response.Errors);
        }

        #endregion

        #region Update discount

        [Fact]
        public async Task Put_ReturnsNoContent_WhenUpdateSuccessful()
        {
            // Arrange
            const int discountId = 1;
            var apiDiscount = new ApiDiscountForUpdate
            {
                Program = "Updated Program",
                Amount = 0.15m
            };

            _mockDiscountManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Ok(_discounts[0]));

            // Act
            var result = await _controller.Put(discountId, apiDiscount);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_WhenApiDiscountIsNull()
        {
            // Arrange
            const int discountId = 1;

            // Act
            var result = await _controller.Put(discountId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Discount data is required", response.Errors);
        }

        [Fact]
        public async Task Put_ReturnsNotFound_WhenDiscountNotFound()
        {
            // Arrange
            const int discountId = 999;
            var apiDiscount = new ApiDiscountForUpdate { Program = "Test" };

            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ReturnsAsync(ProcessingResult<GasDiscount>.NotFound("Not found"));

            // Act
            var result = await _controller.Put(discountId, apiDiscount);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int discountId = 1;
            var apiDiscount = new ApiDiscountForUpdate { Program = "Test" };

            _mockDiscountManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Invalid("Update failed"));

            // Act
            var result = await _controller.Put(discountId, apiDiscount);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Patch discount

        [Fact]
        public async Task Patch_ReturnsNoContent_WhenPatchSuccessful()
        {
            // Arrange
            const int discountId = 1;
            var patchDoc = new JsonPatchDocument<ApiDiscountForUpdate>();
            patchDoc.Replace(d => d.Comment, "Patched comment");

            _mockDiscountManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Ok(_discounts[0]));

            // Act
            var result = await _controller.Patch(discountId, patchDoc);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenPatchDocIsNull()
        {
            // Arrange
            const int discountId = 1;

            // Act
            var result = await _controller.Patch(discountId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
            Assert.Contains("Patch information is null or invalid id found", response.Errors);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var patchDoc = new JsonPatchDocument<ApiDiscountForUpdate>();

            // Act
            var result = await _controller.Patch(0, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Patch_ReturnsNotFound_WhenDiscountNotFound()
        {
            // Arrange
            const int discountId = 999;
            var patchDoc = new JsonPatchDocument<ApiDiscountForUpdate>();

            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ReturnsAsync(ProcessingResult<GasDiscount>.NotFound("Not found"));

            // Act
            var result = await _controller.Patch(discountId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Patch_ReturnsBadRequest_WhenUpdateFails()
        {
            // Arrange
            const int discountId = 1;
            var patchDoc = new JsonPatchDocument<ApiDiscountForUpdate>();
            patchDoc.Replace(d => d.Comment, "Test");

            _mockDiscountManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Invalid("Update failed"));

            // Act
            var result = await _controller.Patch(discountId, patchDoc);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        #endregion

        #region Delete discount

        [Fact]
        public async Task Delete_ReturnsOk_WhenDeleteSuccessful()
        {
            // Arrange
            const int discountId = 1;

            _mockDiscountManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Ok(_discounts[0]));

            // Act
            var result = await _controller.Delete(discountId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenDiscountNotFound()
        {
            // Arrange
            const int discountId = 999;

            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ReturnsAsync(ProcessingResult<GasDiscount>.NotFound("Discount not found"));

            // Act
            var result = await _controller.Delete(discountId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsBadRequest_WhenGetFails()
        {
            // Arrange
            const int discountId = 1;

            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Invalid("Database error"));

            // Act
            var result = await _controller.Delete(discountId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ApiBadRequestResponse>(badRequestResult.Value);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_WhenUpdateFails()
        {
            // Arrange
            const int discountId = 1;

            _mockDiscountManager
                .Setup(m => m.UpdateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<GasDiscount>.Invalid("Update failed"));

            // Act
            var result = await _controller.Delete(discountId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsInternalServerError_OnException()
        {
            // Arrange
            const int discountId = 1;

            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(discountId))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.Delete(discountId);

            // Assert
            var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        }

        #endregion

        #region Helper methods

        private void SetupMockManager()
        {
            _mockDiscountManager
                .Setup(m => m.GetEntitiesAsync(It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<GasDiscount>>.Ok(
                    PageList<GasDiscount>.Create(_discounts.AsQueryable(), 1, 20)));

            _mockDiscountManager
                .Setup(m => m.GetEntityByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    var discount = _discounts.FirstOrDefault(d => d.Id == id);
                    return discount != null
                        ? ProcessingResult<GasDiscount>.Ok(discount)
                        : ProcessingResult<GasDiscount>.NotFound($"Discount {id} not found");
                });
        }

        private static List<GasDiscount> GetSampleDiscounts()
        {
            return
            [
                new GasDiscount
                {
                    Id = 1,
                    Program = "Costco Membership",
                    Amount = new Money(0.05m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter,
                    IsActive = true,
                    Comment = "Costco gas discount"
                },
                new GasDiscount
                {
                    Id = 2,
                    Program = "PC Optimum",
                    Amount = new Money(0.03m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter,
                    IsActive = true,
                    Comment = "Superstore gas discount"
                },
                new GasDiscount
                {
                    Id = 3,
                    Program = "Journie Rewards",
                    Amount = new Money(0.02m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter,
                    IsActive = true,
                    Comment = "Petro Canada discount"
                }
            ];
        }

        #endregion
    }
}
