namespace Serifu.Importer.Kancolle.Models;
internal record Ship(int ShipNumber, string EnglishName, string JapaneseName)
{
    public override string ToString() => EnglishName;
}