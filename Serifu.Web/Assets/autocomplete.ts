import getCaretCoordinates from 'textarea-caret';

const MENTION_REGEX = /(?<=(?:^|\s)@)(\S*)$/;

const MAX_AUTOCOMPLETE_COUNT = 50;
const MAX_AUTOCOMPLETE_HEIGHT = 10;

interface Mention {
  value: string;
  start: number;
  end: number;
}

(async () => {
  const input = document.querySelector('#searchBox input') as HTMLInputElement;
  const select = document.getElementById('searchBoxAutocomplete') as HTMLSelectElement;

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
      select.hidden = true;
      return;
    }

    // Check if the select list needs to be repopulated (user may have just clicked away and then back)
    if (select.dataset.suggestionsFor !== mention.value) {
      select.dataset.suggestionsFor = mention.value;
      select.options.length = 0;

      // Search the list of names and add <option>s. There are more efficient ways of doing this (e.g. a trie) but at
      // that point we'd probably want to shift this work to the server.
      const mentionValueNormalized = normalizeSearchString(mention.value);
      for (const name of names) {
        if (!name.some(n => normalizeSearchString(n).includes(mentionValueNormalized))) {
          continue;
        }
        const option = document.createElement('option');
        option.innerText = name[0];
        if (select.options.length === 0) {
          option.selected = true;
        }
        select.options.add(option);
        if (select.options.length === MAX_AUTOCOMPLETE_COUNT) {
          break;
        }
      }
    }

    // Show the autocomplete list only if not empty and not already completed
    select.hidden =
      select.options.length === 0 || (select.options.length === 1 && select.options[0].value === mention.value);

    if (select.hidden) {
      return;
    }

    // Resize the list according to the number of options. This has to be at least 2, since 1 turns it into a regular
    // select dropdown (we use css to shrink it to a height of 1 in that case).
    select.size = Math.max(2, Math.min(MAX_AUTOCOMPLETE_HEIGHT, select.options.length));

    // Position the select list under the caret (or as an extension of the textbox on mobile)
    const relLeft = getCaretCoordinates(input, mention.start).left;
    const inputRect = input.getBoundingClientRect();

    select.style.top = `${inputRect.bottom}px`;

    if (window.matchMedia('max-width: 500px').matches || select.clientWidth > inputRect.width) {
      select.style.left = `${inputRect.left}px`;
      select.style.right = `${window.innerWidth - inputRect.right}px`;
    } else if (relLeft + select.clientWidth > inputRect.width) {
      select.style.left = 'auto';
      select.style.right = `${window.innerWidth - inputRect.right}px`;
    } else {
      select.style.left = `${inputRect.left + relLeft}px`;
      select.style.right = 'auto';
    }
  }

  function acceptSuggestion(option: HTMLOptionElement) {
    const mention = getMentionUnderCaret();
    if (!mention) {
      return;
    }

    // Replace the mention being typed with the selected autocomplete option
    const completedValue = option.value + ' ';
    input.value = input.value.substring(0, mention.start) + completedValue + input.value.substring(mention.end);

    // Trigger validation check
    input.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertReplacementText' }));

    // Make sure the input is focused (user may have used mouse to select) with the caret after the mention
    const newCaretPos = mention.start + completedValue.length;
    input.focus();
    input.setSelectionRange(newCaretPos, newCaretPos);
  }

  function replaceAtSigns() {
    // Replace fullwidth at signs with halfwidth ones. This will abort IME composition when an ＠ is typed, but that
    // actually works in our favor.
    input.value = input.value.replace(/＠/g, '@');
  }

  input.addEventListener('input', replaceAtSigns, true);

  input.addEventListener('selectionchange', updateAutocomplete);
  input.addEventListener('focus', updateAutocomplete);

  input.addEventListener('blur', e => {
    if (e.relatedTarget !== select) {
      select.hidden = true;
    }
  });

  input.addEventListener('keydown', e => {
    if (select.hidden) {
      return;
    }
    switch (e.key) {
      case 'ArrowDown':
        if (select.selectedIndex === select.options.length - 1) {
          select.selectedIndex = 0;
        } else {
          select.selectedIndex++;
        }
        e.preventDefault();
        break;
      case 'ArrowUp':
        if (select.selectedIndex === 0) {
          select.selectedIndex = select.options.length - 1;
        } else {
          select.selectedIndex--;
        }
        e.preventDefault();
        break;
      case 'Enter':
      case 'Tab':
        acceptSuggestion(select.selectedOptions[0]);
        e.preventDefault();
        break;
      case 'Escape':
        select.hidden = true;
        break;
    }
  });

  select.addEventListener('click', e => {
    if (e.target instanceof HTMLOptionElement) {
      acceptSuggestion(e.target);
    }
  });
})();
