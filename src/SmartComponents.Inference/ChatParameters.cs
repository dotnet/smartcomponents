// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartComponents.StaticAssets.Inference;

public class ChatParameters
{
    public IList<ChatMessage>? Messages { get; set; }
    public float? Temperature { get; set; }
    public float? TopP { get; set; }
    public int? MaxTokens { get; set; }
    public float? FrequencyPenalty { get; set; }
    public float? PresencePenalty { get; set; }
    public IList<string>? StopSequences { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RespondJson { get; set; }
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
