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

using Serilog;

namespace Serifu.Data.Sqlite;

public class SqliteService : ISqliteService
{
    private readonly SerifuContext db;
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public SqliteService(SerifuContext db, HttpClient httpClient, ILogger logger)
    {
        this.db = db;
        this.httpClient = httpClient;
        this.logger = logger.ForContext<SqliteService>();
    }

    public async Task SaveQuotes(Source source, IEnumerable<Quote> quotes, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string?> GetCachedAudioFile(Uri uri, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<string> ImportAudioFile(Stream stream, Uri? originalUri = null, CancellationToken cancellationToken = default)
    {
        // TODO: Should this method require a seekable stream, since then we can potentially create a Content-Length
        // sized MemoryStream in DownloadAudioFile and leverage TryGetBuffer the way GetByteArrayAsyncCore does? At some
        // point we need to turn the stream into a byte array, but working with streams may be a bit nicer (might even
        // be a good idea to interact with the sqlar table via ADO.NET the way sqlarserver does, and skip the whole EF
        // byte[] mess altogether...)
        // TODO: Remember to add the unsupported audio exception to the xml doc
        throw new NotImplementedException();
    }

    public async Task<string> DownloadAudioFile(string uri, CancellationToken cancellationToken = default)
    {
        // TODO: Remember to add the unsupported audio exception to the xml doc
        throw new NotImplementedException();
    }

    public async Task DeleteOrphanedAudioFiles(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
