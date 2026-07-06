/* MapLibre + PMTiles map helper for TruckDeck NAV skin */
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

    function loadScript(src) {
        return new Promise(function (resolve, reject) {
            var base = src.split('?')[0];
            var nodes = document.querySelectorAll('script[src]');
            for (var i = 0; i < nodes.length; i++) {
                var existing = nodes[i].getAttribute('src') || '';
                if (existing.split('?')[0] === base) {
                    resolve();
                    return;
                }
            }
            var s = document.createElement('script');
            s.src = src;
            s.onload = function () { resolve(); };
            s.onerror = function () { reject(new Error('Failed to load ' + src)); };
            document.head.appendChild(s);
        });
    }

    function loadCss(href) {
        if (document.querySelector('link[href="' + href + '"]')) return;
        var l = document.createElement('link');
        l.rel = 'stylesheet';
        l.href = href;
        document.head.appendChild(l);
    }

    function ensureLibs() {
        if (global.proj4 && global.pmtiles && global.maplibregl) {
            return Promise.resolve();
        }
        loadCss(assetUrl('/scripts/vendor/maplibre-gl.css'));
        function loadVendor(localPath, cdnUrl) {
            return loadScript(assetUrl(localPath))['catch'](function () {
                return loadScript(cdnUrl);
            });
        }
        return Promise.all([
            loadVendor('/scripts/vendor/proj4.js', 'https://unpkg.com/proj4@2.11.0/dist/proj4.js'),
            loadVendor('/scripts/vendor/pmtiles.js', 'https://unpkg.com/pmtiles@4.0.1/dist/pmtiles.js'),
            loadVendor('/scripts/vendor/maplibre-gl.js', 'https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js')
        ]);
    }

    var EMPTY_FEATURE_COLLECTION = { type: 'FeatureCollection', features: [] };

    /* Day/night colour palettes. Route colours stay constant (Google-blue reads fine on both). */
    var PALETTES = {
        night: {
            bg: '#242f3e',
            mapArea: ['#17263c', '#263c3f', '#38414e', '#1a1f24'], // water, park, urban, misc
            roadCasing: { freeway: '#3d3428', divided: '#3d4a5c', local: '#4a5668' },
            road: { freeway: '#9a8468', divided: '#6d7d92', local: '#5c6b7f', dflt: '#667688' },
            labelText: '#d5d2d2',
            labelHalo: '#242f3e'
        },
        day: {
            bg: '#f2efe8',
            mapArea: ['hsl(206, 40%, 87%)', 'hsl(40, 48%, 86%)', 'hsl(40, 52%, 80%)', 'hsl(95, 33%, 81%)'],
            roadCasing: { freeway: '#d99a2b', divided: '#c7ccd1', local: '#d7dadd' },
            road: { freeway: '#f8b84e', divided: '#ffffff', local: '#fdfdfd', dflt: '#e7e9eb' },
            labelText: '#3a3f33',
            labelHalo: '#f2efe8'
        }
    };

    function paletteFor(mode) {
        return PALETTES[mode] || PALETTES.night;
    }

    function mapAreaFillExpr(pal) {
        // color 0=water, 1=park, 2=urban, 3=misc
        return ['match', ['get', 'color'], 0, pal.mapArea[0], 1, pal.mapArea[1], 2, pal.mapArea[2], 3, pal.mapArea[3], pal.mapArea[0]];
    }

    function roadCasingColorExpr(pal) {
        return ['match', ['get', 'roadType'], 'freeway', pal.roadCasing.freeway, 'divided', pal.roadCasing.divided, pal.roadCasing.local];
    }

    function roadColorExpr(pal) {
        return ['match', ['get', 'roadType'], 'freeway', pal.road.freeway, 'divided', pal.road.divided, 'local', pal.road.local, pal.road.dflt];
    }

    function spriteUrl() {
        return assetUrl('/maps/sprites/sprites');
    }

    /** Map tile sprite/icon keys to spritesheet ids (fuel stations use gas_ico in the sheet). */
    function poiSpriteImageExpr() {
        return ['match', ['coalesce', ['get', 'sprite'], ['get', 'icon']],
            'fuel_ico', 'gas_ico',
            'fuel_station_ico', 'gas_ico',
            'petrol_ico', 'gas_ico',
            'gas_station_ico', 'gas_ico',
            'low_fuel_ico', 'gas_ico',
            ['coalesce', ['get', 'sprite'], ['get', 'icon']]
        ];
    }

    function poiSortKeyExpr() {
        return ['match', poiSpriteImageExpr(),
            'garage_large_ico', 900,
            'gas_ico', 850,
            'service_ico', 820,
            'dealer_ico', 800,
            'parking_ico', 780,
            'recruitment_ico', 760,
            'weigh_station_ico', 740,
            'toll_ico', 720,
            'weigh_ico', 700,
            'port_overlay', 680,
            'train_ico', 660,
            'viewpoint', 640,
            'photo_sight_captured', 620,
            'agri_check', 600,
            'border_ico', 580,
            500
        ];
    }

    function trafficSpriteImageExpr() {
        return ['match', ['coalesce', ['get', 'sprite'], ['get', 'icon'], ''],
            'trafficlight', 'trafficlight',
            'traffic_light', 'trafficlight',
            'traffic_light_ico', 'trafficlight',
            'trafficlight'
        ];
    }

    function iconLayoutTraffic() {
        return {
            'icon-image': trafficSpriteImageExpr(),
            'icon-allow-overlap': true,
            'icon-ignore-placement': true,
            'icon-padding': 2,
            'symbol-sort-key': 950,
            'icon-size': [
                'interpolate', ['exponential', 1.5], ['zoom'],
                10, 0.55, 13, 0.95, 16, 1.35
            ]
        };
    }

    function iconLayout(scale4, scale9, scale13, opts) {
        opts = opts || {};
        var layout = {
            'icon-image': poiSpriteImageExpr(),
            'icon-allow-overlap': opts.allowOverlap !== false,
            'icon-ignore-placement': opts.ignorePlacement !== false,
            'icon-padding': opts.padding || [4, 10],
            'symbol-sort-key': opts.sortKey !== false ? poiSortKeyExpr() : undefined,
            'icon-size': [
                'interpolate', ['exponential', 1.5], ['zoom'],
                4, scale4, 9, scale9, 13, scale13
            ]
        };
        if (layout['symbol-sort-key'] === undefined) delete layout['symbol-sort-key'];
        return layout;
    }

    function poiBaseFilter() {
        return ['all',
            ['==', ['geometry-type'], 'Point'],
            ['==', ['get', 'type'], 'poi'],
            ['!=', ['get', 'hidden'], true]
        ];
    }

    /** Prefer maps/sprites, fall back to maps/generated after map build. */
    function resolveSpriteUrl() {
        var candidates = [
            assetUrl('/maps/sprites/sprites'),
            assetUrl('/maps/generated/sprites')
        ];
        function probePng(base, timeoutMs) {
            return new Promise(function (resolve) {
                var done = false;
                var timer = setTimeout(function () {
                    if (!done) { done = true; resolve(false); }
                }, timeoutMs || 2500);
                fetch(base + '.png', { method: 'GET', cache: 'no-store' }).then(function (r) {
                    if (done) return;
                    done = true;
                    clearTimeout(timer);
                    resolve(!!(r && r.ok));
                })['catch'](function () {
                    if (done) return;
                    fetch(base + '@2x.png', { method: 'GET', cache: 'no-store' }).then(function (r2) {
                        if (done) return;
                        done = true;
                        clearTimeout(timer);
                        resolve(!!(r2 && r2.ok));
                    })['catch'](function () {
                        if (!done) { done = true; clearTimeout(timer); resolve(false); }
                    });
                });
            });
        }
        function tryCandidate(idx) {
            if (idx >= candidates.length) {
                console.warn('[TruckDeck NAV] Sprite PNG not found — run Map Generator to build sprites.png');
                return Promise.resolve(candidates[0]);
            }
            return probePng(candidates[idx], 2500).then(function (ok) {
                if (ok) {
                    if (idx > 0) console.log('[TruckDeck NAV] Using generated sprite sheet');
                    return candidates[idx];
                }
                return tryCandidate(idx + 1);
            });
        }
        return tryCandidate(0);
    }

    function visibleRoadFilter() {
        return ['all',
            ['==', ['geometry-type'], 'LineString'],
            ['==', ['get', 'type'], 'road'],
            ['!=', ['get', 'roadType'], 'train'],
            ['==', ['get', 'hidden'], false]
        ];
    }

    var GLYPHS_CDN = 'https://tiles.openfreemap.org/fonts/{fontstack}/{range}.pbf';

    function localGlyphsUrl() {
        return assetUrl('/maps/fonts/{fontstack}/{range}.pbf');
    }

    /** Probes font URL with GET (HEAD often fails on embedded static servers). */
    function resolveGlyphsUrl(opts) {
        opts = opts || {};
        var mobile = isLowMemoryDevice() && !opts.fullLabels;
        function probe(url, timeoutMs) {
            return new Promise(function (resolve) {
                var done = false;
                var timer = setTimeout(function () {
                    if (done) return;
                    done = true;
                    resolve(false);
                }, timeoutMs || 3000);
                var ctrl = typeof AbortController !== 'undefined' ? new AbortController() : null;
                var abortTimer = ctrl ? setTimeout(function () {
                    try { ctrl.abort(); } catch (e) { /* ignore */ }
                }, timeoutMs || 3000) : null;
                fetch(url, {
                    method: 'GET',
                    mode: 'cors',
                    cache: 'no-store',
                    signal: ctrl ? ctrl.signal : undefined
                }).then(function (r) {
                    if (done) return;
                    done = true;
                    clearTimeout(timer);
                    if (abortTimer) clearTimeout(abortTimer);
                    resolve(!!(r && r.ok));
                })['catch'](function () {
                    if (done) return;
                    done = true;
                    clearTimeout(timer);
                    if (abortTimer) clearTimeout(abortTimer);
                    resolve(false);
                });
            });
        }
        var localTest = assetUrl('/maps/fonts/Noto%20Sans%20Regular/0-255.pbf');
        var cdnTest = 'https://tiles.openfreemap.org/fonts/Noto%20Sans%20Regular/0-255.pbf';
        return probe(localTest, mobile ? 900 : 2000).then(function (localOk) {
            if (localOk) return { url: localGlyphsUrl(), includeLabels: true };
            if (mobile) {
                return { url: null, includeLabels: false };
            }
            return probe(cdnTest, opts.fullLabels ? 5000 : 4000).then(function (cdnOk) {
                if (cdnOk) return { url: GLYPHS_CDN, includeLabels: true };
                console.warn('[TruckDeck NAV] No label fonts available — city names disabled');
                return { url: null, includeLabels: false };
            });
        });
    }

    function buildStyle(tileUrl, gameKey, mode, styleOpts) {
        styleOpts = styleOpts || {};
        var minimal = !!styleOpts.minimal;
        var navRotate = styleOpts.rotate !== false;
        var includeLabels = styleOpts.includeLabels !== false && !!styleOpts.glyphsUrl;
        var includeSymbols = styleOpts.includeSymbols !== false && !minimal;
        var sl = gameKey === 'ats' ? 'ats' : 'ets2';
        var pal = paletteFor(mode);
        var layers = [
                { id: 'bg', type: 'background', paint: { 'background-color': pal.bg } },
                {
                    id: 'mapAreas',
                    type: 'fill',
                    source: 'map',
                    'source-layer': sl,
                    filter: ['all', ['==', ['geometry-type'], 'Polygon'], ['==', ['get', 'type'], 'mapArea']],
                    paint: { 'fill-color': mapAreaFillExpr(pal) }
                },
                {
                    id: 'roads-casing',
                    type: 'line',
                    source: 'map',
                    'source-layer': sl,
                    filter: visibleRoadFilter(),
                    layout: { 'line-cap': 'round', 'line-join': 'round' },
                    paint: {
                        'line-color': roadCasingColorExpr(pal),
                        'line-width': [
                            'interpolate', ['exponential', 1.5], ['zoom'],
                            3, ['match', ['get', 'roadType'], 'freeway', 2.2, 'divided', 1.6, 0.9],
                            10, ['match', ['get', 'roadType'], 'freeway', 14, 'divided', 10, 4.5],
                            14, ['match', ['get', 'roadType'], 'freeway', 34, 'divided', 22, 9],
                            16, ['match', ['get', 'roadType'], 'freeway', 52, 'divided', 34, 14]
                        ]
                    }
                },
                {
                    id: 'roads',
                    type: 'line',
                    source: 'map',
                    'source-layer': sl,
                    filter: visibleRoadFilter(),
                    layout: { 'line-cap': 'round', 'line-join': 'round' },
                    paint: {
                        'line-color': roadColorExpr(pal),
                        'line-width': [
                            'interpolate', ['exponential', 1.5], ['zoom'],
                            3, ['match', ['get', 'roadType'], 'freeway', 1.6, 'divided', 1.2, 0.85],
                            10, ['match', ['get', 'roadType'], 'freeway', 10, 'divided', 7, 4],
                            14, ['match', ['get', 'roadType'], 'freeway', 28, 'divided', 18, 8.5],
                            16, ['match', ['get', 'roadType'], 'freeway', 42, 'divided', 28, 12]
                        ]
                    }
                },
        ];
        if (!minimal) {
            layers.push({
                    id: 'roads-lanes',
                    type: 'line',
                    source: 'map',
                    'source-layer': sl,
                    minzoom: 10,
                    filter: ['all',
                        ['==', ['geometry-type'], 'LineString'],
                        ['==', ['get', 'type'], 'road'],
                        ['!=', ['get', 'roadType'], 'train'],
                        ['==', ['get', 'hidden'], false],
                        ['any',
                            ['==', ['get', 'roadType'], 'freeway'],
                            ['==', ['get', 'roadType'], 'divided']
                        ]
                    ],
                    layout: { 'line-cap': 'butt', 'line-join': 'round' },
                    paint: {
                        'line-color': mode === 'day' ? '#ffffff' : '#e8eef5',
                        'line-width': [
                            'interpolate', ['exponential', 1.5], ['zoom'],
                            10, ['match', ['get', 'roadType'], 'freeway', 0.35, 'divided', 0.25, 0],
                            14, ['match', ['get', 'roadType'], 'freeway', 1.4, 'divided', 1.0, 0],
                            16, ['match', ['get', 'roadType'], 'freeway', 2.2, 'divided', 1.6, 0]
                        ],
                        'line-opacity': 0.65,
                        'line-dasharray': [2, 3]
                    }
                });
        }
        layers.push(
                {
                    id: 'route-dots',
                    type: 'circle',
                    source: 'route-dots',
                    paint: {
                        'circle-radius': [
                            'interpolate', ['linear'], ['zoom'],
                            8, 5, 11, 7, 14, 9, 17, 11
                        ],
                        'circle-color': '#9BE7A8',
                        'circle-stroke-width': 1.5,
                        'circle-stroke-color': 'rgba(255, 255, 255, 0.55)',
                        'circle-opacity': 0.95,
                        'circle-blur': 0,
                        'circle-pitch-alignment': 'viewport'
                    }
                },
                {
                    id: 'route-checkpoint',
                    type: 'circle',
                    source: 'route-checkpoint',
                    paint: {
                        'circle-radius': [
                            'interpolate', ['linear'], ['zoom'],
                            10, 8, 13, 11, 16, 14
                        ],
                        'circle-color': '#FFB020',
                        'circle-stroke-width': 2.5,
                        'circle-stroke-color': '#ffffff',
                        'circle-opacity': 0.95,
                        'circle-pitch-alignment': 'map'
                    }
                }
        );
        if (includeSymbols) {
            layers.push(
                {
                    id: 'poi-icons',
                    type: 'symbol',
                    source: 'map',
                    'source-layer': sl,
                    minzoom: 5,
                    filter: ['all',
                        poiBaseFilter(),
                        ['!=', ['get', 'poiType'], 'company'],
                        ['!=', ['coalesce', ['get', 'sprite'], ['get', 'icon'], ''], 'garage_large_ico']
                    ],
                    layout: iconLayout(0.55, 1.1, 2.2, { allowOverlap: true, ignorePlacement: true })
                },
                {
                    id: 'poi-icons-garage',
                    type: 'symbol',
                    source: 'map',
                    'source-layer': sl,
                    minzoom: 5,
                    filter: ['all',
                        poiBaseFilter(),
                        ['any',
                            ['==', ['get', 'sprite'], 'garage_large_ico'],
                            ['==', ['get', 'icon'], 'garage_large_ico']
                        ]
                    ],
                    layout: iconLayout(0.6, 1.15, 2.3, { allowOverlap: true, ignorePlacement: true })
                },
                {
                    id: 'company-icons',
                    type: 'symbol',
                    source: 'map',
                    'source-layer': sl,
                    minzoom: 6,
                    filter: ['all',
                        poiBaseFilter(),
                        ['==', ['get', 'poiType'], 'company']
                    ],
                    layout: iconLayout(0.85, 1.1, 3.0, { allowOverlap: true, ignorePlacement: true })
                },
                {
                    id: 'traffic-icons',
                    type: 'symbol',
                    source: 'map',
                    'source-layer': sl,
                    minzoom: 10,
                    filter: ['all',
                        ['==', ['geometry-type'], 'Point'],
                        ['==', ['get', 'type'], 'traffic'],
                        ['!=', ['coalesce', ['get', 'hidden'], false], true]
                    ],
                    layout: iconLayoutTraffic()
                }
            );
        }
        if (includeLabels) {
            layers.push({
                    id: 'city-labels',
                    type: 'symbol',
                    source: 'map',
                    'source-layer': sl,
                    minzoom: 5,
                    filter: ['all', ['==', ['geometry-type'], 'Point'], ['==', ['get', 'type'], 'city'], ['has', 'name']],
                    layout: {
                        'text-field': ['get', 'name'],
                        'text-font': ['Noto Sans Regular'],
                        'text-size': ['interpolate', ['linear'], ['zoom'], 5, 9, 8, 11, 12, 14],
                        'text-variable-anchor': ['top', 'bottom', 'left', 'right'],
                        'text-radial-offset': 0.6,
                        'text-justify': 'auto'
                    },
                    paint: { 'text-color': pal.labelText, 'text-halo-color': pal.labelHalo, 'text-halo-width': 1.4 }
                });
        }
        var style = {
            version: 8,
            sources: {
                map: { type: 'vector', url: 'pmtiles://' + tileUrl },
                'route-dots': { type: 'geojson', data: EMPTY_FEATURE_COLLECTION },
                'route-checkpoint': { type: 'geojson', data: EMPTY_FEATURE_COLLECTION }
            },
            layers: layers
        };
        if (includeSymbols) style.sprite = styleOpts.spriteBase || spriteUrl();
        if (styleOpts.glyphsUrl) style.glyphs = styleOpts.glyphsUrl;
        return style;
    }

    /** Evenly spaced points along a road polyline (metres). */
    function polylineLengthM(coords) {
        if (!coords || coords.length < 2) return 0;
        var total = 0;
        for (var i = 1; i < coords.length; i++) {
            total += haversineMeters(coords[i - 1], coords[i]);
        }
        return total;
    }

    /** Minimum gap between route dots so they read as separate markers, not a solid line. */
    function routeDotSpacingM(coords, zoom, lowMemory, maxDots) {
        maxDots = maxDots || 3000;
        var minGap = 55;
        if (lowMemory) minGap = 65;
        if (zoom < 9) minGap = 200;
        else if (zoom < 11) minGap = 140;
        else if (zoom < 13) minGap = 90;
        var len = polylineLengthM(coords);
        if (len > 0 && maxDots > 0) {
            return Math.max(minGap, len / maxDots);
        }
        return minGap;
    }

    /** Evenly spaced points along a road polyline (metres). */
    function routeCoordsToDotPoints(coords, spacingM, maxDots) {
        if (!coords || coords.length < 2) return [];
        spacingM = Math.max(40, spacingM || 55);
        maxDots = maxDots || 3000;
        var pts = [];
        var carry = 0;
        for (var i = 0; i < coords.length - 1; i++) {
            var a = coords[i];
            var b = coords[i + 1];
            var len = haversineMeters(a, b);
            if (len < 0.01) continue;
            var d = carry;
            while (d <= len) {
                var t = len > 0 ? d / len : 0;
                pts.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]);
                if (pts.length >= maxDots) return pts;
                d += spacingM;
            }
            carry = d - len;
        }
        return pts;
    }

    /** Place route dots along PMTiles road geometry (not graph chords). */
    function routeCoordsToRoadDotPoints(coords, roadSegments, spacingM, maxDots, lowMemory) {
        if (!coords || coords.length < 2) return [];
        if (!roadSegments || !roadSegments.length) {
            return routeCoordsToDotPoints(coords, spacingM, maxDots);
        }
        spacingM = Math.max(40, spacingM || 55);
        maxDots = maxDots || 6000;
        var pts = [];
        var carry = 0;

        var aligned = snapRouteToRoads(coords, roadSegments, lowMemory);
        aligned = snapVerticesToRoads(aligned, roadSegments, 98);
        aligned = enrichPathAtTurns(aligned, roadSegments);

        function edgePointsOnRoad(a, b, bendHint) {
            var len = haversineMeters(a, b);
            if (len < 10) return [a, b];
            var tight = bendHint > 1 || len < 900;
            var traced = traceChordOnRoads(a, b, roadSegments, tight);
            var pathLen = polylineLengthMeters(traced);
            if (traced.length >= 3 && (pathLen > len * 1.004 || bendHint > 1)) {
                return traced;
            }
            var step = bendHint > 8 ? 0.5 : (bendHint > 18 ? 3 : (bendHint > 3 ? 4 : 9));
            var subSteps = len > 4 ? Math.min(120, Math.ceil(len / step) - 1) : 0;
            var edgePts = [a];
            for (var si = 1; si <= subSteps; si++) {
                var tf = si / (subSteps + 1);
                var p = [a[0] + (b[0] - a[0]) * tf, a[1] + (b[1] - a[1]) * tf];
                var brg = bearingDeg(a, b);
                edgePts.push(nearestRoadPointDirected(p, roadSegments, 85, brg) || p);
            }
            edgePts.push(b);
            return edgePts;
        }

        function emitAlongPath(poly) {
            for (var i = 0; i < poly.length - 1 && pts.length < maxDots; i++) {
                var a = poly[i];
                var b = poly[i + 1];
                var len = haversineMeters(a, b);
                if (len < 0.01) continue;
                var segBrg = bearingDeg(a, b);
                var bendAhead = i + 2 < poly.length ? turnAngleDeg(poly[i], poly[i + 1], poly[i + 2]) : 0;
                var bendBehind = i > 0 ? turnAngleDeg(poly[i - 1], poly[i], poly[i + 1]) : 0;
                var maxBend = Math.max(bendAhead, bendBehind);
                var localSpacing = spacingM;
                if (maxBend > 3) localSpacing = 0.5;
                else if (maxBend > 1) localSpacing = Math.max(8, spacingM * 0.35);
                var edgePts = edgePointsOnRoad(a, b, maxBend);
                if (maxBend > 3 && edgePts.length > 2 && pts.length < maxDots) {
                    for (var vi = 1; vi < edgePts.length - 1 && pts.length < maxDots; vi++) {
                        var vpt = nearestRoadPointDirected(edgePts[vi], roadSegments, 70, segBrg) || edgePts[vi];
                        var vlast = pts[pts.length - 1];
                        if (!vlast || haversineMeters(vlast, vpt) > 0.5) {
                            pts.push(vpt);
                            carry = 0;
                        }
                    }
                }
                for (var ei = 0; ei < edgePts.length - 1 && pts.length < maxDots; ei++) {
                    var ea = edgePts[ei];
                    var eb = edgePts[ei + 1];
                    var elen = haversineMeters(ea, eb);
                    if (elen < 0.01) continue;
                    var ebrg = bearingDeg(ea, eb);
                    var d = carry;
                    while (d <= elen && pts.length < maxDots) {
                        var t = elen > 0 ? d / elen : 0;
                        var pt = [ea[0] + (eb[0] - ea[0]) * t, ea[1] + (eb[1] - ea[1]) * t];
                        pt = nearestRoadPointDirected(pt, roadSegments, 62, ebrg) || pt;
                        pts.push(pt);
                        d += localSpacing;
                    }
                    carry = d - elen;
                }
                if (i + 1 < poly.length - 1 && pts.length < maxDots) {
                    var bend = turnAngleDeg(poly[i], poly[i + 1], poly[i + 2]);
                    if (bend > 1) {
                        var jvtx = nearestRoadPointDirected(poly[i + 1], roadSegments, 82, segBrg) || poly[i + 1];
                        var last = pts[pts.length - 1];
                        if (!last || haversineMeters(last, jvtx) > 0.5) {
                            pts.push(jvtx);
                            carry = 0;
                        }
                    }
                }
            }
        }

        emitAlongPath(aligned);
        return pts;
    }

    /** Chevron icon; tip points to top of canvas (straight ahead in heading-up mode). */
    function buildArrowImageData(size) {
        var canvas = document.createElement('canvas');
        canvas.width = size;
        canvas.height = size;
        var ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, size, size);
        ctx.fillStyle = '#ffffff';
        ctx.strokeStyle = '#004466';
        ctx.lineWidth = Math.max(1, size * 0.07);
        ctx.lineJoin = 'round';
        var cx = size / 2;
        ctx.beginPath();
        ctx.moveTo(cx, size * 0.06);
        ctx.lineTo(size * 0.8, size * 0.88);
        ctx.lineTo(cx, size * 0.62);
        ctx.lineTo(size * 0.2, size * 0.88);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
        return ctx.getImageData(0, 0, size, size);
    }

    /** Lane-change chevron rotated on canvas (left/right). */
    function buildSideArrowImageData(size, side) {
        var canvas = document.createElement('canvas');
        canvas.width = size;
        canvas.height = size;
        var ctx = canvas.getContext('2d');
        ctx.translate(size / 2, size / 2);
        ctx.rotate((side === 'left' ? -90 : 90) * Math.PI / 180);
        ctx.translate(-size / 2, -size / 2);
        ctx.fillStyle = '#39ff14';
        ctx.strokeStyle = '#145214';
        ctx.lineWidth = Math.max(1, size * 0.07);
        ctx.lineJoin = 'round';
        var cx = size / 2;
        ctx.beginPath();
        ctx.moveTo(cx, size * 0.06);
        ctx.lineTo(size * 0.8, size * 0.88);
        ctx.lineTo(cx, size * 0.62);
        ctx.lineTo(size * 0.2, size * 0.88);
        ctx.closePath();
        ctx.fill();
        ctx.stroke();
        return ctx.getImageData(0, 0, size, size);
    }

    /** Side-view truck marker (cab at top = forward); used with viewport alignment in heading-up mode. */
    function createTruckMarkerElement() {
        var el = document.createElement('div');
        el.className = 'tdn-truck-marker';
        el.style.cssText = 'width:30px;height:36px;pointer-events:none;filter:drop-shadow(0 1px 2px rgba(0,0,0,0.85));';
        el.innerHTML = '<svg viewBox="0 0 30 36" width="30" height="36" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">' +
            '<rect x="8" y="15" width="14" height="17" rx="2" fill="#39ff14" stroke="#145214" stroke-width="1.2"/>' +
            '<path d="M7 15 L7 9 Q7 4 15 3 Q23 4 23 9 L23 15 Z" fill="#4dff26" stroke="#145214" stroke-width="1.2"/>' +
            '<path d="M10 9 L15 6 L20 9 L20 12 L10 12 Z" fill="#0f2a14" opacity="0.55"/>' +
            '<rect x="5" y="11" width="4" height="6" rx="1.2" fill="#1a1a1a"/>' +
            '<rect x="21" y="11" width="4" height="6" rx="1.2" fill="#1a1a1a"/>' +
            '<rect x="5" y="24" width="4" height="6" rx="1.2" fill="#1a1a1a"/>' +
            '<rect x="21" y="24" width="4" height="6" rx="1.2" fill="#1a1a1a"/>' +
            '<rect x="11" y="17" width="8" height="2" rx="0.5" fill="#2dcc10" opacity="0.7"/>' +
            '</svg>';
        return el;
    }

    /** Haversine distance in metres between two [lon,lat] points. */
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

    /** Nearest point on a route polyline to lngLat. */
    function nearestPointOnRoute(route, lngLat) {
        if (!route || route.length < 2 || !lngLat) return null;
        var best = null;
        for (var i = 0; i < route.length - 1; i++) {
            var proj = projectPointOnSegment(lngLat, route[i], route[i + 1]);
            if (!best || proj.distSq < best.distSq) {
                best = { point: proj.point, distSq: proj.distSq, index: i, segT: proj.segT };
            }
        }
        if (!best) return null;
        best.distM = haversineMeters(lngLat, best.point);
        return best;
    }

    /** Snap truck position onto the route when close enough to sit on the line. */
    function snapLngLatToRoute(lng, lat, route, maxSnapMeters) {
        maxSnapMeters = maxSnapMeters || 120;
        var nearest = nearestPointOnRoute(route, [lng, lat]);
        if (!nearest || nearest.distM > maxSnapMeters) return [lng, lat];
        return nearest.point;
    }

    function snapLngLatToRoads(lngLat, roadSegments, maxDistM, headingDeg) {
        if (!lngLat || !roadSegments || !roadSegments.length) return lngLat;
        maxDistM = maxDistM || 80;
        if (isFinite(headingDeg)) {
            return nearestRoadPointDirected(lngLat, roadSegments, maxDistM, headingDeg) || lngLat;
        }
        return nearestRoadPoint(lngLat, roadSegments, maxDistM) || lngLat;
    }

    /** Road-first truck position — stays on pavement while panning/zooming the map. */
    function resolveTruckDisplayLngLat(lng, lat, headingDeg, route, roadSegments) {
        var hdg = isFinite(headingDeg) ? headingDeg : NaN;
        if (roadSegments && roadSegments.length) {
            var road = nearestRoadPointDirected([lng, lat], roadSegments, 95, hdg);
            if (road) return road;
        }
        if (route && route.length > 1) {
            var routePt = snapLngLatToRoute(lng, lat, route, 95);
            if (roadSegments && roadSegments.length) {
                return nearestRoadPointDirected(routePt, roadSegments, 95, hdg) || routePt;
            }
            return routePt;
        }
        return [lng, lat];
    }

    /** Finds the closest point on segment a-b to point p (all [lon,lat]); returns { point, distSq }. */
    function projectPointOnSegment(p, a, b) {
        var abx = b[0] - a[0];
        var aby = b[1] - a[1];
        var apx = p[0] - a[0];
        var apy = p[1] - a[1];
        var lenSq = abx * abx + aby * aby;
        var t = lenSq > 0 ? (apx * abx + apy * aby) / lenSq : 0;
        if (t < 0) t = 0;
        else if (t > 1) t = 1;
        var cx = a[0] + t * abx;
        var cy = a[1] + t * aby;
        var dx = p[0] - cx;
        var dy = p[1] - cy;
        return { point: [cx, cy], distSq: dx * dx + dy * dy, segT: t };
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

    /** Bearing of the route segment under lngLat (optionally looks one segment ahead). */
    function bearingOnRouteAt(route, lngLat) {
        var nearest = nearestPointOnRoute(route, lngLat);
        if (!nearest || nearest.index < 0) return NaN;
        var from = nearest.point;
        var to = route[nearest.index + 1];
        if (!to) return NaN;
        if (nearest.segT > 0.82 && nearest.index + 2 < route.length) {
            from = route[nearest.index + 1];
            to = route[nearest.index + 2];
        }
        return bearingDeg(from, to);
    }

    /** Pick display heading: telemetry when moving, otherwise route tangent when on the line. */
    function resolveTruckHeading(headingDeg, speedKmh, route, lngLat) {
        var hdg = isFinite(headingDeg) ? headingDeg : NaN;
        if (!route || route.length < 2 || !lngLat) {
            return isFinite(hdg) ? hdg : 0;
        }
        var routeBearing = bearingOnRouteAt(route, lngLat);
        if (!isFinite(routeBearing)) {
            return isFinite(hdg) ? hdg : 0;
        }
        var snap = nearestPointOnRoute(route, lngLat);
        var onRoute = snap && snap.distM < 120;
        if (!isFinite(hdg)) return routeBearing;
        if (onRoute && isFinite(speedKmh) && speedKmh < 4) return routeBearing;
        if (onRoute && headingDelta(hdg, routeBearing) > 30) return routeBearing;
        return hdg;
    }

    function routesNearlyEqual(a, b) {
        if (!a || !b) return !a && !b;
        if (a.length !== b.length || a.length < 2) return false;
        var picks = [0, Math.floor(a.length / 2), a.length - 1];
        for (var i = 0; i < picks.length; i++) {
            var idx = picks[i];
            if (Math.abs(a[idx][0] - b[idx][0]) > 0.0008 || Math.abs(a[idx][1] - b[idx][1]) > 0.0008) {
                return false;
            }
        }
        return true;
    }

    /** Stable key for route-dot GeoJSON — skip setData when unchanged to avoid flicker. */
    function routeDotsDataKey(points) {
        if (!points || !points.length) return '';
        var n = points.length;
        var mid = Math.floor(n / 2);
        return n + '|' +
            points[0][0].toFixed(5) + ',' + points[0][1].toFixed(5) + '|' +
            points[mid][0].toFixed(5) + ',' + points[mid][1].toFixed(5) + '|' +
            points[n - 1][0].toFixed(5) + ',' + points[n - 1][1].toFixed(5);
    }

    function routeGraphKey(coords) {
        if (!coords || coords.length < 2) return '';
        var n = coords.length;
        var mid = Math.floor(n / 2);
        return n + '|' + coords[0][0].toFixed(5) + ',' + coords[0][1].toFixed(5) + '|' +
            coords[mid][0].toFixed(5) + ',' + coords[mid][1].toFixed(5) + '|' +
            coords[n - 1][0].toFixed(5) + ',' + coords[n - 1][1].toFixed(5);
    }

    function turnAngleDeg(a, b, c) {
        return headingDelta(bearingDeg(a, b), bearingDeg(b, c));
    }

    /** Drop back-tracking spikes that make the line wiggle. */
    function removeWigglePoints(coords, maxTurnDeg) {
        if (!coords || coords.length < 3) return coords;
        var out = [coords[0]];
        for (var i = 1; i < coords.length - 1; i++) {
            var ang = turnAngleDeg(out[out.length - 1], coords[i], coords[i + 1]);
            if (ang < maxTurnDeg) out.push(coords[i]);
        }
        out.push(coords[coords.length - 1]);
        return out;
    }

    function isLowMemoryDevice() {
        if (typeof global.TruckDeckNavLowMemory === 'boolean') return global.TruckDeckNavLowMemory;
        var ua = typeof navigator !== 'undefined' ? navigator.userAgent : '';
        if (/Android|iPhone|iPad|iPod|Mobile|Silk/i.test(ua)) return true;
        if (typeof window !== 'undefined' && window.innerWidth > 0 && window.innerWidth < 768) return true;
        return false;
    }

    /** Evenly sample a polyline to a maximum point count (last-resort fallback). */
    function decimateRouteUniform(coords, maxPoints) {
        if (!coords || coords.length <= maxPoints) return coords;
        if (maxPoints < 2) return [coords[0], coords[coords.length - 1]];
        var out = new Array(maxPoints);
        out[0] = coords[0];
        var step = (coords.length - 1) / (maxPoints - 1);
        for (var i = 1; i < maxPoints - 1; i++) {
            out[i] = coords[Math.round(i * step)];
        }
        out[maxPoints - 1] = coords[coords.length - 1];
        return out;
    }

    function perpendicularDistMeters(p, a, b) {
        var proj = projectPointOnSegment(p, a, b);
        return haversineMeters(p, proj.point);
    }

    /** Ramer–Douglas–Peucker — keeps bends while reducing point count. */
    function rdpDecimate(coords, epsilonM) {
        if (!coords || coords.length < 3) return coords;
        var end = coords.length - 1;
        var maxDist = 0;
        var maxIdx = 0;
        for (var i = 1; i < end; i++) {
            var d = perpendicularDistMeters(coords[i], coords[0], coords[end]);
            if (d > maxDist) {
                maxDist = d;
                maxIdx = i;
            }
        }
        if (maxDist > epsilonM) {
            var left = rdpDecimate(coords.slice(0, maxIdx + 1), epsilonM);
            var right = rdpDecimate(coords.slice(maxIdx), epsilonM);
            return left.slice(0, -1).concat(right);
        }
        return [coords[0], coords[end]];
    }

    function decimateRouteShape(coords, maxPoints, lowMemory) {
        if (!coords || coords.length <= maxPoints) return coords;
        var epsilon = lowMemory ? 16 : 10;
        var result = rdpDecimate(coords, epsilon);
        var guard = 0;
        while (result.length > maxPoints && epsilon < 220 && guard++ < 14) {
            epsilon *= 1.3;
            result = rdpDecimate(coords, epsilon);
        }
        if (result.length > maxPoints) {
            result = decimateRouteUniform(coords, maxPoints);
        }
        return result;
    }

    function polylineLengthMeters(coords) {
        if (!coords || coords.length < 2) return 0;
        var total = 0;
        for (var i = 0; i < coords.length - 1; i++) {
            total += haversineMeters(coords[i], coords[i + 1]);
        }
        return total;
    }

    function bboxOfCoords(coords) {
        var minX = coords[0][0];
        var maxX = coords[0][0];
        var minY = coords[0][1];
        var maxY = coords[0][1];
        for (var i = 1; i < coords.length; i++) {
            var c = coords[i];
            if (c[0] < minX) minX = c[0];
            if (c[0] > maxX) maxX = c[0];
            if (c[1] < minY) minY = c[1];
            if (c[1] > maxY) maxY = c[1];
        }
        return { minX: minX, minY: minY, maxX: maxX, maxY: maxY };
    }

    function bboxIntersects(a, b) {
        return a.minX <= b.maxX && a.maxX >= b.minX && a.minY <= b.maxY && a.maxY >= b.minY;
    }

    function segmentBBox(a, b, pad) {
        pad = pad || 0.0018;
        return {
            minX: Math.min(a[0], b[0]) - pad,
            maxX: Math.max(a[0], b[0]) + pad,
            minY: Math.min(a[1], b[1]) - pad,
            maxY: Math.max(a[1], b[1]) + pad
        };
    }

    function closestOnLineString(lngLat, line) {
        var best = null;
        for (var i = 0; i < line.length - 1; i++) {
            var proj = projectPointOnSegment(lngLat, line[i], line[i + 1]);
            var distM = haversineMeters(lngLat, proj.point);
            if (!best || distM < best.distM) {
                best = { distM: distM, index: i, point: proj.point, segT: proj.segT };
            }
        }
        return best;
    }

    function comparePositionOnLine(posA, posB) {
        if (posA.index !== posB.index) return posA.index - posB.index;
        return posA.segT - posB.segT;
    }

    function extractLineBetween(line, posA, posB) {
        var ordered = comparePositionOnLine(posA, posB) <= 0;
        var start = ordered ? posA : posB;
        var end = ordered ? posB : posA;
        var fwd = [start.point];
        for (var i = start.index + 1; i <= end.index; i++) {
            fwd.push(line[i]);
        }
        if (end.segT > 0.001 || end.index > start.index) {
            fwd.push(end.point);
        }
        var rev = [end.point];
        for (var j = end.index; j > start.index; j--) {
            rev.push(line[j]);
        }
        if (start.segT > 0.001 || end.index > start.index) {
            rev.push(start.point);
        }
        if (rev.length < 2) return fwd;
        var lenFwd = polylineLengthMeters(fwd);
        var lenRev = polylineLengthMeters(rev);
        if (lenRev < lenFwd * 0.94) return rev;
        return fwd;
    }

    function dedupeAdjacent(coords, minM) {
        minM = minM || 1.5;
        if (!coords || coords.length < 2) return coords;
        var out = [coords[0]];
        for (var i = 1; i < coords.length; i++) {
            if (haversineMeters(out[out.length - 1], coords[i]) >= minM) {
                out.push(coords[i]);
            }
        }
        return out.length > 1 ? out : coords;
    }

    function mergeRoadCaches(base, extra) {
        if (!extra || !extra.length) return base || [];
        if (!base || !base.length) return extra;
        return base.concat(extra);
    }

    /** Extra road geometry along the active route (rendered tiles at sample points). */
    function buildRoadSegmentCacheAlongRoute(map, routeCoords) {
        if (!map || !routeCoords || routeCoords.length < 2) return [];
        var out = [];
        var seen = {};
        function addFeature(f) {
            if (!f || !f.geometry || f.geometry.type !== 'LineString') return;
            var props = f.properties || {};
            if (props.type !== 'road' || props.roadType === 'train') return;
            var c = f.geometry.coordinates;
            if (!c || c.length < 2) return;
            var key = c[0][0].toFixed(5) + ',' + c[0][1].toFixed(5) + '|' + c.length;
            if (seen[key]) return;
            seen[key] = 1;
            out.push({ coords: c, bbox: bboxOfCoords(c) });
        }
        var step = Math.max(1, Math.floor(routeCoords.length / 28));
        for (var i = 0; i < routeCoords.length; i += step) {
            try {
                var px = map.project(routeCoords[i]);
                var feats = map.queryRenderedFeatures(
                    [[px.x - 140, px.y - 140], [px.x + 140, px.y + 140]],
                    { layers: ['roads'] });
                for (var j = 0; j < feats.length; j++) addFeature(feats[j]);
            } catch (e) { /* map not ready */ }
        }
        return out;
    }

    /** Road geometry under the truck (rendered tiles at truck screen position). */
    function buildRoadSegmentCacheAtPoint(map, lngLat) {
        if (!map || !lngLat || !map.isStyleLoaded()) return [];
        var out = [];
        var seen = {};
        function addFeature(f) {
            if (!f || !f.geometry || f.geometry.type !== 'LineString') return;
            var props = f.properties || {};
            if (props.type !== 'road' || props.roadType === 'train') return;
            var c = f.geometry.coordinates;
            if (!c || c.length < 2) return;
            var key = c[0][0].toFixed(5) + ',' + c[0][1].toFixed(5) + '|' + c.length;
            if (seen[key]) return;
            seen[key] = 1;
            out.push({ coords: c, bbox: bboxOfCoords(c) });
        }
        try {
            var px = map.project(lngLat);
            var feats = map.queryRenderedFeatures(
                [[px.x - 200, px.y - 200], [px.x + 200, px.y + 200]],
                { layers: ['roads'] });
            for (var j = 0; j < feats.length; j++) addFeature(feats[j]);
        } catch (e) { /* map not ready */ }
        return out;
    }

    function buildRoadSegmentCache(map, sourceLayer) {
        if (!map || !map.isStyleLoaded()) return [];
        var features;
        try {
            features = map.querySourceFeatures('map', {
                sourceLayer: sourceLayer,
                filter: ['all',
                    ['==', ['geometry-type'], 'LineString'],
                    ['==', ['get', 'type'], 'road'],
                    ['!=', ['get', 'roadType'], 'train']
                ]
            });
        } catch (e) {
            return [];
        }
        var out = [];
        for (var i = 0; i < features.length; i++) {
            var geom = features[i].geometry;
            if (!geom || !geom.coordinates || geom.coordinates.length < 2) continue;
            out.push({ coords: geom.coordinates, bbox: bboxOfCoords(geom.coordinates) });
        }
        return out;
    }

    function maxTurnAlongPolyline(coords) {
        if (!coords || coords.length < 3) return 0;
        var maxTurn = 0;
        for (var t = 1; t < coords.length - 1; t++) {
            var td = turnAngleDeg(coords[t - 1], coords[t], coords[t + 1]);
            if (td > maxTurn) maxTurn = td;
        }
        return maxTurn;
    }

    function traceChordOnRoads(a, b, roadSegments, tight) {
        var segLen = haversineMeters(a, b);
        if (segLen < 8) return [a, b];
        var stepM = tight ? 0.5 : (segLen < 900 ? 4 : (segLen < 1200 ? 7 : 10));
        var steps = Math.max(16, Math.ceil(segLen / stepM));
        var minGap = tight ? 0.35 : 1.1;
        var out = [];
        for (var i = 0; i <= steps; i++) {
            var f = i / steps;
            var p = [a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f];
            var brg = i < steps
                ? bearingDeg(p, [a[0] + (b[0] - a[0]) * ((i + 1) / steps), a[1] + (b[1] - a[1]) * ((i + 1) / steps)])
                : bearingDeg(a, b);
            var snapped = nearestRoadPointDirected(p, roadSegments, 120, brg);
            if (snapped) {
                if (!out.length || haversineMeters(out[out.length - 1], snapped) > minGap) {
                    out.push(snapped);
                }
            } else if (!out.length) {
                out.push(p);
            }
        }
        if (out.length < 2) {
            out.push(nearestRoadPointDirected(b, roadSegments, 110, bearingDeg(a, b)) || b);
        }
        return out;
    }

    /** Extra road-snapped samples on curved legs and at corners. */
    function enrichPathAtTurns(poly, roadSegments) {
        if (!poly || poly.length < 2 || !roadSegments || !roadSegments.length) return poly;
        var out = [poly[0]];
        for (var i = 0; i < poly.length - 1; i++) {
            var a = out[out.length - 1];
            var b = poly[i + 1];
            var bend = 0;
            if (i > 0 && i < poly.length - 1) {
                bend = turnAngleDeg(poly[i - 1], poly[i], poly[i + 1]);
            } else if (i + 2 < poly.length) {
                bend = turnAngleDeg(poly[i], poly[i + 1], poly[i + 2]);
            }
            var chord = haversineMeters(a, b);
            var tight = bend > 1 || chord < 900;
            var traced = traceChordOnRoads(a, b, roadSegments, tight);
            var pathLen = polylineLengthMeters(traced);
            var useTrace = traced.length >= 3 && (pathLen > chord * 1.004 || bend > 1);
            if (useTrace) {
                for (var t = 1; t < traced.length; t++) {
                    var pt = traced[t];
                    if (haversineMeters(out[out.length - 1], pt) > 0.5) out.push(pt);
                }
            } else if (chord > 2) {
                out.push(b);
            }
        }
        return dedupeAdjacent(out, 1.5);
    }

    function snapSegmentWithRoads(a, b, roadSegments, maxEndpointM, relaxed) {
        var segBearing = bearingDeg(a, b);
        var segLen = haversineMeters(a, b);
        if (segLen < 22) return null;
        var sb = segmentBBox(a, b, 0.0026);
        var best = null;
        var bestScore = Infinity;
        var maxRatio = segLen > 900 ? 3.2 : (segLen > 300 ? 3.6 : (segLen < 200 ? 4.3 : 4.0));

        for (var r = 0; r < roadSegments.length; r++) {
            var road = roadSegments[r];
            if (!bboxIntersects(sb, road.bbox)) continue;
            var line = road.coords;
            var ca = closestOnLineString(a, line);
            var cb = closestOnLineString(b, line);
            if (!ca || !cb || ca.distM > maxEndpointM || cb.distM > maxEndpointM) continue;
            var sub = extractLineBetween(line, ca, cb);
            if (!sub || sub.length < 2) continue;
            var roadBearingIn = bearingDeg(sub[0], sub[Math.min(1, sub.length - 1)]);
            var roadBearingOut = bearingDeg(sub[Math.max(0, sub.length - 2)], sub[sub.length - 1]);
            if (headingDelta(segBearing, roadBearingIn) > 125) continue;
            var roadLen = polylineLengthMeters(sub);
            if (roadLen < segLen * 0.32 || roadLen > segLen * maxRatio) continue;
            var maxTurn = maxTurnAlongPolyline(sub);
            if (maxTurn > 132 && roadLen > segLen * 1.8) continue;
            var bearingErr = headingDelta(segBearing, roadBearingIn) + headingDelta(segBearing, roadBearingOut);
            if (bearingErr > (relaxed ? 250 : 220)) continue;
            var score = ca.distM + cb.distM + Math.abs(roadLen - segLen) * 0.07 +
                maxTurn * 0.42 + bearingErr * 0.12;
            if (score < bestScore) {
                bestScore = score;
                best = sub;
            }
        }
        if (!best && !relaxed) {
            return snapSegmentWithRoads(a, b, roadSegments, maxEndpointM + 45, true);
        }
        return best;
    }

    function nearestRoadPoint(pt, roadSegments, maxDistM) {
        return nearestRoadPointDirected(pt, roadSegments, maxDistM, null);
    }

    /** Snap to road; when bearing is set, prefer the lane aligned with travel direction. */
    function nearestRoadPointDirected(pt, roadSegments, maxDistM, bearingDegHint) {
        if (!pt || !roadSegments || !roadSegments.length) return null;
        var best = null;
        var bestScore = Infinity;
        var pad = 0.003;
        var useBearing = isFinite(bearingDegHint);
        for (var r = 0; r < roadSegments.length; r++) {
            var road = roadSegments[r];
            if (pt[0] < road.bbox.minX - pad || pt[0] > road.bbox.maxX + pad ||
                pt[1] < road.bbox.minY - pad || pt[1] > road.bbox.maxY + pad) {
                continue;
            }
            var hit = closestOnLineString(pt, road.coords);
            if (!hit || hit.distM > maxDistM) continue;
            var score = hit.distM;
            if (useBearing) {
                var line = road.coords;
                var i0 = hit.index;
                var i1 = Math.min(i0 + 1, line.length - 1);
                var roadBrg = bearingDeg(line[i0], line[i1]);
                score += headingDelta(bearingDegHint, roadBrg) * 0.42;
            }
            if (score < bestScore) {
                bestScore = score;
                best = hit.point;
            }
        }
        return best;
    }

    /** Subdivide long polyline legs and pull samples onto road centreline (curves + junctions). */
    function enrichRoadPolyline(poly, maxEdgeM, roadSegments) {
        if (!poly || poly.length < 2) return poly;
        maxEdgeM = maxEdgeM || 20;
        var out = [poly[0]];
        for (var i = 0; i < poly.length - 1; i++) {
            var a = poly[i];
            var b = poly[i + 1];
            var len = haversineMeters(a, b);
            var segBrg = bearingDeg(a, b);
            var steps = Math.max(0, Math.min(18, Math.ceil(len / maxEdgeM) - 1));
            for (var s = 1; s <= steps; s++) {
                var t = s / (steps + 1);
                var p = [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t];
                if (roadSegments) {
                    p = nearestRoadPointDirected(p, roadSegments, 88, segBrg) || p;
                }
                out.push(p);
            }
            var endPt = b;
            if (roadSegments) {
                endPt = nearestRoadPointDirected(b, roadSegments, 92, segBrg) || b;
            }
            out.push(endPt);
        }
        return dedupeAdjacent(out, 3);
    }

    /** Pull every vertex onto the nearest road centreline within maxDistM. */
    function snapVerticesToRoads(coords, roadSegments, maxDistM) {
        if (!coords || coords.length < 2 || !roadSegments || !roadSegments.length) return coords;
        maxDistM = maxDistM || 90;
        var out = [];
        for (var i = 0; i < coords.length; i++) {
            var brg = null;
            if (i > 0 && i < coords.length - 1) {
                brg = bearingDeg(coords[i - 1], coords[i + 1]);
            } else if (i > 0) {
                brg = bearingDeg(coords[i - 1], coords[i]);
            } else if (i + 1 < coords.length) {
                brg = bearingDeg(coords[i], coords[i + 1]);
            }
            var snapped = nearestRoadPointDirected(coords[i], roadSegments, maxDistM, brg);
            out.push(snapped || coords[i]);
        }
        return dedupeAdjacent(out, 1.5);
    }

    /** Replace graph chords with road geometry — one stable pass over the full route. */
    function snapRouteToRoads(coords, roadSegments, lowMemory) {
        if (!coords || coords.length < 2 || !roadSegments || !roadSegments.length) return coords;
        var minSegM = lowMemory ? 40 : 28;
        var out = [];

        for (var i = 0; i < coords.length - 1; i++) {
            var a = coords[i];
            var b = coords[i + 1];
            if (out.length === 0) out.push(a);

            if (haversineMeters(a, b) < minSegM) {
                var shortLen = haversineMeters(a, b);
                if (shortLen >= 14) {
                    var shortTrace = traceChordOnRoads(a, b, roadSegments);
                    for (var u = 1; u < shortTrace.length; u++) {
                        var upt = shortTrace[u];
                        if (haversineMeters(out[out.length - 1], upt) > 2) out.push(upt);
                    }
                } else {
                    out.push(b);
                }
                continue;
            }

            var snapped = snapSegmentWithRoads(a, b, roadSegments, 200);
            var useSnap = false;
            if (snapped && snapped.length > 2) {
                var segLen = haversineMeters(a, b);
                var roadLen = polylineLengthMeters(snapped);
                var loopCutoff = segLen < 420 ? 2.35 : 2.15;
                useSnap = roadLen <= loopCutoff || segLen < 140;
            }
            if (useSnap) {
                for (var s = 1; s < snapped.length; s++) {
                    var pt = snapped[s];
                    if (haversineMeters(out[out.length - 1], pt) > 2) {
                        out.push(pt);
                    }
                }
            } else {
                var traced = traceChordOnRoads(a, b, roadSegments);
                for (var t = 1; t < traced.length; t++) {
                    var tpt = traced[t];
                    if (haversineMeters(out[out.length - 1], tpt) > 2) {
                        out.push(tpt);
                    }
                }
            }
        }
        return dedupeAdjacent(out, 2);
    }

    /** Insert points along long segments so curves can be rounded visually. */
    function densifyRoutePolyline(coords, maxSegM) {
        if (!coords || coords.length < 2) return coords;
        maxSegM = maxSegM || 40;
        var out = [coords[0]];
        for (var i = 0; i < coords.length - 1; i++) {
            var a = coords[i];
            var b = coords[i + 1];
            var dist = haversineMeters(a, b);
            var steps = Math.max(0, Math.min(12, Math.floor(dist / maxSegM) - 1));
            for (var s = 1; s <= steps; s++) {
                var t = s / (steps + 1);
                out.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]);
            }
            out.push(b);
        }
        return out;
    }

    /** Chaikin corner-cutting — rounds sharp graph junctions for GPS-style lines. */
    function chaikinSmoothRoute(coords, iterations) {
        if (!coords || coords.length < 3) return coords;
        iterations = iterations || 1;
        var result = coords;
        for (var iter = 0; iter < iterations; iter++) {
            if (result.length > 1800) break;
            var next = [result[0]];
            for (var i = 0; i < result.length - 1; i++) {
                var p0 = result[i];
                var p1 = result[i + 1];
                next.push(
                    [p0[0] * 0.75 + p1[0] * 0.25, p0[1] * 0.75 + p1[1] * 0.25],
                    [p0[0] * 0.25 + p1[0] * 0.75, p0[1] * 0.25 + p1[1] * 0.75]
                );
            }
            next.push(result[result.length - 1]);
            result = next;
        }
        return result;
    }

    function prepareRouteDots(coords, lowMemory, roadSegments) {
        if (!coords || coords.length < 2) return coords;
        var mobile = lowMemory !== false && isLowMemoryDevice();
        coords = removeWigglePoints(coords, 180);
        coords = densifyRoutePolyline(coords, mobile ? 28 : 18);
        if (roadSegments && roadSegments.length) {
            coords = snapVerticesToRoads(coords, roadSegments, 65);
        }
        var maxPts = mobile ? 900 : 2000;
        if (coords.length > maxPts) {
            coords = decimateRouteShape(coords, maxPts, mobile);
            if (roadSegments && roadSegments.length) {
                coords = snapVerticesToRoads(coords, roadSegments, 65);
            }
        }
        return coords;
    }

    function prepareRouteCoords(coords, lowMemory) {
        if (!coords || coords.length < 2) return coords;
        var mobile = lowMemory !== false && isLowMemoryDevice();
        var maxPts = mobile ? 1200 : 2500;
        try {
            if (coords.length < maxPts * 0.9) {
                coords = densifyRoutePolyline(coords, mobile ? 38 : 28);
            }
            coords = removeWigglePoints(coords, 158);
            if (coords.length >= 3 && coords.length <= (mobile ? 1100 : 2000)) {
                coords = chaikinSmoothRoute(coords, 1);
            }
            if (coords.length > maxPts) {
                coords = decimateRouteShape(coords, maxPts, mobile);
            }
            return coords;
        } catch (e) {
            console.warn('[TruckDeck NAV] Route simplify failed, using decimated path', e);
            return decimateRouteShape(coords, maxPts, mobile);
        }
    }

    function smoothRoutePolyline(coords) {
        return prepareRouteCoords(coords);
    }

    function ensureRouteForward(coords, truckLngLat, headingDeg) {
        if (!coords || coords.length < 2 || !isFinite(headingDeg)) return coords;
        var anchor = truckLngLat || coords[0];
        var best = null;
        var bestIndex = -1;
        for (var i = 0; i < coords.length - 1; i++) {
            var proj = projectPointOnSegment(anchor, coords[i], coords[i + 1]);
            if (!best || proj.distSq < best.distSq) {
                best = proj;
                bestIndex = i;
            }
        }
        if (bestIndex < 0) return coords;
        var from = best.point;
        var to = coords[bestIndex + 1];
        var segBearing = bearingDeg(from, to);
        if (headingDelta(headingDeg, segBearing) > 90) {
            return coords.slice().reverse();
        }
        return coords;
    }

    /** Trims a route polyline to the portion from the truck's projected position onward. */
    function trimRouteAhead(route, truckLngLat, hintIndex) {
        if (!route || route.length < 2) return { ahead: route, index: 0 };
        var n = route.length - 1;
        var best = null;
        var bestIndex = -1;
        var lo = 0;
        var hi = n - 1;

        if (hintIndex >= 0 && hintIndex < n) {
            lo = Math.max(0, hintIndex - 80);
            hi = Math.min(n - 1, hintIndex + 320);
        } else if (truckLngLat && route.length > 2 &&
            haversineMeters(truckLngLat, route[0]) < 250) {
            lo = 0;
            hi = Math.min(n - 1, 400);
        } else if (n > 500) {
            var step = Math.max(4, Math.floor(n / 250));
            for (var c = 0; c < n; c += step) {
                var coarse = projectPointOnSegment(truckLngLat, route[c], route[c + 1]);
                if (!best || coarse.distSq < best.distSq) {
                    best = coarse;
                    bestIndex = c;
                }
            }
            lo = Math.max(0, bestIndex - step * 2);
            hi = Math.min(n - 1, bestIndex + step * 2);
            best = null;
            bestIndex = -1;
        }

        for (var i = lo; i <= hi; i++) {
            var proj = projectPointOnSegment(truckLngLat, route[i], route[i + 1]);
            if (!best || proj.distSq < best.distSq) {
                best = proj;
                bestIndex = i;
            }
        }
        if (bestIndex < 0) return { ahead: route, index: 0 };
        var ahead = [best.point];
        for (var j = bestIndex + 1; j < route.length; j++) ahead.push(route[j]);
        if (ahead.length < 2) {
            if (bestIndex > 0) {
                ahead.unshift(route[bestIndex]);
            } else if (route.length > 1) {
                ahead.push(route[route.length - 1]);
            }
        }
        if (truckLngLat && ahead.length >= 2 &&
            haversineMeters(truckLngLat, ahead[0]) > 120) {
            ahead.unshift([truckLngLat[0], truckLngLat[1]]);
        }
        return { ahead: ahead, index: bestIndex };
    }

    function defaultCenter(gameKey) {
        return gameKey === 'ats' ? [-98, 39] : [10, 50];
    }

    function headerCenter(hdr, gameKey) {
        if (hdr && isFinite(hdr.minLon) && isFinite(hdr.maxLon) && isFinite(hdr.minLat) && isFinite(hdr.maxLat)) {
            var c = [(hdr.minLon + hdr.maxLon) / 2, (hdr.minLat + hdr.maxLat) / 2];
            console.log('[TruckDeck NAV] Center from bounds:', c, 'bounds:', hdr.minLon, hdr.minLat, hdr.maxLon, hdr.maxLat);
            return c;
        }
        if (hdr && isFinite(hdr.centerLon) && isFinite(hdr.centerLat)) {
            var c2 = [hdr.centerLon, hdr.centerLat];
            console.log('[TruckDeck NAV] Center from header:', c2);
            return c2;
        }
        var d = defaultCenter(gameKey);
        console.log('[TruckDeck NAV] Using default center:', d);
        return d;
    }

    function sizeMapContainer(container) {
        if (!container) return;
        var parent = container.parentElement;
        var h = (parent && parent.clientHeight) || window.innerHeight || document.documentElement.clientHeight || 480;
        var w = (parent && parent.clientWidth) || window.innerWidth || document.documentElement.clientWidth || 640;
        if (h < 100) h = window.innerHeight || document.documentElement.clientHeight || 480;
        if (w < 100) w = window.innerWidth || document.documentElement.clientWidth || 640;
        container.style.position = 'absolute';
        container.style.top = '0';
        container.style.left = '0';
        container.style.right = '0';
        container.style.bottom = '0';
        container.style.overflow = 'hidden';
        container.style.width = w + 'px';
        container.style.height = h + 'px';
        container.style.minHeight = h + 'px';
    }

    function waitForLayout(container) {
        return new Promise(function (resolve) {
            var attempts = 0;
            function check() {
                sizeMapContainer(container);
                var parent = container && container.parentElement;
                var h = (parent && parent.clientHeight) || container.clientHeight || 0;
                if (h >= 100 || attempts++ > 40) {
                    resolve();
                    return;
                }
                requestAnimationFrame(check);
            }
            check();
        });
    }

    function waitForMapLoad(map, timeoutMs) {
        timeoutMs = timeoutMs || 45000;
        return new Promise(function (resolve, reject) {
            if (map.loaded()) {
                resolve(map);
                return;
            }
            var settled = false;
            function finish(mapInstance) {
                if (settled) return;
                settled = true;
                clearTimeout(timer);
                resolve(mapInstance);
            }
            function fail(err) {
                if (settled) return;
                settled = true;
                clearTimeout(timer);
                reject(err || new Error('Map failed to load'));
            }
            var timer = setTimeout(function () {
                fail(new Error('Map load timed out'));
            }, timeoutMs);
            map.once('load', function () { finish(map); });
            /* Non-fatal tile/sprite/glyph errors are logged but do not abort init. */
            map.on('error', function (evt) {
                var msg = (evt && evt.error && evt.error.message) ? evt.error.message : String(evt);
                if (msg.indexOf('non-existing layer') >= 0) return;
                console.warn('[TruckDeck NAV] MapLibre non-fatal error:', msg);
            });
        });
    }

    function bindSpriteImageFallbacks(map) {
        if (!map || map._tdSpriteFallbackBound) return;
        map._tdSpriteFallbackBound = true;
        var warned = Object.create(null);
        map.on('styleimagemissing', function (e) {
            var id = e && e.id;
            if (!id || warned[id]) return;
            warned[id] = true;
            console.warn('[TruckDeck NAV] Missing map icon:', id);
        });
    }

    function setupMapAfterLoad(map, self, hdr, game) {
        console.log('[TruckDeck NAV] Map loaded');
        sizeMapContainer(self.container);
        map.resize();
        bindMapResize(map, self.container);
        bindMapResizeOverlay(map, self.container, self);
        bindTruckScreenOverlay(map, self);
        if (self._useFixedTruckOverlay()) {
            self._ensureFixedTruckOverlay();
        } else {
            self._ensureTruckScreenOverlay();
            if (self._truckMarker) {
                self._truckMarker.remove();
                self._truckMarker = null;
            }
        }

        if (self._lastLngLat) {
            self.setTruck(self._lastLngLat[0], self._lastLngLat[1], self._lastHeading, self._lastSpeed);
        }

        self._applyPalette(paletteFor(self._mode));

        var interactiveFired = false;
        function markInteractive() {
            if (interactiveFired) return;
            interactiveFired = true;
            self._mapInteractive = true;
            if (self._routeDotsPending && self._routeDotsPending.length > 1) {
                self._scheduleRouteDots(self._routeDotsPending);
            } else if (self._routeFull && self._routeFull.length > 1) {
                self._publishRouteDisplay();
            }
            if (typeof self.options.onInteractive === 'function') {
                try { self.options.onInteractive(); } catch (e) { /* ignore */ }
            }
        }
        map.once('idle', markInteractive);
        setTimeout(markInteractive, self._lowMemory ? 2500 : 5000);

        map.on('idle', function () {
            var manual = Date.now() < (self._manualUntil || 0);
            if (self._lastLngLat && self.map && self.map.isStyleLoaded()) {
                if (!manual) {
                    var now = Date.now();
                    if (now - (self._truckRoadRefreshAt || 0) > 700) {
                        self._truckRoadRefreshAt = now;
                        self._refreshRoadSegments(self._routeFull);
                        self._resnapTruckMarker();
                    }
                } else {
                    self._updateTruckScreenOverlay();
                }
            }
            if (!self._routeGraph || self._routeGeomReady) return;
            self._refreshRoadSegments();
            self._applyRouteGeometry();
        });

        map.on('moveend', function () {
            if (!self._lastLngLat || !self.map || !self.map.isStyleLoaded()) return;
            self._unfreezeTruckScreenOverlay();
            if (Date.now() < (self._manualUntil || 0)) return;
            self._refreshRoadSegments(self._routeFull);
            self._resnapTruckMarker();
        });

        map.on('zoomend', function () {
            if (!self._routeFull) return;
            var z = Math.round(map.getZoom());
            if (z === self._routeDotsZoom) return;
            self._routeDotsZoom = z;
            self._publishRouteDisplay();
        });
    }

    function createMapInstance(self, tileUrl, hdr, game, styleOpts, timeoutMs) {
        var pal = paletteFor(self._mode);
        return waitForLayout(self.container).then(function () {
            if (self.container) {
                self.container.style.backgroundColor = pal.bg;
                self.container.style.display = 'block';
                self.container.style.visibility = 'visible';
                self.container.style.opacity = '1';
            }

            var followPitch = self._followPitch || 0;
            self._includeLabels = styleOpts.includeLabels !== false && !!styleOpts.glyphsUrl;
            self.map = new global.maplibregl.Map({
                container: self.container,
                style: buildStyle(tileUrl, game, self._mode, styleOpts),
                center: headerCenter(hdr, game),
                zoom: 4,
                attributionControl: false,
                pitch: followPitch,
                maxPitch: Math.max(followPitch, 60),
                touchZoomRotate: true,
                touchPitch: followPitch > 0,
                dragRotate: true,
                pitchWithRotate: followPitch > 0,
                cooperativeGestures: false,
                preserveDrawingBuffer: !self._lowMemory
            });

            bindTouchInteraction(self.map, self);
            bindDestinationPick(self.map, self);
            bindSpriteImageFallbacks(self.map);

            self.map.on('load', function () {
                setupMapAfterLoad(self.map, self, hdr, game);
            });

            return waitForMapLoad(self.map, timeoutMs).then(function (map) {
                sizeMapContainer(self.container);
                map.resize();
                return map;
            });
        });
    }

    function bindMapResize(map, container) {
        if (!map || !container) return;
        var parent = container.parentElement;
        var ro = typeof ResizeObserver !== 'undefined' ? new ResizeObserver(function () {
            sizeMapContainer(container);
            try { map.resize(); } catch (e) { /* ignore */ }
        }) : null;
        if (ro && parent) ro.observe(parent);
        window.addEventListener('resize', function () {
            sizeMapContainer(container);
            try { map.resize(); } catch (e) { /* ignore */ }
        });
    }

    function bindTruckScreenOverlay(map, self) {
        if (!map || !self || map._tdTruckOverlayBound) return;
        map._tdTruckOverlayBound = true;
        function onMapFrame() {
            if (self._truckScreenFrozen) return;
            if (self._cameraLock > 0) return;
            self._updateTruckScreenOverlay();
        }
        function onUserPanStart() {
            if (self._cameraLock > 0) return;
            self._freezeTruckScreenOverlay();
        }
        map.on('dragstart', onUserPanStart);
        map.on('zoomstart', onUserPanStart);
        map.on('rotatestart', onUserPanStart);
        map.on('pitchstart', onUserPanStart);
        map.on('move', onMapFrame);
        map.on('rotate', onMapFrame);
        map.on('pitch', onMapFrame);
        map.on('resize', function () {
            self._unfreezeTruckScreenOverlay();
        });
    }

    function bindMapResizeOverlay(map, container, self) {
        if (!self) return;
        function syncLayout() {
            if (self._useFixedTruckOverlay()) self._updateFixedTruckOverlayLayout();
            else self._updateTruckScreenOverlay();
        }
        var ro = typeof ResizeObserver !== 'undefined' ? new ResizeObserver(syncLayout) : null;
        if (ro && container) {
            ro.observe(container);
            if (container.parentElement) ro.observe(container.parentElement);
        }
        window.addEventListener('resize', syncLayout);
    }

    function bindTouchInteraction(map, self) {
        if (!map) return;
        var pauseMs = 45000;
        function pauseFollow() {
            if (self._cameraLock > 0) return;
            self._manualUntil = Date.now() + pauseMs;
        }
        map.on('dragstart', pauseFollow);
        map.on('zoomstart', pauseFollow);
        map.on('rotatestart', pauseFollow);
        map.on('pitchstart', pauseFollow);
    }

    function applyFollowCamera(map, self, opts, immediate) {
        if (!map || !self) return;
        self._cameraLock = (self._cameraLock || 0) + 1;
        try {
            if (immediate) {
                map.jumpTo(opts);
            } else {
                map.easeTo(Object.assign(opts, {
                    duration: 200,
                    easing: function (t) { return t; }
                }));
            }
        } catch (e) { /* ignore */ }
        setTimeout(function () {
            self._cameraLock = Math.max(0, (self._cameraLock || 0) - 1);
        }, 0);
    }

    function bindDestinationPick(map, self) {
        if (!map || !self.options.onPickDestination) return;
        var down = null;
        map.on('mousedown', function (e) {
            down = { x: e.point.x, y: e.point.y };
        });
        map.on('touchstart', function (e) {
            if (e.points && e.points.length === 1) {
                down = { x: e.points[0].x, y: e.points[0].y };
            }
        });
        map.on('mouseup', function (e) {
            if (!down) return;
            var dx = e.point.x - down.x;
            var dy = e.point.y - down.y;
            down = null;
            if ((dx * dx + dy * dy) > 64) return;
            var ll = e.lngLat;
            if (ll) self.options.onPickDestination([ll.lng, ll.lat]);
        });
        map.on('touchend', function (e) {
            if (!down || !e.changedTouches || e.changedTouches.length !== 1) return;
            var rect = map.getContainer().getBoundingClientRect();
            var pt = e.changedTouches[0];
            var x = pt.clientX - rect.left;
            var y = pt.clientY - rect.top;
            var dx = x - down.x;
            var dy = y - down.y;
            down = null;
            if ((dx * dx + dy * dy) > 100) return;
            var ll = map.unproject([x, y]);
            if (ll) self.options.onPickDestination([ll.lng, ll.lat]);
        });
    }

    function pmtilesApi() {
        var lib = global.pmtiles;
        if (!lib || !lib.PMTiles) throw new Error('pmtiles library not loaded');
        return lib;
    }

    function resolveTileUrl(game) {
        var name = game === 'ats' ? 'ats.pmtiles' : 'ets2.pmtiles';
        var candidates = [
            assetUrl('/' + name),
            assetUrl('/maps/generated/' + name)
        ];
        var probeTimeoutMs = isLowMemoryDevice() ? 8000 : 15000;
        var index = 0;
        var lastErr = null;

        function tryNext() {
            if (index >= candidates.length) {
                return Promise.reject(lastErr || new Error(
                    'No PMTiles file found. Generate map in Map Generator (maps/generated/' + name + ').'));
            }
            var url = candidates[index++];
            var PM = pmtilesApi();
            return Promise.race([
                new PM.PMTiles(url).getHeader().then(function (hdr) { return { url: url, hdr: hdr }; }),
                new Promise(function (_, reject) {
                    setTimeout(function () { reject(new Error('PMTiles probe timed out for ' + url)); }, probeTimeoutMs);
                })
            ]).catch(function (err) {
                lastErr = err;
                console.warn('[TruckDeck NAV] PMTiles probe failed for', url, err);
                return tryNext();
            });
        }

        return tryNext();
    }

    function TruckDeckPmtilesMap(container, options) {
        this.container = container;
        this.options = options || {};
        this.map = null;
        this._game = (this.options.game || 'ets2').toLowerCase();
        this._follow = this.options.follow !== false;
        this._rotate = this.options.rotate !== false;
        var followPitch = this.options.followPitch;
        this._followPitch = (typeof followPitch === 'number' && isFinite(followPitch))
            ? Math.max(0, Math.min(60, followPitch))
            : 0;
        var followScreenY = this.options.followTruckScreenY;
        this._followTruckScreenY = (typeof followScreenY === 'number' && isFinite(followScreenY))
            ? Math.max(0.55, Math.min(0.92, followScreenY))
            : 0.8;
        this._truckMarker = null;
        this._routeFull = null;
        this._routeGraph = null;
        this._mode = this.options.mode === 'day' ? 'day' : 'night';
        this._manualUntil = 0;
        this._cameraLock = 0;
        this._lastLngLat = null;
        this._lastHeading = 0;
        this._lastSpeed = 0;
        this._fixedTruckEl = null;
        this._truckScreenLayer = null;
        this._truckOverlayEl = null;
        this._displayLngLat = null;
        this._truckScreenFrozen = false;
        this._truckFrozenPt = null;
        this._lowMemory = isLowMemoryDevice();
        this._fullMap = this.options.fullMapFeatures === true;
        this._routeTrimAt = 0;
        this._routeAheadKey = '';
        this._roadSegments = null;
        this._roadSnapAt = 0;
        this._routeGraphKey = '';
        this._routeGeomReady = false;
        this._routeProcessed = null;
        this._routeSnapAttempts = 0;
        this._routeSnappedToRoads = false;
        this._routeDotsZoom = -1;
        this._routeDotsDataKey = '';
        this._roadSegmentsAt = 0;
        this._mapInteractive = false;
        this._routeDotsGenId = 0;
        this._routeDotsRaf = 0;
        this._routeDotsDebounce = 0;
        this._routeDotsPending = null;
    }

    TruckDeckPmtilesMap.prototype._useFixedTruckOverlay = function () {
        return this._follow && this._followPitch > 0;
    };

    TruckDeckPmtilesMap.prototype._fixedTruckOverlayHost = function () {
        return (this.container && this.container.parentElement) ? this.container.parentElement : this.container;
    };

    TruckDeckPmtilesMap.prototype._truckOverlayHost = function () {
        if (this.container) {
            var dash = this.container.closest('.dashboard.tdn');
            if (dash) return dash;
            if (this.container.parentElement) return this.container.parentElement;
        }
        return this.container;
    };

    TruckDeckPmtilesMap.prototype._ensureTruckScreenOverlay = function () {
        if (this._truckScreenLayer || this._useFixedTruckOverlay()) return;
        if (!this.container) return;
        var layer = document.createElement('div');
        layer.className = 'tdn-truck-screen-layer';
        layer.setAttribute('aria-hidden', 'true');
        var el = createTruckMarkerElement();
        el.className = 'tdn-truck-marker tdn-truck-screen-overlay';
        layer.appendChild(el);
        this.container.appendChild(layer);
        this._truckScreenLayer = layer;
        this._truckOverlayEl = el;
    };

    TruckDeckPmtilesMap.prototype._freezeTruckScreenOverlay = function () {
        if (!this.map || this._useFixedTruckOverlay() || !this._truckOverlayEl) return;
        var pos = this._displayLngLat || this._lastLngLat;
        if (!pos) return;
        try {
            var pt = this.map.project(pos);
            var rot = this._rotate ? 0 : ((this._lastHeading || 0) - this.map.getBearing());
            this._truckFrozenPt = { x: pt.x, y: pt.y, rot: rot };
            this._truckScreenFrozen = true;
            this._applyTruckScreenOverlayPt(pt.x, pt.y, rot);
        } catch (e) { /* ignore */ }
    };

    TruckDeckPmtilesMap.prototype._unfreezeTruckScreenOverlay = function () {
        this._truckScreenFrozen = false;
        this._truckFrozenPt = null;
        this._updateTruckScreenOverlay();
    };

    TruckDeckPmtilesMap.prototype._applyTruckScreenOverlayPt = function (x, y, rot) {
        if (!this._truckOverlayEl) return;
        this._truckOverlayEl.style.visibility = 'visible';
        this._truckOverlayEl.style.left = Math.round(x) + 'px';
        this._truckOverlayEl.style.top = Math.round(y) + 'px';
        this._truckOverlayEl.style.transform = 'translate(-50%, -100%) rotate(' + rot + 'deg)';
    };

    TruckDeckPmtilesMap.prototype._updateTruckScreenOverlay = function () {
        if (this._useFixedTruckOverlay()) {
            if (this._truckScreenLayer) this._truckScreenLayer.style.display = 'none';
            return;
        }
        this._ensureTruckScreenOverlay();
        if (!this._truckOverlayEl || !this._truckScreenLayer || !this.map) return;
        this._truckScreenLayer.style.display = '';
        if (this._truckScreenFrozen && this._truckFrozenPt) {
            this._applyTruckScreenOverlayPt(
                this._truckFrozenPt.x, this._truckFrozenPt.y, this._truckFrozenPt.rot);
            return;
        }
        var pos = this._displayLngLat || this._lastLngLat;
        if (!pos) {
            this._truckOverlayEl.style.visibility = 'hidden';
            return;
        }
        try {
            var pt = this.map.project(pos);
            var w = this.container.clientWidth || 0;
            var h = this.container.clientHeight || 0;
            if (pt.x < -50 || pt.y < -50 || pt.x > w + 50 || pt.y > h + 50) {
                this._truckOverlayEl.style.visibility = 'hidden';
                return;
            }
            var rot = this._rotate ? 0 : ((this._lastHeading || 0) - this.map.getBearing());
            this._applyTruckScreenOverlayPt(pt.x, pt.y, rot);
        } catch (e) {
            this._truckOverlayEl.style.visibility = 'hidden';
        }
    };

    TruckDeckPmtilesMap.prototype._removeTruckScreenOverlay = function () {
        if (this._truckScreenLayer && this._truckScreenLayer.parentNode) {
            this._truckScreenLayer.parentNode.removeChild(this._truckScreenLayer);
        }
        this._truckScreenLayer = null;
        this._truckOverlayEl = null;
    };

    TruckDeckPmtilesMap.prototype._ensureFixedTruckOverlay = function () {
        if (this._fixedTruckEl || !this.container) return;
        var host = this._fixedTruckOverlayHost();
        if (!host) return;
        var el = createTruckMarkerElement();
        el.className = 'tdn-truck-marker tdn-fixed-truck-overlay';
        el.style.pointerEvents = 'none';
        this._fixedTruckEl = el;
        host.appendChild(el);
        this._updateFixedTruckOverlayLayout();
    };

    TruckDeckPmtilesMap.prototype._updateFixedTruckOverlayLayout = function () {
        if (!this._fixedTruckEl || !this.container) return;
        var mapEl = this.container;
        var host = this._fixedTruckEl.parentElement;
        if (!host) return;
        var mapRect = mapEl.getBoundingClientRect();
        var hostRect = host.getBoundingClientRect();
        var screenY = this._followTruckScreenY || 0.8;
        var left = mapRect.left - hostRect.left + (mapRect.width * 0.5);
        var top = mapRect.top - hostRect.top + (mapRect.height * screenY);
        this._fixedTruckEl.style.position = 'absolute';
        this._fixedTruckEl.style.left = Math.round(left) + 'px';
        this._fixedTruckEl.style.top = Math.round(top) + 'px';
        var rot = this._rotate ? 0 : (this._lastHeading || 0);
        this._fixedTruckEl.style.transform = 'translate(-50%, -100%) rotate(' + rot + 'deg)';
    };

    TruckDeckPmtilesMap.prototype._removeFixedTruckOverlay = function () {
        if (this._fixedTruckEl && this._fixedTruckEl.parentNode) {
            this._fixedTruckEl.parentNode.removeChild(this._fixedTruckEl);
        }
        this._fixedTruckEl = null;
    };

    TruckDeckPmtilesMap.prototype._notifyStatus = function (msg) {
        if (typeof this.options.onStatus === 'function') {
            try { this.options.onStatus(msg); } catch (e) { /* ignore */ }
        }
    };

    TruckDeckPmtilesMap.prototype.init = function () {
        var self = this;
        var game = this._game;
        console.log('[TruckDeck NAV] Initializing map for', game, 'mode:', this._mode);

        if (this.map) {
            try { this.destroy(); } catch (e) { /* ignore */ }
        }

        self._notifyStatus('Loading libraries…');
        return ensureLibs().then(function () {
            console.log('[TruckDeck NAV] Libraries loaded');
            if (!global.proj4) throw new Error('proj4 not loaded');
            if (!global.pmtiles || !global.maplibregl) throw new Error('map libraries not loaded');

            if (!global.TruckDeckPmtilesProtocolAdded) {
                try {
                    var PM = pmtilesApi();
                    var protocol = new PM.Protocol();
                    global.maplibregl.addProtocol('pmtiles', protocol.tile);
                    global.TruckDeckPmtilesProtocolAdded = true;
                    console.log('[TruckDeck NAV] PMTiles protocol registered');
                } catch (e) {
                    console.warn('[TruckDeck NAV] PMTiles protocol already registered or failed:', e);
                    global.TruckDeckPmtilesProtocolAdded = true;
                }
            }

            self._notifyStatus('Probing map file…');
            return resolveTileUrl(game).then(function (resolved) {
                var tileUrl = resolved.url;
                var hdr = resolved.hdr;
                self._tileUrl = tileUrl;
                console.log('[TruckDeck NAV] Using PMTiles:', tileUrl, hdr);

                self._notifyStatus('Loading map assets…');
                var mobileLite = self._lowMemory && !self._fullMap;
                var assetsPromise = mobileLite
                    ? resolveGlyphsUrl().then(function (glyphs) { return [null, glyphs]; })
                    : Promise.all([
                        resolveSpriteUrl(),
                        resolveGlyphsUrl({ fullLabels: self._fullMap })
                    ]);

                return assetsPromise.then(function (assets) {
                    var spriteBase = assets[0];
                    var glyphs = assets[1];
                    var styleOpts = {
                        glyphsUrl: glyphs.url,
                        includeLabels: glyphs.includeLabels,
                        spriteBase: spriteBase,
                        includeSymbols: !mobileLite,
                        minimal: mobileLite,
                        rotate: self._rotate
                    };

                    self._notifyStatus('Starting renderer…');
                    var loadTimeout = self._lowMemory ? 12000 : 20000;
                    if (mobileLite) {
                        return createMapInstance(self, tileUrl, hdr, game, styleOpts, loadTimeout);
                    }
                    return createMapInstance(self, tileUrl, hdr, game, styleOpts, loadTimeout)['catch'](function (err) {
                        console.warn('[TruckDeck NAV] Full map style failed, retrying simplified:', err);
                        if (self.map) {
                            try { self.destroy(); } catch (e) { /* ignore */ }
                        }
                        self._notifyStatus('Retrying simplified map…');
                        var minimalOpts = {
                            glyphsUrl: null,
                            includeLabels: false,
                            includeSymbols: false,
                            minimal: true,
                            rotate: self._rotate
                        };
                        return createMapInstance(self, tileUrl, hdr, game, minimalOpts, 25000);
                    });
                });
            });
        });
    };

    /** Applies a palette's colours to the live style via setPaintProperty (no style reload/flicker). */
    TruckDeckPmtilesMap.prototype._applyPalette = function (pal) {
        if (!this.map) return;
        try {
            this.map.setPaintProperty('bg', 'background-color', pal.bg);
            this.map.setPaintProperty('mapAreas', 'fill-color', mapAreaFillExpr(pal));
            this.map.setPaintProperty('roads-casing', 'line-color', roadCasingColorExpr(pal));
            this.map.setPaintProperty('roads', 'line-color', roadColorExpr(pal));
            if (this._includeLabels && this.map.getLayer('city-labels')) {
                this.map.setPaintProperty('city-labels', 'text-color', pal.labelText);
                this.map.setPaintProperty('city-labels', 'text-halo-color', pal.labelHalo);
            }
        } catch (err) {
            console.warn('[TruckDeck NAV] Applying palette failed', err);
        }
    };

    TruckDeckPmtilesMap.prototype._truckAnchorPadding = function (mapHeight) {
        var h = mapHeight || 480;
        var screenY = this._followTruckScreenY || 0.8;
        // Padding shifts the vanishing point; top padding moves the geo center down on screen.
        var top = Math.max(0, Math.round((screenY - 0.5) * 2 * h));
        return { top: top, bottom: 0, left: 0, right: 0 };
    };

    /** Camera pitch + viewport padding so the truck lng/lat sits at followTruckScreenY. */
    TruckDeckPmtilesMap.prototype._followCameraExtras = function () {
        var pitch = this._followPitch || 0;
        if (!pitch) return { pitch: 0 };
        var h = (this.container && this.container.clientHeight) ? this.container.clientHeight : 480;
        return {
            pitch: pitch,
            padding: this._truckAnchorPadding(h)
        };
    };

    TruckDeckPmtilesMap.prototype.setMode = function (mode) {
        mode = mode === 'day' ? 'day' : 'night';
        if (this._mode === mode) return;
        this._mode = mode;
        if (!this.map || !this.map.isStyleLoaded()) return;
        this._applyPalette(paletteFor(mode));
    };

    TruckDeckPmtilesMap.prototype.setTruck = function (lng, lat, headingDeg, speedKmh) {
        if (!isFinite(lng) || !isFinite(lat)) return;

        var speed = isFinite(speedKmh) ? speedKmh : 0;
        this._lastLngLat = [lng, lat];
        this._lastSpeed = speed;

        if (this.map && this.map.isStyleLoaded()) {
            var roadNow = Date.now();
            if (!this._roadSegments || !this._roadSegments.length ||
                roadNow - (this._roadSegmentsAt || 0) > 1200) {
                this._refreshRoadSegments(this._routeFull);
                this._roadSegmentsAt = roadNow;
            }
        }

        var displayLngLat = resolveTruckDisplayLngLat(
            lng, lat, headingDeg, this._routeFull, this._roadSegments);
        this._displayLngLat = displayLngLat;

        var hdg = resolveTruckHeading(headingDeg, speed, this._routeFull, displayLngLat);
        this._lastHeading = hdg;

        if (!this.map) return;

        if (this._useFixedTruckOverlay()) {
            this._ensureFixedTruckOverlay();
            this._updateFixedTruckOverlayLayout();
            this._removeTruckScreenOverlay();
            if (this._truckMarker) {
                this._truckMarker.remove();
                this._truckMarker = null;
            }
        } else {
            if (this._fixedTruckEl) this._removeFixedTruckOverlay();
            if (this._truckMarker) {
                this._truckMarker.remove();
                this._truckMarker = null;
            }
            this._updateTruckScreenOverlay();
        }

        if (this._routeFull) {
            var now = Date.now();
            var trimMs = this._lowMemory ? 700 : 300;
            if (now - this._routeTrimAt >= trimMs) {
                this._routeTrimAt = now;
                var trimmed = trimRouteAhead(this._routeFull, displayLngLat, this._routeTrimHint);
                this._routeTrimHint = trimmed.index;
                var ahead = trimmed.ahead;
                var zoomBucket = Math.round(this.map.getZoom());
                var key = routeAheadDisplayKey(ahead, zoomBucket);
                if (key !== this._routeAheadKey) {
                    this._routeAheadKey = key;
                    this._routeDotsZoom = zoomBucket;
                    this._setRouteSourceData(ahead);
                }
            }
        }

        if (!this._follow) return;
        if (Date.now() < this._manualUntil) return;

        var zoom = Math.max(this.map.getZoom(), 10);
        var bearing = this._rotate ? hdg : 0;
        var center = (this._displayLngLat || this._lastLngLat).slice();
        var camera = this._followCameraExtras();
        var opts = Object.assign({
            center: center,
            bearing: bearing,
            zoom: zoom
        }, camera);

        applyFollowCamera(this.map, this, opts, this._useFixedTruckOverlay());
    };

    /** Resume truck-following after the user panned/zoomed the map manually. */
    TruckDeckPmtilesMap.prototype.recenter = function () {
        this._manualUntil = 0;
        if (this._lastLngLat) {
            this.setTruck(this._lastLngLat[0], this._lastLngLat[1], this._lastHeading, this._lastSpeed);
        }
    };

    TruckDeckPmtilesMap.prototype.zoomBy = function (delta) {
        if (!this.map) return;
        this._manualUntil = Date.now() + 45000;
        var z = this.map.getZoom();
        this.map.easeTo({ zoom: Math.max(3, Math.min(18, z + delta)), duration: 250 });
    };

    TruckDeckPmtilesMap.prototype._applyRouteDotsToMap = function (points, forceClear) {
        if (!this.map) return;
        var source = this.map.getSource('route-dots');
        if (!source) return;
        if (!points || !points.length) {
            if (!forceClear) return;
            this._routeDotsDataKey = '';
            this._routeAheadKey = '';
            source.setData(EMPTY_FEATURE_COLLECTION);
            return;
        }
        var dataKey = routeDotsDataKey(points);
        if (dataKey === this._routeDotsDataKey) return;
        this._routeDotsDataKey = dataKey;
        source.setData({
            type: 'FeatureCollection',
            features: points.map(function (p) {
                return {
                    type: 'Feature',
                    properties: {},
                    geometry: { type: 'Point', coordinates: p }
                };
            })
        });
    };

    function routeAheadDisplayKey(ahead, zoomBucket) {
        if (!ahead || ahead.length < 1) return '';
        var qLng = Math.round(ahead[0][0] * 10000) / 10000;
        var qLat = Math.round(ahead[0][1] * 10000) / 10000;
        return ahead.length + '|' + zoomBucket + '|' + qLng + ',' + qLat;
    }

    function buildRouteDotPoints(coords, zoom, lowMemory, roads, useRoads) {
        if (!coords || coords.length < 2) return [];
        var maxDots = lowMemory ? 3000 : 6000;
        var spacing = routeDotSpacingM(coords, zoom, lowMemory, maxDots);
        if (!useRoads || !roads || !roads.length) {
            return routeCoordsToDotPoints(coords, spacing, maxDots);
        }
        var points = routeCoordsToRoadDotPoints(coords, roads, spacing, maxDots, lowMemory);
        if (points.length) {
            var roadSnapM = lowMemory ? 58 : 72;
            for (var i = 0; i < points.length; i++) {
                var brg = null;
                if (i > 0) brg = bearingDeg(points[i - 1], points[i]);
                else if (i + 1 < points.length) brg = bearingDeg(points[i], points[i + 1]);
                points[i] = nearestRoadPointDirected(points[i], roads, roadSnapM, brg) || points[i];
            }
        }
        return points;
    }

    TruckDeckPmtilesMap.prototype._cancelRouteDotsJob = function () {
        this._routeDotsGenId = (this._routeDotsGenId || 0) + 1;
        if (this._routeDotsRaf) {
            cancelAnimationFrame(this._routeDotsRaf);
            this._routeDotsRaf = 0;
        }
        if (this._routeDotsDebounce) {
            clearTimeout(this._routeDotsDebounce);
            this._routeDotsDebounce = 0;
        }
    };

    /** Build route dots — preview is synchronous so telemetry cannot cancel before paint. */
    TruckDeckPmtilesMap.prototype._scheduleRouteDots = function (coords) {
        var self = this;
        this._routeDotsPending = coords;
        if (this._lowMemory && !this._mapInteractive) return;
        if (!coords || coords.length < 2) {
            return;
        }
        var zoom = this.map ? this.map.getZoom() : 10;
        var preview = buildRouteDotPoints(coords, zoom, this._lowMemory, null, false);
        if (!preview.length) return;
        this._applyRouteDotsToMap(preview);

        var longRoute = polylineLengthM(coords) > 250000 || coords.length > 800;
        if (longRoute || this._lowMemory) return;

        this._cancelRouteDotsJob();
        var genId = this._routeDotsGenId;
        this._routeDotsDebounce = setTimeout(function () {
            self._routeDotsDebounce = 0;
            if (genId !== self._routeDotsGenId) return;
            if (self._routeDotsPending !== coords) return;
            if (!self.map || !self.map.isStyleLoaded()) return;
            self._refreshRoadSegments(coords);
            var roads = self._roadSegments;
            var z = self.map.getZoom();
            var points = buildRouteDotPoints(coords, z, self._lowMemory, roads, !!(roads && roads.length));
            self._applyRouteDotsToMap(points);
        }, 350);
    };

    TruckDeckPmtilesMap.prototype._setRouteSourceData = function (coords) {
        if (!coords || coords.length < 2) return;
        this._scheduleRouteDots(coords);
    };

    TruckDeckPmtilesMap.prototype._refreshRoadSegments = function (routeCoords) {
        if (!this.map || !this.map.isStyleLoaded()) return;
        var sl = this._game === 'ats' ? 'ats' : 'ets2';
        var merged = buildRoadSegmentCache(this.map, sl);
        if (routeCoords && routeCoords.length > 1) {
            merged = mergeRoadCaches(merged, buildRoadSegmentCacheAlongRoute(this.map, routeCoords));
        }
        if (this._lastLngLat) {
            merged = mergeRoadCaches(merged, buildRoadSegmentCacheAtPoint(this.map, this._lastLngLat));
        }
        this._roadSegments = merged;
    };

    TruckDeckPmtilesMap.prototype._resnapTruckMarker = function () {
        if (!this._lastLngLat) return;
        var display = resolveTruckDisplayLngLat(
            this._lastLngLat[0], this._lastLngLat[1],
            this._lastHeading, this._routeFull, this._roadSegments);
        this._displayLngLat = display;
        this._lastHeading = resolveTruckHeading(
            this._lastHeading, this._lastSpeed, this._routeFull, display);
        if (this._useFixedTruckOverlay()) {
            this._updateFixedTruckOverlayLayout();
        } else {
            this._updateTruckScreenOverlay();
        }
    };

    TruckDeckPmtilesMap.prototype._publishRouteDisplay = function () {
        if (this._routeFull && this._lastLngLat) {
            var displayLngLat = resolveTruckDisplayLngLat(
                this._lastLngLat[0], this._lastLngLat[1],
                this._lastHeading, this._routeFull, this._roadSegments);
            this._displayLngLat = displayLngLat;
            if (this._useFixedTruckOverlay()) {
                this._updateFixedTruckOverlayLayout();
            } else {
                this._updateTruckScreenOverlay();
            }
            var trimmed = trimRouteAhead(this._routeFull, displayLngLat, this._routeTrimHint);
            this._routeTrimHint = trimmed.index;
            var ahead = trimmed.ahead;
            var zoomBucket = this.map ? Math.round(this.map.getZoom()) : 10;
            this._routeAheadKey = routeAheadDisplayKey(ahead, zoomBucket);
            this._routeDotsZoom = zoomBucket;
            this._setRouteSourceData(ahead);
        } else {
            this._routeAheadKey = '';
            this._setRouteSourceData(this._routeFull);
        }
    };

    TruckDeckPmtilesMap.prototype._applyRouteGeometry = function () {
        var coords = this._routeGraph;
        if (!coords || coords.length < 2) {
            this._routeFull = null;
            this._routeProcessed = null;
            this._routeGeomReady = false;
            this._setRouteSourceData(null);
            return;
        }

        if (this._routeGeomReady && this._routeProcessed) {
            this._routeFull = this._routeProcessed;
            this._publishRouteDisplay();
            return;
        }

        if (this._routeProcessed && routePathSimilar(coords, this._routeProcessed)) {
            this._routeFull = this._routeProcessed;
            this._routeGeomReady = true;
            this._publishRouteDisplay();
            return;
        }

        coords = ensureRouteForward(coords, this._lastLngLat, this._lastHeading);
        var canSnap = !!(this.map && this.map.isStyleLoaded());
        var longRoute = coords.length > 600;
        if (canSnap && !longRoute) {
            if (!this._roadSegments || !this._roadSegments.length) {
                this._refreshRoadSegments();
            }
            if (this._roadSegments && this._roadSegments.length) {
                coords = snapRouteToRoads(coords, this._roadSegments, this._lowMemory);
                coords = snapVerticesToRoads(coords, this._roadSegments, 85);
                this._routeSnappedToRoads = true;
            }
            this._routeSnapAttempts = (this._routeSnapAttempts || 0) + 1;
        } else if (canSnap && longRoute) {
            this._routeSnappedToRoads = true;
        }
        this._routeProcessed = coords;
        this._routeGeomReady = this._routeSnappedToRoads ||
            !canSnap ||
            (this._routeSnapAttempts || 0) >= 10;

        this._routeFull = coords;
        this._routeAheadKey = '';
        this._routeDotsZoom = -1;
        this._routeDotsDataKey = '';
        this._routeTrimAt = 0;
        this._routeTrimHint = 0;
        this._publishRouteDisplay();
    };

    /** Sets the full road-following route (array of [lon,lat]); trimmed to "ahead of truck" on each setTruck() call. */
    TruckDeckPmtilesMap.prototype.setRoute = function (coords) {
        var next = (coords && coords.length > 1) ? coords : null;
        var key = next ? routeGraphKey(next) : '';
        if (key !== this._routeGraphKey) {
            this._routeGraphKey = key;
            this._routeGeomReady = false;
            this._routeProcessed = null;
            this._routeSnapAttempts = 0;
            this._routeSnappedToRoads = false;
            this._routeTrimHint = 0;
        }
        this._routeGraph = next;
        var self = this;
        if (this._routeSetTimer) clearTimeout(this._routeSetTimer);
        this._routeSetTimer = setTimeout(function () {
            self._routeSetTimer = 0;
            if (self._routeGraph !== next) return;
            self._applyRouteGeometry();
        }, 0);
    };

    TruckDeckPmtilesMap.prototype.setLaneHint = function () {
        /* lane / turn indicators removed from NAV skin */
    };

    TruckDeckPmtilesMap.prototype.setCheckpoint = function (lngLat) {
        if (!this.map) return;
        var source = this.map.getSource('route-checkpoint');
        if (!source) return;
        if (!lngLat || !isFinite(lngLat[0]) || !isFinite(lngLat[1])) {
            source.setData(EMPTY_FEATURE_COLLECTION);
            return;
        }
        source.setData({
            type: 'FeatureCollection',
            features: [{
                type: 'Feature',
                properties: {},
                geometry: { type: 'Point', coordinates: [lngLat[0], lngLat[1]] }
            }]
        });
    };

    TruckDeckPmtilesMap.prototype.clearCheckpoint = function () {
        this.setCheckpoint(null);
    };

    TruckDeckPmtilesMap.prototype.clearRoute = function () {
        this._routeFull = null;
        this._routeGraph = null;
        this._routeGraphKey = '';
        this._routeGeomReady = false;
        this._routeProcessed = null;
        this._routeSnapAttempts = 0;
        this._routeSnappedToRoads = false;
        this._routeAheadKey = '';
        this._routeDotsDataKey = '';
        this._routeDotsPending = null;
        this._cancelRouteDotsJob();
        this._applyRouteDotsToMap(null, true);
    };

    TruckDeckPmtilesMap.prototype.destroy = function () {
        this._cancelRouteDotsJob();
        this._removeFixedTruckOverlay();
        this._removeTruckScreenOverlay();
        if (this._truckMarker) {
            this._truckMarker.remove();
            this._truckMarker = null;
        }
        if (this.map) {
            try { this.map.remove(); } catch (e) { /* ignore */ }
            this.map = null;
        }
        this._routeFull = null;
        this._routeGraph = null;
        this._tileUrl = null;
    };

    global.TruckDeckPmtilesMap = TruckDeckPmtilesMap;
})(typeof window !== 'undefined' ? window : this);
