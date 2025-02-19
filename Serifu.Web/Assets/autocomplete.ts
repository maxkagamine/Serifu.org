import getCaretCoordinates from 'textarea-caret';

const MENTION_REGEX = /(?<=(?:^|\s)@)(\S*)$/;
const MAX_AUTOCOMPLETE_COUNT = 50;

interface Mention {
  value: string;
  start: number;
  end: number;
}

// Must opt-in to the virtual keyboard api to enable env(keyboard-inset-height) in css
if ('virtualKeyboard' in navigator) {
  // @ts-ignore
  navigator.virtualKeyboard.overlaysContent = true;
}

(async () => {
  const input = document.querySelector('#searchBox input') as HTMLInputElement;
  const list = document.querySelector('autocomplete-list') as AutocompleteList;

  if (!input) {
    // Page is using base layout without search box
    return;
  }

  const names: string[][] = (await import('./names.json')).default;

  function getMentionUnderCaret(): Mention | null {
    // Check if the text preceeding the caret ends with an @-mention and no space
    const caretPos = input.selectionStart!;
    const beforeMatch = input.value.substring(0, caretPos).match(MENTION_REGEX);
    if (!beforeMatch) {
      return null;
    }

    // Combine the matched part before the caret with the part after the caret (if typing in the middle of a mention)
    const beforePart = beforeMatch[1];
    const afterPart = input.value.substring(caretPos).match(/^\S*/)?.[0] ?? '';
    const value = beforePart + afterPart;

    const start = beforeMatch.index!;
    const end = start + value.length;

    // Don't show suggestions if highlighting text outside the mention
    if (input.selectionEnd! > end) {
      return null;
    }

    return { value, start, end };
  }

  /** Lowercases and converts katakana to hiragana */
  function normalizeSearchString(str: string) {
    return str.toLowerCase().replace(/[ァ-ヶ]/g, x => String.fromCodePoint(x.codePointAt(0)! - 96));
  }

  function updateAutocomplete() {
    const mention = getMentionUnderCaret();
    if (!mention) {
      list.hidden = true;
      return;
    }

    // Check if the select list needs to be repopulated (user may have just clicked away and then back)
    if (list.dataset.suggestionsFor !== mention.value) {
      const options: string[] = [];

      // Search the list of names. There are more efficient ways of doing this (e.g. a trie), but at that point we'd
      // probably want to shift this work to the server anyway.
      const mentionValueNormalized = normalizeSearchString(mention.value);
      for (const name of names) {
        if (!name.some(n => normalizeSearchString(n).includes(mentionValueNormalized))) {
          continue;
        }
        options.push(name[0]);
        if (options.length === MAX_AUTOCOMPLETE_COUNT) {
          break;
        }
      }

      list.options = options;
      list.dataset.suggestionsFor = mention.value;
    }

    // Show the autocomplete list only if not empty and not already completed
    list.hidden = list.options.length === 0 || (list.options.length === 1 && list.options[0] === mention.value);

    if (list.hidden) {
      return;
    }

    // Position the autocomplete list under the mention (or as an extension of the entire textbox on mobile)
    const relLeft = getCaretCoordinates(input, mention.start - 1).left;
    const inputRect = input.getBoundingClientRect();

    list.style.top = `${inputRect.bottom}px`;

    list.style.left = 'auto'; // If both left & right are values other than auto, we won't get an accurate offsetWidth
    list.style.right = 'auto';

    if (window.matchMedia('(max-width: 500px)').matches || list.offsetWidth > inputRect.width) {
      list.style.left = `${inputRect.left}px`;
      list.style.right = `${document.body.clientWidth - inputRect.right}px`;
    } else if (relLeft + list.offsetWidth > inputRect.width) {
      list.style.left = 'auto';
      list.style.right = `${document.body.clientWidth - inputRect.right}px`;
    } else {
      list.style.left = `${inputRect.left + relLeft}px`;
      list.style.right = 'auto';
    }
  }

  function acceptSuggestion(option: string) {
    const mention = getMentionUnderCaret();
    if (!mention) {
      return;
    }

    // Replace the mention being typed with the selected autocomplete option
    const completedValue = option + ' ';
    input.value = input.value.substring(0, mention.start) + completedValue + input.value.substring(mention.end);

    // Trigger validation check
    input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertReplacementText' }));

    // Make sure the input is focused (user may have used mouse to select) with the caret after the mention
    const newCaretPos = mention.start + completedValue.length;
    input.focus();
    input.setSelectionRange(newCaretPos, newCaretPos);
  }

  function handleInput() {
    // Replace fullwidth at signs with halfwidth ones. This will abort IME composition when an ＠ is typed, but that
    // actually works in our favor.
    input.value = input.value.replace(/＠/g, '@');

    // The selectionchange event doesn't fire when backspacing for some reason, so to be safe we'll update on input too
    updateAutocomplete();
  }

  input.addEventListener('input', handleInput, true);

  // On recent browser versions, the selectionchange event is fired from the input element as you'd expect, but
  // historically it was fired on document only. For best support, we'll listen to it there instead.
  document.addEventListener('selectionchange', e => {
    if (e.target === input) {
      updateAutocomplete();
    }
  });

  input.addEventListener('focus', updateAutocomplete);

  input.addEventListener('blur', e => {
    if (!e.relatedTarget || !(e.relatedTarget as Element).closest('autocomplete-list')) {
      list.hidden = true;
    }
  });

  input.addEventListener('keydown', e => {
    if (list.hidden) {
      return;
    }
    switch (e.key) {
      case 'ArrowDown':
        if (list.selectedIndex === list.options.length - 1) {
          list.selectedIndex = 0;
        } else {
          list.selectedIndex++;
        }
        e.preventDefault();
        break;
      case 'ArrowUp':
        if (list.selectedIndex === 0) {
          list.selectedIndex = list.options.length - 1;
        } else {
          list.selectedIndex--;
        }
        e.preventDefault();
        break;
      case 'Enter':
      case 'Tab':
        acceptSuggestion(list.selectedOption);
        e.preventDefault();
        break;
      case 'Escape':
        list.hidden = true;
        break;
    }
  });

  list.addEventListener('option-clicked', () => {
    acceptSuggestion(list.selectedOption);
  });
})();

