window.scrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// Kategorie auf der Anmeldeseite nach dem Aufklappen ins Bild holen. Wartet, bis die
// Zuklapp-Animation der anderen Kategorien (0.4s) fertig ist, und scrollt nur, wenn der
// Header nicht schon gut sichtbar oben im Bild steht.
window.scrollCategoryIntoView = function (key) {
    setTimeout(function () {
        var el = document.querySelector('[data-category="' + CSS.escape(key) + '"]');
        if (!el) return;
        var top = el.getBoundingClientRect().top;
        if (top >= 0 && top <= window.innerHeight * 0.4) return;
        var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.scrollTo({ top: window.scrollY + top - 16, behavior: reduce ? 'auto' : 'smooth' });
    }, 420);
};
