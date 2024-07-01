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

using Serifu.ML.Abstractions;

namespace Serifu.ML;

internal class QuestionAnsweringPipeline : IQuestionAnsweringPipeline
{
    private readonly TransformersContext ctx;
    private readonly dynamic pipe;

    public QuestionAnsweringPipeline(TransformersContext ctx, dynamic pipe)
    {
        this.ctx = ctx;
        this.pipe = pipe;
    }

    public async Task<IEnumerable<QuestionAnsweringPrediction>> Pipe(
        string[] questions, string context,
        CancellationToken cancellationToken = default) => await ctx.Run(() =>
        {
            List<QuestionAnsweringPrediction> predictions = new(questions.Length);

            // Prevent useless warning when reusing a pipeline (it wants you to use a dataset, even when you're passing
            // a list which uses a dataset...)
            // https://github.com/huggingface/transformers/blob/v4.36.2/src/transformers/pipelines/base.py#L1052-L1120
            pipe.call_count = 0;

            foreach (dynamic prediction in pipe(question: questions.ToPyList(), context: context))
            {
                predictions.Add(new(
                    Score: prediction["score"],
                    Start: prediction["start"],
                    End: prediction["end"],
                    Answer: prediction["answer"]));
            }

            return predictions;
        }, cancellationToken);
}
