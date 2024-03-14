import { registerSmartComboBoxCustomElement } from './SmartComboBox';
import { registerSmartPasteClickHandler } from './SmartPaste';
import { registerSmartTextAreaCustomElement } from './SmartTextArea/SmartTextArea';

// Only run this script once. If you import it multiple times, the 2nd-and-later are no-ops.
const isLoadedMarker = '__smart_components_loaded__';
if (!Object.getOwnPropertyDescriptor(document, isLoadedMarker)) {
    Object.defineProperty(document, isLoadedMarker, { enumerable: false, writable: false });

    registerSmartComboBoxCustomElement();
    registerSmartPasteClickHandler();
    registerSmartTextAreaCustomElement();
}
