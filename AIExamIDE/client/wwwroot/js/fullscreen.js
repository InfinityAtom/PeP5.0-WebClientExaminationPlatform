window.fullscreenMonitor = {
    dotNetRef: null,
    
    initialize: function(dotNetRef) {
        this.dotNetRef = dotNetRef;
        
        // Check initial state
        this.checkFullscreenStatus();
        
        // Listen for fullscreen changes
        document.addEventListener('fullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('webkitfullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('mozfullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('MSFullscreenChange', () => this.handleFullscreenChange());
        
        // Prevent escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.isFullscreen()) {
                e.preventDefault();
            }
        });
        
        // Monitor window focus
        window.addEventListener('blur', () => {
            setTimeout(() => this.checkFullscreenStatus(), 100);
        });
    },
    
    isFullscreen: function() {
        return !!(
            document.fullscreenElement ||
            document.webkitFullscreenElement ||
            document.mozFullScreenElement ||
            document.msFullscreenElement
        );
    },
    
    checkFullscreenStatus: function() {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnFullscreenChanged', this.isFullscreen());
        }
    },
    
    handleFullscreenChange: function() {
        setTimeout(() => this.checkFullscreenStatus(), 100);
    },
    
    requestFullscreen: function() {
        const element = document.documentElement;
        
        if (element.requestFullscreen) {
            element.requestFullscreen();
        } else if (element.webkitRequestFullscreen) {
            element.webkitRequestFullscreen();
        } else if (element.mozRequestFullScreen) {
            element.mozRequestFullScreen();
        } else if (element.msRequestFullscreen) {
            element.msRequestFullscreen();
        }
    },
    
    dispose: function() {
        this.dotNetRef = null;
    }
};
document.addEventListener('visibilitychange', () => {
  if (window.fullscreenMonitor.dotNetRef)
    window.fullscreenMonitor.dotNetRef.invokeMethodAsync('OnVisibilityChanged', document.hidden);
});
window.addEventListener('blur', () => {
  if (window.fullscreenMonitor.dotNetRef)
    window.fullscreenMonitor.dotNetRef.invokeMethodAsync('OnWindowBlur');
});
window.addEventListener('focus', () => {
  if (window.fullscreenMonitor.dotNetRef)
    window.fullscreenMonitor.dotNetRef.invokeMethodAsync('OnWindowFocus');
});
window.initializeFullscreenMonitor = (dotNetRef) => {
    window.fullscreenMonitor.initialize(dotNetRef);
};

window.requestFullscreen = () => {
    window.fullscreenMonitor.requestFullscreen();
};

window.disposeFullscreenMonitor = () => {
    window.fullscreenMonitor.dispose();
};

// pune la finalul fișierului
window.toggleNoFullscreenClass = function(flag){
  document.documentElement.classList.toggle('no-fullscreen', !!flag);
};
