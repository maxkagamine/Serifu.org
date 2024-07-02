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

using Moq;
using Serifu.Data;
using Serifu.ML.Abstractions;
using Serilog;

namespace Serifu.ML.Tests;

public class WordAlignerTests
{
    private readonly Mock<ITransformersContext> transformers;
    private readonly WordAligner aligner;

    public WordAlignerTests()
    {
        transformers = new Mock<ITransformersContext>(MockBehavior.Strict);
        aligner = new WordAligner(transformers.Object, new LoggerConfiguration().CreateLogger());
    }

    // Test cases generated by running a random selection of Zuihou quotes through align.py, with and without
    // --no-simplify. test-quotes.tsv contains quotes exported from Kibana and converted to TSV (two columns:
    // english.text and japanese.text):
    //
    // $ while IFS=$'\t' read -r en ja; do ./align.py --from-language en --to-language ja --from-text "$en" --to-text "$ja" --symmetric --symmetric-mode OR; done <test-quotes.tsv | tee test-quotes_aligned_simplified.txt
    // $ while IFS=$'\t' read -r en ja; do ./align.py --from-language en --to-language ja --from-text "$en" --to-text "$ja" --symmetric --symmetric-mode OR --no-simplify; done <test-quotes.tsv" | tee test-quotes_aligned_raw.txt
    // $ paste test-quotes_aligned_raw.txt test-quotes_aligned_simplified.txt | head -n10 | awk '{ print "    [InlineData(\n        \"" $1 "\",\n        \"" $2 "\"\n    )]" }' > test-quotes_inlinedata.txt
    //
    // Note that there are many possible, valid simplifications of the same input, and the algorithm used may not even
    // necessarily produce the shortest possible result (see comments in simplify.py and simplify_slow.py). This only
    // tests that our implementation matches the Python code; correctness needs to be confirmed using the visualizer.
    //
    // See https://github.com/maxkagamine/word-alignment-demo
    [Theory]
    [InlineData(
        "0,4,3,4,0,4,4,5,5,7,3,4,5,7,4,5,12,19,0,2,19,20,5,6,26,29,13,15,26,29,15,17,26,29,17,18,36,39,6,8,36,39,8,9,40,44,6,8,40,44,8,9,45,55,10,12,56,61,13,15,56,61,15,17,56,61,17,18,61,62,18,19,63,65,19,20,65,66,20,21,78,84,28,29,78,84,29,31,85,92,26,28,93,103,26,28,106,107,31,32,108,110,32,34,110,111,34,35,112,116,35,37,117,124,37,39,125,127,39,41,127,128,41,42,129,131,42,44,131,132,44,45,133,136,46,47,137,139,45,46,139,140,47,48,141,143,48,49,141,143,49,50,141,143,50,51,153,157,52,54,153,157,54,57,158,164,52,54,158,164,54,57,167,169,52,54,167,169,54,57,171,173,62,64,171,173,64,65,174,179,52,54,174,179,54,57,179,182,65,66,182,185,62,64,182,185,64,65,185,186,68,69,12,19,0,2,0,4,3,4,5,7,3,4,0,4,4,5,5,7,4,5,19,20,5,6,36,39,6,8,40,44,6,8,36,39,8,9,40,44,8,9,45,55,10,12,56,61,13,15,26,29,15,17,56,61,17,18,61,62,18,19,63,65,19,20,65,66,20,21,93,103,24,25,85,92,26,28,71,75,28,29,56,61,29,31,106,107,31,32,108,110,32,34,110,111,34,35,112,116,35,37,117,124,37,39,125,127,39,41,127,128,41,42,129,131,42,44,131,132,44,45,137,139,45,46,133,136,46,47,139,140,47,48,141,143,48,49,141,143,49,50,143,146,49,50,147,149,49,50,141,143,50,51,143,146,50,51,147,149,50,51,171,173,51,52,173,174,51,52,174,179,51,52,153,157,52,54,158,164,52,54,158,164,54,57,149,152,57,58,141,143,58,59,143,146,58,59,141,143,59,60,143,146,59,60,147,149,59,60,141,143,60,61,143,146,60,61,147,149,60,61,171,173,61,62,173,174,61,62,174,179,61,62,171,173,62,64,173,174,62,64,174,179,62,64,171,173,65,66,173,174,65,66,174,179,65,66,171,173,66,68,173,174,66,68,174,179,66,68,179,182,66,68,182,185,66,68,185,186,68,69,185,186,69,70",
        "0,7,3,5,12,19,0,2,19,20,5,6,26,29,13,18,36,44,6,9,45,55,10,12,56,61,13,18,56,61,29,31,61,62,18,19,63,65,19,20,65,66,20,21,71,75,28,29,78,84,28,31,85,103,26,28,93,103,24,25,106,107,31,32,108,110,32,34,110,111,34,35,112,116,35,37,117,124,37,39,125,127,39,41,127,128,41,42,129,131,42,44,131,132,44,45,133,136,46,47,137,139,45,46,139,140,47,48,141,143,48,51,141,146,58,61,143,149,49,51,147,149,59,61,149,152,57,58,153,164,52,57,167,169,52,57,171,173,61,68,171,179,51,52,173,179,61,64,173,182,65,68,174,179,52,57,182,185,62,65,182,185,66,68,185,186,68,70"
    )]
    [InlineData(
        "0,4,0,3,4,7,0,3,10,13,4,6,10,13,6,7,10,13,7,10,10,13,10,11,14,18,4,6,14,18,6,7,14,18,7,10,14,18,10,11,18,19,11,12,20,23,16,17,20,23,17,18,20,23,18,19,20,23,19,21,20,23,21,22,20,23,22,23,24,25,16,17,24,25,17,18,24,25,18,19,24,25,19,21,24,25,21,22,24,25,22,23,26,30,16,17,26,30,17,18,26,30,18,19,26,30,19,21,26,30,21,22,26,30,22,23,33,36,16,17,33,36,17,18,33,36,18,19,33,36,19,21,33,36,21,22,33,36,22,23,42,46,14,16,47,51,14,16,51,52,25,26,0,4,0,3,4,7,0,3,0,4,3,4,4,7,3,4,10,13,4,6,14,18,4,6,10,13,6,7,10,13,7,10,10,13,10,11,14,18,10,11,18,19,11,12,33,36,12,14,42,46,14,16,47,51,14,16,26,30,16,17,26,30,17,18,31,32,17,18,33,36,17,18,37,39,17,18,40,41,17,18,42,46,17,18,47,51,17,18,20,23,19,21,20,23,21,22,20,23,22,23,51,52,25,26",
        "0,7,0,4,10,18,4,11,18,19,11,12,20,30,16,23,31,51,17,18,33,36,12,14,33,36,16,17,33,36,18,23,42,51,14,16,51,52,25,26"
    )]
    [InlineData(
        "8,11,2,3,8,11,3,4,8,11,4,5,12,14,2,3,12,14,3,4,12,14,4,5,14,15,5,6,16,18,27,28,16,18,28,30,18,21,5,6,22,25,6,7,22,25,7,8,22,25,8,9,22,25,9,10,33,36,16,17,33,36,17,19,33,36,19,20,33,36,20,21,33,36,21,23,33,36,23,25,33,36,25,26,40,44,16,17,40,44,17,19,40,44,19,20,40,44,20,21,40,44,21,23,40,44,23,25,40,44,25,26,45,49,15,16,50,56,11,15,56,57,30,31,8,11,0,2,12,14,0,2,8,11,2,3,12,14,2,3,8,11,3,4,12,14,3,4,8,11,4,5,12,14,4,5,14,15,5,6,16,18,6,7,18,21,6,7,16,18,7,8,18,21,7,8,22,25,8,9,22,25,9,10,50,56,11,15,45,49,15,16,40,44,16,17,40,44,17,19,40,44,19,20,31,33,21,23,33,36,21,23,31,33,23,25,33,36,23,25,37,39,25,26,8,11,27,28,12,14,27,28,56,57,30,31",
        "8,14,0,5,8,14,27,28,14,15,5,6,16,18,6,8,16,18,27,30,18,21,5,8,22,25,6,10,31,33,21,25,33,36,16,26,37,44,25,26,40,44,16,25,45,49,15,16,50,56,11,15,56,57,30,31"
    )]
    [InlineData(
        "0,5,0,2,0,5,2,3,0,5,3,4,0,5,4,5,6,10,0,2,6,10,2,3,6,10,3,4,6,10,4,5,10,11,5,6,6,10,0,2,6,10,2,3,0,5,3,4,6,10,3,4,0,5,4,5,6,10,4,5,10,11,5,6",
        "0,10,0,5,10,11,5,6"
    )]
    [InlineData(
        "1,3,2,4,4,10,0,2,10,11,4,5,13,16,10,11,13,16,11,14,13,16,14,16,13,16,16,17,17,23,10,11,17,23,11,14,17,23,14,16,17,23,16,17,27,32,5,6,27,32,6,8,33,40,8,10,40,41,17,18,43,46,27,30,43,46,30,32,47,49,27,30,47,49,30,32,53,57,18,22,53,57,22,23,57,58,23,24,59,63,24,27,64,68,24,27,68,69,32,33,4,10,0,2,0,1,2,4,1,3,2,4,10,11,4,5,27,32,5,6,27,32,6,8,33,40,8,10,17,23,10,11,17,23,11,14,17,23,14,16,17,23,16,17,40,41,17,18,59,63,24,27,64,68,24,27,42,43,27,30,43,46,27,30,47,49,27,30,42,43,30,32,43,46,30,32,47,49,30,32,68,69,32,33",
        "0,3,2,4,4,10,0,2,10,11,4,5,13,23,10,17,27,32,5,8,33,40,8,10,40,41,17,18,42,49,27,32,53,57,18,23,57,58,23,24,59,68,24,27,68,69,32,33"
    )]
    [InlineData(
        "3,8,0,2,12,16,3,5,17,22,3,5,17,22,5,7,17,22,7,8,23,26,3,5,23,26,5,7,23,26,7,8,26,27,8,9,30,32,9,12,30,32,12,15,30,32,15,17,30,32,17,18,30,32,18,19,33,37,9,12,33,37,12,15,33,37,15,17,33,37,17,18,33,37,18,19,38,42,9,12,38,42,12,15,38,42,15,17,38,42,17,18,38,42,18,19,45,47,9,12,45,47,12,15,45,47,15,17,45,47,17,18,45,47,18,19,47,50,9,12,47,50,12,15,47,50,15,17,47,50,17,18,47,50,18,19,51,54,9,12,51,54,12,15,51,54,15,17,51,54,17,18,51,54,18,19,55,58,9,12,55,58,12,15,55,58,15,17,55,58,17,18,55,58,18,19,58,59,19,20,3,8,0,2,9,11,2,3,12,16,3,5,17,22,5,7,23,26,5,7,17,22,7,8,23,26,7,8,26,27,8,9,51,54,9,12,55,58,9,12,45,47,12,15,47,50,12,15,33,37,15,17,38,42,17,18,58,59,19,20",
        "3,8,0,2,9,11,2,3,12,26,3,5,17,26,5,8,26,27,8,9,30,42,9,19,45,58,9,19,58,59,19,20"
    )]
    [InlineData(
        "1,3,9,11,8,15,0,1,8,15,1,2,15,16,0,1,15,16,1,2,16,21,2,3,22,27,3,4,22,27,4,6,28,35,3,4,28,35,4,6,35,36,6,7,37,43,7,9,43,44,11,12,47,50,26,27,47,50,27,28,51,61,13,15,51,61,15,16,62,69,24,26,70,72,21,22,70,72,22,23,70,72,23,24,75,79,16,18,79,80,16,18,80,85,16,18,86,91,18,20,86,91,20,21,91,92,28,29,93,97,29,30,93,97,30,31,100,109,31,33,100,109,33,35,110,116,33,35,116,117,35,36,118,122,35,36,125,128,48,49,125,128,49,51,125,128,51,52,129,136,36,38,129,136,38,39,129,136,39,40,137,146,46,48,147,149,43,44,147,149,44,45,147,149,45,46,152,157,40,41,152,157,41,43,158,165,40,41,158,165,41,43,165,166,52,53,169,172,56,57,169,172,57,60,169,172,60,61,169,172,61,64,173,177,54,56,173,177,56,57,173,177,57,60,173,177,60,61,173,177,61,64,180,185,54,56,180,185,56,57,180,185,57,60,180,185,60,61,180,185,61,64,186,190,56,57,186,190,57,60,186,190,60,61,186,190,61,64,191,194,56,57,191,194,57,60,191,194,60,61,191,194,61,64,197,203,75,77,197,203,77,78,197,203,78,80,197,203,80,81,204,206,73,75,211,215,69,71,211,215,71,72,216,220,72,73,228,232,65,67,233,238,67,69,238,239,81,82,8,15,0,1,15,16,0,1,16,21,0,1,8,15,1,2,15,16,1,2,16,21,1,2,8,15,2,3,15,16,2,3,16,21,2,3,22,27,3,4,28,35,4,6,35,36,6,7,37,43,7,9,1,3,9,11,43,44,11,12,51,61,13,15,51,61,15,16,75,79,16,18,79,80,16,18,80,85,16,18,86,91,18,20,86,91,20,21,70,72,21,22,70,72,22,23,70,72,23,24,62,69,24,26,47,50,26,27,47,50,27,28,91,92,28,29,93,97,29,30,93,97,30,31,100,109,31,33,110,116,33,35,116,117,35,36,129,136,36,38,129,136,38,39,129,136,39,40,152,157,40,41,158,165,41,43,147,149,43,44,147,149,44,45,147,149,45,46,137,146,46,48,125,128,48,49,125,128,49,51,125,128,51,52,165,166,52,53,180,185,54,56,180,185,56,57,186,190,57,60,169,172,60,61,173,177,60,61,169,172,61,64,173,177,61,64,228,232,65,67,233,238,65,67,228,232,67,69,233,238,67,69,211,215,69,71,211,215,71,72,216,220,72,73,204,206,73,75,197,203,75,77,197,203,77,78,197,203,78,80,197,203,80,81,238,239,81,82",
        "1,3,9,11,8,21,0,3,22,35,3,6,35,36,6,7,37,43,7,9,43,44,11,12,47,50,26,28,51,61,13,16,62,69,24,26,70,72,21,24,75,85,16,18,86,91,18,21,91,92,28,29,93,97,29,31,100,109,31,35,110,116,33,35,116,122,35,36,125,128,48,52,129,136,36,40,137,146,46,48,147,149,43,46,152,165,40,43,165,166,52,53,169,172,56,64,173,177,54,64,180,185,54,64,186,194,56,64,197,203,75,81,204,206,73,75,211,215,69,72,216,220,72,73,228,238,65,69,238,239,81,82"
    )]
    [InlineData(
        "0,2,0,1,0,2,1,2,2,5,0,1,2,5,1,2,2,5,2,3,2,5,3,6,5,11,0,1,5,11,1,2,5,11,2,3,5,11,3,6,11,12,6,7,15,21,18,20,15,21,20,22,15,21,22,23,15,21,23,24,15,21,24,25,22,24,18,20,22,24,20,22,22,24,22,23,22,24,23,24,22,24,24,25,25,29,7,11,25,29,11,13,30,40,13,15,41,43,18,20,41,43,20,22,41,43,22,23,41,43,23,24,41,43,24,25,44,46,16,17,44,46,17,18,44,46,18,20,51,54,16,17,51,54,17,18,51,54,18,20,54,55,25,26,5,11,0,1,5,11,1,2,0,2,2,3,2,5,2,3,5,11,2,3,5,11,3,6,11,12,6,7,5,11,7,11,25,29,11,13,30,40,13,15,51,54,16,17,51,54,17,18,51,54,18,20,41,43,20,22,22,24,23,24,22,24,24,25,54,55,25,26",
        "0,2,0,3,2,11,0,6,5,11,7,11,11,12,6,7,15,24,18,25,25,29,7,13,30,40,13,15,41,43,18,25,44,46,16,20,51,54,16,20,54,55,25,26"
    )]
    [InlineData(
        "1,3,13,15,1,3,15,16,4,8,13,15,4,8,15,16,9,13,9,10,9,13,10,11,9,13,11,12,17,23,3,5,24,29,3,5,24,29,5,6,30,34,9,10,30,34,10,11,30,34,11,12,43,53,7,9,54,58,24,27,54,58,27,28,58,59,16,17,62,65,30,33,62,65,33,34,62,65,34,35,66,70,28,30,79,83,28,30,84,88,24,27,84,88,27,28,89,91,24,27,89,91,27,28,93,95,20,21,93,95,21,23,98,103,17,18,104,111,18,20,111,112,35,36,17,23,3,5,24,29,5,6,43,53,7,9,30,34,9,10,35,39,9,10,30,34,10,11,35,39,10,11,9,13,11,12,4,8,13,15,4,8,15,16,58,59,16,17,98,103,17,18,104,111,18,20,93,95,20,21,93,95,21,23,84,88,24,27,89,91,24,27,84,88,27,28,89,91,27,28,66,70,28,30,71,72,28,30,73,78,28,30,79,83,28,30,62,65,30,33,111,112,35,36",
        "1,8,13,16,9,13,9,12,17,29,3,5,24,29,5,6,30,34,9,12,35,39,9,11,43,53,7,9,54,58,24,28,58,59,16,17,62,65,30,35,66,83,28,30,84,91,24,28,93,95,20,23,98,103,17,18,104,111,18,20,111,112,35,36"
    )]
    [InlineData(
        "2,5,7,9,2,5,9,11,6,12,7,9,6,12,9,11,18,20,0,3,18,20,3,6,21,25,0,3,21,25,3,6,26,31,0,3,26,31,3,6,31,32,11,12,18,20,0,3,21,25,0,3,26,31,0,3,18,20,3,6,21,25,3,6,26,31,3,6,6,12,7,9,2,5,9,11,31,32,11,12",
        "2,12,7,11,18,31,0,6,31,32,11,12"
    )]
    public void SimplifiesAlignments(string inputStr, string expectedStr)
    {
        Alignment[] input = StringToAlignments(inputStr);
        Alignment[] expected = StringToAlignments(expectedStr);

        var actual = aligner.SimplifyAlignments(input);

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// align.py outputs alignments as a list of integers, where every group of four elements corresponds to the
    /// parameters of our <see cref="Alignment"/> struct.
    /// </summary>
    private static Alignment[] StringToAlignments(string str) =>
        str.Split(',').Select(ushort.Parse).Chunk(4).Select(x => new Alignment(x[0], x[1], x[2], x[3])).ToArray();
}
