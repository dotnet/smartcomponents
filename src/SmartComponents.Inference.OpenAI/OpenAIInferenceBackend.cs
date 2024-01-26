using SmartComponents.StaticAssets.Inference;
using System;
using System.Threading.Tasks;

namespace SmartComponents.OpenAI;

public class OpenAIInferenceBackend : IInferenceBackend
{
    public Task<string> GetChatResponseAsync(ChatOptions options)
    {
        return Task.FromResult("TODO: Actually call OpenAI");
    }
}
