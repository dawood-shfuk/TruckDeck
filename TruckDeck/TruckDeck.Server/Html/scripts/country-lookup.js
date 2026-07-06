/*
    Resolve ETS2/ATS country from truck world position (nearest anchor points).
    Uses multiple anchors per country for sharper border detection, plus
    movement hinting when two countries are equally close.
    Data: /scripts/countries-ets2.json
*/
(function (global) {
    'use strict';

    var cache = { ets2: null, promise: null, anchorIndex: null };

    function siteRoot() {
        var p = global.location.pathname || '/';
        var i = p.indexOf('/skins/');
        return (i >= 0) ? p.substring(0, i) : p.replace(/\/[^/]*$/, '');
    }

    function loadEts2Countries() {
        if (cache.ets2) return Promise.resolve(cache.ets2);
        if (cache.promise) return cache.promise;
        cache.promise = fetch(siteRoot() + '/scripts/countries-ets2.json')
            .then(function (r) { return r.ok ? r.json() : []; })
            ['catch'](function () { return []; })
            .then(function (list) {
                cache.ets2 = Array.isArray(list) ? list : [];
                cache.anchorIndex = null;
                return cache.ets2;
            });
        return cache.promise;
    }

    function titleCase(s) {
        if (!s) return '';
        return String(s).replace(/[_-]+/g, ' ').replace(/\b\w/g, function (c) { return c.toUpperCase(); });
    }

    function countryPoints(c) {
        if (c.pts && c.pts.length) return c.pts;
        if (isFinite(c.x) && isFinite(c.z)) return [[c.x, c.z]];
        return [];
    }

    function buildAnchorIndex(countries) {
        if (!countries || !countries.length) return [];
        var anchors = [];
        for (var i = 0; i < countries.length; i++) {
            var c = countries[i];
            var pts = countryPoints(c);
            for (var j = 0; j < pts.length; j++) {
                anchors.push({ country: c, x: pts[j][0], z: pts[j][1] });
            }
        }
        return anchors;
    }

    function getAnchorIndex(countries) {
        if (!cache.anchorIndex || cache.anchorIndex._src !== countries) {
            cache.anchorIndex = buildAnchorIndex(countries);
            cache.anchorIndex._src = countries;
        }
        return cache.anchorIndex;
    }

    function nearestCountryAnchors(x, z, countries, topN) {
        if (!countries || !countries.length || !isFinite(x) || !isFinite(z)) return [];
        var anchors = getAnchorIndex(countries);
        var byToken = {};
        for (var i = 0; i < anchors.length; i++) {
            var a = anchors[i];
            var dx = x - a.x;
            var dz = z - a.z;
            var d = dx * dx + dz * dz;
            var t = a.country.t;
            if (!byToken[t] || d < byToken[t].d) {
                byToken[t] = { country: a.country, d: d };
            }
        }
        var hits = [];
        for (var k in byToken) {
            if (byToken.hasOwnProperty(k)) hits.push(byToken[k]);
        }
        hits.sort(function (a, b) { return a.d - b.d; });
        return hits.slice(0, topN || 4);
    }

    function nearestCountry(x, z, countries) {
        var hits = nearestCountryAnchors(x, z, countries, 1);
        return hits.length ? hits[0].country : null;
    }

    function pickBorderCountry(x, z, hits, motion) {
        if (!hits.length) return null;
        if (!motion || hits.length < 2) return hits[0].country;

        var d0 = hits[0].d;
        var d1 = hits[1].d;
        // Border band: runners-up are almost as close as the leader.
        if (d1 > d0 * 1.4) return hits[0].country;

        var vx = 0;
        var vz = 0;
        if (isFinite(motion.prevX) && isFinite(motion.prevZ)) {
            vx = x - motion.prevX;
            vz = z - motion.prevZ;
        }
        var speed2 = vx * vx + vz * vz;

        if (speed2 >= 100) {
            var best = hits[0].country;
            var bestDot = -Infinity;
            for (var i = 0; i < hits.length && i < 4; i++) {
                if (hits[i].d > d0 * 1.55) continue;
                var c = hits[i].country;
                var pts = countryPoints(c);
                var cx = 0;
                var cz = 0;
                for (var j = 0; j < pts.length; j++) {
                    cx += pts[j][0];
                    cz += pts[j][1];
                }
                cx /= pts.length;
                cz /= pts.length;
                var dot = vx * (cx - x) + vz * (cz - z);
                if (dot > bestDot) {
                    bestDot = dot;
                    best = c;
                }
            }
            return best;
        }

        return hits[0].country;
    }

    function resolveCountry(game, x, z, countries, fallback, motion) {
        fallback = fallback || {};
        var isAts = game === 'ATS' || game === 'ats';
        var hasPos = isFinite(x) && isFinite(z);
        if (!isAts && countries && countries.length && hasPos) {
            var hits = nearestCountryAnchors(x, z, countries, 4);
            var pick = pickBorderCountry(x, z, hits, motion);
            if (pick) {
                return { token: pick.t, name: pick.n, flag: pick.f || '' };
            }
        }
        if (!hasPos || !countries || !countries.length) {
            var id = fallback.id || fallback.token || '';
            var name = fallback.name || '';
            if (!name && id) name = titleCase(id);
            if (!name) return null;
            return { token: id, name: name, flag: '' };
        }
        return null;
    }

    global.RcCountry = {
        loadEts2Countries: loadEts2Countries,
        nearestCountry: nearestCountry,
        nearestCountryAnchors: nearestCountryAnchors,
        resolveCountry: resolveCountry,
        titleCase: titleCase
    };
}(typeof window !== 'undefined' ? window : globalThis));
