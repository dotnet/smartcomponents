using System.Collections.Generic;

namespace SmartComponents.StaticAssets.Inference;

public class ChatOptions
{
    public ICollection<ChatMessage>? Messages { get; set; }
    public float? Temperature { get; set; }
    public float? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public float? FrequencyPenalty { get; set; }
    public float? PresencePenalty { get; set; }
    public ICollection<string>? StopSequences { get; set; }
}

public class ChatMessage(ChatMessageRole role, string text)
{
    public ChatMessageRole Role => role;
    public string Text => text;
}

public enum ChatMessageRole
{
    System,
    User,
    Assistant,
}
