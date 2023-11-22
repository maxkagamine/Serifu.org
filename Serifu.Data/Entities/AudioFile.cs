using System.Diagnostics;

namespace Serifu.Data.Entities;

[DebuggerDisplay("{OriginalName,nq}")]
public record AudioFile(
    string Hash,
    string Extension,
    string? OriginalName,
    DateTime? OriginalLastModified
);
