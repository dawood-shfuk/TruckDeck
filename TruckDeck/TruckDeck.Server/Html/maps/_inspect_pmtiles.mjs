import { PMTiles } from 'pmtiles';
const p = new PMTiles('file:///root/.truckdeck/map-work/ets2/generated/ets2.pmtiles');
const h = await p.getHeader();
console.log(JSON.stringify(h, null, 2));
