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

using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Serifu.Data.Entities;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class Quote
{
    public required long Id { get; set; }

    public required Source Source { get; set; }

    public TranslationCollection Translations { get; set; } = [];

    public DateTime DateImported { get; set; } = DateTime.Now;

    private string GetDebuggerDisplay()
    {
        if (!Translations.TryGetValue("en", out var tl))
        {
            tl = Translations.FirstOrDefault();
        }

        return $"Speaker = \"{tl?.SpeakerName}\", Context = \"{tl?.Context}\", Text = \"{tl?.Text}\"";
    }
}
