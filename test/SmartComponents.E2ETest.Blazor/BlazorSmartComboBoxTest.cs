using TestBlazorApp;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace SmartComponents.E2ETest.Blazor;

public class BlazorSmartComboBoxTest : SmartComboBoxTest<Program>
{
    public BlazorSmartComboBoxTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }

    [Theory]
    [InlineData("webassembly")]
    [InlineData("server")]
    public async Task TriggersBindings(string host)
    {
        await Page.GotoAsync(Server.Address + "/smartcombobox/" + host);
        await Expect(Page.Locator("#is-interactive")).ToHaveTextAsync("True", new() { Timeout = 20000 });

        var input = Page.Locator("#default-params input");
        var suggestions = Page.Locator("#default-params smart-combobox");
        var boundValue = Page.Locator("#default-params-bound-value");

        // Check we show the initial bound value
        await Expect(input).ToHaveValueAsync("Initial value");
        await Expect(boundValue).ToHaveTextAsync("Initial value");

        // Now begin typing a value
        await input.FillAsync("cash");
        await Expect(suggestions).ToHaveCSSAsync("display", "block");

        // We don't overwrite the input value until the user selects a suggestion
        await Expect(input).ToHaveValueAsync("cash");

        // If you blur at any time, we'll bind the value even if it doesn't match a suggestion
        await Expect(boundValue).ToHaveTextAsync("Initial value");
        await input.BlurAsync();
        await Expect(boundValue).ToHaveTextAsync("cash");

        // If you press enter before using the cursors, that accepts the first suggestion
        await input.FocusAsync();
        await Expect(suggestions).ToHaveCSSAsync("display", "block");
        await input.PressAsync("Enter");
        await Expect(boundValue).ToHaveTextAsync("Current Assets: Cash");

        // Using the cursors to highlight a suggestion will trigger the binding
        await input.FocusAsync();
        await Expect(suggestions).ToHaveCSSAsync("display", "block");
        await input.PressAsync("ArrowDown");
        await Expect(boundValue).ToHaveTextAsync("Current Assets: Petty Cash");

        // Using the mouse to select a suggestion will trigger the binding
        await suggestions.Locator(".smartcombobox-suggestion").First.ClickAsync();
        await Expect(boundValue).ToHaveTextAsync("Current Assets: Cash");
    }
}
