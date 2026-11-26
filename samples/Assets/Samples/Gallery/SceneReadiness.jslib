mergeInto(LibraryManager.library, {
  SetUnityPortfolioActiveSample: function (keyPointer, titlePointer, folderPointer) {
    if (typeof document === 'undefined') return;
    var canvas = document.getElementById('player');
    if (!canvas) return;

    var change = {};
    canvas.unityPortfolioSceneChange = change;
    delete canvas.dataset.activeSample;
    canvas.setAttribute('aria-label', 'Interactive Unity sample');
    canvas.unityPortfolioPageTitle = canvas.unityPortfolioPageTitle || document.title;
    document.title = canvas.unityPortfolioPageTitle;

    var codeLink = document.getElementById('sample-code-link');
    if (codeLink) {
      codeLink.unityPortfolioCatalog = codeLink.unityPortfolioCatalog || codeLink.href;
      codeLink.href = codeLink.unityPortfolioCatalog;
      codeLink.removeAttribute('aria-label');
    }

    var key = UTF8ToString(keyPointer);
    var title = UTF8ToString(titlePointer);
    var folder = UTF8ToString(folderPointer);
    if (!key) return;

    // Two animation-frame callbacks leave a browser paint between Unity's frame and the ready marker.
    window.requestAnimationFrame(function () {
      window.requestAnimationFrame(function () {
        if (!canvas.isConnected || canvas.unityPortfolioSceneChange !== change) return;
        canvas.setAttribute('aria-label', title);
        if (folder) {
          document.title = title + ' · Unity samples';
          if (codeLink) {
            codeLink.href = codeLink.unityPortfolioCatalog + '/Assets/Samples/' + encodeURIComponent(folder);
            codeLink.setAttribute('aria-label', 'Code and checks for ' + title);
          }
        }
        canvas.dataset.activeSample = key;
      });
    });
  },
});
