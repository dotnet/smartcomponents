// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace SmartComponents.Inference.OpenAI;

internal class ApiConfig
{
    public string ApiKey { get; }
    public string? DeploymentName { get; }
    public string? Endpoint { get; }

    public ApiConfig(IConfiguration config)
    {
        var configSection = config.GetRequiredSection("SmartComponents");
        ApiKey = configSection.GetValue<string>("ApiKey")
            ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:ApiKey");
        DeploymentName = configSection.GetValue<string>("DeploymentName")
            ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:DeploymentName");
        Endpoint = configSection.GetValue<string>("Endpoint");
    }
}
