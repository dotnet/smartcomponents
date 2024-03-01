// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SmartComponents.Infrastructure;
using SmartComponents.StaticAssets.Inference;

namespace SmartComponents.Inference;

public class SmartTextAreaInference
{
    public virtual ChatParameters BuildPrompt(SmartTextAreaConfig config, string textBefore, string textAfter)
    {
        var systemMessageBuilder = new StringBuilder();
        systemMessageBuilder.Append(@"Predict what text the user in the given ROLE would insert at the cursor position indicated by ^^^.
Only give predictions for which you have an EXTREMELY high confidence that the user would insert that EXACT text.
Do not make up new information. If you're not sure, just reply with NO_PREDICTION.

RULES:
1. Reply with OK:, then in square brackets the predicted text, then END_INSERTION, and no other output.
2. When a specific value or quantity cannot be inferred and would need to be provided, use the word NEED_INFO.
3. If there isn't enough information to predict any words that the user would type next, just reply with the word NO_PREDICTION.
4. NEVER invent new information. If you can't be sure what the user is about to type, ALWAYS stop the prediction with END_INSERTION.");

        if (config.UserPhrases is { Length: > 0 } stockPhrases)
        {
            systemMessageBuilder.Append("\nAlways try to use variations on the following phrases as part of the predictions:\n");
            foreach (var phrase in stockPhrases)
            {
                systemMessageBuilder.AppendFormat("- {0}\n", phrase);
            }
        }

        List<ChatMessage> messages =
        [
            new(ChatMessageRole.System, systemMessageBuilder.ToString()),

            new(ChatMessageRole.User, @"ROLE: Family member sending a text
USER_TEXT: Hey, it's a nice day - the weather is ^^^"),
            new(ChatMessageRole.Assistant, @"OK:[great!]END_INSERTION"),

            new(ChatMessageRole.User, @"ROLE: Customer service assistant
USER_TEXT: You can find more information on^^^

Alternatively, phone us."),
            new(ChatMessageRole.Assistant, @"OK:[ our website at NEED_INFO]END_INSERTION"),

            new(ChatMessageRole.User, @"ROLE: Casual
USER_TEXT: Oh I see!

Well sure thing, we can"),
            new(ChatMessageRole.Assistant, @"OK:[ help you out with that!]END_INSERTION"),

            new(ChatMessageRole.User, @"ROLE: Storyteller
USER_TEXT: Sir Digby Chicken Caesar, also know^^^"),
            new(ChatMessageRole.Assistant, @"OK:[n as NEED_INFO]END_INSERTION"),

            new(ChatMessageRole.User, @"ROLE: Customer support agent
USER_TEXT: Goodbye for now.^^^"),
            new(ChatMessageRole.Assistant, @"NO_PREDICTION END_INSERTION"),

            new(ChatMessageRole.User, @"ROLE: Pirate
USER_TEXT: Have you found^^^"),
            new(ChatMessageRole.Assistant, @"OK:[ the treasure, me hearties?]END_INSERTION"),

            new(ChatMessageRole.User, @$"ROLE: {config.UserRole}
USER_TEXT: {textBefore}^^^{textAfter}"),
        ];

        return new ChatParameters
        {
            Messages = messages,
            Temperature = 0,
            MaxTokens = 400,
            StopSequences = ["END_INSERTION", "NEED_INFO"],
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };
    }

    public virtual async Task<string> GetInsertionSuggestionAsync(IInferenceBackend inference, SmartTextAreaConfig config, string textBefore, string textAfter)
    {
        var chatOptions = BuildPrompt(config, textBefore, textAfter);
        var response = await inference.GetChatResponseAsync(chatOptions);
        if (response.Length > 5 && response.StartsWith("OK:[", StringComparison.Ordinal))
        {
            // Avoid returning multiple sentences as it's unlikely to avoid inventing some new train of thought.
            var trimAfter = response.IndexOfAny(['.', '?', '!']);
            if (trimAfter > 0 && response.Length > trimAfter + 1 && response[trimAfter + 1] == ' ')
            {
                response = response.Substring(0, trimAfter + 1);
            }

            // Leave it up to the frontend code to decide whether to add a training space
            var trimmedResponse = response.Substring(4).TrimEnd(']', ' ');

            // Don't have a leading space on the suggestion if there's already a space right
            // before the cursor. The language model normally gets this right anyway (distinguishing
            // between starting a new word, vs continuing a partly-typed one) but sometimes it adds
            // an unnecessary extra space.
            if (textBefore.Length > 0 && textBefore[textBefore.Length - 1] == ' ')
            {
                trimmedResponse = trimmedResponse.TrimStart(' ');
            }

            return trimmedResponse;
        }

        return string.Empty;
    }
}
