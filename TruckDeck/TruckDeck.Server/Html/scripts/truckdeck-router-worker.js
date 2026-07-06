/*
 * TruckDeck NAV routing worker.
 * Loads the compact {game}-graph.json produced by Html/maps/wsl/build-graph.js
 * (nodes: [[lon,lat],...] and parallel adjacency: [[[toId,distanceMeters],...],...])
 * and runs A* between two already-projected lon/lat points.
 *
 * Runs off the main thread so pathfinding never blocks the UI, even on the
 * largest cross-map routes.
 */
'use strict';

var graphCache = Object.create(null);

var SPEED_MPS = [13.5, 22, 30];

function normalizeNavMode(mode) {
    var m = String(mode || 'best').toLowerCase().replace(/[\s-]+/g, '_');
    if (m === 'shortest') return 'shortest';
    if (m === 'small_roads' || m === 'small' || m === 'scenic' || m === 'slow' || m === 'smallroads') {
        return 'small_roads';
    }
    return 'best';
}

function edgeDistance(edge) {
    return edge[1];
}

function classifyNodeRanks(adjacency) {
    var n = adjacency.length;
    var ranks = new Uint8Array(n);
    for (var i = 0; i < n; i++) {
        var edges = adjacency[i];
        if (!edges || !edges.length) continue;
        var maxLen = 0;
        var longSegs = 0;
        for (var j = 0; j < edges.length; j++) {
            var w = edgeDistance(edges[j]);
            if (w > maxLen) maxLen = w;
            if (w >= 280) longSegs++;
        }
        if (maxLen >= 400 || longSegs >= 2) ranks[i] = 2;
        else if (maxLen >= 180 || edges.length >= 5) ranks[i] = 1;
    }
    return ranks;
}

function nodeDegrees(adjacency) {
    var deg = new Uint16Array(adjacency.length);
    for (var i = 0; i < adjacency.length; i++) {
        deg[i] = (adjacency[i] || []).length;
    }
    return deg;
}

function enrichGraph(data) {
    var graph = { nodes: data.nodes, adjacency: data.adjacency };
    if (Array.isArray(data.nodeRank) && data.nodeRank.length === data.nodes.length) {
        graph.nodeRank = new Uint8Array(data.nodeRank);
    } else {
        graph.nodeRank = classifyNodeRanks(data.adjacency);
    }
    graph.degree = nodeDegrees(data.adjacency);
    return graph;
}

function edgeRank(fromId, toId, edge, nodeRank) {
    if (edge.length >= 3 && edge[2] != null) return edge[2];
    if (!nodeRank) return 0;
    return Math.max(nodeRank[fromId] || 0, nodeRank[toId] || 0);
}

function routingWeight(distM, rank, navMode, destDegree) {
    if (navMode === 'shortest') return distM;
    if (navMode === 'small_roads') {
        if (rank >= 2) return distM * 12;
        if (rank >= 1) return distM * 2.5;
        return distM;
    }
    var time = distM / SPEED_MPS[Math.min(rank, 2)];
    if (destDegree >= 6) time += 70;
    else if (destDegree >= 4) time += 32;
    else if (destDegree >= 3) time += 14;
    return time;
}

function goalHeuristic(nodes, fromId, goalId, navMode) {
    var h = haversineMeters(nodes[fromId], nodes[goalId]);
    if (navMode === 'best') return h / SPEED_MPS[2];
    return h;
}

/** Tries each candidate URL in turn via GET (no HEAD - not reliably supported by the static file server). */
function loadGraph(urls) {
    var cacheKey = urls.join('|');
    if (graphCache[cacheKey]) return graphCache[cacheKey];

    var index = 0;
    var lastErr = null;

    function tryNext() {
        if (index >= urls.length) {
            return Promise.reject(lastErr || new Error('No routing graph URL succeeded'));
        }
        var url = urls[index++];
        return fetch(url)
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status + ' loading ' + url);
                return res.json();
            })
            .then(function (data) {
                if (!data || !Array.isArray(data.nodes) || !Array.isArray(data.adjacency)) {
                    throw new Error('Malformed graph data from ' + url);
                }
                return enrichGraph(data);
            })
            .catch(function (err) {
                lastErr = err;
                return tryNext();
            });
    }

    var promise = tryNext().catch(function (err) {
        delete graphCache[cacheKey];
        throw err;
    });
    graphCache[cacheKey] = promise;
    return promise;
}

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

