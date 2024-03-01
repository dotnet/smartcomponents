// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using FastBertTokenizer;
using Microsoft.Extensions.ObjectPool;

namespace SmartComponents.LocalEmbeddings;

public partial class LocalEmbedder
{
    private sealed class BertTokenizerPoolPolicy : DefaultPooledObjectPolicy<BertTokenizer>
    {
        private readonly string vocabFilePath;
        private readonly bool caseSensitive;

        public BertTokenizerPoolPolicy(string modelName, bool caseSensitive)
        {
            vocabFilePath = GetFullPathToModelFile(modelName, "vocab.txt");
            this.caseSensitive = caseSensitive;
        }

        public override BertTokenizer Create()
        {
            var tokenizer = new BertTokenizer();
            using var vocabReader = new StreamReader(vocabFilePath);
            tokenizer.LoadVocabulary(vocabReader, convertInputToLowercase: !caseSensitive);
            return tokenizer;
        }

        public override bool Return(BertTokenizer obj)
            => true;
    }
}
