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
/// Encapsulates the Python transformers interop and provides methods for running machine learning tasks.
/// </summary>
public interface ITransformersContext : IDisposable
{
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
