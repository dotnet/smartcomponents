// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;

namespace SmartComponents.Inference.OpenAI;

/// <summary>
/// Used to resolve queries using Ollama or anything else that exposes an OpenAI-compatible
/// endpoint with a scheme/host/port set of your choice.
/// </summary>
internal class SelfHostedLlmTransport(Uri endpoint) : HttpClientTransport
{
    public override ValueTask ProcessAsync(HttpMessage message)
    {
        message.Request.Uri.Scheme = endpoint.Scheme;
        message.Request.Uri.Host = endpoint.Host;
        message.Request.Uri.Port = endpoint.Port;
        return base.ProcessAsync(message);
    }
}
