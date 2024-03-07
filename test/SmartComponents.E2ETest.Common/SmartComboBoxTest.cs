// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Playwright;

namespace SmartComponents.E2ETest.Common;

public class SmartComboBoxTest<TStartup> : PlaywrightTestBase<TStartup> where TStartup : class
{
    public SmartComboBoxTest(KestrelWebApplicationFactory<TStartup> server) : base(server)
    {
    }

    protected override async Task OnBrowserReadyAsync()
    {
        await Page.GotoAsync(Server.Address + "/smartcombobox");
    }

    [Fact]
    public async Task DoesNotShowSuggestionsUntilEditedAndNonempty()
    {
        var input = Page.Locator("#default-params input");
        var suggestions = Page.Locator("#default-params smart-combobox");

        await Expect(input).ToHaveValueAsync("Initial value");
        await input.FocusAsync();
        await Expect(suggestions).ToHaveCSSAsync("display", "none");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.Null(await input.GetAttributeAsync("aria-activedescendant"));

        // Still doesn't show suggestions if you clear the input
        await input.SelectTextAsync();
        await input.PressAsync("Backspace");
        await Task.Delay(500); // Wait for debouncing to complete
        await Expect(suggestions).ToHaveCSSAsync("display", "none");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.Null(await input.GetAttributeAsync("aria-activedescendant"));
    }

    [Fact]
    public async Task ShowsSuggestionsWhenTypingPauses()
    {
        var input = Page.Locator("#default-params input");
        var suggestions = Page.Locator("#default-params smart-combobox");

        // After typing, the suggestions list is shown
        await input.SelectTextAsync();
        await input.PressSequentiallyAsync("transport", new() { Delay = 50 });
        await Expect(suggestions).ToHaveCSSAsync("display", "block");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await AssertNthSuggestionIsActive(input, suggestions, 0);

        // The suggestions list contains the expected contents
        var suggestionItems = suggestions.Locator(".smartcombobox-suggestion[role=option]");
        Assert.Equal(10, await suggestionItems.CountAsync());
        var suggestionTexts = await suggestionItems.AllTextContentsAsync();
        Assert.Equal(["Transportation: Air", "Transportation: Rail", "Transportation: Road"], suggestionTexts.Take(3).OrderBy(x => x));
        await Expect(suggestionItems.First).ToHaveTextAsync("Transportation: Road");

        // Suggestion list is hidden if you focus out of the input
        await input.BlurAsync();
        await Expect(suggestions).ToHaveCSSAsync("display", "none");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.Null(await input.GetAttributeAsync("aria-activedescendant"));

        // ...and re-shows if you re-focus
        await input.FocusAsync();
        await Expect(suggestions).ToHaveCSSAsync("display", "block");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "true");
        await AssertNthSuggestionIsActive(input, suggestions, 0);

