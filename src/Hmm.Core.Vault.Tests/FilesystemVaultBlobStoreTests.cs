namespace Hmm.Core.Vault.Tests;

/// <summary>
/// Mirrors the Dart-side <c>local_vault_store_test.dart</c> so both
/// sides exercise the same I/O contract.
/// </summary>
public class FilesystemVaultBlobStoreTests : IDisposable
{
    private const int Author = 7;

    private readonly string _tmpRoot;
    private readonly FilesystemVaultBlobStore _store;

    public FilesystemVaultBlobStoreTests()
    {
        _tmpRoot = Path.Combine(
            Path.GetTempPath(),
            "hmm_vault_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpRoot);
        _store = new FilesystemVaultBlobStore(_tmpRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tmpRoot))
        {
            try
            {
                Directory.Delete(_tmpRoot, recursive: true);
            }
            catch
            {
                // Best-effort cleanup in case the OS still holds a
                // handle from a test that crashed.
            }
        }
    }

    private static byte[] Bytes(string s) =>
        System.Text.Encoding.UTF8.GetBytes(s);

    public class PutGetExists : FilesystemVaultBlobStoreTests
    {
        [Fact]
        public async Task Round_trips_bytes_at_a_vault_path()
        {
            var payload = Bytes("hello-vault");
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", payload);

            Assert.True(await _store.ExistsAsync(
                Author, "attachments/note-1/a.jpg"));
            var read = await _store.GetBytesAsync(
                Author, "attachments/note-1/a.jpg");
            Assert.Equal(payload, read);
        }

        [Fact]
        public async Task Creates_parent_dir_tree_on_first_write()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-42/x.png", Bytes("x"));
            var dir = Path.Combine(
                _tmpRoot,
                Author.ToString(),
                "attachments",
                "note-42");
            Assert.True(Directory.Exists(dir));
        }

