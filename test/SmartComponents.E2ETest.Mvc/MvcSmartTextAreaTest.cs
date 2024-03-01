// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestMvcApp;

namespace SmartComponents.E2ETest.Mvc;

public class MvcSmartTextAreaInlineTest : SmartTextAreaInlineTest<Program>
{
    public MvcSmartTextAreaInlineTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}

public class MvcSmartTextAreaOverlayTest : SmartTextAreaOverlayTest<Program>
{
    public MvcSmartTextAreaOverlayTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}
