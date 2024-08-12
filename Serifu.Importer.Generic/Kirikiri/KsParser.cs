
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serilog;
using System.Text;
using System.Text.RegularExpressions;

namespace Serifu.Importer.Generic.Kirikiri;

/// <summary>
/// Parser for Kirikiri (KAG) scenario files.
/// </summary>
internal partial class KsParser : IParser<KsParserOptions>
{
    private readonly KsParserOptions options;
    private readonly ILogger logger;
    private readonly Regex lineSeparatorTagsRegex;
    private readonly Regex? quoteStopRegex;

    // Refer to https://stackoverflow.com/a/1732454
    [GeneratedRegex("""
        (?<!\[)\[\s*nm\s(
            \s*t\s*=\s*("(?<t>[^"]*)"|(?<t>\S*)) |
            \s*s\s*=\s*("(?<s>[^"]*)"|(?<s>[^\s\]]*))
        )+
        """,
        RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private partial Regex GSenjouNmTagRegex();

    [GeneratedRegex(@"(?<!\[)\[[^\]]*\]")]
    private partial Regex TagRegex();

    public KsParser(IOptions<KsParserOptions> options, ILogger logger)
    {
        this.options = options.Value;
        this.logger = logger.ForContext<KsParser>();

        string[] lineSeparatorTags = options.Value.LineSeparatorTags ?? KsParserOptions.DefaultLineSeparatorTags;

        lineSeparatorTagsRegex = new Regex($@"(?<!\[)\[\s*({string.Join('|', lineSeparatorTags)})\s*\]",
            RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

        if (options.Value.QuoteStopTags.Length > 0)
        {
            quoteStopRegex = new Regex($@"(?<!\[)\[\s*({string.Join('|', options.Value.QuoteStopTags)})\s*\].*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

    public IEnumerable<ParsedQuoteTranslation> Parse(string path, Language language)
    {
        if (language is Language.Multilingual)
        {
            throw new ArgumentException($"{nameof(KsParser)} does not handle multilingual dialogue files.", nameof(language));
        }

        bool hasAudioForLanguage = options.AudioDirectories.ContainsKey(language);

        using StreamReader reader = File.OpenText(path);
        string filename = Path.GetFileName(path);

        foreach (var (key, text) in ReadDialogueLines(reader, filename))
        {
            var (speakerName, audioFile) = ExtractSpeakerNameAndAudioFile(text);

            yield return new ParsedQuoteTranslation()
            {
                Key = key,
                Language = language,
                Text = StripFormatting(text),
                AudioFilePath = hasAudioForLanguage ? audioFile : null,
                SpeakerName = speakerName,
            };
        }
    }

    private IEnumerable<(string Key, string Text)> ReadDialogueLines(StreamReader reader, string filename)
    {
        string lastLabel = "";
        StringBuilder text = new();

        bool inCodeBlock = false;

        // We read the text in between labels first, then split that text using the configured separator tags (depends
        // on the game), since sometimes a label has multiple lines of dialogue under it. Ultimately it's the tags that
        // decide when a line ends, not the labels, but it's helpful to use them as separators anyway.
        IEnumerable<(string Key, string Text)> SplitLines()
        {
            if (text.Length == 0)
            {
                return [];
            }

            return lineSeparatorTagsRegex.Split(text.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => quoteStopRegex is null ? x : quoteStopRegex.Replace(x, ""))
                .Select((x, i) => ($"{filename}{UnifyLabel(lastLabel)}#{i}", x.Trim()));
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            // Leading tab characters are ignored
            line = line.TrimStart('\t');

            // Skip code blocks
            if (line is "@iscript" or "[iscript]")
            {
                inCodeBlock = true;
                continue;
            }
            else if (line is "@endscript" or "[endscript]")
            {
                inCodeBlock = false;
                continue;
            }

            if (inCodeBlock)
            {
                continue;
            }

            // Ignore blank lines, commands, and comments
            if (line.Length == 0 || line[0] is '@' or ';')
            {
                continue;
            }

            // Labels take the form "*labelName|Optional header"
            // The asterisk seems to be considered as part of the label name
            if (line[0] is '*')
            {
                // Return the previous label's line(s)
                foreach (var dialogueLine in SplitLines())
                {
                    yield return dialogueLine;
                }

                lastLabel = line.Split('|')[0];
                text.Clear();
                continue;
            }

            // Any other lines should be text
            // We'll handle stripping out formatting in a separate step
            text.AppendLine(line);
        }

        // Return the last label's line(s)
        foreach (var dialogueLine in SplitLines())
        {
            yield return dialogueLine;
        }
    }

    /// <summary>
    /// Unifies differences in label format between the English & Japanese versions so that they match up.
    /// </summary>
    private string UnifyLabel(string label) => options.Source switch
    {
        Source.GSenjouNoMaou => label.Replace("page", "p"),
        _ => label
    };

    private (string SpeakerName, string? AudioFile) ExtractSpeakerNameAndAudioFile(string text)
    {
        switch (options.Source)
        {
            case Source.GSenjouNoMaou:
                var matches = GSenjouNmTagRegex().Matches(text);
                switch (matches.Count)
                {
                    case 0:
                        return ("", "");
                    case 1:
                        string speakerName = matches[0].Groups["t"].Value;
                        string? audioFile = EmptyStringToNull(matches[0].Groups["s"].Value);
                        return (speakerName, audioFile);
                    default:
                        throw new Exception("More than one [nm] tag was found in the text; the dialogue line separator is probably wrong.");
                }
            default:
                throw new NotImplementedException($"{nameof(ExtractSpeakerNameAndAudioFile)} needs to be implemented for {options.Source}.");
        }
    }

    private static string? EmptyStringToNull(string str) => str == "" ? null : str;

    private string StripFormatting(string text)
    {
        return TagRegex().Replace(text, "");
    }
}
