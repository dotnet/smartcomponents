// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace SmartComponents.Inference.OpenAI;

internal class ApiConfig
{
    public string? ApiKey { get; }
    public string? DeploymentName { get; }
    public Uri? Endpoint { get; }
    public bool SelfHosted { get; }

    public ApiConfig(IConfiguration config)
    {
        var configSection = config.GetRequiredSection("SmartComponents");

        SelfHosted = configSection.GetValue<bool?>("SelfHosted") ?? false;

        if (SelfHosted)
        {
            Endpoint = configSection.GetValue<Uri>("Endpoint")
                ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:Endpoint. This is required for SelfHosted inference.");

            // Ollama uses this, but other self-hosted backends might not, so it's optional.
            DeploymentName = configSection.GetValue<string>("DeploymentName");

            // Ollama doesn't use this, but other self-hosted backends might do, so it's optional.
            ApiKey = configSection.GetValue<string>("ApiKey");
        }
        else
        {
            // If set, we assume Azure OpenAI. If not, we assume OpenAI.
            Endpoint = configSection.GetValue<Uri>("Endpoint");

            // For Azure OpenAI, it's your deployment name. For OpenAI, it's the model name.
            DeploymentName = configSection.GetValue<string>("DeploymentName")
                ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:DeploymentName");

            ApiKey = configSection.GetValue<string>("ApiKey")
                ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:ApiKey");
        }
    }
}
