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
