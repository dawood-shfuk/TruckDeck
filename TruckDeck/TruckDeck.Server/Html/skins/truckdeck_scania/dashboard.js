/*
    TruckDeck Scania — digital cluster skin.
    Feature parity with TruckDeckDash; Scania visual layout.
*/

Funbit.Ets.Telemetry.Dashboard.prototype.initialize = function (skinConfig, utils) {
    var self = this;
    var BRIDGE_PORT = 25556;
    var SKIN = '.dashboard.scania-digital';

    if (typeof utils.triggerGameAction !== 'function') {
        utils._bridgeWarned = false;
        utils.triggerGameAction = function (action) {
            var host = window.location.hostname || '127.0.0.1';
            var url = 'http://' + host + ':' + BRIDGE_PORT + '/api/command/' + action;
            try {
                fetch(url, { method: 'POST' })['catch'](function () {
                    if (!utils._bridgeWarned) {
                        utils._bridgeWarned = true;
                        console.info('[Input Bridge] not running on :' + BRIDGE_PORT);
                    }
                });
            } catch (e) {
                var xhr = new XMLHttpRequest();
                xhr.open('POST', url, true);
                xhr.send();
            }
        };
    }

    // Day / night + accent (tdd.* keys, shared across brand skins)
    this.accents = [
        ['lime', 'LIME'], ['amber', 'AMBER'], ['red', 'RED'], ['blue', 'BLUE'],
        ['green', 'GREEN'], ['ice', 'ICE'], ['violet', 'VIOLET']
    ];
    var savedMode = localStorage.getItem('tdd.mode');
    var savedAccent = localStorage.getItem('tdd.accent');
    var legacyMode = localStorage.getItem('sc.mode');
    if (legacyMode && !savedMode) savedMode = legacyMode;
    this.mode = (savedMode === 'day') ? 'day' : 'night';
    this.accentIdx = 0;
    for (var ai = 0; ai < this.accents.length; ai++) {
        if (this.accents[ai][0] === (savedAccent || 'lime')) { this.accentIdx = ai; break; }
    }
    function applyTheme() {
        var acc = self.accents[self.accentIdx];
        var root = document.querySelector(SKIN);
        if (root) {
            root.setAttribute('data-mode', self.mode);
            root.setAttribute('data-accent', acc[0]);
        }
        $('.js-theme-swatch').css('background', 'var(--brand-accent)');
        $('.js-mode-label').html(self.mode === 'day' ? '&#9728;' : '&#9790;');
    }
    applyTheme();
    $(document).on('click', '.js-theme', function () {
        self.accentIdx = (self.accentIdx + 1) % self.accents.length;
        localStorage.setItem('tdd.accent', self.accents[self.accentIdx][0]);
        applyTheme();
    });
    $(document).on('click', '.js-mode', function () {
        self.mode = (self.mode === 'day') ? 'night' : 'day';
        localStorage.setItem('tdd.mode', self.mode);
        localStorage.setItem('sc.mode', self.mode);
        applyTheme();
    });

    // Screen manager (local only)
    this._cycleLock = false;
    var screenCount = $('.js-screen').length;

    function loadScreenIndex() {
        var idx = parseInt(localStorage.getItem('sc.screenIdx') || '', 10);
        if (!isNaN(idx) && idx >= 0 && idx < screenCount) return idx;
        return 0;
    }
    this.currentScreen = loadScreenIndex();

    function applyScreen() {
        var idx = self.currentScreen;
        if (idx < 0 || idx >= screenCount) idx = 0;
        self.currentScreen = idx;
        $('.js-screen').removeClass('active');
        var $screen = $('.js-screen[data-id="' + idx + '"]').addClass('active');
        $('.js-screen-label').text($screen.data('title') || ('SCREEN ' + (idx + 1)));
        localStorage.setItem('sc.screenIdx', '' + idx);
    }
    applyScreen();

    function cycleScreen() {
        if (self._cycleLock || screenCount < 2) return;
        self._cycleLock = true;
        setTimeout(function () { self._cycleLock = false; }, 280);
        self.currentScreen = (self.currentScreen + 1) % screenCount;
        applyScreen();
    }

    $(document).on('click', '.js-screen-cycle', function (e) {
        e.preventDefault();
        e.stopPropagation();
        cycleScreen();
    });

    (function startDashboardJoyPoll() {
        var host = window.location.hostname || '127.0.0.1';
        var pollUrl = 'http://' + host + ':' + BRIDGE_PORT + '/api/dashboard/events';
        setInterval(function () {
            if (self._joyPollBusy) return;
            self._joyPollBusy = true;
            fetch(pollUrl)['catch'](function () { return null; })
                .then(function (r) { return (r && r.ok) ? r.json() : null; })
                .then(function (data) {
                    if (data && data.events && data.events.screenCycle > 0) cycleScreen();
                })
                ['finally'](function () { self._joyPollBusy = false; });
        }, 120);
    })();

    this.autoBlink = localStorage.getItem('sc.autoBlink') !== '0';
    this._ab = { side: null, armed: false };

    this.econUnit = (Funbit.Ets.Telemetry.Dashboard.getSpeedUnit() === 'mph') ? 'imperial' : 'metric';
    this.econ = { active: false, paused: false, distKm: 0, fuelL: 0, lastOdo: null, lastFuel: null, lastT: null, startISO: null, info: {} };
    this._lastJobActive = false;
    (function restoreLive() {
        try {
            var saved = JSON.parse(localStorage.getItem('sc.econLive') || 'null');
            if (saved && saved.active) {
                var ec = self.econ;
                ec.active = true;
                ec.paused = !!saved.paused;
                ec.distKm = saved.distKm || 0;
                ec.fuelL = saved.fuelL || 0;
                ec.startISO = saved.startISO || new Date().toISOString();
                ec.info = saved.info || {};
                ec.lastOdo = null; ec.lastFuel = null; ec.lastT = Date.now();
            }
        } catch (e) { /* ignore */ }
    })();

    $(document).on('keydown', function (e) {
        if (e.ctrlKey && (e.key === "'" || e.keyCode === 222)) {
            e.preventDefault();
            utils.triggerGameAction('radioStar');
        }
    });

    this.speedUnit = Funbit.Ets.Telemetry.Dashboard.getSpeedUnit();
    $(document).on('click', '.gauge-speed', function () {
        self.speedUnit = Funbit.Ets.Telemetry.Dashboard.toggleSpeedUnit();
        self.econUnit = self.speedUnit === 'mph' ? 'imperial' : 'metric';
    });

    this.gearModes = ['num', 'auto', 'range'];
    this.gearMode = localStorage.getItem('sc.gearMode') || 'num';
    if (this.gearModes.indexOf(this.gearMode) < 0) this.gearMode = 'num';
    $(document).on('click', '.gauge-rpm', function () {
        var idx = (self.gearModes.indexOf(self.gearMode) + 1) % self.gearModes.length;
        self.gearMode = self.gearModes[idx];
        localStorage.setItem('sc.gearMode', self.gearMode);
    });

    (function () {
        var S = function (body) {
            return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" ' +
                'stroke-width="2" stroke-linecap="round" stroke-linejoin="round">' + body + '</svg>';
        };
        var icons = {
            side:    S('<circle cx="12" cy="12" r="3.4"/><path d="M2 9v6M22 9v6M5.5 12H7M17 12h1.5"/>'),
            low:     S('<path d="M14 5a7 7 0 0 1 0 14"/><path d="M3 8l7-1M3 12l7-1M3 16l7-1"/>'),
            high:    S('<path d="M14 5a7 7 0 0 1 0 14"/><path d="M3 8h7M3 12h7M3 16h7"/>'),
            fogf:    S('<path d="M4 8h7M4 12h7M4 16h7"/><path d="M16 6c-2 1.4 2 2.6 0 4s2 2.6 0 4"/>'),
            fogr:    S('<path d="M13 8h7M13 12h7M13 16h7"/><path d="M8 6c-2 1.4 2 2.6 0 4s2 2.6 0 4"/>'),
            beacon:  S('<path d="M8 17h8M9.5 17l1-5h3l1 5"/><path d="M12 9V6M6.5 7L8 8.5M17.5 7L16 8.5"/>'),
            hazard:  S('<path d="M12 4 21 20H3z"/><path d="M12 10v4M12 17h.01"/>'),
            park:    S('<circle cx="12" cy="12" r="5"/><path d="M3.5 7a12 12 0 0 0 0 10M20.5 7a12 12 0 0 1 0 10"/><path d="M12 10v3M12 15.5h.01"/>'),
            diff:    S('<circle cx="4.5" cy="12" r="2"/><circle cx="19.5" cy="12" r="2"/><path d="M6.5 12h3.5M14 12h3.5"/><rect x="10" y="8.5" width="4" height="7" rx="1"/>'),
            ebrake:  S('<path d="M5 10h2V8h4v2h3l3 2v4H5z"/><path d="M18 11h2v3h-2"/>'),
            ret:     S('<path d="M8 4v6M5.5 8L8 10.5L10.5 8M16 4v6M13.5 8L16 10.5L18.5 8"/><path d="M5 15h14"/>'),
            lift:    S('<circle cx="12" cy="16.5" r="3.6"/><path d="M12 10V3M9 6l3-3 3 3"/>'),
            trailer: S('<rect x="2.5" y="8" width="12" height="7" rx="1"/><path d="M14.5 10h3l2.5 2.5V15h-5.5z"/><circle cx="6" cy="17" r="1.5"/><circle cx="17" cy="17" r="1.5"/>'),
            cruise:  S('<circle cx="12" cy="12" r="8"/><path d="M12 12l5-3"/><circle cx="12" cy="12" r="1.4" fill="currentColor"/>'),
            wipers:  S('<path d="M4 18a13 13 0 0 1 16 0"/><path d="M12 18l5-10"/><circle cx="12" cy="18" r="1.2" fill="currentColor"/>'),
            fuel:    S('<path d="M5 20V6a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v14"/><path d="M4 20h10M5 11h7"/><path d="M14 8h3l2 2v6a2 2 0 0 1-4 0v-3h-1"/>'),
            adblue:  S('<path d="M12 4c3.4 4.8 5.4 6.8 5.4 9.6a5.4 5.4 0 0 1-10.8 0C6.6 10.8 8.6 8.8 12 4z"/>'),
            batt:    S('<rect x="3" y="8" width="18" height="9" rx="1"/><path d="M7 8V6h3v2M14 8V6h3v2"/><path d="M6.5 12.5h3M8 11v3M14.5 12.5h3"/>'),
            oil:     S('<path d="M3 13h7l3-2 8 1v3a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><path d="M10 11V8h3l2 2"/><path d="M6 17.5V20"/>'),
            temp:    S('<path d="M11 14V5a2 2 0 0 1 4 0v9a3.4 3.4 0 1 1-4 0z"/><path d="M4 9h3.5M4 13h3.5M4 17h3.5"/>'),
            air:     S('<circle cx="12" cy="12" r="8"/><path d="M12 12l4-2"/><path d="M12 4v2M20 12h-2M12 20v-2M4 12h2"/>'),
            blinkL:  S('<path d="M15 18l-6-6 6-6"/>'),
            blinkR:  S('<path d="M9 18l6-6-6-6"/>')
        };
        for (var k in icons) {
            if (!icons.hasOwnProperty(k)) continue;
            $('.sc-tells .js-tell-' + k).each(function () {
                var $e = $(this);
                if (!$e.attr('title')) $e.attr('title', ($e.text() || k).trim());
                $e.empty().append(icons[k]);
            });
            $('.sc-indicators .js-tell-' + k).html(icons[k]);
        }
        $('.js-fuel-icon').html(icons.fuel);
        $('.js-adblue-icon').html(icons.adblue);
        $('.icon-cruise').html(icons.cruise);
    })();

    this.wakeLock = null;
    this.requestWakeLock = function () {
        if ('wakeLock' in navigator) {
            navigator.wakeLock.request('screen').then(function (wl) { self.wakeLock = wl; })['catch'](function () {});
        }
    };
    this.requestWakeLock();
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') self.requestWakeLock();
    });

    this._countriesEts2 = null;
    this._countryKey = '';
    this._countryPos = { x: null, z: null };
    (function loadCountryScript() {
        if (window.RcCountry) {
            window.RcCountry.loadEts2Countries().then(function (list) { self._countriesEts2 = list; });
            return;
        }
        var p = window.location.pathname || '/';
        var i = p.indexOf('/skins/');
        var root = (i >= 0) ? p.substring(0, i) : p.replace(/\/[^/]*$/, '');
        var s = document.createElement('script');
        s.src = root + '/scripts/country-lookup.js';
        s.onload = function () {
            if (window.RcCountry) {
                window.RcCountry.loadEts2Countries().then(function (list) { self._countriesEts2 = list; });
            }
        };
        document.head.appendChild(s);
    })();
};

