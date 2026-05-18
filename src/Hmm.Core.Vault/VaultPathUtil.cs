namespace Hmm.Core.Vault;

/// <summary>
/// Vault relative-path utility. Pure (no I/O, no clock, no random).
/// </summary>
/// <remarks>
/// The single source of truth lives in
/// <c>docs/attachments-path-spec.md</c>. Any change here must be
/// mirrored on the Flutter side in
/// <c>lib/core/data/vault/vault_path.dart</c> — otherwise client
/// and server will disagree about which paths are valid and the
/// vault will silently lose files.
/// </remarks>
public static class VaultPathUtil
{
    private const int MaxSegmentLength = 255;
    private const int MaxPathLength = 1024;

    /// <summary>
    /// Reserved Windows device names. The vault must stay syncable
    /// to NTFS via OneDrive on a Windows host, so refuse these as
    /// whole segments (case-insensitive).
    /// </summary>
    private static readonly HashSet<string> WindowsReservedNames = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "con", "prn", "aux", "nul",
        "com1", "com2", "com3", "com4", "com5",
        "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5",
        "lpt6", "lpt7", "lpt8", "lpt9",
    };

    private static bool IsAllowedChar(char c) =>
        (c >= 'A' && c <= 'Z') ||
        (c >= 'a' && c <= 'z') ||
        (c >= '0' && c <= '9') ||
        c == '-' || c == '_' || c == '.';

    private static void ValidateSegment(string segment)
    {
        if (segment.Length == 0)
            throw new ArgumentException("empty segment", nameof(segment));

        if (segment.Length > MaxSegmentLength)
            throw new ArgumentException(
                $"segment exceeds max length ({MaxSegmentLength})",
                nameof(segment));

        if (segment == "." || segment == "..")
            throw new ArgumentException(
                $"segment \"{segment}\" not allowed (dot/parent)",
                nameof(segment));

        if (WindowsReservedNames.Contains(segment))
            throw new ArgumentException(
                "segment is a reserved Windows device name",
                nameof(segment));

        // Windows silently strips a trailing dot, which would
        // corrupt vault refs. Reject explicitly.
        if (segment.EndsWith('.'))
            throw new ArgumentException(
                "segment must not end with \".\"",
                nameof(segment));

        foreach (var c in segment)
        {
            if (!IsAllowedChar(c))
                throw new ArgumentException(
                    $"disallowed character (0x{(int)c:X})",
                    nameof(segment));
        }
    }

    /// <summary>
    /// Join <paramref name="segments"/> into a POSIX relative path
    /// after validating each segment in isolation. A segment must
    /// not itself contain a separator — passing
    /// <c>["a", "b/c"]</c> is a bug, not a convenience.
    /// </summary>
    /// <exception cref="ArgumentException">Any rule violation.</exception>
    public static string Join(IEnumerable<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        var list = segments.ToList();
        if (list.Count == 0)
            throw new ArgumentException(
                "at least one segment required",
                nameof(segments));

        foreach (var s in list)
        {
            if (s.Contains('/') || s.Contains('\\'))
                throw new ArgumentException(
                    "segment must not contain a separator",
                    nameof(segments));
            ValidateSegment(s);
        }

        var joined = string.Join('/', list);
        if (joined.Length > MaxPathLength)
            throw new ArgumentException(
                $"joined path exceeds max length ({MaxPathLength})",
                nameof(segments));

        return joined;
    }

    /// <summary>
    /// Validate a full vault relative path. Returns the input
    /// unchanged on success (the path is its own canonical form).
    /// </summary>
    /// <exception cref="ArgumentException">Any rule violation.</exception>
    public static string Validate(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (path.Length == 0)
            throw new ArgumentException("empty path", nameof(path));

        if (path.Length > MaxPathLength)
            throw new ArgumentException(
                $"exceeds max path length ({MaxPathLength})",
                nameof(path));

        if (path.Contains('\\'))
            throw new ArgumentException(
                "backslash not allowed",
                nameof(path));

        if (path.StartsWith('/'))
            throw new ArgumentException(
                "leading slash not allowed",
                nameof(path));

        // Trailing slash, doubled slashes, and empty segments are
        // all caught by ValidateSegment's empty-segment check after
        // splitting.
        var segments = path.Split('/');
        foreach (var s in segments)
            ValidateSegment(s);

        return path;
    }
}
