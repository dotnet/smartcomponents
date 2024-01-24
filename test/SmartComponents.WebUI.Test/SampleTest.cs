namespace SmartComponents.Blazor.Test;

public abstract class SampleTest<TStartup> : ServerTestBase<TStartup> where TStartup: class
{
    public SampleTest(CustomWebApplicationFactory<TStartup> server) : base(server)
    {   
    }

    [Fact]
    public async Task SaysHelloWorld()
    {
        await Page.GotoAsync(Server.Address);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");
    }

    [Fact]
    public async Task ContainsTestComponentWithStyle()
    {
        await Page.GotoAsync(Server.Address);

        var componentLocator = Page.Locator(".my-component");
        await Expect(componentLocator).ToContainTextAsync("This is a test component");
        await Expect(componentLocator).ToHaveCSSAsync("border-color", "rgb(255, 0, 0)");
    }
}
