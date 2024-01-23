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
}
