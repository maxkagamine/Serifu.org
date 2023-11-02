using System.Diagnostics;

namespace Serifu.Data.Entities;

[DebuggerDisplay("Speaker = {SpeakerEnglish,nq}, Context = {Context,nq}, Text = {TextEnglish,nq}")]
public class VoiceLine
{
    public Guid Id { get; set; }

    public required Source Source { get; set; }

    public required string SpeakerEnglish { get; set; }

    public required string SpeakerJapanese { get; set; }

    public required string Context { get; set; }

    public required string TextEnglish { get; set; }

    public required string TextJapanese { get; set; }

    public string Notes { get; set; } = "";

    public string? AudioFile { get; set; }

    /// <summary>
    /// Sort order relative to the other voice lines for the same source and speaker.
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
