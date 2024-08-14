using DotNext.Collections.Generic;
using DotNext.Threading;
using Kagamine.Extensions.Logging;
using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Serifu.Data;
using Serifu.Data.Sqlite;
using Serifu.Importer.Skyrim.Resolvers;
using Serifu.ML.Abstractions;
using Serilog;
using Serilog.Context;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Web;
using static Serifu.Data.Sqlite.ImportHelper;
using Alignment = Serifu.Data.Alignment;

namespace Serifu.Importer.Skyrim;

internal sealed partial class SkyrimImporter : IDisposable
{
    private static readonly HashSet<RecordType> ExcludedSubtypes = [
        SubtypeName.Bash, SubtypeName.Block, SubtypeName.Death, SubtypeName.EnterBowZoomBreath,
        SubtypeName.EnterSprintBreath, SubtypeName.ExitBowZoomBreath, SubtypeName.Hit, SubtypeName.OutOfBreath,
        SubtypeName.PowerAttack, SubtypeName.VoicePowerEndLong, SubtypeName.VoicePowerEndShort,
        SubtypeName.VoicePowerStartLong, SubtypeName.VoicePowerStartShort,
    ];

    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly SceneActorResolver sceneActorResolver;
    private readonly ConditionsResolver conditionsResolver;
    private readonly IFormIdProvider formIdProvider;
    private readonly ISpeakerFactory speakerFactory;
    private readonly IFuzConverter fuzConverter;
    private readonly ISqliteService sqliteService;
    private readonly IWordAligner wordAligner;
    private readonly SkyrimOptions options;
    private readonly ILogger logger;

    private readonly VoiceFileArchive englishArchive;
    private readonly VoiceFileArchive japaneseArchive;

    // Ensure only one thread is accessing the db at a time
    private readonly AsyncLock sqliteServiceLock = AsyncLock.Exclusive();

    public SkyrimImporter(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        SceneActorResolver sceneActorResolver,
        ConditionsResolver conditionsResolver,
        IFormIdProvider formIdProvider,
        ISpeakerFactory speakerFactory,
        IFuzConverter fuzConverter,
        ISqliteService sqliteService,
        IWordAligner wordAligner,
        IOptions<SkyrimOptions> options,
        ILogger logger)
    {
        this.env = env;
        this.sceneActorResolver = sceneActorResolver;
        this.conditionsResolver = conditionsResolver;
        this.formIdProvider = formIdProvider;
        this.speakerFactory = speakerFactory;
        this.fuzConverter = fuzConverter;
        this.sqliteService = sqliteService;
        this.wordAligner = wordAligner;
        this.options = options.Value;
        this.logger = logger = logger.ForContext<SkyrimImporter>();

        using (logger.BeginTimedOperation("Indexing archives"))
        {
            // VoiceFileArchive takes multiple paths so that we can use the Unofficial High Definition Audio Project mod
            // for English voices (https://www.nexusmods.com/skyrimspecialedition/mods/18115), as the vanilla voice
            // files on PC are extremely poor quality. There's no UHDAP download for Japanese, but the quality of the
            // vanilla files is noticeably better than the English ones.
            englishArchive = new(this.options.EnglishVoiceBsaPaths, this.options.ExcludedEnglishVoiceFiles, logger);
            japaneseArchive = new(this.options.JapaneseVoiceBsaPaths, this.options.ExcludedJapaneseVoiceFiles, logger);
        }
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        using var progress = new TerminalProgressBar();
        int current = 0;

        IDialogTopicGetter[] topics = GetFlattenedDialogTopics()
            .Where(t => !ExcludedSubtypes.Contains(t.SubtypeName))
            .ToArray();

        ConcurrentBag<Quote> quotes = [];

        await Parallel.ForEachAsync(topics, cancellationToken, async (topic, cancellationToken) =>
        {
            await foreach (var quote in ProcessTopic(topic, cancellationToken))
            {
                quotes.Add(quote);
            }

            Interlocked.Increment(ref current);
            progress.SetProgress(current, topics.Length);
        });

        // Remove duplicates, preferring quotes with speakers over those without
        Quote[] deduped = quotes.GroupBy(q => (q.English.Text, q.Japanese.Text))
            .Select(g => g.FirstOrDefault(q => q.English.SpeakerName != "") ?? g.First())
            .ToArray();

        logger.Information("Removed {RemovedCount} duplicate quotes.", quotes.Count - deduped.Length);

        await sqliteService.SaveQuotes(Source.Skyrim, deduped, cancellationToken);
    }

