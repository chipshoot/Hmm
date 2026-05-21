namespace Hmm.Core.Vault.Tests;

/// <summary>
/// Mirrors the verbatim test-vector tables in
/// <c>docs/attachments-path-spec.md</c> — the same vectors run on the
/// Dart side in <c>test/core/data/vault/vault_path_test.dart</c>.
/// Adding a vector here without updating the spec is a process error;
/// updating the spec without updating both sides' tests is worse.
/// </summary>
public class VaultPathUtilTests
{
    public class ValidatesGoodPaths
    {
        public static IEnumerable<object[]> ValidInputs() => new[]
        {
            new object[] { "attachments/note-1/a.jpg" },
            new object[] { "attachments/note-42/9c8a3f12-7d6e-4a8b-90d1-2b4e5a6f7c01.jpg" },
            new object[] { "a" },
            new object[] { "a/b/c" },
            new object[] { "note-9999/photo-01.heic" },
            new object[] { "_.png" },
            new object[] { "-.webp" },
            new object[] { "a.b.c.jpg" },
        };

        [Theory]
        [MemberData(nameof(ValidInputs))]
        public void Passes_and_returns_input_unchanged(string input)
        {
            Assert.Equal(input, VaultPathUtil.Validate(input));
        }
    }

    public class RejectsBadPaths
    {
        public static IEnumerable<object[]> InvalidInputs() => new[]
        {
            new object[] { "", "empty path" },
            new object[] { "/foo", "leading slash" },
            new object[] { "foo/", "trailing empty segment" },
            new object[] { "foo//bar", "empty segment" },
            new object[] { "..", "parent segment" },
            new object[] { "foo/../bar", "parent segment" },
            new object[] { "./foo", "dot segment" },
            new object[] { "foo/./bar", "dot segment" },
            new object[] { @"foo\bar", "backslash" },
            new object[] { "foo bar", "space" },
            new object[] { " foo", "leading space" },
            new object[] { "foo ", "trailing space" },
            new object[] { "foo\u0001bar", "control char" },
            new object[] { "héllo", "non-ASCII" },
            new object[] { "foo.", "trailing dot on segment" },
            new object[] { "CON", "reserved Windows name" },
            new object[] { "attachments/CON/x.jpg", "reserved Windows name as a segment" },
            new object[] { "prn", "reserved Windows name (case-insensitive)" },
        };

        [Theory]
        [MemberData(nameof(InvalidInputs))]
        public void Throws_ArgumentException(string input, string reason)
        {
            // The `reason` parameter shows up in xUnit's DisplayName
            // so a failure surfaces "expected reject for control char"
            // rather than just "expected reject for <bytes>".
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Validate(input));
            _ = reason;
        }

        [Fact]
        public void Rejects_segment_over_255_chars()
        {
            var longSegment = new string('a', 256);
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Validate(longSegment));
        }

        [Fact]
        public void Accepts_segment_exactly_255_chars()
        {
            var justFitting = new string('a', 255);
            Assert.Equal(justFitting, VaultPathUtil.Validate(justFitting));
        }

        [Fact]
        public void Rejects_path_over_1024_chars()
        {
            // 11 segments of 100 chars + 10 separators = 1110.
            var seg = new string('a', 100);
            var tooLong = string.Join('/', Enumerable.Repeat(seg, 11));
            Assert.True(tooLong.Length > 1024);
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Validate(tooLong));
        }

        [Fact]
        public void Accepts_path_exactly_1024_chars()
        {
            // 9 segments of 100 + 8 '/' = 908; + '/' + 115 final = 1024.
            var parts = Enumerable.Repeat(new string('a', 100), 9).ToList();
            var assembled = string.Join('/', parts) + '/' + new string('a', 115);
            Assert.Equal(1024, assembled.Length);
            Assert.Equal(assembled, VaultPathUtil.Validate(assembled));
        }
    }

    public class JoinBehavior
    {
        [Fact]
        public void Joins_valid_segments_with_slash()
        {
            Assert.Equal(
                "attachments/note-5/x.jpg",
                VaultPathUtil.Join(new[] { "attachments", "note-5", "x.jpg" }));
        }

        [Fact]
        public void Throws_when_a_segment_contains_a_forward_slash()
        {
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Join(new[] { "a", "b/c" }));
        }

        [Fact]
        public void Throws_when_a_segment_contains_a_backslash()
        {
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Join(new[] { "a", @"b\c" }));
        }

        [Fact]
        public void Throws_on_empty_segment()
        {
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Join(new[] { "a", "" }));
        }

        [Fact]
        public void Throws_on_empty_input()
        {
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Join(Array.Empty<string>()));
        }

        [Fact]
        public void Throws_on_parent_segment()
        {
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Join(new[] { "a", "..", "b" }));
        }

        [Fact]
        public void Throws_on_reserved_Windows_name_segment()
        {
            Assert.Throws<ArgumentException>(
                () => VaultPathUtil.Join(new[] { "attachments", "CON", "x.jpg" }));
        }
    }
}