function nearestNode(nodes, lngLat) {
    var best = -1;
    var bestD = Infinity;
    for (var i = 0; i < nodes.length; i++) {
        var dx = nodes[i][0] - lngLat[0];
        var dy = nodes[i][1] - lngLat[1];
        var d = dx * dx + dy * dy;
        if (d < bestD) { bestD = d; best = i; }
    }
    return best;
}

/* Binary min-heap of [fScore, nodeId] pairs. */
function MinHeap() {
    this.items = [];
}
MinHeap.prototype.push = function (f, id) {
    var items = this.items;
    items.push([f, id]);
    var i = items.length - 1;
    while (i > 0) {
        var parent = (i - 1) >> 1;
        if (items[parent][0] <= items[i][0]) break;
        var tmp = items[parent]; items[parent] = items[i]; items[i] = tmp;
        i = parent;
    }
};
MinHeap.prototype.pop = function () {
    var items = this.items;
    if (items.length === 0) return null;
    var top = items[0];
    var last = items.pop();
    if (items.length > 0) {
        items[0] = last;
        var i = 0;
        for (;;) {
            var left = i * 2 + 1;
            var right = i * 2 + 2;
            var smallest = i;
            if (left < items.length && items[left][0] < items[smallest][0]) smallest = left;
            if (right < items.length && items[right][0] < items[smallest][0]) smallest = right;
            if (smallest === i) break;
            var tmp = items[smallest]; items[smallest] = items[i]; items[i] = tmp;
            i = smallest;
        }
    }
    return top;
};

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

function destinationPoint(from, bearingDeg, distM) {
    var R = 6371000;
    var brng = bearingDeg * Math.PI / 180;
    var lat1 = from[1] * Math.PI / 180;
    var lon1 = from[0] * Math.PI / 180;
    var lat2 = Math.asin(
        Math.sin(lat1) * Math.cos(distM / R) +
        Math.cos(lat1) * Math.sin(distM / R) * Math.cos(brng)
    );
    var lon2 = lon1 + Math.atan2(
        Math.sin(brng) * Math.sin(distM / R) * Math.cos(lat1),
        Math.cos(distM / R) - Math.sin(lat1) * Math.sin(lat2)
    );
    return [lon2 * 180 / Math.PI, lat2 * 180 / Math.PI];
}

function navIterLimit(navDistM, maxIterations) {
    if (maxIterations) return maxIterations;
    if (navDistM > 1200000) return 15000000;
    if (navDistM > 600000) return 10000000;
    return Math.max(1200000, Math.min(10000000, Math.round(navDistM * 4)));
}

function navMatchTolerance(navDistM) {
    if (navDistM > 1200000) return Math.max(navDistM * 0.12, 35000);
    if (navDistM > 500000) return Math.max(navDistM * 0.15, 25000);
    return Math.max(navDistM * 0.08, 800);
}

function navFallbackTolerance(navDistM) {
    if (navDistM > 1200000) return Math.max(navDistM * 0.22, 80000);
    return Math.max(navDistM * 0.28, 40000);
}

function pathLengthMeters(nodes, adjacency, pathIds) {
    var total = 0;
    for (var i = 0; i < pathIds.length - 1; i++) {
        var from = pathIds[i];
        var to = pathIds[i + 1];
        var edges = adjacency[from];
        var found = false;
        for (var j = 0; j < edges.length; j++) {
            if (edges[j][0] === to) {
                total += edges[j][1];
                found = true;
                break;
            }
        }
        if (!found) total += haversineMeters(nodes[from], nodes[to]);
    }
    return total;
}

function routeBetween(graph, from, to, maxIterations, navMode) {
    navMode = normalizeNavMode(navMode);
    var startId = nearestNode(graph.nodes, from);
    var goalId = nearestNode(graph.nodes, to);
    if (startId < 0 || goalId < 0) return null;
    if (startId === goalId) return { pathIds: [startId], coords: [graph.nodes[startId]] };
    var pathIds = aStar(
        graph.nodes, graph.adjacency, startId, goalId, maxIterations || 500000,
        navMode, graph.nodeRank, graph.degree);
    if (!pathIds) return null;
    var coords = new Array(pathIds.length);
    for (var i = 0; i < pathIds.length; i++) coords[i] = graph.nodes[pathIds[i]];
    return { pathIds: pathIds, coords: coords, lengthM: pathLengthMeters(graph.nodes, graph.adjacency, pathIds) };
}

