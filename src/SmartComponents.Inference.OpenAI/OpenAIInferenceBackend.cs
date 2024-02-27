using System;
using System.Threading.Tasks;
using System.Linq;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;
using SmartComponents.StaticAssets.Inference;
using System.Collections.Generic;

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

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = apiConfig.DeploymentName,
            Temperature = options.Temperature ?? 0f,
            NucleusSamplingFactor = options.TopP ?? 1,
            MaxTokens = options.MaxTokens ?? 200,
            FrequencyPenalty = options.FrequencyPenalty ?? 0,
            PresencePenalty = options.PresencePenalty ?? 0,
        };

        foreach (var message in options.Messages ?? Enumerable.Empty<ChatMessage>())
        {
            chatCompletionsOptions.Messages.Add(message.Role switch
            {
                ChatMessageRole.System => new ChatRequestSystemMessage(message.Text),
                ChatMessageRole.User => new ChatRequestUserMessage(message.Text),
                ChatMessageRole.Assistant => new ChatRequestAssistantMessage(message.Text),
                _ => throw new InvalidOperationException($"Unknown chat message role: {message.Role}")
            });
        }

        if (options.StopSequences is { } stopSequences)
        {
            foreach (var stopSequence in stopSequences)
            {
                chatCompletionsOptions.StopSequences.Add(stopSequence);
            }
        }

        var completionsResponse = await client.GetChatCompletionsAsync(chatCompletionsOptions);

        var response = completionsResponse.Value.Choices.FirstOrDefault()?.Message.Content ?? string.Empty;

        #if DEBUG
        ResponseCache.SetCachedResponse(options, response);
        #endif

        return response;
    }
}
