using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public static class TestComponentEndpointExtensions
{
    public static IServiceCollection AddSmartComponents(this IServiceCollection services)
    {
        services.AddTransient<IStartupFilter, StartupEnhancementStartupFilter>();
        return services;
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
                    app.MapGet("/_example", () =>
                    {
                        return "Hello, this is /_example";
                    });
                });
            };
        }
    }
}
