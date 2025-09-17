// screenshot-guard.js - Enhanced screenshot protection
(function () {
  const PULSE_MS = 2000; // Increased duration
  let isProtectionActive = false;

  // Create persistent watermark overlay
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
      display: none !important;
      backdrop-filter: blur(10px) !important;
      -webkit-backdrop-filter: blur(10px) !important;
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

  // Show protection immediately
  function showProtection() {
    if (isProtectionActive) return;
    
    isProtectionActive = true;
    const overlay = getOverlay();
    overlay.style.display = 'block !important';
    
    // Also blur the main content
    document.documentElement.classList.add('wm');
    
    // Trigger pulse
    window.__wmPulse?.(PULSE_MS);
  }

  // Hide protection
  function hideProtection() {
    isProtectionActive = false;
    const overlay = getOverlay();
    overlay.style.display = 'none !important';
    document.documentElement.classList.remove('wm');
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
      e.preventDefault();
      e.stopPropagation();
      e.stopImmediatePropagation();
      showProtection();
      
      setTimeout(hideProtection, PULSE_MS);
      return false;
    }
  }, { capture: true, passive: false });

  // Window focus/blur events
  let blurTimeout;
  
  window.addEventListener('blur', () => {
    showProtection();
    // Keep protection active while window is not focused
  }, true);

  window.addEventListener('focus', () => {
    // Delay hiding protection to ensure we're really focused
    clearTimeout(blurTimeout);
    blurTimeout = setTimeout(() => {
      if (document.hasFocus() && !isProtectionActive) {
        hideProtection();
      }
    }, 500);
  }, true);

  // Visibility change
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      showProtection();
    } else {
      // Delay hiding when becoming visible
      setTimeout(() => {
        if (!document.hidden && document.hasFocus()) {
          hideProtection();
        }
      }, 500);
    }
  }, true);

  // Print events
  window.addEventListener('beforeprint', (e) => {
    e.preventDefault();
    showProtection();
    setTimeout(hideProtection, PULSE_MS);
  });

  // Context menu (right-click) prevention
  document.addEventListener('contextmenu', (e) => {
    e.preventDefault();
    showProtection();
    setTimeout(hideProtection, 1000);
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
        showProtection();
      }
    } else {
      if (devtools.open) {
        devtools.open = false;
        setTimeout(hideProtection, 1000);
      }
    }
  }, 500);

  // Prevent drag and drop
  document.addEventListener('dragstart', (e) => {
    e.preventDefault();
    showProtection();
    setTimeout(hideProtection, 1000);
  }, true);

  // Initialize overlay on load
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', createWatermarkOverlay);
  } else {
    createWatermarkOverlay();
  }

})();