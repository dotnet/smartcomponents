using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartComponents.Inference;
using SmartComponents.Infrastructure;
using SmartComponents.StaticAssets.Inference;
using System.Text.Json;

namespace Microsoft.AspNetCore.Builder;

public static class SmartComponentsServiceCollectionExtensions
{
    public static ISmartComponentsBuilder AddSmartComponents(this IServiceCollection services)
    {
        services.AddTransient<IStartupFilter, AttachSmartComponentsEndpointsStartupFilter>();
        return new DefaultSmartComponentsBuilder(services);
    }

    private class AttachSmartComponentsEndpointsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => builder =>
        {
            next(builder);

            builder.UseEndpoints(app =>
            {
                app.MapPost("/_smartcomponents/smartpaste", async (IInferenceBackend inference, HttpContext httpContext, IAntiforgery antiforgery, [FromForm] string dataJson) =>
                {
                    await antiforgery.ValidateRequestAsync(httpContext);
                    var result = await SmartPasteInference.GetFormCompletionsAsync(inference, dataJson);
                    return result.BadRequest ? Results.BadRequest() : Results.Content(result.Response);
                });

                app.MapPost("/_smartcomponents/smarttextarea", async (IInferenceBackend inference, HttpContext httpContext, IAntiforgery antiforgery, [FromForm] string config, [FromForm] string textBefore, [FromForm] string textAfter) =>
                {
                    await antiforgery.ValidateRequestAsync(httpContext);

                    var parsedConfig = JsonSerializer.Deserialize<SmartTextAreaConfig>(config)!;
                    var suggestion = await SmartTextAreaInference.GetInsertionSuggestionAsync(inference, parsedConfig, textBefore, textAfter);
                    return Results.Content(suggestion);
                });
            });
        };
    }
}
