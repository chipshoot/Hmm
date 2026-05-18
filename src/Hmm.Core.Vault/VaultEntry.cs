namespace Hmm.Core.Vault;

/// <summary>
/// One entry returned by <see cref="IVaultBlobStore.ListAsync"/>.
/// Mirrors the Dart-side <c>VaultEntry</c> shape so list semantics
/// are identical on both sides of the wire.
/// </summary>
public sealed record VaultEntry
{
    /// <summary>
    /// Vault relative path, POSIX form. Always valid per
    /// <see cref="VaultPathUtil.Validate(string)"/>.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>File size in bytes.</summary>
    public required long ByteSize { get; init; }
}
