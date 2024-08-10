# Getting started with .NET Smart Components in MVC / Razor Pages

1. **Create a new ASP.NET Core Web App project or use an existing one (.NET 6 or later).**

   * Command line: Run `dotnet new mvc` or `dotnet new razor`
   * Visual Studio: Select *File*->*New*->*Project...* then choose *ASP.NET Core Web App (Model-View-Controller or Razor pages)*

1. **Reference the .NET Smart Components from your app**

   * If you haven't already, clone this repo locally.
   * Add the `SmartComponents.AspNetCore` project from this repo to your solution and reference it from your app.

1. **Configure the .NET Smart Components in your app**

   a. In `Program.cs`, under the comment `// Add services to the container`, add:

   ```cs
   builder.Services.AddSmartComponents();
   ```

   b. In your `_ViewImports.cshtml` file (in the `Pages` or `Views` folder), reference the tag helpers:

   ```cshtml
   @addTagHelper *, SmartComponents.AspNetCore
   ```

1. **Configure the OpenAI backend** (if needed)

   If you will be using either `smart-paste-button` or `smart-textarea`, you need to provide access to a language model backend. See: [Configure the OpenAI backend](configure-openai-backend.md).
   
   If you will only use `smart-combobox`, you don't need any language model backend and can skip this step.

1. **Add .NET Smart Components to your pages**

   You can now add the following inside your views/pages:

   * [smart-paste-button](smart-paste.md)
   * [smart-textarea](smart-textarea.md)
   * [smart-combobox](smart-combobox.md)
