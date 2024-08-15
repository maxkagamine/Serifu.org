// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Options;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serifu.ML.Abstractions;
using Serilog;
using Serilog.Events;

using static Serifu.Data.Sqlite.ImportHelper;

namespace Serifu.Importer.Generic;

internal partial class GenericImporter
{
    // Note: The max word count here is quite long, and only to prevent the ML pipeline from exploding. We may want to
    // put a lower clamp on the quote length, possibly in the Elasticsearch build so that it applies to all sources.
    private const int MinimumEnglishWordCount = 2;
    private const int MaximumEnglishWordCount = 100;

    private static readonly string[] AudioFileExtensions = ["opus", "ogg", "mp3"];

    private readonly IParser parser;
    private readonly ISqliteService sqliteService;
    private readonly IWordAligner wordAligner;
    private readonly ParserOptions options;
    private readonly ILogger logger;

    private record PairedWithKeyOrIndex(int KeyOrIndex, ParsedQuoteTranslation English, ParsedQuoteTranslation Japanese);

    public GenericImporter(
        IParser parser,
        ISqliteService sqliteService,
        IWordAligner wordAligner,
        IOptions<ParserOptions> options,
        ILogger logger)
    {
        this.parser = parser;
        this.sqliteService = sqliteService;
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
            .Select(ReplaceSpeakerName)
            .Distinct()
            .GroupBy(x => x.Key)
            .ToArray();

        // Filter out unusable quotes and duplicates, preferring quotes with speakers over those without
        PairedWithKeyOrIndex[] paired = ValidateAndFilter(groupedByKey, cancellationToken)
            .GroupBy(x => (x.English.Text, x.Japanese.Text))
            .Select(g => g.FirstOrDefault(x => x.English.SpeakerName != "") ?? g.First())
            .ToArray();

        logger.Information("Found {Count} quotes ({RemovedCount} filtered out).",
            paired.Length, groupedByKey.Length - paired.Length);

        // Import audio files and run word alignment
        List<Quote> quotes = new(paired.Length);
        for (int i = 0; i < paired.Length; i++)
        {
            progress.SetProgress(i, paired.Length);
            cancellationToken.ThrowIfCancellationRequested();

            quotes.Add(await CreateQuote(paired[i], cancellationToken));
        }

        await sqliteService.SaveQuotes(options.Source, quotes, cancellationToken);
    }

    private async Task<Quote> CreateQuote(PairedWithKeyOrIndex item, CancellationToken cancellationToken)
    {
        var (keyOrIndex, english, japanese) = item;

        // Import audio files
        Task<string?> englishAudioFileTask = ImportAudioFile(english, cancellationToken);
        Task<string?> japaneseAudioFileTask = ImportAudioFile(japanese, cancellationToken);

        // Run word alignment
        Task<IEnumerable<Alignment>> alignmentDataTask = wordAligner.AlignSymmetric(english.Text, japanese.Text, cancellationToken);
        
        // Wait for tasks to complete
        await Task.WhenAll(englishAudioFileTask, japaneseAudioFileTask, alignmentDataTask);

        // Create quote
        return new Quote()
        {
            Id = QuoteId.CreateGenericId(options.Source, keyOrIndex),
            Source = options.Source,
            English = new()
            {
                SpeakerName = english.SpeakerName,
                Context = english.Context,
                Text = english.Text,
                WordCount = wordAligner.EnglishTokenizer.GetWordCount(english.Text),
                Notes = english.Notes,
                AudioFile = englishAudioFileTask.Result,
            },
            Japanese = new()
            {
                SpeakerName = japanese.SpeakerName,
                Context = japanese.Context,
                Text = japanese.Text,
                WordCount = wordAligner.JapaneseTokenizer.GetWordCount(japanese.Text),
                Notes = japanese.Notes,
                AudioFile = japaneseAudioFileTask.Result,
            },
            AlignmentData = alignmentDataTask.Result.ToArray()
        };
    }

