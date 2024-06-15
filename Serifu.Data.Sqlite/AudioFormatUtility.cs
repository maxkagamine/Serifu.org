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

namespace Serifu.Data.Sqlite;

internal static class AudioFormatUtility
{
    private static readonly byte[] Mpeg1Layer3Header = [0xff, 0xfb];
    private static readonly byte[] Id3Header = [0x49, 0x44, 0x33]; // "ID3"
    private static readonly byte[] OggHeader = [0x4f, 0x67, 0x67, 0x53]; // "OggS"
    private static readonly byte[] VorbisStreamHeader = [0x01, 0x76, 0x6f, 0x72, 0x62, 0x69, 0x73]; // "\x01vorbis"
    private static readonly byte[] OpusStreamHeader = [0x4f, 0x70, 0x75, 0x73, 0x48, 0x65, 0x61, 0x64]; // "OpusHead"

    private static readonly int MaxHeaderLength = new[] { Mpeg1Layer3Header.Length, Id3Header.Length, OggHeader.Length }.Max();
    private static readonly int MaxOggStreamHeaderLength = new[] { VorbisStreamHeader.Length, OpusStreamHeader.Length }.Max();

    private const int OggStreamIndex = 28;

    /// <summary>
    /// Determines the extension for a given file, based on a list of supported audio formats.
    /// </summary>
    /// <param name="file">The file stream.</param>
    /// <returns>The lowercase extension without a leading dot.</returns>
    /// <exception cref="UnsupportedAudioFormatException">File does not contain a supported audio format.</exception>
    /// <exception cref="NotSupportedException">The provided stream is not seekable.</exception>
    public static string GetExtension(Stream file)
    {
        if (file.Length == 0)
        {
            throw new UnsupportedAudioFormatException("File is zero bytes.");
        }

        Span<byte> buffer = stackalloc byte[MaxHeaderLength];
        file.Read(buffer);

        if (buffer[..Mpeg1Layer3Header.Length].SequenceEqual(Mpeg1Layer3Header) ||
            buffer[..Id3Header.Length].SequenceEqual(Id3Header))
        {
            return "mp3";
        } 

        if (buffer[..OggHeader.Length].SequenceEqual(OggHeader))
        {
            buffer = stackalloc byte[MaxOggStreamHeaderLength];
            file.Seek(OggStreamIndex, SeekOrigin.Begin);
            file.Read(buffer);

            if (buffer[..VorbisStreamHeader.Length].SequenceEqual(VorbisStreamHeader))
            {
                return "ogg";
            }

            if (buffer[..OpusStreamHeader.Length].SequenceEqual(OpusStreamHeader))
            {
                return "opus";
            }

            throw new UnsupportedAudioFormatException("File contains an unsupported Ogg stream.");
        }

        throw new UnsupportedAudioFormatException("File contains an unsupported audio format.");
    }
}

public class UnsupportedAudioFormatException(string message) : Exception(message)
{ }
