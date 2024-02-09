using SmartComponents.Inference;
using TestBlazorApp;

namespace SmartComponents.E2ETest.Blazor;

public class BlazorSmartPasteTest : SmartPasteTest<Program>
{
    public BlazorSmartPasteTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
        SmartPasteInference.OverrideDateForTesting = new DateTime(2024, 2, 9);
    }
}
