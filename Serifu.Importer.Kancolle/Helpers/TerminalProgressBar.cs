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

namespace Serifu.Importer.Kancolle.Helpers;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Intended to be wrapped in a using.")]
internal class TerminalProgressBar : IDisposable
{
    // https://learn.microsoft.com/en-us/windows/terminal/tutorials/progress-bar-sequences

    public void SetProgress(int value, int maxValue)
        => SetProgress((float)value / maxValue);

    public void SetProgress(float value)
    {
        int progress = Math.Clamp((int)Math.Round(value * 100), 0, 100);
        Console.Write($"\x1b]9;4;1;{progress}\x07");
    }

    public void ClearProgress()
    {
        Console.Write("\x1b]9;4;0;0\x07");
    }

    public void Dispose() => ClearProgress();
}
