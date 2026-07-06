(function () {
    'use strict';

    var OFFICIAL_BASE = 'https://truckdeck.site/downloads/';
    var OFFICIAL_PAGE = 'https://truckdeck.site/downloads';

    var ASSETS = {
        setup: {
            btnId: 'btn-setup',
            statusId: 'status-setup',
            cardId: 'card-setup',
            local: ['TruckDeck-Setup.exe', 'Extras/TruckDeck-Setup.exe'],
            official: OFFICIAL_BASE + 'TruckDeck-Setup.exe',
            label: 'Windows installer'
        },
        apk: {
            btnId: 'btn-apk',
            statusId: 'status-apk',
            cardId: 'card-apk',
            local: ['TruckDeck.apk', 'Extras/TruckDeck.apk'],
            official: OFFICIAL_BASE + 'TruckDeck.apk',
            label: 'Android APK'
        },
        mod: {
            btnId: 'btn-mod',
            statusId: 'status-mod',
            cardId: 'card-mod',
            local: ['mod/TruckDeck_NAV.scs', 'Extras/TruckDeck_NAV.scs'],
            official: OFFICIAL_BASE + 'TruckDeck_NAV.scs',
            label: 'NAV mod'
        }
    };

    function getUrl(path) {
        try {
            return Funbit.Ets.Telemetry.Configuration.getUrl(path);
        } catch (e) {
            return path;
        }
    }

    function probe(path) {
        return fetch(getUrl(path), { method: 'HEAD', cache: 'no-store' })
            .then(function (r) { return r.ok ? path : null; })
            .catch(function () { return null; });
    }

    function resolveLocal(candidates) {
        var chain = Promise.resolve(null);
        for (var i = 0; i < candidates.length; i++) {
            (function (p) {
                chain = chain.then(function (found) {
                    if (found) return found;
                    return probe(p);
                });
            })(candidates[i]);
        }
        return chain;
    }

    function wireAsset(asset) {
        var btn = document.getElementById(asset.btnId);
        var status = document.getElementById(asset.statusId);
        var card = document.getElementById(asset.cardId);
        if (!btn || !status) return;

        resolveLocal(asset.local).then(function (localPath) {
            btn.removeAttribute('aria-disabled');
            btn.classList.remove('disabled');
            card && card.classList.remove('download-unavailable');

            if (localPath) {
                btn.href = getUrl(localPath);
                if (localPath.toLowerCase().indexOf('.exe') !== -1) {
                    btn.removeAttribute('download');
                } else {
                    btn.setAttribute('download', '');
                }
                status.textContent = 'Ready — served from this PC';
                status.className = 'download-status ok';
            } else {
                btn.href = asset.official;
                btn.removeAttribute('download');
                btn.setAttribute('target', '_blank');
                btn.setAttribute('rel', 'noopener');
                status.innerHTML = 'Ready — <a href="' + OFFICIAL_PAGE + '" target="_blank" rel="noopener">truckdeck.site/downloads</a>';
                status.className = 'download-status ok';
            }
        });
    }

    function init() {
        wireAsset(ASSETS.setup);
        wireAsset(ASSETS.apk);
        wireAsset(ASSETS.mod);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
