using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SmartComponents.Inference;

namespace Microsoft.AspNetCore.Builder;

public static class SmartComboBoxEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSmartComboBox(this IEndpointRouteBuilder builder, string url, Func<SuggestionsRequest, IEnumerable<string>> suggestions)
        => MapSmartComboBoxCore(builder, url, null, req => Task.FromResult(suggestions(req)));

    public static IEndpointRouteBuilder MapSmartComboBox<T>(this IEndpointRouteBuilder builder, string url, Func<SuggestionsRequest, IEnumerable<string>> suggestions) where T: ISimilarityMatcher, new()
        => MapSmartComboBoxCore(builder, url, new T(), req => Task.FromResult(suggestions(req)));

    public static IEndpointRouteBuilder MapSmartComboBox(this IEndpointRouteBuilder builder, string url, ISimilarityMatcher similarityMatcher, Func<SuggestionsRequest, IEnumerable<string>> suggestions)
        => MapSmartComboBoxCore(builder, url, similarityMatcher, req => Task.FromResult(suggestions(req)));

    public static IEndpointRouteBuilder MapSmartComboBox(this IEndpointRouteBuilder builder, string url, Func<SuggestionsRequest, Task<IEnumerable<string>>> suggestions)
        => MapSmartComboBoxCore(builder, url, null, req => suggestions(req));

    public static IEndpointRouteBuilder MapSmartComboBox(this IEndpointRouteBuilder builder, string url, ISimilarityMatcher similarityMatcher, Func<SuggestionsRequest, Task<IEnumerable<string>>> suggestions)
        => MapSmartComboBoxCore(builder, url, similarityMatcher, req => suggestions(req));

    public static IEndpointRouteBuilder MapSmartComboBox<T>(this IEndpointRouteBuilder builder, string url, Func<SuggestionsRequest, Task<IEnumerable<string>>> suggestions) where T: ISimilarityMatcher, new()
        => MapSmartComboBoxCore(builder, url, new T(), req => suggestions(req));

    private static IEndpointRouteBuilder MapSmartComboBoxCore(this IEndpointRouteBuilder builder, string url, ISimilarityMatcher? similarityMatcher, Func<SuggestionsRequest, Task<IEnumerable<string>>> suggestions)
    {
        // Validates antiforgery implicitly because we accept [FromForm] parameters
        // https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0#antiforgery-with-minimal-apis
        builder.MapPost(url, async (HttpContext httpContext,
            [FromForm] string inputValue,
            [FromForm] int maxResults,
            [FromForm] float similarityThreshold) =>
        {
            if (string.IsNullOrEmpty(inputValue))
            {
                return Results.BadRequest("inputValue is required");
            }

            var suggestionsList = await suggestions(new SuggestionsRequest
            {
                HttpContext = httpContext,
                InputValue = inputValue,
                MaxResults = maxResults,
                SimilarityThreshold = similarityThreshold,
            });

            // If you've provided a similarity matcher, we use it. Otherwise we assume the
            // callback has already done whatever filtering/ordering you want.
            if (similarityMatcher is not null)
            {
                var matches = similarityMatcher.FindClosest(inputValue, suggestionsList, similarityThreshold);
                suggestionsList = matches.Take(maxResults).Select(r => r.Text);
            }

            return Results.Ok(suggestionsList);
        });

        return builder;
    }
}
