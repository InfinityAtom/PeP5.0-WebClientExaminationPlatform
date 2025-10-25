(function(){
  const ACE_CDN = "https://cdnjs.cloudflare.com/ajax/libs/ace/1.32.0";
  let aceLoaded = false;
  let loadPromise = null;
  function loadAce(){
    if(aceLoaded) return Promise.resolve();
    if(loadPromise) return loadPromise;
    loadPromise = new Promise((res, rej)=>{
      const script = document.createElement('script');
      script.src = ACE_CDN + '/ace.js';
      script.onload = ()=>{ aceLoaded = true; res(); };
      script.onerror = (e)=>rej(e);
      document.head.appendChild(script);
    });
    return loadPromise;
  }
  async function initJsonAce(id, jsonText, theme){
    await loadAce();
    if(!window.ace){ console.warn('Ace not available'); return; }
    const el = document.getElementById(id);
    if(!el){ console.warn('Ace container not found', id); return; }
    const editor = window.ace.edit(id, { mode: 'ace/mode/json', readOnly: true, highlightActiveLine: false, showPrintMargin:false });
    editor.setTheme(theme || 'ace/theme/monokai');
    editor.session.setValue(jsonText || '');
    editor.session.setUseWrapMode(true);
    editor.renderer.setScrollMargin(8,8,8,8);
  editor.setOptions({ minLines: 30, fontSize: '16px' });
    window.__jsonPreviewEditors = window.__jsonPreviewEditors || {};
    window.__jsonPreviewEditors[id] = editor;
  }
  function updateJsonAce(id, jsonText){
    const ed = window.__jsonPreviewEditors && window.__jsonPreviewEditors[id];
    if(ed){ ed.session.setValue(jsonText||''); }
  }
  function setJsonAceTheme(id, theme){
    const ed = window.__jsonPreviewEditors && window.__jsonPreviewEditors[id];
    if(ed){ ed.setTheme(theme); }
  }
  window.JsonPreviewAce = { initJsonAce, updateJsonAce, setJsonAceTheme };
  // Simple download helper if not already present
  if(!window.BlazorDownloadFile){
    window.BlazorDownloadFile = function(filename, contentType, base64Data){
      try{
        const link = document.createElement('a');
        link.download = filename;
        link.href = 'data:' + contentType + ';base64,' + base64Data;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      }catch(e){ console.warn('Download failed', e); }
    }
  }
})();