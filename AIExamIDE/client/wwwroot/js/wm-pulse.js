// wm-pulse.js - Enhanced with immediate protection
(function () {
  let pulseTimeout = null;
  let isActive = false;

  function pulse(ms = 1500) {
    // Immediate activation
    if (!isActive) {
      document.documentElement.classList.add('wm');
      isActive = true;
    }
    
    // Reset timeout
    clearTimeout(pulseTimeout);
    pulseTimeout = setTimeout(() => {
      document.documentElement.classList.remove('wm');
      isActive = false;
    }, ms);
  }

  // Expose globally
  window.__wmPulse = pulse;
  
  // Immediate protection on any suspicious activity
  window.__showProtection = function() {
    pulse(2000);
  };

})();