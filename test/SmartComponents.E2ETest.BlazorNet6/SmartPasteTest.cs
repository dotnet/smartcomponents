// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SmartComponents.E2ETest.BlazorNet6;

public class SmartPasteTest : PlaywrightTestBase<TestBlazorServerNet6App.Program>
{
    public SmartPasteTest(KestrelWebApplicationFactory<TestBlazorServerNet6App.Program> server) : base(server)
    {
    }

    protected override async Task OnBrowserReadyAsync()
    {
        await Page.GotoAsync(Server.Address + "/smartpaste");
        await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
    }

    [Fact]
    public async Task SiteLoads()
    {
        Assert.Equal("Smart Paste", await Page.TitleAsync());
    }

    [Fact]
    public async Task CanPopulateTextBoxes()
    {
        var form = Page.Locator("#simple-case");
        await Expect(form.Locator("[name=firstname]")).ToBeEmptyAsync();
        await Expect(form.Locator("[name=lastname]")).ToBeEmptyAsync();

        await SetClipboardContentsAsync("Rahul Mandal");

        await form.Locator(".smart-paste-button").ClickAsync();
        await Expect(form.Locator("[name=firstname]")).ToHaveValueAsync("Rahul");
        await Expect(form.Locator("[name=lastname]")).ToHaveValueAsync("Mandal");
    }

    protected Task SetClipboardContentsAsync(string text)
        => Page.Locator("html").EvaluateAsync("(ignored, value) => navigator.clipboard.writeText(value)", text);
}
