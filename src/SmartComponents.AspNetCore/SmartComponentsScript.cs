using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace SmartComponents.AspNetCore;

[HtmlTargetElement("smart-components-script", TagStructure = TagStructure.NormalOrSelfClosing)]
public class SmartComponentsScript : TagHelper
{
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var httpContext = ViewContext.HttpContext;
        var fileVersionProvider = httpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
        var pathBase = httpContext.Request.PathBase;
        var relativeSrc = UriHelper.BuildRelative(
            pathBase: pathBase,
            "/_content/SmartComponents.AspNetCore.Components/SmartComponents.AspNetCore.Components.lib.module.js");
        var srcWithFileVersion = fileVersionProvider.AddFileVersionToPath(pathBase, relativeSrc);

        output.TagName = "script";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Add("src", srcWithFileVersion);
        output.Content.Clear();
    }
}
