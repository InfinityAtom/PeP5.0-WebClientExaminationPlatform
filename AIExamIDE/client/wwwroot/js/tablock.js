// wwwroot/js/tablock.js
(function(){
  const ch = new BroadcastChannel('exam-tab');
  let isPrimary = true;
  ch.onmessage = e => {
    if (e.data === 'hello') {
      isPrimary = false;
      document.body.innerHTML = '<h2 style="font-family:sans-serif">Session active in another tab</h2>';
    }
  };
  setTimeout(()=> ch.postMessage('hello'), 100);
})();
