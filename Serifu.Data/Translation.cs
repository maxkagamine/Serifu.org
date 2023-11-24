using System.Diagnostics;

namespace Serifu.Data;

[DebuggerDisplay("Language = {Language}, Text = {Text}")]
public class Translation
{
    public required string Language { get; set; }

    public required string SpeakerName { get; set; }

    public required string Context { get; set; }

    public required string Text { get; set; }

    public string Notes { get; set; } = "";

    public string? AudioFile { get; set; }

    public string? OriginalAudioFile { get; set; }

    public DateTime? DateAudioFileImported { get; set; }
}
