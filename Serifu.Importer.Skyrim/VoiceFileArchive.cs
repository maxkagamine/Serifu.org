using DotNext.Collections.Generic;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Serifu.Importer.Skyrim;

internal partial class VoiceFileArchive
{
    private readonly IEnumerable<string> archivePaths;
    private readonly HashSet<string> excludedVoiceFiles;
    private readonly ILogger logger;
    private readonly Dictionary<(FormKey, int), Dictionary<string, IArchiveFile>> voiceFiles = []; // (DialogInfo, ResponseNumber) -> VoiceType -> Files

    [GeneratedRegex(@"^sound[\\/]voice[\\/](?<Mod>[^\\/]+)[\\/](?<VoiceType>[^\\/]+)[\\/].*_(?<FormId>[0-9a-f]{8})_(?<ResponseNumber>\d+)\.fuz$", RegexOptions.IgnoreCase)]
    private static partial Regex VoiceFileRegex { get; }

    public VoiceFileArchive(IEnumerable<string> archivePaths, IEnumerable<string> excludedVoiceFiles, ILogger logger)
    {
        this.archivePaths = archivePaths;
        this.excludedVoiceFiles = excludedVoiceFiles.ToHashSet(VoiceFilePathComparer.Instance);
        this.logger = logger.ForContext<VoiceFileArchive>().ForContext("ArchivePaths", archivePaths);

        foreach (string archivePath in archivePaths)
        {
            ReadArchive(archivePath);
        }

        if (this.excludedVoiceFiles.Count > 0)
        {
            throw new ArgumentException($"Excluded voice file{(this.excludedVoiceFiles.Count == 1 ? "" : "s")} \"{string.Join("\", \"", this.excludedVoiceFiles)}\" did not match any paths in \"{string.Join("\", \"", archivePaths)}\".", nameof(excludedVoiceFiles));
        }
    }

    private void ReadArchive(string archivePath)
    {
        logger.Information("Reading archive: {ArchivePath}", archivePath);

        IArchiveReader archive = Archive.CreateReader(GameRelease.SkyrimSE, archivePath);

        foreach (IArchiveFile file in archive.Files)
        {
            if (excludedVoiceFiles.Remove(file.Path))
            {
                continue;
            }

            Match match = VoiceFileRegex.Match(file.Path);

            if (!match.Success)
            {
                // There are a number of alternate voice lines, and a few mistakes too.
                // Psst... search the BSA for "sound/voice/skyrim.esm/malenord/wedl12_wedl12wheredidthish_000bd744" :)
                continue;
            }

            string mod = match.Groups["Mod"].Value;
            string voiceType = match.Groups["VoiceType"].Value;
            uint formId = uint.Parse(match.Groups["FormId"].Value, NumberStyles.HexNumber);
            int responseNumber = int.Parse(match.Groups["ResponseNumber"].Value); // Starts at 1

            ModKey modKey = ModKey.FromNameAndExtension(mod); // ModKey.Equals() uses OrdinalIgnoreCase
            FormKey formKey = new(modKey, formId);

            // The BSA contains duplicates. It's not clear whether the game only looks at the form id or if the quest &
            // topic names have to match, too... See the "InfoFileName" function in xEdit's "Export dialogues" script.
            voiceFiles.GetOrAdd((formKey, responseNumber), new Dictionary<string, IArchiveFile>(StringComparer.OrdinalIgnoreCase))
                .TryAdd(voiceType, file);
        }
    }

    /// <summary>
    /// Gets the available voice types for the specified dialogue.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="response">The dialogue response within <paramref name="info"/>.</param>
    /// <returns>Voice type editor IDs. Note that the casing may not match the records.</returns>
    public IEnumerable<string> GetVoiceTypes(IDialogInfoGetter info, IDialogResponseGetter response) =>
        voiceFiles.GetValueOrDefault((info.FormKey, response.ResponseNumber))?.Keys.AsEnumerable() ?? [];

    /// <summary>
    /// Determines whether the archive contains a voice file for the specified dialogue and voice type.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="response">The dialogue response within <paramref name="info"/>.</param>
    /// <param name="voiceType">The voice type editor ID.</param>
    /// <returns><see langword="true"/> if the archive contains a matching voice file; otherwise, <see
    /// langword="false"/>.</returns>
    public bool HasVoiceFile(IDialogInfoGetter info, IDialogResponseGetter response, string voiceType) =>
        voiceFiles.TryGetValue((info.FormKey, response.ResponseNumber), out var voiceTypes) &&
        voiceTypes.ContainsKey(voiceType);

    /// <summary>
    /// Gets the voice file from the archive.
    /// </summary>
    /// <param name="info">The dialogue info.</param>
    /// <param name="response">The dialogue response within <paramref name="info"/>.</param>
    /// <param name="voiceType">The voice type editor ID.</param>
    /// <exception cref="FileNotFoundException">No voice file exists for the given dialogue and voice type.</exception>
    public IArchiveFile GetVoiceFile(IDialogInfoGetter info, IDialogResponseGetter response, string voiceType)
    {
        if (!voiceFiles.TryGetValue((info.FormKey, response.ResponseNumber), out var voiceTypes) ||
            !voiceTypes.TryGetValue(voiceType, out IArchiveFile? file))
        {
            throw new FileNotFoundException(
                $"No voice file exists for {info.FormKey} response number {response.ResponseNumber} and voice type {voiceType}. Searched archives:\n- " +
                string.Join("\n- ", archivePaths));
        }

        return file;
    }

    private class VoiceFilePathComparer : IEqualityComparer<string>
    {
        public static readonly VoiceFilePathComparer Instance = new();

        public bool Equals(string? x, string? y) =>
            StringComparer.OrdinalIgnoreCase.Equals(NormalizePath(x), NormalizePath(y));

        public int GetHashCode([DisallowNull] string obj) =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(NormalizePath(obj));

        [return: NotNullIfNotNull(nameof(path))]
        private static string? NormalizePath(string? path) => path?.Replace('\\', '/');
    }
}
