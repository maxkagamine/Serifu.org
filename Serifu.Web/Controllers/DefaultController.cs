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

using Microsoft.AspNetCore.Mvc;
using Serifu.Data;
using Serifu.Web.Models;
using System.Globalization;

namespace Serifu.Web.Controllers;

public class DefaultController : Controller
{
    [HttpGet("/")]
    public ActionResult Index()
    {
        // This is the only route that won't have a "lang" route value set. This causes the culture provider to fall
        // back to using a cookie if set and Accept-Language otherwise; we can then use the determined culture to
        // redirect to the appropriate homepage.
        return RedirectToAction(nameof(Home), new { lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName });
    }

    [HttpGet("/translate")]
    [HttpGet("/翻訳")]
    public ActionResult Home(string? query)
    {
        if (query is not null)
        {
            // Fallback in case user has JS disabled
            Response.Headers.Append("X-Robots-Tag", "noindex");
            return RedirectToAction(nameof(Results), new { query });
        }

        return View();
    }

    [HttpGet("/translate/{query}")]
    [HttpGet("/翻訳/{query}")]
    public ActionResult Results(string query)
    {
        Quote quote1 = new() // Typical
        {
            Id = 0,
            Source = Source.Skyrim,
            English = new()
            {
                SpeakerName = "Balgruuf the Greater",
                Context = "Before the Storm",
                Text = "Come, let's go find Farengar, my court wizard. He's been looking into a matter related to these dragons and... rumors of dragons.",
                AudioFile = "0c/63/a585f3f43e28f860a86dcc4d995c00ac00af.opus",
                WordCount = 0,
            },
            Japanese = new()
            {
                SpeakerName = "偉大なるバルグルーフ",
                Context = "嵐の前",
                Text = "さあ、王宮魔術師のファレンガーを探しに行こう。ドラゴンに関連したことやドラゴンの噂について、ずっと調べてもらっていたんだ",
                AudioFile = "17/ef/9c5a89dfcebd59f1073e63f49b4a81a9a56f.opus",
                WordCount = 0
            },
            AlignmentData = [],
        };

        Quote quote2 = new() // Long, with notes, no EN audio
        {
            Id = 0,
            Source = Source.Kancolle,
            English = new()
            {
                SpeakerName = "Zuihou",
                Context = "Library",
                Text = "I'm the Shouhou-class light carrier, Zuihou. I was originally planned as a high-speed oiler, then a submarine tender, then I was finally completed as a light carrier. I may have a small body but I fought to the last days of the Task Force!",
                WordCount = 0,
                Notes = """
                    The IJN officially classified her as a Zuihou-class carrier, the Shouhou-class designation was used in post-war publications.<br>
                    She was originally planned as the oiler Takasaki, later submarine tender after Japan left the <a href="https://en.wikipedia.org/wiki/London_Naval_Treaty">London Naval Treaty</a>.
                    """
            },
            Japanese = new()
            {
                SpeakerName = "瑞鳳",
                Context = "図鑑説明",
                Text = "祥鳳型軽空母、瑞鳳です。元々は高速給油艦として計画され、次に潜水母艦、最終的に軽空母として完成しました。小柄なボディだけれど、機動部隊最後の日まで敢闘しました！",
                AudioFile = "22/28/167dd335b103f0972a38c2ed88214996fcd9.ogg",
                WordCount = 0
            },
            AlignmentData = [],
        };

        Quote quote3 = new() // Short, no context, no speaker name (it's actually Solitude Guard but whatever)
        {
            Id = 0,
            Source = Source.Skyrim,
            English = new()
            {
                SpeakerName = "",
                Context = "",
                Text = "Let me guess - someone stole your sweetroll...",
                AudioFile = "c6/41/2a5eeb0f795f249f56a50ca4b38f44e7edc7.opus",
                WordCount = 0,
            },
            Japanese = new()
            {
                SpeakerName = "",
                Context = "",
                Text = "当ててやろうか？　誰かにスイートロールを盗まれたかな？",
                AudioFile = "47/16/c70487b0dd9e9248fe30cc70cac702955fea.opus",
                WordCount = 0
            },
            AlignmentData = [],
        };

        bool englishFirst = true;
        string audioFileBaseUrl = "http://localhost:3939";

        ResultsViewModel model = new()
        {
            Quotes = [
                new QuoteViewModel(quote1, englishFirst, audioFileBaseUrl),
                new QuoteViewModel(quote2, englishFirst, audioFileBaseUrl),
                new QuoteViewModel(quote3, englishFirst, audioFileBaseUrl)
            ]
        };

        return View(model);
    }

    [HttpGet("/about")]
    [HttpGet("/について")]
    public ActionResult About()
    {
        return View();
    }
}
