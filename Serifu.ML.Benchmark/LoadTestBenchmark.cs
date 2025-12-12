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

using BenchmarkDotNet.Attributes;
using Serifu.ML.Abstractions;

using static Serifu.ML.Benchmark.MockData;

namespace Serifu.ML.Benchmark;

/// <summary>
/// Stress-tests the GPU to see how large of a batch it can handle at once, and if performance ever begins to worsen.
/// Tests for both small and large inputs, as the size of the input does seem to have an effect irrespective of question
/// count / batch size.
/// </summary>
internal sealed class LoadTestBenchmark
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
