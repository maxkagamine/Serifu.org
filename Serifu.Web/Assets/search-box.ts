const keyframes = document.getElementById('searchBoxKeyframes') as HTMLStyleElement;
const form = document.getElementById('search') as HTMLFormElement;
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
  e.preventDefault(); // TODO: Remove me
  document.body.classList.add('loading');
  input.disabled = true;
});
