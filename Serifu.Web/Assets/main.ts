import './global.css';
import './view-transitions.css';
import './popovers.css';
import './icons.css';
import './header.css';
import './search-box.css';
import './autocomplete.css';
import './footer.css';
import './results.css';
import './about-page.css';
import './error-page.css';

import './images/serifu.svg';
import './images/favicon.svg?no-inline';

import './search-box';
import './autocomplete';
import './audio-button';

// Polyfill for Safari/iOS
import { apply, isSupported } from '@oddbird/popover-polyfill/fn';

// Initially, when a user visits the root they'll be redirected to the homepage
// corresponding to their Accept-Language header. If they switch languages
// manually, we'll want to remember and redirect them to their preferred
// language instead next time.
//
// Since cookies, unlike localStorage, have a max-age of 400 days[0], it's
// necessary to "refresh" cookies periodically. The easiest way to do this here
// is to simply set the culture cookie to the current culture on page load,
// meaning Accept-Language will only be used the first time and every subsequent
// visit to the site root will redirect to the last-used language.
//
// I'm setting the cookie in JS rather than via headers to avoid problems with
// CDNs later.
//
// [0]: https://developer.chrome.com/blog/cookie-max-age-expires
//
const lang = document.documentElement.lang;
document.cookie = `.AspNetCore.Culture=c=${lang}|uic=${lang}; path=/; max-age=34560000`;

if (!isSupported()) {
  console.log('Polyfilling popovers');
  apply();
}
