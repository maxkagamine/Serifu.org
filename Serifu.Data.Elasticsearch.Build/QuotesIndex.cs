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

namespace Serifu.Data.Elasticsearch.Build;

internal static class QuotesIndex
{
    private const string IndexName = "quotes";

    private const string EnglishConjugationsAnalyzer = "english"; // Built-in
    private const string JapaneseConjugationsAnalyzer = "serifu_japanese_conjugations";

    private const string NormalizeUnicodeCharFilter = "serifu_normalize_unicode_char_filter";
    private const string JapaneseKuromojiTokenizer = "serifu_japanese_kuromoji_tokenizer";
    private const string CjkFilter = "serifu_cjk_filter";
    private const string DesDivFilter = "serifu_desdiv_filter";

    public static CreateIndexRequest Create() => new(IndexName)
    {
        Mappings = CreateMappings(),
        Settings = CreateSettings(),
    };

    private static TypeMapping CreateMappings() => new()
    {
        Dynamic = DynamicMapping.Strict,
        Properties = new()
        {
            ["id"] = new KeywordProperty(),
            ["source"] = new KeywordProperty(),
            ["english"] = CreateTranslationMappings(EnglishConjugationsAnalyzer),
            ["japanese"] = CreateTranslationMappings(JapaneseConjugationsAnalyzer),
            ["dateImported"] = new DateProperty()
            {
                Index = false
            },
            ["alignmentData"] = new KeywordProperty()
            {
                Index = false
            }
        }
    };

    private static ObjectProperty CreateTranslationMappings(string conjugationsAnalyzer) => new()
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
            ["text"] = new TextProperty()
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
            },
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

    private static IndexSettings CreateSettings() => new()
    {
        Analysis = CreateAnalysisSettings(),
        NumberOfShards = 1,
        NumberOfReplicas = 0,
        RefreshInterval = -1
    };

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
            }
        },
        TokenFilters = new()
        {
            [DesDivFilter] = new PatternCaptureTokenFilter()
            {
                Patterns = [
                    // A search for "desdiv" should pull up "DesDiv3" etc.
                    @"^((?:bat|car|cru|des|dru)div)\d+$"
                ],
                PreserveOriginal = false
            },
            [CjkFilter] = new CjkBigramTokenFilter()
            {
                OutputUnigrams = true
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