        [Fact]
        public async Task Overwrites_an_existing_file()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-1/x.jpg", Bytes("old"));
            await _store.PutBytesAsync(
                Author, "attachments/note-1/x.jpg", Bytes("new"));
            var read = await _store.GetBytesAsync(
                Author, "attachments/note-1/x.jpg");
            Assert.Equal(Bytes("new"), read);
        }

        [Fact]
        public async Task Leaves_no_tmp_residue_after_successful_write()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", Bytes("payload"));
            var dir = Path.Combine(
                _tmpRoot, Author.ToString(), "attachments", "note-1");
            var entries = Directory.GetFileSystemEntries(dir);
            Assert.Single(entries);
            Assert.EndsWith(".jpg", entries[0]);
        }

        [Fact]
        public async Task GetBytes_returns_null_for_a_missing_file()
        {
            var bytes = await _store.GetBytesAsync(
                Author, "attachments/note-1/missing.jpg");
            Assert.Null(bytes);
        }

        [Fact]
        public async Task Exists_returns_false_for_a_missing_file()
        {
            Assert.False(await _store.ExistsAsync(
                Author, "attachments/note-1/missing.jpg"));
        }
    }

    public class Delete : FilesystemVaultBlobStoreTests
    {
        [Fact]
        public async Task Removes_an_existing_file()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", Bytes("x"));
            await _store.DeleteAsync(
                Author, "attachments/note-1/a.jpg");
            Assert.False(await _store.ExistsAsync(
                Author, "attachments/note-1/a.jpg"));
        }

        [Fact]
        public async Task Succeeds_silently_for_a_missing_file()
        {
            // No exception expected.
            await _store.DeleteAsync(
                Author, "attachments/note-1/missing.jpg");
        }
    }

    public class List : FilesystemVaultBlobStoreTests
    {
        [Fact]
        public async Task Empty_prefix_returns_every_file_under_root()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", Bytes("a"));
            await _store.PutBytesAsync(
                Author, "attachments/note-1/b.jpg", Bytes("bb"));
            await _store.PutBytesAsync(
                Author, "attachments/note-2/c.jpg", Bytes("ccc"));

            var all = await _store.ListAsync(Author, string.Empty);
            Assert.Equal(
                new[]
                {
                    "attachments/note-1/a.jpg",
                    "attachments/note-1/b.jpg",
                    "attachments/note-2/c.jpg",
                },
                all.Select(e => e.RelativePath));
            Assert.Equal(
                2,
                all.First(e => e.RelativePath.EndsWith("b.jpg")).ByteSize);
        }

        [Fact]
        public async Task Folder_prefix_returns_only_entries_beneath_it()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", Bytes("a"));
            await _store.PutBytesAsync(
                Author, "attachments/note-2/b.jpg", Bytes("b"));

            var note1 = await _store.ListAsync(
                Author, "attachments/note-1");
            Assert.Single(note1);
            Assert.Equal("attachments/note-1/a.jpg", note1[0].RelativePath);
        }

        [Fact]
        public async Task File_prefix_returns_that_single_entry()
        {
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", Bytes("hello"));
            var single = await _store.ListAsync(
                Author, "attachments/note-1/a.jpg");
            Assert.Single(single);
            Assert.Equal("attachments/note-1/a.jpg", single[0].RelativePath);
            Assert.Equal(5, single[0].ByteSize);
        }

        [Fact]
        public async Task Non_existent_prefix_returns_empty()
        {
            var empty = await _store.ListAsync(
                Author, "attachments/note-99");
            Assert.Empty(empty);
        }

        [Fact]
        public async Task Skips_half_written_tmp_files()
        {
            var dir = Path.Combine(
                _tmpRoot, Author.ToString(), "attachments", "note-1");
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(
                Path.Combine(dir, "a.jpg.tmp"), Bytes("partial"));
            await _store.PutBytesAsync(
                Author, "attachments/note-1/a.jpg", Bytes("full"));

            var entries = await _store.ListAsync(
                Author, "attachments/note-1");
            Assert.Single(entries);
            Assert.Equal("attachments/note-1/a.jpg", entries[0].RelativePath);
        }
    }

    public class InputValidation : FilesystemVaultBlobStoreTests
    {
        [Fact]
        public async Task PutBytes_rejects_invalid_paths()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _store.PutBytesAsync(
                    Author, "../escape.jpg", Bytes("x")));
            await Assert.ThrowsAsync<ArgumentException>(
                () => _store.PutBytesAsync(
                    Author, "/leading-slash.jpg", Bytes("x")));
        }

        [Fact]
        public async Task GetBytes_rejects_invalid_paths()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _store.GetBytesAsync(
                    Author, "foo/../bar.jpg"));
        }

        [Fact]
        public async Task List_rejects_invalid_non_empty_prefixes()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _store.ListAsync(Author, "../escape"));
        }

        [Fact]
        public async Task List_accepts_the_empty_prefix()
        {
            var entries = await _store.ListAsync(Author, string.Empty);
            Assert.Empty(entries);
        }
    }

    public class PerAuthorIsolation : FilesystemVaultBlobStoreTests
    {
        [Fact]
        public async Task Author_A_does_not_see_author_B_files()
        {
            await _store.PutBytesAsync(
                authorId: 1,
                "attachments/note-1/a.jpg",
                Bytes("for author 1"));

            var authorB = await _store.ListAsync(authorId: 2, string.Empty);
            Assert.Empty(authorB);

            Assert.False(await _store.ExistsAsync(
                authorId: 2, "attachments/note-1/a.jpg"));
            Assert.Null(await _store.GetBytesAsync(
                authorId: 2, "attachments/note-1/a.jpg"));
        }

        [Fact]
        public async Task Same_relative_path_can_exist_under_two_authors_independently()
        {
            await _store.PutBytesAsync(
                authorId: 1, "attachments/note-1/a.jpg", Bytes("alice"));
            await _store.PutBytesAsync(
                authorId: 2, "attachments/note-1/a.jpg", Bytes("bob"));

            Assert.Equal(
                Bytes("alice"),
                await _store.GetBytesAsync(1, "attachments/note-1/a.jpg"));
            Assert.Equal(
                Bytes("bob"),
                await _store.GetBytesAsync(2, "attachments/note-1/a.jpg"));
        }
    }
}
