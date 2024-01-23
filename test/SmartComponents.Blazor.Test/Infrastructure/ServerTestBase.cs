
using System.Diagnostics;

namespace SmartComponents.Test.Infrastructure;

public abstract class ServerTestBase<TStartup>
    : IAsyncLifetime, IClassFixture<CustomWebApplicationFactory<TStartup>> where TStartup : class
{
    protected CustomWebApplicationFactory<TStartup> Server { get; }
    protected IPlaywright Playwright { get; private set; } = default!;
    protected IBrowser Browser { get; private set; } = default!;
    protected IPage Page { get; private set; } = default!;

    public ServerTestBase(CustomWebApplicationFactory<TStartup> server)
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
