using AutoMapper;
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Controllers;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class CheatsheetsControllerTests
    {
        private readonly Mock<ICheatsheetManager> _manager = new();
        private readonly CheatsheetsController _controller;

        public CheatsheetsControllerTests()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance);

            _controller = new CheatsheetsController(
                _manager.Object,
                config.CreateMapper(),
                new Mock<ILogger<CheatsheetsController>>().Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        private static CheatsheetCard Card(string id = "c-1", string title = "Passport") => new()
        {
            NoteId = 42,
            AuthorId = 9,
            Id = id,
            Title = title,
            WalletGroup = "Travel",
            TemplateId = "blank"
        };

        private static PageList<CheatsheetCard> Page(params CheatsheetCard[] cards)
            => new(cards, cards.Length, 1, 10);

        [Fact]
        public async Task Get_ReturnsOkWithThePage()
        {
            _manager
                .Setup(m => m.GetCardsAsync(null, null, It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<CheatsheetCard>>.Ok(Page(Card(), Card("c-2", "Alarm"))));

            var result = await _controller.Get(null, null, new ResourceCollectionParameters());

            var ok = Assert.IsType<OkObjectResult>(result);
            var page = Assert.IsType<PageList<CheatsheetCard>>(ok.Value);
            Assert.Equal(2, page.Count);
        }

        [Fact]
        public async Task Get_PassesFiltersThrough()
        {
            _manager
                .Setup(m => m.GetCardsAsync("Home", "trip", It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<CheatsheetCard>>.Ok(Page(Card())));

            await _controller.Get("Home", "trip", new ResourceCollectionParameters());

            _manager.Verify(
                m => m.GetCardsAsync("Home", "trip", It.IsAny<ResourceCollectionParameters>()),
                Times.Once);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenManagerFails()
        {
            _manager
                .Setup(m => m.GetCardsAsync(null, null, It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<CheatsheetCard>>.Invalid("Database error"));

            var result = await _controller.Get(null, null, new ResourceCollectionParameters());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequest.Value);
            Assert.Contains("Database error", response.Errors);
        }

        [Fact]
        public async Task GetById_ReturnsOk()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("c-1"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Ok(Card()));

            var result = await _controller.Get("c-1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("c-1", Assert.IsType<CheatsheetCard>(ok.Value).Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("missing"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.NotFound());

            var result = await _controller.Get("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsCreatedAtRoute()
        {
            _manager
                .Setup(m => m.CreateAsync(It.IsAny<CheatsheetCard>(), true))
                .ReturnsAsync((CheatsheetCard card, bool _) => ProcessingResult<CheatsheetCard>.Ok(card));

            var result = await _controller.Post(new ApiCheatsheetForCreate
            {
                Id = "c-1",
                Title = "Passport",
                WalletGroup = "Travel",
                TemplateId = "blank"
            });

            var created = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal("GetCheatsheetById", created.RouteName);
            Assert.Equal("c-1", Assert.IsType<CheatsheetCard>(created.Value).Id);
        }

        [Fact]
        public async Task Post_NullBody_ReturnsBadRequest()
        {
            var result = await _controller.Post(null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Post_Conflict_ReturnsConflict()
        {
            _manager
                .Setup(m => m.CreateAsync(It.IsAny<CheatsheetCard>(), true))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Conflict("already exists"));

            var result = await _controller.Post(new ApiCheatsheetForCreate { Id = "c-1", Title = "T" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("c-1"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Ok(Card()));
            _manager
                .Setup(m => m.UpdateAsync(It.IsAny<CheatsheetCard>(), true))
                .ReturnsAsync((CheatsheetCard card, bool _) => ProcessingResult<CheatsheetCard>.Ok(card));

            var result = await _controller.Put("c-1", new ApiCheatsheetForUpdate
            {
                Title = "Renewed",
                WalletGroup = "Documents",
                TemplateId = "blank"
            });

            Assert.IsType<NoContentResult>(result);
            _manager.Verify(
                m => m.UpdateAsync(It.Is<CheatsheetCard>(c => c.Id == "c-1" && c.Title == "Renewed"), true),
                Times.Once);
        }

        [Fact]
        public async Task Put_UnknownCard_ReturnsNotFound()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("missing"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.NotFound());

            var result = await _controller.Put("missing", new ApiCheatsheetForUpdate { Title = "T" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_NullBody_ReturnsBadRequest()
        {
            var result = await _controller.Put("c-1", null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _manager
                .Setup(m => m.DeleteAsync("c-1"))
                .ReturnsAsync(ProcessingResult<Unit>.Ok(Unit.Value));

            var result = await _controller.Delete("c-1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_UnknownCard_ReturnsNotFound()
        {
            _manager
                .Setup(m => m.DeleteAsync("missing"))
                .ReturnsAsync(ProcessingResult<Unit>.NotFound());

            var result = await _controller.Delete("missing");

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
