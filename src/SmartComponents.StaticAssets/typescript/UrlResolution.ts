export function resolveUrl(component: Element, pathbaseRelativeUrl: string) {
    // For MVC/Razor Pages, the server emits a data-pathbase attribute.
    // Blazor doesn't need that because it can rely on the page defining a base href.
    const dataPathBase = component.getAttribute('data-pathbase');
    if (dataPathBase) {
        const base = location.origin
            + (dataPathBase.endsWith('/') ? dataPathBase : dataPathBase + '/');
        return new URL(pathbaseRelativeUrl, base).toString();
    }

    return new URL(pathbaseRelativeUrl, document.baseURI).toString();
}
