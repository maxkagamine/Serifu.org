using BenchmarkDotNet.Attributes;
using Serifu.ML.Abstractions;

using static Serifu.ML.Benchmark.MockData;

namespace Serifu.ML.Benchmark;

/// <summary>
/// Stress-tests the GPU to see how large of a batch it can handle at once, and if performance ever begins to worsen.
/// Tests for both small and large inputs, as the size of the input does seem to have an effect irrespective of question
/// count / batch size.
/// </summary>
public class LoadTestBenchmark
{
    [Params(1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024)]
    public int QuestionCount { get; set; }

    private IQuestionAnsweringPipeline? pipeline;

    private string[] shortEnglishQuestions = [];
    private string[] longEnglishQuestions = [];

    [GlobalSetup]
    public void Setup()
    {
        pipeline = Program.GetPipeline(QuestionCount);

        shortEnglishQuestions = Enumerable.Range(0, QuestionCount).Select(i => EnglishQuestions[i % EnglishQuestions.Length]).ToArray();
        longEnglishQuestions = Enumerable.Range(0, QuestionCount).Select(i => LongEnglishQuestions[i % LongEnglishQuestions.Length]).ToArray();
    }

    [Benchmark]
    public async Task ShortSentences()
    {
        await pipeline!.Pipe(shortEnglishQuestions, JapaneseContext);
    }

    [Benchmark]
    public async Task LongSentences()
    {
        await pipeline!.Pipe(longEnglishQuestions, LongJapaneseContext);
    }
}
