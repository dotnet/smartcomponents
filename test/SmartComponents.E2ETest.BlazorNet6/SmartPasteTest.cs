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
}
