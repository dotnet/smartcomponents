// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using SmartComponents.Infrastructure;

namespace SmartComponents.AspNetCore;

[HtmlTargetElement("smart-textarea", TagStructure = TagStructure.NormalOrSelfClosing)]
public class SmartTextAreaTagHelper : TagHelper
{
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public string UserRole { get; set; } = default!;

    public string[]? UserPhrases { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // It just doesn't make sense to use this component without providing some information about
        // the kinds of suggestions you want.
        if (string.IsNullOrEmpty(UserRole))
        {
            throw new InvalidOperationException($"<smart-textarea> requires a non-null, non-empty 'user-role' parameter.");
        }

        output.TagName = "textarea";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Add("aria-autocomplete", "both");
        output.Attributes.Add("data-smart-textarea", null);

        var services = ViewContext.HttpContext.RequestServices;
        var urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(ViewContext);

        output.PostElement.SetHtmlContent("<smart-textarea");
        AddPostElementAttribute(output, " data-url", urlHelper.Content("~/_smartcomponents/smarttextarea"));

        var config = new SmartTextAreaConfig { UserRole = UserRole, UserPhrases = UserPhrases };
        AddPostElementAttribute(output, " data-config", JsonSerializer.Serialize(config));

        var antiforgery = services.GetRequiredService<IAntiforgery>();
        if (antiforgery is not null)
        {
            var tokens = antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
            AddPostElementAttribute(output, " data-antiforgery-name", tokens.FormFieldName);
            AddPostElementAttribute(output, " data-antiforgery-value", tokens.RequestToken);
        }

        output.PostElement.AppendHtml("></smart-textarea>");
    }

    private static void AddPostElementAttribute(TagHelperOutput output, string nameWithLeadingSpace, string? value)
    {
        var postElement = output.PostElement;
        postElement.AppendHtml(nameWithLeadingSpace);
        if (!string.IsNullOrEmpty(value))
        {
            postElement.AppendHtml("=\"");
            postElement.Append(value);
            postElement.AppendHtml("\"");
        }
    }
}
