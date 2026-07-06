/* Route maneuver + lane hint for TruckDeck NAV (derived from polyline; not in-game GPS). */
(function (global) {
    'use strict';

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

    function projectPointOnSegment(p, a, b) {
        var abx = b[0] - a[0];
        var aby = b[1] - a[1];
        var apx = p[0] - a[0];
        var apy = p[1] - a[1];
        var lenSq = abx * abx + aby * aby;
        var t = lenSq > 0 ? (apx * abx + apy * aby) / lenSq : 0;
        if (t < 0) t = 0;
        else if (t > 1) t = 1;
        return [a[0] + t * abx, a[1] + t * aby];
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

    function signedDelta(fromBearing, toBearing) {
        var d = (toBearing - fromBearing + 540) % 360 - 180;
        return d;
    }

    var TURN_THRESHOLD_DEG = 28;
    var GUIDANCE_DECIMATE_M = 40;
    var MIN_TURN_LEG_M = 35;

    /** ETS2 world-map breakpoint spacing (~254 mi / 409 km). */
    var GAME_CHECKPOINT_SPACING_M = 408772;

    function estimateGameCheckpointM(remainingToDestM, peakNavM) {
        if (!isFinite(remainingToDestM) || remainingToDestM <= 0) return NaN;
        var totalM = (isFinite(peakNavM) && peakNavM >= remainingToDestM)
            ? peakNavM
            : remainingToDestM;
        if (remainingToDestM < GAME_CHECKPOINT_SPACING_M * 0.55) {
            return Math.round(remainingToDestM);
        }
        var driven = Math.max(0, totalM - remainingToDestM);
        var intoSeg = driven % GAME_CHECKPOINT_SPACING_M;
        var nextM = (intoSeg < 800) ? GAME_CHECKPOINT_SPACING_M : (GAME_CHECKPOINT_SPACING_M - intoSeg);
        return Math.round(Math.min(nextM, remainingToDestM));
    }

    /**
     * Checkpoint ETA: proportional slice of game navigation.routeTimeSeconds.
     * Dynamic via telemetry — the game recalculates route time for traffic/speed;
     * do not multiply by current speed (that double-counts and adds ~17 min).
     */
    function estimateGameCheckpointSec(checkpointM, remainingToDestM, navTimeSec) {
        if (!isFinite(checkpointM) || checkpointM <= 0 ||
            !isFinite(remainingToDestM) || remainingToDestM <= 50 ||
            !isFinite(navTimeSec) || navTimeSec <= 0) {
            return null;
        }
        var proportional = (checkpointM / remainingToDestM) * navTimeSec;
        return Math.max(60, Math.round(proportional));
    }

    /** Walk forward along the route from truck position by forwardM metres. */
    function pointAlongRouteForward(routeCoords, truckLngLat, forwardM) {
        if (!routeCoords || routeCoords.length < 2 || !truckLngLat ||
            !isFinite(forwardM) || forwardM <= 0) return null;
        var hit = nearestOnRoute(routeCoords, truckLngLat);
        if (!hit) return null;
        var need = forwardM;
        var segStart = hit.point;
        for (var j = hit.seg + 1; j < routeCoords.length; j++) {
            var from = (j === hit.seg + 1) ? segStart : routeCoords[j - 1];
            var to = routeCoords[j];
            if (!from || !to || from.length < 2 || to.length < 2) continue;
            var segM = haversineMeters(from, to);
            if (need <= segM) {
                var frac = segM > 0 ? need / segM : 0;
                return [
                    from[0] + (to[0] - from[0]) * frac,
                    from[1] + (to[1] - from[1]) * frac
                ];
            }
            need -= segM;
        }
        var end = routeCoords[routeCoords.length - 1];
        return end && end.length >= 2 ? [end[0], end[1]] : null;
    }

    function decimateForGuidance(coords, minM) {
        if (!coords || coords.length < 3) return coords || [];
        var out = [coords[0]];
        for (var i = 1; i < coords.length; i++) {
            if (haversineMeters(out[out.length - 1], coords[i]) >= minM) out.push(coords[i]);
        }
        var last = coords[coords.length - 1];
        if (out[out.length - 1] !== last) out.push(last);
        return out.length < 2 ? coords : out;
    }

    function nearestOnRoute(routeCoords, truckLngLat) {
        if (!routeCoords || routeCoords.length < 2 || !truckLngLat) return null;
        var bestSeg = -1;
        var bestDistSq = Infinity;
        var truckOnRoute = truckLngLat;
        for (var i = 0; i < routeCoords.length - 1; i++) {
            var proj = projectPointOnSegment(truckLngLat, routeCoords[i], routeCoords[i + 1]);
            var dx = truckLngLat[0] - proj[0];
            var dy = truckLngLat[1] - proj[1];
            var dSq = dx * dx + dy * dy;
            if (dSq < bestDistSq) {
                bestDistSq = dSq;
                bestSeg = i;
                truckOnRoute = proj;
            }
        }
        if (bestSeg < 0) return null;
        return { seg: bestSeg, point: truckOnRoute, distSq: bestDistSq };
    }

    /** Road distance from truck to end of polyline along the route. */
    function remainingPathMeters(routeCoords, truckLngLat) {
        var hit = nearestOnRoute(routeCoords, truckLngLat);
        if (!hit) return NaN;
        var total = 0;
        var segStart = hit.point;
        for (var j = hit.seg + 1; j < routeCoords.length; j++) {
            var from = (j === hit.seg + 1) ? segStart : routeCoords[j - 1];
            total += haversineMeters(from, routeCoords[j]);
        }
        return total;
    }

    /** Polyline from truck projection to end, optionally capped at maxLenM along the route. */
    function routePrefixFromTruck(routeCoords, truckLngLat, maxLenM) {
        var hit = nearestOnRoute(routeCoords, truckLngLat);
        if (!hit) return routeCoords;
        var out = [hit.point];
        var accum = 0;
        var segStart = hit.point;
        for (var j = hit.seg + 1; j < routeCoords.length; j++) {
            var from = (j === hit.seg + 1) ? segStart : routeCoords[j - 1];
            var to = routeCoords[j];
            var stepM = haversineMeters(from, to);
            if (maxLenM > 0 && accum + stepM > maxLenM) {
                var need = maxLenM - accum;
                var frac = stepM > 0 ? need / stepM : 0;
                out.push([
                    from[0] + (to[0] - from[0]) * frac,
                    from[1] + (to[1] - from[1]) * frac
                ]);
                return out;
            }
            accum += stepM;
            out.push(to);
        }
        return out;
    }

    /** Point along route measured backwards from the destination (game nav remaining). */
    function pointFromRouteEnd(routeCoords, remainingToDestM) {
        if (!routeCoords || routeCoords.length < 2 || !isFinite(remainingToDestM) || remainingToDestM <= 0) {
            return null;
        }
        var need = remainingToDestM;
        for (var i = routeCoords.length - 1; i > 0; i--) {
            var from = routeCoords[i];
            var to = routeCoords[i - 1];
            if (!from || !to || from.length < 2 || to.length < 2) continue;
            var segM = haversineMeters(from, to);
            if (need <= segM) {
                var frac = segM > 0 ? need / segM : 0;
                return {
                    point: [
                        from[0] + (to[0] - from[0]) * frac,
                        from[1] + (to[1] - from[1]) * frac
                    ],
                    seg: i - 1
                };
            }
            need -= segM;
        }
        var start = routeCoords[0];
        return start && start.length >= 2 ? { point: [start[0], start[1]], seg: 0 } : null;
    }

    /**
     * Next checkpoint distance from game nav remaining + route peak (world-map model).
     */
    function analyzeRouteFromGameNav(routeCoords, remainingToDestM, peakNavM, truckLngLat, headingDeg, navTimeSec) {
        var empty = {
            nextManeuverM: null,
            nextManeuverSec: null,
            turn: 'straight',
            laneHint: 'C',
            laneChange: null,
            maneuverLngLat: null,
            hasTurn: false
        };
        var nextM = estimateGameCheckpointM(remainingToDestM, peakNavM);
        if (!isFinite(nextM) || nextM <= 0) return empty;

        var hit = pointFromRouteEnd(routeCoords, remainingToDestM);
        var pos = (hit && hit.point) ? hit.point : truckLngLat;
        var mLngLat = (pos && routeCoords && routeCoords.length > 1)
            ? pointAlongRouteForward(routeCoords, pos, nextM)
            : null;
        var nextSec = estimateGameCheckpointSec(nextM, remainingToDestM, navTimeSec);

        return {
            nextManeuverM: nextM,
            nextManeuverSec: nextSec,
            turn: 'straight',
            laneHint: 'C',
            laneChange: null,
            maneuverLngLat: mLngLat,
            hasTurn: true
        };
    }

    /** Perpendicular distance from truck to nearest route segment (metres). */
    function distanceToRouteMeters(routeCoords, truckLngLat) {
        var hit = nearestOnRoute(routeCoords, truckLngLat);
        if (!hit) return Infinity;
        var lat = truckLngLat[1];
        var metersPerDegLat = 111320;
        var metersPerDegLng = metersPerDegLat * Math.cos((lat * Math.PI) / 180);
        var dx = (truckLngLat[0] - hit.point[0]) * metersPerDegLng;
        var dy = (truckLngLat[1] - hit.point[1]) * metersPerDegLat;
        return Math.sqrt(dx * dx + dy * dy);
    }

    /**
     * @param {Array<[number,number]>} routeCoords
     * @param {[number,number]} truckLngLat
     * @param {number} headingDeg
     * @param {number} speedKmh
     */
    function analyzeRoute(routeCoords, truckLngLat, headingDeg, speedKmh) {
        var empty = {
            nextManeuverM: null,
            nextManeuverSec: null,
            turn: 'straight',
            laneHint: 'C',
            laneChange: null,
            maneuverLngLat: null,
            hasTurn: false
        };
        if (!routeCoords || routeCoords.length < 2 || !truckLngLat) return empty;

        var fullCoords = routeCoords;
        routeCoords = decimateForGuidance(routeCoords, GUIDANCE_DECIMATE_M);
        if (routeCoords.length < 2) return empty;

        var hit = nearestOnRoute(routeCoords, truckLngLat);
        if (!hit) return empty;
        var bestSeg = hit.seg;
        var truckOnRoute = hit.point;

        var distAlong = 0;
        var segStart = truckOnRoute;
        var legM = 0;
        var legBearing = bearingDeg(routeCoords[bestSeg], routeCoords[bestSeg + 1]);

        for (var j = bestSeg + 1; j < routeCoords.length; j++) {
            var from = (j === bestSeg + 1) ? segStart : routeCoords[j - 1];
            var to = routeCoords[j];
            var stepM = haversineMeters(from, to);
            distAlong += stepM;
            legM += stepM;
            if (j < routeCoords.length - 1 && legM >= MIN_TURN_LEG_M) {
                var nextBearing = bearingDeg(routeCoords[j], routeCoords[j + 1]);
                var delta = signedDelta(legBearing, nextBearing);
                if (Math.abs(delta) >= TURN_THRESHOLD_DEG) {
                    var turn = delta > 0 ? 'right' : 'left';
                    var speedMs = Math.max((speedKmh || 0) / 3.6, 30 / 3.6);
                    if (speedKmh < 1) speedMs = 80 / 3.6;
                    var mLngLat = to;
                    return {
                        nextManeuverM: Math.round(distAlong),
                        nextManeuverSec: Math.round(distAlong / speedMs),
                        turn: turn,
                        laneHint: turn === 'left' ? 'L' : (turn === 'right' ? 'R' : 'C'),
                        laneChange: null,
                        maneuverLngLat: mLngLat && mLngLat.length >= 2
                            ? [mLngLat[0], mLngLat[1]] : null,
                        hasTurn: true
                    };
                }
                legBearing = nextBearing;
                legM = 0;
            }
        }

        return empty;
    }

    global.TruckDeckNavGuidance = {
        analyzeRoute: analyzeRoute,
        analyzeRouteFromGameNav: analyzeRouteFromGameNav,
        estimateGameCheckpointM: estimateGameCheckpointM,
        estimateGameCheckpointSec: estimateGameCheckpointSec,
        pointFromRouteEnd: pointFromRouteEnd,
        pointAlongRouteForward: pointAlongRouteForward,
        remainingPathMeters: remainingPathMeters,
        distanceToRouteMeters: distanceToRouteMeters,
        routePrefixFromTruck: routePrefixFromTruck
    };
})(typeof window !== 'undefined' ? window : this);
