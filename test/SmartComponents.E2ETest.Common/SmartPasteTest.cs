namespace SmartComponents.E2ETest.Common;

public class SmartPasteTest<TStartup> : PlaywrightTestBase<TStartup> where TStartup : class
{
    public SmartPasteTest(KestrelWebApplicationFactory<TStartup> server) : base(server)
    {
    }

    protected override async Task OnBrowserReadyAsync()
    {
        await Page.GotoAsync(Server.Address + "/smartpaste");
        await Page.Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"]);
    }

    [Fact]
    public async Task CanPopulateTextBoxes()
    {
        var form = Page.Locator("#simple-case");
        await Expect(form.Locator("[name=firstname]")).ToBeEmptyAsync();
        await Expect(form.Locator("[name=lastname]")).ToBeEmptyAsync();

        await SetClipboardContentsAsync("Rahul Mandal");

        await form.Locator(".smart-paste-button").ClickAsync();
        await Expect(form.Locator("[name=firstname]")).ToHaveValueAsync("Rahul");
        await Expect(form.Locator("[name=lastname]")).ToHaveValueAsync("Mandal");
    }

    [Fact]
    public async Task LeavesInputsUnchangedWhenThereIsNoMatchingData()
    {
        var form = Page.Locator("#simple-case");
        var unrelatedValue = await form.Locator("[name=unrelated]").InputValueAsync();
        Assert.NotEmpty(unrelatedValue);

        await SetClipboardContentsAsync("Rahul Mandal");

        await form.Locator(".smart-paste-button").ClickAsync();
        await Expect(form.Locator("[name=firstname]")).ToHaveValueAsync("Rahul");
        await Expect(form.Locator("[name=unrelated]")).ToHaveValueAsync(unrelatedValue);
    }

    [Fact]
    public async Task HasDefaultCssClassAndTitleAndContent()
    {
        var button = Page.Locator("#default-params");
        await Expect(button).ToHaveAttributeAsync("class", "smart-paste-button");
        await Expect(button).ToHaveAttributeAsync("title", "Use content on the clipboard to fill out the form");
        await Expect(button).ToHaveTextAsync("Smart Paste");
        await Expect(button.Locator("svg")).ToHaveCountAsync(0); // No icon by default
    }

    [Fact]
    public Task CanOverrideCssClass()
        => Expect(Page.Locator("#custom-css-class"))
        .ToHaveAttributeAsync("class", "my-custom-class"); // Note the absence of smart-paste-button

    [Fact]
    public Task CanOverrideTooltip()
        => Expect(Page.Locator("#custom-tooltip"))
        .ToHaveAttributeAsync("title", "This is the tooltip");

    [Fact]
    public async Task CanHaveDefaultIcon()
    {
        var button = Page.Locator("#with-icon");
        
        var normalIcon = button.Locator("svg.smart-paste-icon.smart-paste-icon-normal");
        var runningIcon = button.Locator("svg.smart-paste-icon.smart-paste-icon-running");

        // Only the normal icon should be visible by default
        await Expect(normalIcon).ToHaveCSSAsync("display", "inline");
        await Expect(runningIcon).ToHaveCSSAsync("display", "none");

        // ... but when the button is disabled, they switch
        await button.EvaluateAsync("b => b.disabled = true");
        await Expect(normalIcon).ToHaveCSSAsync("display", "none");
        await Expect(runningIcon).ToHaveCSSAsync("display", "inline");
    }

    [Fact]
    public async Task CanHaveChildContent()
    {
        var button = Page.Locator("#with-child-content");
        await Expect(button).ToHaveTextAsync("This is my custom content");
        await Expect(button.Locator("strong")).ToHaveTextAsync("custom");
    }

    [Fact]
    public async Task CanHaveDefaultIconAndChildContent()
    {
        var button = Page.Locator("#with-icon-and-child-content");
        await Expect(button.Locator("svg.smart-paste-icon.smart-paste-icon-normal")).ToHaveCountAsync(1);
        await Expect(button.Locator("svg.smart-paste-icon.smart-paste-icon-running")).ToHaveCountAsync(1);
        await Expect(button).ToHaveTextAsync("This is my custom content");
        await Expect(button.Locator("strong")).ToHaveTextAsync("custom");
    }

    [Fact]
    public async Task CanPopulateAllFormFieldTypes()
    {
        var form = Page.Locator("#element-types");

        await SetClipboardContentsAsync("AI: Artificial Intelligence (2001, director: Steven Spielberg) is a sci-fi movie about a robot boy who desperately wants to be human. The tragedy at the heart of the film, though, is star Haley Joel Osment’s immortality. He was designed as a child, but outlives everyone he ever loves. Available now through streaming services.");
        await form.Locator(".smart-paste-button").ClickAsync();

        await Expect(form.Locator("[name='movie.title']")).ToHaveValueAsync("AI: Artificial Intelligence");
        await Expect(form.Locator("[name='movie.release_year']")).ToHaveValueAsync("2001");
        await Expect(form.Locator("[name='movie_genre']")).ToHaveValueAsync("sci");
        await Expect(form.Locator("[name='movie.for_kids']")).Not.ToBeCheckedAsync();
        await Expect(form.Locator("[name='movie.can_stream']")).ToBeCheckedAsync();
        await Expect(form.Locator("[name='movie.starring']:checked")).ToHaveValueAsync("hjo");

        var description = await form.Locator("[name='movie.description']").InputValueAsync();
        Assert.Contains("wants to be human", description);
    }

    [Fact]
    public async Task CanInferFieldDescriptions()
    {
        var form = Page.Locator("#inferring-descriptions");
        await SetClipboardContentsAsync(@"
            Hairstyle: Tonsure
            Metal band: Sad Iron
            Philosophy: Nihilism
            City: Cairns
            Shoe size: 55
        ");
        await form.Locator(".smart-paste-button").ClickAsync();

        await Expect(form.Locator("[name='explicitly-annotated']")).ToHaveValueAsync("Cairns");
        await Expect(form.Locator("#labelled-field")).ToHaveValueAsync("Sad Iron");
        await Expect(form.Locator("[name='inferred-from-nearby-text']")).ToHaveValueAsync("Tonsure");
        await Expect(form.Locator("[name='shoe-size']")).ToHaveValueAsync("55");
        await Expect(form.Locator("#philosophy")).ToHaveValueAsync("Nihilism");
    }

    protected Task SetClipboardContentsAsync(string text)
        => Page.Locator("html").EvaluateAsync("(ignored, value) => navigator.clipboard.writeText(value)", text);
}
