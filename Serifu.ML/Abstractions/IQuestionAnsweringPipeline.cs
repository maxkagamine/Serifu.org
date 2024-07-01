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
