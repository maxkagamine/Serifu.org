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

using Serifu.Importer.Kancolle.Models;
using Serilog;

using static Serifu.Importer.Kancolle.Regexes;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Translates the English contexts to Japanese. This works by tokenizing the strings, such that "Equipment 2 (Kai Ni)"
/// becomes "Equipment" and "Kai Ni", and looking up their respective translations in a hardcoded table, which are
/// ultimately sourced from the <see href="https://wikiwiki.jp/kancolle/">JP wiki</see>. (Actually trying to scrape the
/// JP wiki and find the matching quotes would add a whole 'nother layer of fragility, and probably isn't possible
/// anyway due to slight variances in the Japanese text.)
/// </summary>
/// <seealso cref="ContextTokenizer"/>
internal class TranslationService
{
    private static readonly Dictionary<string, string> Translations = new()
    {
        ["2nd"]                               = "二周年",
        ["3rd"]                               = "三周年",
        ["4th"]                               = "四周年",
        ["5th"]                               = "五周年",
        ["6th"]                               = "六周年",
        ["7th"]                               = "七周年",
        ["8th"]                               = "八周年",
        ["9th"]                               = "九周年",
        ["10th"]                              = "十周年",
        ["11th"]                              = "十一周年",
        ["12th"]                              = "十二周年",
        ["13th"]                              = "十三周年",
        ["14th"]                              = "十四周年",
        ["15th"]                              = "十五周年",
        ["16th"]                              = "十六周年",
        ["17th"]                              = "十七周年",
        ["18th"]                              = "十八周年",
        ["19th"]                              = "十九周年",
        ["Air Battle"]                        = "航空戦開始",
        ["Anniversary"]                       = "記念",
        ["Attack"]                            = "昼戦攻撃",
        ["Autumn"]                            = "秋",
        ["Base & Dva"]                        = "未改＆改二",
        ["Christmas"]                         = "クリスマス",
        ["Christmas Eve"]                     = "クリスマスイブ",
        ["Coming of Spring"]                  = "春の訪れ",
        ["Completed"]                         = "完了",
        ["Construction"]                      = "建造完了",
        ["Daytime Spotting"]                  = "カットイン",
        ["Docking"]                           = "入渠",
        ["Early Autumn"]                      = "初秋",
        ["Early Fall"]                        = "初秋",
        ["Early Summer"]                      = "初夏",
        ["End of Year"]                       = "年末",
        ["Equipment"]                         = "装備",
        ["Eve of the Final Battle"]           = "決戦前夜",
        ["Fall"]                              = "秋",
        ["Final Battle"]                      = "決戦",
        ["Halloween"]                         = "ハロウィン",
        ["Hinamatsuri"]                       = "ひな祭り",
        ["Homecoming"]                        = "帰投",
        ["I-"]                                = "伊",
        ["Introduction"]                      = "入手/ログイン",
        ["Joining the Fleet"]                 = "編成",
        ["Kai"]                               = "改",
        ["Kai & Kai Ni"]                      = "改＆改二",
        ["Kai Ni"]                            = "改二",
        ["Kai Ni A"]                          = "改二甲",
        ["Kai Ni B"]                          = "改二乙",
        ["Kai Ni C"]                          = "改二丙",
        ["Kai Ni B, C"]                       = "改二乙/丙",
        ["Kai Ni B,C"]                        = "改二乙/丙",
        ["Kai Ni D"]                          = "改二丁",
        ["Kai Ni Kou"]                        = "航改二",
        ["Kou Kai Ni"]                        = "航改二",
        ["Late Autumn"]                       = "晩秋",
        ["Library"]                           = "図鑑説明",
        ["MVP"]                               = "勝利MVP",
        ["Major Damage"]                      = "中破以上",
        ["Married"]                           = "ケッコン後",
        ["Midautumn"]                         = "秋",
        ["Midsummer"]                         = "盛夏",
        ["Minor Damage"]                      = "小破以下",
        ["New Year"]                          = "新年",
        ["Night Battle"]                      = "夜戦開始",
        ["Night Battle Attack"]               = "夜戦攻撃",
        ["Overcoming This Together"]          = "共に乗り越える",
        ["Player's Score"]                    = "戦績表示",
        ["Rainy Season"]                      = "梅雨",
        ["Returning From Sortie"]             = "帰投",
        ["Sasebo Base Tour"]                  = "佐世保帰郷",
        ["Saury"]                             = "秋刀魚",
        ["Secretary"]                         = "母港",
        ["Secretary Idle"]                    = "放置時",
        ["Setsubun"]                          = "節分",
        ["Shiratsuyu Kai Ni Congratulation"]  = "白露改二記念",
        ["Shiratsuyu's Biggest Question"]     = "謎のいっちばーん",
        ["Special Attack"]                    = "特殊攻撃",
        ["Spring"]                            = "春",
        ["Starting a Battle"]                 = "昼戦開始",
        ["Starting a Sortie"]                 = "出撃",
        ["Summer"]                            = "夏",
        ["Sunk"]                              = "轟沈",
        ["Supply"]                            = "補給",
        ["UIT-"]                              = "UIT-",
        ["Valentine's Day"]                   = "バレンタイン",
        ["Wedding"]                           = "ケッコンカッコカリ",
        ["White Day"]                         = "ホワイトデー",
        ["Winter"]                            = "冬",
        ["Zuiun Festival"]                    = "瑞雲祭り",
    };

