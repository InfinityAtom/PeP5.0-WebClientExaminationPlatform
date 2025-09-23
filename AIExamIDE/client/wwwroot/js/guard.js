(function () {
  const DEBUG = location.search.includes('debug=1');
  if (DEBUG) { console.log('[guard] disabled'); return; }

  const block = e => e.preventDefault();
  document.addEventListener("contextmenu", block, { capture: true });
  document.addEventListener("dragstart", block, { capture: true });
  document.addEventListener("dragover", block, { capture: true });
  document.addEventListener("drop", block, { capture: true });
  document.addEventListener("selectstart", block, { capture: true });
  document.addEventListener("copy", block, { capture: true });
  document.addEventListener("paste", block, { capture: true });
  document.addEventListener("keydown", e => {
  const k = e.key.toLowerCase();
  const ctrl = e.ctrlKey || e.metaKey;
  const meta = e.metaKey;
  const shift = e.shiftKey;
  const alt = e.altKey;

  const blocked =
    // copy, paste, cut, save, print, select all, find
    ctrl && ["c","v","x","s","p","a","f"].includes(k) ||
    // devtools
    ctrl && e.shiftKey && ["i","j","c"].includes(k) ||
    k === "f12" ||
    // refresh
    k === "f5" || (ctrl && k === "r") ||
    // navigation back/forward
    (alt && (k === "arrowleft" || k === "arrowright")) ||
    (k === "backspace" && !isEditableTarget(e.target)) ||
    // zoom
    (ctrl && ["=","+","-","0"].includes(k)) ||
    // screenshot shortcuts
    (ctrl && shift && k === "s") ||       // Ctrl+Shift+S (Windows, unele tool-uri)
    (meta && shift && ["3","4","5","6"].includes(k)) || // macOS Cmd+Shift+3/4/5/6
    k.includes("print"); // PrintScreen

  if (blocked) e.preventDefault();
}, { capture: true });

  // Block ctrl+wheel zoom
  document.addEventListener('wheel', (e) => {
    if (e.ctrlKey) {
      e.preventDefault();
    }
  }, { passive: false, capture: true });

  // Confirm on refresh/close
  window.addEventListener('beforeunload', (e) => {
    try {
      // Try to avoid accidental close/refresh
      const confirmationMessage = 'Are you sure you want to leave the exam? Your progress may be lost.';
      (e || window.event).returnValue = confirmationMessage;
      return confirmationMessage;
    } catch {}
  });

  function isEditableTarget(t) {
    if (!t) return false;
    const tag = (t.tagName || '').toLowerCase();
    const editableTags = ['input','textarea'];
    if (editableTags.includes(tag)) return !t.readOnly && !t.disabled;
    return !!t.isContentEditable;
  }
})();
