using System.Diagnostics;

namespace Serifu.Data;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class Quote
{
    public required long Id { get; set; }

    public required Source Source { get; set; }

    public TranslationCollection Translations { get; set; } = [];

    public DateTime DateImported { get; set; } = DateTime.Now;

    private string GetDebuggerDisplay()
    {
        if (!Translations.TryGetValue("en", out var tl))
        {
            tl = Translations.FirstOrDefault();
        }

        return $"Speaker = \"{tl?.SpeakerName}\", Context = \"{tl?.Context}\", Text = \"{tl?.Text}\"";
    }
}
