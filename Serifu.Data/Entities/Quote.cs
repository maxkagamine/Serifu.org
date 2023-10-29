using System.Diagnostics;

namespace Serifu.Data.Entities;

[DebuggerDisplay("{SpeakerEnglish,nq} - {Context,nq}: {QuoteEnglish,nq}")]
public class Quote
{
    public Guid Id { get; set; }

    public required Source Source { get; set; }

    public required string SpeakerEnglish { get; set; }

    public required string SpeakerJapanese { get; set; }

    public required string Context { get; set; }

    public required string QuoteEnglish { get; set; }

    public required string QuoteJapanese { get; set; }

    public string Notes { get; set; } = "";

    public string? AudioFile { get; set; }
}
