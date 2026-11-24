async function copyCommand(button) {
  const code = button.closest('.snippet').querySelector('code');
  try {
    await navigator.clipboard.writeText(code.textContent);
    button.textContent = 'Kopyalandı';
  } catch {
    const range = document.createRange();
    range.selectNodeContents(code);
    const selection = window.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
    button.textContent = 'Seçildi';
  }
}

for (const button of document.querySelectorAll('.copy')) {
  button.hidden = false;
  button.addEventListener('click', () => copyCommand(button));
  button.addEventListener('focusout', () => {
    button.textContent = 'Kopyala';
  });
}

const demoSteps = [
  {
    command: "printf '# Git Denemem\\n' > README.md",
    result: 'README.md oluşturuldu. git status --short çıktısı: ?? README.md',
    nextLabel: 'Hazırlık alanına ekle'
  },
  {
    command: 'git add README.md',
    result: 'README.md hazır. git status --short çıktısı: A  README.md',
    nextLabel: 'Commit oluştur'
  },
  {
    command: 'git commit -m "İlk commit"',
    result: '[main (root-commit) f17e2a0] İlk commit',
    nextLabel: 'Commit kaydedildi'
  }
];

const practiceActions = document.querySelector('#practice-actions');
const runStep = document.querySelector('#run-step');
const resetDemo = document.querySelector('#reset-demo');
const output = document.querySelector('.terminal-output');
const initialOutput = output.firstElementChild.cloneNode(true);
const progress = [...document.querySelectorAll('.practice-progress li')];
let stepIndex = 0;

practiceActions.hidden = false;

function updateProgress() {
  progress.forEach((item, index) => {
    item.classList.toggle('done', index < stepIndex);
    item.removeAttribute('aria-current');
    if (index === stepIndex) item.setAttribute('aria-current', 'step');
  });
}

runStep.addEventListener('click', () => {
  const step = demoSteps[stepIndex];
  if (stepIndex === 0) output.replaceChildren();

  const command = document.createElement('p');
  const prompt = document.createElement('span');
  prompt.className = 'prompt';
  prompt.textContent = '$ ';
  command.append(prompt, step.command);

  const result = document.createElement('p');
  result.className = 'terminal-note';
  result.textContent = step.result;

  output.append(command, result);
  stepIndex++;
  runStep.textContent = step.nextLabel;
  resetDemo.disabled = false;
  updateProgress();

  if (stepIndex === demoSteps.length) {
    runStep.disabled = true;
    resetDemo.focus();
  }
});

resetDemo.addEventListener('click', () => {
  stepIndex = 0;
  output.replaceChildren(initialOutput.cloneNode(true));
  updateProgress();
  runStep.textContent = 'Dosyayı oluştur';
  runStep.disabled = false;
  resetDemo.disabled = true;
  runStep.focus();
});
