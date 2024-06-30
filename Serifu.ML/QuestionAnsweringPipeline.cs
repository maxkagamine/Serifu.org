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
        IEnumerable<string> questions,
        string context,
        CancellationToken cancellationToken = default) => await ctx.Run(() =>
        {
            List<QuestionAnsweringPrediction> predictions = new(questions.Count());

            foreach (dynamic prediction in pipe(question: questions, context: context))
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
