using FastBertTokenizer;
using Microsoft.Extensions.ObjectPool;
using System.IO;

namespace SmartComponents.LocalEmbedding;

public partial class LocalEmbedding
{
    private class BertTokenizerPoolPolicy : DefaultPooledObjectPolicy<BertTokenizer>
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
