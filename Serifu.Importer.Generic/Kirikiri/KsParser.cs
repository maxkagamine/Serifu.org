
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serilog;
using System.Runtime.InteropServices;
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
    private partial Regex GSenjouNmTagRegex { get; }

    [GeneratedRegex(@"(?<!\[)\[[^\]]*\]")]
    private partial Regex TagRegex { get; }

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

        using StreamReader reader = File.OpenText(path);
        string filename = Path.GetFileName(path);
        bool hasAudioForLanguage = options.AudioDirectories.ContainsKey(language);
        List<ParsedQuoteTranslation> result = [];

        // Labels are *supposed* to be unique, but there's a duplicated label in one of G-senjou's scenarios, so we'll
        // have to remember and increment the last index for the label instead of using the split text's indexes.
        Dictionary<string, int> indexesPerLabel = [];

        // We read the text in between labels first, then split that text using the configured separator tags (depends
        // on the game), since sometimes a label has multiple lines of dialogue under it. Ultimately it's the tags that
        // decide when a line ends, not the labels, but it's helpful to use them as separators anyway.
        foreach (var (label, labelText) in ReadLabels(reader))
        {
            ref int i = ref CollectionsMarshal.GetValueRefOrAddDefault(indexesPerLabel, label, out _);

            foreach (string dialogueLine in lineSeparatorTagsRegex.Split(labelText))
            {
                if (string.IsNullOrWhiteSpace(dialogueLine))
                {
                    continue;
                }

                var (speakerName, audioFile) = ExtractSpeakerNameAndAudioFile(dialogueLine);

                if (options.VoicedLinesOnly && audioFile is null)
                {
                    continue;
                }

                string key = $"{filename}{label}[{i++}]";
                string text = quoteStopRegex is null ? dialogueLine : quoteStopRegex.Replace(dialogueLine, "");

                ParsedQuoteTranslation tl = new()
                {
                    Key = key,
                    Language = language,
                    Text = StripFormatting(text).Trim(),
                    AudioFilePath = hasAudioForLanguage ? audioFile : null,
                    SpeakerName = speakerName ?? "",
                };

                result.Add(tl);
            }
        }

        return result;
    }

    private IEnumerable<(string Label, string Text)> ReadLabels(StreamReader reader)
    {
        string label = "";
        StringBuilder text = new();

        bool inCodeBlock = false;

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
                // Return the previous label's text
                yield return (label, text.ToString());

                label = UnifyLabel(line.Split('|')[0]);
                text.Clear();
                continue;
            }

            // Any other lines should be text
            text.AppendLine(line);
        }

        // Return the last label's text
        yield return (label, text.ToString());
    }

    /// <summary>
    /// Unifies differences in label format between the English & Japanese versions so that they match up.
    /// </summary>
    private string UnifyLabel(string label) => options.Source switch
    {
        Source.GSenjouNoMaou => label.Replace("page", "p"),
        _ => label
    };

    private (string? SpeakerName, string? AudioFile) ExtractSpeakerNameAndAudioFile(string text)
    {
        switch (options.Source)
        {
            case Source.GSenjouNoMaou:
                var matches = GSenjouNmTagRegex.Matches(text);
                switch (matches.Count)
                {
                    case 0:
                        return (null, null);
                    case 1:
                        string? speakerName = EmptyStringToNull(matches[0].Groups["t"].Value);
                        string? audioFile = EmptyStringToNull(matches[0].Groups["s"].Value);

                        // For some reason, a lot of Maou's lines in the JP version have the name empty
                        if (speakerName is null && audioFile is not null && audioFile.StartsWith("mao_"))
                        {
                            speakerName = "魔王";
                        }

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
        return TagRegex.Replace(text, "");
    }
}
