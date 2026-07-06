#!/usr/bin/env node
/*
 * Builds compact routing-graph + city-lookup sidecar files for TruckDeck NAV
 * from truckermudgeon/maps parser + `generator graph` output.
 *
 * Projects node/city game coordinates to WGS84 using the exact same Lambert
 * Conformal Conic formulas as Html/scripts/ets2-projection.js, so the routed
 * line lines up with the live truck marker and the PMTiles road network.
 *
 * Usage:
 *   node build-graph.js --game ets2 --nodes <nodes.json> --graph <graph.json>
 *        --cities <cities.json> --mapToolsRoot <path-with-node_modules> --out <dir>
 */
'use strict';

const fs = require('fs');
const path = require('path');

function parseArgs(argv) {
    const args = {};
    for (let i = 0; i < argv.length; i++) {
        if (argv[i].startsWith('--')) {
            const key = argv[i].slice(2);
            const value = (i + 1 < argv.length && !argv[i + 1].startsWith('--')) ? argv[++i] : true;
            args[key] = value;
        }
    }
    return args;
}

const args = parseArgs(process.argv.slice(2));
const game = args.game === 'ats' ? 'ats' : 'ets2';
if (!args.nodes || !args.graph || !args.cities || !args.mapToolsRoot || !args.out) {
    console.error('Usage: build-graph.js --game <ets2|ats> --nodes <path> --graph <path> --cities <path> --mapToolsRoot <path> --out <dir>');
    process.exit(1);
}

const proj4 = require(path.join(args.mapToolsRoot, 'node_modules', 'proj4'));

/* Mirrors Html/scripts/ets2-projection.js exactly - keep in sync. */
const earthRadiusMeters = 6370997;
const lengthOfDegree = (earthRadiusMeters * Math.PI) / 180;

const ets2Def = { mapFactor: [-0.000171570875, 0.0001729241463], mapOffset: [16660, 4150] };
const ets2Proj = '+proj=lcc +R=' + earthRadiusMeters + ' +lat_1=37 +lat_2=65 +lat_0=50 +lon_0=15';

const atsDef = { mapFactor: [-0.00017706234, 0.000176689948] };
const atsProj = '+proj=lcc +R=' + earthRadiusMeters + ' +lat_1=33 +lat_2=45 +lat_0=39 +lon_0=-96';

function fromEts2CoordsToWgs84(x, y) {
    const sx = Math.floor(x / 4000);
    const sy = Math.floor(y / 4000);
    x -= ets2Def.mapOffset[0];
    y -= ets2Def.mapOffset[1];
    const ukScaleFactor = 0.75;
    const calais = [-31100, -5500];
    const isUk = sx <= -8 && sy <= -2 && !(sx === -8 && sy === -2);
    if (isUk) {
        x = (x + calais[0] / 2) * ukScaleFactor;
        y = (y + calais[1] / 2) * ukScaleFactor;
    }
    const lcc = [x * ets2Def.mapFactor[1] * lengthOfDegree, y * ets2Def.mapFactor[0] * lengthOfDegree];
    return proj4(ets2Proj).inverse(lcc);
}

function fromAtsCoordsToWgs84(x, y) {
    const lcc = [x * atsDef.mapFactor[1] * lengthOfDegree, y * atsDef.mapFactor[0] * lengthOfDegree];
    return proj4(atsProj).inverse(lcc);
}

function gameToLngLat(x, z) {
    if (!isFinite(x) || !isFinite(z)) return null;
    const ll = game === 'ats' ? fromAtsCoordsToWgs84(x, z) : fromEts2CoordsToWgs84(x, z);
    return ll && isFinite(ll[0]) && isFinite(ll[1]) ? ll : null;
}

console.log('[build-graph] Loading nodes:', args.nodes);
const nodesArr = JSON.parse(fs.readFileSync(args.nodes, 'utf8'));
const nodeCoords = new Map();
/* map-parser node schema uses (x, y) as the horizontal plane and z as elevation
   (unlike the live SCS telemetry SDK, which uses x/z horizontal and y as height). */
for (const n of nodesArr) {
    nodeCoords.set(n.uid, [n.x, n.y]);
}
console.log('[build-graph] Loaded', nodeCoords.size, 'raw nodes');

console.log('[build-graph] Loading graph:', args.graph);
const graphData = JSON.parse(fs.readFileSync(args.graph, 'utf8'));
const rawGraph = graphData.graph || [];
console.log('[build-graph] Loaded', rawGraph.length, 'graph nodes');

const uidToCompact = new Map();
const nodes = [];
let skippedNoCoord = 0;

for (const [uid] of rawGraph) {
    const coord = nodeCoords.get(uid);
    if (!coord) { skippedNoCoord++; continue; }
    const ll = gameToLngLat(coord[0], coord[1]);
    if (!ll) { skippedNoCoord++; continue; }
    uidToCompact.set(uid, nodes.length);
    nodes.push([Math.round(ll[0] * 1e6) / 1e6, Math.round(ll[1] * 1e6) / 1e6]);
}

const adjacency = nodes.map(() => []);
let edgeCount = 0;
let skippedEdge = 0;

for (const [uid, edges] of rawGraph) {
    const fromId = uidToCompact.get(uid);
    if (fromId === undefined) continue;
    const list = adjacency[fromId];
    for (const dir of ['forward', 'backward']) {
        const arr = edges[dir];
        if (!arr) continue;
        for (const e of arr) {
            const toId = uidToCompact.get(e.nodeUid);
            if (toId === undefined) { skippedEdge++; continue; }
            list.push([toId, Math.round(e.distance * 10) / 10]);
            edgeCount++;
        }
    }
}

console.log('[build-graph] Built', nodes.length, 'routable nodes,', edgeCount, 'edges',
    '(skipped', skippedNoCoord, 'nodes without coords,', skippedEdge, 'dangling edges)');

console.log('[build-graph] Loading cities:', args.cities);
const citiesArr = JSON.parse(fs.readFileSync(args.cities, 'utf8'));
const cities = citiesArr
    .filter((c) => isFinite(c.x) && isFinite(c.y))
    .map((c) => ({
        name: c.name,
        token: c.token,
        country: c.countryToken,
        x: c.x,
        z: c.y
    }));
console.log('[build-graph] Extracted', cities.length, 'cities');

fs.mkdirSync(args.out, { recursive: true });

const graphOutPath = path.join(args.out, game + '-graph.json');
fs.writeFileSync(graphOutPath, JSON.stringify({ game, nodes, adjacency }));
console.log('[build-graph] Wrote', graphOutPath, '(' + (fs.statSync(graphOutPath).size / 1e6).toFixed(1) + ' MB)');

const citiesOutPath = path.join(args.out, game + '-cities.json');
fs.writeFileSync(citiesOutPath, JSON.stringify(cities));
console.log('[build-graph] Wrote', citiesOutPath, '(' + (fs.statSync(citiesOutPath).size / 1e6).toFixed(2) + ' MB)');
