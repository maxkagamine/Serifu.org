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

using MeCab;
using MeCab.Extension.IpaDic;
using Serifu.ML.Abstractions;
using Ve.DotNet;

namespace Serifu.ML.Tokenizers;

public sealed class JapaneseTokenizer : ITokenizer, IDisposable
{
    private readonly MeCabTagger mecab = MeCabTagger.Create();

    public IEnumerable<Token> Tokenize(string text)
    {
        List<Token> tokens = [];

        IEnumerable<MeCabNode> nodes = mecab.ParseToNodes(text);
        IEnumerable<VeWord> words = nodes.ParseVeWords();

        int cursor = 0;

        foreach (VeWord word in words)
        {
            if (word.PartOfSpeech == PartOfSpeech.記号) // Symbols
            {
                continue;
            }

            int start = text.IndexOf(word.Word, cursor);
            int end = start + word.Word.Length;

            tokens.Add(new(start, end));

            cursor = end;
        }

        return tokens;
    }

    public void Dispose() => mecab.Dispose();
}