    private readonly ILogger logger;

    public TranslationService(ILogger logger)
    {
        this.logger = logger.ForContext<TranslationService>();
    }

    /// <summary>
    /// Normalizes the <paramref name="context"/> and translates it into Japanese. If any part of the string can't be
    /// translated, logs a warning and returns the normalized context for both.
    /// </summary>
    /// <param name="ship">The ship, for logging.</param>
    /// <param name="context">The context in English.</param>
    /// <returns>The normalized and translated context.</returns>
    public (string NormalizedContext, string TranslatedContext) TranslateContext(Ship ship, string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);

        context = NormalizeContext(ship, context);

        bool success = true;
        string translatedContext = ContextTokenizer.Replace(context, match =>
        {
            if (Translations.TryGetValue(match.Value, out var translatedToken))
            {
                return translatedToken;
            }

            logger.Warning("No translation for \"{Token}\" in {Ship}'s context {Context}.",
                match.Value, ship, context);

            success = false;
            return "";
        });

        if (success)
        {
            // 10th Anniversary -> 十周年 記念 -> 十周年記念
            return (context, SpacesBetweenJapaneseCharacters.Replace(translatedContext, ""));
        }
        
        return (context, context);
    }

    /// <summary>
    /// Title-cases the context and fixes some minor inconsistencies.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The normalized context.</returns>
    private static string NormalizeContext(Ship ship, string context)
    {
        // Air Battle/ Daytime Spotting/ Night Battle Attack => Air Battle / Daytime Spotting / Night Battle Attack
        context = Slash.Replace(context, " / ");

        // Valentine’s Day 2017 => Valentine's Day 2017
        context = context.Replace('’', '\'');

        // Minor Damage2 => Minor Damage 2, NightBattle => Night Battle
        context = FirstCharacterOfPascalCaseWord.Replace(context, x => " " + x.ToString());

        // Title case
        context = FirstCharacterOfWord.Replace(context, x => x.ToString().ToUpper());
        context = TitleCaseLowercaseWords.Replace(context, x => x.ToString().ToLower());

        // Equipment 2 => Equipment (excludes numbers in parenthesis in case someone writes Kai 2 instead of Kai Ni)
        context = SingleDigitNumberAfterContext.Replace(context, "");

        // Docking (Major), Docking Major => Docking (Major Damage)
        context = DockingMajorMinorDamage.Replace(context, x => $"Docking ({x.Groups[1]} Damage)");

        // Summer Event 2019 => Summer 2019, Hinamatsuri 2020 Mini-Event => Hinamatsuri 2020, 7th Anniversary 2020 => 7th Anniversary
        context = EventNextToYear.Replace(context, "");
        context = YearNextToAnniversary.Replace(context, "");

        // Special => Special Attack (but not when followed by other words, so no "Special Attack Attack" or "Special Attack Event")
        context = JustSpecial.Replace(context, "Special Attack");

        // Normalize specific patterns, based on most prominent usage in dataset
        context = context
            .Replace("Daytime Spotting / Air Battle / Night Battle Attack",
                     "Air Battle / Daytime Spotting / Night Battle Attack")
            .Replace("Night Attack", "Night Battle Attack")
            .Replace("Secondary Attack", "Night Battle Attack")
            .Replace("Starting Sortie", "Starting a Sortie")
            .Replace("Starting Battle", "Starting a Battle")
            .Replace("Battle Start", "Starting a Battle")
            .Replace("Joining a Fleet", "Joining the Fleet")
            .Replace("Saury Festival", "Saury")
            .Replace("Secretary Married", "Secretary (Married)")
            .Replace("Secretary (Idle)", "Secretary Idle")
            .Replace("Looking at Scores", "Player's Score")
            .Replace("New Years", "New Year")
            .Replace("Return From Sortie", "Returning From Sortie")
            .Replace($"{ship.EnglishName} Special Attack", "Special Attack");

        if (context.StartsWith("Idle"))
        {
            context = context.Replace("Idle", "Secretary Idle");
        }

        if (context == "Intro")
        {
            context = "Introduction";
        }

        return context;
    }
}
