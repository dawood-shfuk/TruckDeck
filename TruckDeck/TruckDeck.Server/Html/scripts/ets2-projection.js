/* ETS2/ATS game coords <-> WGS84 (from truckermudgeon/maps projections.ts) */
(function (global) {
    'use strict';

    var earthRadiusMeters = 6370997;
    var lengthOfDegree = (earthRadiusMeters * Math.PI) / 180;

    var ets2Def = {
        mapFactor: [-0.000171570875, 0.0001729241463],
        mapOffset: [16660, 4150]
    };
    var ets2Proj = '+proj=lcc +R=' + earthRadiusMeters +
        ' +lat_1=37 +lat_2=65 +lat_0=50 +lon_0=15';

    var atsDef = {
        mapFactor: [-0.00017706234, 0.000176689948]
    };
    var atsProj = '+proj=lcc +R=' + earthRadiusMeters +
        ' +lat_1=33 +lat_2=45 +lat_0=39 +lon_0=-96';

    function fromEts2CoordsToWgs84(x, y) {
        var sx = Math.floor(x / 4000);
        var sy = Math.floor(y / 4000);
        x -= ets2Def.mapOffset[0];
        y -= ets2Def.mapOffset[1];
        var ukScaleFactor = 0.75;
        var calais = [-31100, -5500];
        var isUk = sx <= -8 && sy <= -2 && !(sx === -8 && sy === -2);
        if (isUk) {
            x = (x + calais[0] / 2) * ukScaleFactor;
            y = (y + calais[1] / 2) * ukScaleFactor;
        }
        var lcc = [
            x * ets2Def.mapFactor[1] * lengthOfDegree,
            y * ets2Def.mapFactor[0] * lengthOfDegree
        ];
        return global.proj4(ets2Proj).inverse(lcc);
    }

    function fromAtsCoordsToWgs84(x, y) {
        var lcc = [
            x * atsDef.mapFactor[1] * lengthOfDegree,
            y * atsDef.mapFactor[0] * lengthOfDegree
        ];
        return global.proj4(atsProj).inverse(lcc);
    }

    function gameToLngLat(gameName, x, z) {
        if (!isFinite(x) || !isFinite(z) || !global.proj4) return null;
        var ll = (gameName === 'ATS')
            ? fromAtsCoordsToWgs84(x, z)
            : fromEts2CoordsToWgs84(x, z);
        return ll && isFinite(ll[0]) && isFinite(ll[1]) ? ll : null;
    }

    /** SCS SDK heading: unit range 0..1, counterclockwise from game north. */
    function normalizeHeadingUnit(heading) {
        if (!isFinite(heading)) return NaN;
        if (Math.abs(heading) > 1.5) {
            return ((heading / (Math.PI * 2)) % 1 + 1) % 1;
        }
        return ((heading % 1) + 1) % 1;
    }

    function bearingDeg(from, to) {
        var dLon = (to[0] - from[0]) * Math.PI / 180;
        var lat1 = from[1] * Math.PI / 180;
        var lat2 = to[1] * Math.PI / 180;
        var y = Math.sin(dLon) * Math.cos(lat2);
        var x = Math.cos(lat1) * Math.sin(lat2) - Math.sin(lat1) * Math.cos(lat2) * Math.cos(dLon);
        return (Math.atan2(y, x) * 180 / Math.PI + 360) % 360;
    }

    /** MapLibre bearing (clockwise from geographic north) for the truck at game coords. */
    function gameHeadingToBearingDeg(gameName, x, z, headingRaw) {
        var unit = normalizeHeadingUnit(headingRaw);
        if (!isFinite(unit) || !isFinite(x) || !isFinite(z)) return 0;
        var rad = unit * Math.PI * 2;
        var step = 80;
        var x2 = x + (-Math.sin(rad)) * step;
        var z2 = z + (-Math.cos(rad)) * step;
        var from = gameToLngLat(gameName, x, z);
        var to = gameToLngLat(gameName, x2, z2);
        if (!from || !to) return 0;
        return bearingDeg(from, to);
    }

    global.TruckDeckProjection = {
        gameToLngLat: gameToLngLat,
        gameHeadingToBearingDeg: gameHeadingToBearingDeg,
        normalizeHeadingUnit: normalizeHeadingUnit
    };
})(typeof window !== 'undefined' ? window : this);
