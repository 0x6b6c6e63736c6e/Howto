try {
  document.documentElement.dataset.theme = localStorage.getItem('dotnet-theme') === 'light' ? 'light' : 'dark';
} catch {
  document.documentElement.dataset.theme = 'dark';
}
