/*

    Shared helpers for skin mock.html previews (no telemetry server required).

    Provides the Funbit Dashboard stub and small utilities used by all mocks.

*/

(function (global) {

    'use strict';



    var SPEED_KEY = 'rc.speedUnit';



    function Dashboard() {}



    Dashboard.getSpeedUnit = function () {

        try {

            return localStorage.getItem(SPEED_KEY) === 'mph' ? 'mph' : 'kmh';

        } catch (e) {

            return 'kmh';

        }

    };



    Dashboard.setSpeedUnit = function (unit) {

        var u = unit === 'mph' ? 'mph' : 'kmh';

        try { localStorage.setItem(SPEED_KEY, u); } catch (e) { /* ignore */ }

        if (typeof global.dispatchEvent === 'function') {

            try {

                global.dispatchEvent(new CustomEvent('rc-speedunit-change', { detail: { unit: u } }));

            } catch (evErr) { /* ignore */ }

        }

        return u;

    };



    Dashboard.toggleSpeedUnit = function () {

        return Dashboard.setSpeedUnit(Dashboard.getSpeedUnit() === 'mph' ? 'kmh' : 'mph');

    };



    global.Funbit = {

        Ets: {

            Telemetry: {

                Dashboard: Dashboard

            }

        }

    };



    function makeUtils(opts) {

        opts = opts || {};

        return {

            preloadImages: function () {},

            formatFloat: function (v) { return opts.formatFloat ? opts.formatFloat(v) : v; },

            triggerGameAction: opts.triggerGameAction || function (action) {

                console.log('[mock] action', action);

            }

        };

    }



    /** Integrate a winding GPS path (metres + heading 0..1). Mutates state. */

    function stepDrive(state, dt, t) {

        dt = Math.min(0.1, dt || 0.05);

        var kmh = state.kmh;

        if (typeof state.speedFn === 'function') {

            kmh = state.speedFn(t);

        }

        kmh = Math.max(0, kmh);

        var v = kmh / 3.6;

        var hh = state.headingRad;

        if (typeof state.headingFn === 'function') {

            hh = state.headingFn(t);

        }

        state.x += (-Math.sin(hh)) * v * dt;

        state.z += (-Math.cos(hh)) * v * dt;

        state.headingRad = hh;

        state.heading01 = ((hh / (Math.PI * 2)) % 1 + 1) % 1;

        state.speed = kmh;

        return state;

    }



    function bootCountries(dash) {

        if (!dash) return Promise.resolve();

        function apply(list) {

            if (Array.isArray(list) && list.length) dash._countriesEts2 = list;

        }

        if (global.RcCountry && global.RcCountry.loadEts2Countries) {

            return global.RcCountry.loadEts2Countries().then(apply)['catch'](function () {

                return fetch('../../scripts/countries-ets2.json')

                    .then(function (r) { return r.ok ? r.json() : []; })

                    .then(apply)['catch'](function () { /* preview without countries */ });

            });

        }

        return fetch('../../scripts/countries-ets2.json')

            .then(function (r) { return r.ok ? r.json() : []; })

            .then(apply)['catch'](function () { /* ignore */ });

    }



    global.MockHarness = {

        makeUtils: makeUtils,

        stepDrive: stepDrive,

        bootCountries: bootCountries

    };

}(typeof window !== 'undefined' ? window : globalThis));

