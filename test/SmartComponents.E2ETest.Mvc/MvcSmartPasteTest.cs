// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestMvcApp;

namespace SmartComponents.E2ETest.Mvc;

public class MvcSmartPasteTest : SmartPasteTest<Program>
{
    public MvcSmartPasteTest(KestrelWebApplicationFactory<Program> server) : base(server)
    {
    }
}
