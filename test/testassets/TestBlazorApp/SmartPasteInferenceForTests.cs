// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using SmartComponents.Inference;
using SmartComponents.StaticAssets.Inference;

namespace E2ETests;

public class SmartPasteInferenceForTests : SmartPasteInference
{
    private readonly DateTime overrideDateForTests = new DateTime(2024, 2, 9);

    public override ChatParameters BuildPrompt(SmartPasteRequestData data)
    {
        var chatParameters = base.BuildPrompt(data);

        // For the E2E tests, we need the "current date" to be a fixed, known value
        // otherwise the captured requests won't match
        var messages = chatParameters.Messages!;
        var firstMessage = messages.First();
        var newFirstMessageText = new Regex("^Current date\\: .*$", RegexOptions.Multiline)
            .Replace(firstMessage.Text, $"Current date: {overrideDateForTests.ToString("D", CultureInfo.InvariantCulture)}");
        if (newFirstMessageText == firstMessage.Text)
        {
            throw new InvalidOperationException($"Could not find \"Current date:\" line in prompt");
        }
        messages.Remove(firstMessage);
        messages.Insert(0, new ChatMessage(firstMessage.Role, newFirstMessageText));

        return chatParameters;
    }
}
