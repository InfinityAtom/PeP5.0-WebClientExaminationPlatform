// wwwroot/js/watermark.js
(function () {
  function draw(text) {
    const c = document.createElement('canvas');
    const size = 320;
    c.width = size; c.height = size;
    const ctx = c.getContext('2d');
    ctx.clearRect(0,0,size,size);
    ctx.translate(size/2, size/2);
    ctx.rotate(-Math.PI/6);
    ctx.font = "700 18px system-ui, sans-serif";
    ctx.fillStyle = "rgba(255,255,255,0.18)";
    ctx.textAlign = "center"; ctx.textBaseline = "middle";
    ctx.fillText(text, 0, -24);
    ctx.fillText(text, 0, +24);
    return c.toDataURL("image/png");
  }
  function apply() {
    const sid = localStorage.getItem("sid") || "session";
    const text = `${sid} • ${new Date().toISOString()}`;
    document.documentElement.style.setProperty("--wm-url", `url(${draw(text)})`);
  }
  window.__wmSet = function(on, strong){
    document.documentElement.classList.toggle('wm', !!on);
    document.documentElement.classList.toggle('wm-strong', !!on && !!strong);
    document.documentElement.style.setProperty("--wm-opacity", strong ? ".75" : ".22");
  };
  apply();
  setInterval(apply, 7000);
})();