/** Mild preference for straight-through junctions (~30% of full lane-aware tuning). */
function junctionTurnPenalty(nodes, prevId, currentId, nextId, navMode) {
    if (prevId < 0 || currentId < 0 || nextId < 0) return 0;
    var delta = headingDelta(
        bearingDeg(nodes[prevId], nodes[currentId]),
        bearingDeg(nodes[currentId], nodes[nextId]));
    if (delta < 28) return 0;
    if (delta < 50) return navMode === 'shortest' ? 4 : 8;
    if (delta < 80) return navMode === 'shortest' ? 16 : 35;
    if (delta < 115) return navMode === 'shortest' ? 48 : 105;
    return navMode === 'shortest' ? 100 : 250;
}

function aStar(nodes, adjacency, startId, goalId, maxIterations, navMode, nodeRank, degree) {
    navMode = normalizeNavMode(navMode);
    var n = nodes.length;
    var gScore = new Float64Array(n).fill(Infinity);
    var visited = new Uint8Array(n);
    var cameFrom = new Int32Array(n).fill(-1);

    var heap = new MinHeap();
    gScore[startId] = 0;
    heap.push(goalHeuristic(nodes, startId, goalId, navMode), startId);

    var iterations = 0;
    while (heap.items.length > 0) {
        iterations++;
        if (iterations > maxIterations) return null;

        var top = heap.pop();
        var current = top[1];
        if (visited[current]) continue;
        visited[current] = 1;

        if (current === goalId) {
            var path = [];
            var c = current;
            while (c !== -1) {
                path.push(c);
                c = cameFrom[c];
            }
            path.reverse();
            return path;
        }

        var prevId = cameFrom[current];
        var edges = adjacency[current];
        for (var i = 0; i < edges.length; i++) {
            var edge = edges[i];
            var to = edge[0];
            var dist = edgeDistance(edge);
            if (visited[to]) continue;
            var rank = edgeRank(current, to, edge, nodeRank);
            var step = routingWeight(dist, rank, navMode, degree ? degree[to] : 0);
            step += junctionTurnPenalty(nodes, prevId, current, to, navMode);
            var tentativeG = gScore[current] + step;
            if (tentativeG < gScore[to]) {
                gScore[to] = tentativeG;
                cameFrom[to] = current;
                heap.push(tentativeG + goalHeuristic(nodes, to, goalId, navMode), to);
            }
        }
    }
    return null;
}

/** Route toward a point projected along truck heading at roughly the nav distance. */
function matchNavByHeadingProjection(graph, from, navDistM, headingDeg, maxIterations, navMode) {
    if (!isFinite(headingDeg)) return null;
    var iterLimit = navIterLimit(navDistM, maxIterations);
    var dest = destinationPoint(from, headingDeg, navDistM * 0.82);
    var result = routeBetween(graph, from, dest, iterLimit, navMode);
    if (!result || !result.coords || result.coords.length < 2) return null;
    var matchErrorM = Math.abs(result.lengthM - navDistM);
    if (matchErrorM > Math.max(navDistM * 0.25, 50000)) return null;
    return {
        coords: result.coords,
        lengthM: result.lengthM,
        matchErrorM: matchErrorM,
        method: 'heading'
    };
}

