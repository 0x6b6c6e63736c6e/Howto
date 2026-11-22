(function () {
  "use strict";

  const THEME_STORAGE_KEY = "color-scheme";
  const CHECKLIST_STORAGE_KEY = "antigravity-checklist-v1";

  /* ------------------------------------------------------------------------
     1. Dark Mode Toggle & Cross-Tab Sync
     ------------------------------------------------------------------------ */
  function initThemeToggle() {
    const meta = document.querySelector('meta[name="color-scheme"]');
    const toggleBtn = document.getElementById("theme-toggle");
    if (!meta || !toggleBtn) return;

    const mq = window.matchMedia("(prefers-color-scheme: dark)");

    function getStoredScheme() {
      try {
        const val = localStorage.getItem(THEME_STORAGE_KEY);
        return val === "light" || val === "dark" ? val : null;
      } catch {
        return null;
      }
    }

    function applyMeta(stored) {
      meta.content = stored ?? "light dark";
    }

    toggleBtn.addEventListener("click", () => {
      const osScheme = mq.matches ? "dark" : "light";
      const stored = getStoredScheme();
      const rendered = stored ?? osScheme;
      const target = rendered === "dark" ? "light" : "dark";

      try {
        if (target !== osScheme) {
          localStorage.setItem(THEME_STORAGE_KEY, target);
          applyMeta(target);
        } else {
          localStorage.removeItem(THEME_STORAGE_KEY);
          applyMeta(null);
        }
      } catch {
        // Fallback when storage is blocked: still switch for current session
        applyMeta(target !== osScheme ? target : null);
      }
    });

    window.addEventListener("storage", (event) => {
      if (event.key === THEME_STORAGE_KEY) {
        applyMeta(getStoredScheme());
      }
    });
  }

  /* ------------------------------------------------------------------------
     2. Scroll Progress Indicator Fallback
     ------------------------------------------------------------------------ */
  function initScrollProgressFallback() {
    const progressEl = document.getElementById("scroll-progress");
    if (!progressEl) return;

    const supportsScrollTimeline =
      typeof CSS !== "undefined" &&
      typeof CSS.supports === "function" &&
      CSS.supports("animation-timeline", "scroll()");
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (supportsScrollTimeline || prefersReducedMotion) return;

    let rafId = 0;
    function updateProgress() {
      rafId = 0;
      const scrollable = document.documentElement.scrollHeight - window.innerHeight;
      const ratio = scrollable > 0 ? Math.min(1, Math.max(0, window.scrollY / scrollable)) : 0;
      progressEl.style.transform = `scaleX(${ratio})`;
    }

    window.addEventListener(
      "scroll",
      () => {
        if (!rafId) rafId = window.requestAnimationFrame(updateProgress);
      },
      { passive: true }
    );
    updateProgress();
  }

  /* ------------------------------------------------------------------------
     3. Accessible Tabs (Selection Follows Focus + beforematch + Fallback)
     ------------------------------------------------------------------------ */
  function initTabs() {
    const hasNativeFocusGroup = Object.hasOwn(HTMLElement.prototype, "focusGroup");
    const groups = document.querySelectorAll(".tabs-container");

    groups.forEach((group) => {
      const tablist = group.querySelector('[role="tablist"]');
      if (!tablist) return;

      const tabs = Array.from(tablist.querySelectorAll('[role="tab"]'));
      const panels = Array.from(group.querySelectorAll('[role="tabpanel"]'));

      function getPanelForTab(tab) {
        return panels.find((panel) => panel.getAttribute("aria-labelledby") === tab.id) || null;
      }

      function selectTab(selectedTab, { scrollIntoView = true } = {}) {
        const activePanel = getPanelForTab(selectedTab);

        tabs.forEach((tab) => {
          const isSelected = tab === selectedTab;
          tab.setAttribute("aria-selected", String(isSelected));
          tab.toggleAttribute("focusgroupstart", isSelected);
          if (!hasNativeFocusGroup) {
            tab.tabIndex = isSelected ? 0 : -1;
          }
        });

        panels.forEach((panel) => {
          const isMatch = panel === activePanel;
          if (isMatch) {
            panel.hidden = false;
            panel.setAttribute("tabindex", "0");
          } else {
            panel.hidden = "until-found";
            panel.removeAttribute("tabindex");
          }
        });

        if (scrollIntoView) {
          selectedTab.scrollIntoView({
            block: "nearest",
            inline: "nearest",
            container: "nearest",
          });
        }
      }

      if (!hasNativeFocusGroup) {
        const initiallySelected =
          tabs.find((t) => t.getAttribute("aria-selected") === "true") || tabs[0];
        tabs.forEach((t) => {
          t.tabIndex = t === initiallySelected ? 0 : -1;
        });

        tablist.addEventListener("keydown", (event) => {
          const currentIndex = tabs.indexOf(document.activeElement);
          if (currentIndex === -1) return;

          let nextIndex = -1;
          if (event.key === "ArrowRight") nextIndex = (currentIndex + 1) % tabs.length;
          else if (event.key === "ArrowLeft") nextIndex = (currentIndex - 1 + tabs.length) % tabs.length;
          else if (event.key === "Home") nextIndex = 0;
          else if (event.key === "End") nextIndex = tabs.length - 1;

          if (nextIndex !== -1) {
            event.preventDefault();
            tabs[nextIndex].focus();
            selectTab(tabs[nextIndex]);
          }
        });
      }

      tabs.forEach((tab) => {
        tab.addEventListener("click", () => {
          selectTab(tab);
        });

        // Selection follows focus (also covers native focusgroup arrow navigation)
        tab.addEventListener("focus", () => {
          if (tab.getAttribute("aria-selected") !== "true") {
            selectTab(tab);
          }
        });
      });

      panels.forEach((panel) => {
        panel.addEventListener("beforematch", () => {
          const ownerId = panel.getAttribute("aria-labelledby");
          const ownerTab = tabs.find((t) => t.id === ownerId);
          if (ownerTab) {
            selectTab(ownerTab);
          }
        });
      });
    });
  }

  /* ------------------------------------------------------------------------
     4. Accordion Expand/Collapse All (Invoker Commands + Fallback)
     ------------------------------------------------------------------------ */
  function initAccordion() {
    const accordion = document.getElementById("custom-accordion");
    const toggleAllBtn = document.querySelector('[command="--toggle-all"]');
    if (!accordion || !toggleAllBtn) return;

    function runToggleAll(target, sourceBtn) {
      const expand = sourceBtn.getAttribute("aria-expanded") !== "true";
      for (const details of target.querySelectorAll(":scope > details")) {
        details.open = expand;
      }
    }

    document.addEventListener(
      "command",
      (event) => {
        if (event.command !== "--toggle-all") return;
        runToggleAll(event.target, event.source);
      },
      { capture: true }
    );

    if (!Object.hasOwn(HTMLButtonElement.prototype, "commandForElement")) {
      toggleAllBtn.addEventListener("click", () => {
        const targetId = toggleAllBtn.getAttribute("commandfor");
        const target = targetId ? document.getElementById(targetId) : null;
        if (target) runToggleAll(target, toggleAllBtn);
      });
    }

    function syncToggleAllState() {
      const items = Array.from(accordion.querySelectorAll(":scope > details"));
      const allOpen = items.length > 0 && items.every((d) => d.open);
      toggleAllBtn.setAttribute("aria-expanded", String(allOpen));
    }

    document.addEventListener(
      "toggle",
      (event) => {
        if (!(event.target instanceof HTMLDetailsElement)) return;
        if (event.target.parentElement !== accordion) return;
        syncToggleAllState();
      },
      { capture: true }
    );

    syncToggleAllState();
  }

  /* ------------------------------------------------------------------------
     5. Slash Commands Search & Category Filter
     ------------------------------------------------------------------------ */
  function initCommandFilter() {
    const searchInput = document.getElementById("cmd-search");
    const filterButtons = Array.from(document.querySelectorAll("[data-cmd-filter]"));
    const tableBody = document.querySelector("#commands-table tbody");
    const statusEl = document.getElementById("cmd-results-status");
    const emptyEl = document.getElementById("cmd-empty-state");

    if (!searchInput || !tableBody || !statusEl || !emptyEl) return;

    const rows = Array.from(tableBody.querySelectorAll("tr"));
    let activeCategory = "all";

    function applyFilter() {
      const query = searchInput.value.trim().toLocaleLowerCase("tr-TR");
      let visibleCount = 0;

      rows.forEach((row) => {
        const cat = row.dataset.category || "";
        const searchBlob = `${row.dataset.search || ""} ${row.textContent || ""}`.toLocaleLowerCase("tr-TR");
        const matchesCat = activeCategory === "all" || cat === activeCategory;
        const matchesQuery = !query || searchBlob.includes(query);
        const show = matchesCat && matchesQuery;

        row.hidden = !show;
        if (show) visibleCount += 1;
      });

      statusEl.textContent = `${visibleCount} komut gösteriliyor`;
      emptyEl.hidden = visibleCount > 0;
    }

    filterButtons.forEach((btn) => {
      btn.addEventListener("click", () => {
        activeCategory = btn.dataset.cmdFilter || "all";
        filterButtons.forEach((b) => {
          const isCurrent = b === btn;
          b.classList.toggle("is-active", isCurrent);
          b.setAttribute("aria-pressed", String(isCurrent));
        });
        applyFilter();
      });
    });

    searchInput.addEventListener("input", applyFilter);
    applyFilter();
  }

  /* ------------------------------------------------------------------------
     6. Copy-to-Clipboard Helpers
     ------------------------------------------------------------------------ */
  async function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) {
      await navigator.clipboard.writeText(text);
      return;
    }
    const helper = document.createElement("textarea");
    helper.value = text;
    helper.setAttribute("readonly", "");
    helper.style.position = "fixed";
    helper.style.insetBlockStart = "-9999px";
    document.body.appendChild(helper);
    helper.select();
    document.execCommand("copy");
    document.body.removeChild(helper);
  }

  function initCopyButtons() {
    const liveRegion = document.getElementById("copy-live-region");
    const buttons = document.querySelectorAll("[data-copy], [data-copy-target]");

    buttons.forEach((btn) => {
      const originalLabel = btn.textContent;
      let resetTimer = 0;

      btn.addEventListener("click", async () => {
        let text = btn.getAttribute("data-copy");
        if (text === null) {
          const targetId = btn.getAttribute("data-copy-target");
          const targetEl = targetId ? document.getElementById(targetId) : null;
          text = targetEl ? targetEl.textContent || "" : "";
        }
        if (!text) return;

        try {
          await copyText(text);
          btn.textContent = "Kopyalandı ✓";
          btn.classList.add("is-copied");
          if (liveRegion) {
            liveRegion.textContent = "İçerik panoya kopyalandı.";
          }
          window.clearTimeout(resetTimer);
          resetTimer = window.setTimeout(() => {
            btn.textContent = originalLabel;
            btn.classList.remove("is-copied");
          }, 1800);
        } catch {
          if (liveRegion) {
            liveRegion.textContent = "Otomatik kopyalama başarısız; metni manuel seçip kopyalayabilirsiniz.";
          }
        }
      });
    });
  }

  /* ------------------------------------------------------------------------
     7. 5-Step Checklist with Progress & LocalStorage Persistence
     ------------------------------------------------------------------------ */
  function initChecklist() {
    const checkboxes = Array.from(document.querySelectorAll(".checklist-input"));
    const progressBar = document.getElementById("onboarding-progress");
    const statusText = document.getElementById("checklist-status-text");
    const sidebarText = document.getElementById("sidebar-progress-text");
    const sidebarFill = document.getElementById("sidebar-progress-fill");
    const resetBtn = document.getElementById("reset-checklist-btn");

    if (!checkboxes.length || !progressBar || !statusText) return;

    function loadSaved() {
      try {
        const raw = localStorage.getItem(CHECKLIST_STORAGE_KEY);
        const parsed = raw ? JSON.parse(raw) : [];
        return Array.isArray(parsed) ? parsed : [];
      } catch {
        return [];
      }
    }

    function saveState() {
      const checkedIds = checkboxes.filter((cb) => cb.checked).map((cb) => cb.id);
      try {
        localStorage.setItem(CHECKLIST_STORAGE_KEY, JSON.stringify(checkedIds));
      } catch {
        // Ignore storage restrictions
      }
    }

    function updateUI() {
      const total = checkboxes.length;
      const done = checkboxes.filter((cb) => cb.checked).length;
      const pct = Math.round((done / total) * 100);

      progressBar.value = done;
      progressBar.textContent = `${pct}%`;
      statusText.textContent = `${done} / ${total} adım tamamlandı`;

      if (sidebarText) sidebarText.textContent = `${done} / ${total}`;
      if (sidebarFill) sidebarFill.style.inlineSize = `${pct}%`;
    }

    const saved = new Set(loadSaved());
    checkboxes.forEach((cb) => {
      cb.checked = saved.has(cb.id);
      cb.addEventListener("change", () => {
        saveState();
        updateUI();
      });
    });

    if (resetBtn) {
      resetBtn.addEventListener("click", () => {
        checkboxes.forEach((cb) => {
          cb.checked = false;
        });
        saveState();
        updateUI();
      });
    }

    updateUI();
  }

  /* ------------------------------------------------------------------------
     8. Light-Dismiss Shortcuts Modal Dialog + Mandatory Backdrop Fallback
     ------------------------------------------------------------------------ */
  function initShortcutsDialog() {
    const dialog = document.getElementById("shortcuts-dialog");
    const openHeaderBtn = document.getElementById("open-shortcuts-btn");
    const openHeroBtn = document.getElementById("hero-shortcuts-btn");
    if (!dialog) return;

    function openDialog() {
      if (!dialog.open) {
        dialog.showModal();
      }
    }

    if (openHeaderBtn) openHeaderBtn.addEventListener("click", openDialog);
    if (openHeroBtn) openHeroBtn.addEventListener("click", openDialog);

    document.addEventListener("keydown", (event) => {
      if (event.key !== "?" || event.metaKey || event.ctrlKey || event.altKey) return;
      const target = event.target;
      if (
        target instanceof HTMLElement &&
        (target.isContentEditable ||
          target.tagName === "INPUT" ||
          target.tagName === "TEXTAREA" ||
          target.tagName === "SELECT")
      ) {
        return;
      }
      if (!dialog.open) {
        event.preventDefault();
        openDialog();
      }
    });

    // Mandatory fallback for browsers without <dialog closedby> support (e.g. Safari)
    if (!("closedBy" in HTMLDialogElement.prototype)) {
      dialog.addEventListener("click", (event) => {
        if (event.target !== dialog) return;
        const rect = dialog.getBoundingClientRect();
        const isDialogContent =
          rect.top <= event.clientY &&
          event.clientY <= rect.top + rect.height &&
          rect.left <= event.clientX &&
          event.clientX <= rect.left + rect.width;
        if (isDialogContent) return;
        dialog.close();
      });
    }
  }

  /* ------------------------------------------------------------------------
     9. Sidebar Table of Contents Scrollspy
     ------------------------------------------------------------------------ */
  function initScrollspy() {
    const links = Array.from(document.querySelectorAll("#toc-list a[data-toc-target]"));
    const sections = links
      .map((a) => document.getElementById(a.dataset.tocTarget))
      .filter(Boolean);

    if (!links.length || !sections.length || !("IntersectionObserver" in window)) return;

    function setActive(id) {
      links.forEach((link) => {
        if (link.dataset.tocTarget === id) {
          link.setAttribute("aria-current", "location");
        } else {
          link.removeAttribute("aria-current");
        }
      });
    }

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible.length > 0) {
          setActive(visible[0].target.id);
        }
      },
      { rootMargin: "-20% 0px -65% 0px", threshold: 0 }
    );

    sections.forEach((sec) => observer.observe(sec));
    setActive(sections[0].id);
  }

  /* ------------------------------------------------------------------------
     Bootstrap
     ------------------------------------------------------------------------ */
  document.addEventListener("DOMContentLoaded", () => {
    initThemeToggle();
    initScrollProgressFallback();
    initTabs();
    initAccordion();
    initCommandFilter();
    initCopyButtons();
    initChecklist();
    initShortcutsDialog();
    initScrollspy();
  });
})();
