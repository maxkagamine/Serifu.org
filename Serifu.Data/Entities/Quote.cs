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

using System.Diagnostics;

namespace Serifu.Data.Entities;

[DebuggerDisplay("Speaker = {SpeakerEnglish,nq}, Context = {Context,nq}, Quote = {QuoteEnglish,nq}")]
public class Quote
{
    public required long Id { get; set; }

    public required Source Source { get; set; }

    public required string SpeakerEnglish { get; set; }

    public required string SpeakerJapanese { get; set; }

    public required string Context { get; set; }

    public required string QuoteEnglish { get; set; }

    public required string QuoteJapanese { get; set; }

    public string Notes { get; set; } = "";

    public string? AudioFile { get; set; }
}
