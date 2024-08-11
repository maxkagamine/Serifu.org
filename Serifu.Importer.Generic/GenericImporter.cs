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

using Microsoft.Extensions.Options;
using Serilog;

namespace Serifu.Importer.Generic;

internal class GenericImporter
{
    private readonly IParser parser;
    private readonly ParserOptions options;
    private readonly ILogger logger;

    public GenericImporter(
        IParser parser,
        IOptions<ParserOptions> options,
        ILogger logger)
    {
        this.parser = parser;
        this.options = options.Value;
        this.logger = logger.ForContext<GenericImporter>();
    }

    public Task Run(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
