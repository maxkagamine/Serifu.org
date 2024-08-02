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

using DotNext.Collections.Generic;
using Loqui;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Assets;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Binary.Translations;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Strings;
using Noggog.StructuredStrings;

namespace Serifu.Importer.Skyrim;

internal class FlattenedDialogTopic : IDialogTopicGetter
{
    private readonly IDialogTopicGetter winningOverride;

    public FlattenedDialogTopic(IDialogTopicGetter winningOverride, IEnumerable<IDialogInfoGetter> infos)
    {
        this.winningOverride = winningOverride;
        Responses = infos.ToArray();
    }

    /// <summary>
    /// The winning overrides of each INFO in the topic from every mod in the load order.
    /// </summary>
    public IReadOnlyList<IDialogInfoGetter> Responses { get; }

    #region Passthrough to winning override
    public ITranslatedStringGetter? Name => winningOverride.Name;
    ITranslatedStringGetter ITranslatedNamedRequiredGetter.Name => ((ITranslatedNamedRequiredGetter)winningOverride).Name;
    public float Priority => winningOverride.Priority;
    public IFormLinkNullableGetter<IDialogBranchGetter> Branch => winningOverride.Branch;
    public IFormLinkNullableGetter<IQuestGetter> Quest => winningOverride.Quest;
    public DialogTopic.TopicFlag TopicFlags => winningOverride.TopicFlags;
    public DialogTopic.CategoryEnum Category => winningOverride.Category;
    public DialogTopic.SubtypeEnum Subtype => winningOverride.Subtype;
    public RecordType SubtypeName => winningOverride.SubtypeName;
    public int Timestamp => winningOverride.Timestamp;
    public int Unknown => winningOverride.Unknown;
    public ushort FormVersion => winningOverride.FormVersion;
    public ushort Version2 => winningOverride.Version2;
    public SkyrimMajorRecord.SkyrimMajorRecordFlag SkyrimMajorRecordFlags => winningOverride.SkyrimMajorRecordFlags;
    public bool IsCompressed => winningOverride.IsCompressed;
    public bool IsDeleted => winningOverride.IsDeleted;
    public int MajorRecordFlagsRaw => winningOverride.MajorRecordFlagsRaw;
    public uint VersionControl => winningOverride.VersionControl;
    public string? EditorID => winningOverride.EditorID;
    public FormKey FormKey => winningOverride.FormKey;
    public Type Type => winningOverride.Type;
    public object BinaryWriteTranslator => winningOverride.BinaryWriteTranslator;
    public ILoquiRegistration Registration => winningOverride.Registration;
    ushort? IMajorRecordGetter.FormVersion => ((IMajorRecordGetter)winningOverride).FormVersion;
    ushort? IFormVersionGetter.FormVersion => ((IFormVersionGetter)winningOverride).FormVersion;
    string? INamedGetter.Name => ((INamedGetter)winningOverride).Name;
    string INamedRequiredGetter.Name => ((INamedRequiredGetter)winningOverride).Name;
    public object CommonInstance() => winningOverride.CommonInstance();
    public object? CommonSetterInstance() => winningOverride.CommonSetterInstance();
    public object CommonSetterTranslationInstance() => winningOverride.CommonSetterTranslationInstance();
    public IEnumerable<IAssetLinkGetter> EnumerateAssetLinks(AssetLinkQuery queryCategories, IAssetLinkCache? linkCache = null, Type? assetType = null) => winningOverride.EnumerateAssetLinks(queryCategories, linkCache, assetType);
    public IEnumerable<IFormLinkGetter> EnumerateFormLinks() => winningOverride.EnumerateFormLinks();
    public IEnumerable<IMajorRecordGetter> EnumerateMajorRecords() => winningOverride.EnumerateMajorRecords();
    public IEnumerable<IMajorRecordGetter> EnumerateMajorRecords(Type type, bool throwIfUnknown = true) => winningOverride.EnumerateMajorRecords(type, throwIfUnknown);
    public bool Equals(IFormLinkGetter? other) => winningOverride.Equals(other);
    public void Print(StructuredStringBuilder sb, string? name = null) => winningOverride.Print(sb, name);
    public void WriteToBinary(MutagenWriter writer, TypedWriteParams translationParams = default) => winningOverride.WriteToBinary(writer, translationParams);
    IEnumerable<T> IMajorRecordGetterEnumerable.EnumerateMajorRecords<T>(bool throwIfUnknown) => winningOverride.EnumerateMajorRecords<T>(throwIfUnknown);
    #endregion
}
