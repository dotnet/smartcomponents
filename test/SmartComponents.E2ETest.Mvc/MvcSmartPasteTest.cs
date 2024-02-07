using TestMvcApp;

namespace SmartComponents.E2ETest.Mvc;

public class MvcSmartPasteTest : SmartPasteTest<Program>
{
    public MvcSmartPasteTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}