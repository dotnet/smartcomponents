using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SmartComponents.StaticAssets.Inference;

namespace Microsoft.AspNetCore.Builder;

public static class TestComponentEndpointExtensions
{
    public static ISmartComponentsBuilder AddSmartComponents(this IServiceCollection services)
    {
        services.AddTransient<IStartupFilter, StartupEnhancementStartupFilter>();
        return new DefaultSmartComponentsBuilder(services);
    }

    private class StartupEnhancementStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
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
                });
            };
        }
    }
}
