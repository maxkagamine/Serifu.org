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
using System.Globalization;

namespace Serifu.Web.Controllers;

public class HomeController : Controller
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
    public ActionResult Home()
    {
        return View();
    }
}
