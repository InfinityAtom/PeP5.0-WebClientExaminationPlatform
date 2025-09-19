// screenshot-guard.js - Enhanced screenshot protection
(function () {
  const PULSE_MS = 2000; // Show overlay for this long unless held by focus/visibility/devtools
  let isProtectionActive = false;
  let autoHideTimer = null;

  // Create persistent overlay (hidden by default)
  function createWatermarkOverlay() {
    const overlay = document.createElement('div');
    overlay.id = 'screenshot-protection-overlay';
    overlay.style.cssText = `
      position: fixed !important;
      top: 0 !important;
      left: 0 !important;
      width: 100vw !important;
      height: 100vh !important;
      z-index: 2147483647 !important;
      pointer-events: none !important;
      background: rgba(0, 0, 0, 0.95) !important;
      backdrop-filter: blur(10px) !important;
      -webkit-backdrop-filter: blur(10px) !important;
      display: none; /* do NOT use !important here so we can toggle reliably */
    `;

    // Add watermark text pattern
    overlay.innerHTML = `
      <div style="
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-image: repeating-linear-gradient(
          45deg,
          transparent,
          transparent 50px,
          rgba(255, 255, 255, 0.1) 50px,
          rgba(255, 255, 255, 0.1) 100px
        );
        display: flex;
        align-items: center;
        justify-content: center;
        font-family: Arial, sans-serif;
        font-size: 48px;
        font-weight: bold;
        color: rgba(255, 255, 255, 0.8);
        text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.8);
        transform: rotate(-15deg);
        user-select: none;
        -webkit-user-select: none;
        -moz-user-select: none;
        -ms-user-select: none;
      ">
        PROTECTED CONTENT<br>
        SCREENSHOT BLOCKED<br>
        ${new Date().toISOString()}
      </div>
    `;

    document.body.appendChild(overlay);
    return overlay;
  }

  // Get or create overlay
  function getOverlay() {
    let overlay = document.getElementById('screenshot-protection-overlay');
    if (!overlay) {
      overlay = createWatermarkOverlay();
    }
    return overlay;
  }

  function scheduleAutoHide() {
    clearTimeout(autoHideTimer);
    autoHideTimer = setTimeout(() => {
      hideProtection();
    }, PULSE_MS);
  }

  // Show protection immediately
  function showProtection({ hold = false } = {}) {
    const overlay = getOverlay();
    overlay.style.display = 'block';
    isProtectionActive = true;
    if (!hold) scheduleAutoHide();
  }

  // Hide protection
  function hideProtection() {
    const overlay = getOverlay();
    overlay.style.display = 'none';
    isProtectionActive = false;
    clearTimeout(autoHideTimer);
  }

  // Enhanced keyboard detection
  document.addEventListener('keydown', (e) => {
    const key = (e.key || "").toLowerCase();
    const ctrl = e.ctrlKey;
    const shift = e.shiftKey;
    const meta = e.metaKey;
    const alt = e.altKey;

    // Detect various screenshot combinations
    const isScreenshot = (
      key.includes('print') || // PrintScreen
      (ctrl && shift && key === 's') || // Ctrl+Shift+S (Firefox)
      (meta && shift && ['3','4','5','6'].includes(key)) || // Mac screenshots
      (alt && key === 'printscreen') || // Alt+PrintScreen
      (ctrl && key === 'printscreen') || // Ctrl+PrintScreen
      (meta && shift && key === '3') || // Cmd+Shift+3
      (meta && shift && key === '4') || // Cmd+Shift+4
      (meta && shift && key === '5') || // Cmd+Shift+5
      key === 'f12' || // DevTools
      (ctrl && shift && key === 'i') || // DevTools
      (ctrl && shift && key === 'j') || // DevTools Console
      (ctrl && shift && key === 'c') || // DevTools Elements
      (ctrl && key === 'u') // View Source
    );

    if (isScreenshot) {
      // Do our best to show overlay synchronously
      showProtection();
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      return false;
    }
  }, { capture: true, passive: false });

  // Window focus/blur events
  window.addEventListener('blur', () => {
    // Hold overlay while window is unfocused
    showProtection({ hold: true });
  }, true);

  window.addEventListener('focus', () => {
    // When we regain focus, hide the overlay after a short delay
    setTimeout(() => {
      if (document.hasFocus()) hideProtection();
    }, 250);
  }, true);

  // Visibility change
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      showProtection({ hold: true });
    } else {
      setTimeout(() => {
        if (!document.hidden && document.hasFocus()) hideProtection();
      }, 250);
    }
  }, true);

  // Print events
  window.addEventListener('beforeprint', () => {
    showProtection();
  });

  // Context menu (right-click) prevention
  document.addEventListener('contextmenu', (e) => {
    e.preventDefault();
    showProtection();
  }, true);

  // DevTools detection (basic)
  let devtools = {
    open: false,
    orientation: null
  };

  const threshold = 160;

  setInterval(() => {
    if (window.outerHeight - window.innerHeight > threshold || 
        window.outerWidth - window.innerWidth > threshold) {
      if (!devtools.open) {
        devtools.open = true;
        showProtection({ hold: true });
      }
    } else {
      if (devtools.open) {
        devtools.open = false;
        hideProtection();
      }
    }
  }, 500);

  // Prevent drag and drop
  document.addEventListener('dragstart', (e) => {
    e.preventDefault();
    showProtection();
  }, true);

  // Initialize overlay on load
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', createWatermarkOverlay);
  } else {
    createWatermarkOverlay();
  }

})();