Funbit.Ets.Telemetry.Dashboard.prototype._rv = function (obj, path) {
    return path.split('.').reduce(function (p, c) { return (p == null) ? undefined : p[c]; }, obj);
};
Funbit.Ets.Telemetry.Dashboard.prototype._num = function (data, paths) {
    for (var i = 0; i < paths.length; i++) {
        var v = this._rv(data, paths[i]);
        if (typeof v === 'number' && isFinite(v)) return v;
    }
    return null;
};
Funbit.Ets.Telemetry.Dashboard.prototype._bool = function (data, paths) {
    for (var i = 0; i < paths.length; i++) {
        var v = this._rv(data, paths[i]);
        if (typeof v === 'boolean') return v;
    }
    return false;
};
Funbit.Ets.Telemetry.Dashboard.prototype._str = function (data, paths) {
    for (var i = 0; i < paths.length; i++) {
        var v = this._rv(data, paths[i]);
        if (typeof v === 'string' && v) return v;
    }
    return '';
};

Funbit.Ets.Telemetry.Dashboard.prototype._saveEconomySession = function () {
    var ec = this.econ;
    if (ec.distKm <= 0 && ec.fuelL <= 0) return;
    try {
        var log = JSON.parse(localStorage.getItem('sc.fuelLog') || '[]');
        if (!Array.isArray(log)) log = [];
        var info = ec.info || {};
        log.push({
            start: ec.startISO, end: new Date().toISOString(),
            distanceKm: +ec.distKm.toFixed(3), fuelL: +ec.fuelL.toFixed(3),
            from: info.from || '', to: info.to || '', cargo: info.cargo || '',
            massKg: (info.massKg != null ? info.massKg : null)
        });
        localStorage.setItem('sc.fuelLog', JSON.stringify(log));
    } catch (e) { /* ignore */ }
};
Funbit.Ets.Telemetry.Dashboard.prototype._saveEconLive = function () {
    var ec = this.econ;
    try {
        if (!ec.active) { localStorage.removeItem('sc.econLive'); return; }
        localStorage.setItem('sc.econLive', JSON.stringify({
            active: ec.active, paused: ec.paused,
            distKm: ec.distKm, fuelL: ec.fuelL,
            startISO: ec.startISO, info: ec.info
        }));
    } catch (e) { /* ignore */ }
};
Funbit.Ets.Telemetry.Dashboard.prototype._clearEconLive = function () {
    try { localStorage.removeItem('sc.econLive'); } catch (e) { /* ignore */ }
};
Funbit.Ets.Telemetry.Dashboard.prototype._startEcon = function () {
    var ec = this.econ;
    if (ec.active) return;
    ec.active = true; ec.paused = false;
    ec.distKm = 0; ec.fuelL = 0;
    ec.lastOdo = null; ec.lastFuel = null; ec.lastT = Date.now();
    ec.startISO = new Date().toISOString();
    ec.info = this._lastInfo ? JSON.parse(JSON.stringify(this._lastInfo)) : {};
    this._saveEconLive();
};
Funbit.Ets.Telemetry.Dashboard.prototype._stopEcon = function () {
    var ec = this.econ;
    if (!ec.active) return;
    ec.active = false; ec.paused = false;
    this._saveEconomySession();
    this._clearEconLive();
};
Funbit.Ets.Telemetry.Dashboard.prototype._renderEconomy = function () {
    var ec = this.econ;
    var imp = this.econUnit === 'imperial';
    var KM2MI = 0.621371, L2GAL = 0.264172;
    var dist = imp ? ec.distKm * KM2MI : ec.distKm;
    var fuel = imp ? ec.fuelL * L2GAL : ec.fuelL;
    $('.js-econ-dist').text(dist.toFixed(1));
    $('.js-econ-fuel').text(fuel.toFixed(1));
    $('.js-econ-distunit').text(imp ? 'MI' : 'KM');
    $('.js-econ-fuelunit').text(imp ? 'GAL' : 'L');
    $('.js-econ-rateunit').text(imp ? 'MPG' : 'L/100KM');
    var rate;
    if (imp) {
        rate = (ec.fuelL > 0.0001 && ec.distKm > 0.05) ? (ec.distKm * KM2MI) / (ec.fuelL * L2GAL) : null;
    } else {
        rate = ec.distKm > 0.05 ? (ec.fuelL / ec.distKm * 100) : null;
    }
    $('.js-econ-rate').text(rate == null ? '--' : rate.toFixed(2));
};

