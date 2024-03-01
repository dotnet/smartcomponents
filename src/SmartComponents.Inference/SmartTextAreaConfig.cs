// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SmartComponents.Infrastructure;

#if SMART_COMPONENTS_COMPONENTS
internal
#else
public
#endif
struct SmartTextAreaConfig
{
    public string? Parameters { get; set; }
    public string? UserRole { get; set; }
    public string[]? UserPhrases { get; set; }
}
