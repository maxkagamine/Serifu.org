using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Serifu.Importer.Skyrim;

internal partial class VoiceFileArchive
{
    private readonly IEnumerable<string> archivePaths;
    private readonly ILogger logger;
    private readonly Dictionary<VoiceFileIdentifier, IArchiveFile> voiceFiles = [];

    private readonly record struct VoiceFileIdentifier(FormKey DialogInfo, string VoiceType, int ResponseNumber)
    {
        public VoiceFileIdentifier(IDialogInfoGetter info, IDialogResponseGetter response, string voiceType)
            : this(info.FormKey, voiceType.ToLowerInvariant(), response.ResponseNumber)
        { }
    }

    [GeneratedRegex(@"^sound[\\/]voice[\\/](?<Mod>[^\\/]+)[\\/](?<VoiceType>[^\\/]+)[\\/].*_(?<FormId>[0-9a-f]{8})_(?<ResponseNumber>\d+)\.fuz$", RegexOptions.IgnoreCase)]
    private static partial Regex VoiceFileRegex();

    public VoiceFileArchive(IEnumerable<string> archivePaths, ILogger logger)
    {
        this.archivePaths = archivePaths;
        this.logger = logger.ForContext<VoiceFileArchive>().ForContext("ArchivePaths", archivePaths);

        foreach (string archivePath in archivePaths)
        {
            ReadArchive(archivePath);
        }
    }

    private void ReadArchive(string archivePath)
    {
        logger.Information("Reading archive: {ArchivePath}", archivePath);

        IArchiveReader archive = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);

        foreach (IArchiveFile file in archive.Files)
        {
            Match match = VoiceFileRegex().Match(file.Path);

            if (!match.Success)
            {
                // There are a number of alternate voice lines, and a few mistakes too.
                // Psst... search the BSA for "sound/voice/skyrim.esm/malenord/wedl12_wedl12wheredidthish_000bd744" :)
                continue;
            }

            string mod = match.Groups["Mod"].Value;
            string voiceType = match.Groups["VoiceType"].Value.ToLowerInvariant(); // Make sure to lower when checking dict
            uint formId = uint.Parse(match.Groups["FormId"].Value, NumberStyles.HexNumber);
            int responseNumber = int.Parse(match.Groups["ResponseNumber"].Value); // Starts at 1

            ModKey modKey = ModKey.FromNameAndExtension(mod); // ModKey.Equals() uses OrdinalIgnoreCase
            FormKey formKey = new(modKey, formId);

            // The BSA contains duplicates. It's not clear whether the game only looks at the form id or if the quest &
            // topic names have to match, too... See the "InfoFileName" function in xEdit's "Export dialogues" script.
            voiceFiles.TryAdd(new(formKey, voiceType, responseNumber), file);
        }
    }

    /// <summary>
    /// Determines whether the archive contains a voice file for the specified dialogue and voice type.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="response">The dialogue response within <paramref name="info"/>.</param>
    /// <param name="voiceType">The voice type editor ID.</param>
    /// <returns><see langword="true"/> if the archive contains a matching voice file; otherwise, <see
    /// langword="false"/>.</returns>
    public bool HasVoiceFile(IDialogInfoGetter info, IDialogResponseGetter response, string voiceType) =>
        voiceFiles.ContainsKey(new(info, response, voiceType));

    /// <summary>
    /// Gets the voice file stream from the archive.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="response">The dialogue response within <paramref name="info"/>.</param>
    /// <param name="voiceType">The voice type editor ID.</param>
    /// <exception cref="FileNotFoundException">No voice file exists for the given dialogue and voice type.</exception>
    public Stream GetStream(IDialogInfoGetter info, IDialogResponseGetter response, string voiceType)
    {
        VoiceFileIdentifier identifier = new(info, response, voiceType);
        if (!voiceFiles.TryGetValue(identifier, out IArchiveFile? file))
        {
            throw new FileNotFoundException(
                $"No voice file exists matching {identifier}. Searched archives:\n- " +
                string.Join("\n- ", archivePaths));
        }

        return file.AsStream();
    }
}
