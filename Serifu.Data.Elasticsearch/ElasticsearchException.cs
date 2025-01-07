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

using Elastic.Transport;
using System.Text;

namespace Serifu.Data.Elasticsearch;

public class ElasticsearchException : Exception
{
    internal ElasticsearchException(TransportException ex)
        : this(ex, UnwrapTransportException(ex))
    { }

    private ElasticsearchException(TransportException ex, (string Message, Exception? InnerException) unwrapped)
        : base(unwrapped.Message, unwrapped.InnerException)
    {
        ApiCallDetails = ex.ApiCallDetails;
    }

    protected ElasticsearchException(string message, Exception? innerException = null) : base(message, innerException)
    { }

    public ApiCallDetails? ApiCallDetails { get; }

    public bool IsConnectionError =>
        InnerException is HttpRequestException { HttpRequestError: > HttpRequestError.Unknown };

    private static (string Message, Exception? InnerException) UnwrapTransportException(TransportException ex)
    {
        // Regular error
        if (ex.ApiCallDetails is { HasSuccessfulStatusCode: false, ResponseBodyInBytes: { Length: > 0 } bytes })
        {
            string body = Encoding.UTF8.GetString(bytes);

            return ($"Elasticsearch failed with status code {ex.ApiCallDetails.HttpStatusCode}:\n\n{body}", null);
        }

        // Connection error
        if (ex.InnerException is HttpRequestException { HttpRequestError: > HttpRequestError.Unknown } inner)
        {
            return ("Cannot connect to Elasticsearch.", inner);
        }

        // Unknown error
        return ("Elasticsearch threw an unexpected error.", ex);
    }
}
