using Microsoft.Playwright;
using System.Diagnostics;

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
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}
