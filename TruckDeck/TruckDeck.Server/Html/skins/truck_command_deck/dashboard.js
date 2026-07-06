/*
    "Trucker's Command Deck" skin logic.
    - Sends control commands to the local Input Bridge (the Telemetry Server is
      read-only). See /input_bridge.
    - Updates all gauges and button states from telemetry. State updates run in
      filter() (called on every telemetry push) so they work even if the core
      render loop hiccups, and they reflect real telemetry where the server
      provides it - otherwise the button toggles locally on tap.
*/

Funbit.Ets.Telemetry.Dashboard.prototype.initialize = function (skinConfig, utils) {
    var self = this;

    // ---------------------------------------------------------------------
    // Input Bridge (port 25556): turns button presses into PC key strokes.
    // ---------------------------------------------------------------------
    var BRIDGE_PORT = 25556;
    var bridgeHost = function () {
        return localStorage.getItem('tcd.bridgeHost')
            || window.location.hostname
            || '127.0.0.1';
    };
    var bridgeWarn = function (msg) {
        if (utils._bridgeWarned) return;
        utils._bridgeWarned = true;
        console.warn('[Input Bridge] ' + msg);
        var $w = $('.js-pcm-warn');
        if ($w.length) { $w.text(msg).removeAttr('hidden'); }
    };
    var bridgePost = function (path, body) {
        var url = 'http://' + bridgeHost() + ':' + BRIDGE_PORT + path;
        var opts = { method: 'POST', mode: 'cors' };
        if (body !== undefined) {
            opts.headers = { 'Content-Type': 'application/json' };
            opts.body = JSON.stringify(body);
        }
        try {
            fetch(url, opts).then(function (res) {
                if (res.ok) return;
                if (path.indexOf('/api/mouse/') === 0 && res.status === 404) {
                    bridgeWarn('Mouse API missing — close the old bridge window and run start_bridge.bat again.');
                } else if (!utils._bridgeWarned) {
                    bridgeWarn('not reachable on :' + BRIDGE_PORT + ' — run Html\\input_bridge\\start_bridge.bat');
                }
            })['catch'](function () {
                bridgeWarn('not reachable on :' + BRIDGE_PORT + ' — run Html\\input_bridge\\start_bridge.bat');
            });
        } catch (e) {
            var xhr = new XMLHttpRequest();
            xhr.open('POST', url, true);
            if (body !== undefined) {
                xhr.setRequestHeader('Content-Type', 'application/json');
                xhr.send(JSON.stringify(body));
            } else {
                xhr.send();
            }
        }
    };
    utils._bridgeWarned = false;
    if (typeof utils.triggerGameAction !== 'function') {
        utils.triggerGameAction = function (action) {
            bridgePost('/api/command/' + action);
        };
    }
    utils.bridgeMouseMove = function (dx, dy) {
        bridgePost('/api/mouse/move', { dx: dx, dy: dy });
    };
    utils.bridgeMouseClick = function (button, state) {
        bridgePost('/api/mouse/click', { button: button, state: state });
    };
    utils.bridgeMouseScroll = function (delta) {
        bridgePost('/api/mouse/scroll', { delta: delta });
    };

    function checkBridgeMouse() {
        var url = 'http://' + bridgeHost() + ':' + BRIDGE_PORT + '/health';
        fetch(url, { mode: 'cors' })['catch'](function () { return null; })
            .then(function (res) { return res && res.ok ? res.json() : null; })
            .then(function (data) {
                if (!data) return;
                if (data.mouse !== true) {
                    bridgeWarn('Mouse API missing — close the old bridge window and run start_bridge.bat again.');
                } else {
                    $('.js-pcm-warn').attr('hidden', 'hidden');
                }
            });
    }

    // ---- In-skin page navigation (deck <-> PC mouse / NAV) ----
    var TCD_PAGES = {
        deck: 'tcd-page-deck',
        pcmouse: 'tcd-page-pcmouse',
        nav: 'tcd-page-nav'
    };

    function buildNavIframeSrc() {
        var qs = 'skin=truckdeck_nav&seed=' + Date.now();
        try {
            var C = Funbit.Ets.Telemetry.Configuration;
            if (C && typeof C.getUrl === 'function') {
                return C.getUrl('/dashboard-host.html?' + qs);
            }
        } catch (e) { /* ignore */ }
        var path = window.location.pathname || '';
        var base = path.replace(/\/[^/]*$/, '') || '';
        var extra = window.location.search ? window.location.search.replace(/^\?/, '&') : '';
        return base + '/dashboard-host.html?' + qs + extra;
    }

    function ensureNavIframe() {
        var frame = document.querySelector('.js-nav-frame');
        if (!frame || frame.getAttribute('src')) return;
        frame.setAttribute('src', buildNavIframeSrc());
    }

    function showTcdPage(name) {
        var pageClass = TCD_PAGES[name] || TCD_PAGES.deck;
        $('.tcd-page').attr('hidden', 'hidden');
        $('.' + pageClass).removeAttr('hidden');
        if (name === 'pcmouse') {
            utils._bridgeWarned = false;
            checkBridgeMouse();
        }
        if (name === 'nav') {
            ensureNavIframe();
        }
    }
    $(document).on('click', '.nav-btn[data-page]', function () {
        var page = $(this).data('page');
        if (page) showTcdPage(page);
    });

    // ---- Colour scheme + day/night (same schema as the other dashboards,
    //      sharing the rc.accent / rc.mode keys so the look stays consistent) ----
    this.accents = [
        ['lime', 'LIME'], ['amber', 'AMBER'], ['red', 'RED'], ['blue', 'BLUE'],
        ['green', 'GREEN'], ['ice', 'ICE'], ['violet', 'VIOLET']
    ];
    this.mode = (localStorage.getItem('rc.mode') === 'day') ? 'day' : 'night';
    this.accentIdx = 0;
    var savedAccent = localStorage.getItem('rc.accent');
    for (var ai = 0; ai < this.accents.length; ai++) {
        if (this.accents[ai][0] === savedAccent) { this.accentIdx = ai; break; }
    }
    function applyTheme() {
        var acc = self.accents[self.accentIdx];
        var root = document.querySelector('.dashboard.tcd');
        if (root) {
            root.setAttribute('data-mode', self.mode);
            root.setAttribute('data-accent', acc[0]);
        }
        $('.js-theme-label').text(acc[1]);
        $('.js-mode-label').text(self.mode === 'day' ? 'DAY' : 'NIGHT');
    }
    applyTheme();
    $(document).on('click', '.js-theme', function () {
        self.accentIdx = (self.accentIdx + 1) % self.accents.length;
        localStorage.setItem('rc.accent', self.accents[self.accentIdx][0]);
        applyTheme();
    });
    $(document).on('click', '.js-mode', function () {
        self.mode = (self.mode === 'day') ? 'night' : 'day';
        localStorage.setItem('rc.mode', self.mode);
        applyTheme();
    });

    // --- Screen Wake Lock ---
    this.wakeLock = null;
    this.requestWakeLock = async function () {
        if ('wakeLock' in navigator) {
            try { self.wakeLock = await navigator.wakeLock.request('screen'); } catch (e) { /* ignore */ }
        }
    };
    this.requestWakeLock();
    document.addEventListener('visibilitychange', async function () {
        if (self.wakeLock !== null && document.visibilityState === 'visible') {
            await self.requestWakeLock();
        }
    });

    // Which toggle buttons are currently driven by real telemetry. Buttons not
    // found in telemetry fall back to local (tap-to-toggle) feedback.
    this.telemetryDriven = {};
    var toggleCommands = ['engine', 'highBeam', 'beacons', 'hazards',
        'wipers', 'diffLock', 'liftAxle', 'trailerLiftAxle', 'light',
        'parkingBrake', 'radio', 'cruiseControl'];

    // Auto engine brake toggle (OFF / AUTO) — no reliable telemetry for toggle state.
    this.engBrakeAuto = localStorage.getItem('tcd.engBrakeAuto') === '1';
    function engBrakeUI() {
        // Local flag is inverse of in-game auto engine brake (toggle key sync).
        var on = !self.engBrakeAuto;
        $('[data-command="engineBrake"]').toggleClass('active', on);
        $('.js-eng-brake').text(on ? 'AUTO' : 'OFF');
    }
    engBrakeUI();

    // Auto retarder toggle (OFF / AUTO) — same cycle style as engine brake.
    this.retarderAuto = localStorage.getItem('tcd.retarderAuto') === '1';
    function retarderUI() {
        var on = self.retarderAuto;
        $('[data-command="autoRetarder"]').toggleClass('active', on);
        $('.js-retarder').text(on ? 'AUTO' : 'OFF');
    }
    retarderUI();

    // ---- Auto blinker cancel ----
    // ETS2/ATS don't self-cancel turn signals, so once a single blinker is on
    // and the wheel has been turned, we send the blinker key again to switch it
    // off as the wheel returns near centre (like a real steering column).
    this.autoBlink = localStorage.getItem('tcd.autoBlink') !== '0';   // default ON
    this._ab = { side: null, armed: false };
    $('[data-command="autoBlink"]').toggleClass('active', this.autoBlink);

    // ---- Fuel economy trip recorder ----
    // Completed sessions are stored in localStorage ('rc.fuelLog', shared with
    // fuel-economy.html on the same origin) for that page to read and export.
    // A live (in-progress) session is mirrored to 'tcd.econLive' so it survives
    // page reloads / the game being switched off - it only ends when you press
    // STOP, and can be PAUSEd in between.
    this.econUnit = (Funbit.Ets.Telemetry.Dashboard.getSpeedUnit() === 'mph') ? 'imperial' : 'metric';
    this.econ = { active: false, paused: false, distKm: 0, fuelL: 0, lastOdo: null, lastFuel: null, lastT: null, startISO: null, info: {} };
    this._lastJobActive = false;
    (function restoreLive() {
        try {
            var saved = JSON.parse(localStorage.getItem('tcd.econLive') || 'null');
            if (saved && saved.active) {
                var ec = self.econ;
                ec.active = true;
                ec.paused = !!saved.paused;
                ec.distKm = saved.distKm || 0;
                ec.fuelL = saved.fuelL || 0;
                ec.startISO = saved.startISO || new Date().toISOString();
                ec.info = saved.info || {};
                // Reset deltas so resuming after a reload / game-off never counts
                // a jump - the next telemetry frame just re-establishes baselines.
                ec.lastOdo = null; ec.lastFuel = null; ec.lastT = Date.now();
            }
        } catch (e) { /* ignore corrupt state */ }
    })();

    function econUI() {
        var ec = self.econ;
        $('.js-econ-toggle').toggleClass('running', ec.active);
        $('.js-econ-label').text(ec.active ? 'STOP ECON' : 'START ECON');
        var $p = $('.js-econ-pause');
        if (ec.active) { $p.removeAttr('hidden'); } else { $p.attr('hidden', 'hidden'); }
        $p.toggleClass('paused', ec.paused);
        $('.js-econ-pause-label').text(ec.paused ? 'RESUME' : 'PAUSE');
    }
    function econUnitUI() {
        $('.js-econ-unit-label').text(self.econUnit === 'imperial' ? 'MI' : 'KM');
    }
    econUI();
    econUnitUI();
    this._updateEconUI = econUI;

    $(document).on('click', '.js-econ-toggle', function () {
        if (!self.econ.active) {
            self._startEcon();
        } else {
            self._stopEcon();
        }
    });

    // PAUSE / RESUME: keeps the running totals but stops accumulating.
    $(document).on('click', '.js-econ-pause', function () {
        var ec = self.econ;
        if (!ec.active) return;
        ec.paused = !ec.paused;
        if (!ec.paused) { ec.lastOdo = null; ec.lastFuel = null; ec.lastT = Date.now(); }
        self._saveEconLive();
        econUI();
    });

    // Tap the unit button (or the read-out) to cycle KM <-> MI for the econ.
    $(document).on('click', '.js-econ-unit, .js-econ-units', function () {
        self.econUnit = (self.econUnit === 'imperial') ? 'metric' : 'imperial';
        Funbit.Ets.Telemetry.Dashboard.setSpeedUnit(self.econUnit === 'imperial' ? 'mph' : 'kmh');
        econUnitUI();
        self._renderEconomy();
    });

    // ---- Press-and-hold repeat (radio volume) ----
    // Volume +/- fire once on tap and then auto-repeat while the button is held,
    // so you can ramp the radio up/down without machine-gun tapping.
    this._holdTimer = null;
    var stopHold = function () {
        if (self._holdTimer) { clearInterval(self._holdTimer); self._holdTimer = null; }
        $('.hold-btn').removeClass('flash');
    };
    $(document).on('pointerdown', '.hold-btn', function (e) {
        e.preventDefault();
        var $btn = $(this);
        if ($btn.hasClass('js-pcm-scroll')) {
            var delta = parseInt($btn.data('delta'), 10);
            if (!delta) return;
            stopHold();
            $btn.addClass('flash');
            utils.bridgeMouseScroll(delta);
            self._holdTimer = setInterval(function () { utils.bridgeMouseScroll(delta); }, 160);
            return;
        }
        var cmd = $btn.data('command');
        if (!cmd) return;
        stopHold();
        $btn.addClass('flash');
        utils.triggerGameAction(cmd);
        self._holdTimer = setInterval(function () { utils.triggerGameAction(cmd); }, 160);
    });
    $(document).on('pointerup pointercancel pointerleave', '.hold-btn', stopHold);
    $(window).on('pointerup pointercancel blur', stopHold);

    $(document).on('click', '.control-btn', function () {
        var $btn = $(this);
        if ($btn.hasClass('nav-btn') || $btn.hasClass('js-pcm-click')) return;
        var cmd = $btn.data('command');
        if (!cmd || cmd === 'cruise') return;
        // Hold-to-repeat buttons (radio volume) are driven by pointer events.
        if ($btn.hasClass('hold-btn')) return;

        // AUTO BLINK is a local setting, not a game key - toggle and stop here.
        if (cmd === 'autoBlink') {
            self.autoBlink = !self.autoBlink;
            localStorage.setItem('tcd.autoBlink', self.autoBlink ? '1' : '0');
            $btn.toggleClass('active', self.autoBlink);
            if (!self.autoBlink) self._ab = { side: null, armed: false };
            return;
        }

        // Engine brake: cycle OFF <-> AUTO (bind to Engine Brake Toggle in-game).
        if (cmd === 'engineBrake') {
            self.engBrakeAuto = !self.engBrakeAuto;
            localStorage.setItem('tcd.engBrakeAuto', self.engBrakeAuto ? '1' : '0');
            engBrakeUI();
            utils.triggerGameAction(cmd);
            return;
        }

        // Retarder: cycle OFF <-> AUTO (bind to Automatic Retarder in-game).
        if (cmd === 'autoRetarder') {
            self.retarderAuto = !self.retarderAuto;
            localStorage.setItem('tcd.retarderAuto', self.retarderAuto ? '1' : '0');
            retarderUI();
            utils.triggerGameAction(cmd);
            return;
        }

        if ($btn.hasClass('momentary')) {
            $btn.addClass('flash');
            setTimeout(function () { $btn.removeClass('flash'); }, 180);
        }

        // Local toggle feedback for buttons the server doesn't report.
        if (toggleCommands.indexOf(cmd) !== -1 && !self.telemetryDriven[cmd]) {
            $btn.toggleClass('active');
        }

        utils.triggerGameAction(cmd);
    });

    // Hotkeys
    $(document).on('keydown', function (e) {
        // Ctrl + ' (quote) -> Radio Star / Favorite
        if (e.ctrlKey && (e.key === "'" || e.keyCode === 222)) {
            e.preventDefault();
            $('[data-command="radioStar"]').trigger('click');
        }
    });

    // ---- PC Mouse touchpad ----
    var PCM_SENS = 2.2;
    var pcmPad = { active: false, lastX: 0, lastY: 0, raf: 0, pendingDx: 0, pendingDy: 0 };

    function flushPadMove() {
        pcmPad.raf = 0;
        if (!pcmPad.pendingDx && !pcmPad.pendingDy) return;
        utils.bridgeMouseMove(pcmPad.pendingDx, pcmPad.pendingDy);
        pcmPad.pendingDx = 0;
        pcmPad.pendingDy = 0;
    }

    function onPadMove(e) {
        if (!pcmPad.active) return;
        e.preventDefault();
        var dx = Math.round((e.clientX - pcmPad.lastX) * PCM_SENS);
        var dy = Math.round((e.clientY - pcmPad.lastY) * PCM_SENS);
        pcmPad.lastX = e.clientX;
        pcmPad.lastY = e.clientY;
        if (!dx && !dy) return;
        pcmPad.pendingDx += dx;
        pcmPad.pendingDy += dy;
        if (!pcmPad.raf) {
            pcmPad.raf = requestAnimationFrame(flushPadMove);
        }
    }

    function detachPadTrack() {
        $(document).off('pointermove.pcm', onPadMove);
        $(document).off('pointerup.pcm pointercancel.pcm', onPadEnd);
    }

    function endPad(e) {
        if (!pcmPad.active) return;
        pcmPad.active = false;
        detachPadTrack();
        if (pcmPad.raf) { cancelAnimationFrame(pcmPad.raf); pcmPad.raf = 0; }
        flushPadMove();
        var el = $('.js-pcm-pad')[0];
        if (el && el.releasePointerCapture && e && e.pointerId != null) {
            try { el.releasePointerCapture(e.pointerId); } catch (err) { /* ignore */ }
        }
    }

    function onPadEnd(e) {
        endPad(e);
    }

    $(document).on('pointerdown', '.js-pcm-pad', function (e) {
        e.preventDefault();
        var el = this;
        pcmPad.active = true;
        pcmPad.lastX = e.clientX;
        pcmPad.lastY = e.clientY;
        detachPadTrack();
        $(document).on('pointermove.pcm', onPadMove);
        $(document).on('pointerup.pcm pointercancel.pcm', onPadEnd);
        if (el.setPointerCapture) {
            try { el.setPointerCapture(e.pointerId); } catch (err) { /* ignore */ }
        }
    });

    // ---- PC Mouse click buttons (hold for drag) ----
    $(document).on('pointerdown', '.js-pcm-click', function (e) {
        e.preventDefault();
        var btn = $(this).data('button');
        if (!btn) return;
        $(this).addClass('flash active');
        utils.bridgeMouseClick(btn, 'down');
    });
    function endPcmClick(e) {
        $('.js-pcm-click.active').each(function () {
            var btn = $(this).data('button');
            if (btn) utils.bridgeMouseClick(btn, 'up');
            $(this).removeClass('flash active');
        });
    }
    $(document).on('pointerup pointercancel', '.js-pcm-click', endPcmClick);
    $(window).on('pointerup pointercancel blur', endPcmClick);

};

