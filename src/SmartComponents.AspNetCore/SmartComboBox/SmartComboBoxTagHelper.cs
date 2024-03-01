// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace SmartComponents.AspNetCore;

[HtmlTargetElement("smart-combobox", TagStructure = TagStructure.WithoutEndTag)]
public class SmartComboBoxTagHelper : TagHelper
{
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public string Url { get; set; } = default!;

    public int MaxSuggestions { get; set; } = 10;

    public float SimilarityThreshold { get; set; } = 0.5f;

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // This is better than specifying attributes on [HtmlTargetElement] because
        // it gives the developer feedback if they forget to supply the attribute
        if (string.IsNullOrEmpty(Url))
        {
            throw new InvalidOperationException($"The smart-combobox tag helper requires a value for the '{nameof(Url)}' attribute.");
        }

        output.TagName = "input";
        output.Attributes.Add("role", "combobox");
        output.Attributes.Add("aria-expanded", "false");
        output.Attributes.Add("aria-autocomplete", "list");

        output.PostElement.SetHtmlContent("<smart-combobox role=\"listbox\"");
        PassThroughAttributeIfPresent(context, output, "title", "title");
        PassThroughAttributeIfPresent(context, output, "id", "aria-label");

        AddPostElementAttribute(output, " data-max-suggestions", MaxSuggestions.ToString(CultureInfo.InvariantCulture));
        AddPostElementAttribute(output, " data-similarity-threshold", SimilarityThreshold.ToString(CultureInfo.InvariantCulture));

        var services = ViewContext.HttpContext.RequestServices;
        var urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(ViewContext);
        AddPostElementAttribute(output, " data-suggestions-url", urlHelper.Content(Url));

        var antiforgery = services.GetRequiredService<IAntiforgery>();
        if (antiforgery is not null)
        {
            var tokens = antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
            AddPostElementAttribute(output, " data-antiforgery-name", tokens.FormFieldName);
            AddPostElementAttribute(output, " data-antiforgery-value", tokens.RequestToken);
        }

        output.PostElement.AppendHtml("></smart-combobox>");

        return Task.CompletedTask;
    }

    private static void AddPostElementAttribute(TagHelperOutput output, string nameWithLeadingSpace, string? value)
    {
        var postElement = output.PostElement;
        postElement.AppendHtml(nameWithLeadingSpace);
        if (value is not null)
        {
            postElement.AppendHtml("=\"");
            postElement.Append(value);
            postElement.AppendHtml("\"");
        }
    }

    private static void PassThroughAttributeIfPresent(TagHelperContext context, TagHelperOutput output, string inputAttributeName, string outputAttributeName)
    {
        if (context.AllAttributes.TryGetAttribute(inputAttributeName, out var attrib) && attrib is { Value: HtmlString value })
        {
            output.PostElement.AppendHtml($" {outputAttributeName}=\"");
            output.PostElement.AppendHtml(value);
            output.PostElement.AppendHtml("\"");
        }
    }
}
