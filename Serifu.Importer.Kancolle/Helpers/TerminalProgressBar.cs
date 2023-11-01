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
