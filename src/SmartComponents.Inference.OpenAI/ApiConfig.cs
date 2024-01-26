using Microsoft.Extensions.Configuration;
using System;

namespace SmartComponents.Inference.OpenAI;

internal class ApiConfig
{
    public string DeploymentName { get; }
    public string Endpoint { get; }
    public string ApiKey { get; }

    public ApiConfig(IConfiguration config)
    {
        var configSection = config.GetRequiredSection("SmartComponents");
        DeploymentName = configSection.GetValue<string>("DeploymentName")
            ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:DeploymentName");
        Endpoint = configSection.GetValue<string>("Endpoint")
            ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:Endpoint");
        ApiKey = configSection.GetValue<string>("ApiKey")
            ?? throw new InvalidOperationException("Missing required configuration value: SmartComponents:ApiKey");
    }
}
