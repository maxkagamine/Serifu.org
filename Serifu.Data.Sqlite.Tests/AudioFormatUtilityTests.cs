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

namespace Serifu.Data.Sqlite.Tests;

public sealed class AudioFormatUtilityTests
{
    [Fact]
    public void DetectsMp3WithMpeg1Header()
    {
        // Bits 13 and/or 16 could be zero, indicating MPEG-2 and CRC protection, respectively, but I haven't yet seen
        // this in practice. See http://mpgedit.org/mpgedit/mpeg_format/MP3Format.html
        using var stream = new MemoryStream([0xff, 0xfb]);

        Assert.Equal("mp3", AudioFormatUtility.GetExtension(stream));
    }

    [Fact]
    public void DetectsMp3WithId3Header()
    {
        using var stream = new MemoryStream([0x49, 0x44, 0x33]);

        Assert.Equal("mp3", AudioFormatUtility.GetExtension(stream));
    }

    [Fact]
    public void DetectsOggVorbis()
    {
        using var stream = new MemoryStream([
            0x4F, 0x67, 0x67, 0x53, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2C, 0xE8,
            0xE1, 0x66, 0x00, 0x00, 0x00, 0x00, 0xDE, 0x44, 0xAB, 0x96, 0x01, 0x1E, 0x01, 0x76, 0x6F, 0x72,
            0x62, 0x69, 0x73]);

        Assert.Equal("ogg", AudioFormatUtility.GetExtension(stream));
    }

    [Fact]
    public void DetectsOggOpus()
    {
        using var stream = new MemoryStream([
            0x4F, 0x67, 0x67, 0x53, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE7, 0xEA,
            0xA5, 0xED, 0x00, 0x00, 0x00, 0x00, 0x09, 0x52, 0x0D, 0x11, 0x01, 0x13, 0x4F, 0x70, 0x75, 0x73,
            0x48, 0x65, 0x61, 0x64]);

        Assert.Equal("opus", AudioFormatUtility.GetExtension(stream));
    }

    [Fact]
    public void ThrowsIfUnsupportedFormat()
    {
        using var stream = new MemoryStream([0x00, 0x00, 0x00]);

        Assert.Throws<UnsupportedAudioFormatException>(() => AudioFormatUtility.GetExtension(stream));
    }

    [Fact]
    public void ThrowsIfUnsupportedOggCodec()
    {
        using var stream = new MemoryStream([
            0x4F, 0x67, 0x67, 0x53, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFA, 0x41,
            0xE1, 0x9E, 0x00, 0x00, 0x00, 0x00, 0x37, 0xCD, 0x6D, 0x6E, 0x01, 0x2A, 0x80, 0x74, 0x68, 0x65,
            0x6F, 0x72, 0x61]);

        var ex = Assert.Throws<UnsupportedAudioFormatException>(() => AudioFormatUtility.GetExtension(stream));
        Assert.Contains("unsupported Ogg stream", ex.Message);
    }

    [Fact]
    public void ThrowsIfEmptyFile()
    {
        using var stream = new MemoryStream();

        Assert.Throws<UnsupportedAudioFormatException>(() => AudioFormatUtility.GetExtension(stream));
    }
}
