using SmartComponents.Inference;
using TestMvcApp;

namespace SmartComponents.E2ETest.Mvc;

public class MvcSmartPasteTest : SmartPasteTest<Program>
{
    public MvcSmartPasteTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
        SmartPasteInference.OverrideDateForTesting = new DateTime(2024, 2, 9);
    }
}