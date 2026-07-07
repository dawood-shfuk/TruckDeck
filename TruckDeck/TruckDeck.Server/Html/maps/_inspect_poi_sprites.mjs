import { createRequire } from 'module';
import { PMTiles } from 'pmtiles';

const require = createRequire(import.meta.url);const VectorTile = require('@mapbox/vector-tile').VectorTile;
const Protobuf = require('pbf');

const pmtilesPath = process.argv[2];
if (!pmtilesPath) {
    console.error('Usage: node _inspect_poi_sprites.mjs <path-to-ets2.pmtiles>');
    process.exit(1);
}
const p = new PMTiles(pmtilesPath);

const sprites = new Map();
const poiTypes = new Map();

async function scanTile(z, x, y) {
    const resp = await p.getZxy(z, x, y);
    if (!resp || !resp.data) return;
    const vt = new VectorTile(new Protobuf(Buffer.from(resp.data)));
    const layer = vt.layers.ets2 || vt.layers.ats;
    if (!layer) return;
    for (let i = 0; i < layer.length; i++) {
        const props = layer.feature(i).properties;
        if (props.type !== 'poi') continue;
        const sp = props.sprite || '(none)';
        sprites.set(sp, (sprites.get(sp) || 0) + 1);
        const pt = props.poiType || '(none)';
        poiTypes.set(pt, (poiTypes.get(pt) || 0) + 1);
    }
}

const header = await p.getHeader();
console.log('PMTiles:', pmtilesPath, 'z', header.minZoom, '-', header.maxZoom);

for (let z = 5; z <= 8; z++) {
    for (let x = 16; x < 40; x++) {
        for (let y = 20; y < 30; y++) {
            try { await scanTile(z, x, y); } catch (_) { /* skip */ }
        }
    }
}

console.log('\nPOI sprites found:');
[...sprites.entries()].sort((a, b) => b[1] - a[1]).forEach(([k, v]) => console.log(' ', v, k));
console.log('\nPOI types found:');
[...poiTypes.entries()].sort((a, b) => b[1] - a[1]).forEach(([k, v]) => console.log(' ', v, k));

const json = require('./sprites/sprites.json');
const missing = [...sprites.keys()].filter(k => k !== '(none)' && !(k in json));
if (missing.length) {
    console.log('\nMISSING from sprites.json:', missing.join(', '));
} else if (sprites.size) {
    console.log('\nAll found sprites exist in sprites.json');
} else {
    console.log('\nNo POI sprites decoded (try different tile range)');
}
