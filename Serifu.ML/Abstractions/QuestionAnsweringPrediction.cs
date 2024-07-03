namespace Serifu.ML.Abstractions;

/// <summary>
/// The <see cref="QuestionAnsweringPipeline"/>'s answer to a question.
/// </summary>
/// <remarks>
/// Mirrors <see href="https://huggingface.co/docs/transformers/main/en/main_classes/pipelines#transformers.QuestionAnsweringPipeline.__call__"/>.
/// </remarks>
/// <param name="Score">The probability associated to the answer.</param>
/// <param name="Start">The character start index of the answer.</param>
/// <param name="End">The character end index of the answer.</param>
/// <param name="Answer">The answer to the question.</param>
public record QuestionAnsweringPrediction(double Score, int Start, int End, string Answer);