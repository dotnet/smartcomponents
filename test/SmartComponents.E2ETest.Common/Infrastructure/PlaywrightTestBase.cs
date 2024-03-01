// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Playwright;

namespace SmartComponents.E2ETest.Common.Infrastructure;

public abstract class PlaywrightTestBase<TStartup>
    : IAsyncLifetime, IClassFixture<KestrelWebApplicationFactory<TStartup>> where TStartup : class
{
    protected KestrelWebApplicationFactory<TStartup> Server { get; }
    protected IPlaywright Playwright { get; private set; } = default!;
    protected IBrowser Browser { get; private set; } = default!;
    protected IPage Page { get; private set; } = default!;

    public PlaywrightTestBase(KestrelWebApplicationFactory<TStartup> server)
    {
        Server = server;
    }

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = !Debugger.IsAttached,
        });

        Page = await Browser.NewPageAsync();
        await OnBrowserReadyAsync();
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    protected virtual Task OnBrowserReadyAsync()
        => Task.CompletedTask;
}
