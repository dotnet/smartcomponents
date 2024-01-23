namespace SmartComponents.Blazor.Test;

public class SampleTest : ServerTestBase<ExampleBlazorApp.Program>
{
    public SampleTest(CustomWebApplicationFactory<ExampleBlazorApp.Program> server) : base(server)
    {   
    }

    [Fact]
    public async Task SaysHelloWorld()
    {
        await Page.GotoAsync(Server.Address);
        await Expect(Page.Locator("h1")).ToContainTextAsync("Hello, world!");
    }
}
