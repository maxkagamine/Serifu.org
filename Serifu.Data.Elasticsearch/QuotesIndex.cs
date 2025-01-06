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
using System.Text.Json.Serialization;

namespace Serifu.Data.Elasticsearch;

public static class QuotesIndex
{
    public const string Name = "quotes";

    private const string EnglishConjugationsAnalyzer = "english"; // Built-in
    private const string JapaneseConjugationsAnalyzer = "serifu_japanese_conjugations";
    private const string JapaneseKanjiAnalyzer = "serifu_japanese_kanji";

    private const string NormalizeUnicodeCharFilter = "serifu_normalize_unicode_char_filter";
    private const string JapaneseKuromojiTokenizer = "serifu_japanese_kuromoji_tokenizer";
    private const string KanjiTokenizer = "serifu_kanji_tokenizer";
    private const string CjkFilter = "serifu_cjk_filter";
    private const string DesDivFilter = "serifu_desdiv_filter";

    public static TypeMapping Mappings => new()
    {
        Dynamic = DynamicMapping.Strict,
        Properties = new()
        {
            ["id"] = new KeywordProperty(),
            ["source"] = new KeywordProperty(),
            ["english"] = CreateTranslationMappings(EnglishConjugationsAnalyzer, includeKanjiField: false),
            ["japanese"] = CreateTranslationMappings(JapaneseConjugationsAnalyzer, includeKanjiField: true),
            ["alignmentData"] = new KeywordProperty()
            {
                Index = false
            },
            ["dateImported"] = new DateProperty()
            {
                Index = false
            },
            ["weight"] = new DoubleNumberProperty()
        }
    };

    public static IndexSettings Settings => new()
    {
        Analysis = CreateAnalysisSettings(),
        NumberOfShards = 1,
        NumberOfReplicas = 0,
        RefreshInterval = -1,
        Similarity = new()
        {
            ["default"] = new SettingsSimilarityBm25()
            {
                // Do not take document length into account when scoring (prevents Elasticsearch from effectively
                // sorting results by word count, putting shorter and less useful quotes at the top)
                b = 0
            }
        }
    };

    private static ObjectProperty CreateTranslationMappings(string conjugationsAnalyzer, bool includeKanjiField)
    {
        TextProperty text = new()
        {
            Fields = new()
            {
                ["conjugations"] = new TextProperty()
                {
                    // Unclear if search_analyzer will default to the field's analyzer or the
                    // "default_search" analyzer when both are set; documentation is conflicting.
                    Analyzer = conjugationsAnalyzer,
                    SearchAnalyzer = conjugationsAnalyzer
                }
            }
        };

        if (includeKanjiField)
        {
            text.Fields.Add("kanji", new TextProperty()
            {
                Analyzer = JapaneseKanjiAnalyzer,
                SearchAnalyzer = JapaneseKanjiAnalyzer
            });
        }

        return new ObjectProperty()
        {
            Properties = new()
            {
                ["speakerName"] = new TextProperty()
                {
                    Fields = new()
                    {
                        ["keyword"] = new KeywordProperty()
                    }
                },
                ["context"] = new TextProperty(),
                ["text"] = text,
                ["wordCount"] = new IntegerNumberProperty(),
                ["notes"] = new KeywordProperty()
                {
                    Index = false
                },
                ["audioFile"] = new KeywordProperty()
                {
                    Index = false
                }
            }
        };
    }

    private static IndexSettingsAnalysis CreateAnalysisSettings() => new()
    {
        Analyzers = new()
        {
            ["default"] = CreateDefaultAnalyzer(),
            ["default_search"] = CreateDefaultAnalyzer(),
            [JapaneseConjugationsAnalyzer] = new CustomAnalyzer()
            {
                CharFilter = [
                    NormalizeUnicodeCharFilter
                ],
                Tokenizer = JapaneseKuromojiTokenizer,
                Filter = [
                    "kuromoji_number",
                    "kuromoji_baseform",
                    "kuromoji_part_of_speech",
                    "cjk_width",
                    "ja_stop",
                    "kuromoji_stemmer",
                    "lowercase"
                ]
            },
            [JapaneseKanjiAnalyzer] = new CustomAnalyzer()
            {
                Tokenizer = KanjiTokenizer
            }
        },
        CharFilters = new()
        {
            [NormalizeUnicodeCharFilter] = new IcuNormalizationCharFilter()
            {
                Name = IcuNormalizationType.Nfkc,
                Mode = IcuNormalizationMode.Compose
            }
        },
        Tokenizers = new()
        {
            [JapaneseKuromojiTokenizer] = new KuromojiTokenizer()
            {
                Mode = KuromojiTokenizationMode.Search,
                DiscardCompoundToken = true
            },
            [KanjiTokenizer] = new PatternTokenizer()
            {
                Pattern = @"\p{IsHan}",
                Group = 0
            }
        },
        TokenFilters = new()
        {
            [DesDivFilter] = new PatternCaptureTokenFilter()
            {
                Patterns = [
                    // A search for "desdiv" should pull up "DesDiv3", "4thDesDiv" etc.
                    // See https://en.kancollewiki.net/Historical_Formations
                    @"^(\\d+(?:st|nd|rd|th))?((?:bat|car|cru|des)(?:div|ron))(\\d+)?$"
                ],
                PreserveOriginal = false
            },
            [CjkFilter] = new CjkBigramTokenFilter()
            {
                OutputUnigrams = false
            }
        }
    };

    private static CustomAnalyzer CreateDefaultAnalyzer() => new()
    {
        Tokenizer = "standard",
        Filter = [
            "icu_folding", // Normalizes unicode, strips accents, and lowercases
            DesDivFilter,
            CjkFilter
        ]
    };

    // Missing from the .NET library
    private class CjkBigramTokenFilter : ITokenFilter
    {
        [JsonInclude, JsonPropertyName("output_unigrams")]
        public bool? OutputUnigrams { get; set; }

        [JsonInclude, JsonPropertyName("ignored_scripts")]
        public ICollection<string>? IgnoredScripts { get; set; }

        [JsonInclude, JsonPropertyName("type")]
        public string? Type => "cjk_bigram";
    }
}
