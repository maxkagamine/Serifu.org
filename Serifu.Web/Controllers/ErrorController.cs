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

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serifu.Data.Elasticsearch;
using Serifu.Web.Models;

namespace Serifu.Web.Controllers;

public sealed class ErrorController : Controller
{
    [Route("/Error/{statusCode:range(400, 599)}")]
    public ActionResult Error(int statusCode)
    {
        var exceptionHandler = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var statusCodePage = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        if (exceptionHandler is null && statusCodePage is null)
        {
            // Someone's trying to navigate directly to the error page (that's fine, but don't return 200)
            Response.StatusCode = StatusCodes.Status418ImATeapot;
        }

        if (exceptionHandler?.Error is ElasticsearchException { IsConnectionError: true })
        {
            // Elasticsearch is down
            Response.StatusCode = statusCode = StatusCodes.Status503ServiceUnavailable;
        }

        ErrorViewModel model = new()
        {
            StatusCode = statusCode,
            Exception = exceptionHandler?.Error
        };

        return View(model);
    }
}
