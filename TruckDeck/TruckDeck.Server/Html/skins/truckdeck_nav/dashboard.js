Funbit.Ets.Telemetry.Dashboard.prototype.initialize = function (skinConfig, utils) {
    var self = this;
    var rootEl = document.querySelector('.dashboard.tdn');
    var mapHost = document.getElementById('tdn-map');
    self._pmtilesMap = null;
    self._mapScriptsLoaded = false;
    self._activeGame = null;
    self._mapReady = false;
    self._lastLngLat = null;
    self._lastHeading = 0;
    self._activeRoutePath = null;
    self._countriesEts2 = null;
    self._countriesLoadPending = false;
    self._countryKey = '';
    self._countryPos = { x: null, z: null };

    function setMapStatus() { /* status dot removed */ }

    // Day / night mode, synced to the shared tdd.mode (falls back to rc.mode) theme toggle
    // used by the other TruckDeck skins — NAV has no toggle of its own, it just follows.
    function readMode() {
        var saved = localStorage.getItem('tdd.mode') || localStorage.getItem('rc.mode');
        return (saved === 'day') ? 'day' : 'night';
    }
    self._mode = readMode();

    function applyMode() {
        if (rootEl) rootEl.setAttribute('data-mode', self._mode);
        if (self._pmtilesMap) self._pmtilesMap.setMode(self._mode);
    }

    function syncMode() {
        var mode = readMode();
        if (mode === self._mode) return;
        self._mode = mode;
        applyMode();
    }

    applyMode();
    window.addEventListener('storage', function (e) {
        if (!e || !e.key || e.key === 'tdd.mode' || e.key === 'rc.mode') syncMode();
    });
    setInterval(syncMode, 2000);

    $(document).on('click', '.js-map-zoom-in', function (e) {
        e.preventDefault();
        if (self._pmtilesMap && self._pmtilesMap.zoomBy) self._pmtilesMap.zoomBy(1);
    });
    $(document).on('click', '.js-map-zoom-out', function (e) {
        e.preventDefault();
        if (self._pmtilesMap && self._pmtilesMap.zoomBy) self._pmtilesMap.zoomBy(-1);
    });
    $(document).on('click', '.js-map-recenter', function (e) {
        e.preventDefault();
        if (self._pmtilesMap && self._pmtilesMap.recenter) self._pmtilesMap.recenter();
    });

    var NAV_MODES = ['best', 'shortest', 'small_roads'];
    var NAV_MODE_LABELS = { best: 'BEST', shortest: 'SHORT', small_roads: 'LOCAL' };
    var NAV_MODE_TITLES = {
        best: 'Best — efficient route (match Interface → Navigation mode)',
        shortest: 'Shortest — shortest distance',
        small_roads: 'Small roads — avoid highways'
    };

    function readNavMode() {
        try {
            var s = localStorage.getItem('tdn.navMode');
            if (s && NAV_MODES.indexOf(s) >= 0) return s;
        } catch (e) { /* ignore */ }
        return 'best';
    }

    self._navMode = readNavMode();

    function applyNavMode() {
        if (rootEl) rootEl.setAttribute('data-nav-mode', self._navMode);
        var label = NAV_MODE_LABELS[self._navMode] || 'BEST';
        var title = NAV_MODE_TITLES[self._navMode] || NAV_MODE_TITLES.best;
        $('.js-nav-mode').attr('title', title).text(label);
        if (window.TruckDeckRouter && TruckDeckRouter.setNavMode) {
            TruckDeckRouter.setNavMode(self._navMode);
        }
    }

    function cycleNavMode() {
        var idx = NAV_MODES.indexOf(self._navMode);
        self._navMode = NAV_MODES[(idx + 1) % NAV_MODES.length];
        try { localStorage.setItem('tdn.navMode', self._navMode); } catch (e) { /* ignore */ }
        applyNavMode();
        invalidateGpsRoute();
        self._routeKey = null;
        self._routeRetryAt = 0;
    }

    applyNavMode();

    $(document).on('click', '.js-nav-mode', function (e) {
        e.preventDefault();
        cycleNavMode();
    });

    var _chromeSyncRaf = 0;

    function syncViewportFill() {
        if (!rootEl) return;
        var h = 0;
        if (window.visualViewport && window.visualViewport.height > 0) {
            h = Math.round(window.visualViewport.height);
        } else if (document.body && document.body.clientHeight > 0) {
            h = document.body.clientHeight;
        } else if (window.innerHeight > 0) {
            h = window.innerHeight;
        }
        if (h > 0) {
            rootEl.style.height = h + 'px';
            rootEl.style.maxHeight = h + 'px';
            rootEl.style.minHeight = '0';
        }
    }

    function syncChromeInsets() {
        if (!rootEl) return;
        syncViewportFill();
        var topBar = rootEl.querySelector('.tdn-top-bar');
        var bottomChrome = rootEl.querySelector('.tdn-bottom-chrome') ||
            rootEl.querySelector('.tdn-bottom-bar');
        if (topBar) {
            rootEl.style.setProperty('--tdn-chrome-top', topBar.offsetHeight + 'px');
        }
        if (bottomChrome) {
            rootEl.style.setProperty('--tdn-chrome-bottom', bottomChrome.offsetHeight + 'px');
        }
        if (self._pmtilesMap && self._pmtilesMap.map) {
            try { self._pmtilesMap.map.resize(); } catch (err) { /* ignore */ }
        }
    }

    function scheduleChromeSync() {
        if (_chromeSyncRaf) return;
        _chromeSyncRaf = requestAnimationFrame(function () {
            _chromeSyncRaf = 0;
            syncChromeInsets();
        });
    }

    function bindChromeLayoutSync() {
        if (!rootEl || typeof ResizeObserver === 'undefined') {
            syncChromeInsets();
            return;
        }
        var ro = new ResizeObserver(function () { scheduleChromeSync(); });
        var topBar = rootEl.querySelector('.tdn-top-bar');
        var bottomChrome = rootEl.querySelector('.tdn-bottom-chrome') ||
            rootEl.querySelector('.tdn-bottom-bar');
        if (topBar) ro.observe(topBar);
        if (bottomChrome) ro.observe(bottomChrome);
        syncChromeInsets();
    }

    bindChromeLayoutSync();
    window.addEventListener('resize', scheduleChromeSync);
    window.addEventListener('orientationchange', function () {
        setTimeout(syncChromeInsets, 120);
    });
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', scheduleChromeSync);
        window.visualViewport.addEventListener('scroll', scheduleChromeSync);
    }

    window.addEventListener('storage', function (e) {
        if (!e || e.key !== 'tdn.navMode') return;
        var mode = readNavMode();
        if (mode === self._navMode) return;
        self._navMode = mode;
        applyNavMode();
        invalidateGpsRoute();
        self._routeKey = null;
        self._routeRetryAt = 0;
    });

    self.speedUnit = Funbit.Ets.Telemetry.Dashboard.getSpeedUnit();
    $(document).on('click', '.tdn-top-speed', function () {
        self.speedUnit = Funbit.Ets.Telemetry.Dashboard.toggleSpeedUnit();
    });
    window.addEventListener('rc-speedunit-change', function (e) {
        if (e && e.detail && e.detail.unit) self.speedUnit = e.detail.unit;
    });

    function assetUrl(path) {
        if (!path) path = '/';
        if (path.charAt(0) !== '/') path = '/' + path;
        try {
            var C = window.Funbit && Funbit.Ets && Funbit.Ets.Telemetry && Funbit.Ets.Telemetry.Configuration;
            if (C && typeof C.getUrl === 'function') {
                return C.getUrl(path);
            }
        } catch (e) { /* ignore */ }
        var p = window.location.pathname || '/';
        var i = p.indexOf('/skins/');
        var root = (i >= 0) ? p.substring(0, i) : p.replace(/\/[^/]*$/, '');
        return (root || '') + path;
    }

    function loadOne(path) {
        return new Promise(function (resolve, reject) {
            var src = assetUrl(path) + '?seed=' + Date.now();
            var s = document.createElement('script');
            s.src = src;
            s.onload = resolve;
            s.onerror = function () { reject(new Error(path)); };
            document.head.appendChild(s);
        });
    }

    function loadCountryData() {
        function applyList(list) {
            self._countriesEts2 = list;
        }
        if (window.RcCountry) {
            window.RcCountry.loadEts2Countries().then(applyList)['catch'](function () { /* non-critical */ });
            return;
        }
        loadOne('/scripts/country-lookup.js').then(function () {
            if (window.RcCountry) {
                return window.RcCountry.loadEts2Countries().then(applyList);
            }
        })['catch'](function () { /* non-critical */ });
    }

    loadCountryData();

    function loadCss(href) {
        var base = href.split('?')[0];
        var nodes = document.querySelectorAll('link[rel="stylesheet"][href]');
        for (var i = 0; i < nodes.length; i++) {
            if ((nodes[i].getAttribute('href') || '').split('?')[0] === base) return;
        }
        var l = document.createElement('link');
        l.rel = 'stylesheet';
        l.href = href;
        document.head.appendChild(l);
    }

    function loadMapScripts() {
        if (self._mapScriptsLoaded || (window.TruckDeckPmtilesMap && window.TruckDeckProjection && window.TruckDeckRouter)) {
            self._mapScriptsLoaded = true;
            return Promise.resolve();
        }
        loadCss(assetUrl('/scripts/vendor/maplibre-gl.css'));
        return Promise.all([
            loadOne('/scripts/vendor/proj4.js'),
            loadOne('/scripts/vendor/pmtiles.js'),
            loadOne('/scripts/vendor/maplibre-gl.js')
        ])
            .then(function () { return loadOne('/scripts/ets2-projection.js'); })
            .then(function () { return loadOne('/scripts/truckdeck-pmtiles-map.js'); })
            .then(function () {
                if (!window.TruckDeckPmtilesMap) {
                    throw new Error('truckdeck-pmtiles-map.js (TruckDeckPmtilesMap missing — check console for syntax errors)');
                }
            })
            .then(function () { return loadOne('/scripts/truckdeck-router.js'); })
            .then(function () { return loadOne('/scripts/truckdeck-nav-guidance.js'); })
            .then(function () {
                self._mapScriptsLoaded = true;
            });
    }

    // Routing: follow in-game GPS (navigation.*) first; manual map tap is fallback only.
    self._routeKey = null;
    self._routeRetryAt = 0;
    self._pendingRoute = null;
    self._navMatchInFlight = false;
    self._gpsRoute = null; // cached match for active game GPS session
    self._gpsNavRefreshAt = 0;
    self._gpsLastNavM = NaN;
    self._graphFailLogged = false;
    self._graphUpgradeNextAt = 0;
    self._jobCitySkipUntil = 0;
    self._lastRouteResetAt = 0;
    self._routeWatchPos = null;
    self._mapInteractive = false;
    self._navLowMemory = /Android|iPhone|iPad|iPod|Mobile|Silk/i.test(navigator.userAgent) ||
        (window.innerWidth > 0 && window.innerWidth < 768);
    if (self._navLowMemory) {
        window.TruckDeckNavLowMemory = true;
    }

    function routePathSimilar(a, b) {
        if (!a || !b || a.length < 2 || b.length < 2) return false;
        if (Math.abs(a.length - b.length) > Math.max(8, a.length * 0.05)) return false;
        var picks = [0, Math.floor(a.length / 2), a.length - 1];
        for (var i = 0; i < picks.length; i++) {
            var idx = picks[i];
            if (!b[idx]) return false;
            if (Math.abs(a[idx][0] - b[idx][0]) > 0.002 || Math.abs(a[idx][1] - b[idx][1]) > 0.002) {
                return false;
            }
        }
        return true;
    }

    function resetAllRoutes(reason, force) {
        var now = Date.now();
        if (!force && reason !== 'quick-travel' && now - (self._lastRouteResetAt || 0) < 8000) {
            return;
        }
        self._lastRouteResetAt = now;
        if (reason) console.log('[TruckDeck NAV] Resetting route:', reason);
        invalidateGpsRoute();
        self._routeRetryAt = 0;
        self._navMatchInFlight = false;
        self._pendingRoute = null;
        if (self._pmtilesMap) self._pmtilesMap.clearRoute();
        if (self._pmtilesMap && self._pmtilesMap.clearCheckpoint) self._pmtilesMap.clearCheckpoint();
    }

    function detectQuickTravel(data, posX, posZ, kmh) {
        if (!isFinite(posX) || !isFinite(posZ)) return false;
        // Ferry/train SDK flags often stick or oscillate — only reset on real teleports.
        var jumped = false;
        var last = self._routeWatchPos;
        if (last && isFinite(last.x) && isFinite(last.z)) {
            var dx = posX - last.x;
            var dz = posZ - last.z;
            var distM = Math.sqrt(dx * dx + dz * dz);
            var dt = Math.max(Date.now() - (last.t || 0), 50);
            var speedMs = Math.max(kmh || 0, 2) / 3.6;
            var plausibleM = speedMs * (dt / 1000) + 120;
            var jumpMin = self._navLowMemory ? 3500 : 2000;
            var jumpFactor = self._navLowMemory ? 4.5 : 3.5;
            if (distM > Math.max(jumpMin, plausibleM * jumpFactor)) jumped = true;
        }
        self._routeWatchPos = { x: posX, z: posZ, t: Date.now() };
        if (jumped) {
            resetAllRoutes('quick-travel', true);
            return true;
        }
        return false;
    }

    function sameJobCityRoute(city, key) {
        return !!(self._gpsRoute && self._gpsRoute.method === 'job-city' &&
            self._gpsRoute.jobCity === city && self._gpsRoute.key === key &&
            self._gpsRoute.fullPath && self._gpsRoute.fullPath.length > 1);
    }

    function haversineMeters(a, b) {
        if (!a || !b) return Infinity;
        var R = 6371000;
        var dLat = (b[1] - a[1]) * Math.PI / 180;
        var dLon = (b[0] - a[0]) * Math.PI / 180;
        var lat1 = a[1] * Math.PI / 180;
        var lat2 = b[1] * Math.PI / 180;
        var h = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
        return 2 * R * Math.asin(Math.sqrt(h));
    }

    /** Graph routes start at the nearest node — prepend truck so the line begins at the cab. */
    function anchorRouteAtTruck(coords, fromLngLat) {
        if (!coords || coords.length < 2 || !fromLngLat) return coords;
        if (haversineMeters(fromLngLat, coords[0]) > 80) {
            return [[fromLngLat[0], fromLngLat[1]]].concat(coords);
        }
        return coords;
    }

    function refreshJobCityTrim(fromLngLat, navM) {
        if (!self._gpsRoute || !self._gpsRoute.fullPath) return false;
        var trimmed = trimJobCityRoute(self._gpsRoute.fullPath, fromLngLat, navM);
        if (!trimmed || trimmed.length < 2) return false;
        self._gpsRoute.path = trimmed;
        self._gpsRoute.lastNavM = navM;
        return true;
    }

    function gpsNeedsFullReroute(routePath, ll, navM, gpsRoute) {
        if (!routePath || !ll || !isFinite(navM) || navM < 50) return true;
        var G = window.TruckDeckNavGuidance;
        if (gpsRoute && (gpsRoute.method === 'job-city' || gpsRoute.method === 'city' ||
            gpsRoute.method === 'city-weak') && gpsRoute.fullPath) {
            if (G && G.distanceToRouteMeters) {
                var offJob = G.distanceToRouteMeters(gpsRoute.fullPath, ll);
                if (isFinite(offJob) && offJob > 3000) return true;
            }
            if (G && G.remainingPathMeters) {
                var remainJob = G.remainingPathMeters(gpsRoute.fullPath, ll);
                if (isFinite(remainJob) && Math.abs(remainJob - navM) > Math.max(20000, navM * 0.06)) {
                    return true;
                }
            }
        }
        if (gpsRoute && gpsRoute.method === 'job-city') {
            var lastNavJ = gpsRoute.lastNavM;
            if (isFinite(lastNavJ)) {
                if (navM > lastNavJ + Math.max(500, lastNavJ * 0.04)) return true;
                if (lastNavJ - navM > Math.max(2500, lastNavJ * 0.25)) return true;
            }
            return false;
        }
        var poorMatch = false;
        if (gpsRoute && isFinite(gpsRoute.matchErrorM) && isFinite(navM)) {
            poorMatch = gpsRoute.matchErrorM > Math.max(5000, navM * 0.18);
        }
        var lockedUntil = gpsRoute && gpsRoute.lockedUntil ? gpsRoute.lockedUntil : 0;
        if (lockedUntil && Date.now() < lockedUntil &&
            gpsRoute.method !== 'job-city') {
            return false;
        }
        if (!poorMatch && G && G.distanceToRouteMeters) {
            var offM = G.distanceToRouteMeters(routePath, ll);
            var offTol = (gpsRoute && gpsRoute.method === 'job-city') ? 80 : 120;
            if (isFinite(offM) && offM > offTol) return true;
        }
        if (!poorMatch && G && G.remainingPathMeters) {
            var remain = G.remainingPathMeters(routePath, ll);
            if (isFinite(remain)) {
                var drift = Math.abs(remain - navM);
                var tol = Math.max(3500, navM * 0.2);
                if (gpsRoute && gpsRoute.method === 'job-city') {
                    tol = Math.max(2500, navM * 0.12);
                }
                if (drift > tol) return true;
            }
        }
        var lastNav = gpsRoute && isFinite(gpsRoute.lastNavM) ? gpsRoute.lastNavM : NaN;
        if (isFinite(lastNav)) {
            if (navM > lastNav + Math.max(500, lastNav * 0.04)) return true;
            if (lastNav - navM > Math.max(2500, lastNav * 0.25)) return true;
        }
        return false;
    }

    function invalidateGpsRoute() {
        if (self._gpsRoute) {
            self._gpsRoute.path = null;
            self._gpsRoute.key = null;
        }
        self._activeRoutePath = null;
        self._routeKey = null;
    }

    function pathLengthFromCoords(coords) {
        if (!coords || coords.length < 2) return 0;
        var total = 0;
        for (var i = 1; i < coords.length; i++) {
            var a = coords[i - 1];
            var b = coords[i];
            var R = 6371000;
            var dLat = (b[1] - a[1]) * Math.PI / 180;
            var dLon = (b[0] - a[0]) * Math.PI / 180;
            var lat1 = a[1] * Math.PI / 180;
            var lat2 = b[1] * Math.PI / 180;
            var h = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
                Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
            total += 2 * R * Math.asin(Math.sqrt(h));
        }
        return total;
    }

    function routeRemainingMeters(coords, fromLngLat) {
        var G = window.TruckDeckNavGuidance;
        if (G && G.remainingPathMeters && fromLngLat && coords && coords.length > 1) {
            var remain = G.remainingPathMeters(coords, fromLngLat);
            if (isFinite(remain) && remain > 0) return remain;
        }
        return pathLengthFromCoords(coords);
    }

    function gpsMatchAcceptable(result, navM) {
        if (!result || !result.path || result.path.length < 2) return false;
        var method = result.method || 'graph';
        if (!isFinite(navM) || navM < 50) return true;
        var err = result.matchErrorM;
        if (!isFinite(err)) return true;
        var tol = Math.max(1500, navM * 0.12);
        if (method === 'city' || method === 'city-weak') tol = Math.max(3000, navM * 0.22);
        if (method === 'job-city') tol = Math.max(4000, navM * 0.25);
        if (method === 'job-city-approx') tol = Math.max(8000, navM * 0.18);
        if (method === 'heading') tol = Math.max(8000, navM * 0.25);
        if (err > tol) return false;
        if (method === 'graph-fallback' && err > Math.max(800, navM * 0.06)) return false;
        return true;
    }

    function trimJobCityRoute(coords, fromLngLat, navM) {
        if (!coords || coords.length < 2) return coords;
        if (fromLngLat && isFinite(navM) && navM >= 50 &&
            haversineMeters(fromLngLat, coords[0]) < 200) {
            var head = [[fromLngLat[0], fromLngLat[1]]];
            var accum = 0;
            for (var j = 1; j < coords.length; j++) {
                var fromPt = head[head.length - 1];
                var toPt = coords[j];
                var stepM = haversineMeters(fromPt, toPt);
                if (accum + stepM > navM) {
                    var need = navM - accum;
                    var frac = stepM > 0 ? need / stepM : 0;
                    head.push([
                        fromPt[0] + (toPt[0] - fromPt[0]) * frac,
                        fromPt[1] + (toPt[1] - fromPt[1]) * frac
                    ]);
                    return head;
                }
                accum += stepM;
                head.push(toPt);
            }
            return head;
        }
        var G = window.TruckDeckNavGuidance;
        if (!G || !G.routePrefixFromTruck || !fromLngLat || !isFinite(navM) || navM < 50) {
            return coords;
        }
        return G.routePrefixFromTruck(coords, fromLngLat, navM);
    }

    function applyJobCityRoute(coords, game, target, key, city, statusPrefix, fromLngLat) {
        if (!coords || coords.length < 2) return false;
        coords = anchorRouteAtTruck(coords, fromLngLat);
        var trimmed = trimJobCityRoute(coords, fromLngLat, target.navM);
        var remainM = routeRemainingMeters(coords, fromLngLat);
        var err = Math.abs(remainM - target.navM);
        var result = {
            path: trimmed,
            matchErrorM: err,
            lengthM: remainM,
            method: err <= Math.max(4000, target.navM * 0.08) ? 'job-city' : 'job-city-approx',
            dest: { name: city }
        };
        if (!gpsMatchAcceptable(result, target.navM)) {
            console.warn('[TruckDeck NAV] Job-city route rejected:',
                'remain', Math.round(remainM), 'm',
                'err', Math.round(err), 'm',
                'nav', Math.round(target.navM || 0), 'm',
                'dest', city);
            self._jobCitySkipUntil = Date.now() + 60000;
            return false;
        }
        if (sameJobCityRoute(city, key)) {
            self._gpsRoute.fullPath = coords;
            refreshJobCityTrim(fromLngLat, target.navM);
            self._applyRouteCoords(coords, statusPrefix || self._gpsRoute.label || ('GPS → ' + city), true);
            return true;
        }
        if (self._gpsRoute && self._gpsRoute.path && self._gpsRoute.method === 'job-city' &&
            routePathSimilar(self._gpsRoute.path, trimmed)) {
            self._gpsRoute.lastNavM = target.navM;
            self._gpsRoute.fullPath = coords;
            self._applyRouteCoords(coords, self._gpsRoute.label || ('GPS → ' + city), true);
            return true;
        }
        var label = statusPrefix || ('GPS → ' + city);
        console.log('[TruckDeck NAV] Game GPS route: job-city (fallback)',
            'remain', Math.round(remainM), 'm',
            'err', Math.round(err), 'm',
            'nav', Math.round(target.navM || 0), 'm',
            'dest', city);
        self._gpsRoute = self._gpsRoute || { game: game, peakNavM: target.navM };
        self._gpsRoute.key = key;
        self._gpsRoute.fullPath = coords;
        self._gpsRoute.path = trimmed;
        self._gpsRoute.dest = { name: city };
        self._gpsRoute.label = label;
        self._gpsRoute.method = result.method;
        self._gpsRoute.lastNavM = target.navM;
        self._gpsRoute.jobCity = city;
        self._gpsRoute.matchErrorM = err;
        self._gpsRoute.lockedUntil = Date.now() + 120000;
        self._gpsLastNavM = target.navM;
        self._jobCitySkipUntil = 0;
        self._applyRouteCoords(coords, label, true);
        if (result.method === 'job-city-approx') {
            console.warn('[TruckDeck NAV] Job-city route approximate:',
                'remain', Math.round(remainM), 'm',
                'err', Math.round(err), 'm',
                'nav', Math.round(target.navM || 0), 'm',
                'dest', city);
        }
        return true;
    }

    function tryJobCityRoute(game, fromLngLat, target, key, statusPrefix) {
        var city = target.jobCity;
        if (!city || !window.TruckDeckRouter) return Promise.resolve(false);
        if (sameJobCityRoute(city, key)) {
            refreshJobCityTrim(fromLngLat, target.navM);
            return Promise.resolve(true);
        }
        if (self._jobCitySkipUntil && Date.now() < self._jobCitySkipUntil) {
            return Promise.resolve(false);
        }
        return window.TruckDeckRouter.getRoute(game, fromLngLat, city).then(function (coords) {
            return applyJobCityRoute(anchorRouteAtTruck(coords, fromLngLat), game, target, key, city, statusPrefix, fromLngLat);
        });
    }

    function applyGpsNavResult(result, game, target, key, statusPrefix) {
        if (!result || !result.path || result.path.length < 2) return false;
        if (!gpsMatchAcceptable(result, target.navM)) {
            console.warn('[TruckDeck NAV] GPS route match rejected:',
                result.method || 'graph',
                'err', Math.round(result.matchErrorM || 0), 'm',
                'nav', Math.round(target.navM || 0), 'm');
            return false;
        }
        if (self._gpsRoute && self._gpsRoute.path && routePathSimilar(self._gpsRoute.path, result.path)) {
            self._gpsRoute.lastNavM = target.navM;
            self._gpsRoute.matchErrorM = result.matchErrorM;
            return true;
        }
        var destLabel = result.dest && result.dest.name && result.dest.name !== 'GPS'
            ? result.dest.name
            : 'nav';
        var label = statusPrefix || ('GPS route · ' + Math.round(result.matchErrorM || 0) + ' m');
        console.log('[TruckDeck NAV] Game GPS route:', result.method || 'graph',
            'len', Math.round(result.lengthM || 0), 'm',
            'err', Math.round(result.matchErrorM || 0), 'm',
            'nav', Math.round(target.navM || 0), 'm');
        self._gpsRoute = self._gpsRoute || { game: game, peakNavM: target.navM };
        self._gpsRoute.key = key;
        self._gpsRoute.path = result.path;
        self._gpsRoute.fullPath = result.path;
        self._gpsRoute.dest = result.dest;
        self._gpsRoute.label = label;
        self._gpsRoute.method = result.method || 'graph';
        self._gpsRoute.lastNavM = target.navM;
        self._gpsRoute.jobCity = target.jobCity || (result.dest && result.dest.name) ||
            (self._gpsRoute && self._gpsRoute.jobCity) || null;
        self._gpsRoute.matchErrorM = result.matchErrorM;
        self._gpsRoute.lockedUntil = Date.now() + 90000;
        self._gpsLastNavM = target.navM;
        self._applyRouteCoords(result.path, label);
        return true;
    }

    function matchGpsRoute(game, fromLngLat, target, key, statusPrefix) {
        return runGpsMatchers(game, fromLngLat, target, key, statusPrefix);
    }

    function runGraphGpsMatchers(game, fromLngLat, target, key, statusPrefix) {
        return window.TruckDeckRouter.matchNavRouteByDistance(
            game, fromLngLat, target.navM, self._lastHeading).then(function (result) {
            if (applyGpsNavResult(result, game, target, key, statusPrefix)) return true;
            return window.TruckDeckRouter.matchNavDestination(
                game, fromLngLat, target.navM, self._lastHeading).then(function (fallback) {
                var label = fallback && fallback.dest && fallback.dest.name
                    ? ('GPS → ' + fallback.dest.name) : null;
                return applyGpsNavResult(fallback, game, target, key, label || statusPrefix);
            });
        });
    }

    function tryCityNavMatch(game, fromLngLat, target, key, statusPrefix) {
        if (!target.jobCity || !window.TruckDeckRouter ||
            !window.TruckDeckRouter.matchNavToCity) {
            return Promise.resolve(false);
        }
        return window.TruckDeckRouter.matchNavToCity(
            game, fromLngLat, target.jobCity, target.navM, self._lastHeading).then(function (result) {
            var label = statusPrefix || ('GPS → ' + target.jobCity);
            return applyGpsNavResult(result, game, target, key, label);
        });
    }

    function runGpsMatchers(game, fromLngLat, target, key, statusPrefix) {
        return runGraphGpsMatchers(game, fromLngLat, target, key, statusPrefix).then(function (ok) {
            if (ok) {
                self._graphFailLogged = false;
                return true;
            }
            return tryCityNavMatch(game, fromLngLat, target, key, statusPrefix).then(function (cityOk) {
                if (cityOk) {
                    self._graphFailLogged = false;
                    return true;
                }
                if (target.jobCity) {
                    if (!self._graphFailLogged) {
                        console.warn('[TruckDeck NAV] Graph GPS match unavailable — using job-city fallback');
                        self._graphFailLogged = true;
                    }
                    return tryJobCityRoute(game, fromLngLat, target, key, statusPrefix);
                }
                return false;
            });
        });
    }

    function graphBeatsJobCity(result, gpsRoute) {
        if (!result || !result.path || result.path.length < 2) return false;
        if (!gpsRoute || gpsRoute.method !== 'job-city' || !gpsRoute.path) return true;
        var graphErr = result.matchErrorM;
        if (!isFinite(graphErr)) return true;
        var jobErr = isFinite(gpsRoute.matchErrorM) ? gpsRoute.matchErrorM : Infinity;
        if (graphErr <= Math.max(8000, (gpsRoute.lastNavM || 0) * 0.08)) return true;
        return graphErr < jobErr + 500;
    }

    self._refreshGameNavRoute = function (game, fromLngLat, navM, headingDeg, force) {
        if (!window.TruckDeckRouter || !fromLngLat || !isFinite(navM) || navM < 50) return;
        var now = Date.now();
        var throttleMs = force ? 0 : (self._navLowMemory ? 30000 : (self._gpsRoute && self._gpsRoute.method === 'job-city' ? 120000 : 8000));
        if (!force && now - self._gpsNavRefreshAt < throttleMs) return;
        self._gpsNavRefreshAt = now;
        function applyRefreshResult(result) {
            if (!result || !result.path || result.path.length < 2) return false;
            if (!gpsMatchAcceptable(result, navM)) return false;
            self._graphFailLogged = false;
            if (!force && self._gpsRoute && self._gpsRoute.path &&
                routePathSimilar(self._gpsRoute.path, result.path)) {
                self._gpsRoute.lastNavM = navM;
                return true;
            }
            var peak = (self._gpsRoute && self._gpsRoute.peakNavM) ? self._gpsRoute.peakNavM : navM;
            var key = game + '|gps|' + Math.round(peak / 500);
            var prevErr = (self._gpsRoute && isFinite(self._gpsRoute.matchErrorM))
                ? self._gpsRoute.matchErrorM : Infinity;
            var upgradeFromJobCity = self._gpsRoute && self._gpsRoute.method === 'job-city' &&
                (result.method || 'graph') !== 'job-city' && graphBeatsJobCity(result, self._gpsRoute);
            if (!force && !upgradeFromJobCity && self._gpsRoute && self._gpsRoute.path) {
                if (isFinite(result.matchErrorM) && result.matchErrorM > prevErr * 1.15 &&
                    result.matchErrorM > 1200) {
                    self._gpsRoute.lastNavM = navM;
                    return true;
                }
                if (!routePathSimilar(self._gpsRoute.path, result.path) &&
                    isFinite(result.matchErrorM) && result.matchErrorM >= prevErr * 0.9) {
                    self._gpsRoute.lastNavM = navM;
                    return true;
                }
            }
            applyGpsNavResult(result, game, { navM: navM, key: key }, key, self._gpsRoute && self._gpsRoute.label);
            if (self._gpsRoute) {
                self._gpsRoute.matchErrorM = result.matchErrorM;
            }
            return true;
        }
        window.TruckDeckRouter.matchNavRouteByDistance(game, fromLngLat, navM, headingDeg).then(function (result) {
            if (applyRefreshResult(result)) return;
            if (self._gpsRoute && self._gpsRoute.path && self._gpsRoute.path.length > 1) return;
            return window.TruckDeckRouter.matchNavDestination(game, fromLngLat, navM, headingDeg).then(function (fallback) {
                applyRefreshResult(fallback);
            });
        });
    };

    function readNavDistM(data) {
        if (!data || !data.navigation) return NaN;
        var navM = Number(data.navigation.estimatedDistance);
        if (!isFinite(navM) || navM <= 0) navM = Number(data.navigation.routeDistance);
        return navM;
    }

    function isGpsActive(data) {
        var navM = readNavDistM(data);
        return isFinite(navM) && navM > 50;
    }

    function readManualDest() {
        try {
            var raw = localStorage.getItem('tdn.manualDest');
            if (!raw) return null;
            var parsed = JSON.parse(raw);
            if (!parsed || !isFinite(parsed.lng) || !isFinite(parsed.lat)) return null;
            return [parsed.lng, parsed.lat];
        } catch (e) { return null; }
    }

    function saveManualDest(lng, lat) {
        try {
            localStorage.setItem('tdn.manualDest', JSON.stringify({ lng: lng, lat: lat }));
        } catch (e) { /* ignore */ }
    }

    function clearManualDest() {
        try { localStorage.removeItem('tdn.manualDest'); } catch (e) { /* ignore */ }
    }

    function clearGpsRoute() {
        self._gpsRoute = null;
    }

    function isValidCityName(city) {
        if (!city) return false;
        city = String(city).trim();
        if (city.length < 2) return false;
        if (/^\d+$/.test(city)) return false;
        var bad = ['distance', 'null', 'undefined', 'unknown', 'none'];
        return bad.indexOf(city.toLowerCase()) < 0;
    }

    function resolveDestCity(data) {
        if (!data) return '';
        var rootCandidates = [
            data.destinationCity,
            data.cityDst,
            data.cargoDestinationCity
        ];
        for (var r = 0; r < rootCandidates.length; r++) {
            var rootCity = rootCandidates[r] ? String(rootCandidates[r]).trim() : '';
            if (isValidCityName(rootCity)) return rootCity;
        }
        if (!data.job) return '';
        var j = data.job;
        var candidates = [
            j.destinationCity,
            j.cityDst,
            j.cargoDestinationCity,
            j.destinationCityId,
            j.destination && j.destination.city && (j.destination.city.name || j.destination.city.id)
        ];
        for (var i = 0; i < candidates.length; i++) {
            var city = candidates[i] ? String(candidates[i]).trim() : '';
            if (isValidCityName(city)) return city;
        }
        return '';
    }

    function resolveRouteTarget(data, ll, game) {
        var navM = readNavDistM(data);
        var jobCity = resolveDestCity(data);
        var modeKey = '|' + self._navMode;

        // Active in-game GPS — match remaining distance on the road graph (same as game nav).
        if (ll && isFinite(navM) && navM > 50) {
            clearManualDest();
            if (!self._gpsRoute || self._gpsRoute.game !== game) {
                self._gpsRoute = { game: game, peakNavM: navM };
            } else if (navM > self._gpsRoute.peakNavM * 1.04 + 250) {
                self._gpsRoute = { game: game, peakNavM: navM };
            } else {
                self._gpsRoute.peakNavM = Math.max(self._gpsRoute.peakNavM, navM);
            }
            return {
                kind: 'gps',
                key: game + '|gps|' + Math.round(self._gpsRoute.peakNavM / 500) + modeKey,
                navM: navM,
                jobCity: jobCity || null
            };
        }

        if (jobCity) {
            clearManualDest();
            clearGpsRoute();
            return { kind: 'city', key: game + '|job|' + jobCity + modeKey, city: jobCity, navM: navM };
        }

        var manual = readManualDest();
        if (manual) {
            return {
                kind: 'lnglat',
                key: game + '|manual|' + manual[0].toFixed(3) + ',' + manual[1].toFixed(3) + modeKey,
                lngLat: manual
            };
        }

        return null;
    }

    self._applyRouteCoords = function (coords, statusMsg, force) {
        if (!coords || coords.length < 2) {
            self._activeRoutePath = null;
            return false;
        }
        if (!force && self._activeRoutePath && routePathSimilar(self._activeRoutePath, coords)) {
            return true;
        }
        self._activeRoutePath = coords;
        if (coords && coords.length > 1 && self._pmtilesMap) {
            var map = self._pmtilesMap;
            var msg = statusMsg || 'Map ready';
            setTimeout(function () {
                if (self._activeRoutePath !== coords || !map) return;
                map.setRoute(coords);
                setMapStatus(msg);
            }, 0);
            return true;
        }
        return false;
    };

    self._scheduleRouteCompute = function (game, target, fromLngLat) {
        self._pendingRouteCompute = { game: game, target: target, from: fromLngLat };
        function runScheduled() {
            if (self._routeComputeTimer) return;
            self._routeComputeTimer = setTimeout(function () {
                self._routeComputeTimer = 0;
                var pending = self._pendingRouteCompute;
                self._pendingRouteCompute = null;
                if (pending && self._computeRoute) {
                    self._computeRoute(pending.game, pending.target, pending.from);
                }
            }, 0);
        }
        if (self._navLowMemory && !self._mapInteractive) {
            return;
        }
        runScheduled();
    };

    self._flushPendingRoute = function () {
        if (!self._pendingRoute || !self._pmtilesMap) return;
        if (self._navLowMemory && !self._mapInteractive) return;
        var pr = self._pendingRoute;
        self._pendingRoute = null;
        self._scheduleRouteCompute(pr.game, pr.target, pr.from);
    };

    self._mapRouteReady = function () {
        return !!(self._mapReady && self._pmtilesMap &&
            (!self._navLowMemory || self._mapInteractive));
    };

    self._routingReady = null;

    function checkRoutingAssets(game) {
        if (!window.TruckDeckRouter || !window.TruckDeckRouter.preflight) return Promise.resolve(false);
        return window.TruckDeckRouter.preflight(game).then(function (st) {
            self._routingReady = !!st.ready;
            if (!st.ready) {
                console.warn('[TruckDeck NAV] Routing data missing (graph:', st.graph, 'cities:', st.cities, ')');
            }
            return st.ready;
        })['catch'](function () {
            self._routingReady = false;
            return false;
        });
    }

    self._computeRoute = function (game, target, fromLngLat) {
        if (!window.TruckDeckRouter || !target) return;
        var key = target.key;
        self._routeKey = key;

        function onRouteError(err, retryMsg) {
            console.warn('[TruckDeck NAV] Route computation failed', err);
            var msg = (err && err.message) ? String(err.message) : '';
            if (msg.indexOf('404') >= 0 || msg.indexOf('routing data') >= 0) {
                setMapStatus('Routing data missing — run Map Generator', true);
            } else if (self._routeKey === key) {
                setMapStatus(retryMsg || 'Route failed — retrying…', true);
                self._routeRetryAt = Date.now() + 15000;
            }
        }

        function ensureRoutingThen(run) {
            if (self._routingReady === true) {
                run();
                return;
            }
            if (self._routingReady === false) {
                setMapStatus('Routing data missing — run Map Generator', true);
                self._routeRetryAt = Date.now() + 60000;
                return;
            }
            checkRoutingAssets(game).then(function (ok) {
                if (!ok) {
                    setMapStatus('Routing data missing — run Map Generator', true);
                    self._routeRetryAt = Date.now() + 60000;
                    return;
                }
                run();
            });
        }

        setMapStatus('Computing route…');

        if (target.kind === 'city') {
            ensureRoutingThen(function () {
            function tryGpsFallback() {
                if (!isFinite(target.navM) || target.navM < 50) return Promise.resolve(false);
                var gpsKey = game + '|gps|' + Math.round(target.navM / 500) + '|' + self._navMode;
                self._routeKey = gpsKey;
                return matchGpsRoute(game, fromLngLat, { navM: target.navM, key: gpsKey }, gpsKey, 'GPS route');
            }
            window.TruckDeckRouter.getRoute(game, fromLngLat, target.city).then(function (coords) {
                if (self._routeKey !== key) return;
                if (self._applyRouteCoords(coords, 'Map ready')) return;
                return tryGpsFallback().then(function (ok) {
                    if (ok || self._routeKey !== key) return;
                    console.warn('[TruckDeck NAV] No graph route for', target.city, '- trying direct line');
                    return window.TruckDeckRouter.resolveCityLngLat(game, target.city).then(function (dest) {
                        if (self._routeKey !== key || !dest || !self._pmtilesMap) {
                            self._routeRetryAt = Date.now() + 15000;
                            setMapStatus('Route unavailable', true);
                            return;
                        }
                        self._pmtilesMap.setRoute([fromLngLat, dest]);
                        self._activeRoutePath = [fromLngLat, dest];
                        setMapStatus('Map ready');
                    });
                });
            })['catch'](function (err) {
                onRouteError(err);
            });
            });
            return;
        }

        if (target.kind === 'lnglat') {
            ensureRoutingThen(function () {
            window.TruckDeckRouter.getRouteToLngLat(game, fromLngLat, target.lngLat).then(function (coords) {
                if (self._routeKey !== key) return;
                if (self._applyRouteCoords(coords, 'Map ready')) return;
                if (self._routeKey === key && self._pmtilesMap) {
                    self._pmtilesMap.setRoute([fromLngLat, target.lngLat]);
                    self._activeRoutePath = [fromLngLat, target.lngLat];
                    setMapStatus('Map ready');
                }
            })['catch'](function (err) {
                onRouteError(err, 'Route failed — tap map to retry');
            });
            });
            return;
        }

        if (target.kind === 'gps') {
            if (self._gpsRoute && self._gpsRoute.key === target.key && self._gpsRoute.path && self._gpsRoute.path.length > 1) {
                self._applyRouteCoords(self._gpsRoute.path, self._gpsRoute.label || 'GPS route');
                self._refreshGameNavRoute(game, fromLngLat, target.navM, self._lastHeading, false);
                return;
            }
            if (self._navMatchInFlight) return;
            self._navMatchInFlight = true;
            var matchToken = Date.now();
            self._navMatchToken = matchToken;
            setTimeout(function () {
                if (self._navMatchInFlight && self._navMatchToken === matchToken) {
                    self._navMatchInFlight = false;
                    console.warn('[TruckDeck NAV] GPS route match timed out — using cached route');
                }
            }, 120000);
            setMapStatus('Matching in-game GPS route…');
            ensureRoutingThen(function () {
            matchGpsRoute(game, fromLngLat, target, key).then(function (ok) {
                self._navMatchInFlight = false;
                if (self._routeKey !== key) return;
                if (!ok) {
                    self._routeRetryAt = Date.now() + 15000;
                    setMapStatus('GPS route unavailable — retrying…', true);
                }
            })['catch'](function (err) {
                self._navMatchInFlight = false;
                onRouteError(err, 'GPS route unavailable — retrying…');
            });
            });
        }
    };

    function initMap(game) {
        var host = mapHost || document.getElementById('tdn-map');
        if (!host || !window.TruckDeckPmtilesMap) {
            console.warn('[TruckDeck NAV] Cannot init map: mapHost or TruckDeckPmtilesMap missing');
            return;
        }
        mapHost = host;
        if (self._pmtilesMap) {
            self._pmtilesMap.destroy();
            self._pmtilesMap = null;
        }
        self._activeGame = game;
        self._routeKey = null;
        self._routeRetryAt = 0;
        self._mapReady = false;
        self._mapInteractive = false;
        clearGpsRoute();

        var dash = rootEl || mapHost.closest('.dashboard');
        if (dash) {
            dash.style.position = 'absolute';
            dash.style.inset = '0';
            dash.style.width = '100%';
            dash.style.height = '100%';
            dash.style.maxHeight = '100%';
            dash.style.minHeight = '0';
        }
        mapHost.style.position = 'absolute';
        mapHost.style.left = '0';
        mapHost.style.right = '0';
        mapHost.style.width = '100%';
        mapHost.style.minHeight = '0';

        self._pmtilesMap = new window.TruckDeckPmtilesMap(mapHost, {
            game: game,
            follow: true,
            rotate: true,
            followPitch: 45,
            followTruckScreenY: 0.8,
            fullMapFeatures: true,
            mode: self._mode,
            onStatus: function (msg) {
                if (msg) setMapStatus(msg);
            },
            onInteractive: function () {
                self._mapInteractive = true;
                if (self._pendingRouteCompute && self._scheduleRouteCompute) {
                    var p = self._pendingRouteCompute;
                    self._scheduleRouteCompute(p.game, p.target, p.from);
                }
                self._flushPendingRoute();
                if (window.TruckDeckRouter && window.TruckDeckRouter.warmup) {
                    window.TruckDeckRouter.warmup();
                }
            },
            onPickDestination: function (lngLat) {
                if (!lngLat || !isFinite(lngLat[0]) || !isFinite(lngLat[1])) return;
                saveManualDest(lngLat[0], lngLat[1]);
                self._routeKey = null;
                self._routeRetryAt = 0;
                if (self._lastLngLat && self._mapReady) {
                    self._computeRoute(game, {
                        kind: 'lnglat',
                        key: game + '|manual|' + lngLat[0].toFixed(3) + ',' + lngLat[1].toFixed(3) + '|' + self._navMode,
                        lngLat: lngLat
                    }, self._lastLngLat);
                }
            }
        });
        setMapStatus('Loading map…');
        self._pmtilesMap.init().then(function () {
            console.log('[TruckDeck NAV] Map initialization successful');
            self._mapReady = true;
            setMapStatus('Map ready');
            syncChromeInsets();
            self._flushPendingRoute();
        })['catch'](function (err) {
            console.error('[TruckDeck NAV] Map initialization failed:', err);
            var msg = (err && err.message) ? String(err.message) : '';
            if (msg.indexOf('proj4') >= 0 || msg.indexOf('map libraries') >= 0) {
                setMapStatus('Map libraries failed to load', true);
            } else if (msg.indexOf('PMTiles') >= 0 || msg.indexOf('No PMTiles') >= 0 || msg.indexOf('404') >= 0) {
                setMapStatus('No map file — generate PMTiles in Map Generator', true);
            } else if (msg) {
                setMapStatus('Map failed: ' + msg, true);
            } else {
                setMapStatus('Map failed to load', true);
            }
        });
    }

    self._ensureMapForGame = function (game) {
        if (!self._mapScriptsLoaded || !window.TruckDeckPmtilesMap) return;
        if (game === self._activeGame && self._pmtilesMap) return;
        initMap(game);
    };

    loadMapScripts().then(function () {
        initMap('ets2');
    })['catch'](function (err) {
        console.error(err);
        var missing = (err && err.message) ? err.message : 'scripts';
        setMapStatus('Could not load ' + missing, true);
    });

    self._clearGpsRoute = clearGpsRoute;
    self._resolveRouteTarget = resolveRouteTarget;
    self._detectQuickTravel = detectQuickTravel;
    self._gpsNeedsFullReroute = gpsNeedsFullReroute;
    self._invalidateGpsRoute = invalidateGpsRoute;
    self._refreshJobCityTrim = refreshJobCityTrim;
    self._sameJobCityRoute = sameJobCityRoute;
};

