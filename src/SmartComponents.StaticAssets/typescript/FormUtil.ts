export function setFormElementValueWithEvents(elem: HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement, value: string | boolean) {
    if (elem instanceof HTMLSelectElement) {
        const valueToString = value.toString();
        const newSelectedIndex = findSelectOptionByText(elem, valueToString);
        if (newSelectedIndex !== null && elem.selectedIndex !== newSelectedIndex) {
            notifyFormElementBeforeWritten(elem);
            elem.selectedIndex = newSelectedIndex;
            notifyFormElementWritten(elem);
        }
    } else if (elem instanceof HTMLInputElement && (elem.type === 'radio' || elem.type === 'checkbox')) {
        const valueStringLower = value?.toString().toLowerCase();
        const shouldCheck = (valueStringLower === "true") || (valueStringLower === "yes") || (valueStringLower === "on");
        if (elem && elem.checked !== shouldCheck) {
            notifyFormElementBeforeWritten(elem);
            elem.checked = shouldCheck;
            notifyFormElementWritten(elem);
        }
    } else {
        if (isComboBox(elem)) {
            // TODO: Support datalist by interpreting it as a set of allowed values. When populating
            // the form, only accept suggestions that match one of the allowed values.
            return;
        }

        value = value.toString();
        if (elem.value !== value) {
            notifyFormElementBeforeWritten(elem);
            elem.value = value;
            notifyFormElementWritten(elem);
        }
    }
}

export function isComboBox(elem): boolean {
    return !!(elem.list || elem.getAttribute('data-autocomplete'));
}

// Client-side code (e.g., validation) may react when an element value is changed
// We'll trigger the same kinds of events that fire if you type
function notifyFormElementBeforeWritten(elem: HTMLElement) {
    elem.dispatchEvent(new CustomEvent('beforeinput', { bubbles: true, detail: { fromSmartComponents: true } }));
}

function notifyFormElementWritten(elem: HTMLElement) {
    elem.dispatchEvent(new CustomEvent('input', { bubbles: true, detail: { fromSmartComponents: true } }));
    elem.dispatchEvent(new CustomEvent('change', { bubbles: true, detail: { fromSmartComponents: true } }));
}

function findSelectOptionByText(selectElem: HTMLSelectElement, valueText: string): number | null {
    const options = Array.from(selectElem.querySelectorAll('option'));
    const exactMatches = options.filter(o => o.textContent === valueText);
    if (exactMatches.length > 0) {
        return options.indexOf(exactMatches[0]);
    }

    const partialMatches = options.filter(o => o.textContent && o.textContent.indexOf(valueText) >= 0);
    if (partialMatches.length === 1) {
        return options.indexOf(partialMatches[0]);
    }

    return null;
}
