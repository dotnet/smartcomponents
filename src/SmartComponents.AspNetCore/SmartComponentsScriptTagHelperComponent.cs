// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace SmartComponents.AspNetCore;

internal sealed class SmartComponentsScriptTagHelperComponent : TagHelperComponent
{
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (string.Equals(context.TagName, "body", StringComparison.OrdinalIgnoreCase))
        {
            var httpContext = ViewContext.HttpContext;
            var fileVersionProvider = httpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
            var pathBase = httpContext.Request.PathBase;
            var relativeSrc = UriHelper.BuildRelative(
                pathBase,
                "/_content/SmartComponents.AspNetCore.Components/SmartComponents.AspNetCore.Components.lib.module.js");
            var srcWithFileVersion = fileVersionProvider.AddFileVersionToPath(pathBase, relativeSrc);

            output.PostContent.AppendHtml("<script type=\"module\" src=\"");
            output.PostContent.Append(srcWithFileVersion);
            output.PostContent.AppendHtml("\"></script>");
        }

        return Task.CompletedTask;
    }
}