Funbit.Ets.Telemetry.Dashboard.prototype.filter = function (data, utils) {
    var self = this;
    if (!data || !data.game) return data;

    function rv(path) {
        var parts = path.split('.');
        var v = data;
        for (var j = 0; j < parts.length && v != null; j++) v = v[parts[j]];
        return v;
    }

    function num(paths) {
        for (var i = 0; i < paths.length; i++) {
            var v = rv(paths[i]);
            if (typeof v === 'number' && isFinite(v)) return v;
        }
        return NaN;
    }

    function bool(paths) {
        for (var i = 0; i < paths.length; i++) {
            var v = rv(paths[i]);
            if (typeof v === 'boolean') return v;
        }
        return false;
    }

    function str(paths) {
        for (var i = 0; i < paths.length; i++) {
            var v = rv(paths[i]);
            if (typeof v === 'string' && v) return v;
        }
        return '';
    }

    function pad(n) {
        return (n < 10 ? '0' : '') + n;
    }

    function formatGameDur(ms) {
        if (!isFinite(ms) || ms <= 0) return '--';
        var m = Math.round(ms / 60000);
        var h = Math.floor(m / 60);
        return h > 0 ? h + 'h ' + pad(m % 60) + 'm' : (m % 60) + 'm';
    }

    function formatHmLeft(totalSec) {
        if (!isFinite(totalSec) || totalSec <= 0) return '--';
        var totalMin = Math.round(totalSec / 60);
        var h = Math.floor(totalMin / 60);
        var m = totalMin % 60;
        if (h > 0) return h + 'h' + pad(m) + 'm';
        return m + 'm';
    }

    function readNavTimeSec(data) {
        var sec = num(['navigation.routeTimeSeconds']);
        if (isFinite(sec) && sec > 0) return sec;
        var navTime = data.navigation && data.navigation.estimatedTime;
        if (navTime) {
            var epoch = new Date('0001-01-01T00:00:00Z').getTime();
            var ms = new Date(navTime).getTime() - epoch;
            if (isFinite(ms) && ms > 0) return ms / 1000;
        }
        return NaN;
    }

    function readNavDistM(data) {
        if (!data || !data.navigation) return NaN;
        var navM = Number(data.navigation.estimatedDistance);
        if (!isFinite(navM) || navM <= 0) navM = Number(data.navigation.routeDistance);
        return navM;
    }

    function formatNavDistanceM(meters, isMph) {
        if (!isFinite(meters) || meters <= 0) return '--';
        if (isMph) {
            var mi = meters * 0.000621371;
            if (mi >= 10) return Math.round(mi) + 'mi';
            return mi.toFixed(1) + 'mi';
        }
        if (meters >= 10000) return Math.round(meters / 1000) + 'km';
        return (meters / 1000).toFixed(1) + 'km';
    }

    function formatWaypointGuidance(g, isMph) {
        if (!g || g.nextManeuverM == null || !g.hasTurn) return '--';
        return formatNavDistanceM(g.nextManeuverM, isMph);
    }

    var isMph = self.speedUnit === 'mph';
    var toUnit = function (v) { return isMph ? v * 0.621371 : v; };
    var kmh = Math.abs(num(['truck.speed']) || 0);
    var limitKmh = num(['navigation.speedLimit']);
    var distM = num(['navigation.estimatedDistance']);

    $('.js-speed').text(Math.round(toUnit(kmh)));
    $('.js-speed-unit').text(isMph ? 'MPH' : 'KM/H');
    $('.js-speedLimit').text((limitKmh && limitKmh > 0) ? Math.round(toUnit(limitKmh)) : '');
    var overspeed = limitKmh > 0 && kmh > limitKmh + 3;
    $('.js-speedLimit-wrap').toggleClass('overspeed', overspeed);

    if (isFinite(distM)) {
        if (isMph) {
            var tripMi = distM * 0.000621371;
            $('.js-dist').text(tripMi >= 10 ? Math.round(tripMi) : tripMi.toFixed(1));
            $('.js-dist-unit').text('MI');
        } else {
            var tripKm = distM / 1000;
            $('.js-dist').text(tripKm >= 10 ? Math.round(tripKm) : tripKm.toFixed(1));
            $('.js-dist-unit').text('KM');
        }
    } else {
        $('.js-dist').text('--');
    }

    var epoch = new Date('0001-01-01T00:00:00Z').getTime();
    var gameTime = data.game && data.game.time;
    var gameMs = gameTime ? new Date(gameTime).getTime() : NaN;
    var navSec = readNavTimeSec(data);
    if (isFinite(navSec) && navSec > 0 && isFinite(distM) && distM > 50) {
        $('.js-arrive').text(formatHmLeft(navSec));
    } else {
        $('.js-arrive').text('--');
    }
    if (isFinite(navSec) && navSec > 0 && isFinite(gameMs)) {
        var eta = new Date(gameMs + navSec * 1000);
        $('.js-eta').text('ETA ' + pad(eta.getUTCHours()) + ':' + pad(eta.getUTCMinutes()));
    } else {
        $('.js-eta').text('ETA --:--');
    }

    $('.js-rest').text(Funbit.Ets.Telemetry.Dashboard.formatRestRemaining(
        rv('game.nextRestStopTime'), data.game));

    var jobRemaining = rv('job.remainingTime');
    var jobMs = jobRemaining ? (new Date(jobRemaining).getTime() - epoch) : NaN;
    if (isFinite(jobMs) && jobMs > 0) {
        $('.js-job').text(formatGameDur(jobMs));
    } else {
        var deadlineTime = rv('job.deadlineTime');
        var deadlineMs = deadlineTime ? (new Date(deadlineTime).getTime() - epoch) : NaN;
        if (isFinite(deadlineMs) && deadlineMs > 0 && isFinite(gameMs)) {
            var diffMs = deadlineMs - gameMs;
            $('.js-job').text(diffMs > 0 ? formatGameDur(diffMs) : 'LATE');
        } else {
            $('.js-job').text('--');
        }
    }

    var gameName = (data.game && data.game.gameName) ? String(data.game.gameName) : 'ETS2';
    var game = gameName.toUpperCase().indexOf('ATS') >= 0 ? 'ats' : 'ets2';
    if (game !== self._activeGame && typeof self._ensureMapForGame === 'function') {
        self._ensureMapForGame(game);
    }

    var posX = num(['truck.placement.x', 'truck.placement.X', 'truck.coordinateX']);
    var posZ = num(['truck.placement.z', 'truck.placement.Z', 'truck.coordinateZ']);
    var heading = num(['truck.placement.heading', 'truck.placement.rotation']);

    if (isFinite(posX) && isFinite(posZ) && self._detectQuickTravel) {
        self._detectQuickTravel(data, posX, posZ, kmh);
    }

    var countryInfo = null;
    if (window.RcCountry) {
        if (!self._countriesEts2 && !self._countriesLoadPending) {
            self._countriesLoadPending = true;
            window.RcCountry.loadEts2Countries().then(function (list) {
                self._countriesEts2 = list;
                self._countriesLoadPending = false;
            })['catch'](function () {
                self._countriesLoadPending = false;
            });
        }
        var motion = null;
        if (isFinite(posX) && isFinite(posZ)) {
            motion = { prevX: self._countryPos.x, prevZ: self._countryPos.z };
            self._countryPos.x = posX;
            self._countryPos.z = posZ;
        }
        countryInfo = window.RcCountry.resolveCountry(
            gameName,
            posX,
            posZ,
            self._countriesEts2,
            {
                id: str(['truck.licensePlateCountryId']),
                name: str(['truck.licensePlateCountry'])
            },
            motion);
    }
    var $country = $('.js-country');
    if (countryInfo && countryInfo.name) {
        var cKey = countryInfo.token + '|' + countryInfo.name;
        if (cKey !== self._countryKey) {
            self._countryKey = cKey;
            $('.js-country-flag').text(countryInfo.flag || '');
            $('.js-country-name').text(countryInfo.name);
            $country.addClass('country-flash');
            setTimeout(function () { $country.removeClass('country-flash'); }, 400);
        }
        $country.prop('hidden', false);
    } else {
        self._countryKey = '';
        $country.prop('hidden', true);
    }

    var ll = null;
    if (self._pmtilesMap && window.TruckDeckProjection && isFinite(posX) && isFinite(posZ)) {
        ll = window.TruckDeckProjection.gameToLngLat(
            game === 'ats' ? 'ATS' : 'ETS2', posX, posZ);
        if (ll) {
            self._lastLngLat = ll;
            if (isFinite(heading)) {
                self._lastHeading = window.TruckDeckProjection.gameHeadingToBearingDeg(
                    game === 'ats' ? 'ATS' : 'ETS2', posX, posZ, heading);
            }
            self._pmtilesMap.setTruck(ll[0], ll[1], self._lastHeading || 0, kmh);
        }
    }

    var routeTarget = self._resolveRouteTarget ? self._resolveRouteTarget(data, ll, game) : null;
    var routeKey = routeTarget ? routeTarget.key : '';

    if (!routeTarget) {
        self._waypointText = '--';
        if (self._routeKey || self._gpsRoute) {
            self._routeKey = null;
            self._routeRetryAt = 0;
            self._pendingRoute = null;
            self._navMatchInFlight = false;
            self._activeRoutePath = null;
            if (self._clearGpsRoute) self._clearGpsRoute();
            if (self._pmtilesMap) self._pmtilesMap.clearRoute();
            if (self._pmtilesMap && self._pmtilesMap.clearCheckpoint) self._pmtilesMap.clearCheckpoint();
        }
        $('.js-waypoint').text('--');
    } else {
        var routePath = (self._gpsRoute && self._gpsRoute.path) ? self._gpsRoute.path : self._activeRoutePath;
        if (window.TruckDeckNavGuidance && routePath) {
            var navRemainM = readNavDistM(data);
            var peakNavM = (self._gpsRoute && self._gpsRoute.peakNavM) ? self._gpsRoute.peakNavM : navRemainM;
            var guidanceFn = window.TruckDeckNavGuidance.analyzeRouteFromGameNav
                || window.TruckDeckNavGuidance.analyzeRoute;
            if (!guidanceFn) {
                self._waypointText = '--';
            } else {
                var guidance = guidanceFn.call(
                    window.TruckDeckNavGuidance,
                    routePath,
                    navRemainM,
                    peakNavM,
                    ll,
                    self._lastHeading || 0,
                    navSec);
                self._waypointText = formatWaypointGuidance(guidance, isMph);
                if (self._pmtilesMap && self._pmtilesMap.setCheckpoint) {
                    if (guidance && guidance.hasTurn && guidance.maneuverLngLat) {
                        self._pmtilesMap.setCheckpoint(guidance.maneuverLngLat);
                    } else {
                        self._pmtilesMap.clearCheckpoint();
                    }
                }
            }
        } else {
            self._waypointText = '--';
            if (self._pmtilesMap && self._pmtilesMap.clearCheckpoint) {
                self._pmtilesMap.clearCheckpoint();
            }
        }
        $('.js-waypoint').text(self._waypointText || '--');
    }

    if (routeTarget && ll && window.TruckDeckRouter) {
        var needsRoute = routeKey !== self._routeKey;
        if (!needsRoute && routeTarget.kind === 'gps' && self._mapReady &&
            !(self._gpsRoute && self._gpsRoute.path) && !self._navMatchInFlight &&
            Date.now() >= self._routeRetryAt) {
            self._routeKey = null;
            needsRoute = true;
        }
        if (!needsRoute && routeTarget.kind === 'city' && self._mapReady &&
            !(self._activeRoutePath && self._activeRoutePath.length > 1) &&
            Date.now() >= self._routeRetryAt) {
            needsRoute = true;
        }
        if (needsRoute && Date.now() >= self._routeRetryAt) {
            if (self._mapRouteReady()) {
                self._scheduleRouteCompute(game, routeTarget, ll);
            } else {
                self._pendingRoute = { game: game, target: routeTarget, from: ll };
            }
        } else if (routeTarget.kind === 'gps' && self._mapRouteReady() && ll && !needsRoute) {
            var activePath = self._activeRoutePath ||
                (self._gpsRoute && self._gpsRoute.path) || null;
            if (self._gpsNeedsFullReroute(activePath, ll, routeTarget.navM, self._gpsRoute)) {
                if (self._invalidateGpsRoute) self._invalidateGpsRoute();
                if (Date.now() >= self._routeRetryAt) {
                    self._scheduleRouteCompute(game, routeTarget, ll);
                }
            } else {
                if (self._gpsRoute && self._gpsRoute.method === 'job-city' && self._gpsRoute.fullPath) {
                    if (self._refreshJobCityTrim) {
                        self._refreshJobCityTrim(ll, routeTarget.navM);
                    }
                } else {
                    self._refreshGameNavRoute(game, ll, routeTarget.navM, self._lastHeading, false);
                }
            }
        }
    }

    return data;
};

Funbit.Ets.Telemetry.Dashboard.prototype.render = function () {};
