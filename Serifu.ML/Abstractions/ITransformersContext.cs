namespace Serifu.ML.Abstractions;

/// <summary>
/// Encapsulates the Python transformers interop and provides methods for running machine learning tasks.
/// </summary>
public interface ITransformersContext : IDisposable
{
    /// <summary>
    /// Gets the name of the GPU or CPU that PyTorch is running on.
    /// </summary>
    string DeviceName { get; }

    /// <summary>
    /// Creates a pipeline for question-answering.
    /// </summary>
    /// <remarks>
    /// Mirrors <see href="https://huggingface.co/docs/transformers/v4.42.0/en/main_classes/pipelines#transformers.QuestionAnsweringPipeline"/>.
    /// </remarks>
    /// <param name="model">The model that will be used by the pipeline to make predictions.</param>
    /// <param name="batchSize">The batch size to use. See <a
    /// href="https://huggingface.co/docs/transformers/main_classes/pipelines#pipeline-batching">Pipeline
    /// batching</a>.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel loading the model.</param>
    Task<IQuestionAnsweringPipeline> QuestionAnswering(string model, int batchSize = 1, CancellationToken cancellationToken = default);
}
