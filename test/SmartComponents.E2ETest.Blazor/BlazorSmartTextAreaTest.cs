// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestBlazorApp;
using Xunit;
using static Microsoft.Playwright.Assertions;
using static SmartComponents.E2ETest.Common.Infrastructure.TextAreaAssertions;

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

        // Accept it
        await textArea.PressAsync("Tab");
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
        await AssertSelectionPositionAsync(textArea, 50, 0);

        // See the binding is updated only after the change event
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

        // Accept it
        await textArea.PressAsync("Tab");
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
        await AssertSelectionPositionAsync(textArea, 50, 0);

        // See the binding is updated only after the change event
        await Expect(Page.Locator("#bound-text")).ToHaveTextAsync("It's 35 degrees C\n\nNext, sport.");
        await textArea.BlurAsync();
        await Expect(Page.Locator("#bound-text")).ToHaveTextAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
    }
}
