(function () {
    'use strict';

    const SERVER_PORT = 25555;
    const LS_IP = 'tcd.serverIp';
    const LS_SKIN = 'tcd.skin';

    const el = {
        frame: document.getElementById('skin-frame'),
        menuBtn: document.getElementById('menu-btn'),
        empty: document.getElementById('empty-state'),
        emptyMsg: document.getElementById('empty-msg'),
        emptyOpen: document.getElementById('empty-open'),
        backdrop: document.getElementById('backdrop'),
        drawer: document.getElementById('drawer'),
        drawerClose: document.getElementById('drawer-close'),
        ipInput: document.getElementById('ip-input'),
        ipSave: document.getElementById('ip-save'),
        connStatus: document.getElementById('conn-status'),
        refreshSkins: document.getElementById('refresh-skins'),
        skinList: document.getElementById('skin-list'),
    };

    let wakeLock = null;

    // ---- State helpers ----
    function getIp() {
        const stored = localStorage.getItem(LS_IP);
        if (stored) return stored;
        // When served over http from the server, default to that host.
        const host = window.location.hostname;
        if (host && host !== 'localhost' && window.location.protocol.startsWith('http')) {
            return host;
        }
        return '';
    }
    function setIp(ip) { localStorage.setItem(LS_IP, ip); }
    function getSkin() { return localStorage.getItem(LS_SKIN) || ''; }
    function setSkin(name) { localStorage.setItem(LS_SKIN, name); }

    function serverUrl(path) {
        return 'http://' + getIp() + ':' + SERVER_PORT + path;
    }
    function isValidIp(ip) {
        return /^[a-zA-Z0-9.\-]+$/.test(ip);
    }

    // ---- Menu open/close ----
    function openMenu() {
        el.drawer.classList.add('open');
        el.backdrop.classList.remove('hidden');
        const ip = getIp();
        if (ip && !el.skinList.querySelector('.skin-card')) loadSkins();
    }
    function closeMenu() {
        el.drawer.classList.remove('open');
        el.backdrop.classList.add('hidden');
    }

    // ---- Skin list ----
    function setStatus(text, cls) {
        el.connStatus.textContent = text;
        el.connStatus.className = 'status' + (cls ? ' ' + cls : '');
    }

    async function loadSkins() {
        const ip = getIp();
        if (!ip) {
            setStatus('Enter a server IP first.', 'bad');
            return;
        }
        setStatus('Connecting to ' + ip + ' ...', 'busy');
        el.skinList.innerHTML = '<div class="skin-empty">Loading dashboards...</div>';
        try {
            const res = await fetch(serverUrl('/config.json?t=' + Date.now()), { cache: 'no-store' });
            if (!res.ok) throw new Error('HTTP ' + res.status);
            const json = await res.json();
            const skins = sortSkinsTdFirst((json && json.skins) || []);
            renderSkins(skins);
            setStatus('Connected \u2022 ' + skins.length + ' dashboards', 'ok');
        } catch (err) {
            console.error('Failed to load skins:', err);
            setStatus('Cannot reach ' + ip + ' (is the server running?)', 'bad');
            el.skinList.innerHTML = '<div class="skin-empty">No dashboards. Check the IP and that the Telemetry Server is running.</div>';
        }
    }

    // TruckDeck skins first, Funbit OG skins after (mirrors the tab order on
    // the main site's index.html). Stable sort keeps each group's original order.
    function sortSkinsTdFirst(skins) {
        return skins.slice().sort(function (a, b) {
            const rank = function (s) { return s.group === 'Original Funbit skins' ? 1 : 0; };
            return rank(a) - rank(b);
        });
    }

    function renderSkins(skins) {
        el.skinList.innerHTML = '';
        if (!skins.length) {
            el.skinList.innerHTML = '<div class="skin-empty">No dashboards found on the server.</div>';
            return;
        }
        const active = getSkin();
        skins.forEach(function (skin) {
            const card = document.createElement('div');
            card.className = 'skin-card' + (skin.name === active ? ' active' : '');
            card.dataset.name = skin.name;

            const img = document.createElement('img');
            img.className = 'skin-thumb';
            img.alt = '';
            img.src = serverUrl('/skins/' + skin.name + '/dashboard.jpg');
            img.onerror = function () { img.style.visibility = 'hidden'; };

            const meta = document.createElement('div');
            meta.className = 'skin-meta';
            const name = document.createElement('div');
            name.className = 'skin-name';
            name.textContent = skin.title || skin.name;
            const author = document.createElement('div');
            author.className = 'skin-author';
            author.textContent = skin.author ? 'by ' + skin.author : '';
            meta.appendChild(name);
            meta.appendChild(author);

            const badge = document.createElement('span');
            badge.className = 'skin-badge';
            badge.textContent = 'ACTIVE';

            // Preview button - only revealed if the skin ships a mock.html.
            const view = document.createElement('button');
            view.className = 'view-btn hidden';
            view.type = 'button';
            view.textContent = 'VIEW';
            view.title = 'Preview with mock data';
            view.addEventListener('click', function (e) {
                e.stopPropagation();
                previewSkin(skin.name);
            });

            const right = document.createElement('div');
            right.className = 'skin-right';
            right.appendChild(view);
            right.appendChild(badge);

            card.appendChild(img);
            card.appendChild(meta);
            card.appendChild(right);
            card.addEventListener('click', function () { selectSkin(skin.name); });
            el.skinList.appendChild(card);

            // Probe for a mock preview file; show the VIEW button if present.
            // GET (not HEAD) so it works on simple static servers that 405 HEAD.
            fetch(serverUrl('/skins/' + skin.name + '/mock.html?probe=' + Date.now()),
                { cache: 'no-store' })
                .then(function (r) { if (r.ok) view.classList.remove('hidden'); })
                .catch(function () { /* no mock for this skin */ });
        });
    }

    // ---- Load a dashboard ----
    function selectSkin(name) {
        const ip = getIp();
        if (!ip) { openMenu(); return; }
        setSkin(name);
        const url = serverUrl('/dashboard-host.html?skin=' + encodeURIComponent(name) +
            '&ip=' + encodeURIComponent(ip) + '&t=' + Date.now());
        el.frame.src = url;
        el.frame.classList.add('active');
        el.empty.classList.add('hidden');
        // reflect active selection in the list
        el.skinList.querySelectorAll('.skin-card').forEach(function (c) {
            c.classList.toggle('active', c.dataset.name === name);
        });
        closeMenu();
        requestWakeLock();
    }

    // ---- Preview a dashboard with mock data (skin's mock.html) ----
    function previewSkin(name) {
        const ip = getIp();
        if (!ip) { openMenu(); return; }
        const url = serverUrl('/skins/' + encodeURIComponent(name) + '/mock.html?t=' + Date.now());
        el.frame.src = url;
        el.frame.classList.add('active');
        el.empty.classList.add('hidden');
        closeMenu();
        requestWakeLock();
    }

    function showEmpty(msg) {
        if (msg) el.emptyMsg.textContent = msg;
        el.empty.classList.remove('hidden');
        el.frame.classList.remove('active');
        el.frame.removeAttribute('src');
    }

    // ---- IP save ----
    function saveIp() {
        const ip = el.ipInput.value.trim();
        if (!ip) { setStatus('Enter a server IP.', 'bad'); return; }
        if (!isValidIp(ip)) { setStatus('Invalid IP / hostname format.', 'bad'); return; }
        setIp(ip);
        loadSkins();
    }

    // ---- Wake lock ----
    async function requestWakeLock() {
        if ('wakeLock' in navigator) {
            try { wakeLock = await navigator.wakeLock.request('screen'); } catch (e) { /* ignore */ }
        }
    }
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') requestWakeLock();
    });

    // ---- Wire events ----
    el.menuBtn.addEventListener('click', openMenu);
    el.emptyOpen.addEventListener('click', openMenu);
    el.drawerClose.addEventListener('click', closeMenu);
    el.backdrop.addEventListener('click', closeMenu);
    el.ipSave.addEventListener('click', saveIp);
    el.ipInput.addEventListener('keydown', function (e) { if (e.key === 'Enter') saveIp(); });
    el.refreshSkins.addEventListener('click', loadSkins);

    // ---- Theme: colour scheme + day/night (shared with the dashboards) ----
    const ACCENTS = [
        ['lime', 'LIME'], ['amber', 'AMBER'], ['red', 'RED'], ['blue', 'BLUE'],
        ['green', 'GREEN'], ['ice', 'ICE'], ['violet', 'VIOLET']
    ];
    let themeMode = localStorage.getItem('rc.mode') === 'day' ? 'day' : 'night';
    let accentIdx = 0;
    (function () {
        const saved = localStorage.getItem('rc.accent');
        for (let i = 0; i < ACCENTS.length; i++) {
            if (ACCENTS[i][0] === saved) { accentIdx = i; break; }
        }
    })();
    function applyTheme() {
        const root = document.documentElement;
        root.setAttribute('data-mode', themeMode);
        root.setAttribute('data-accent', ACCENTS[accentIdx][0]);
        const lbl = document.querySelector('.js-theme-label');   // optional (dot only now)
        const mlbl = document.querySelector('.js-mode-label');
        if (lbl) lbl.textContent = ACCENTS[accentIdx][1];
        // Day/night shown as an icon: sun for day, moon for night.
        if (mlbl) mlbl.innerHTML = themeMode === 'day' ? '&#9728;' : '&#9790;';
    }
    function wireTheme() {
        const accentBtn = document.querySelector('.js-theme');
        const modeBtn = document.querySelector('.js-mode');
        if (accentBtn) accentBtn.addEventListener('click', function () {
            accentIdx = (accentIdx + 1) % ACCENTS.length;
            localStorage.setItem('rc.accent', ACCENTS[accentIdx][0]);
            applyTheme();
        });
        if (modeBtn) modeBtn.addEventListener('click', function () {
            themeMode = (themeMode === 'day') ? 'night' : 'day';
            localStorage.setItem('rc.mode', themeMode);
            applyTheme();
        });
    }

    // ---- Init ----
    function init() {
        applyTheme();
        wireTheme();
        const ip = getIp();
        el.ipInput.value = ip;
        const skin = getSkin();
        if (ip && skin) {
            selectSkin(skin);
        } else if (ip) {
            showEmpty('Open the menu and pick a dashboard.');
            loadSkins();
        } else {
            showEmpty('Open the menu to set your server IP and pick a dashboard.');
            openMenu();
        }
    }

    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('sw.js').catch(function () { /* SW needs https/localhost */ });
        });
    }

    init();
})();
