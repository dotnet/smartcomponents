using TestBlazorApp;

namespace SmartComponents.E2ETest.Blazor;

public class BlazorSmartPasteTest : SmartPasteTest<Program>
{
    public BlazorSmartPasteTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}
