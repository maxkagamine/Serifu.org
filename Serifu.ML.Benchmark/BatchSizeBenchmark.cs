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
/// Simulates symmetric word alignment of short and long sentences with varying batch sizes.
/// </summary>
internal sealed class BatchSizeBenchmark
{
    [Params(1, 2, 4, 8, 16, 32, 64, 83)]
    public int BatchSize { get; set; }

    private IQuestionAnsweringPipeline? pipeline;

    [GlobalSetup]
    public void Setup()
    {
        pipeline = Program.GetPipeline(BatchSize);
    }

    [Benchmark]
    public async Task ShortSentencesSymmetric()
    {
        // Total questions: 53, max batch size: 29

        await pipeline!.Pipe(EnglishQuestions, JapaneseContext);
        await pipeline!.Pipe(JapaneseQuestions, EnglishContext);
    }

    [Benchmark]
    public async Task LongSentencesSymmetric()
    {
        // Total questions: 165, max batch size: 83

        await pipeline!.Pipe(LongEnglishQuestions, LongJapaneseContext);
        await pipeline!.Pipe(LongJapaneseQuestions, LongEnglishContext);
    }
}
