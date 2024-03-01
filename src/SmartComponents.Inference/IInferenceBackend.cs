// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace SmartComponents.StaticAssets.Inference;

public interface IInferenceBackend
{
    Task<string> GetChatResponseAsync(ChatParameters options);
}
