/* =======================================================================
   Menu theme + preview wiring. Kept separate from the framework's menu.js
   (which is marked "do not change"). Adds:
     - a day/night toggle and a colour-scheme cycler (same schema as the
       Big Rig Cluster; shares the rc.mode / rc.accent localStorage keys so
       the menu and dashboards stay in sync),
     - a VIEW button on each skin row, shown only when that skin ships a
       mock.html, which opens the mock preview.
   ======================================================================= */
(function () {
    'use strict';

    var ACCENTS = [
        ['lime', 'LIME'], ['amber', 'AMBER'], ['red', 'RED'], ['blue', 'BLUE'],
        ['green', 'GREEN'], ['ice', 'ICE'], ['violet', 'VIOLET']
    ];

    function getUrl(path) {
        try {
            return Funbit.Ets.Telemetry.Configuration.getUrl(path);
        } catch (e) {
            return path; // fall back to a relative URL
        }
    }

    // ---- Theme state (shared with the dashboards) ----
    var mode = localStorage.getItem('rc.mode') === 'day' ? 'day' : 'night';
    var accentIdx = 0;
    var savedAccent = localStorage.getItem('rc.accent');
    for (var i = 0; i < ACCENTS.length; i++) {
        if (ACCENTS[i][0] === savedAccent) { accentIdx = i; break; }
    }

    function applyTheme() {
        var root = document.documentElement;
        root.setAttribute('data-mode', mode);
        root.setAttribute('data-accent', ACCENTS[accentIdx][0]);
        var lbl = document.querySelector('.js-theme-label');
        var mlbl = document.querySelector('.js-mode-label');
        if (lbl) lbl.textContent = ACCENTS[accentIdx][1];
        if (mlbl) mlbl.textContent = mode === 'day' ? 'DAY' : 'NIGHT';
    }

    function wireToolbar() {
        var accentBtn = document.querySelector('.js-theme');
        var modeBtn = document.querySelector('.js-mode');
        if (accentBtn) accentBtn.addEventListener('click', function () {
            accentIdx = (accentIdx + 1) % ACCENTS.length;
            localStorage.setItem('rc.accent', ACCENTS[accentIdx][0]);
            applyTheme();
        });
        if (modeBtn) modeBtn.addEventListener('click', function () {
            mode = (mode === 'day') ? 'night' : 'day';
            localStorage.setItem('rc.mode', mode);
            applyTheme();
        });
    }

    function skinPath(name, file) {
        var parts = String(name || '').split('/').map(function (p) {
            return encodeURIComponent(p);
        }).join('/');
        return '/skins/' + parts + '/' + file;
    }

    // ---- VIEW (mock preview) wiring ----
    // Rows are (re)built asynchronously by menu.js, so watch for changes and
    // process any rows that haven't been handled yet.
    function processRows() {
        var rows = document.querySelectorAll('table.skins tr[data-name]');
        for (var r = 0; r < rows.length; r++) {
            var tr = rows[r];
            if (tr.getAttribute('data-mock-checked')) continue;
            tr.setAttribute('data-mock-checked', '1');
            (function (row) {
                var name = row.getAttribute('data-name');
                var btn = row.querySelector('.skin-view');
                if (!name || !btn) return;
                if (name.indexOf('FUNBITskins/') === 0) return;
                btn.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();   // don't trigger the row's "open live" handler
                    window.location.href = getUrl(skinPath(name, 'mock.html'));
                });
                fetch(getUrl(skinPath(name, 'mock.html') + '?probe=' + Date.now()),
                    { cache: 'no-store' })
                    .then(function (resp) { if (resp.ok) btn.style.display = 'inline-block'; })
                    .catch(function () { /* no mock for this skin */ });
            })(tr);
        }
    }

    function wireTabs() {
        var tabs = document.querySelectorAll('.menu-tab');
        var panels = document.querySelectorAll('.tab-panel');
        if (!tabs.length || !panels.length) return;

        function activate(name) {
            for (var i = 0; i < tabs.length; i++) {
                var tab = tabs[i];
                var on = tab.getAttribute('data-tab') === name;
                tab.classList.toggle('is-active', on);
                tab.setAttribute('aria-selected', on ? 'true' : 'false');
            }
            for (var p = 0; p < panels.length; p++) {
                var panel = panels[p];
                var show = panel.getAttribute('data-panel') === name;
                panel.classList.toggle('is-active', show);
                if (show) {
                    panel.removeAttribute('hidden');
                } else {
                    panel.setAttribute('hidden', 'hidden');
                }
            }
            try { localStorage.setItem('menu.tab', name); } catch (e) { /* ignore */ }
        }

        var saved = 'home';
        try { saved = localStorage.getItem('menu.tab') || saved; } catch (e) { /* ignore */ }
        if (!document.querySelector('.menu-tab[data-tab="' + saved + '"]')) saved = 'home';

        for (var t = 0; t < tabs.length; t++) {
            tabs[t].addEventListener('click', function () {
                activate(this.getAttribute('data-tab'));
            });
        }
        activate(saved);

        document.querySelectorAll('[data-goto-tab]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var name = btn.getAttribute('data-goto-tab');
                var tab = document.querySelector('.menu-tab[data-tab="' + name + '"]');
                if (tab) tab.click();
            });
        });
    }

    function applyVersionFromPage() {
        var nodes = document.querySelectorAll('.js-app-version');
        if (!nodes.length)
            return;
        var sample = nodes[0].textContent || '';
        if (sample && sample.indexOf('%TRUCKDECK_VERSION%') < 0)
            return;
        try {
            $.when(Funbit.Ets.Telemetry.Configuration.getInstance().initialized).done(function (config) {
                if (config.serverVersion)
                    Funbit.Ets.Telemetry.Configuration.applyVersionLabels(config.serverVersion);
            });
        } catch (e) { /* config not ready */ }
    }

    function init() {
        applyTheme();
        wireToolbar();
        wireTabs();
        applyVersionFromPage();
        processRows();
        var table = document.querySelector('table.skins-truckdeck') || document.querySelector('table.skins');
        if (table && window.MutationObserver) {
            document.querySelectorAll('table.skins').forEach(function (tbl) {
                new MutationObserver(processRows).observe(tbl, { childList: true, subtree: true });
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
