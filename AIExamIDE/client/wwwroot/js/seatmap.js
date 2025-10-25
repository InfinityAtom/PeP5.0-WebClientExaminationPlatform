window.seatmap = (function(){
  function init(containerId){
    const el = document.getElementById(containerId);
    if(!el) return;
    el.style.position = 'relative';
    const bounds = el.getBoundingClientRect();
    el.querySelectorAll('.desk').forEach(d => {
      d.style.position = 'absolute';
      d.style.cursor = 'move';
      let dragging = false, startX=0, startY=0, origX=0, origY=0;
      d.addEventListener('pointerdown', (e)=>{
        dragging = true; d.setPointerCapture(e.pointerId);
        startX = e.clientX; startY = e.clientY;
        origX = parseInt(d.style.left||'0',10); origY = parseInt(d.style.top||'0',10);
      });
      d.addEventListener('pointermove', (e)=>{
        if(!dragging) return;
        const dx = e.clientX - startX; const dy = e.clientY - startY;
        let nx = Math.max(0, Math.min(origX + dx, el.clientWidth - d.offsetWidth));
        let ny = Math.max(0, Math.min(origY + dy, el.clientHeight - d.offsetHeight));
        d.style.left = nx + 'px'; d.style.top = ny + 'px';
      });
      d.addEventListener('pointerup', (e)=>{ dragging = false; d.releasePointerCapture(e.pointerId); });
    });
  }
  function getPositions(containerId){
    const el = document.getElementById(containerId); if(!el) return [];
    const arr = [];
    el.querySelectorAll('.desk').forEach(d => {
      arr.push({ id: d.dataset.id, x: parseInt(d.style.left||'0',10), y: parseInt(d.style.top||'0',10) });
    });
    return arr;
  }
  return { init, getPositions };
})();

