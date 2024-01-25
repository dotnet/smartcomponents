using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace SmartComponents.AspNetCore;

[HtmlTargetElement("test-component", TagStructure = TagStructure.NormalOrSelfClosing)]
public class TestComponent : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("my-component", HtmlEncoder.Default);
        output.Content.Append("This is a test component. TODO: Real ones.");
    }
}
