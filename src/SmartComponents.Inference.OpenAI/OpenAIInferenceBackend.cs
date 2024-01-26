using System;
using System.Threading.Tasks;
using System.Linq;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using SmartComponents.StaticAssets.Inference;

namespace SmartComponents.Inference.OpenAI;

public class OpenAIInferenceBackend(IConfiguration configuration)
    : IInferenceBackend
{

    public async Task<string> GetChatResponseAsync(ChatOptions options)
    {
        #if DEBUG
        if (ResponseCache.TryGetCachedResponse(options, out var cachedResponse))
        {
            return cachedResponse!;
        }
        #endif

        var apiConfig = new ApiConfig(configuration);
        var client = new OpenAIClient(
            new Uri(apiConfig.Endpoint), // TODO: Don't assume it's Azure OpenAI
            new AzureKeyCredential(apiConfig.ApiKey));

        var completionsResponse = await client.GetChatCompletionsAsync(new ChatCompletionsOptions
        {
            DeploymentName = apiConfig.DeploymentName,
            Messages = {
                new ChatRequestSystemMessage(options.SystemMessage ?? "You are a helpful AI assistant"),
                new ChatRequestUserMessage(options.UserMessage),
            },
            Temperature = options.Temperature ?? 0f,
            NucleusSamplingFactor = options.TopP ?? 1,
            MaxTokens = options.MaxTokens ?? 200,
            FrequencyPenalty = options.FrequencyPenalty ?? 0,
            PresencePenalty = options.PresencePenalty ?? 0,
        });

        var response = completionsResponse.Value.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

        #if DEBUG
        ResponseCache.SetCachedResponse(options, response);
        #endif

        return response;
    }
}
