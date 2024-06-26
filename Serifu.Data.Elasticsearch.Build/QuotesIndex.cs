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

using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Serifu.Data.Elasticsearch.Build;

internal static class QuotesIndex
{
    const string EnglishDefaultAnalyzer = "english_default";
    const string EnglishConjugationsAnalyzer = "english"; // Built-in
    const string JapaneseDefaultAnalyzer = "japanese_default";
    const string JapaneseConjugationsAnalyzer = "japanese_conjugations";
    const string JapaneseKanjiAnalyzer = "japanese_kanji";

    const string KeywordField = "keyword";
    const string ConjugationsField = "conjugations";
    const string KanjiField = "kanji";

    const string NormalizeUnicodeCharFilter = "normalize_unicode";
    const string JapaneseKuromojiTokenizer = "japanese_kuromoji_tokenizer";
    const string KanjiTokenizer = "kanji_tokenizer";
    const string DesDivFilter = "desdiv";

    public static CreateIndexRequestDescriptor<Quote> Descriptor => new CreateIndexRequestDescriptor<Quote>()
        .Mappings(x => x
            .Dynamic(DynamicMapping.Strict)
            .Properties(x => x
                .Keyword(q => q.Id)
                .Keyword(q => q.Source)
                .Object(q => q.English, x => x
                    .Properties(x => x
                        .Text(q => q.English.SpeakerName, x => x
                            .Fields(x => x
                                .Keyword(KeywordField)))
                        .Text(q => q.English.Context)
                        .Text(q => q.English.Text, x => x
                            .Analyzer(EnglishDefaultAnalyzer)
                            .Fields(x => x
                                .Text(ConjugationsField, x => x
                                    .Analyzer(EnglishConjugationsAnalyzer))))
                        .Keyword(q => q.English.Notes, x => x
                            .Index(false))
                        .Keyword(q => q.English.AudioFile!, x => x
                            .Index(false))))
                .Object(q => q.Japanese, x => x
                    .Properties(x => x
                        .Text(q => q.Japanese.SpeakerName, x => x
                            .Analyzer(JapaneseDefaultAnalyzer)
                            .Fields(x => x
                                .Keyword(KeywordField)))
                        .Text(q => q.Japanese.Context, x => x
                            .Analyzer(JapaneseDefaultAnalyzer))
                        .Text(q => q.Japanese.Text, x => x
                            .Analyzer(JapaneseDefaultAnalyzer)
                            .Fields(x => x
                                .Text(ConjugationsField, x => x
                                    .Analyzer(JapaneseConjugationsAnalyzer))
                                .Text(KanjiField, x => x
                                    .Analyzer(JapaneseKanjiAnalyzer))))
                        .Keyword(q => q.Japanese.Notes, x => x
                            .Index(false))
                        .Keyword(q => q.Japanese.AudioFile!, x => x
                            .Index(false))))
                .Date(q => q.DateImported, x => x
                    .Index(false))
                .Keyword(q => q.AlignmentData, x => x
                    .Index(false))))
        .Settings(x => x
            .Analysis(x => x
                .Analyzers(x => x
                    .Custom(EnglishDefaultAnalyzer, x => x
                        .Tokenizer("standard")
                        .Filter([
                            "icu_folding", // Normalizes unicode, strips accents, and lowercases
                            DesDivFilter
                        ]))
                    .Custom(JapaneseDefaultAnalyzer, x => x
                        .CharFilter([
                            NormalizeUnicodeCharFilter
                        ])
                        .Tokenizer(JapaneseKuromojiTokenizer)
                        .Filter([
                            "cjk_width",
                            "lowercase"
                        ]))
                    .Custom(JapaneseConjugationsAnalyzer, x => x
                        .CharFilter([
                            NormalizeUnicodeCharFilter
                        ])
                        .Tokenizer(JapaneseKuromojiTokenizer)
                        .Filter([
                            "kuromoji_number",
                            "kuromoji_baseform",
                            "kuromoji_part_of_speech",
                            "cjk_width",
                            "ja_stop",
                            "kuromoji_stemmer",
                            "lowercase"
                        ]))
                    .Custom(JapaneseKanjiAnalyzer, x => x
                        .Tokenizer(KanjiTokenizer)))
                .CharFilters(x => x
                    .IcuNormalization(NormalizeUnicodeCharFilter, x => x
                        .Name(IcuNormalizationType.Nfkc)
                        .Mode(IcuNormalizationMode.Compose)))
                .Tokenizers(x => x
                    .Kuromoji(JapaneseKuromojiTokenizer, x => x
                        .Mode(KuromojiTokenizationMode.Search)
                        .DiscardCompoundToken())
                    .Pattern(KanjiTokenizer, x => x
                        .Pattern(@"(\p{IsHan})")
                        .Group(0)))
                .TokenFilters(x => x
                    .PatternCapture(DesDivFilter, x => x
                        .Patterns([
                            // A search for "desdiv" should pull up "DesDiv3" etc.
                            @"^((?:bat|car|cru|des|dru)div)\d+$"
                        ]))))
            .Index(x => x
                .NumberOfShards(1)
                .NumberOfReplicas(0)
                .RefreshInterval(-1)));
}
