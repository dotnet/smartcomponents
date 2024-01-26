namespace SmartComponents.StaticAssets.Inference;

public class ChatOptions(string userMessage)
{
    public string UserMessage => userMessage;
    public string? SystemMessage { get; set; }
    public float? Temperature { get; set; }
    public float? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public float? FrequencyPenalty { get; set; }
    public float? PresencePenalty { get; set; }
}