    private async Task<string?> ImportAudioFile(ParsedQuoteTranslation tl, CancellationToken cancellationToken)
    {
        if (tl.AudioFilePath is null)
        {
            return null;
        }

        // Try the file path as given, then with each file extension until we find it in the cache or on disk
        IEnumerable<string> audioFilePaths = AudioFileExtensions.Select(x => $"{tl.AudioFilePath}.{x}")
            .Prepend(tl.AudioFilePath);

        foreach (string audioFilePath in audioFilePaths)
        {
            // Check cache
            string relativePath = Path.Combine(options.AudioDirectories[tl.Language], audioFilePath).Replace('\\', '/');
            Uri cacheKey = new($"file:///{options.Source}/{relativePath}");

            if (await sqliteService.GetCachedAudioFile(cacheKey, cancellationToken) is string objectName)
            {
                return objectName;
            }

            // Check directory
            string absolutePath = Path.GetFullPath(relativePath, options.BaseDirectory);

            if (File.Exists(absolutePath))
            {
                logger.Information("Importing {Path}", absolutePath);

                using FileStream stream = File.OpenRead(absolutePath);
                return await sqliteService.ImportAudioFile(stream, cacheKey, cancellationToken);
            }
        }

        logger.Error("Audio file {Path} for {Key} ({Language}) does not exist (searched for {AudioFilePaths} in {AudioDirectory}).",
            tl.AudioFilePath, tl.Key, tl.Language, audioFilePaths, Path.GetFullPath(options.AudioDirectories[tl.Language], options.BaseDirectory));

        return null;
    }

    private IEnumerable<(Language Language, string Path)> EnumerateDialogueFiles()
    {
        DirectoryInfoWrapper baseDir = new(new(options.BaseDirectory));

        foreach (var (language, globs) in options.DialogueFiles)
        {
            Matcher matcher = new(StringComparison.OrdinalIgnoreCase);

            foreach (string glob in globs)
            {
                if (glob.StartsWith('!'))
                {
                    matcher.AddExclude(glob[1..]);
                }
                else
                {
                    matcher.AddInclude(glob);
                }
            }

            foreach (FilePatternMatch match in matcher.Execute(baseDir).Files)
            {
                string fullPath = Path.GetFullPath(match.Path, baseDir.FullName);
                yield return (language, fullPath);
            }
        }
    }

    private IEnumerable<PairedWithKeyOrIndex> ValidateAndFilter(
        IEnumerable<IGrouping<object, ParsedQuoteTranslation>> groupedByKey, CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();

            if (group.Any(x => x.Language is not (Language.English or Language.Japanese)))
            {
                logger.Fatal("Group {Key} contains invalid languages: {@Group}", group.Key, group.ToArray());
                throw new Exception($"Group {group.Key} contains invalid languages.");
            }

            if (group.Count(x => x.Language == Language.English) > 1 ||
                group.Count(x => x.Language == Language.Japanese) > 1)
            {
                logger.Write(options.IgnoreDuplicateKeysWithinLanguage ? LogEventLevel.Warning : LogEventLevel.Fatal,
                    "Group {Key} contains the same language multiple times: {@Group}", group.Key, group.ToArray());

                if (!options.IgnoreDuplicateKeysWithinLanguage)
                {
                    throw new Exception($"Group {group.Key} contains the same language multiple times.");
                }
            }

            var english = group.FirstOrDefault(x => x.Language == Language.English);
            var japanese = group.FirstOrDefault(x => x.Language == Language.Japanese);
            string error;

            Lazy<int> englishWordCount = new(() => wordAligner.EnglishTokenizer.GetWordCount(english!.Text));

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
            else if (!ContainsKanjiOrHiragana(japanese.Text))
            {
                error = "Japanese text contains neither kanji nor hiragana";
            }
            else if (englishWordCount.Value < MinimumEnglishWordCount)
            {
                error = "English word count is below threshold";
            }
            else if (englishWordCount.Value > MaximumEnglishWordCount)
            {
                error = "English word count exceeds threshold";
            }
            else
            {
                // Return the paired translations with their texts trimmed of whitespace & wrapping quotes
                yield return new(
                    KeyOrIndex: isIntKey ? (int)group.Key : index++,
                    English: english with { Text = FormatEnglishText(english.Text) },
                    Japanese: japanese with { Text = FormatJapaneseText(japanese.Text) }
                );

                continue;
            }

            logger.Warning("Skipping {Key}: {Reason}", group.Key, error);
        }
    }

    private ParsedQuoteTranslation ReplaceSpeakerName(ParsedQuoteTranslation tl)
    {
        if (options.SpeakerNameMap.TryGetValue(tl.Language, out var map) &&
            map.TryGetValue(tl.SpeakerName, out var name))
        {
            return tl with { SpeakerName = name };
        }

        return tl;
    }
}
