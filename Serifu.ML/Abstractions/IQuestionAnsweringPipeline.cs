namespace Serifu.ML.Abstractions;

/// <summary>
/// Pipeline handling the question-answering task.
/// </summary>
/// <remarks>
/// Mirrors <see href="https://huggingface.co/docs/transformers/v4.42.0/en/main_classes/pipelines#transformers.QuestionAnsweringPipeline"/>.
/// </remarks>
public interface IQuestionAnsweringPipeline
{
    /// <summary>
    /// Answers the <paramref name="questions"/> given the <paramref name="context"/>.
    /// </summary>
    /// <remarks>
    /// Mirrors <see href="https://huggingface.co/docs/transformers/main/en/main_classes/pipelines#transformers.QuestionAnsweringPipeline.__call__"/>.
    /// </remarks>
    /// <param name="questions">Questions to ask given the <paramref name="context"/>.</param>
    /// <param name="context">The context for the given <paramref name="questions"/>.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>An array of <see cref="QuestionAnsweringPrediction"/> corresponding to the <paramref
    /// name="questions"/>.</returns>
    Task<QuestionAnsweringPrediction[]> Pipe(string[] questions, string context, CancellationToken cancellationToken = default);
}
