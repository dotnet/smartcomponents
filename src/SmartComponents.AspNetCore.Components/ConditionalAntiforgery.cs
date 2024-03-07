// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Forms;
#endif

namespace SmartComponents;

internal sealed class ConditionalAntiforgery
{
    public string? FormFieldName { get; }

    public string? Value { get; }

    public ConditionalAntiforgery(IServiceProvider services)
    {
        // AntiforgeryStateProvider only exists in .NET 8 and later. We'll use it when
        // we can. If not, we'll just leave the properties null, and then the client-side
        // code will try to obtain the token from an enclosing <form>.
#if NET8_0_OR_GREATER
        var antiforgeryState = services.GetService<AntiforgeryStateProvider>();
        if (antiforgeryState?.GetAntiforgeryToken() is {} requestToken)
        {
            FormFieldName = requestToken.FormFieldName;
            Value = requestToken.Value;
        }
#endif
    }
}