    /// <summary>
    /// Returns the winning override for every dialogue topic, with the winning overrides of each of their responses
    /// from every mod in the load order, essentially flattening the dialog topic/info record tree as the game does.
    /// </summary>
    private IEnumerable<IDialogTopicGetter> GetFlattenedDialogTopics()
    {
        // Using form key equality, with the mod listings in priority order, the winning override for the topic will be
        // added first, and every overridden instance that follows (including the original definition) will get its hash
        // set to which to add their own infos.
        Dictionary<IDialogTopicGetter, HashSet<IDialogInfoGetter>> topicInfos = new(MajorRecord.FormKeyEqualityComparer);

        foreach (IModListingGetter<ISkyrimModGetter> modListing in env.LoadOrder.PriorityOrder)
        {
            if (modListing.Mod is null)
            {
                continue;
            }

            foreach (IDialogTopicGetter topic in modListing.Mod.EnumerateMajorRecords<IDialogTopicGetter>())
            {
                HashSet<IDialogInfoGetter> infos = topicInfos.GetOrAdd(topic, _ => new(MajorRecord.FormKeyEqualityComparer));

                foreach (IDialogInfoGetter info in topic.Responses)
                {
                    if (env.LoadOrder.IndexOf(info.FormKey.ModKey) < env.LoadOrder.IndexOf(topic.FormKey.ModKey))
                    {
                        // This is a weird situation where an INFO exists in two DIALs. They look like overrides to both
                        // xEdit and Mutagen, but the game doesn't seem to treat them as such. See notes for details.
                        logger.Debug("Skipping {@Info} in {@Topic}; it's using the form ID from another mod but is not an override.",
                            info, topic);

                        continue;
                    }

                    // Similarly here too, using form key equality for the hash set, only the winning override of each
                    // info will be added. The result is the same as flattening the dialog topic/info record tree.
                    infos.Add(info);
                }
            }
        }

        return topicInfos.Select(x => new FlattenedDialogTopic(x.Key, x.Value));
    }

