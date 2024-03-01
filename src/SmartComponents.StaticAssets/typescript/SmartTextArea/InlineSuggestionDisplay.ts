import { SuggestionDisplay } from './SuggestionDisplay';
import { SmartTextArea } from './SmartTextArea';
import { getCaretOffsetFromOffsetParent, scrollTextAreaDownToCaretIfNeeded } from './CaretUtil';

export class InlineSuggestionDisplay implements SuggestionDisplay {
    latestSuggestionText: string = '';
    suggestionStartPos: number | null = null;
    suggestionEndPos: number | null = null;
    fakeCaret: FakeCaret | null = null;

    constructor(private owner: SmartTextArea, private textArea: HTMLTextAreaElement) {
    }

    isShowing(): boolean {
        return this.suggestionStartPos !== null;
    }

    show(suggestion: string): void {
        this.latestSuggestionText = suggestion;
        this.suggestionStartPos = this.textArea.selectionStart;
        this.suggestionEndPos = this.suggestionStartPos + suggestion.length;

        this.textArea.setAttribute('data-suggestion-visible', '');
        this.textArea.value = this.textArea.value.substring(0, this.suggestionStartPos) + suggestion + this.textArea.value.substring(this.suggestionStartPos);
        this.textArea.setSelectionRange(this.suggestionStartPos, this.suggestionEndPos);

        this.fakeCaret ??= new FakeCaret(this.owner, this.textArea);
        this.fakeCaret.show();
    }

    get currentSuggestion() {
        return this.latestSuggestionText;
    }

    accept(): void {
        this.textArea.setSelectionRange(this.suggestionEndPos, this.suggestionEndPos);
        this.suggestionStartPos = null;
        this.suggestionEndPos = null;
        this.fakeCaret?.hide();
        this.textArea.removeAttribute('data-suggestion-visible');

        // The newly-inserted text could be so long that the new caret position is off the bottom of the textarea.
        // It won't scroll to the new caret position by default
        scrollTextAreaDownToCaretIfNeeded(this.textArea);
    }

    reject(): void {
        if (!this.isShowing()) {
            return; // No suggestion is shown
        }

        const prevSelectionStart = this.textArea.selectionStart;
        const prevSelectionEnd = this.textArea.selectionEnd;
        this.textArea.value = this.textArea.value.substring(0, this.suggestionStartPos) + this.textArea.value.substring(this.suggestionEndPos);

        if (this.suggestionStartPos === prevSelectionStart && this.suggestionEndPos === prevSelectionEnd) {
            // For most interactions we don't need to do anything to preserve the cursor position, but for
            // 'scroll' events we do (because the interaction isn't going to set a cursor position naturally)
            this.textArea.setSelectionRange(prevSelectionStart, prevSelectionStart /* not 'end' because we removed the suggestion */);
        }

        this.suggestionStartPos = null;
        this.suggestionEndPos = null;
        this.textArea.removeAttribute('data-suggestion-visible');
        this.fakeCaret?.hide();
    }
}

class FakeCaret {
    readonly caretDiv: HTMLDivElement;

    constructor(owner: SmartTextArea, private textArea: HTMLTextAreaElement) {
        this.caretDiv = document.createElement('div');
        this.caretDiv.classList.add('smart-textarea-caret');
        owner.appendChild(this.caretDiv);
    }

    show() {
        const caretOffset = getCaretOffsetFromOffsetParent(this.textArea);
        const style = this.caretDiv.style;
        style.display = 'block';
        style.top = caretOffset.top + 'px';
        style.left = caretOffset.left + 'px';
        style.height = caretOffset.height + 'px';
        style.zIndex = this.textArea.style.zIndex;
        style.backgroundColor = caretOffset.elemStyle.caretColor;
    }

    hide() {
        this.caretDiv.style.display = 'none';
    }
}
