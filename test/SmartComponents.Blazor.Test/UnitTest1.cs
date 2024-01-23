using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace SmartComponents.Blazor.Test;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await page.GotoAsync("https://playwright.dev/dotnet");


        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Get started" })).ToBeVisibleAsync();
    }
}
