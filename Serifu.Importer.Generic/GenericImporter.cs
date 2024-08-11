using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.ML.Abstractions;
using Serilog;
using System.Text.RegularExpressions;

namespace Serifu.Importer.Generic;

internal partial class GenericImporter
{
    private const int MinimumEnglishWordCount = 2;

    private readonly IParser parser;
    private readonly IWordAligner wordAligner;
    private readonly ParserOptions options;
    private readonly ILogger logger;

    [GeneratedRegex(@"[一-龠ぁ-ゔ]")]
    private static partial Regex KanjiOrHiraganaRegex();

    private record PairedWithKeyOrIndex(int KeyOrIndex, ParsedQuoteTranslation English, ParsedQuoteTranslation Japanese);

    public GenericImporter(
        IParser parser,
        IWordAligner wordAligner,
        IOptions<ParserOptions> options,
        ILogger logger)
    {
        this.parser = parser;
        this.wordAligner = wordAligner;
        this.options = options.Value;
        this.logger = logger.ForContext<GenericImporter>();
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        using var progress = new TerminalProgressBar();
        progress.SetIndeterminate();

        logger.Information("Base directory is {BaseDirectory}", options.BaseDirectory);

        var groupedByKey = EnumerateDialogueFiles()
            .SelectMany(x =>
            {
                logger.Information("Parsing {DialogueFile} ({Language})...",
                    Path.GetRelativePath(options.BaseDirectory, x.Path), x.Language);

                cancellationToken.ThrowIfCancellationRequested();
                return parser.Parse(x.Path, x.Language);
            })
            .GroupBy(x => x.Key);

        // Filter out unusable quotes and duplicates, preferring quotes with speakers over those without
        PairedWithKeyOrIndex[] paired = ValidateAndFilter(groupedByKey)
            .GroupBy(x => (x.English.Text, x.Japanese.Text))
            .Select(g => g.FirstOrDefault(x => x.English.SpeakerName != "") ?? g.First())
            .ToArray();

        logger.Information("Found {Count} quotes ({RemovedCount} filtered out).",
            paired.Length, groupedByKey.Count() - paired.Length);

        // Import audio files and run word alignment
        List<Quote> quotes = new(paired.Length);
        for (int i = 0; i < paired.Length; i++)
        {
            progress.SetProgress(i, paired.Length);
            quotes.Add(await CreateQuote(paired[i]));
        }

        progress.SetProgress(1);
        // TODO: Save quotes
    }

    private Task<Quote> CreateQuote(PairedWithKeyOrIndex item)
    {
        var (keyOrIndex, english, japanese) = item;

        // TODO: Create quote
        throw new NotImplementedException();
    }

    private IEnumerable<(Language Language, string Path)> EnumerateDialogueFiles()
    {
        DirectoryInfoWrapper baseDir = new(new(options.BaseDirectory));

        foreach (var (language, globs) in options.DialogueFiles)
        {
            Matcher matcher = new(StringComparison.OrdinalIgnoreCase);
            matcher.AddIncludePatterns(globs);

            foreach (FilePatternMatch match in matcher.Execute(baseDir).Files)
            {
                string fullPath = Path.GetFullPath(Path.Combine(baseDir.FullName, match.Path));
                yield return (language, fullPath);
            }
        }
    }

    private IEnumerable<PairedWithKeyOrIndex> ValidateAndFilter(
        IEnumerable<IGrouping<object, ParsedQuoteTranslation>> groupedByKey)
    {
        if (!groupedByKey.Any())
        {
            throw new Exception("Parser did not produce any results.");
        }

        bool isIntKey = groupedByKey.All(g => g.Key is int);
        if (!isIntKey)
        {
            logger.Warning("Keys are not Int32, index will be used instead.");
        }

        int index = 0;

        foreach (var group in groupedByKey)
        {
            if (group.Any(x => x.Language is not (Language.English or Language.Japanese)))
            {
                logger.Fatal("Group {Key} contains invalid languages: {@Group}", group.Key, group.ToArray());
                throw new Exception($"Group {group.Key} contains invalid languages.");
            }

            if (group.Count(x => x.Language == Language.English) > 1 ||
                group.Count(x => x.Language == Language.Japanese) > 1)
            {
                logger.Fatal("Group {Key} contains the same language multiple times: {@Group}", group.Key, group.ToArray());
                throw new Exception($"Group {group.Key} contains the same language multiple times.");
            }

            var english = group.SingleOrDefault(x => x.Language == Language.English);
            var japanese = group.SingleOrDefault(x => x.Language == Language.Japanese);
            string error;

            // This part is similar to the Skyrim importer's ValidateDialogue
            if (english is null)
            {
                error = "No English translation.";
            }
            else if (japanese is null)
            {
                error = "No Japanese translation.";
            }
            else if (string.IsNullOrWhiteSpace(english.Text))
            {
                error = "English translation is empty.";
            }
            else if (string.IsNullOrWhiteSpace(japanese.Text))
            {
                error = "Japanese translation is empty";
            }
            else if (!KanjiOrHiraganaRegex().IsMatch(japanese.Text))
            {
                error = "Japanese text contains neither kanji nor hiragana";
            }
            else if (wordAligner.EnglishTokenizer.GetWordCount(english.Text) < MinimumEnglishWordCount)
            {
                error = "English word count is below threshold";
            }
            else
            {
                // Return the paired translations with their texts trimmed of whitespace & wrapping quotes
                yield return new(
                    KeyOrIndex: isIntKey ? (int)group.Key : index++,
                    English: english with { Text = TrimQuoteText(english.Text) },
                    Japanese: japanese with { Text = TrimQuoteText(japanese.Text) }
                );

                continue;
            }

            logger.Warning("Skipping {Key}: {Reason}", group.Key, error);
        }
    }

    /// <summary>
    /// Trims whitespace and wrapping quotes.
    /// </summary>
    private static string TrimQuoteText(ReadOnlySpan<char> text)
    {
        text = text.Trim();

        if (text[0] is '"' or '“' or '「' or '『' && text[^1] is '"' or '”' or '」' or '』')
        {
            text = text[1..^1].Trim();
        }

        return text.ToString();
    }
}
