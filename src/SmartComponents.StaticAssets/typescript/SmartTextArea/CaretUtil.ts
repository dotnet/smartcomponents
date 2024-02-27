import * as caretPos from 'caret-pos';

export function scrollTextAreaDownToCaretIfNeeded(textArea: HTMLTextAreaElement) {
    // Note that this only scrolls *down*, because that's the only scenario after a suggestion is accepted
    const pos = caretPos.position(textArea);
    const lineHeightInPixels = parseFloat(window.getComputedStyle(textArea).lineHeight);
    if (pos.top > textArea.clientHeight + textArea.scrollTop - lineHeightInPixels) {
        textArea.scrollTop = pos.top - textArea.clientHeight + lineHeightInPixels;
    }
}

export function getCaretOffsetFromOffsetParent(elem: HTMLTextAreaElement): { top: number, left: number, height: number, elemStyle: CSSStyleDeclaration } {
    const elemStyle = window.getComputedStyle(elem);
    const pos = caretPos.position(elem);

    return {
        top: pos.top + parseFloat(elemStyle.borderTopWidth) + elem.offsetTop - elem.scrollTop,
        left: pos.left + parseFloat(elemStyle.borderLeftWidth) + elem.offsetLeft - elem.scrollLeft - 0.25,
        height: pos.height,
        elemStyle: elemStyle,
    }
}
