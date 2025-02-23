// I'd originally built the drawer as a pure HTML+CSS popover, like the notes & link menu on the results page, but a
// recent version of Chrome introduced a bug that causes a STATUS_ACCESS_VIOLATION when light-dismissing the drawer
// which crashes the tab, so I ended up replacing the popover drawer with a normal JS version. Due to mixed browser
// support for @starting-style and allow-discrete, this approach provides a more consistent user experience anyway.
//
// https://github.com/maxkagamine/Serifu.org/pull/6
// https://issues.chromium.org/issues/398651928

(() => {
  const button = document.getElementById('menuButton') as HTMLButtonElement;
  const drawer = document.getElementById('drawer') as HTMLElement;
  const desktopNav = document.querySelector('.site-header nav') as HTMLElement;

  if (!button) {
    // Page is using base layout without header
    return;
  }

  // Clone the nav into the drawer. Reusing the same element for both modes would make styling difficult due to the
  // separate media queries needed for either language (can't use css variables there), while relying on JS to set a
  // ".mobile" class instead would introduce an undesirable FOUC.
  drawer.innerHTML = desktopNav.innerHTML;

  function open() {
    drawer.hidden = false;
    drawer.querySelector('a')?.focus();
    setTimeout(() => drawer.classList.add('open'), 1);
  }

  function close() {
    drawer.classList.remove('open');
  }

  button.addEventListener('click', e => {
    open();
    e.stopPropagation();
  });

  drawer.addEventListener('transitionend', () => {
    if (!drawer.classList.contains('open')) {
      drawer.hidden = true;
    }
  });

  function onClick(e: Event) {
    if (!drawer.hidden && e.target instanceof Node && !drawer.contains(e.target)) {
      close();
    }
  }

  document.addEventListener('click', onClick);
  document.addEventListener('touchend', onClick);

  drawer.addEventListener('focusout', e => {
    if (e.relatedTarget instanceof Node && !drawer.contains(e.relatedTarget)) {
      close();
    }
  });

  document.addEventListener('keydown', e => {
    if (!drawer.hidden && e.key === 'Escape') {
      close();
    }
  });
})();
