namespace Serifu.Importer;
internal interface IProcessor
{
    Task Run(CancellationToken cancellationToken);
}
