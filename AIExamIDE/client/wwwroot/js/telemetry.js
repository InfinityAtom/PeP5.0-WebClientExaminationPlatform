(function () {
  const ORIGIN = location.origin.replace(/\/$/, '');
  const SID = localStorage.getItem("sid") || (crypto.randomUUID?.() || String(Date.now()));
  localStorage.setItem("sid", SID);
  const TELEMETRY_URL = `${ORIGIN}/telemetry?sid=${encodeURIComponent(SID)}`;

  function send(obj) {
    try {
      const payload = JSON.stringify({ ...obj, ts: Date.now(), sid: SID });
      navigator.sendBeacon(TELEMETRY_URL, payload);
      // debug
      if (location.search.includes('debug=1')) console.log('[telemetry->]', obj);
    } catch {}
  }

  setInterval(() => {
    send({
      evt: "heartbeat",
      focus: document.hasFocus(),
      visible: !document.hidden,
      fullscreen: !!(document.fullscreenElement || document.webkitFullscreenElement)
    });
  }, 5000);

  document.addEventListener("visibilitychange", () => send({ evt: document.hidden ? "hidden" : "visible" }));
  window.addEventListener("blur", () => send({ evt: "blur" }));
  window.addEventListener("focus", () => send({ evt: "focus" }));

  let last = { w: innerWidth, h: innerHeight };
  setInterval(() => {
    const dw = Math.abs(innerWidth - last.w);
    const dh = Math.abs(innerHeight - last.h);
    if (dw > 200 || dh > 200) send({ evt: "resize", w: innerWidth, h: innerHeight });
    last = { w: innerWidth, h: innerHeight };
  }, 2000);
})();
