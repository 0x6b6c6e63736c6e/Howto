const themeButton = document.querySelector('.theme');
const root = document.documentElement;

function updateThemeButton() {
  const light = root.dataset.theme === 'light';
  themeButton.textContent = light ? '☾' : '☼';
  themeButton.setAttribute('aria-label', light ? 'Koyu temaya geç' : 'Açık temaya geç');
}

updateThemeButton();
themeButton.addEventListener('click', () => {
  root.dataset.theme = root.dataset.theme === 'light' ? 'dark' : 'light';
  try {
    localStorage.setItem('dotnet-theme', root.dataset.theme);
  } catch {
    // The current page can still switch themes when browser storage is unavailable.
  }
  updateThemeButton();
});

const menuButton = document.querySelector('.menu-button');
const menu = document.querySelector('.sidebar');

function setMenu(open) {
  menu.classList.toggle('open', open);
  menuButton.setAttribute('aria-expanded', String(open));
}

menuButton.addEventListener('click', () => setMenu(!menu.classList.contains('open')));
menu.addEventListener('click', (event) => {
  if (event.target.closest('a')) setMenu(false);
});
document.addEventListener('keydown', (event) => {
  if (event.key === 'Escape' && menu.classList.contains('open')) {
    setMenu(false);
    menuButton.focus();
  }
});
document.addEventListener('click', (event) => {
  if (!menu.contains(event.target) && !menuButton.contains(event.target)) setMenu(false);
});
matchMedia('(max-width: 760px)').addEventListener('change', () => setMenu(false));

for (const button of document.querySelectorAll('.copy')) {
  button.setAttribute('aria-live', 'polite');
  let resetTimer;
  button.addEventListener('click', async () => {
    const code = button.closest('.code').querySelector('code');
    try {
      await navigator.clipboard.writeText(code.textContent);
      button.textContent = 'Kopyalandı';
    } catch {
      const range = document.createRange();
      range.selectNodeContents(code);
      const selection = window.getSelection();
      selection.removeAllRanges();
      selection.addRange(range);
      button.textContent = 'Kod seçildi';
    }
    clearTimeout(resetTimer);
    resetTimer = setTimeout(() => { button.textContent = 'Kopyala'; }, 2000);
  });
}