// Resolve a dotted path against an object, returning undefined if missing.
Funbit.Ets.Telemetry.Dashboard.prototype._rv = function (obj, path) {
    return path.split('.').reduce(function (p, c) { return (p == null) ? undefined : p[c]; }, obj);
};

// First finite number found among the candidate paths.
Funbit.Ets.Telemetry.Dashboard.prototype._num = function (data, paths) {
    for (var i = 0; i < paths.length; i++) {
        var v = this._rv(data, paths[i]);
        if (typeof v === 'number' && isFinite(v)) return v;
    }
    return null;
};

// First boolean found among the candidate paths.
Funbit.Ets.Telemetry.Dashboard.prototype._bool = function (data, paths) {
    for (var i = 0; i < paths.length; i++) {
        var v = this._rv(data, paths[i]);
        if (typeof v === 'boolean') return v;
    }
    return false;
};

// First non-empty string found among the candidate paths.
Funbit.Ets.Telemetry.Dashboard.prototype._str = function (data, paths) {
    for (var i = 0; i < paths.length; i++) {
        var v = this._rv(data, paths[i]);
        if (typeof v === 'string' && v) return v;
    }
    return '';
};

// Append the finished economy session to localStorage ('rc.fuelLog').
Funbit.Ets.Telemetry.Dashboard.prototype._saveEconomySession = function () {
    var ec = this.econ;
    if (ec.distKm <= 0 && ec.fuelL <= 0) return; // nothing meaningful to log
    try {
        var log = JSON.parse(localStorage.getItem('rc.fuelLog') || '[]');
        if (!Array.isArray(log)) log = [];
        var info = ec.info || {};
        log.push({
            start: ec.startISO,
            end: new Date().toISOString(),
            distanceKm: +ec.distKm.toFixed(3),
            fuelL: +ec.fuelL.toFixed(3),
            from: info.from || '',
            to: info.to || '',
            cargo: info.cargo || '',
            massKg: (info.massKg != null ? info.massKg : null)
        });
        localStorage.setItem('rc.fuelLog', JSON.stringify(log));
    } catch (e) { console.error('Could not save economy session:', e); }
};

