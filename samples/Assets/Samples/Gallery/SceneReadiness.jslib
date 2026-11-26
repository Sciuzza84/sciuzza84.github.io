mergeInto(LibraryManager.library, {
  SetUnityPortfolioActiveSample: function (keyPointer, titlePointer) {
    if (typeof document === 'undefined') return;
    var canvas = document.getElementById('player');
    if (!canvas) return;

    var change = {};
    canvas.unityPortfolioSceneChange = change;
    delete canvas.dataset.activeSample;
    canvas.setAttribute('aria-label', 'Interactive Unity sample');

    var key = UTF8ToString(keyPointer);
    var title = UTF8ToString(titlePointer);
    if (!key) return;

    // Two animation-frame callbacks leave a browser paint between Unity's frame and the ready marker.
    window.requestAnimationFrame(function () {
      window.requestAnimationFrame(function () {
        if (!canvas.isConnected || canvas.unityPortfolioSceneChange !== change) return;
        canvas.setAttribute('aria-label', title);
        canvas.dataset.activeSample = key;
      });
    });
  },
});
