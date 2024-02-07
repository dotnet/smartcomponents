using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartComponents.Inference;
using SmartComponents.StaticAssets.Inference;

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
                app.MapGet("/_example", async (IInferenceBackend inference) =>
                {
                    var prompt = "The capital of France is: ";
                    var response = await inference.GetChatResponseAsync(new ChatOptions(prompt));
                    return response;
                });

                app.MapPost("/_smartcomponents/smartpaste", async (IInferenceBackend inference, HttpContext httpContext, IAntiforgery antiforgery, [FromForm] string dataJson) =>
                {
                    await antiforgery.ValidateRequestAsync(httpContext);
                    var result = await SmartPasteInference.GetFormCompletionsAsync(inference, dataJson);
                    return result.BadRequest ? Results.BadRequest() : Results.Content(result.Response);
                });
            });
        };
    }
}