Funbit.Ets.Telemetry.Dashboard.prototype.filter = function (data, utils) {
    var self = this;
    var num = function (paths) { return self._num(data, paths); };
    var bool = function (paths) { return self._bool(data, paths); };
    var str = function (paths) { return self._str(data, paths); };
    var pad = function (n) { return (n < 10 ? '0' : '') + n; };
    var SKIN = '.dashboard.scania-digital';
    var vcache = self._vcache || (self._vcache = {});
    var setVar = function (sel, name, value) {
        var key = sel + '|' + name;
        if (vcache[key] === value) return;
        vcache[key] = value;
        var els = document.querySelectorAll(sel);
        for (var i = 0; i < els.length; i++) els[i].style.setProperty(name, value);
    };
    var clampPct = function (v) { return Math.max(0, Math.min(100, v)); };

    self._lastInfo = {
        from: str(['job.sourceCity', 'job.citySrc', 'job.cargoSourceCity']),
        to: str(['job.destinationCity', 'job.cityDst', 'job.cargoDestinationCity']),
        cargo: str(['cargo.cargo', 'job.cargo', 'job.cargoName', 'cargo.name']),
        massKg: num(['cargo.mass', 'job.mass', 'cargo.cargoMass'])
    };

    var jobActive = !!self._lastInfo.to;
    if (jobActive && !self._lastJobActive) self._startEcon();
    else if (!jobActive && self._lastJobActive) self._stopEcon();
    if (bool(['jobEvent.jobFinished', 'jobEvent.jobDelivered', 'jobEvent.jobCancelled'])) self._stopEcon();
    self._lastJobActive = jobActive;

    var STEER_MAX_DEG = 120, TURN_DEG = 25, CENTER_DEG = 5;
    var steerDeg = Math.abs(num(['truck.userSteer', 'truck.gameSteer']) || 0) * STEER_MAX_DEG;
    var blinkL = bool(['truck.blinkerLeftActive', 'truck.blinkerLeftOn']);
    var blinkR = bool(['truck.blinkerRightActive', 'truck.blinkerRightOn']);
    var side = (blinkL && !blinkR) ? 'left' : (blinkR && !blinkL) ? 'right' : null;
    var ab = self._ab || (self._ab = { side: null, armed: false });
    if (self.autoBlink && side) {
        if (ab.side !== side) { ab.side = side; ab.armed = false; }
        if (steerDeg >= TURN_DEG) ab.armed = true;
        else if (ab.armed && steerDeg <= CENTER_DEG) {
            utils.triggerGameAction(side === 'left' ? 'blinkerLeft' : 'blinkerRight');
            ab.side = null; ab.armed = false;
        }
    } else { ab.side = null; ab.armed = false; }

    var ec = self.econ;
    if (ec && ec.active && !ec.paused) {
        var nowT = Date.now();
        var kmhRaw = Math.abs(num(['truck.speed']) || 0);
        var odo = num(['truck.odometer']);
        if (odo != null) {
            if (ec.lastOdo != null && odo > ec.lastOdo && (odo - ec.lastOdo) < 50) ec.distKm += odo - ec.lastOdo;
            ec.lastOdo = odo;
        } else if (ec.lastT != null) {
            ec.distKm += kmhRaw * ((nowT - ec.lastT) / 1000) / 3600;
        }
        var fuelRaw = num(['truck.fuel']);
        if (fuelRaw != null) {
            if (ec.lastFuel != null && fuelRaw < ec.lastFuel) ec.fuelL += ec.lastFuel - fuelRaw;
            ec.lastFuel = fuelRaw;
        }
        ec.lastT = nowT;
        if (!ec._lastSave || (nowT - ec._lastSave) > 3000) { ec._lastSave = nowT; self._saveEconLive(); }
    }
    if (ec) self._renderEconomy();

    var isMph = self.speedUnit === 'mph';
    var toUnit = function (v) { return isMph ? v * 0.621371 : v; };
    var speedMax = isMph ? 90 : 120;
    var kmh = Math.abs(num(['truck.speed']) || 0);
    var shownSpeed = Math.round(toUnit(kmh));
    var limitKmh = num(['navigation.speedLimit']);
    var shownLimit = (limitKmh && limitKmh > 0) ? Math.round(toUnit(limitKmh)) : null;
    var speedRatio = (shownLimit && shownLimit > 0) ? (shownSpeed / shownLimit) : null;
    var speedZone = 'neutral';
    if (speedRatio != null) {
        if (speedRatio >= 1) speedZone = 'over';
        else if (speedRatio >= 0.85) speedZone = 'warn';
        else speedZone = 'ok';
    }
    var speedPct;
    if (shownLimit && shownLimit > 0) {
        speedPct = clampPct(shownSpeed / shownLimit * 100);
    } else {
        speedPct = clampPct(shownSpeed / speedMax * 100);
    }
    $('.js-speed, .js-speed-dup').text(shownSpeed);
    $('.js-speed-unit, .js-speed-unit-dup').text(isMph ? 'MPH' : 'KM/H');
    $('.js-speed-bar').css('height', speedPct + '%')
        .removeClass('zone-ok zone-warn zone-over zone-neutral')
        .addClass('zone-' + speedZone);
    $('.speed-bar').removeClass('speed-ok speed-warn speed-over speed-neutral')
        .addClass('speed-' + speedZone);
    $('.speed-bar .sc-gauge-big').removeClass('zone-ok zone-warn zone-over zone-neutral')
        .addClass('zone-' + speedZone);

    var rpm = Math.max(0, num(['truck.engineRpm']) || 0);
    var rpmMax = num(['truck.engineRpmMax']);
    if (!rpmMax || rpmMax < 100) rpmMax = 2500;
    var rpmFrac = clampPct(rpm / rpmMax * 100);
    var rpmShown = Math.round(rpmFrac / 100 * 25000);
    $('.js-rpm').text(rpmShown);
    var rpmCol;
    if (rpmShown >= 20000) rpmCol = '#ffffff';
    else if (rpmShown >= 18000) rpmCol = '#ff453a';
    else if (rpmShown >= 13000) rpmCol = '#ffb020';
    else rpmCol = '#9fb200';
    setVar(SKIN, '--rpmcol', rpmCol);
    $('.js-rpm-bar').css('height', rpmFrac + '%')
        .removeClass('zone-green zone-amber zone-red zone-white')
        .addClass(rpmShown >= 20000 ? 'zone-white' : rpmShown >= 18000 ? 'zone-red' : rpmShown >= 13000 ? 'zone-amber' : 'zone-green');

    var g = (data.truck || {}).gear;
    if (g == null && typeof (data.truck || {}).displayedGear === 'number') g = data.truck.displayedGear;
    var gearText;
    if (g == null || g === 0) gearText = 'N';
    else if (g < 0) gearText = 'R';
    else if (self.gearMode === 'auto') gearText = 'A' + g;
    else if (self.gearMode === 'range') {
        var pair = Math.floor((g - 1) / 2);
        var half = (((g - 1) % 2) === 0) ? 'L' : 'H';
        gearText = (pair === 0 ? 'C' : ('' + pair)) + half;
    } else gearText = '' + g;
    $('.js-gear').text(gearText);

    var shifter = str(['truck.shifterType']);
    var isAuto = shifter ? (shifter === 'automatic' || shifter === 'arcade') : (self.gearMode === 'auto');
    $('.js-trans').text(isAuto ? 'AUTO' : 'MAN').toggleClass('auto', isAuto);

    var ccOn = bool(['truck.cruiseControlOn']);
    var ccSpeed = Math.round(toUnit(num(['truck.cruiseControlSpeed']) || 0));
    $('.js-cruise').toggleClass('active', ccOn).text(ccOn ? ('' + ccSpeed) : 'CRUISE OFF');

    var oil = num(['truck.oilTemperature']);
    var water = num(['truck.waterTemperature']);
    var air = num(['truck.airPressure']);
    var oilP = num(['truck.oilPressure']);
    $('.js-oilTemp').text(oil == null ? '--' : Math.round(oil));
    $('.js-waterTemp').text(water == null ? '--' : Math.round(water));
    $('.js-airPress').text(air == null ? '--' : Math.round(air));
    $('.js-oilPress').text(oilP == null ? '--' : oilP.toFixed(1));

    var fuel = num(['truck.fuel']);
    var cap = num(['truck.fuelCapacity']);
    var pct = (fuel != null && cap && cap > 0) ? clampPct(fuel / cap * 100) : null;
    $('.js-fuel').text(pct == null ? '--' : Math.round(pct));

    var odoVal = num(['truck.odometer']);
    $('.js-odometer').text(odoVal == null ? '--' : Math.round(toUnit(odoVal)).toLocaleString());

    var $units = $('.js-fuel-bars .fuel-unit');
    if ($units.length) {
        var barsOn = pct == null ? 0 : Math.ceil(pct / 10);
        $units.each(function (i) {
            var $u = $(this);
            $u.toggleClass('on', i < barsOn);
            $u.removeClass('red amber green');
            if (i < 3) $u.addClass('red');
            else if (i < 6) $u.addClass('amber');
            else $u.addClass('green');
        });
        var $fi = $('.js-fuel-icon');
        $fi.removeClass('low warn ok');
        if (pct != null) {
            if (pct <= 30) $fi.addClass('low');
            else if (pct <= 60) $fi.addClass('warn');
            else $fi.addClass('ok');
        }
    }

    var distUnitLabel = isMph ? 'MI' : 'KM';
    $('.js-range-unit, .js-dist-unit').text(distUnitLabel);
    var avg = num(['truck.fuelAverageConsumption']);
    if (fuel != null && avg && avg > 0.0001) {
        $('.js-range').text(Math.min(9999, Math.round(toUnit(fuel / avg))));
    } else {
        $('.js-range').text('--');
    }

    var wears = [];
    ['truck.wearEngine', 'truck.wearTransmission', 'truck.wearCabin', 'truck.wearChassis', 'truck.wearWheels'].forEach(function (p) {
        var v = self._rv(data, p);
        if (typeof v === 'number' && isFinite(v)) wears.push(v);
        var key = p.split('.').pop();
        $('.js-' + key).text(typeof v === 'number' ? Math.round(v * 100) : '--');
    });

    var steer = num(['truck.userSteer', 'truck.gameSteer']);
    if (steer == null) steer = 0;
    steer = Math.max(-1, Math.min(1, steer));
    var steerRot = steer * 120;
    $('.js-steer-deg').text(Math.abs(Math.round(steerRot)));
    $('.js-steer-dir').text(Math.abs(steer) < 0.02 ? '' : (steer < 0 ? 'L' : 'R'));

    var pitch = num(['truck.placement.pitch', 'truck.orientation.pitch', 'truck.placement.rotationX']);
    if (pitch == null) pitch = 0;
    var pitchDeg = pitch * 360;
    while (pitchDeg > 180) pitchDeg -= 360;
    while (pitchDeg < -180) pitchDeg += 360;
    $('.js-incl-deg').text(Math.abs(pitchDeg).toFixed(1));

    var adblue = num(['truck.adblue']);
    var adblueCap = num(['truck.adblueCapacity']);
    var adbluePct = (adblue != null && adblueCap && adblueCap > 0) ? clampPct(adblue / adblueCap * 100) : null;
    $('.js-adblue-val').text(adbluePct == null ? '--' : Math.round(adbluePct));

    var batt = num(['truck.batteryVoltage']);
    $('.js-batt').text(batt == null ? '--' : batt.toFixed(1));

    var setTell = function (cls, on) { $('.js-tell-' + cls).toggleClass('on', !!on); };
    blinkL = bool(['truck.blinkerLeftActive']) || bool(['truck.blinkerLeftOn']);
    blinkR = bool(['truck.blinkerRightActive']) || bool(['truck.blinkerRightOn']);
    var hazard = bool(['truck.lightsHazardOn', 'truck.blinkersHazardOn']) || (blinkL && blinkR);
    var attached = self._rv(data, 'trailer.attached');
    if (typeof attached !== 'boolean') attached = self._rv(data, 'trailers.0.attached');
    var ret = num(['truck.retarderBrake', 'truck.retarderLevel']);

    setTell('blinkL', blinkL);
    setTell('blinkR', blinkR);
    setTell('side', bool(['truck.lightsParkingOn', 'truck.lightsSideOn']));
    setTell('low', bool(['truck.lightsBeamLowOn', 'truck.lightsLowOn']));
    setTell('high', bool(['truck.lightsBeamHighOn', 'truck.lightsHighOn']));
    setTell('fogf', bool(['truck.lightsFogOn', 'truck.lightsFrontFogOn']));
    setTell('fogr', bool(['truck.lightsRearFogOn']));
    setTell('beacon', bool(['truck.lightsBeaconOn', 'truck.beaconOn']));
    setTell('hazard', hazard);
    setTell('park', bool(['truck.parkBrakeOn', 'truck.parkingBrakeOn']));
    setTell('diff', bool(['truck.differentialLockOn', 'truck.diffLockOn']));
    setTell('ebrake', bool(['truck.motorBrakeOn', 'truck.engineBrakeOn']));
    setTell('ret', typeof ret === 'number' && ret > 0);
    setTell('lift', bool(['truck.liftAxleOn', 'truck.truckLiftAxleOn']));
    setTell('trailer', attached === true);
    setTell('cruise', ccOn);
    setTell('wipers', bool(['truck.wipersOn']));
    setTell('fuel', bool(['truck.fuelWarningOn']) || (pct != null && pct <= 12));
    setTell('adblue', bool(['truck.adblueWarningOn']));
    setTell('batt', bool(['truck.batteryVoltageWarningOn']));
    setTell('oil', bool(['truck.oilPressureWarningOn']) || (oil != null && oil >= 120));
    setTell('temp', bool(['truck.waterTemperatureWarningOn']) || (water != null && water >= 105));
    setTell('air', bool(['truck.airPressureWarningOn', 'truck.airPressureEmergencyOn']));

    $('.js-edge-l').toggleClass('on', blinkL);
    $('.js-edge-r').toggleClass('on', blinkR);

    $('.js-cargo').text(self._lastInfo.cargo || '--');
    $('.js-destination').text(self._lastInfo.to || '--');

    var distM = num(['navigation.estimatedDistance']);
    $('.js-dist').text(distM == null ? '--' : Math.round(toUnit(distM / 1000)));

    var epoch = new Date('0001-01-01T00:00:00Z').getTime();
    var gameTime = data.game && data.game.time;
    var gameMs = new Date(gameTime).getTime();

    var navTime = self._rv(data, 'navigation.estimatedTime');
    var navMs = new Date(navTime).getTime() - epoch;
    if (isFinite(navMs) && navMs > 0 && isFinite(gameMs)) {
        var eta = new Date(gameMs + navMs);
        $('.js-eta').text(pad(eta.getUTCHours()) + ':' + pad(eta.getUTCMinutes()));
    } else {
        $('.js-eta').text('--:--');
    }
    if (isFinite(navMs) && navMs > 0) {
        var ttgMin = Math.round(navMs / 60000);
        var ttgH = Math.floor(ttgMin / 60);
        $('.js-ttg').text(ttgH > 0 ? ttgH + 'h ' + pad(ttgMin % 60) + 'm' : (ttgMin % 60) + 'm');
    } else {
        $('.js-ttg').text('--');
    }

    var jobRemaining = self._rv(data, 'job.remainingTime');
    var jobMs = new Date(jobRemaining).getTime() - epoch;
    if (isFinite(jobMs) && jobMs > 0) {
        var jm = Math.round(jobMs / 60000);
        var jh = Math.floor(jm / 60);
        $('.js-deadline').text(jh > 0 ? jh + 'h ' + pad(jm % 60) + 'm' : (jm % 60) + 'm');
    } else {
        var deadlineTime = self._rv(data, 'job.deadlineTime');
        var deadlineMs = new Date(deadlineTime).getTime() - epoch;
        if (isFinite(deadlineMs) && deadlineMs > 0 && isFinite(gameMs)) {
            var diffMs = deadlineMs - gameMs;
            if (diffMs > 0) {
                var dm = Math.round(diffMs / 60000);
                var dh = Math.floor(dm / 60);
                $('.js-deadline').text(dh > 0 ? dh + 'h ' + pad(dm % 60) + 'm' : (dm % 60) + 'm');
            } else {
                $('.js-deadline').text('LATE');
            }
        } else {
            $('.js-deadline').text('--');
        }
    }

    $('.js-rest').text(Funbit.Ets.Telemetry.Dashboard.formatRestRemaining(
        self._rv(data, 'game.nextRestStopTime'), data.game));

    if (isFinite(gameMs)) {
        var t = new Date(gameMs);
        $('.js-time').text(pad(t.getUTCHours()) + ':' + pad(t.getUTCMinutes()));
    }

    var limit = num(['navigation.speedLimit']);
    if (limit > 0) {
        $('.js-speedLimit').text(Math.round(toUnit(limit)));
        $('.js-speedLimit').parent().show();
    } else {
        $('.js-speedLimit').text('');
        $('.js-speedLimit').parent().hide();
    }

    var gameName = (data.game && data.game.gameName) ? data.game.gameName : 'ETS2';
    var posX = num(['truck.placement.x', 'truck.placement.X', 'truck.coordinateX']);
    var posZ = num(['truck.placement.z', 'truck.placement.Z', 'truck.coordinateZ']);
    var countryInfo = null;
    if (window.RcCountry) {
        var motion = null;
        if (isFinite(posX) && isFinite(posZ)) {
            motion = { prevX: self._countryPos.x, prevZ: self._countryPos.z };
            self._countryPos.x = posX;
            self._countryPos.z = posZ;
        }
        countryInfo = window.RcCountry.resolveCountry(gameName, posX, posZ, self._countriesEts2, {
            id: str(['truck.licensePlateCountryId']),
            name: str(['truck.licensePlateCountry'])
        }, motion);
    }
    var $country = $('.js-country');
    if (countryInfo && countryInfo.name) {
        var cKey = countryInfo.token + '|' + countryInfo.name;
        if (cKey !== self._countryKey) {
            self._countryKey = cKey;
            $('.js-country-flag').text(countryInfo.flag || '');
            $('.js-country-name').text(countryInfo.name);
            $country.addClass('country-flash');
            setTimeout(function () { $country.removeClass('country-flash'); }, 400);
        }
        $country.prop('hidden', false);
    } else {
        self._countryKey = '';
        $country.prop('hidden', true);
    }

    return data;
};

Funbit.Ets.Telemetry.Dashboard.prototype.render = function () {};