        // ...and updates if you type more
        await input.PressSequentiallyAsync(" train");
        await Expect(suggestionItems.First).ToHaveTextAsync("Transportation: Rail");
    }

    [Fact]
    public async Task CanSetSimilarityThreshold()
    {
        var input = Page.Locator("#with-similarity-threshold input");
        var suggestions = Page.Locator("#with-similarity-threshold smart-combobox");
        await input.FillAsync("transport");
        await Expect(suggestions).ToHaveCSSAsync("display", "block");

        // With the default similarity threshold, we get 10 items. With the threshold 0.7, there are only 3.
        Assert.Equal(3, await suggestions.Locator(".smartcombobox-suggestion[role=option]").CountAsync());
    }

    [Fact]
    public async Task CanSetMaxSuggestions()
    {
        var input = Page.Locator("#with-max-suggestions input");
        var suggestions = Page.Locator("#with-max-suggestions smart-combobox");
        await input.FillAsync("transport");
        await Expect(suggestions).ToHaveCSSAsync("display", "block");

        // This test case is configured with MaxSuggestions=2
        Assert.Equal(2, await suggestions.Locator(".smartcombobox-suggestion[role=option]").CountAsync());
    }

    [Fact]
    public async Task CanUseKeyboardToHighlightAndAcceptSuggestion()
    {
        // Type and wait for suggestions to appear
        var input = Page.Locator("#default-params input");
        var suggestions = Page.Locator("#default-params smart-combobox");
        await input.FillAsync("cash");
        await Expect(suggestions).ToHaveCSSAsync("display", "block");

        // Initially, the first suggestion is highlighted
        await AssertNthSuggestionIsActive(input, suggestions, 0);

        // Pressing enter at this stage accepts the suggestion and blurs the input
        await input.PressAsync("Enter");
        await Expect(input).Not.ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("Current Assets: Cash");
        await Expect(suggestions).ToHaveCSSAsync("display", "none");

        // After re-focusing, we can use the cursors to highlight a suggestion
        await input.FocusAsync();
        await input.PressAsync("ArrowDown");
        await input.PressAsync("ArrowDown");
        await input.PressAsync("ArrowDown");
        await input.PressAsync("ArrowUp");
        await Expect(input).ToHaveValueAsync("Equity: Paid-in Capital");
        await AssertNthSuggestionIsActive(input, suggestions, 2);

        // If needed, the suggestions list scrolls to show the highlighted item
        Assert.Equal(0, await suggestions.EvaluateAsync<int>("s => s.scrollTop"));
        for (var i = 0; i < 20; i++) // Keep pressing even after we get to the bottom to show that no-ops
        {
            await input.PressAsync("ArrowDown");
        }
        await AssertNthSuggestionIsActive(input, suggestions, 9);
        Assert.NotEqual(0, await suggestions.EvaluateAsync<int>("s => s.scrollTop"));

        // Pressing enter again now accepts the suggestion and blurs the input
        var highlightedSuggestionText = await suggestions.Locator("[aria-selected=true]").TextContentAsync();
        Assert.NotNull(highlightedSuggestionText);
        Assert.NotEmpty(highlightedSuggestionText);
        await Expect(input).ToHaveValueAsync(highlightedSuggestionText);
        await input.PressAsync("Enter");
        await Expect(input).Not.ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync(highlightedSuggestionText);
        await Expect(suggestions).ToHaveCSSAsync("display", "none");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task CanUseMouseToAcceptSuggestion()
    {
        // Type and wait for suggestions to appear
        var input = Page.Locator("#default-params input");
        var suggestions = Page.Locator("#default-params smart-combobox");
        await input.FillAsync("cash");
        await Expect(suggestions).ToHaveCSSAsync("display", "block");

        // Click on the second suggestion
        var suggestionToAccept = suggestions.Locator(".smartcombobox-suggestion").Nth(1);
        var expectedsuggestionText = "Current Assets: Petty Cash";
        await Expect(suggestionToAccept).ToHaveTextAsync(expectedsuggestionText);
        await suggestionToAccept.ClickAsync();

        // Check the results
        await Expect(input).Not.ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync(expectedsuggestionText);
        await Expect(suggestions).ToHaveCSSAsync("display", "none");
        await Expect(input).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task InferenceEndpointValidatesAntiforgery()
    {
        var url = Server.Address + "/api/accounting-categories";
        var response = await new HttpClient().SendAsync(new(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent([
                new("inputValue", ""),
                new("maxResults", "0"),
                new("similarityThreshold", "0"),
            ])
        });

        // Strange that it's not a 400. Maybe it's for historical reasons.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("AntiforgeryValidationException", await response.Content.ReadAsStringAsync());
    }

    private static async Task AssertNthSuggestionIsActive(ILocator input, ILocator suggestions, int expectedSuggestionIndex)
    {
        var suggestionItems = suggestions.Locator(".smartcombobox-suggestion");
        var expectedSuggestionId = await suggestionItems.Nth(expectedSuggestionIndex).GetAttributeAsync("id");
        Assert.NotNull(expectedSuggestionId);
        Assert.NotEmpty(expectedSuggestionId);
        await Expect(input).ToHaveAttributeAsync("aria-activedescendant", expectedSuggestionId);

        // Also check that only this suggestion is marked as aria-selected
        await Expect(suggestionItems.Nth(expectedSuggestionIndex)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(suggestions.Locator(".smartcombobox-suggestion[aria-selected=true]")).ToHaveCountAsync(1);
    }
}
