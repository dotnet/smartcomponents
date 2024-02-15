using SmartComponents.Inference.OpenAI;
using SmartComponents.LocalEmbeddings;

namespace TestMvcApp;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddRepoSharedConfig();

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddSmartComponents()
            .WithInferenceBackend<OpenAIInferenceBackend>();
        builder.Services.AddAntiforgery();

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
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAntiforgery();

        app.UseAuthorization();

        app.MapSmartComboBox<LocalEmbeddingsCache>("/api/accounting-categories",
            _ => E2ETests.TestData.AccountingCategories);

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}