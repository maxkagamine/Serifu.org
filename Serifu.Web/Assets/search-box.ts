import { MENTION_REGEX, MULTIPLE_MENTIONS_REGEX } from './autocomplete';
import { assertDefined } from './util';

// Sync with constants in ElasticsearchService
const MAX_LENGTH_ENGLISH = 64;
const MAX_LENGTH_JAPANESE = 32;

(() => {
  const form = document.getElementById('searchBox') as HTMLFormElement;
  if (!form) {
    // Page is using base layout without search box
    return;
  }

  const keyframes = document.getElementById('searchBoxKeyframes') as HTMLStyleElement;
  const svg = form.querySelector('svg') as SVGSVGElement;
  const input = form.querySelector('input') as HTMLInputElement;

  // Focus search box when loading homepage. This is more reliable than the autofocus attribute. The media query is to
  // prevent a blinking cursor appearing on a mobile device without a keyboard where the user would have to tap the
  // search box anyway (there's a proposal for detecting a keyboard: https://github.com/w3c/csswg-drafts/issues/3871).
  if (input.classList.contains('autofocus') && window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
    input.focus();
    input.select();
  }

  function update() {
    const box = svg.getBoundingClientRect();
    const totalLength = box.width * 2 + box.height * 2;
    svg.style.setProperty('--total-length', totalLength + 'px');
    svg.style.setProperty('--stroke-length', box.height * 0.75 + 'px');

    // Firefox workaround, see css
    keyframes.innerHTML = `
@keyframes loading-spin {
  to {
    --stroke-offset: ${totalLength}px;
  }
}
@keyframes loading-stretch {
  to {
    --stroke-length: ${totalLength / 8}px;
  }
}
`;
  }

  window.addEventListener('resize', update);
  update();

  form.addEventListener('submit', e => {
    e.preventDefault();
    const query = input.value.trim();
    if (!query) {
      return;
    }
    const routeTemplate = assertDefined(form.dataset.routeTemplate, 'routeTemplate');
    const url = routeTemplate.replace('__QUERY__', encodeURIComponent(query));
    document.body.classList.add('loading');
    input.disabled = true;
    document.location = url;
  });

  // Links with data-search will put the value in the textbox and start the loading animation
  document.addEventListener('click', e => {
    const link = (e.target as HTMLElement)?.closest('a');
    if (link?.dataset.search) {
      document.body.classList.add('loading');
      input.value = link.dataset.search;
      input.disabled = true;
    }
  });

  // Clear loading state when the page loads from the browser's bfcache (navigating via back button)
  window.addEventListener('pageshow', () => {
    document.body.classList.remove('loading');
    input.disabled = false;
  });

  input.addEventListener('input', () => {
    let query = input.value.trim().normalize();

    // If the input is empty, there's no need to display a validation warning; the form just won't submit (unless JS is
    // disabled, but we handle that server-side)
    if (!query) {
      input.setCustomValidity('');
      return;
    }

    if (MULTIPLE_MENTIONS_REGEX.test(query)) {
      input.setCustomValidity(assertDefined(form.dataset.multipleMentions, 'multipleMentions'));

      // Trigger browser's validation popup so user isn't left wondering why the autocomplete list won't appear
      input.reportValidity();

      return;
    }

    // Remove any @-mention prior to validating query length
    query = query.replace(MENTION_REGEX, ' ').trim();

    // Unlike .NET, regexes in JS have full Unicode support, so we can match the server-side validation easily
    const isJapanese = /\p{Script=Hiragana}|\p{Script=Katakana}|\p{Script=Han}/u.test(query);
    const length = getLength(query);
    const maxLength = isJapanese ? MAX_LENGTH_JAPANESE : MAX_LENGTH_ENGLISH;

    if (length > maxLength) {
      input.setCustomValidity(assertDefined(form.dataset.tooLong, 'tooLong'));
      return;
    }

    if (length < 2 && !/^\p{Script=Han}$/u.test(query)) {
      input.setCustomValidity(assertDefined(form.dataset.tooShort, 'tooShort'));
      return;
    }

    if (/;\s*DROP TABLE/i.test(query)) {
      input.setCustomValidity('(╯°□°)╯︵ ┻━┻');
      return;
    }

    input.setCustomValidity('');
  });

  function getLength(str: string) {
    // Kindof overkill, but at the same time it's weird that neither C# nor JS has a way to get the "actual" length
    // directly from String. Globalization shouldn't be an afterthought. Compare '鏡'.length vs '𪚲'.length.
    if ('Segmenter' in Intl) {
      return Array.from(new Intl.Segmenter().segment(str)).length;
    }
    return str.length;
  }
})();