/** Find a graph node whose road distance from `from` best matches in-game GPS remaining distance. */
function matchNavByGraphDistance(graph, from, navDistM, headingDeg, maxIterations, navMode) {
    navMode = normalizeNavMode(navMode);
    var startId = nearestNode(graph.nodes, from);
    if (startId < 0) return null;

    var nodes = graph.nodes;
    var adjacency = graph.adjacency;
    var tolerance = navMatchTolerance(navDistM);
    var maxDist = navDistM + tolerance * 1.5;
    var hasHeading = isFinite(headingDeg);
    var iterLimit = navIterLimit(navDistM, maxIterations);

    var distArr = new Float64Array(nodes.length);
    distArr.fill(Infinity);
    var visited = new Uint8Array(nodes.length);

    var heap = new MinHeap();
    distArr[startId] = 0;
    heap.push(0, startId);

    var bestId = -1;
    var bestErr = Infinity;
    var fallbackId = -1;
    var fallbackErr = Infinity;
    var iterations = 0;

    while (heap.items.length > 0 && iterations < iterLimit) {
        iterations++;
        var top = heap.pop();
        var d = top[0];
        var u = top[1];
        if (visited[u]) continue;
        if (d > maxDist) continue;
        visited[u] = 1;

        if (u !== startId) {
            var err = Math.abs(d - navDistM);
            if (err < fallbackErr) {
                fallbackErr = err;
                fallbackId = u;
            }
            if (err <= tolerance && err < bestErr) {
                var ok = true;
                if (hasHeading && navDistM < 450000) {
                    ok = headingDelta(headingDeg, bearingDeg(from, nodes[u])) <= 105;
                }
                if (ok) {
                    bestErr = err;
                    bestId = u;
                }
            }
        }

        var edges = adjacency[u];
        for (var i = 0; i < edges.length; i++) {
            var v = edges[i][0];
            var w = edges[i][1];
            if (visited[v]) continue;
            var nd = d + w;
            if (nd < distArr[v]) {
                distArr[v] = nd;
                heap.push(nd, v);
            }
        }
    }

    var goalId = bestId >= 0 ? bestId : fallbackId;
    if (goalId < 0 || goalId === startId) return null;
    if (bestId < 0 && fallbackErr > navFallbackTolerance(navDistM)) return null;

    var routeIter = Math.max(iterLimit, Math.min(15000000, Math.round(navDistM * 8)));
    var routed = routeBetween(graph, from, nodes[goalId], routeIter, navMode);
    if (!routed || !routed.coords || routed.coords.length < 2) return null;

    return {
        coords: routed.coords,
        lengthM: routed.lengthM,
        matchErrorM: Math.abs(routed.lengthM - navDistM),
        method: bestId >= 0 ? 'graph' : 'graph-fallback'
    };
}

function postRouteResult(requestId, result, error) {
    self.postMessage({
        type: 'route-result',
        requestId: requestId,
        path: result ? result.coords : null,
        dest: result ? result.dest : null,
        error: error || null
    });
}

