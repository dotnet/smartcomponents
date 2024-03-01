// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace SmartComponents.AspNetCore;

[HtmlTargetElement("smart-paste-button", TagStructure = TagStructure.NormalOrSelfClosing)]
public class SmartPasteButtonTagHelper : TagHelper
{
    [ViewContext]
    public ViewContext ViewContext { get; set; } = default!;

    public bool DefaultIcon { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "button";
        output.Attributes.Add("type", "button");
        output.Attributes.Add("title", "Use content on the clipboard to fill out the form");
        output.Attributes.Add("data-smart-paste-trigger", "true");

        var services = ViewContext.HttpContext.RequestServices;
        var urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(ViewContext);
        output.Attributes.Add("data-url", urlHelper.Content("~/_smartcomponents/smartpaste"));

        var antiforgery = services.GetRequiredService<IAntiforgery>();
        if (antiforgery is not null)
        {
            var tokens = antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
            output.Attributes.Add("data-antiforgery-name", tokens.FormFieldName);
            output.Attributes.Add("data-antiforgery-value", tokens.RequestToken);
        }

        if (!output.Attributes.ContainsName("class"))
        {
            output.AddClass("smart-paste-button", HtmlEncoder.Default);
        }

        if (DefaultIcon)
        {
            output.PreContent.SetHtmlContent(@"
<svg class=""smart-paste-icon smart-paste-icon-normal"" fill=""currentColor"" viewBox=""-164, 0, 1460, 1560"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xml:space=""preserve"" overflow=""hidden""><defs><clipPath id=""clip0""><rect x=""611"" y=""580"" width=""1131"" height=""1460"" /></clipPath></defs><g clip-path=""url(#clip0)"" transform=""translate(-611 -580)""><path d=""M1106.74 1792.99 1246.26 1792.99C1243.04 1829.5 1212.99 1857.42 1176.5 1857.42 1140.01 1857.42 1109.96 1829.5 1106.74 1792.99ZM1069.17 1685.61 1283.83 1685.61C1302.08 1685.61 1316.03 1699.57 1316.03 1717.82 1316.03 1736.08 1302.08 1750.04 1283.83 1750.04L1069.17 1750.04C1050.92 1750.04 1036.97 1736.08 1036.97 1717.82 1036.97 1699.57 1050.92 1685.61 1069.17 1685.61ZM1069.17 1578.23 1283.83 1578.23C1302.08 1578.23 1316.03 1592.19 1316.03 1610.45 1316.03 1628.7 1302.08 1642.66 1283.83 1642.66L1069.17 1642.66C1050.92 1642.66 1036.97 1628.7 1036.97 1610.45 1036.97 1592.19 1050.92 1578.23 1069.17 1578.23ZM1177.57 1018.8C1060.58 1019.87 965.06 1114.36 962.913 1231.41L962.913 1240C963.987 1265.77 968.28 1291.54 977.939 1315.16 986.526 1336.64 999.406 1357.04 1014.43 1374.22 1038.04 1404.28 1059.51 1436.5 1076.68 1470.86L1176.5 1470.86 1277.39 1470.86C1293.49 1436.5 1314.96 1404.28 1339.64 1374.22 1355.74 1357.04 1367.55 1336.64 1376.13 1315.16 1384.72 1291.54 1390.09 1265.77 1391.16 1240L1392.23 1240 1392.23 1231.41C1390.09 1113.29 1294.56 1019.87 1177.57 1018.8ZM1176.5 955.445C1328.91 956.519 1452.34 1078.93 1455.56 1231.41L1455.56 1241.07C1454.48 1274.36 1448.04 1306.57 1436.24 1337.71 1425.51 1366.7 1408.33 1393.55 1387.94 1417.17 1362.18 1445.09 1334.27 1499.85 1322.47 1523.47 1319.25 1530.99 1311.74 1535.28 1303.15 1535.28L1049.85 1535.28C1041.26 1535.28 1033.75 1530.99 1030.53 1523.47 1018.72 1499.85 990.819 1445.09 965.06 1417.17 944.667 1393.55 928.568 1366.7 916.761 1337.71 904.955 1306.57 898.515 1274.36 897.442 1241.07L897.442 1231.41C900.662 1078.93 1024.09 956.519 1176.5 955.445ZM692.383 779.941 692.383 1965.49 1660.62 1965.49 1660.62 779.941 1425.21 779.941 1425.21 867.98 927.791 867.98 927.791 779.941ZM1176.5 653C1161.91 653 1147.31 658.475 1138.19 669.425 1127.25 678.55 1121.77 693.15 1121.77 707.75 1121.77 738.775 1145.49 762.5 1176.5 762.5 1207.51 762.5 1231.23 738.775 1231.23 707.75 1231.23 676.724 1207.51 653 1176.5 653ZM1067.05 580 1285.95 580C1326.08 580 1358.92 612.85 1358.92 653L1358.92 689.5 1669.03 689.5C1709.16 689.5 1742 722.35 1742 762.5L1742 1967C1742 2007.15 1709.16 2040 1669.03 2040L683.968 2040C643.835 2040 611 2007.15 611 1967L611 762.5C611 722.35 643.835 689.5 683.968 689.5L994.08 689.5 994.08 653C994.08 612.85 1026.92 580 1067.05 580Z"" fill-rule=""evenodd"" /></g></svg>
        <svg class=""smart-paste-icon smart-paste-icon-running"" viewBox=""0 0 24 24"" xmlns=""http://www.w3.org/2000/svg""><g stroke=""currentColor""><circle cx=""12"" cy=""12"" r=""9.5"" fill=""none"" stroke-width=""3"" stroke-linecap=""round""><animate attributeName=""stroke-dasharray"" dur=""1.5s"" calcMode=""spline"" values=""0 150;42 150;42 150;42 150"" keyTimes=""0;0.475;0.95;1"" keySplines=""0.42,0,0.58,1;0.42,0,0.58,1;0.42,0,0.58,1"" repeatCount=""indefinite"" /><animate attributeName=""stroke-dashoffset"" dur=""1.5s"" calcMode=""spline"" values=""0;-16;-59;-59"" keyTimes=""0;0.475;0.95;1"" keySplines=""0.42,0,0.58,1;0.42,0,0.58,1;0.42,0,0.58,1"" repeatCount=""indefinite"" /></circle><animateTransform attributeName=""transform"" type=""rotate"" dur=""2s"" values=""0 12 12;360 12 12"" repeatCount=""indefinite"" /></g></svg>
");
        }

        if (output.TagMode == TagMode.StartTagAndEndTag
            && (await output.GetChildContentAsync()) is { } content
            && (content is { IsEmptyOrWhiteSpace: false }))
        {
            output.Content = content;
        }
        else
        {
            // Default content. In most cases we expect apps to override this, since that's how
            // it would be localized.
            output.Content.SetHtmlContent("Smart Paste");
            output.TagMode = TagMode.StartTagAndEndTag;
        }
    }
}