/**
 * Custom element for the autocomplete list, partially emulating a select list. Originally I wanted to use a native
 * <select> element, but on mobile it always appears as a dropdown even if size > 1.
 */
class AutocompleteList extends HTMLElement {
  private static readonly SELECTED_CLASS = 'selected';
  private static readonly MAX_SIZE = 10;

  #options: string[] = [];

  connectedCallback() {
    this.addEventListener('mouseover', this.onMouseOver);
    this.addEventListener('click', this.onClick);
  }

  disconnectedCallback() {
    this.removeEventListener('mouseover', this.onMouseOver);
    this.removeEventListener('click', this.onClick);
  }

  get options(): readonly string[] {
    return this.#options;
  }

  set options(options: string[]) {
    this.#options = options;
    this.replaceChildren(
      ...options.map((x, i) => {
        const el = document.createElement('button');
        el.innerText = x;
        el.classList.toggle(AutocompleteList.SELECTED_CLASS, i === 0);
        return el;
      }),
    );
    this.scrollTop = 0;
    this.style.setProperty('--size', Math.min(options.length, AutocompleteList.MAX_SIZE).toString());
  }

  get selectedIndex(): number {
    for (let i = 0; i < this.childElementCount; i++) {
      if (this.children[i].classList.contains(AutocompleteList.SELECTED_CLASS)) {
        return i;
      }
    }
    return -1;
  }

  set selectedIndex(index: number) {
    for (let i = 0; i < this.childElementCount; i++) {
      const child = this.children[i] as HTMLButtonElement;
      child.classList.toggle(AutocompleteList.SELECTED_CLASS, i === index);

      // Scroll into view if needed
      if (
        i === index &&
        (child.offsetTop < this.scrollTop || child.offsetTop + child.offsetHeight > this.scrollTop + this.clientHeight)
      ) {
        child.scrollIntoView({ block: 'nearest', behavior: 'instant' });
      }
    }
  }

  get selectedOption(): string {
    return this.#options[this.selectedIndex];
  }

  onMouseOver = (e: MouseEvent) => {
    if (e.target instanceof HTMLButtonElement) {
      for (const child of this.children) {
        child.classList.toggle(AutocompleteList.SELECTED_CLASS, child === e.target);
      }
    }
  };

  onClick = (e: MouseEvent) => {
    if (e.target instanceof HTMLButtonElement) {
      this.dispatchEvent(new CustomEvent('option-clicked', { bubbles: true }));
    }
  };
}

customElements.define('autocomplete-list', AutocompleteList);
