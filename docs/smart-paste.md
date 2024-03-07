# Smart Paste

Smart Paste is an intelligent app feature that fills out forms automatically using data from the user's clipboard. You can use this with any existing form in your web app.

### Example use cases

 * **Mailing address form**
 
   A user could copy a whole mailing address from an email or Word document, and then click "Smart Paste" in your application to populate all the address-related fields in a form (name, line 1, line 2, city, state, etc.).
 
   This reduces the workload on your user, because they don't have to type out each field manually, or separately copy-and-paste each field.

 * **Bug tracking form**
 
   A user could copy a short natural-language description of a problem (perhaps sent to them via IM/Teams), and then click "Smart Paste" inside your "Create New Issue" page. This would populate fields like "Title", "Severity", "Repro steps", etc., based on the clipboard text.

   The language model will automatically rephrase the source text as needed. For example, it would convert phrases like "I just clicked X on screen Y" to repro steps like "1. Go to screen Y, 2. Click X.".

Smart Paste is designed to work with any form. You don't have to configure or annotate your forms, since the system will infer the meanings of the fields from your HTML. You can optionally provide annotations if it helps to produce better results.

## Prerequisites

First, make sure you've followed the Smart Components installation steps, depending on which UI framework you're using:

 * [Getting started with Smart Components in Blazor](getting-started-blazor.md)
 * [Getting started with Smart Components in MVC / Razor Pages](getting-started-mvc-razor-pages.md)

This includes [configuring an OpenAI backend](configure-openai-backend.md), which is a prerequisite for Smart Paste.

## Adding SmartPaste in Blazor

In a `.razor` file, inside any `<form>` or `<EditForm>`, add the `<SmartPasteButton>` component. Example:

```razor
@page "/"
@using SmartComponents

<form>
    <p>Name: <InputText @bind-Value="@name" /></p>
    <p>Address line 1: <InputText @bind-Value="@addr1" /></p>
    <p>City: <InputText @bind-Value="@city" /></p>
    <p>Zip/postal code: <InputText @bind-Value="@zip" /></p>

    <button type="submit">Submit</button>
    <SmartPasteButton DefaultIcon />
</form>

@code {
    string? name, addr1, city, zip;
}
```

Now when this app is run, you can copy a mailing address to your clipboard from some other application, and then click the "Smart Paste" button to fill out all the corresponding form fields.

Note: this example is only intended to show `SmartPasteButton`. This form won't do anything useful if submitted - see [Blazor docs for more about form submissions](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms).

## Adding SmartPaste in MVC / Razor Pages

In any page/view `.cshtml` file, inside any `<form>`, add a `<smart-paste-button>` tag. Example:

```cshtml
<form>
    <p>Name: <input name="name" /></p>
    <p>Address line 1: <input name="addr1" /></p>
    <p>City: <input name="city" /></p>
    <p>Zip/postal code: <input name="zip" /></p>

    <button type="submit">Submit</button>
    <smart-paste-button default-icon />
</form>
```

Now when this app is run, you can copy a mailing address to your clipboard from some other application, and then click the "Smart Paste" button to fill out all the corresponding form fields.