// Persist the in-progress session so it survives a reload / the game closing.
Funbit.Ets.Telemetry.Dashboard.prototype._saveEconLive = function () {
    var ec = this.econ;
    try {
        if (!ec.active) { localStorage.removeItem('tcd.econLive'); return; }
        localStorage.setItem('tcd.econLive', JSON.stringify({
            active: ec.active, paused: ec.paused,
            distKm: ec.distKm, fuelL: ec.fuelL,
            startISO: ec.startISO, info: ec.info
        }));
    } catch (e) { /* ignore */ }
};
Funbit.Ets.Telemetry.Dashboard.prototype._clearEconLive = function () {
    try { localStorage.removeItem('tcd.econLive'); } catch (e) { /* ignore */ }
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
    if (this._updateEconUI) this._updateEconUI();
};

Funbit.Ets.Telemetry.Dashboard.prototype._stopEcon = function () {
    var ec = this.econ;
    if (!ec.active) return;
    ec.active = false; ec.paused = false;
    this._saveEconomySession();
    this._clearEconLive();
    if (this._updateEconUI) this._updateEconUI();
};

// Update the live economy read-out (metric: km/L/L-per-100km, imperial: mi/gal/MPG).
Funbit.Ets.Telemetry.Dashboard.prototype._renderEconomy = function () {
    var ec = this.econ;
    var imp = this.econUnit === 'imperial';
    var KM2MI = 0.621371, L2GAL = 0.264172;   // US gallon for MPG
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
    var rv = this._rv;
    var num = function (paths) { return self._num(data, paths); };
    var bool = function (paths) { return self._bool(data, paths); };
    var str = function (paths) { return self._str(data, paths); };

    var truck = data.truck || {};

    // Snapshot of the current job/cargo, captured when an economy session starts.
    self._lastInfo = {
        from: str(['job.sourceCity', 'job.citySrc', 'job.cargoSourceCity']),
        to: str(['job.destinationCity', 'job.cityDst', 'job.cargoDestinationCity']),
        cargo: str(['cargo.cargo', 'job.cargo', 'job.cargoName', 'cargo.name']),
        massKg: num(['cargo.mass', 'job.mass', 'cargo.cargoMass'])
    };

    // ---- Auto Econ Start/Stop ----
    // Detect new job (destination city becomes non-empty) or job end.
    var jobActive = !!self._lastInfo.to;
    if (jobActive && !self._lastJobActive) {
        self._startEcon();
    } else if (!jobActive && self._lastJobActive) {
        self._stopEcon();
    }
    // Also stop if the server explicitly reports jobFinished event.
    if (bool(['jobEvent.jobFinished', 'jobEvent.jobDelivered', 'jobEvent.jobCancelled'])) {
        self._stopEcon();
    }
    self._lastJobActive = jobActive;

    // ---- Auto blinker cancel ----
    // Treat full steering input as ~120 deg of wheel travel. Once a single
    // blinker is on and the wheel passes TURN_DEG, arm; when it returns under
    // CENTER_DEG, tap the blinker key again to cancel it.
    var STEER_MAX_DEG = 120, TURN_DEG = 25, CENTER_DEG = 5;
    var steerDeg = Math.abs(num(['truck.userSteer', 'truck.gameSteer']) || 0) * STEER_MAX_DEG;
    var blinkL = bool(['truck.blinkerLeftActive', 'truck.blinkerLeftOn']);
    var blinkR = bool(['truck.blinkerRightActive', 'truck.blinkerRightOn']);
    var side = (blinkL && !blinkR) ? 'left' : (blinkR && !blinkL) ? 'right' : null; // skip hazards
    var ab = self._ab || (self._ab = { side: null, armed: false });
    if (self.autoBlink && side) {
        if (ab.side !== side) { ab.side = side; ab.armed = false; }
        if (steerDeg >= TURN_DEG) {
            ab.armed = true;
        } else if (ab.armed && steerDeg <= CENTER_DEG) {
            utils.triggerGameAction(side === 'left' ? 'blinkerLeft' : 'blinkerRight');
            ab.side = null; ab.armed = false;
        }
    } else {
        ab.side = null; ab.armed = false;
    }

    // ---- Toggle button states (telemetry where available) ----
    // Each command maps to candidate telemetry field paths (different server
    // builds use slightly different names).
    var toggleMap = {
        engine: ['truck.engineOn', 'truck.electricOn'],
        highBeam: ['truck.lightsBeamHighOn', 'truck.lightsHighOn', 'truck.lightsHighBeamOn'],
        beacons: ['truck.lightsBeaconOn', 'truck.beaconOn'],
        hazards: ['truck.lightsHazardOn', 'truck.hazardWarningOn', 'truck.blinkersHazardOn'],
        wipers: ['truck.wipersOn'],
        diffLock: ['truck.differentialLockOn', 'truck.diffLockOn'],
        liftAxle: ['truck.liftAxleOn', 'truck.truckLiftAxleOn'],
        trailerLiftAxle: ['truck.trailerLiftAxleOn'],
        parkingBrake: ['truck.parkBrakeOn', 'truck.parkingBrakeOn'],
        cruiseControl: ['truck.cruiseControlOn']
    };

    var setActive = function (cmd, on) {
        $('[data-command="' + cmd + '"]').toggleClass('active', !!on);
    };

    Object.keys(toggleMap).forEach(function (cmd) {
        var paths = toggleMap[cmd];
        var found = false, val = false;
        for (var i = 0; i < paths.length; i++) {
            var v = rv(data, paths[i]);
            if (typeof v === 'boolean') { found = true; val = v; break; }
        }
        self.telemetryDriven[cmd] = found;
        if (found) setActive(cmd, val);
    });

    // Headlights (composite of low/parking/high) + label
    var low = rv(data, 'truck.lightsBeamLowOn');
    if (typeof low !== 'boolean') low = rv(data, 'truck.lightsLowOn');
    var parking = rv(data, 'truck.lightsParkingOn');
    var high = rv(data, 'truck.lightsBeamHighOn');
    if (typeof high !== 'boolean') high = rv(data, 'truck.lightsHighOn');
    var anyLight = (typeof low === 'boolean' || typeof parking === 'boolean');
    self.telemetryDriven.light = anyLight;
    if (anyLight) {
        setActive('light', low || parking || high);
        var label = 'OFF';
        if (high) label = 'HIGH';
        else if (low) label = 'LOW BEAM';
        else if (parking) label = 'PARKING';
        $('.light-state').text(label);
    }

    // Wipers label
    if (typeof truck.wipersOn === 'boolean') {
        $('.js-wipers').text(truck.wipersOn ? 'ACTIVE' : 'OFF');
    }

    // Trailer attach
    var attached = rv(data, 'trailer.attached');
    if (typeof attached !== 'boolean') attached = rv(data, 'trailers.0.attached');
    if (typeof attached === 'boolean') {
        self.telemetryDriven.trailer = true;
        setActive('trailer', attached);
    }

    // ---- Fuel economy session accumulation ----
    // Runs only while a session is active and not paused; the session itself
    // persists (localStorage) so it carries over reloads / the game closing.
    var ec = self.econ;
    if (ec && ec.active && !ec.paused) {
        var nowT = Date.now();
        var kmh = Math.abs(num(['truck.speed']) || 0);
        var odo = num(['truck.odometer']);
        // Distance: prefer the odometer; ignore teleport jumps (> 50 km).
        if (odo != null) {
            if (ec.lastOdo != null && odo > ec.lastOdo && (odo - ec.lastOdo) < 50) {
                ec.distKm += odo - ec.lastOdo;
            }
            ec.lastOdo = odo;
        } else if (ec.lastT != null) {
            ec.distKm += kmh * ((nowT - ec.lastT) / 1000) / 3600;
        }
        // Fuel used: count only decreases so refuelling doesn't corrupt totals.
        var fuel = num(['truck.fuel']);
        if (fuel != null) {
            if (ec.lastFuel != null && fuel < ec.lastFuel) ec.fuelL += ec.lastFuel - fuel;
            ec.lastFuel = fuel;
        }
        ec.lastT = nowT;
        // Throttled persistence of the live session (~every 3s).
        if (!ec._lastSave || (nowT - ec._lastSave) > 3000) {
            ec._lastSave = nowT;
            self._saveEconLive();
        }
    }
    if (ec) self._renderEconomy();

    return data;
};

Funbit.Ets.Telemetry.Dashboard.prototype.render = function (data, utils) {
    // State rendering is handled in filter() for robustness.
};
