var Funbit;
(function (Funbit) {
    (function (Ets) {
        (function (Telemetry) {
            var serverPort = 25555;
            var rencloudBridgePort = 25557;
            var LS_TELEMETRY_PORT = 'tcd.telemetryPort';
            var LS_RENCLOUD_BRIDGE_PORT = 'tcd.rencloudBridgePort';
            var LEGACY_TRUCKSIM_GPS_PORT = 31377;

            var Strings = (function () {
                function Strings() {
                }
                Strings.dashboardHtmlLoadFailed = 'Failed to load dashboard.html for skin: ';

                Strings.connecting = 'Connecting...';
                Strings.connected = 'Connected';
                Strings.disconnected = 'Disconnected';
                Strings.enterServerIpMessage = 'Please enter server IP address (aa.bb.cc.dd)';
                Strings.incorrectServerIpFormat = 'Entered server IP or hostname has incorrect format.';

                Strings.dayOfTheWeek = [
                    'Sunday',
                    'Monday',
                    'Tuesday',
                    'Wednesday',
                    'Thursday',
                    'Friday',
                    'Saturday'
                ];
                Strings.noTimeLeft = 'Overdue';
                Strings.disconnectedFromServer = 'Disconnected from server';
                Strings.couldNotConnectToServer = 'Could not connect to the server';
                Strings.connectedAndWaitingForDrive = 'Connected, waiting for the drive...';
                Strings.connectingToServer = 'Connecting to the server...';
                return Strings;
            })();
            Telemetry.Strings = Strings;

            var Configuration = (function () {
                function Configuration() {
                    var _this = this;
                    this.anticacheSeed = Date.now();
                    this.initialized = $.Deferred();
                    this.skins = [];
                    this.serverVersion = '';

                    if (!Configuration.isCordovaAvailable()) {
                        this.insomnia = {
                            keepAwake: function () {
                            }
                        };
                        this.prefs = {
                            fetch: function () {
                            },
                            store: function () {
                            }
                        };
                    } else {
                        this.insomnia = plugins.insomnia;
                        this.prefs = plugins.appPreferences;

                        this.insomnia.keepAwake();
                    }

                    var ip = Configuration.getParameter('ip');
                    if (ip) {
                        this.serverIp = ip;
                        this.initialize();
                        return;
                    }
                    this.serverIp = '';
                    if (!Configuration.isCordovaAvailable()) {
                        this.serverIp = window.location.hostname;
                        this.initialize();
                    } else {
                        this.prefs.fetch(function (savedIp) {
                            _this.serverIp = savedIp;
                            _this.initialize();
                        }, function () {
                            _this.initialize();
                        }, 'serverIp');
                    }
                }
                Configuration.getInstance = function () {
                    if (!Configuration.instance) {
                        Configuration.instance = new Configuration();
                    }
                    return Configuration.instance;
                };

                Configuration.getParameter = function (name) {
                    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
                    var regex = new RegExp("[\\?&]" + name + "=([^&#]*)");
                    var results = regex.exec(location.search);
                    return results == null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
                };

                Configuration.getTelemetryPort = function () {
                    var qp = Configuration.getParameter('telemetryPort');
                    if (qp) {
                        var fromQuery = parseInt(qp, 10);
                        if (fromQuery > 0) return fromQuery;
                    }
                    var saved = localStorage.getItem(LS_TELEMETRY_PORT);
                    if (saved) {
                        var p = parseInt(saved, 10);
                        if (p > 0) return p;
                    }
                    return serverPort;
                };

                Configuration.prototype.getTelemetryUrlInternal = function (path) {
                    return "http://" + this.serverIp + ":" + Configuration.getTelemetryPort() + path;
                };

                Configuration.getTelemetryUrl = function (path) {
                    return Configuration.getInstance().getTelemetryUrlInternal(path);
                };

                Configuration.getRencloudBridgePort = function () {
                    var qp = Configuration.getParameter('rencloudBridgePort');
                    if (qp) {
                        var fromQuery = parseInt(qp, 10);
                        if (fromQuery > 0) return fromQuery;
                    }
                    var saved = localStorage.getItem(LS_RENCLOUD_BRIDGE_PORT);
                    if (saved) {
                        var p = parseInt(saved, 10);
                        if (p > 0) return p;
                    }
                    return rencloudBridgePort;
                };

                Configuration.getRencloudBridgeUrl = function (path) {
                    var host = Configuration.getInstance().serverIp || window.location.hostname || 'localhost';
                    return 'http://' + host + ':' + Configuration.getRencloudBridgePort() + path;
                };

                /** Resolve telemetry port — always Funbit (:25555) unless overridden in URL/storage. */
                Configuration.prototype.detectTelemetryPort = function (done) {
                    var qp = Configuration.getParameter('telemetryPort');
                    if (qp) {
                        var fromQuery = parseInt(qp, 10);
                        if (fromQuery > 0 && fromQuery !== LEGACY_TRUCKSIM_GPS_PORT) {
                            done(fromQuery);
                            return;
                        }
                    }
                    var saved = localStorage.getItem(LS_TELEMETRY_PORT);
                    if (saved) {
                        var p = parseInt(saved, 10);
                        if (p === LEGACY_TRUCKSIM_GPS_PORT) {
                            try { localStorage.removeItem(LS_TELEMETRY_PORT); } catch (e) { /* ignore */ }
                        } else if (p > 0) {
                            done(p);
                            return;
                        }
                    }
                    done(serverPort);
                };

                Configuration.prototype.getSkinConfiguration = function () {
                    var skinName = Configuration.getParameter('skin');
                    if (skinName) {
                        for (var i = 0; i < this.skins.length; i++) {
                            if (this.skins[i].name == skinName)
                                return this.skins[i];
                        }
                    }
                    for (var j = 0; j < this.skins.length; j++) {
                        if (this.skins[j].name === 'truckdeck_nav')
                            return this.skins[j];
                    }
                    return this.skins.length ? this.skins[0] : null;
                };

                Configuration.prototype.reload = function (newServerIp, done, fail) {
                    var _this = this;
                    if (typeof done === "undefined") { done = null; }
                    if (typeof fail === "undefined") { fail = null; }
                    if (!newServerIp)
                        return;
                    this.serverIp = newServerIp;
                    this.prefs.store(function () {
                    }, function () {
                    }, 'serverIp', this.serverIp);
                    $.ajax({
                        url: this.getUrlInternal('/config.json?seed=' + this.anticacheSeed++),
                        dataType: 'json',
                        timeout: 3000
                    }).done(function (json) {
                        _this.skins = json.skins;
                        if (json.version)
                            _this.serverVersion = json.version;
                        Configuration.applyVersionLabels(_this.serverVersion);
                        if (done)
                            done();
                    }).fail(function () {
                        _this.skins = [];
                        if (fail)
                            fail();
                    });
                };

                Configuration.isCordovaAvailable = function () {
                    return document.URL.indexOf('http://') === -1 && document.URL.indexOf('https://') === -1;
                };

                Configuration.prototype.getUrlInternal = function (path) {
                    return "http://" + this.serverIp + ":" + serverPort + path;
                };

                Configuration.getUrl = function (path) {
                    return Configuration.getInstance().getUrlInternal(path);
                };

                Configuration.applyVersionLabels = function (version) {
                    if (!version)
                        return;
                    var nodes = document.querySelectorAll('.js-app-version');
                    for (var i = 0; i < nodes.length; i++)
                        nodes[i].textContent = version;
                };

                Configuration.prototype.getSkinResourceUrl = function (skinConfig, name) {
                    return Configuration.getUrl('/skins/' + skinConfig.name + '/' + name + '?seed=' + this.anticacheSeed++);
                };

                Configuration.prototype.initialize = function () {
                    var _this = this;
                    if (!this.serverIp)
                        this.initialized.resolve(this);
                    this.reload(this.serverIp, function () {
                        return _this.initialized.resolve(_this);
                    }, function () {
                        return _this.initialized.resolve(_this);
                    });
                };
                return Configuration;
            })();
            Telemetry.Configuration = Configuration;
            Telemetry.Configuration.serverPort = serverPort;
            Telemetry.Configuration.rencloudBridgePort = rencloudBridgePort;
        })(Ets.Telemetry || (Ets.Telemetry = {}));
        var Telemetry = Ets.Telemetry;
    })(Funbit.Ets || (Funbit.Ets = {}));
    var Ets = Funbit.Ets;
})(Funbit || (Funbit = {}));
