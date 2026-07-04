using Hmm.ServiceApi.Areas.UtilityService.Controllers;
using Hmm.ServiceApi.DtoEntity.Utility;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Misc;
using Hmm.Utility.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Threading;

namespace Hmm.ServiceApi.Core.Tests
{
    public class ReceiptExtractionControllerTests
    {
        private readonly Mock<IReceiptExtractionService> _service = new();
        private readonly ReceiptExtractionController _controller;

        public ReceiptExtractionControllerTests()
        {
            _controller = new ReceiptExtractionController(
                _service.Object,
                new Mock<ILogger<ReceiptExtractionController>>().Object);
        }

        private static IFormFile FakeFile(string contentType, long length, byte[] content = null)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.ContentType).Returns(contentType);
            mock.Setup(f => f.Length).Returns(length);
            mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream s, CancellationToken ct) =>
                {
                    if (content != null) s.Write(content, 0, content.Length);
                    return Task.CompletedTask;
                });
            return mock.Object;
        }

        [Fact]
        public async Task Extract_WithNoFile_ReturnsBadRequest()
        {
            var result = await _controller.Extract(FakeFile("image/jpeg", 0));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Extract_WithUnsupportedType_Returns415()
        {
            var result = await _controller.Extract(FakeFile("text/plain", 10));
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, obj.StatusCode);
        }

        [Fact]
        public async Task Extract_WithOversizeFile_Returns413()
        {
            var result = await _controller.Extract(FakeFile("image/jpeg", 9L * 1024 * 1024));
            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, obj.StatusCode);
        }

        [Fact]
        public async Task Extract_WhenServiceFails_ReturnsBadRequest()
        {
            _service.Setup(s => s.ExtractAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ProcessingResult<ReceiptExtractionResult>.Fail("boom"));

            var result = await _controller.Extract(FakeFile("image/jpeg", 3, new byte[] { 1, 2, 3 }));

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(bad.Value);
            Assert.Contains("boom", response.Errors);
        }

        [Fact]
        public async Task Extract_PassesEngineAndPurposeToService()
        {
            string capturedEngine = null, capturedPurpose = null;
            _service.Setup(s => s.ExtractAsync(
                    It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<byte[], string, string, string>(
                    (b, c, e, p) => { capturedEngine = e; capturedPurpose = p; })
                .ReturnsAsync(ProcessingResult<ReceiptExtractionResult>.Ok(new ReceiptExtractionResult()));

            await _controller.Extract(
                FakeFile("image/jpeg", 3, new byte[] { 1, 2, 3 }), "local", "personal");

            Assert.Equal("local", capturedEngine);
            Assert.Equal("personal", capturedPurpose);
        }

        [Fact]
        public async Task Extract_OnSuccess_ReturnsOkWithMappedDraft()
        {
            var extraction = new ReceiptExtractionResult
            {
                ShopName = "Bob Auto",
                Odometer = 45000,
                Tax = 3.5,
                LineItems =
                {
                    new ReceiptExtractionLineItem
                    {
                        Type = "Part", Name = "Filter", Quantity = 2, UnitCost = 10
                    }
                }
            };
            _service.Setup(s => s.ExtractAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ProcessingResult<ReceiptExtractionResult>.Ok(extraction));

            var result = await _controller.Extract(FakeFile("image/jpeg", 3, new byte[] { 1, 2, 3 }));

            var ok = Assert.IsType<OkObjectResult>(result);
            var draft = Assert.IsType<ApiReceiptDraft>(ok.Value);
            Assert.Equal("Bob Auto", draft.ShopName);
            Assert.Equal(45000, draft.Odometer.Value);
            Assert.Single(draft.LineItems);
            Assert.Equal("Filter", draft.LineItems[0].Name);
            Assert.Equal(2, draft.LineItems[0].Quantity);
        }
    }
}
