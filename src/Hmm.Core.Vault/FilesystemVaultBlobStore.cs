using Microsoft.Extensions.Options;

namespace Hmm.Core.Vault;

/// <summary>
/// On-disk implementation of <see cref="IVaultBlobStore"/>. Reads
/// and writes under
/// <c>{AttachmentSettings.RootDir}/{authorId}/{relativePath}</c>.
/// </summary>
/// <remarks>
/// Mirrors the Flutter-side <c>LocalVaultStore</c> behaviour:
/// atomic put-then-rename writes, idempotent deletes, defensive
/// prefix listing that skips half-written <c>.tmp</c> files and
/// silently ignores anything that doesn't validate as a vault
/// relative path.
/// </remarks>
public sealed class FilesystemVaultBlobStore : IVaultBlobStore
{
    private readonly string _rootDir;

    public FilesystemVaultBlobStore(IOptions<AttachmentSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootDir = options.Value.RootDir;
    }

    /// <summary>
    /// Test-friendly constructor that takes the root directly.
    /// </summary>
    internal FilesystemVaultBlobStore(string rootDir)
    {
        _rootDir = rootDir;
    }

    public async Task PutBytesAsync(
        int authorId,
        string relativePath,
        ReadOnlyMemory<byte> bytes,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var target = ResolveFile(authorId, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        // Atomic-replace: write to a sibling .tmp file then rename
        // into place. Keeps any backup / sync tool from observing
        // a half-written file (same trick the Dart impl uses).
        var tmp = target + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(tmp, bytes.ToArray(), cancellationToken)
                .ConfigureAwait(false);
            File.Move(tmp, target, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmp))
            {
                try
                {
                    File.Delete(tmp);
                }
                catch
                {
                    // Swallow — original error is more important.
                }
            }
            throw;
        }
    }

    public async Task<byte[]?> GetBytesAsync(
        int authorId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var file = ResolveFile(authorId, relativePath);
        if (!File.Exists(file))
            return null;
        return await File.ReadAllBytesAsync(file, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(
        int authorId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var file = ResolveFile(authorId, relativePath);
        return Task.FromResult(File.Exists(file));
    }

    public Task DeleteAsync(
        int authorId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var file = ResolveFile(authorId, relativePath);
        if (File.Exists(file))
            File.Delete(file);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VaultEntry>> ListAsync(
        int authorId,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var authorRoot = Path.Combine(_rootDir, authorId.ToString());
        string scanRoot;
        string relPrefix;

        if (prefix.Length == 0)
        {
            scanRoot = authorRoot;
            relPrefix = string.Empty;
        }
        else
        {
            VaultPathUtil.Validate(prefix);
            var segments = prefix.Split('/');
            var candidate = Path.Combine(
                new[] { authorRoot }.Concat(segments).ToArray());
            if (Directory.Exists(candidate))
            {
                scanRoot = candidate;
                relPrefix = prefix;
            }
            else if (File.Exists(candidate))
            {
                // Prefix addresses a single file rather than a folder.
                var info = new FileInfo(candidate);
                return Task.FromResult<IReadOnlyList<VaultEntry>>(new[]
                {
                    new VaultEntry
                    {
                        RelativePath = prefix,
                        ByteSize = info.Length,
                    },
                });
            }
            else
            {
                return Task.FromResult<IReadOnlyList<VaultEntry>>(
                    Array.Empty<VaultEntry>());
            }
        }

        if (!Directory.Exists(scanRoot))
            return Task.FromResult<IReadOnlyList<VaultEntry>>(
                Array.Empty<VaultEntry>());

        var results = new List<VaultEntry>();
        foreach (var file in Directory.EnumerateFiles(
                     scanRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (file.EndsWith(".tmp", StringComparison.Ordinal))
                continue;

            var raw = file.Substring(authorRoot.Length);
            var trimmed = raw.StartsWith(Path.DirectorySeparatorChar)
                ? raw[1..]
                : raw;
            var posix = trimmed.Replace(Path.DirectorySeparatorChar, '/');

            // Defensive: a file that doesn't validate as a vault
            // relative path was put there by something other than
            // this store — skip rather than surface it as a bogus
            // entry.
            try
            {
                VaultPathUtil.Validate(posix);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (relPrefix.Length > 0 &&
                !(posix == relPrefix ||
                  posix.StartsWith(relPrefix + "/", StringComparison.Ordinal)))
            {
                continue;
            }

            var info = new FileInfo(file);
            results.Add(new VaultEntry
            {
                RelativePath = posix,
                ByteSize = info.Length,
            });
        }

        // Stable ordering — callers can rely on it for snapshot
        // tests + incremental sync diffs.
        results.Sort((a, b) =>
            string.CompareOrdinal(a.RelativePath, b.RelativePath));
        return Task.FromResult<IReadOnlyList<VaultEntry>>(results);
    }

    private string ResolveFile(int authorId, string relativePath)
    {
        VaultPathUtil.Validate(relativePath);
        var segments = relativePath.Split('/');
        return Path.Combine(
            new[] { _rootDir, authorId.ToString() }
                .Concat(segments)
                .ToArray());
    }
}
