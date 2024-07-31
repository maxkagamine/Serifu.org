using Kagamine.Extensions.Logging;
using Kagamine.Extensions.Utilities;
using Microsoft.Extensions.Options;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serifu.Importer.Skyrim.Resolvers;
using Serilog;
using Serilog.Context;
using System.Text.RegularExpressions;

namespace Serifu.Importer.Skyrim;

internal partial class SkyrimImporter
{
    private static readonly HashSet<RecordType> ExcludedSubtypes = [
        SubtypeName.Bash, SubtypeName.Block, SubtypeName.Death, SubtypeName.EnterBowZoomBreath,
        SubtypeName.EnterSprintBreath, SubtypeName.ExitBowZoomBreath, SubtypeName.Hit, SubtypeName.OutOfBreath,
        SubtypeName.PowerAttack, SubtypeName.VoicePowerEndLong, SubtypeName.VoicePowerEndShort,
        SubtypeName.VoicePowerStartLong, SubtypeName.VoicePowerStartShort,
    ];

    [GeneratedRegex(@"[一-龠ぁ-ゔ]")]
    private static partial Regex KanjiOrHiraganaRegex();

    private readonly IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env;
    private readonly SceneActorResolver sceneActorResolver;
    private readonly ConditionsResolver conditionsResolver;
    private readonly ISpeakerFactory speakerFactory;
    private readonly SkyrimOptions options;
    private readonly ILogger logger;

    private readonly VoiceFileArchive englishArchive;
    private readonly VoiceFileArchive japaneseArchive;

    public SkyrimImporter(
        IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env,
        SceneActorResolver sceneActorResolver,
        ConditionsResolver conditionsResolver,
        ISpeakerFactory speakerFactory,
        IOptions<SkyrimOptions> options,
        ILogger logger)
    {
        this.env = env;
        this.sceneActorResolver = sceneActorResolver;
        this.conditionsResolver = conditionsResolver;
        this.speakerFactory = speakerFactory;
        this.options = options.Value;
        this.logger = logger = logger.ForContext<SkyrimImporter>();

        using (logger.BeginTimedOperation("Indexing archives"))
        {
            englishArchive = new(this.options.EnglishVoiceBsaPath, logger);
            japaneseArchive = new(this.options.JapaneseVoiceBsaPath, logger);
        }
    }

    public void Run(CancellationToken cancellationToken)
    {
        using var progress = new TerminalProgressBar();

        IDialogTopicGetter[] topics = env.LoadOrder.PriorityOrder.DialogTopic().WinningOverrides()
            .Where(t => !ExcludedSubtypes.Contains(t.SubtypeName))
            .ToArray();

        for (int i = 0; i < topics.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDialogTopicGetter topic = topics[i];
            progress.SetProgress(i, topics.Length);

            ProcessTopic(topic, cancellationToken);
        }
    }

