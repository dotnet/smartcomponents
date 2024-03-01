// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestBlazorApp;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace SmartComponents.E2ETest.Blazor;

public class BlazorSmartTextAreaInlineTest : SmartTextAreaInlineTest<Program>
{
    public BlazorSmartTextAreaInlineTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public async Task WorksWithInteractiveBinding(string hostPlatform)
    {
        await Page.GotoAsync(Server.Address + $"/smarttextarea/{hostPlatform}");
        await Expect(Page.Locator("#is-interactive")).ToHaveTextAsync("True", new() { Timeout = 30000 });

        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "itively sweltering! ");

        // Accept it, and wait until another suggestion appears
        await textArea.PressAsync("Tab");
        await AssertIsShowingSuggestionAsync(50, "I hope you're staying cool out there! ");

        // See the binding is updated only after the change event, and it doesn't include the suggestion we didn't yet accept
        await Task.Delay(500); // ... even if we wait a bit, it's still not updated
        await Expect(Page.Locator("#bound-text")).ToHaveTextAsync("It's 35 degrees C\n\nNext, sport.");
        await textArea.BlurAsync();
        await Expect(Page.Locator("#bound-text")).ToHaveTextAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
    }
}

public class BlazorSmartTextAreaOverlayTest : SmartTextAreaOverlayTest<Program>
{
    public BlazorSmartTextAreaOverlayTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public async Task WorksWithInteractiveBinding(string hostPlatform)
    {
        await Page.GotoAsync(Server.Address + $"/smarttextarea/{hostPlatform}");
        await Expect(Page.Locator("#is-interactive")).ToHaveTextAsync("True", new() { Timeout = 30000 });

        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // Accept it, and wait until another suggestion appears
        await textArea.PressAsync("Tab");
        await AssertIsShowingSuggestionAsync(50, "", "I hope you're staying cool out there! ");

        // See the binding is updated only after the change event, and it doesn't include the suggestion we didn't yet accept
        await Task.Delay(500); // ... even if we wait a bit, it's still not updated
        await Expect(Page.Locator("#bound-text")).ToHaveTextAsync("It's 35 degrees C\n\nNext, sport.");
        await textArea.BlurAsync();
        await Expect(Page.Locator("#bound-text")).ToHaveTextAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
    }
}
