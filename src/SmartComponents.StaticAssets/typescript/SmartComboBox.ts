import { setFormElementValueWithEvents } from './FormUtil';

export function registerSmartComboBoxCustomElement() {
    customElements.define('smart-combobox', SmartComboBox);
}

class SmartComboBox extends HTMLElement {
    inputElem: HTMLInputElement;
    requestSuggestionsTimeout = 0;
    debounceKeystrokesDelay = 250;
    currentAbortController: AbortController | null = null;
    selectedIndex = 0;
    static nextSuggestionsElemId = 0;

    connectedCallback() {
        this.inputElem = this.previousElementSibling as HTMLInputElement;
        if (!(this.inputElem instanceof HTMLInputElement)) {
            throw new Error('smart-combobox must be placed immediately after an input element');
        }
 
        this.id = `smartcombobox-suggestions-${SmartComboBox.nextSuggestionsElemId++}`;
        this.classList.add('smartcombobox-suggestions');
        this.addEventListener('mousedown', event => {
            if (event.target instanceof HTMLElement && event.target.classList.contains('smartcombobox-suggestion')) {
                this._handleSuggestionSelected(event.target);
            }
        });

        this.inputElem.setAttribute('aria-controls', this.id);
        this._setSuggestions([]);

        this.inputElem.addEventListener('keydown', event => {
            if (event.key === 'ArrowUp') {
                event.preventDefault();
                this._updateSelection({ offset: -1, updateInputToMatch: true });
            } else if (event.key === 'ArrowDown') {
                event.preventDefault();
                this._updateSelection({ offset: 1, updateInputToMatch: true });
            } else if (event.key === 'Enter') {
                event.preventDefault();
                const suggestion = this.children[this.selectedIndex] as HTMLElement;
                if (suggestion) {
                    this._handleSuggestionSelected(suggestion);
                }
            }
        });

        this.inputElem.addEventListener('input', event => {
            if (event instanceof CustomEvent && event.detail.fromSmartComponents) {
                return; // When we triggered the update programmatically, that's not a reason to fetch more suggestions
            }

            clearTimeout(this.requestSuggestionsTimeout);
            this.currentAbortController?.abort();
            this.currentAbortController = null;

            if (this.inputElem.value === '') {
                this._setSuggestions([]);
            } else {
                this.requestSuggestionsTimeout = setTimeout(() => {
                    this._requestSuggestions();
                }, this.debounceKeystrokesDelay);
            }
        });

        this.inputElem.addEventListener('focus', () => this._updateAriaStates());
        this.inputElem.addEventListener('blur', () => this._updateAriaStates());
    }

    async _requestSuggestions() {
        this.currentAbortController = new AbortController();

        const body = {
            inputValue: this.inputElem.value,
            maxResults: this.getAttribute('data-max-suggestions'),
            similarityThreshold: this.getAttribute('data-similarity-threshold'),
        };

        const antiforgeryName = this.getAttribute('data-antiforgery-name');
        if (antiforgeryName) {
            body[antiforgeryName] = this.getAttribute('data-antiforgery-value');
        }

        let response: Response;
        const requestInit: RequestInit = {
            method: 'post',
            headers: {
                'content-type': 'application/x-www-form-urlencoded',
            },
            body: new URLSearchParams(body),
            signal: this.currentAbortController.signal,
        };

        try {
            // We rely on the URL being pathbase-relative for Blazor, or a ~/... URL that would already
            // be resolved on the server for MVC
            response = await fetch(this.getAttribute('data-suggestions-url'), requestInit);
            const suggestions: string[] = await response.json();
            this._setSuggestions(suggestions);
        }
        catch (ex) {
            if (ex instanceof DOMException && ex.name === 'AbortError') {
                return;
            }

            throw ex;
        }
    }

    _setSuggestions(suggestions: string[]) {
        while (this.firstElementChild) {
            this.firstElementChild.remove();
        }

        let optionIndex = 0;
        suggestions.forEach(choice => {
            const option = document.createElement('div');
            option.id = `${this.id}_item${optionIndex++}`;
            option.setAttribute('role', 'option');
            option.setAttribute('aria-selected', 'false');
            option.classList.add('smartcombobox-suggestion');
            option.textContent = choice;
            this.appendChild(option);
        });

        if (suggestions.length) {
            this._updateSelection({ suggestion: this.children[0] as HTMLElement });
            this.style.display = null; // Allow visibility to be controlled by focus rule in CSS

            // We rely on the input not moving relative to its offsetParent while the suggestions
            // are visible. Developers can always put the input directly inside a relatively-positioned
            // container if they need this to work on a fine-grained basis.
            this.style.top = this.inputElem.offsetTop + this.inputElem.offsetHeight + 'px';
            this.style.left = this.inputElem.offsetLeft + 'px';
            this.style.width = this.inputElem.offsetWidth + 'px';
        } else {
            this.style.display = 'none';
        }

        this._updateAriaStates();
    }

    _updateAriaStates() {
        // aria-expanded
        const isExpanded = this.firstChild && document.activeElement === this.inputElem;
        this.inputElem.setAttribute('aria-expanded', isExpanded ? 'true' : 'false');

        // aria-activedescendant
        const suggestion = isExpanded && this.children[this.selectedIndex] as HTMLElement;
        if (!suggestion) {
            this.inputElem.removeAttribute('aria-activedescendant');
        } else {
            this.inputElem.setAttribute('aria-activedescendant', suggestion.id);
        }
    }

    _handleSuggestionSelected(suggestion: HTMLElement) {
        this._updateSelection({ suggestion, updateInputToMatch: true });
        this.inputElem.blur();
    }

    _updateSelection(operation: { offset?: number, suggestion?: HTMLElement, updateInputToMatch?: boolean }) {
        let suggestion = operation.suggestion;
        if (suggestion) {
            this.selectedIndex = Array.from(this.children).indexOf(suggestion);
        } else {
            if (isNaN(operation.offset)) {
                throw new Error('Supply either offset or selection element');
            }

            const newIndex = Math.max(0, Math.min(this.children.length - 1, this.selectedIndex + operation.offset));
            if (newIndex === this.selectedIndex) {
                return;
            }

            this.selectedIndex = newIndex;
            suggestion = this.children[newIndex] as HTMLElement;
        }

        const prevSelectedSuggestion = this.querySelector('.selected');
        if (prevSelectedSuggestion === suggestion && this.inputElem.value === suggestion.textContent) {
            return;
        }

        prevSelectedSuggestion?.setAttribute('aria-selected', 'false');
        prevSelectedSuggestion?.classList.remove('selected');
        suggestion.setAttribute('aria-selected', 'true');
        suggestion.classList.add('selected');

        if (suggestion['scrollIntoViewIfNeeded']) {
            suggestion['scrollIntoViewIfNeeded'](false);
        } else {
            // Firefox doesn't support scrollIntoViewIfNeeded, so we fall back on scrollIntoView.
            // This will align the top of the suggestion with the top of the scrollable area.
            suggestion.scrollIntoView();
        }

        this._updateAriaStates();

        if (operation.updateInputToMatch) {
            setFormElementValueWithEvents(this.inputElem, suggestion.textContent || '');
        }
    }
}
