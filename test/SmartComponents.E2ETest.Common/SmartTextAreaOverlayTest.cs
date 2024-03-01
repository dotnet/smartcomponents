// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static SmartComponents.E2ETest.Common.Infrastructure.TextAreaAssertions;

namespace SmartComponents.E2ETest.Common;

public class SmartTextAreaOverlayTest<TStartup> : PlaywrightTestBase<TStartup> where TStartup : class
{
    protected ILocator textArea = default!;
    protected ILocator suggestionElement = default!;
    protected ILocator suggestionPrefixElement = default!;
    protected ILocator suggestionTextElement = default!;

    public SmartTextAreaOverlayTest(KestrelWebApplicationFactory<TStartup> server) : base(server)
    {
    }

    protected override async Task OnBrowserReadyAsync()
    {
        await Page.GotoAsync(Server.Address + "/smarttextarea");
        textArea = Page.Locator("textarea#with-overlay-display");
        suggestionElement = Page.Locator("textarea#with-overlay-display + smart-textarea > .smart-textarea-suggestion-overlay");
        suggestionPrefixElement = suggestionElement.Locator("span:nth-child(1)");
        suggestionTextElement = suggestionElement.Locator("span:nth-child(2)");
    }

    [Fact]
    public async Task RendersAsTextareaWithInitialValueAndAttributes()
    {
        await Expect(textArea).ToHaveAttributeAsync("cols", "100");
        await Expect(textArea).ToHaveAttributeAsync("rows", "6");
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C\n\nNext, sport.");
    }

    [Fact]
    public async Task ShowsSuggestionAfterTypingAtTheEndOfALine()
    {
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await AssertSelectionPositionAsync(textArea, 17, 0);
        await Expect(textArea).ToBeFocusedAsync();

        // Does not show suggestion before you start typing
        await Task.Delay(500);
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C\n\nNext, sport.");
        await AssertIsNotShowingSuggestionAsync();

        // Does show suggestion after you type
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's pos\n\nNext, sport.");

        // Suggestion remains visible if you type things that match the suggestion
        // Importantly, the timeout on these assertions is zero, since the suggestion should never have gone away
        await textArea.PressSequentiallyAsync("itive");
        Assert.Equal("It's 35 degrees C - that's positive\n\nNext, sport.", await textArea.InputValueAsync());
        await AssertIsShowingSuggestionAsync(35, "positive", "ly sweltering! ", timeout: 0);
    }

    [Fact]
    public async Task DoesNotShowSuggestionAfterTypingInTheMiddleOfALine()
    {
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        for (var i = 0; i < 10; i++)
        {
            await textArea.PressAsync("ArrowLeft");
        }
        await textArea.PressAsync("Backspace");
        await textArea.PressAsync("Backspace");
        await textArea.PressSequentiallyAsync("2");

        // Even if we wait a while, no suggestion
        await Task.Delay(500);
        await Expect(textArea).ToHaveValueAsync("It's 2 degrees C\n\nNext, sport.");
        await AssertSelectionPositionAsync(textArea, 6, 0);
        await AssertIsNotShowingSuggestionAsync();
    }

    [Fact]
    public async Task CanRejectSuggestionByTyping()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // It goes away if you type something that doesn't match the suggestion
        await textArea.PressSequentiallyAsync("sibly");

        // Unfortunately we can't observe in tests that the suggestion went away, because
        // another suggestion will appear later. So the best we can do is assert about the
        // state after the next suggestion appears.
        await AssertIsShowingSuggestionAsync(35, "", " the hottest day of the year! ");
    }

    [Fact]
    public async Task CanRejectSuggestionByMovingCursor()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // It goes away if you move the cursor
        await textArea.PressAsync("ArrowLeft");
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's pos\n\nNext, sport.");
    }

    [Fact]
    public async Task CanRejectSuggestionByClicking()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // It goes away if you click to move the cursor
        await textArea.ClickAsync(new() { Position = new() { X = 20, Y = 5 } });
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's pos\n\nNext, sport.");
    }

    [Fact]
    public async Task CanRejectSuggestionByScrolling()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // It goes away if you scroll
        await textArea.EvaluateAsync("e => e.dispatchEvent(new CustomEvent('scroll'))");
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's pos\n\nNext, sport.");
    }

    [Fact]
    public async Task CanRejectSuggestionByFocusingOut()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // It goes away if you focus out
        await Page.Locator("h3").ClickAsync();
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's pos\n\nNext, sport.");
    }

    [Fact]
    public async Task CanAcceptSuggestionByPressingTab()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // Accept it
        await textArea.PressAsync("Tab");

        // It would be good if we could make an assertion that the suggestion is gone,
        // but that would lead to flaky tests since another suggestion will appear
        // after another 200-300ms. So the best we can do is assert about the state
        // after the next suggestion appears.
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
        await AssertIsShowingSuggestionAsync(50, "", "I hope you're staying cool out there! ");
    }

    [Fact]
    public async Task CanAcceptSuggestionByClickingIt()
    {
        // Show a suggestion
        await textArea.ClickAsync(new() { Position = new() { X = 200, Y = 5 } });
        await textArea.PressSequentiallyAsync(" - that's pos");
        await AssertIsShowingSuggestionAsync(30, "pos", "itively sweltering! ");

        // Accept it
        await suggestionElement.ClickAsync();
        await AssertIsNotShowingSuggestionAsync();
        await Expect(textArea).ToHaveValueAsync("It's 35 degrees C - that's positively sweltering! \n\nNext, sport.");
        await AssertSelectionPositionAsync(textArea, 50, 0);

        // Does not show another suggestion even if we wait a bit first
        // This differs from when you press tab, because only the suggestion display even knows this happened
        // We could change this behavior but it's not clear that it's worth it
        await Task.Delay(500);
        await AssertIsNotShowingSuggestionAsync();
    }

    protected async Task AssertIsShowingSuggestionAsync(int position, string suggestionPrefix, string suggestionText, float? timeout = null)
    {
        if (timeout == 0)
        {
            Assert.True(await suggestionElement.IsVisibleAsync());
            Assert.Contains("smart-textarea-suggestion-overlay-visible", await suggestionElement.EvaluateAsync<string>("e => e.className"));
            Assert.Equal(suggestionPrefix, await suggestionPrefixElement.TextContentAsync());
            Assert.Equal(suggestionText, await suggestionTextElement.TextContentAsync());
        }
        else
        {
            await Expect(suggestionElement).ToBeVisibleAsync();
            await Expect(suggestionElement).ToHaveClassAsync(new Regex("\\bsmart-textarea-suggestion-overlay-visible\\b"));
            await Expect(suggestionPrefixElement).ToHaveTextAsync(suggestionPrefix, new() { Timeout = timeout });
            await Expect(suggestionTextElement).ToHaveTextAsync(suggestionText, new() { Timeout = timeout });
        }

        await AssertSelectionPositionAsync(textArea, position, 0);
    }

    protected async Task AssertIsNotShowingSuggestionAsync()
    {
        await Expect(suggestionElement).Not.ToBeVisibleAsync();
        await Expect(suggestionElement).Not.ToHaveClassAsync(new Regex("\\bsmart-textarea-suggestion-overlay-visible\\b"));
    }
}
