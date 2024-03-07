// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartComponents.Inference;
using SmartComponents.Infrastructure;
using SmartComponents.StaticAssets.Inference;

namespace Microsoft.AspNetCore.Builder;

public static class SmartComponentsServiceCollectionExtensions
{
    public static ISmartComponentsBuilder AddSmartComponents(this IServiceCollection services)
    {
        // Default inference implementations. Developers can subclass these and register their
        // own implementations if they want to override the prompts or the calls to the backend.
        services.TryAddScoped<SmartTextAreaInference>();
        services.TryAddScoped<SmartPasteInference>();

        services.AddTransient<IStartupFilter, AttachSmartComponentsEndpointsStartupFilter>();
        return new DefaultSmartComponentsBuilder(services);
    }

    private sealed class AttachSmartComponentsEndpointsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => builder =>
        {
            next(builder);

            builder.UseEndpoints(app =>
            {
                var smartPasteEndpoint = app.MapPost("/_smartcomponents/smartpaste", async ([FromServices] IInferenceBackend inference, HttpContext httpContext, [FromServices] IAntiforgery antiforgery, [FromServices] SmartPasteInference smartPasteInference, [FromForm] string dataJson) =>
                {
                    // We use DisableAntiforgery and validate manually so that it works whether
                    // or not you have UseAntiforgery middleware in the pipeline. Without doing that,
                    // people will get errors like https://stackoverflow.com/questions/61829324
                    await antiforgery.ValidateRequestAsync(httpContext);

                    var result = await smartPasteInference.GetFormCompletionsAsync(inference, dataJson);
                    return result.BadRequest ? Results.BadRequest() : Results.Content(result.Response!);
                });

                var smartTextAreaEndpoint = app.MapPost("/_smartcomponents/smarttextarea", async ([FromServices] IInferenceBackend inference, HttpContext httpContext, [FromServices] IAntiforgery antiforgery, [FromServices] SmartTextAreaInference smartTextAreaInference, [FromForm] string config, [FromForm] string textBefore, [FromForm] string textAfter) =>
                {
                    // See above for why we validate antiforgery manually
                    await antiforgery.ValidateRequestAsync(httpContext);

                    var parsedConfig = JsonSerializer.Deserialize<SmartTextAreaConfig>(config)!;
                    var suggestion = await smartTextAreaInference.GetInsertionSuggestionAsync(inference, parsedConfig, textBefore, textAfter);
                    return Results.Content(suggestion);
                });

#if NET8_0_OR_GREATER
                smartPasteEndpoint.DisableAntiforgery();
                smartTextAreaEndpoint.DisableAntiforgery();
#endif
            });
        };
    }
}
