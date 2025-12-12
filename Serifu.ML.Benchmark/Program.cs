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

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Serifu.ML;
using Serifu.ML.Abstractions;
using Serifu.ML.Benchmark;
using Serilog;

internal sealed class Program
{
    public static readonly TransformersContext transformers = new(new LoggerConfiguration().CreateLogger());
    private static readonly Dictionary<int, IQuestionAnsweringPipeline> pipelines = [];

    public static IQuestionAnsweringPipeline GetPipeline(int batchSize)
    {
        if (!pipelines.TryGetValue(batchSize, out var pipeline))
        {
            pipeline = transformers.QuestionAnswering("qiyuw/WSPAlign-ft-kftt", batchSize).GetAwaiter().GetResult();
            pipelines.Add(batchSize, pipeline);
        }

        return pipeline;
    }

    private static void Main()
    {
        //Console.WriteLine($"""
        //    Short sentences
        //        Total questions = {EnglishQuestions.Length + JapaneseQuestions.Length}
        //        Max batch size = {Math.Max(EnglishQuestions.Length, JapaneseQuestions.Length)}

        //    Long sentences
        //        Total questions = {LongEnglishQuestions.Length + LongJapaneseQuestions.Length}
        //        Max batch size = {Math.Max(LongEnglishQuestions.Length, LongJapaneseQuestions.Length)}
        //    """);

        try
        {
            BenchmarkRunner.Run<BatchSizeBenchmark>(DefaultConfig.Instance.AddJob(
                Job.MediumRun
                    .WithLaunchCount(1)
                    //.WithToolchain(InProcessEmitToolchain.Instance)
                    .DontEnforcePowerPlan()));

            BenchmarkRunner.Run<LoadTestBenchmark>(DefaultConfig.Instance.AddJob(
                Job.MediumRun
                    .WithLaunchCount(1)
                    //.WithToolchain(InProcessEmitToolchain.Instance)
                    .DontEnforcePowerPlan()));
        }
        finally
        {
            transformers.Dispose();
        }
    }
}
