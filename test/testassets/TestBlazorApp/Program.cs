// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using E2ETests;
using SmartComponents.Inference;
using SmartComponents.Inference.OpenAI;
using SmartComponents.LocalEmbeddings;
using TestBlazorApp.Components;

namespace TestBlazorApp;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddRepoSharedConfig();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();
        builder.Services.AddScoped<SmartPasteInference, SmartPasteInferenceForTests>();
        builder.Services.AddSmartComponents()
            .WithInferenceBackend<OpenAIInferenceBackend>()
            .WithAntiforgeryValidation(); // This doesn't benefit most apps, but we'll validate it works in E2E tests

        var app = builder.Build();

        // Show we can work with pathbase by enforcing its use
        app.UsePathBase("/subdir");
        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.PathBase.Equals("/subdir", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("This server only serves requests at /subdir");
            }
            else
            {
                await next();
            }
        });

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        var embedder = new LocalEmbedder();
        var accountingCategories = embedder.EmbedRange(E2ETests.TestData.AccountingCategories);

        app.MapSmartComboBox("/api/accounting-categories",
            request => embedder.FindClosest(request.Query, accountingCategories));

        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(TestBlazorApp.Client._Imports).Assembly);

        app.Run();
    }
}