self.onmessage = function (evt) {
    var msg = evt.data || {};
    var requestId = msg.requestId;

    if (msg.type === 'route') {
        loadGraph(msg.graphUrls)
            .then(function (graph) {
                var maxIter = msg.maxIterations;
                if (!maxIter) {
                    var aerial = haversineMeters(msg.from, msg.to);
                    maxIter = Math.max(500000, Math.min(10000000, Math.round(aerial * 6)));
                }
                var result = routeBetween(graph, msg.from, msg.to, maxIter, msg.navMode);
                if (!result) {
                    postRouteResult(requestId, null, 'No route found between points');
                    return;
                }
                postRouteResult(requestId, { coords: result.coords, dest: null });
            })
            ['catch'](function (err) {
                postRouteResult(requestId, null, (err && err.message) || String(err));
            });
        return;
    }

    if (msg.type === 'match-nav-graph') {
        loadGraph(msg.graphUrls)
            .then(function (graph) {
                var navDist = msg.navDistM;
                if (!navDist || navDist < 50 || !msg.from) {
                    self.postMessage({ type: 'match-nav-result', requestId: requestId, error: 'No navigation input' });
                    return;
                }
                var navMode = normalizeNavMode(msg.navMode);
                var graphMatch = matchNavByGraphDistance(
                    graph, msg.from, navDist, msg.headingDeg, msg.maxIterations, navMode);
                if (!graphMatch || !graphMatch.coords || graphMatch.coords.length < 2) {
                    graphMatch = matchNavByHeadingProjection(
                        graph, msg.from, navDist, msg.headingDeg, msg.maxIterations, navMode);
                }
                if (!graphMatch || !graphMatch.coords || graphMatch.coords.length < 2) {
                    self.postMessage({
                        type: 'match-nav-result',
                        requestId: requestId,
                        error: 'No route matched navigation distance'
                    });
                    return;
                }
                var end = graphMatch.coords[graphMatch.coords.length - 1];
                self.postMessage({
                    type: 'match-nav-result',
                    requestId: requestId,
                    dest: { name: 'GPS', lng: end[0], lat: end[1] },
                    path: graphMatch.coords,
                    lengthM: graphMatch.lengthM,
                    matchErrorM: graphMatch.matchErrorM,
                    method: graphMatch.method || 'graph'
                });
            })
            ['catch'](function (err) {
                self.postMessage({
                    type: 'match-nav-result',
                    requestId: requestId,
                    error: (err && err.message) || String(err)
                });
            });
        return;
    }

    if (msg.type === 'match-nav') {
        loadGraph(msg.graphUrls)
            .then(function (graph) {
                var navDist = msg.navDistM;
                var candidates = msg.candidates || [];
                if (!navDist || navDist < 50 || !msg.from) {
                    self.postMessage({ type: 'match-nav-result', requestId: requestId, error: 'No navigation input' });
                    return;
                }

                var navMode = normalizeNavMode(msg.navMode);

                if (candidates.length === 0) {
                    var graphOnly = matchNavByGraphDistance(
                        graph, msg.from, navDist, msg.headingDeg, msg.maxIterations, navMode);
                    if (graphOnly && graphOnly.coords && graphOnly.coords.length > 1) {
                        var endOnly = graphOnly.coords[graphOnly.coords.length - 1];
                        self.postMessage({
                            type: 'match-nav-result',
                            requestId: requestId,
                            dest: { name: 'GPS', lng: endOnly[0], lat: endOnly[1] },
                            path: graphOnly.coords,
                            lengthM: graphOnly.lengthM,
                            matchErrorM: graphOnly.matchErrorM,
                            method: graphOnly.method
                        });
                        return;
                    }
                    self.postMessage({ type: 'match-nav-result', requestId: requestId, error: 'No navigation candidates' });
                    return;
                }

                var best = null;
                var bestErr = Infinity;
                var routeIter = navIterLimit(navDist, msg.maxIterations);
                var tolerance = navDist > 500000
                    ? Math.max(navDist * 0.22, 15000)
                    : Math.max(navDist * 0.15, 6000);
                var weakTolerance = Math.max(navDist * 0.35, 80000);

                for (var i = 0; i < candidates.length; i++) {
                    var cand = candidates[i];
                    var result = routeBetween(graph, msg.from, [cand.lng, cand.lat], routeIter, navMode);
                    if (!result) continue;
                    var err = Math.abs(result.lengthM - navDist);
                    if (err < bestErr) {
                        bestErr = err;
                        best = {
                            name: cand.name,
                            lng: cand.lng,
                            lat: cand.lat,
                            coords: result.coords,
                            lengthM: result.lengthM
                        };
                    }
                }

                if (best && bestErr <= tolerance) {
                    self.postMessage({
                        type: 'match-nav-result',
                        requestId: requestId,
                        dest: { name: best.name, lng: best.lng, lat: best.lat },
                        path: best.coords,
                        lengthM: best.lengthM,
                        matchErrorM: bestErr,
                        method: 'city'
                    });
                    return;
                }

                var graphMatch = matchNavByGraphDistance(
                    graph, msg.from, navDist, msg.headingDeg, routeIter, navMode);
                if (!graphMatch || !graphMatch.coords || graphMatch.coords.length < 2) {
                    graphMatch = matchNavByHeadingProjection(
                        graph, msg.from, navDist, msg.headingDeg, routeIter, navMode);
                }
                if (graphMatch && graphMatch.coords && graphMatch.coords.length > 1) {
                    var end = graphMatch.coords[graphMatch.coords.length - 1];
                    self.postMessage({
                        type: 'match-nav-result',
                        requestId: requestId,
                        dest: { name: 'GPS', lng: end[0], lat: end[1] },
                        path: graphMatch.coords,
                        lengthM: graphMatch.lengthM,
                        matchErrorM: graphMatch.matchErrorM,
                        method: graphMatch.method
                    });
                    return;
                }

                if (best && bestErr <= weakTolerance) {
                    self.postMessage({
                        type: 'match-nav-result',
                        requestId: requestId,
                        dest: { name: best.name, lng: best.lng, lat: best.lat },
                        path: best.coords,
                        lengthM: best.lengthM,
                        matchErrorM: bestErr,
                        method: 'city-weak'
                    });
                    return;
                }

                self.postMessage({
                    type: 'match-nav-result',
                    requestId: requestId,
                    error: 'No route matched navigation distance'
                });
            })
            ['catch'](function (err) {
                self.postMessage({
                    type: 'match-nav-result',
                    requestId: requestId,
                    error: (err && err.message) || String(err)
                });
            });
    }
};
