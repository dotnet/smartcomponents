// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace SmartComponents.E2ETest.Common.Infrastructure;

public static class TextAreaAssertions
{
    public static async Task AssertSelectionPositionAsync(ILocator locator, int start, int length)
    {
        Assert.Equal(start, await SelectionStartAsync(locator));
        Assert.Equal(start + length, await SelectionEndAsync(locator));
    }

    public static Task<int> SelectionStartAsync(ILocator locator)
        => locator.EvaluateAsync<int>("s => s.selectionStart");

    public static Task<int> SelectionEndAsync(ILocator locator)
        => locator.EvaluateAsync<int>("s => s.selectionEnd");
}