    /// <summary>
    /// Iterates over the topic's INFOs, determines the speaker, removes unusable dialogue, imports the voice files, and
    /// returns <see cref="Quote"/> objects for each dialogue response asynchronously.
    /// </summary>
    /// <param name="topic">The dialogue topic.</param>
    /// <param name="cancellationToken">The async enumerator cancellation token.</param>
    private async IAsyncEnumerable<Quote> ProcessTopic(IDialogTopicGetter topic, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        logger.Information("Processing topic {@Topic}", topic);

        // Skip empty INFOs and dialogue that appears to be sound effects, grunts, dragon/riekling language, etc. Also
        // resolves the Response Data (DNAM); we'll need to filter out duplicate quotes at the end anyway, and this may
        // get us a speaker when the base info has none.
        var topicDialogue = topic.Responses
            .Select(GetResponses)
            .Where(x => x.Responses.Length > 0)
            .ToArray();

        if (topicDialogue.Length == 0)
        {
            yield break;
        }

        // If the topic's quest has dialogue conditions, these are AND'd together with each INFO's conditions. The quest
        // is also used to resolve alias references in conditions.
        IQuestGetter? quest = topic.Quest.Resolve(env);
        IReadOnlyList<IConditionGetter> questDialogueConditions = quest?.DialogConditions ?? [];
        ITranslatedStringGetter? questJournalEntry = GetJournalEntry(quest);

        using (LogContext.PushProperty("Topic", topic, true))
        using (LogContext.PushProperty("TopicQuest", quest, true))
        {
            // For scene dialogue, all of the INFOs are spoken by the same actor (usually there's only one, but
            // sometimes a line will change depending on e.g. the player's gender)
            SpeakersResult sceneActorResult = sceneActorResolver.Resolve(topic);

            // Iterate over each INFO in the topic and their (already-filtered) responses. Note: There's an important
            // distinction between the "info" (belonging to the dialogue topic we're in) and the "responseDataInfo" (the
            // INFO that contains the "responses," which may be different if "info" links to another INFO via the
            // Response Data property. The former should be used for conditions, and the latter for voice files.
            foreach ((IDialogInfoGetter info, IDialogInfoGetter responseDataInfo, IDialogResponseGetter[] responses) in topicDialogue)
            {
                cancellationToken.ThrowIfCancellationRequested();

                logger.Debug("Processing info {@Info}", info);
                using (LogContext.PushProperty("Info", info, true))
                {
                    // Determine the dialogue's speaker based on its conditions, combined with the quest dialogue
                    // conditions. For scene dialogue, this will usually return the scene actor, but it's possible for
                    // the scene actor's quest alias to have multiple possible NPCs which this then narrows down.
                    SpeakersResult conditionsResult = conditionsResolver.Resolve(
                        sceneActorResult, quest, questDialogueConditions, info.Conditions);

                    // Remove any that do not have a translated name or voice file (we could do this check for each
                    // response, but presumably if an NPC has voice for one line they'll have them all & vice versa)
                    SpeakersResult result = new(
                        conditionsResult.Where(s => ValidateSpeaker(s, responseDataInfo)),
                        conditionsResult.Factions);

                    // Use INFO's Speaker as fallback if set. This field is mainly used to give a name and voice type to
                    // lines spoken by a TACT or XMarker (usually a daedra), but if the TACT already has both, it seems
                    // the Speaker is unused (example is the "Night Mother Voice NPC" in 0009724E).
                    if (result.IsEmpty && info.Speaker.TryResolve(env, out INpcGetter? infoSpeaker))
                    {
                        logger.Debug("Falling back to INFO's Speaker: {@Speaker}", infoSpeaker);
                        Speaker fallback = speakerFactory.Create(infoSpeaker);

                        if (ValidateSpeaker(fallback, responseDataInfo))
                        {
                            result = fallback;
                        }
                    }

                    // Choose a speaker from the result set
                    Speaker? speaker = null;
                    string? voiceType = null;
                    Random rnd = new((int)info.FormKey.ID);

                    if (result.IsEmpty)
                    {
                        logger.Debug("No speakers found for {@Info}.", info);
                    }
                    else
                    {
                        logger.Debug("Eligible speakers for {@Info}: {@Speakers} (factions: {Factions})",
                            info, result, result.Factions);

                        speaker = ChooseSpeaker(result, rnd);
                        voiceType = speaker.VoiceType;

                        logger.Debug("Selected {@Speaker} (voice type: {VoiceType})", speaker, speaker.VoiceType);
                    }

                    // Iterate over each line of dialogue in the INFO
                    foreach (IDialogResponseGetter response in responses)
                    {
                        // Select a random voice type if null or not available for the response (and was chosen
                        // randomly -- i.e. don't pick a new voice type that won't match the name shown on the quote)
                        string[] availableVoiceTypes = GetAvailableVoiceTypes(responseDataInfo, response).ToArray();

                        if (availableVoiceTypes.Length == 0)
                        {
                            throw new UnreachableException("Responses with no voice files should have been filtered out.");
                        }

                        if (speaker is not null && !availableVoiceTypes.Contains(speaker.VoiceType, StringComparer.OrdinalIgnoreCase))
                        {
                            throw new UnreachableException("Eligible speakers should have been filtered to those with available voice types.");
                        }

                        if (voiceType is null || !availableVoiceTypes.Contains(voiceType, StringComparer.OrdinalIgnoreCase))
                        {
                            // Storing it outside the loop will keep the voice consistent for a given dialogue info
                            voiceType = availableVoiceTypes[rnd.Next(availableVoiceTypes.Length)];
                        }

                        // Import the voice files & create the quote
                        yield return await CreateQuote(info, responseDataInfo, response, speaker, voiceType, questJournalEntry, cancellationToken);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Imports the voice files, runs word alignment, and returns a <see cref="Quote"/>.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="responseDataInfo">The dialogue response's parent info.</param>
    /// <param name="response">The dialogue response within <paramref name="responseDataInfo"/>.</param>
    /// <param name="speaker">The speaker to whom to attribute the quote, or <see langword="null"/> if unknown.</param>
    /// <param name="voiceType">The voice type of <paramref name="speaker"/>, or one selected randomly from the
    /// dialogue's available voice types if there is no speaker.</param>
    /// <param name="questJournalEntry">The dialogue topic's associated quest name or journal entry.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private async Task<Quote> CreateQuote(
        IDialogInfoGetter info,
        IDialogInfoGetter responseDataInfo,
        IDialogResponseGetter response,
        Speaker? speaker,
        string voiceType,
        ITranslatedStringGetter? questJournalEntry,
        CancellationToken cancellationToken)
    {
        var (englishText, japaneseText) = FormatQuoteText(response.Text);
        var (englishContext, japaneseContext) = questJournalEntry;
        FormID formId = formIdProvider.GetFormId(info);

        // Import voice files
        Task<string> englishVoiceFileTask = ImportVoiceFile(englishArchive, responseDataInfo, response, voiceType, "Skyrim - Voices_en0.bsa", cancellationToken);
        Task<string> japaneseVoiceFileTask = ImportVoiceFile(japaneseArchive, responseDataInfo, response, voiceType, "Skyrim - Voices_ja0.bsa", cancellationToken);

        // Run word alignment
        Task<IEnumerable<Alignment>> alignmentDataTask = wordAligner.AlignSymmetric(englishText, japaneseText, cancellationToken);

        // Wait for tasks to complete
        await Task.WhenAll(englishVoiceFileTask, japaneseVoiceFileTask, alignmentDataTask);

        // Create quote
        return new Quote()
        {
            Id = QuoteId.CreateSkyrimId(formId.Raw, response.ResponseNumber),
            Source = Source.Skyrim,
            English = new()
            {
                SpeakerName = speaker?.EnglishName ?? "",
                Context = englishContext,
                Text = englishText,
                WordCount = wordAligner.EnglishTokenizer.GetWordCount(englishText),
                Notes = HttpUtility.HtmlEncode(response.ScriptNotes.Trim()),
                AudioFile = englishVoiceFileTask.Result,
            },
            Japanese = new()
            {
                SpeakerName = speaker?.JapaneseName ?? "",
                Context = japaneseContext,
                Text = japaneseText,
                WordCount = wordAligner.JapaneseTokenizer.GetWordCount(japaneseText),
                AudioFile = japaneseVoiceFileTask.Result,
            },
            AlignmentData = alignmentDataTask.Result.ToArray()
        };
    }

    /// <summary>
    /// If the voice file corresponding to the given <paramref name="responseDataInfo"/>, <paramref name="response"/>, and <paramref
    /// name="voiceType"/> has not already been imported, extracts it from the archive, converts it to Opus, and saves
    /// it to the database.
    /// </summary>
    /// <param name="archive">The archive from which to extract the voice file.</param>
    /// <param name="responseDataInfo">The dialogue response's parent info.</param>
    /// <param name="response">The dialogue response within <paramref name="responseDataInfo"/>.</param>
    /// <param name="voiceType">The voice type editor ID.</param>
    /// <param name="bsaNameForCacheKey">The official language-specific BSA name, for use in the cache key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The imported audio file's object name.</returns>
    private async Task<string> ImportVoiceFile(
        VoiceFileArchive archive,
        IDialogInfoGetter responseDataInfo,
        IDialogResponseGetter response,
        string voiceType,
        string bsaNameForCacheKey,
        CancellationToken cancellationToken)
    {
        IArchiveFile voiceFile = archive.GetVoiceFile(responseDataInfo, response, voiceType);
        Uri cacheKey = new($"file:///{nameof(Source.Skyrim)}/Data/{bsaNameForCacheKey}#{voiceFile.Path.Replace('\\', '/')}");

        using (await sqliteServiceLock.AcquireAsync(cancellationToken))
        {
            if (await sqliteService.GetCachedAudioFile(cacheKey, cancellationToken) is string objectName)
            {
                return objectName;
            }
        }

        logger.Information("Importing {AudioFileCacheKey}", cacheKey);

        using Stream fuzStream = voiceFile.AsStream();
        using Stream opusStream = await fuzConverter.ConvertToOpus(fuzStream, cancellationToken);

        using (await sqliteServiceLock.AcquireAsync(cancellationToken))
        {
            // This will check again to see if it exists, in case another thread imported the same file between locks
            return await sqliteService.ImportAudioFile(opusStream, cacheKey, cancellationToken);
        }
    }

    /// <summary>
    /// Follows the INFO's Response Data if set and returns the dialogue responses with unusable dialogue filtered out.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <returns>A tuple containing the given <paramref name="info"/>, the info to which the responses belong (which may
    /// be different if linked via Response Data), and the filtered responses (which may be empty).</returns>
    private (IDialogInfoGetter Info, IDialogInfoGetter ResponseDataInfo, IDialogResponseGetter[] Responses) GetResponses(IDialogInfoGetter info)
    {
        HashSet<FormKey> recursedFormKeys = [info.FormKey];
        var (ResponseDataInfo, Responses) = GetResponsesInternal(info);
        return (info, ResponseDataInfo, Responses);

        (IDialogInfoGetter ResponseDataInfo, IDialogResponseGetter[] Responses) GetResponsesInternal(IDialogInfoGetter info)
        {
            if (info.ResponseData.TryResolve(env, out IDialogInfoGetter? dnam) && recursedFormKeys.Add(dnam.FormKey))
            {
                return GetResponsesInternal(dnam);
            }

            return (info, info.Responses.Where((r, i) => ValidateDialogue(info, r, i)).ToArray());
        }
    }

    /// <summary>
    /// Selects a speaker from <paramref name="speakers"/> to whom to attribute the dialogue in <paramref name="info"/>.
    /// </summary>
    /// <param name="speakers">The eligible speakers for the dialogue.</param>
    /// <param name="rnd">A <see cref="Random"/> seeded with the info form ID.</param>
    /// <returns>A <see cref="Speaker"/> from the result set, or <see langword="null"/> if it is empty.</returns>
    /// <exception cref="ArgumentException"/>
    private Speaker ChooseSpeaker(SpeakersResult speakers, Random rnd)
    {
        if (speakers.IsEmpty)
        {
            throw new ArgumentException($"Cannot call {nameof(ChooseSpeaker)} with an empty collection.", nameof(speakers));
        }

        IEnumerable<Speaker> result = speakers;

        // Filter to prioritized NPCs if any factions used in the conditions have overrides
        //
        // Due to the Civil War questline replacing guards in cities with soldiers, and the large number of generic
        // solider NPCs, a lot of guard dialogue may end up attributed to "Imperial Soldier" or "Stormcloak Soldier"
        // rather than the appropriate "Whiterun Guard" etc. otherwise. See appsettings.json for the faction overrides.
        Speaker[] factionOverride = speakers.Factions
            .SelectMany(f => options.FactionOverrides.GetValueOrDefault(f, []))
            .Distinct()
            .SelectMany(nameOrFormKey =>
            {
                if (nameOrFormKey.Contains(':'))
                {
                    var formKey = FormKey.Factory(nameOrFormKey);
                    return speakers.Where(s => s.FormKey == formKey);
                }
                else
                {
                    return speakers.Where(s => s.EnglishName == nameOrFormKey);
                }
            })
            .ToArray();

        Speaker[] factionVoiceTypeOverride = speakers.Factions
            .SelectMany(f => options.FactionVoiceTypeOverrides.GetValueOrDefault(f, []))
            .Distinct()
            .SelectMany(voiceType => speakers.Where(s => s.VoiceType.Equals(voiceType, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Speaker[] combinedOverride =
            factionVoiceTypeOverride.Length == 0 ? factionOverride :
            factionOverride.Length == 0 ? factionVoiceTypeOverride :
            factionOverride.Intersect(factionVoiceTypeOverride).ToArray();

        if (combinedOverride.Length > 0)
        {
            logger.Debug("Eligible speakers filtered by faction overrides: {@Speakers}", combinedOverride);
            result = combinedOverride;
        }
        else
        {
            // If the majority of the speakers are generic NPCs, rather than have quotes attributed to Bandit Marauder,
            // Bandit Outlaw, and so on, find the name that appears most often ("Bandit") and filter to NPCs with that
            // name. Note that we skip this when a faction override is in effect so that generic guard dialogue doesn't
            // all get attributed to Whiterun Guard.
            int uniqueCount = result.Count(s => speakerFactory.GetNpcProperty(s, npc =>
                npc.Configuration.Flags.HasFlag(NpcConfiguration.Flag.Unique)));
            float percentGeneric = 1 - ((float)uniqueCount / result.Count());

            if (percentGeneric > 0.5)
            {
                result = result
                    .GroupBy(s => s.EnglishName)
                    .MaxBy(g => g.Count())!
                    .AsEnumerable();

                logger.Debug("Eligible speakers are {Percent}% generic NPCs; filtering to most common name: {Name}",
                    Math.Round(percentGeneric * 100), result.First().EnglishName);
            }
        }

        // Select a random NPC from the resulting group
        var deduped = result.DistinctBy(s => (s.EnglishName, s.VoiceType)).ToArray();
        return deduped[rnd.Next(deduped.Length)];
    }

    /// <summary>
    /// Gets the name of the quest or journal entry to be used as the quote's <see cref="Translation.Context"/>.
    /// </summary>
    /// <param name="quest">The dialogue topic's quest.</param>
    /// <returns>The string containing the quest name or journal entry, or <see langword="null"/> if the quest would not
    /// be visible (or the name contains radiant quest aliases).</returns>
    private static ITranslatedStringGetter? GetJournalEntry(IQuestGetter? quest)
    {
        //const int MaxEnglishNameLength = 40;
        //const int MaxJapaneseNameLength = 20;

        if (quest is null)
        {
            return null;
        }

        // Quests with no objective won't appear in the journal
        var objectives = quest.Objectives
            .Where(objective =>
            {
                var (english, japanese) = objective.DisplayText;
                return !string.IsNullOrWhiteSpace(english) && !string.IsNullOrWhiteSpace(japanese);
            })
            .ToArray();

        if (objectives.Length == 0)
        {
            return null;
        }

        // For miscellaneous quests, only the objective names appear in the journal
        var journalEntry = quest.Type == Quest.TypeEnum.Misc ? objectives.First().DisplayText : quest.Name;
        var (english, japanese) = journalEntry;

        // Check that the quest/objective has a translation and that it doesn't contain aliases (radiant quests)
        if (string.IsNullOrWhiteSpace(english) || string.IsNullOrWhiteSpace(japanese) ||
            english.Contains('<') || japanese.Contains('<'))
        {
            return null;
        }

        // Filter out long quest objectives
        //if (english.Length > MaxEnglishNameLength || japanese.Length > MaxJapaneseNameLength)
        //{
        //    return null;
        //}

        return journalEntry;
    }

    /// <summary>
    /// Checks that the speaker has a translated name and voice files for the given dialogue.
    /// </summary>
    /// <param name="speaker">The speaker to validate.</param>
    /// <param name="responseDataInfo">The info containing the responses.</param>
    /// <returns><see langword="true"/> if the speaker can be used; otherwise, logs the removal reason and returns <see
    /// langword="false"/>.</returns>
    private bool ValidateSpeaker(Speaker speaker, IDialogInfoGetter responseDataInfo)
    {
        string? error = null;

        if (!speaker.HasTranslatedName || !speaker.HasVoiceType)
        {
            error = "no translated name and/or voice type";
        }
        else if (!responseDataInfo.Responses.All(r => englishArchive.HasVoiceFile(responseDataInfo, r, speaker.VoiceType)))
        {
            error = "missing English voice file";
        }
        else if (!responseDataInfo.Responses.All(r => japaneseArchive.HasVoiceFile(responseDataInfo, r, speaker.VoiceType)))
        {
            error = "missing Japanese voice file";
        }

        if (error is not null)
        {
            logger.Debug("Removing {@Speaker}: {Reason}", speaker, error);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks that the dialogue contains both English and Japanese translations, neither is wrapped in parenthesis
    /// (sound effects), the Japanese is not only katakana (to filter out dragon and riekling language), the English is
    /// more than one word, and that there are voice files available (should remove unused dialogue).
    /// </summary>
    /// <param name="responseDataInfo">The dialogue response's parent info.</param>
    /// <param name="response">The dialogue response within <paramref name="responseDataInfo"/>.</param>
    /// <param name="index">The dialogue response index, for logging.</param>
    /// <returns><see langword="true"/> if the dialogue can be used; otherwise, logs the reason and returns <see
    /// langword="false"/>.</returns>
    private bool ValidateDialogue(IDialogInfoGetter responseDataInfo, IDialogResponseGetter response, int index)
    {
        var (english, japanese) = response.Text;
        string? error = null;

        if (string.IsNullOrWhiteSpace(english) || string.IsNullOrWhiteSpace(japanese))
        {
            error = "English or Japanese text is empty";
        }
        else if ((english[0] == '(' && english[^1] == ')') || (japanese[0] is '(' or '（' && japanese[^1] is ')' or '）'))
        {
            error = "English or Japanese text is wrapped in parenthesis";
        }
        else if (!ContainsKanjiOrHiragana(japanese))
        {
            error = "Japanese text contains neither kanji nor hiragana";
        }
        else if (wordAligner.EnglishTokenizer.GetWordCount(english) == 1)
        {
            error = "one-word response or yell";
        }
        else if (!GetAvailableVoiceTypes(responseDataInfo, response).Any())
        {
            error = "voice files are missing";
        }

        if (error is not null)
        {
            logger.Debug("Skipping response #{Index} in {@Info}: {Reason}.", index, responseDataInfo, error);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the voice types that have files in both the English archive and Japanese archive for a given dialogue.
    /// </summary>
    /// <param name="responseDataInfo">The dialogue response's parent info.</param>
    /// <param name="response">The dialogue response within <paramref name="responseDataInfo"/>.</param>
    /// <returns>Voice type editor IDs. Note that the casing may not match the records.</returns>
    private IEnumerable<string> GetAvailableVoiceTypes(IDialogInfoGetter responseDataInfo, IDialogResponseGetter response) =>
        englishArchive.GetVoiceTypes(responseDataInfo, response).Intersect(japaneseArchive.GetVoiceTypes(responseDataInfo, response));

    /// <summary>
    /// Destructures <paramref name="text"/>, trims whitespace and wrapping quotes, and removes newlines / collapses
    /// whitespace.
    /// </summary>
    private static (string English, string Japanese) FormatQuoteText(ITranslatedStringGetter text)
    {
        var (english, japanese) = text;
        return (FormatEnglishText(english), FormatJapaneseText(japanese));
    }

    public void Dispose()
    {
        sqliteServiceLock.Dispose();
    }
}
