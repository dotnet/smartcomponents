using TestMvcApp;

namespace SmartComponents.E2ETest.Mvc;

public class MvcSmartComboBoxTest : SmartComboBoxTest<Program>
{
    public MvcSmartComboBoxTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}
