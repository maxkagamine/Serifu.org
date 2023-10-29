namespace Serifu.Importer.Kancolle.Models;
internal record Ship(
    string EnglishName, string JapaneseName)
{
    public override string ToString() => EnglishName;
}