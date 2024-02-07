import { registerSmartPasteClickHandler } from './SmartPaste';
import { resolveUrl } from './UrlResolution';

registerSmartPasteClickHandler();

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
