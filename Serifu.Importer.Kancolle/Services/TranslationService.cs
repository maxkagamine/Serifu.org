using Serifu.Importer.Kancolle.Helpers;
using Serifu.Importer.Kancolle.Models;
using Serilog;

namespace Serifu.Importer.Kancolle.Services;

/// <summary>
/// Translates the English contexts to Japanese. This works by tokenizing the strings, such that "Equipment 2 (Kai Ni)"
/// becomes "Equipment" and "Kai Ni", and looking up their respective translations in a hardcoded table, which are
/// ultimately sourced from the <see href="https://wikiwiki.jp/kancolle/">JP wiki</see>. (Actually trying to scrape the
/// JP wiki and find the matching quotes would add a whole 'nother layer of fragility, and probably isn't possible
/// anyway due to slight variances in the Japanese text.)
/// </summary>
/// <seealso cref="Regexes.ContextTokenizer"/>
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
    /// Translates the given <paramref name="context"/> into Japanese. If any part of the string can't be translated,
    /// logs a warning and returns the original string.
    /// </summary>
    /// <param name="ship">The ship, for logging.</param>
    /// <param name="context">The context in English.</param>
    /// <returns>The translated context, if successful; otherwise the original string.</returns>
    public string TranslateContext(Ship ship, string context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);

        bool success = true;
        string translatedContext = Regexes.ContextTokenizer.Replace(context, match =>
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
            return Regexes.SpacesBetweenJapaneseCharacters.Replace(translatedContext, "");
        }
        
        return context;
    }
}
