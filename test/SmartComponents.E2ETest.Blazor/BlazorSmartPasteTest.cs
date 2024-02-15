using SmartComponents.Inference;
using TestBlazorApp;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace SmartComponents.E2ETest.Blazor;

public class BlazorSmartPasteTest : SmartPasteTest<Program>
{
    public BlazorSmartPasteTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
        SmartPasteInference.OverrideDateForTesting = new DateTime(2024, 2, 9);
    }

    [Fact]
    public async Task WorksOnInteractiveWebAssembly()
    {
        await Page.GotoAsync(Server.Address + "/smartpaste/webassembly");
        await Expect(Page.Locator("#is-interactive")).ToHaveTextAsync("True", new() { Timeout = 20000 });

        var form = Page.Locator("#simple-case");
        await Expect(form.Locator("[name=firstname]")).ToBeEmptyAsync();
        await Expect(form.Locator("[name=lastname]")).ToBeEmptyAsync();

        await SetClipboardContentsAsync("Rahul Mandal");

        await form.Locator(".smart-paste-button").ClickAsync();
        await Expect(form.Locator("[name=firstname]")).ToHaveValueAsync("Rahul");
        await Expect(form.Locator("[name=lastname]")).ToHaveValueAsync("Mandal");

        // See it triggers interactive bindings too
        await Expect(Page.Locator("#bound-firstname")).ToHaveTextAsync("Rahul");
        await Expect(Page.Locator("#bound-lastname")).ToHaveTextAsync("Mandal");
    }

    [Fact]
    public async Task WorksOnInteractiveServer()
    {
        await Page.GotoAsync(Server.Address + "/smartpaste/server");
        await Expect(Page.Locator("#is-interactive")).ToHaveTextAsync("True");

        var form = Page.Locator("#simple-case");
        await Expect(form.Locator("[name=firstname]")).ToBeEmptyAsync();
        await Expect(form.Locator("[name=lastname]")).ToBeEmptyAsync();

        await SetClipboardContentsAsync("Rahul Mandal");

        await form.Locator(".smart-paste-button").ClickAsync();
        await Expect(form.Locator("[name=firstname]")).ToHaveValueAsync("Rahul");
        await Expect(form.Locator("[name=lastname]")).ToHaveValueAsync("Mandal");

        // See it triggers interactive bindings too
        await Expect(Page.Locator("#bound-firstname")).ToHaveTextAsync("Rahul");
        await Expect(Page.Locator("#bound-lastname")).ToHaveTextAsync("Mandal");
    }
}
