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
                var smartPasteEndpoint = app.MapPost("/_smartcomponents/smartpaste", async ([FromServices] IInferenceBackend inference, HttpContext httpContext, [FromServices] IAntiforgery antiforgery, [FromServices] SmartPasteInference smartPasteInference) =>
                {
                    // We use DisableAntiforgery and validate manually so that it works whether
                    // or not you have UseAntiforgery middleware in the pipeline. Without doing that,
                    // people will get errors like https://stackoverflow.com/questions/61829324
                    await antiforgery.ValidateRequestAsync(httpContext);

                    // Can't use [FromForm] on net6.0
                    if (!httpContext.Request.Form.TryGetValue("dataJson", out var dataJson))
                    {
                        return Results.BadRequest("dataJson is required");
                    }

                    var result = await smartPasteInference.GetFormCompletionsAsync(inference, dataJson.ToString());
                    return result.BadRequest ? Results.BadRequest() : Results.Content(result.Response!);
                });

                var smartTextAreaEndpoint = app.MapPost("/_smartcomponents/smarttextarea", async ([FromServices] IInferenceBackend inference, HttpContext httpContext, [FromServices] IAntiforgery antiforgery, [FromServices] SmartTextAreaInference smartTextAreaInference) =>
                {
                    // See above for why we validate antiforgery manually
                    await antiforgery.ValidateRequestAsync(httpContext);

                    // Can't use [FromForm] on net6.0
                    var form = httpContext.Request.Form;
                    if (!form.TryGetValue("config", out var config)
                        || !form.TryGetValue("textBefore", out var textBefore)
                        || !form.TryGetValue("textAfter", out var textAfter))
                    {
                        return Results.BadRequest("config, textBefore, and textAfter are required");
                    }

                    var parsedConfig = JsonSerializer.Deserialize<SmartTextAreaConfig>(config.ToString())!;
                    var suggestion = await smartTextAreaInference.GetInsertionSuggestionAsync(inference, parsedConfig, textBefore.ToString(), textAfter.ToString());
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
