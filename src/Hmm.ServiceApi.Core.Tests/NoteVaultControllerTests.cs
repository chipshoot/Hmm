using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Vault;
using Hmm.ServiceApi.Areas.HmmNoteService.Controllers;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    /// <summary>
    /// Comprehensive coverage of the new <see cref="NoteVaultController"/>
    /// surface. Storage logic itself is exercised by
    /// <c>FilesystemVaultBlobStoreTests</c> in <c>Hmm.Core.Vault.Tests</c>;
    /// these tests verify the HTTP wiring + ownership checks + error
    /// mapping.
    /// </summary>
    public class NoteVaultControllerTests
    {
        // ------------------------------------------------------------
        // Test harness
        // ------------------------------------------------------------

        private const int OwnerId = 7;
        private const int OtherAuthorId = 99;
        private const int NoteId = 42;

        private readonly Mock<IVaultBlobStore> _store = new();
        private readonly Mock<IHmmNoteManager> _noteManager = new();
        private readonly Mock<ICurrentUserAuthorProvider> _authorProvider = new();
        private readonly AttachmentSettings _settings = new()
        {
            RootDir = "/tmp/test-vault",
            MaxBytes = 100,
            AllowedContentTypes = new() { "image/jpeg", "image/png" },
            MaxLongEdgePixels = 4096,
        };

        private NoteVaultController BuildController(
            byte[]? body = null,
            string? contentType = null,
            long? contentLength = null)
        {
            var controller = new NoteVaultController(
                _store.Object,
                Options.Create(_settings),
                _authorProvider.Object,
                _noteManager.Object,
                Mock.Of<ILogger<NoteVaultController>>());

            var httpContext = new DefaultHttpContext();
            if (body != null)
            {
                httpContext.Request.Body = new MemoryStream(body);
            }
            if (contentType != null)
            {
                httpContext.Request.ContentType = contentType;
            }
            if (contentLength != null)
            {
                httpContext.Request.ContentLength = contentLength;
            }
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };
            return controller;
        }

        private void SetupOwner()
        {
            _authorProvider
                .Setup(p => p.GetCurrentUserAuthorAsync())
                .ReturnsAsync(ProcessingResult<Author>.Ok(
                    new Author { Id = OwnerId, AccountName = "owner" }));
        }

        private void SetupUnauthenticated()
        {
            _authorProvider
                .Setup(p => p.GetCurrentUserAuthorAsync())
                .ReturnsAsync(ProcessingResult<Author>.Fail(
                    "no user", ErrorCategory.Unauthorized));
        }

        private void SetupNoteOwned()
        {
            _noteManager
                .Setup(m => m.GetNoteByIdAsync(NoteId, false))
                .ReturnsAsync(ProcessingResult<HmmNote>.Ok(new HmmNote
                {
                    Id = NoteId,
                    Subject = "test",
                    Content = "{}",
                    Author = new Author { Id = OwnerId, AccountName = "owner" },
                }));
        }

        private void SetupNoteNotFound()
        {
            _noteManager
                .Setup(m => m.GetNoteByIdAsync(NoteId, false))
                .ReturnsAsync(ProcessingResult<HmmNote>.NotFound());
        }

        private void SetupNoteOwnedByOther()
        {
            _noteManager
                .Setup(m => m.GetNoteByIdAsync(NoteId, false))
                .ReturnsAsync(ProcessingResult<HmmNote>.Ok(new HmmNote
                {
                    Id = NoteId,
                    Subject = "test",
                    Content = "{}",
                    Author = new Author { Id = OtherAuthorId, AccountName = "other" },
                }));
        }

        // ------------------------------------------------------------
        // POST (Put)
        // ------------------------------------------------------------

        public class Put : NoteVaultControllerTests
        {
            [Fact]
            public async Task Happy_path_returns_200_with_VaultRef_and_writes_to_store()
            {
                SetupOwner();
                SetupNoteOwned();
                var bytes = Encoding.UTF8.GetBytes("hello");
                var controller = BuildController(bytes, "image/jpeg");

                var result = await controller.Put(NoteId, "abc.jpg", CancellationToken.None);

                var ok = Assert.IsType<OkObjectResult>(result);
                var vaultRef = Assert.IsType<VaultRef>(ok.Value);
                Assert.Equal("attachments/note-42/abc.jpg", vaultRef.Path);
                Assert.Equal("image/jpeg", vaultRef.ContentType);
                Assert.Equal(bytes.Length, vaultRef.ByteSize);
                _store.Verify(
                    s => s.PutBytesAsync(
                        OwnerId,
                        "attachments/note-42/abc.jpg",
                        It.IsAny<ReadOnlyMemory<byte>>(),
                        "image/jpeg",
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Fact]
            public async Task Content_Type_outside_allow_list_returns_415()
            {
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"), "image/bmp");

                var result = await controller.Put(NoteId, "x.bmp", CancellationToken.None);

                var obj = Assert.IsType<ObjectResult>(result);
                Assert.Equal(StatusCodes.Status415UnsupportedMediaType, obj.StatusCode);
                _store.Verify(s => s.PutBytesAsync(
                    It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Fact]
            public async Task Content_Type_with_charset_param_is_trimmed_before_allow_list_check()
            {
                // "image/jpeg; charset=utf-8" should match "image/jpeg".
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("hello"),
                    "image/jpeg; charset=utf-8");

                var result = await controller.Put(NoteId, "abc.jpg", CancellationToken.None);

                Assert.IsType<OkObjectResult>(result);
            }

            [Fact]
            public async Task Content_Length_header_over_max_returns_413_before_reading_body()
            {
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"),
                    "image/jpeg",
                    contentLength: _settings.MaxBytes + 1);

                var result = await controller.Put(NoteId, "big.jpg", CancellationToken.None);

                var obj = Assert.IsType<ObjectResult>(result);
                Assert.Equal(StatusCodes.Status413PayloadTooLarge, obj.StatusCode);
                _store.Verify(s => s.PutBytesAsync(
                    It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Fact]
            public async Task Body_streaming_past_cap_returns_413_even_without_Content_Length()
            {
                // Chunked clients omit Content-Length; the read-loop has
                // to catch oversized payloads on its own.
                SetupOwner();
                SetupNoteOwned();
                var oversized = new byte[_settings.MaxBytes + 100];
                var controller = BuildController(oversized, "image/jpeg");
                // Deliberately leave Content-Length unset on the request.
                controller.ControllerContext.HttpContext.Request.ContentLength = null;

                var result = await controller.Put(NoteId, "big.jpg", CancellationToken.None);

                var obj = Assert.IsType<ObjectResult>(result);
                Assert.Equal(StatusCodes.Status413PayloadTooLarge, obj.StatusCode);
            }

            [Fact]
            public async Task Empty_body_returns_400()
            {
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController(Array.Empty<byte>(), "image/jpeg");

                var result = await controller.Put(NoteId, "x.jpg", CancellationToken.None);

                var bad = Assert.IsType<BadRequestObjectResult>(result);
                Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
            }

            [Fact]
            public async Task Filename_with_parent_segment_returns_400()
            {
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"), "image/jpeg");

                var result = await controller.Put(NoteId, "..", CancellationToken.None);

                Assert.IsType<BadRequestObjectResult>(result);
            }

            [Fact]
            public async Task Unauthenticated_returns_401()
            {
                SetupUnauthenticated();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"), "image/jpeg");

                var result = await controller.Put(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<UnauthorizedResult>(result);
            }

            [Fact]
            public async Task Note_not_found_returns_404()
            {
                SetupOwner();
                SetupNoteNotFound();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"), "image/jpeg");

                var result = await controller.Put(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
            }

            [Fact]
            public async Task Note_owned_by_different_author_returns_404_not_403()
            {
                // Don't leak which note ids exist on other accounts.
                SetupOwner();
                SetupNoteOwnedByOther();
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"), "image/jpeg");

                var result = await controller.Put(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
            }

            [Fact]
            public async Task Store_throwing_unexpected_returns_500()
            {
                SetupOwner();
                SetupNoteOwned();
                _store.Setup(s => s.PutBytesAsync(
                    It.IsAny<int>(), It.IsAny<string>(),
                    It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new IOException("disk full"));
                var controller = BuildController(
                    Encoding.UTF8.GetBytes("x"), "image/jpeg");

                var result = await controller.Put(NoteId, "x.jpg", CancellationToken.None);

                var obj = Assert.IsType<ObjectResult>(result);
                Assert.Equal(StatusCodes.Status500InternalServerError, obj.StatusCode);
            }
        }

        // ------------------------------------------------------------
        // GET
        // ------------------------------------------------------------

        public class Get : NoteVaultControllerTests
        {
            [Fact]
            public async Task Returns_200_with_bytes_when_file_exists()
            {
                SetupOwner();
                SetupNoteOwned();
                var bytes = Encoding.UTF8.GetBytes("png-bytes");
                _store.Setup(s => s.GetBytesAsync(
                    OwnerId, "attachments/note-42/x.png", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(bytes);
                var controller = BuildController();

                var result = await controller.Get(NoteId, "x.png", CancellationToken.None);

                var file = Assert.IsType<FileContentResult>(result);
                Assert.Equal(bytes, file.FileContents);
                Assert.Equal("image/png", file.ContentType);
            }

            [Fact]
            public async Task File_missing_returns_404()
            {
                SetupOwner();
                SetupNoteOwned();
                _store.Setup(s => s.GetBytesAsync(
                    OwnerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((byte[]?)null);
                var controller = BuildController();

                var result = await controller.Get(NoteId, "missing.jpg", CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
            }

            [Fact]
            public async Task Note_owned_by_other_returns_404()
            {
                SetupOwner();
                SetupNoteOwnedByOther();
                var controller = BuildController();

                var result = await controller.Get(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
            }

            [Fact]
            public async Task Falls_back_to_octet_stream_for_unknown_extension()
            {
                SetupOwner();
                SetupNoteOwned();
                _store.Setup(s => s.GetBytesAsync(
                    OwnerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new byte[] { 0xff });
                var controller = BuildController();

                var result = await controller.Get(NoteId, "weird.xyz", CancellationToken.None);

                var file = Assert.IsType<FileContentResult>(result);
                Assert.Equal("application/octet-stream", file.ContentType);
            }
        }

        // ------------------------------------------------------------
        // HEAD
        // ------------------------------------------------------------

        public class Head : NoteVaultControllerTests
        {
            [Fact]
            public async Task Exists_returns_200()
            {
                SetupOwner();
                SetupNoteOwned();
                _store.Setup(s => s.ExistsAsync(
                    OwnerId, "attachments/note-42/x.jpg", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
                var controller = BuildController();

                var result = await controller.Head(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<OkResult>(result);
            }

            [Fact]
            public async Task Missing_returns_404()
            {
                SetupOwner();
                SetupNoteOwned();
                _store.Setup(s => s.ExistsAsync(
                    OwnerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                var controller = BuildController();

                var result = await controller.Head(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
            }

            [Fact]
            public async Task Unauthenticated_returns_401_not_200()
            {
                SetupUnauthenticated();
                var controller = BuildController();

                var result = await controller.Head(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<UnauthorizedResult>(result);
            }
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------

        public class Delete : NoteVaultControllerTests
        {
            [Fact]
            public async Task Returns_204_and_calls_store_delete_on_happy_path()
            {
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController();

                var result = await controller.Delete(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<NoContentResult>(result);
                _store.Verify(s => s.DeleteAsync(
                    OwnerId, "attachments/note-42/x.jpg", It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            [Fact]
            public async Task Returns_204_idempotently_when_file_was_already_absent()
            {
                // Store.DeleteAsync is itself idempotent (per its contract);
                // controller just forwards. Returns 204 either way.
                SetupOwner();
                SetupNoteOwned();
                _store.Setup(s => s.DeleteAsync(
                    OwnerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                var controller = BuildController();

                var result = await controller.Delete(NoteId, "gone.jpg", CancellationToken.None);

                Assert.IsType<NoContentResult>(result);
            }

            [Fact]
            public async Task Invalid_filename_returns_400()
            {
                SetupOwner();
                SetupNoteOwned();
                var controller = BuildController();

                var result = await controller.Delete(NoteId, "..", CancellationToken.None);

                Assert.IsType<BadRequestObjectResult>(result);
            }

            [Fact]
            public async Task Cross_author_returns_404_not_204()
            {
                // Sanity: cross-author callers must NOT see 204 (which
                // would imply "I deleted something for you"). 404 only.
                SetupOwner();
                SetupNoteOwnedByOther();
                var controller = BuildController();

                var result = await controller.Delete(NoteId, "x.jpg", CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
                _store.Verify(s => s.DeleteAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }
        }

        // ------------------------------------------------------------
        // LIST
        // ------------------------------------------------------------

        public class List : NoteVaultControllerTests
        {
            [Fact]
            public async Task Returns_200_with_entries()
            {
                SetupOwner();
                SetupNoteOwned();
                var entries = new[]
                {
                    new VaultEntry { RelativePath = "attachments/note-42/a.jpg", ByteSize = 10 },
                    new VaultEntry { RelativePath = "attachments/note-42/b.png", ByteSize = 20 },
                };
                _store.Setup(s => s.ListAsync(
                    OwnerId, "attachments/note-42", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(entries);
                var controller = BuildController();

                var result = await controller.List(NoteId, CancellationToken.None);

                var ok = Assert.IsType<OkObjectResult>(result);
                var listed = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<VaultEntry>>(
                    ok.Value);
                Assert.Equal(2, listed.Count());
            }

            [Fact]
            public async Task Note_not_found_returns_404()
            {
                SetupOwner();
                SetupNoteNotFound();
                var controller = BuildController();

                var result = await controller.List(NoteId, CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
                _store.Verify(s => s.ListAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Fact]
            public async Task Cross_author_returns_404()
            {
                SetupOwner();
                SetupNoteOwnedByOther();
                var controller = BuildController();

                var result = await controller.List(NoteId, CancellationToken.None);

                Assert.IsType<NotFoundObjectResult>(result);
            }
        }
    }
}
