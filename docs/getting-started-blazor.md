# Getting started with .NET Smart Components in Blazor

1. **Create a new Blazor project or use an existing one (.NET 6 or later).**

   * Command line: Run `dotnet new blazor`
   * Visual Studio: Select *File*->*New*->*Project...* then choose *Blazor Web App*

   **Note**: .NET Smart Components work equally in any render mode (e.g., Static SSR, Server, or WebAssembly) but you do need to have an ASP.NET Core server, so you cannot use a Blazor WebAssembly Standalone App hosted on a static file server. This is purely because you need a server to hold your API keys securely.

1. **Reference the .NET Smart Components from your app**

   * If you haven't already, clone this repo locally.
   * Add the `SmartComponents.AspNetCore` project from this repo to your solution and reference it from your **server** project.
   * If you also have a **WebAssembly** client project, add the `SmartComponents.AspNetCore.Components` project from this repo to your solution and reference it from your client project. This is not required if you only have a server project.

1. **Configure the .NET Smart Components in your app**

   In your server's `Program.cs`, under the comment `// Add services to the container`, add:

   ```cs
   builder.Services.AddSmartComponents();
   ```

1. **Configure the OpenAI backend** (if needed)

   If you will be using either `SmartPaste` or `SmartTextArea`, you need to provide access to a language model backend. See: [Configure the OpenAI backend](configure-openai-backend.md).
   
   If you will only use `SmartComboBox`, you don't need any language model backend and can skip this step.

1. **Add components to your pages**

   You can now add the following inside your Blazor pages/components:

   * [SmartPaste](smart-paste.md)
   * [SmartTextArea](smart-textarea.md)
   * [SmartComboBox](smart-combobox.md)
