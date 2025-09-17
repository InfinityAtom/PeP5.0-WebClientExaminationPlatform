(function () {
  const DEBUG = location.search.includes('debug=1');
  if (DEBUG) { console.log('[guard] disabled'); return; }

  const block = e => e.preventDefault();
  document.addEventListener("contextmenu", block, { capture: true });
  document.addEventListener("dragstart", block, { capture: true });
  document.addEventListener("selectstart", block, { capture: true });

  document.addEventListener("copy", block, { capture: true });
  document.addEventListener("cut", block, { capture: true });
  document.addEventListener("paste", block, { capture: true });

  document.addEventListener("keydown", e => {
  const k = e.key.toLowerCase();
  const ctrl = e.ctrlKey || e.metaKey;
  const meta = e.metaKey;
  const shift = e.shiftKey;

  const blocked =
    // copy, paste, cut, save, print, select all, find
    ctrl && ["c","v","x","s","p","a","f"].includes(k) ||
    // devtools
    ctrl && e.shiftKey && ["i","j","c"].includes(k) ||
    k === "f12" ||
    // screenshot shortcuts
    (ctrl && shift && k === "s") ||       // Ctrl+Shift+S (Windows, unele tool-uri)
    (meta && shift && ["3","4","5","6"].includes(k)) || // macOS Cmd+Shift+3/4/5/6
    k.includes("print"); // PrintScreen

  if (blocked) e.preventDefault();
}, { capture: true });
})();
