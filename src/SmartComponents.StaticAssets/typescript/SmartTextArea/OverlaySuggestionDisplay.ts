import { SuggestionDisplay } from './SuggestionDisplay';
import { SmartTextArea } from './SmartTextArea';
import { getCaretOffsetFromOffsetParent, scrollTextAreaDownToCaretIfNeeded } from './CaretUtil';

export class OverlaySuggestionDisplay implements SuggestionDisplay {
    suggestionElement: HTMLDivElement;
    showing: boolean;

    constructor(owner: SmartTextArea, private textArea: HTMLTextAreaElement) {
        this.suggestionElement = document.createElement('div');
        this.suggestionElement.classList.add('smart-textarea-suggestion-overlay');
        this.suggestionElement.addEventListener('mousedown', e => this.handleSuggestionClicked(e));
        this.suggestionElement.addEventListener('touchend', e => this.handleSuggestionClicked(e));

        const computedStyle = window.getComputedStyle(this.textArea);
        this.suggestionElement.style.font = computedStyle.font;
        this.suggestionElement.style.marginTop = (parseFloat(computedStyle.fontSize) * 1.4) + 'px';

        owner.appendChild(this.suggestionElement);
    }

    show(suggestion: string): void {
        this.suggestionElement.textContent = suggestion;

        const caretOffset = getCaretOffsetFromOffsetParent(this.textArea);
        const style = this.suggestionElement.style;
        style.minWidth = null;
        style.top = caretOffset.top + 'px';
        style.left = caretOffset.left + 'px';
        style.zIndex = this.textArea.style.zIndex;
        this.showing = true;

        this.suggestionElement.classList.add('smart-textarea-suggestion-overlay-visible');

        // Normally we're happy for the overlay to take up as much width as it can up to the edge of the page.
        // However, if it's too narrow (because the edge of the page is already too close), it will wrap onto
        // many lines. In this case we'll force it to get wider, and then we have to move it further left to
        // avoid spilling off the screen.
        const suggestionComputedStyle = window.getComputedStyle(this.suggestionElement);
        const numLinesOfText = Math.round((this.suggestionElement.offsetHeight - parseFloat(suggestionComputedStyle.paddingTop) - parseFloat(suggestionComputedStyle.paddingBottom))
            / parseFloat(suggestionComputedStyle.lineHeight));
        if (numLinesOfText > 2) {
            const oldWidth = this.suggestionElement.offsetWidth;
            style.minWidth = `calc(min(70vw, ${ (numLinesOfText * oldWidth / 2) }px))`; // Aim for 2 lines, but don't get wider than 70% of the screen
        }

        // If the suggestion is too far to the right, move it left so it's not off the screen
        const suggestionClientRect = this.suggestionElement.getBoundingClientRect();
        if (suggestionClientRect.right > document.body.clientWidth - 20) {
            style.left = `calc(${parseFloat(style.left) - (suggestionClientRect.right - document.body.clientWidth)}px - 2rem)`;
        }
    }

    accept(): void {
        if (!this.showing) {
            return;
        }

        // Even though document.execCommand is deprecated, it's still the best way to insert text, because it's
        // the only way that interacts correctly with the undo buffer. If we have to fall back on mutating
        // the .value property directly, it works but erases the undo buffer.
        if (document.execCommand) {
            document.execCommand('insertText', false, this.suggestionElement.textContent);
        } else {
            let caretPos = this.textArea.selectionStart;
            this.textArea.value = this.textArea.value.substring(0, caretPos)
                + this.suggestionElement.textContent
                + this.textArea.value.substring(caretPos);
            caretPos += this.suggestionElement.textContent.length;
            this.textArea.setSelectionRange(caretPos, caretPos);
        }

        // The newly-inserted text could be so long that the new caret position is off the bottom of the textarea.
        // It won't scroll to the new caret position by default
        scrollTextAreaDownToCaretIfNeeded(this.textArea);

        this.hide();
    }

    reject(): void {
        this.hide();
    }

    hide(): void {
        if (this.showing) {
            this.showing = false;
            this.suggestionElement.classList.remove('smart-textarea-suggestion-overlay-visible');
        }
    }

    isShowing(): boolean {
        return this.showing;
    }

    handleSuggestionClicked(event: Event) {
        event.preventDefault();
        event.stopImmediatePropagation();
        this.accept();
    }
}
