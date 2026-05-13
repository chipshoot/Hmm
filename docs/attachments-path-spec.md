# Attachments — Vault Relative Path Spec

Owner: shared between Flutter `hmm_console` and .NET `Hmm.ServiceApi`.
Status: **Authoritative spec — both sides implement this byte-for-byte.**
Companion: [`attachments-design.md`](./attachments-design.md).

A *vault relative path* is a string that uniquely names one file
inside the vault, in a way every supported OS (macOS, Windows, Linux,
iOS, Android) can store and serve. Both the Flutter client and the
.NET server canonicalise paths through a pure function implementing
the rules below; mismatched implementations cause silent data loss.

## Grammar

```
relativePath = segment ( "/" segment )*
segment      = 1*allowedChar         (1..255 chars)
allowedChar  = ALPHA / DIGIT / "-" / "_" / "."
```

- Allowed segment characters: ASCII `A-Z`, `a-z`, `0-9`, `-`, `_`, `.`
  — nothing else.
- Total path length: **≤ 1024 characters**.

## Hard rules (validation MUST fail if any is violated)

1. **POSIX separator only** — `/`. A `\` anywhere in the input is
   rejected; don't silently convert. (Windows host code must call
   the utility with `/`, not `\`.)
2. **Relative only** — must NOT start with `/`. The vault root is
   prepended by the store implementation, never by the caller.
3. **No empty segments** — `//` is invalid.
4. **No `.` segment** — redundant; reject.
5. **No `..` segment** — directory escape; reject. This is the
   single most important check.
6. **No leading or trailing whitespace** on the path or any segment;
   no embedded whitespace, control chars, or non-ASCII bytes.
7. **No segment is exactly `.` or `..`** (covered by #4/#5, called
   out explicitly because some validators check character-class only
   and miss segments-as-keywords).
8. **No reserved Windows device names** as a *whole segment*
   (case-insensitive): `CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9`,
   `LPT1`-`LPT9`. Reject so a vault synced to NTFS doesn't blow up.
9. **No trailing dot or trailing space on a segment** — both crash
   on Windows. (Following from #1 a trailing space is already out,
   but a trailing dot like `foo.` slips through the character class;
   reject it explicitly.)
10. **Case-sensitive comparison** — paths are equal iff their bytes
    are equal. Two paths differing only in case refer to different
    files even on case-insensitive filesystems; the canonicaliser
    does not lowercase. (Practical implication: don't generate paths
    that collide only by case on the same FS.)

## Pure-function API (per side)

```dart
// Dart (hmm_console)
String vaultRelativePathJoin(Iterable<String> segments);
//   - throws ArgumentError if any segment violates the rules
//   - returns the joined POSIX path

String vaultRelativePathValidate(String path);
//   - throws ArgumentError if the path violates the rules
//   - returns the path unchanged on success (canonical form)
```

```csharp
// C# (Hmm.Core.Vault)
public static string Join(IEnumerable<string> segments);
//   throws ArgumentException on rule violation

public static string Validate(string relativePath);
//   throws ArgumentException on rule violation; returns input on ok
```

Both implementations are **pure** (no I/O, no clock, no random).
They are the only acceptable source of vault paths anywhere in the
codebase — direct string concatenation is not allowed.

## Canonical layout (built using this utility)

```
attachments/note-{noteId}/{uuid}.{ext}
```

- `attachments` — literal, lowercase.
- `note-{noteId}` — `noteId` is the integer note primary key
  rendered in decimal, no leading zeros (e.g. `note-5`, `note-1234`).
- `{uuid}` — a UUID v4 in lowercase canonical form, hyphens kept
  (e.g. `9c8a3f12-7d6e-4a8b-90d1-2b4e5a6f7c01`).
- `{ext}` — file extension matching the resolved MIME type:
  - `image/jpeg` → `jpg`
  - `image/png`  → `png`
  - `image/heic` → `heic`
  - `image/webp` → `webp`
  - (Schemas may extend this set; the spec mirrors the storage
    policy in `attachments-design.md`.)

Example valid path:

```
attachments/note-42/9c8a3f12-7d6e-4a8b-90d1-2b4e5a6f7c01.jpg
```

## Reference test vectors

Both implementations MUST pass these tests verbatim.

### Valid (validate returns the input unchanged)

| Input |
| --- |
| `attachments/note-1/a.jpg` |
| `attachments/note-42/9c8a3f12-7d6e-4a8b-90d1-2b4e5a6f7c01.jpg` |
| `a` |
| `a/b/c` |
| `note-9999/photo-01.heic` |
| `_.png` |
| `-.webp` |
| `a.b.c.jpg` |

### Invalid (validate throws)

| Input | Reason |
| --- | --- |
| `` (empty) | empty path |
| `/foo` | leading slash |
| `foo/` | trailing empty segment |
| `foo//bar` | empty segment |
| `..` | parent segment |
| `foo/../bar` | parent segment |
| `./foo` | dot segment |
| `foo/./bar` | dot segment |
| `foo\\bar` | backslash |
| `foo bar` | space |
| ` foo` | leading space |
| `foo ` | trailing space |
| `foobar` | control char |
| `héllo` | non-ASCII |
| `foo.` | trailing dot on a segment |
| `CON` | reserved Windows name |
| `attachments/CON/x.jpg` | reserved Windows name as a segment |
| `prn` | reserved Windows name (case-insensitive) |
| `<256-char segment>` | segment over 255 chars |
| `<1025-char path>` | path over 1024 chars |

### Join behavior

| `Join([...])` | Result |
| --- | --- |
| `["attachments", "note-5", "x.jpg"]` | `attachments/note-5/x.jpg` |
| `["a", "b/c"]` | **throws** — a single segment can't contain `/` |
| `["a", ""]` | throws — empty segment |
| `[]` | throws — at least one segment required |

`Join` validates each segment in isolation against rules 1–9, then
concatenates with `/`. It does **not** accept multi-segment strings
in a single argument — that's a deliberate footgun-removal.

## Why these specific rules

- **Windows compatibility (rules 8, 9)**: vaults sync via OneDrive,
  which is a regular folder on Windows hosts. NTFS will refuse to
  create a file named `CON.jpg` or `foo. ` (trailing dot/space);
  better to refuse at the boundary than to lose a file silently.
- **No `..` (rule 5)**: the vault must never escape its root. This
  is the only check that matters for security — without it, a
  client (or a corrupted JSON) could read or write anywhere the
  process can.
- **ASCII-only allowed-char set**: cross-platform filesystem behavior
  for non-ASCII filenames is a swamp (NFC vs NFD on macOS, codepage
  on Windows). UUIDs and decimal note ids never need anything
  outside the allowed set, so the cost is zero and the win is large.
- **Case-sensitive compare (rule 10)**: the vault is the source of
  truth; the FS underneath may collapse case but the index/refs
  don't. Paths in the `attachments` JSON column compare byte-equal.

## Not specified here (deliberately)

- The *vault root* (`<app docs>/vault`, `<OneDrive>/Hmm/vault`,
  `/var/lib/hmm-vault/{authorId}`). Roots are a concern of the store
  implementation, not the path utility.
- File contents, MIME types, byte size. Those belong to the storage
  policy in `attachments-design.md`.
- URL-encoding for HTTP transport. The HTTP layer percent-encodes
  the validated path as a single piece; the path utility produces
  the raw value.
