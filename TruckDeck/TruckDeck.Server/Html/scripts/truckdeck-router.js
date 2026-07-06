/*
 * TruckDeck NAV client-side router.
 * Resolves a destination city name (from telemetry job.destinationCity) to a
 * routable point using {game}-cities.json, then asks a Web Worker to run A*
 * over {game}-graph.json (both produced by Html/maps/wsl/build-graph.js) and
 * returns a road-following [lon,lat] polyline.
 */
(function (global) {
    'use strict';

    function assetUrl(path) {
        if (!path) path = '/';
        if (path.charAt(0) !== '/') path = '/' + path;
        try {
            var C = global.Funbit && Funbit.Ets && Funbit.Ets.Telemetry && Funbit.Ets.Telemetry.Configuration;
            if (C && typeof C.getUrl === 'function') {
                return C.getUrl(path);
            }
        } catch (e) { /* ignore */ }
        var p = global.location.pathname || '/';
        var i = p.indexOf('/skins/');
        var root = (i >= 0) ? p.substring(0, i) : p.replace(/\/[^/]*$/, '');
        return (root || '') + path;
    }

    function normalizeName(s) {
        s = String(s || '').toLowerCase();
        try {
            s = s.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        } catch (e) { /* normalize not supported, best-effort match */ }
        return s.replace(/[^a-z0-9]/g, '');
    }

    function fetchJsonWithFallback(candidates) {
        var index = 0;
        var lastErr = null;
        var tried = [];

        function tryNext() {
            if (index >= candidates.length) {
                var msg = 'HTTP 404 loading routing data';
                if (tried.length) msg += ' (' + tried.join(', ') + ')';
                return Promise.reject(lastErr || new Error(msg));
            }
            var url = candidates[index++];
            tried.push(url);
            return fetch(url)
                .then(function (res) {
                    if (!res.ok) throw new Error('HTTP ' + res.status + ' for ' + url);
                    return res.json().then(function (data) { return { url: url, data: data }; });
                })
                .catch(function (err) {
                    lastErr = err;
                    console.warn('[TruckDeck NAV] Fetch failed:', url, err.message || err);
                    return tryNext();
                });
        }

        return tryNext();
    }

    TruckDeckRouterImpl.prototype.preflight = function (game) {
        game = (game === 'ats') ? 'ats' : 'ets2';
        var citiesName = game + '-cities.json';
        var graphName = game + '-graph.json';
        var cityUrls = [assetUrl('/' + citiesName), assetUrl('/maps/generated/' + citiesName)];
        var graphUrls = graphUrlCandidates(game);
        return Promise.all([
            fetch(cityUrls[0]).then(function (r) { return r.ok; })['catch'](function () { return false; }),
            fetch(cityUrls[1]).then(function (r) { return r.ok; })['catch'](function () { return false; }),
            fetch(graphUrls[0]).then(function (r) { return r.ok; })['catch'](function () { return false; }),
            fetch(graphUrls[1]).then(function (r) { return r.ok; })['catch'](function () { return false; })
        ]).then(function (results) {
            return {
                cities: results[0] || results[1],
                graph: results[2] || results[3],
                ready: (results[0] || results[1]) && (results[2] || results[3])
            };
        });
    };

    function normalizeNavMode(mode) {
        var m = String(mode || 'best').toLowerCase().replace(/[\s-]+/g, '_');
        if (m === 'shortest') return 'shortest';
        if (m === 'small_roads' || m === 'small' || m === 'scenic' || m === 'slow' || m === 'smallroads') {
            return 'small_roads';
        }
        return 'best';
    }

    function TruckDeckRouterImpl() {
        this._worker = null;
        this._workerFailed = false;
        this._nextRequestId = 1;
        this._pending = Object.create(null);
        this._citiesPromise = Object.create(null); // game -> Promise<{url, cities}>
        this._navMode = 'best';
    }

    TruckDeckRouterImpl.prototype.setNavMode = function (mode) {
        this._navMode = normalizeNavMode(mode);
    };

    TruckDeckRouterImpl.prototype.getNavMode = function () {
        return this._navMode || 'best';
    };

    TruckDeckRouterImpl.prototype.normalizeNavMode = normalizeNavMode;

    function haversineMeters(a, b) {
        var R = 6371000;
        var toRad = function (d) { return (d * Math.PI) / 180; };
        var dLat = toRad(b[1] - a[1]);
        var dLon = toRad(b[0] - a[0]);
        var lat1 = toRad(a[1]);
        var lat2 = toRad(b[1]);
        var h = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
        return 2 * R * Math.asin(Math.sqrt(h));
    }

    TruckDeckRouterImpl.prototype._postWorker = function (payload) {
        var self = this;
        var worker = self._ensureWorker();
        if (!worker) return Promise.resolve(null);

        var requestId = self._nextRequestId++;
        payload.requestId = requestId;

        return new Promise(function (resolve, reject) {
            var timer = setTimeout(function () {
                delete self._pending[requestId];
                reject(new Error('Routing timed out'));
            }, 45000);
            self._pending[requestId] = { resolve: resolve, reject: reject, timer: timer };
            worker.postMessage(payload);
        })['catch'](function (err) {
            var msg = (err && err.message) ? String(err.message) : '';
            if (msg.indexOf('No route matched') < 0 && msg.indexOf('No navigation') < 0) {
                console.warn('[TruckDeck NAV] Worker request failed:', err);
            }
            return null;
        });
    };

    TruckDeckRouterImpl.prototype._ensureWorker = function () {
        if (this._worker || this._workerFailed) return this._worker;
        var self = this;
        try {
            this._worker = new global.Worker(assetUrl('/scripts/truckdeck-router-worker.js'));
            this._worker.onmessage = function (evt) {
                var msg = evt.data || {};
                if (msg.type === 'route-result') {
                    var pending = self._pending[msg.requestId];
                    if (!pending) return;
                    delete self._pending[msg.requestId];
                    clearTimeout(pending.timer);
                    if (msg.error) pending.reject(new Error(msg.error));
                    else pending.resolve(msg.path || null);
                    return;
                }
                if (msg.type === 'match-nav-result') {
                    var pendingNav = self._pending[msg.requestId];
                    if (!pendingNav) return;
                    delete self._pending[msg.requestId];
                    clearTimeout(pendingNav.timer);
                    if (msg.error) pendingNav.reject(new Error(msg.error));
                    else pendingNav.resolve(msg);
                }
            };
            this._worker.onerror = function (err) {
                console.error('[TruckDeck NAV] Router worker error', err);
            };
        } catch (e) {
            console.error('[TruckDeck NAV] Could not create router worker', e);
            this._workerFailed = true;
            this._worker = null;
        }
        return this._worker;
    };

    TruckDeckRouterImpl.prototype.warmup = function () {
        this._ensureWorker();
    };

    TruckDeckRouterImpl.prototype._loadCities = function (game) {
        if (!this._citiesPromise[game]) {
            var name = game + '-cities.json';
            this._citiesPromise[game] = fetchJsonWithFallback([
                assetUrl('/' + name),
                assetUrl('/maps/generated/' + name)
            ]).then(function (resolved) { return resolved.data; });
        }
        return this._citiesPromise[game];
    };

    function graphUrlCandidates(game) {
        var name = game + '-graph.json';
        return [assetUrl('/' + name), assetUrl('/maps/generated/' + name)];
    }

    /** Finds a city entry by (accent/case-insensitive) name or token match. */
    TruckDeckRouterImpl.prototype._findCity = function (cities, cityName) {
        var target = normalizeName(cityName);
        if (!target) return null;
        for (var i = 0; i < cities.length; i++) {
            if (normalizeName(cities[i].name) === target) return cities[i];
        }
        for (var j = 0; j < cities.length; j++) {
            if (normalizeName(cities[j].token) === target) return cities[j];
        }
        for (var k = 0; k < cities.length; k++) {
            var n = normalizeName(cities[k].name);
            if (n.indexOf(target) >= 0 || target.indexOf(n) >= 0) return cities[k];
        }
        return null;
    };

    /**
     * Resolves a destination city name to routable [lon,lat] using the
     * projection already loaded on the main thread (TruckDeckProjection).
     */
    TruckDeckRouterImpl.prototype.resolveCityLngLat = function (game, cityName) {
        var self = this;
        return this._loadCities(game).then(function (cities) {
            var city = self._findCity(cities, cityName);
            if (!city) return null;
            if (!global.TruckDeckProjection) return null;
            var gameName = game === 'ats' ? 'ATS' : 'ETS2';
            return global.TruckDeckProjection.gameToLngLat(gameName, city.x, city.z);
        });
    };

    /**
     * Computes a road-following route between two projected lon/lat points.
     */
    TruckDeckRouterImpl.prototype.getRouteToLngLat = function (game, fromLngLat, toLngLat) {
        game = (game === 'ats') ? 'ats' : 'ets2';
        if (!fromLngLat || !toLngLat || !isFinite(fromLngLat[0]) || !isFinite(fromLngLat[1]) ||
            !isFinite(toLngLat[0]) || !isFinite(toLngLat[1])) {
            return Promise.resolve(null);
        }
        var aerial = haversineMeters(fromLngLat, toLngLat);
        var maxIter = Math.max(500000, Math.min(10000000, Math.round(aerial * 6)));
        return this._postWorker({
            type: 'route',
            graphUrls: graphUrlCandidates(game),
            from: fromLngLat,
            to: toLngLat,
            maxIterations: maxIter,
            navMode: this.getNavMode()
        });
    };

    /**
     * Computes a road-following route from fromLngLat to the given destination
     * city name. Resolves to an array of [lon,lat] pairs, or null if the
     * destination/graph could not be resolved.
     */
    TruckDeckRouterImpl.prototype.getRoute = function (game, fromLngLat, destinationCityName) {
        var self = this;
        game = (game === 'ats') ? 'ats' : 'ets2';

        if (!fromLngLat || !isFinite(fromLngLat[0]) || !isFinite(fromLngLat[1]) || !destinationCityName) {
            return Promise.resolve(null);
        }

        return this.resolveCityLngLat(game, destinationCityName).then(function (toLngLat) {
            if (!toLngLat) return null;
            return self.getRouteToLngLat(game, fromLngLat, toLngLat);
        });
    };

    /**
     * Match in-game GPS remaining distance by walking the road graph from the
     * truck position (same remaining-distance model as ETS2/ATS nav).
     */
    TruckDeckRouterImpl.prototype.matchNavRouteByDistance = function (game, fromLngLat, navDistM, headingDeg) {
        game = (game === 'ats') ? 'ats' : 'ets2';
        if (!fromLngLat || !isFinite(navDistM) || navDistM < 50) {
            return Promise.resolve(null);
        }
        return this._postWorker({
            type: 'match-nav-graph',
            graphUrls: graphUrlCandidates(game),
            from: fromLngLat,
            navDistM: navDistM,
            headingDeg: headingDeg,
            maxIterations: navDistM > 1200000
                ? 15000000
                : Math.max(900000, Math.min(10000000, Math.round(navDistM * 4))),
            navMode: this.getNavMode()
        }).then(function (msg) {
            if (!msg || msg.error || !msg.path || msg.path.length < 2) return null;
            return {
                path: msg.path,
                dest: msg.dest,
                lengthM: msg.lengthM,
                matchErrorM: msg.matchErrorM,
                method: msg.method || 'graph'
            };
        });
    };

    /**
     * When the game has active GPS navigation (navigation.estimatedDistance) but
     * no freight-job destination city, guess the destination city by finding which
     * city’s road distance best matches the in-game navigation distance.
     */
    TruckDeckRouterImpl.prototype.matchNavDestination = function (game, fromLngLat, navDistM, headingDeg) {
        var self = this;
        game = (game === 'ats') ? 'ats' : 'ets2';
        if (!fromLngLat || !isFinite(navDistM) || navDistM < 50) {
            return Promise.resolve(null);
        }

        function normalizeNavMsg(msg) {
            if (!msg || msg.error || !msg.path || msg.path.length < 2) return null;
            return {
                path: msg.path,
                dest: msg.dest,
                lengthM: msg.lengthM,
                matchErrorM: msg.matchErrorM,
                method: msg.method || 'city'
            };
        }

        function bearingDeg(from, to) {
            var dLon = (to[0] - from[0]) * Math.PI / 180;
            var lat1 = from[1] * Math.PI / 180;
            var lat2 = to[1] * Math.PI / 180;
            var y = Math.sin(dLon) * Math.cos(lat2);
            var x = Math.cos(lat1) * Math.sin(lat2) - Math.sin(lat1) * Math.cos(lat2) * Math.cos(dLon);
            return (Math.atan2(y, x) * 180 / Math.PI + 360) % 360;
        }

        function headingDelta(a, b) {
            var d = Math.abs(a - b) % 360;
            return d > 180 ? 360 - d : d;
        }

        return this._loadCities(game).then(function (cities) {
            if (!global.TruckDeckProjection) return null;
            var gameName = game === 'ats' ? 'ATS' : 'ETS2';
            var candidates = [];
            var longNav = navDistM > 500000;
            var minAerial = navDistM * (longNav ? 0.08 : 0.15);
            var maxAerial = navDistM * (longNav ? 1.2 : 1.05);
            var aerialTarget = navDistM * (longNav ? 0.72 : 0.85);
            var hasHeading = isFinite(headingDeg);

            for (var i = 0; i < cities.length; i++) {
                var city = cities[i];
                var ll = global.TruckDeckProjection.gameToLngLat(gameName, city.x, city.z);
                if (!ll) continue;
                var aerial = haversineMeters(fromLngLat, ll);
                if (aerial < minAerial || aerial > maxAerial) continue;
                if (hasHeading && headingDelta(headingDeg, bearingDeg(fromLngLat, ll)) > 95) continue;
                candidates.push({
                    name: city.name,
                    lng: ll[0],
                    lat: ll[1],
                    aerial: aerial
                });
            }

            candidates.sort(function (a, b) {
                return Math.abs(a.aerial - aerialTarget) - Math.abs(b.aerial - aerialTarget);
            });

            if (hasHeading && candidates.length === 0) {
                for (var k = 0; k < cities.length; k++) {
                    var city2 = cities[k];
                    var ll2 = global.TruckDeckProjection.gameToLngLat(gameName, city2.x, city2.z);
                    if (!ll2) continue;
                    var aerial2 = haversineMeters(fromLngLat, ll2);
                    if (aerial2 < minAerial || aerial2 > maxAerial) continue;
                    candidates.push({
                        name: city2.name,
                        lng: ll2[0],
                        lat: ll2[1],
                        aerial: aerial2
                    });
                }
                candidates.sort(function (a, b) {
                    return Math.abs(a.aerial - aerialTarget) - Math.abs(b.aerial - aerialTarget);
                });
            }

            candidates = candidates.slice(0, longNav ? 60 : 40);
            if (candidates.length === 0) return null;

            var maxIter = Math.max(900000, Math.min(10000000, Math.round(navDistM * 4)));
            var requestId = self._nextRequestId++;
            return new Promise(function (resolve, reject) {
                var worker = self._ensureWorker();
                if (!worker) {
                    resolve(null);
                    return;
                }
                var timer = setTimeout(function () {
                    delete self._pending[requestId];
                    reject(new Error('Navigation match timed out'));
                }, 45000);
                self._pending[requestId] = {
                    resolve: function (msg) { resolve(normalizeNavMsg(msg)); },
                    reject: reject,
                    timer: timer
                };
                worker.postMessage({
                    type: 'match-nav',
                    requestId: requestId,
                    graphUrls: graphUrlCandidates(game),
                    from: fromLngLat,
                    navDistM: navDistM,
                    headingDeg: headingDeg,
                    maxIterations: maxIter,
                    candidates: candidates,
                    navMode: self.getNavMode()
                });
            })['catch'](function (err) {
                var msg = (err && err.message) ? String(err.message) : '';
                if (msg.indexOf('No route matched') < 0 && msg.indexOf('No navigation') < 0) {
                    console.warn('[TruckDeck NAV] matchNavDestination failed:', err);
                }
                return null;
            });
        });
    };

    /**
     * Route to a known destination city and pick the path whose length best
     * matches in-game navigation remaining distance (job GPS to delivery city).
     */
    TruckDeckRouterImpl.prototype.matchNavToCity = function (game, fromLngLat, cityName, navDistM, headingDeg) {
        var self = this;
        game = (game === 'ats') ? 'ats' : 'ets2';
        if (!fromLngLat || !cityName || !isFinite(navDistM) || navDistM < 50) {
            return Promise.resolve(null);
        }
        return this.resolveCityLngLat(game, cityName).then(function (ll) {
            if (!ll) return null;
            var maxIter = navDistM > 1200000
                ? 15000000
                : Math.max(900000, Math.min(10000000, Math.round(navDistM * 4)));
            var requestId = self._nextRequestId++;
            return new Promise(function (resolve, reject) {
                var worker = self._ensureWorker();
                if (!worker) {
                    resolve(null);
                    return;
                }
                var timer = setTimeout(function () {
                    delete self._pending[requestId];
                    reject(new Error('Navigation match timed out'));
                }, navDistM > 1000000 ? 90000 : 45000);
                self._pending[requestId] = {
                    resolve: function (msg) {
                        if (!msg || msg.error || !msg.path || msg.path.length < 2) {
                            resolve(null);
                            return;
                        }
                        resolve({
                            path: msg.path,
                            dest: msg.dest || { name: cityName },
                            lengthM: msg.lengthM,
                            matchErrorM: msg.matchErrorM,
                            method: msg.method || 'city'
                        });
                    },
                    reject: reject,
                    timer: timer
                };
                worker.postMessage({
                    type: 'match-nav',
                    requestId: requestId,
                    graphUrls: graphUrlCandidates(game),
                    from: fromLngLat,
                    navDistM: navDistM,
                    headingDeg: headingDeg,
                    maxIterations: maxIter,
                    candidates: [{ name: cityName, lng: ll[0], lat: ll[1], aerial: 0 }],
                    navMode: self.getNavMode()
                });
            })['catch'](function () { return null; });
        });
    };

    global.TruckDeckRouter = new TruckDeckRouterImpl();
})(typeof window !== 'undefined' ? window : this);
