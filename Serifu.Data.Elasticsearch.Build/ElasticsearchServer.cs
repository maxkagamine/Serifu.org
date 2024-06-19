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

using Elastic.Clients.Elasticsearch;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Serifu.Data.Elasticsearch.Build;

internal partial class ElasticsearchServer : IDisposable
{
    private readonly ElasticsearchClient client;
    private Process? process;

    public ElasticsearchServer(ElasticsearchClient client)
    {
        this.client = client;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        process = Process.Start(new ProcessStartInfo()
        {
            FileName = "/usr/share/elasticsearch/bin/elasticsearch",
            WorkingDirectory = "/usr/share/elasticsearch",
            Environment =
            {
                ["discovery.type"] = "single-node",
                ["xpack.security.enabled"] = "false",
                ["ES_JAVA_OPTS"] = "-Xlog:disable"
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        })!;

        // Need to consume stdout/stderr so execution on the server doesn't block once the buffer is full
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for server to start
        while (true)
        {
            if (process.HasExited)
            {
                throw new Exception("Elasticsearch failed to start.");
            }

            try
            {
                await client.PingAsync(cancellationToken);
                break;
            }
            catch
            {
                await Task.Delay(500, cancellationToken);
            }
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        if (process is null || process.HasExited)
        {
            return;
        }

        // .NET doesn't provide a way to send SIGTERM, so we have to call out to libc to perform a graceful shutdown
        const int SIGTERM = 15;
        const int ESRCH = 3;

        if (Kill(process.Id, SIGTERM) != 0)
        {
            int errno = Marshal.GetLastPInvokeError();

            if (errno != ESRCH) // No such process
            {
                throw new Exception($"Failed to send SIGTERM (errno = {errno})");
            }
        }

        // Wait for server to stop
        while (!process.HasExited)
        {
            await Task.Delay(100, cancellationToken);
        }
    }

    public void Dispose()
    {
        if (process is not null && !process.HasExited)
        {
            // Did not perform a graceful shutdown. Program has failed; send SIGKILL.
            process.Kill(true);
        }
    }

    [LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static partial int Kill(int pid, int sig);
}
