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

using Python.Runtime;
using Serifu.ML.Abstractions;

namespace Serifu.ML;

internal sealed class QuestionAnsweringPipeline : IQuestionAnsweringPipeline
{
    private readonly TransformersContext ctx;
    private readonly dynamic pipe;

    public QuestionAnsweringPipeline(TransformersContext ctx, dynamic pipe)
    {
        this.ctx = ctx;
        this.pipe = pipe;
    }

    public async Task<QuestionAnsweringPrediction[]> Pipe(
        string[] questions, string context,
        CancellationToken cancellationToken = default) => await ctx.Run(() =>
        {
            List<QuestionAnsweringPrediction> predictions = new(questions.Length);

            // Prevent useless warning when reusing a pipeline (it wants you to use a dataset, even when you're passing
            // a list which uses a dataset...)
            // https://github.com/huggingface/transformers/blob/v4.36.2/src/transformers/pipelines/base.py#L1052-L1120
            pipe.call_count = 0;

            dynamic result = pipe(question: questions.ToPyList(), context: context);

            // Return type is inconsistent; if you give it a list that happens to have only one element, it will return
            // just the prediction rather than a list
            // https://github.com/huggingface/transformers/blob/v4.36.2/src/transformers/pipelines/question_answering.py#L392
            if (PyDict.IsDictType(result))
            {
                double score = result["score"];
                int start = result["start"];
                int end = result["end"];
                string answer = result["answer"];

                predictions.Add(new(score, start, end, answer));
            }
            else
            {
                foreach (dynamic prediction in result)
                {
                    double score = prediction["score"];
                    int start = prediction["start"];
                    int end = prediction["end"];
                    string answer = prediction["answer"];

                    predictions.Add(new(score, start, end, answer));
                }
            }

            if (predictions.Count != questions.Length)
            {
                throw new Exception("Number of predictions does not match the number of questions.");
            }

            return predictions.ToArray();
        }, cancellationToken);
}
