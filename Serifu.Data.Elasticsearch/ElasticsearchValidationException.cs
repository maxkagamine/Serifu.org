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

using System.ComponentModel;
using System.Reflection;

namespace Serifu.Data.Elasticsearch;

public enum ElasticsearchValidationError
{
    // Descriptions here are only for debugging. When adding a new error type, remember to add its localized string in
    // the controller's try-catch.

    [Description("Search query must contain at least two characters or a single kanji.")]
    TooShort,
    [Description("Search query exceeds max length.")]
    TooLong,
    [Description("Only one @-mention is allowed.")]
    MultipleMentions,
}

public class ElasticsearchValidationException : ElasticsearchException
{
    internal ElasticsearchValidationException(ElasticsearchValidationError error)
        : base(GetMessage(error))
    {
        Error = error;
    }

    public ElasticsearchValidationError Error { get; }

    private static string GetMessage(ElasticsearchValidationError error) =>
        typeof(ElasticsearchValidationError)
            .GetField(error.ToString())
            ?.GetCustomAttribute<DescriptionAttribute>()
            ?.Description ?? error.ToString();
}
