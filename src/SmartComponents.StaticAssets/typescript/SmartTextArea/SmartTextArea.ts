import { SuggestionDisplay } from './SuggestionDisplay';
import { InlineSuggestionDisplay } from './InlineSuggestionDisplay';
import { OverlaySuggestionDisplay } from './OverlaySuggestionDisplay';

export function registerSmartTextAreaCustomElement() {
    customElements.define('smart-textarea', SmartTextArea);
}

export class SmartTextArea extends HTMLElement {
    typingDebounceTimeout: number | null = null;
    textArea: HTMLTextAreaElement;
    suggestionDisplay: SuggestionDisplay;
    pendingSuggestionAbortController?: AbortController;

    connectedCallback() {
        if (!(this.previousElementSibling instanceof HTMLTextAreaElement)) {
            throw new Error('smart-textarea must be rendered immediately after a textarea element');
        }

        this.textArea = this.previousElementSibling as HTMLTextAreaElement;
        this.suggestionDisplay = shouldUseInlineSuggestions(this.textArea)
            ? new InlineSuggestionDisplay(this, this.textArea)
            : new OverlaySuggestionDisplay(this, this.textArea);

        this.textArea.addEventListener('keydown', e => this.handleKeyDown(e));
        this.textArea.addEventListener('keyup', e => this.handleKeyUp(e));
        this.textArea.addEventListener('mousedown', () => this.removeExistingOrPendingSuggestion());
        this.textArea.addEventListener('focusout', () => this.removeExistingOrPendingSuggestion());

        // If you scroll, we don't need to kill any pending suggestion request, but we do need to hide
        // any suggestion that's already visible because the fake cursor will now be in the wrong place
        this.textArea.addEventListener('scroll', () => this.suggestionDisplay.reject(), { passive: true });
    }

    handleKeyDown(event: KeyboardEvent) {
        switch (event.key) {
            case 'Tab':
                if (this.suggestionDisplay.isShowing()) {
                    this.suggestionDisplay.accept();
                    event.preventDefault();
                }
                break;
            case 'Alt':
            case 'Control':
            case 'Shift':
            case 'Command':
                break;
            default:
                this.removeExistingOrPendingSuggestion();
                break;
        }
    }

    // If this was changed to a 'keypress' event instead, we'd only initiate suggestions after
    // the user types a visible character, not pressing another key (e.g., arrows, or ctrl+c).
    // However for now I think it is desirable to show suggestions after cursor movement.
    handleKeyUp(event: KeyboardEvent) {
        clearTimeout(this.typingDebounceTimeout);
        this.typingDebounceTimeout = setTimeout(() => this.handleTypingPaused(), 350);
    }

    handleTypingPaused() {
        if (document.activeElement !== this.textArea) {
            return;
        }

        // We only show a suggestion if the cursor is at the end of the current line. Inserting suggestions in
        // the middle of a line is confusing (things move around in unusual ways).
        // TODO: You could also allow the case where all remaining text on the current line is whitespace
        const isAtEndOfCurrentLine = this.textArea.selectionStart === this.textArea.selectionEnd
            && (this.textArea.selectionStart === this.textArea.value.length || this.textArea.value[this.textArea.selectionStart] === '\n');
        if (!isAtEndOfCurrentLine) {
            return;
        }

        this.requestSuggestionAsync();
    }

    removeExistingOrPendingSuggestion() {
        this.pendingSuggestionAbortController?.abort();
        this.pendingSuggestionAbortController = null;

        this.suggestionDisplay.reject();
    }

    async requestSuggestionAsync() {
        this.pendingSuggestionAbortController?.abort();
        this.pendingSuggestionAbortController = new AbortController();

        const snapshot = {
            abortSignal: this.pendingSuggestionAbortController.signal,
            textAreaValue: this.textArea.value,
            cursorPosition: this.textArea.selectionStart,
        };

        const requestInit: RequestInit = {
            method: 'post',
            headers: {
                'content-type': 'application/x-www-form-urlencoded',
            },
            body: new URLSearchParams({
                [this.getAttribute('data-antiforgery-name')]: this.getAttribute('data-antiforgery-value'),

                // TODO: Limit the amount of text we send, e.g., to 100 characters before and after the cursor
                textBefore: snapshot.textAreaValue.substring(0, snapshot.cursorPosition),
                textAfter: snapshot.textAreaValue.substring(snapshot.cursorPosition),
                config: this.getAttribute('data-config'),
            }),
            signal: snapshot.abortSignal,
        };

        let suggestionText: string;
        try {
            // We rely on the URL being pathbase-relative for Blazor, or a ~/... URL that would already
            // be resolved on the server for MVC
            const httpResponse = await fetch(this.getAttribute('data-url'), requestInit);
            suggestionText = httpResponse.ok ? await httpResponse.text() : null;
        } catch (ex) {
            if (ex instanceof DOMException && ex.name === 'AbortError') {
                return;
            }
        }

        // Normally if the user has made further edits in the textarea, our HTTP request would already
        // have been aborted so we wouldn't get here. But if something else (e.g., some other JS code)
        // mutates the textarea, we would still get here. It's important we don't apply the suggestion
        // if the textarea value or cursor position has changed, so compare against our snapshot.
        if (suggestionText
            && snapshot.textAreaValue === this.textArea.value
            && snapshot.cursorPosition === this.textArea.selectionStart) {
            if (!suggestionText.endsWith(' ')) {
                suggestionText += ' ';
            }

            this.suggestionDisplay.show(suggestionText);
        }
    }
}

function shouldUseInlineSuggestions(textArea: HTMLTextAreaElement): boolean {
    // Allow the developer to specify this explicitly if they want
    const explicitConfig = textArea.getAttribute('data-inline-suggestions');
    if (explicitConfig) {
        return explicitConfig.toLowerCase() === 'true';
    }

    // ... but by default, we use overlay on touch devices, inline on non-touch devices
    // That's because:
    //  - Mobile devices will be touch, and most mobile users don't have a "tab" key by which to accept inline suggestions
    //  - Mobile devices such as iOS will display all kinds of extra UI around selected text (e.g., selection handles),
    //    which would look completely wrong
    // In general, the overlay approach is the risk-averse one that works everywhere, even though it's not as attractive.
    const isTouch = 'ontouchstart' in window; // True for any mobile. Usually not true for desktop.
    return !isTouch;
}
