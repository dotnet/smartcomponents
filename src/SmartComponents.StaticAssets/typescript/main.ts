console.log('Hello from main.ts');

document.body.addEventListener('click', (event: MouseEvent) => {
    if (event.target instanceof Element) {
        const testComponent = event.target.closest('.my-component');
        if (testComponent) {
            performServerRequest(testComponent);
        }
    }
});

async function performServerRequest(testComponent: Element) {
    const response = await fetch(resolveUrl(testComponent, '_example'));
    const responseText = await response.text();
    testComponent.textContent = responseText;
}

function resolveUrl(component: Element, pathbaseRelativeUrl: string) {
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
