(function () {
    function preferred() {
        const saved = localStorage.getItem('theme');
        if (saved) return saved;
        return (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light';
    }
    function apply(mode) {
        document.documentElement.setAttribute('data-bs-theme', mode);
        document.documentElement.setAttribute('data-theme', mode);
        localStorage.setItem('theme', mode);
        return mode;
    }
    function init() { return apply(preferred()); }
    function toggle() { return apply((document.documentElement.getAttribute('data-bs-theme') === 'dark') ? 'light' : 'dark'); }

    // 1) inicializace (když by někdo odstranil atribut)
    if (!document.documentElement.hasAttribute('data-bs-theme')) { init(); }

    // 2) ochranný hlídač – když kdokoliv ten atribut změní nebo smaže, vrátíme ho zpět
    const mo = new MutationObserver(() => {
        if (!document.documentElement.hasAttribute('data-bs-theme')) {
            apply(preferred());
        }
    });
    mo.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });

    // 3) po změně URL v rámci SPA re-aplikuj (pro jistotu)
    window.addEventListener('popstate', () => apply(preferred()));
    window.addEventListener('hashchange', () => apply(preferred()));

    // 4) vystav API pro Blazor interop
    window.theme = { init, toggle, apply };
})();
