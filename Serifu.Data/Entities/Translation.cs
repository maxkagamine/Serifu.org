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

[DebuggerDisplay("{Language,nq} = {Text}")]
public class Translation
{
    public required string Language { get; set; }

    public required string SpeakerName { get; set; }

    public required string Context { get; set; }

    public required string Text { get; set; }

    public string Notes { get; set; } = "";

    public AudioFile? AudioFile { get; set; }
}
