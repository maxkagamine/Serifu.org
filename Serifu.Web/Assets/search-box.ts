const keyframes = document.getElementById('searchBoxKeyframes') as HTMLStyleElement;
const form = document.getElementById('searchBox') as HTMLFormElement;
const svg = form.querySelector('svg') as SVGSVGElement;
const input = form.querySelector('input') as HTMLInputElement;

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
  if (!input.value) {
    return;
  }
  const routeTemplate = form.dataset.routeTemplate!;
  const url = routeTemplate.replace('__QUERY__', encodeURIComponent(input.value));
  document.body.classList.add('loading');
  input.disabled = true;
  document.location = url;
});

document.addEventListener('click', e => {
  const link = (e.target as HTMLElement)?.closest('a');
  if (!input.disabled && link?.dataset.search) {
    input.value = link.dataset.search;
    form.requestSubmit();
    e.preventDefault();
  }
});