    private void ProcessTopic(IDialogTopicGetter topic, CancellationToken cancellationToken)
    {
        logger.Information("Processing topic {@Topic}", topic);

        // Skip empty INFOs and dialogue that appears to be sound effects, grunts, dragon/riekling language, etc. Also
        // resolves the Response Data (DNAM); we'll need to filter out duplicate quotes at the end anyway, and this may
        // get us a speaker when the base info has none.
        var topicDialogue = topic.Responses
            .Select(info => (Info: info, Responses: GetResponses(info, [info.FormKey])))
            .Where(x => x.Responses.Length > 0)
            .ToArray();

        if (topicDialogue.Length == 0)
        {
            return;
        }

        // If the topic's quest has dialogue conditions, these are AND'd together with each INFO's conditions. The quest
        // is also used to resolve alias references in conditions.
        IQuestGetter? quest = topic.Quest.Resolve(env);
        IReadOnlyList<IConditionGetter> questDialogueConditions = quest?.DialogConditions ?? [];

        using (LogContext.PushProperty("Topic", topic, true))
        using (LogContext.PushProperty("TopicQuest", quest, true))
        {
            // For scene dialogue, all of the INFOs are spoken by the same actor (usually there's only one, but
            // sometimes a line will change depending on e.g. the player's gender)
            SpeakersResult sceneActorResult = sceneActorResolver.Resolve(topic);

            // Iterate over each INFO in the topic and their (already-filtered) responses
            foreach ((IDialogInfoGetter info, IDialogResponseGetter[] responses) in topicDialogue)
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
                        conditionsResult.Where(s => ValidateSpeaker(s, info)),
                        conditionsResult.Factions);

                    // Use INFO's Speaker as fallback if set. This field is mainly used to give a name and voice type to
                    // lines spoken by a TACT or XMarker (usually a daedra), but if the TACT already has both, it seems
                    // the Speaker is unused (example is the "Night Mother Voice NPC" in 0009724E).
                    if (result.IsEmpty && info.Speaker.TryResolve(env, out INpcGetter? infoSpeaker))
                    {
                        logger.Debug("Falling back to INFO's Speaker: {@Speaker}", infoSpeaker);
                        Speaker fallback = speakerFactory.Create(infoSpeaker);

                        if (ValidateSpeaker(fallback, info))
                        {
                            result = fallback;
                        }
                    }

                    if (result.IsEmpty)
                    {
                        logger.Debug("No speakers found for {@Info}.", info);
                    }
                    else
                    {
                        logger.Debug("Eligible speakers for {@Info}: {@Speakers}", info, result);
                    }

                    // TODO: Choose speaker
                }
            }
        }
    }

    /// <summary>
    /// Follows the INFO's Response Data if set and returns the dialogue responses with unusable dialogue filtered out.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="recursedFormKeys">The form keys of INFOs that have already been seen, to avoid recursion.</param>
    private IDialogResponseGetter[] GetResponses(IDialogInfoGetter info, HashSet<FormKey> recursedFormKeys)
    {
        if (info.ResponseData.TryResolve(env, out IDialogInfoGetter? dnam) && recursedFormKeys.Add(dnam.FormKey))
        {
            return GetResponses(dnam, recursedFormKeys);
        }

        return info.Responses.Where((r, i) => ValidateDialogue(info, r, i)).ToArray();
    }

    /// <summary>
    /// Checks that the speaker has a translated name and voice files for the given dialogue.
    /// </summary>
    /// <param name="speaker">The speaker to validate.</param>
    /// <param name="info">The dialogue info.</param>
    /// <returns><see langword="true"/> if the speaker can be used; otherwise, logs the removal reason and returns <see
    /// langword="false"/>.</returns>
    private bool ValidateSpeaker(Speaker speaker, IDialogInfoGetter info)
    {
        string? error = null;

        if (!speaker.HasTranslatedName || !speaker.HasVoiceType)
        {
            error = "no translated name and/or voice type";
        }
        else if (!info.Responses.All(r => englishArchive.HasVoiceFile(info, r, speaker.VoiceType)))
        {
            error = "missing English voice file";
        }
        else if (!info.Responses.All(r => japaneseArchive.HasVoiceFile(info, r, speaker.VoiceType)))
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
    /// (sound effects), and that the Japanese is not only katakana (to filter out dragon and riekling language).
    /// </summary>
    /// <param name="info">The containing info, for logging.</param>
    /// <param name="response">The dialogue response to validate.</param>
    /// <param name="index">The dialogue response index, for logging.</param>
    /// <returns><see langword="true"/> if the dialogue can be used; otherwise, logs the reason and returns <see
    /// langword="false"/>.</returns>
    private bool ValidateDialogue(IDialogInfoGetter info, IDialogResponseGetter response, int index)
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
        else if (!KanjiOrHiraganaRegex().IsMatch(japanese))
        {
            error = "Japanese text contains neither kanji nor hiragana";
        }

        if (error is not null)
        {
            logger.Debug("Skipping response #{Index} in {@Info}: {Reason}", index, info, error);
            return false;
        }

        return true;
    }
}
