var Funbit;
(function (Funbit) {
    (function (Ets) {
        (function (Telemetry) {
            var Ets2Game = (function () {
                function Ets2Game() {
                    this.connected = false;
                    this.paused = false;
                    this.time = "";
                    this.timeScale = 0;
                    this.nextRestStopTime = "";
                    this.version = "";
                    this.telemetryPluginVersion = "";
                    this.maxTrailerCount = "";
                }
                return Ets2Game;
            })();

            var Ets2Job = (function () {
                function Ets2Job() {
                    this.income = 0;
                    this.deadlineTime = "";
                    this.remainingTime = "";
                    this.sourceCity = "";
                    this.sourceCompany = "";
                    this.destinationCity = "";
                    this.destinationCompany = "";
                    this.specialTransport = false;
                    this.jobMarket = "";
                }
                return Ets2Job;
            })();

            var Ets2Truck = (function () {
                function Ets2Truck() {
                    this.id = "";
                    this.make = "";
                    this.model = "";
                    this.speed = 0;
                    this.cruiseControlSpeed = 0;
                    this.cruiseControlOn = false;
                    this.odometer = 0;
                    this.gear = 0;
                    this.displayedGear = 0;
                    this.forwardGears = 0;
                    this.reverseGears = 0;
                    this.shifterType = "";
                    this.engineRpm = 0;
                    this.engineRpmMax = 0;
                    this.fuel = 0;
                    this.fuelCapacity = 0;
                    this.fuelAverageConsumption = 0;
                    this.fuelWarningFactor = 0;
                    this.fuelWarningOn = false;
                    this.wearEngine = 0;
                    this.wearTransmission = 0;
                    this.wearCabin = 0;
                    this.wearChassis = 0;
                    this.wearWheels = 0;
                    this.userSteer = 0;
                    this.userThrottle = 0;
                    this.userBrake = 0;
                    this.userClutch = 0;
                    this.gameSteer = 0;
                    this.gameThrottle = 0;
                    this.gameBrake = 0;
                    this.gameClutch = 0;
                    this.shifterSlot = 0;
                    this.shifterToggle = 0;
                    this.engineOn = false;
                    this.electricOn = false;
                    this.wipersOn = false;
                    this.retarderBrake = 0;
                    this.retarderStepCount = 0;
                    this.parkBrakeOn = false;
                    this.motorBrakeOn = false;
                    this.brakeTemperature = 0;
                    this.adblue = 0;
                    this.adblueCapacity = 0;
                    this.adblueAverageConsumpton = 0;
                    this.adblueWarningOn = false;
                    this.airPressure = 0;
                    this.airPressureWarningOn = false;
                    this.airPressureWarningValue = 0;
                    this.airPressureEmergencyOn = false;
                    this.airPressureEmergencyValue = 0;
                    this.oilTemperature = 0;
                    this.oilPressure = 0;
                    this.oilPressureWarningOn = false;
                    this.oilPressureWarningValue = 0;
                    this.waterTemperature = 0;
                    this.waterTemperatureWarningOn = false;
                    this.waterTemperatureWarningValue = 0;
                    this.batteryVoltage = 0;
                    this.batteryVoltageWarningOn = false;
                    this.batteryVoltageWarningValue = 0;
                    this.lightsDashboardValue = 0;
                    this.lightsDashboardOn = false;
                    this.blinkerLeftActive = false;
                    this.blinkerRightActive = false;
                    this.blinkerLeftOn = false;
                    this.blinkerRightOn = false;
                    this.lightsParkingOn = false;
                    this.lightsBeamLowOn = false;
                    this.lightsBeamHighOn = false;
                    this.lightsAuxFrontOn = false;
                    this.lightsAuxRoofOn = false;
                    this.lightsBeaconOn = false;
                    this.lightsBrakeOn = false;
                    this.lightsReverseOn = false;
                    this.placement = new Ets2Placement();
                    this.acceleration = new Ets2Vector();
                    this.head = new Ets2Vector();
                    this.cabin = new Ets2Vector();
                    this.hook = new Ets2Vector();
                    this.licensePlate = "";
                    this.licensePlateCountryId = "";
                    this.licensePlateCountry = "";
                }
                return Ets2Truck;
            })();

            var Ets2Cargo = (function () {
                function Ets2Cargo() {
                    this.cargoLoaded = false;
                    this.cargoId = "";
                    this.cargo = "";
                    this.mass = 0;
                    this.unitMass = 0;
                    this.unitCount = 0;
                    this.damage = 0;
                }
                return Ets2Cargo;
            })();

            var Ets2Trailer = (function() {
                function Ets2Trailer() {
                    this.trailerNumber = 0;
                    this.attached = false;
					this.present = false;
                    this.id = "";
                    this.name = "";
                    this.wearWheels = 0;
                    this.wearChassis = 0;
                    this.cargoDamage = 0;
                    this.cargoAccessoryId = "";
                    this.brandId = "";
                    this.brand = "";
                    this.bodyType = "";
                    this.cargo = "";
                    this.licensePlate = "";
                    this.licensePlateCountry = "";
                    this.licensePlateCountryId = "";
                    this.chainType = "";
                    this.placement = new Ets2Placement();
                }
                return Ets2Trailer;
            })();

            var Ets2Navigation = (function () {
                function Ets2Navigation() {
                    this.estimatedTime = "";
                    this.estimatedDistance = 0;
                    this.speedLimit = 0;
                }
                return Ets2Navigation;
            })();

            var Ets2FinedEvent = (function () {
                function Ets2FinedEvent() {
                    this.fineOffense = "";
                    this.fineAmount = 0;
                    this.fined = false;
                }
                return Ets2FinedEvent;
            })();

            var Ets2JobEvent = (function () {
                function Ets2JobEvent() {
                    this.jobFinished = false;
                    this.jobCancelled = false;
                    this.jobDelivered = false;
                    this.cancelPenalty = 0;
                    this.revenue = 0;
                    this.earnedXp = 0;
                    this.cargoDamage = 0;
                    this.distance = 0;
                    this.deliveryTime = "";
                    this.autoparkUsed = false;
                    this.autoloadUsed = false;
                }
                return Ets2JobEvent;
            })();

            var Ets2TollgateEvent = (function () {
                function Ets2TollgateEvent() {
                    this.tollgateUsed = false;
                    this.payAmount = 0;
                }
                return Ets2TollgateEvent;
            })();

            var Ets2FerryEvent = (function () {
                function Ets2FerryEvent() {
                    this.ferryUsed = false;
                    this.sourceName = "";
                    this.targetName = "";
                    this.sourceId = "";
                    this.targetId = "";
                    this.payAmount = 0;
                }
                return Ets2FerryEvent;
            })();

            var Ets2TrainEvent = (function () {
                function Ets2TrainEvent() {
                    this.trainUsed = false;
                    this.sourceName = "";
                    this.targetName = "";
                    this.sourceId = "";
                    this.targetId = "";
                    this.payAmount = 0;
                }
                return Ets2TrainEvent;
            })();

            var Ets2Vector = (function () {
                function Ets2Vector() {
                    this.x = 0;
                    this.y = 0;
                    this.z = 0;
                }
                return Ets2Vector;
            })();

            var Ets2Placement = (function () {
                function Ets2Placement() {
                    this.x = 0;
                    this.y = 0;
                    this.z = 0;
                    this.heading = 0;
                    this.pitch = 0;
                    this.roll = 0;
                }
                return Ets2Placement;
            })();

            var Ets2TelemetryData = (function () {
                function Ets2TelemetryData() {
                    this.game = new Ets2Game();
                    this.truck = new Ets2Truck();
                    this.cargo = new Ets2Cargo();
                    this.job = new Ets2Job();
                    this.navigation = new Ets2Navigation();
                    this.trailer1 = new Ets2Trailer();
                    this.trailer2 = new Ets2Trailer();
                    this.trailer3 = new Ets2Trailer();
                    this.trailer4 = new Ets2Trailer();
                    this.trailer5 = new Ets2Trailer();
                    this.trailer6 = new Ets2Trailer();
                    this.trailer7 = new Ets2Trailer();
                    this.trailer8 = new Ets2Trailer();
                    this.trailer9 = new Ets2Trailer();
                    this.trailer10 = new Ets2Trailer();
                    this.finedEvent = new Ets2FinedEvent();
                    this.jobEvent = new Ets2JobEvent();
                    this.tollgateEvent = new Ets2TollgateEvent();
                    this.ferryEvent = new Ets2FerryEvent();
                    this.trainEvent = new Ets2TrainEvent();
                }
                return Ets2TelemetryData;
            })();

            var Dashboard = (function () {
                function Dashboard(telemetryEndpointUrl, skinConfig) {
                    var _this = this;
                    this.$cache = {};
                    this._boundClasses = null;
                    this._pendingDataRequest = false;
                    this.lastDataRequestFrame = 0;
                    this.lastDataRequestFrameDiff = 0;
                    this.frame = 0;
                    this.latestData = null;
                    this.prevData = null;
                    this.frameData = null;
                    this.lastRafShimTime = 0;
                    this.endpointUrl = telemetryEndpointUrl;
                    this.skinConfig = skinConfig;
                    this.utils = this.utilityFunctions(skinConfig);
                    this.telemetryTransport = 'signalr';
                    this.restPollTimer = null;
                    this.initializeRequestAnimationFrame();

                    this.initialize(skinConfig, this.utils);

                    this.animationLoop();

                    Telemetry.Configuration.getInstance().detectTelemetryPort(function (port) {
                        _this.telemetryPort = port;
                        Dashboard.startRencloudBridgePoll();
                        _this.reconnectTimer = _this.setTimer(_this.reconnectTimer, function () {
                            _this.initializeHub();
                            _this.connectToHub();
                        }, 100);
                    });
                }
                Dashboard.prototype.setTimer = function (timer, func, delay) {
                    if (timer)
                        clearTimeout(timer);
                    return setTimeout(function () {
                        return func();
                    }, delay);
                };

                Dashboard.prototype.initializeRequestAnimationFrame = function () {
                    var _this = this;
                    var vendors = ['ms', 'moz', 'webkit', 'o'];
                    for (var x = 0; x < vendors.length && !window.requestAnimationFrame; ++x) {
                        window.requestAnimationFrame = window[vendors[x] + 'RequestAnimationFrame'];
                        window.cancelAnimationFrame = window[vendors[x] + 'CancelAnimationFrame'] || window[vendors[x] + 'CancelRequestAnimationFrame'];
                    }
                    if (!window.requestAnimationFrame)
                        window.requestAnimationFrame = function (callback) {
                            var now = Date.now();

                            var timeToCall = Math.max(0, (1000 / 30.0) - (now - _this.lastRafShimTime));
                            var id = window.setTimeout(function () {
                                callback(now + timeToCall);
                            }, timeToCall);
                            _this.lastRafShimTime = now + timeToCall;
                            return id;
                        };
                    if (!window.cancelAnimationFrame)
                        window.cancelAnimationFrame = function (id) {
                            clearTimeout(id);
                        };
                };

                Dashboard.prototype.animationLoop = function () {
                    var _this = this;
                    this.frame++;
                    window.requestAnimationFrame(function () {
                        return _this.animationLoop();
                    });

                    if (this.latestData && this.prevData) {
                        this.internalRender();

                        this.render(this.frameData, this.utils);
                    }
                };

                Dashboard.prototype.initializeHub = function () {
                    $.connection.hub.logging = false;
                    $.connection.hub.url = Telemetry.Configuration.getUrl('/signalr');
                    this.ets2TelemetryHub = $.connection['ets2TelemetryHub'];
                    window.onbeforeunload = function () {
                        $.connection.hub.stop();
                    };
                };

                Dashboard.prototype.connectToHub = function () {
                    var _this = this;
                    if (this.telemetryTransport === 'rest')
                        return;
                    $.connection.hub.stop();
                    this.ets2TelemetryHub.client['updateData'] = function (json) {
                        _this.dataUpdateCallback(json);
                    };
                    $.connection.hub.reconnected(function () {
                        _this.requestDataUpdate();
                    });
                    $.connection.hub.reconnecting(function () {
                        _this.process(null, Telemetry.Strings.connectingToServer);
                    });
                    $.connection.hub.disconnected(function () {
                        _this.process(null, Telemetry.Strings.disconnectedFromServer);
                        if (_this.telemetryTransport !== 'rest') {
                            _this.tryFunbitRestFallback(function (ok) {
                                if (!ok) _this.reconnectToHubAfterDelay();
                            });
                        } else {
                            _this.reconnectToHubAfterDelay();
                        }
                    });
                    $.connection.hub.start().done(function () {
                        _this.requestDataUpdate();
                    }).fail(function () {
                        _this.tryFunbitRestFallback(function (ok) {
                            if (ok) return;
                            _this.process(null, Telemetry.Strings.couldNotConnectToServer);
                            _this.reconnectToHubAfterDelay();
                        });
                    });
                };

                /** When SignalR is unavailable, poll Funbit REST /api/ets2/telemetry on :25555. */
                Dashboard.prototype.tryFunbitRestFallback = function (done) {
                    var _this = this;
                    if (this.telemetryTransport === 'rest') {
                        done(false);
                        return;
                    }
                    $.ajax({
                        url: Telemetry.Configuration.getTelemetryUrl('/api/ets2/telemetry'),
                        dataType: 'json',
                        timeout: 2000
                    }).done(function () {
                        _this.startRestTelemetry();
                        done(true);
                    }).fail(function () {
                        done(false);
                    });
                };

                Dashboard.prototype.reconnectToHubAfterDelay = function () {
                    var _this = this;
                    this.process(null, Telemetry.Strings.connectingToServer);
                    this.reconnectTimer = this.setTimer(this.reconnectTimer, function () {
                        _this.connectToHub();
                    }, Dashboard.reconnectDelay);
                };

                Dashboard.prototype.requestDataUpdate = function () {
                    this.lastDataRequestFrame = this.frame;
                    this.ets2TelemetryHub.server['requestData']();
                };

                Dashboard.prototype.dataUpdateCallback = function (jsonData) {
                    var data = Dashboard.normalizeTelemetryPayload(JSON.parse(jsonData));
                    this.process(data);
                    // Never request the next sample synchronously — that can recurse until
                    // the tab runs out of memory when the server responds immediately.
                    if (this._pendingDataRequest || this.telemetryTransport === 'rest') {
                        return;
                    }
                    this._pendingDataRequest = true;
                    var _this = this;
                    setTimeout(function () {
                        _this._pendingDataRequest = false;
                        _this.requestDataUpdate();
                    }, 0);
                };

                Dashboard.prototype.startRestTelemetry = function () {
                    this.telemetryTransport = 'rest';
                    if (this.reconnectTimer) {
                        clearTimeout(this.reconnectTimer);
                        this.reconnectTimer = null;
                    }
                    try { $.connection.hub.stop(); } catch (e) { /* ignore */ }
                    this.pollRestTelemetry();
                };

                Dashboard.prototype.pollRestTelemetry = function () {
                    var _this = this;
                    var url = Telemetry.Configuration.getTelemetryUrl('/api/ets2/telemetry');
                    $.ajax({
                        url: url,
                        dataType: 'json',
                        timeout: 3000
                    }).done(function (data) {
                        data = Dashboard.normalizeTelemetryPayload(data);
                        _this.lastDataRequestFrame = _this.frame;
                        _this.process(data);
                        var delay = (data.game && data.game.connected) ? 100 : 500;
                        _this.restPollTimer = _this.setTimer(_this.restPollTimer, function () {
                            _this.pollRestTelemetry();
                        }, delay);
                    }).fail(function () {
                        _this.process(null, Telemetry.Strings.couldNotConnectToServer);
                        _this.restPollTimer = _this.setTimer(_this.restPollTimer, function () {
                            _this.pollRestTelemetry();
                        }, Dashboard.reconnectDelay);
                    });
                };

                /** Normalize telemetry JSON variants to the Funbit-shaped fields skins expect. */
                Dashboard.SPEED_UNIT_KEY = 'rc.speedUnit';
                Dashboard.ECON_UNIT_KEY = 'tcd.econUnit';

                Dashboard.getSpeedUnit = function () {
                    try {
                        return localStorage.getItem(Dashboard.SPEED_UNIT_KEY) === 'mph' ? 'mph' : 'kmh';
                    } catch (e) {
                        return 'kmh';
                    }
                };

                /** Persist speed unit across all skins; optionally sync economy MI/KM toggle. */
                Dashboard.setSpeedUnit = function (unit, syncEcon) {
                    var u = unit === 'mph' ? 'mph' : 'kmh';
                    try {
                        localStorage.setItem(Dashboard.SPEED_UNIT_KEY, u);
                        if (syncEcon !== false) {
                            localStorage.setItem(Dashboard.ECON_UNIT_KEY, u === 'mph' ? 'imperial' : 'metric');
                        }
                    } catch (e) { /* ignore */ }
                    if (typeof window.dispatchEvent === 'function') {
                        try {
                            window.dispatchEvent(new CustomEvent('rc-speedunit-change', { detail: { unit: u } }));
                        } catch (evErr) { /* ignore */ }
                    }
                    return u;
                };

                Dashboard.toggleSpeedUnit = function () {
                    return Dashboard.setSpeedUnit(Dashboard.getSpeedUnit() === 'mph' ? 'kmh' : 'mph');
                };

                Dashboard.formatDistance = function (meters, unit) {
                    unit = unit || Dashboard.getSpeedUnit();
                    if (!isFinite(meters) || meters < 0) return '--';
                    if (unit === 'mph') {
                        if (meters >= 1609.344) return Math.round(meters / 1609.344) + ' mi';
                        return Math.round(meters * 3.28084) + ' ft';
                    }
                    if (meters >= 1000) return Math.round(meters / 1000) + ' km';
                    return Math.round(meters) + ' m';
                };

                Dashboard.GAME_EPOCH_MS = new Date('0001-01-01T00:00:00Z').getTime();

                Dashboard.parseGameIsoMs = function (value) {
                    if (value == null || value === '') return NaN;
                    var ms = new Date(value).getTime();
                    return isFinite(ms) ? ms : NaN;
                };

                Dashboard.gameTimeMs = function (game) {
                    if (!game) return NaN;
                    if (typeof game._timeMs === 'number' && isFinite(game._timeMs)) return game._timeMs;
                    return Dashboard.parseGameIsoMs(game.time);
                };

                /** Ms until mandatory rest; NaN when unknown. Handles Funbit duration-from-epoch and SCS absolute deadline. */
                Dashboard.calcRestRemainingMs = function (nextRestStopTime, gameTimeOrGame) {
                    var epoch = Dashboard.GAME_EPOCH_MS;
                    var restStopMs = Dashboard.parseGameIsoMs(nextRestStopTime);
                    if (!isFinite(restStopMs)) return NaN;
                    var gameMs = typeof gameTimeOrGame === 'number'
                        ? gameTimeOrGame
                        : Dashboard.gameTimeMs(gameTimeOrGame);
                    var restFromEpoch = restStopMs - epoch;
                    var restFromGame = isFinite(gameMs) ? (restStopMs - gameMs) : NaN;
                    if (isFinite(restFromGame) && (restFromEpoch > 2 * 86400000 ||
                        (restFromGame >= 0 && restFromGame < restFromEpoch))) {
                        return restFromGame;
                    }
                    if (restFromEpoch > 0) return restFromEpoch;
                    return NaN;
                };

                Dashboard.formatGameDuration = function (ms) {
                    if (!isFinite(ms) || ms <= 0) return '--';
                    var m = Math.round(ms / 60000);
                    var h = Math.floor(m / 60);
                    var pad = function (n) { return (n < 10 ? '0' : '') + n; };
                    return h > 0 ? h + 'h ' + pad(m % 60) + 'm' : (m % 60) + 'm';
                };

                Dashboard.formatRestRemaining = function (nextRestStopTime, gameTimeOrGame) {
                    var restMs = Dashboard.calcRestRemainingMs(nextRestStopTime, gameTimeOrGame);
                    if (isFinite(restMs) && restMs <= 0) return 'NOW';
                    return Dashboard.formatGameDuration(restMs);
                };

                Dashboard.normalizeTelemetryPayload = function (data) {
                    if (!data) return data;
                    if (data.trailers && !data.trailer) {
                        data.trailerCount = data.trailers.length;
                        for (var i = 0; i < data.trailers.length; i++) {
                            data['trailer' + (i + 1)] = data.trailers[i];
                        }
                        if (data.trailers.length > 0)
                            data.trailer = data.trailers[0];
                    }
                    if (data.job && !data.cargo) {
                        data.cargo = {
                            cargoLoaded: !!(data.job.cargo || data.job.cargoId),
                            cargoId: data.job.cargoId || '',
                            cargo: data.job.cargo || '',
                            mass: data.job.cargoMass || 0
                        };
                    }
                    if (data.job) {
                        var j = data.job;
                        if (j.destination) {
                            if (!j.destinationCity && j.destination.city) {
                                j.destinationCity = j.destination.city.name || j.destination.city.id || '';
                            }
                            if (!j.destinationCompany && j.destination.company) {
                                j.destinationCompany = j.destination.company.name || j.destination.company.id || '';
                            }
                            if (j.destination.city && j.destination.city.id) {
                                j.destinationCityId = j.destination.city.id;
                            }
                            if (j.destination.company && j.destination.company.id) {
                                j.destinationCompanyId = j.destination.company.id;
                            }
                        }
                    }
                    if (data.gameplay) {
                        var gp = data.gameplay;
                        if (!data.finedEvent) {
                            data.finedEvent = {
                                fined: !!gp.fined,
                                fineAmount: gp.finedDetails ? gp.finedDetails.amount : 0,
                                fineOffense: gp.finedDetails ? gp.finedDetails.offence : ''
                            };
                        }
                        if (!data.jobEvent) {
                            data.jobEvent = {
                                jobFinished: !!gp.jobFinished,
                                jobCancelled: !!gp.jobCancelled,
                                jobDelivered: !!gp.jobDelivered,
                                cancelPenalty: gp.jobCancelledDetails ? gp.jobCancelledDetails.penalty : 0
                            };
                        }
                    }
                    return data;
                };

                /** RenCloud extras are merged server-side in TruckDeck — no :25557 poll needed. */
                Dashboard._rencloudExtras = null;
                Dashboard._rencloudPollTimer = null;
                Dashboard._rencloudPollStarted = false;

                Dashboard.mergeRencloudExtras = function (data) {
                    return data;
                };

                Dashboard.startRencloudBridgePoll = function () {
                    /* native SCSSdkClient merge in TruckDeck.exe */
                };

                Dashboard.prototype.process = function (data, reason) {
                    if (typeof reason === "undefined") { reason = ''; }
                    if (data != null && data.game != null && !data.game.connected) {
                        reason = Telemetry.Strings.connectedAndWaitingForDrive;

                        data = new Ets2TelemetryData();
                    } else if (data === null) {
                        data = new Ets2TelemetryData();
                    }

                    $('.statusMessage').html(reason);

                    data = this.filter(data, this.utils);

                    data = this.internalFilter(data);

                    this.lastDataRequestFrameDiff = this.frame - this.lastDataRequestFrame;
                    // frameData must be assigned before prevData so a requestAnimationFrame
                    // tick cannot run internalRender with prevData set but frameData still null.
                    var previous = this.latestData;
                    this.frameData = previous;
                    this.prevData = previous;
                    this.latestData = data;
                };

                Dashboard.prototype.internalFilter = function (data) {
                    if (data.game) {
                        if (this.isIso8601(data.game.time)) {
                            data.game._timeMs = new Date(data.game.time).getTime();
                        }
                        data.game.time = this.timeToReadableString(data.game.time);
                    }
                    if (data.job) {
                        data.job.deadlineTime = this.timeToReadableString(data.job.deadlineTime);
                        data.job.remainingTime = this.timeDifferenceToReadableString(data.job.remainingTime);
                    }
                    return data;
                };

                /** CSS class names present in the loaded skin (e.g. truck-speed). */
                Dashboard.prototype._scanBoundClasses = function () {
                    this._boundClasses = {};
                    var $root = $('.dashboard');
                    if (!$root.length) {
                        return;
                    }
                    $root.find('[class]').addBack('[class]').each(function () {
                        var parts = (this.className || '').split(/\s+/);
                        for (var i = 0; i < parts.length; i++) {
                            var cls = parts[i];
                            if (cls && cls.charAt(0) !== '_') {
                                this._boundClasses[cls] = true;
                            }
                        }
                    }.bind(this));
                };

                Dashboard.prototype._pathToCss = function (fullPropName) {
                    return this.replaceAll(fullPropName, '.', '-');
                };

                /** True when the skin binds this path or a nested field (truck → truck-speed). */
                Dashboard.prototype._hasBoundDescendants = function (fullPropName) {
                    if (!this._boundClasses) {
                        this._scanBoundClasses();
                    }
                    var exact = this._pathToCss(fullPropName);
                    if (this._boundClasses[exact]) {
                        return true;
                    }
                    var prefix = exact + '-';
                    for (var cls in this._boundClasses) {
                        if (Object.prototype.hasOwnProperty.call(this._boundClasses, cls) &&
                            cls.indexOf(prefix) === 0) {
                            return true;
                        }
                    }
                    return false;
                };

                /** Cached jQuery lookup; false = no matching elements (do not query again). */
                Dashboard.prototype._getElements = function (fullPropName) {
                    if (this.$cache[fullPropName] === false) {
                        return null;
                    }
                    if (this.$cache[fullPropName] !== undefined) {
                        return this.$cache[fullPropName];
                    }
                    if (!this._hasBoundDescendants(fullPropName)) {
                        this.$cache[fullPropName] = false;
                        return null;
                    }
                    var $e = $('.' + this._pathToCss(fullPropName));
                    if ($e.length === 0) {
                        this.$cache[fullPropName] = false;
                        return null;
                    }
                    this.$cache[fullPropName] = $e;
                    return $e;
                };

                Dashboard.prototype.internalRender = function (parent, propNamePrefix) {
                    if (typeof parent === "undefined") { parent = null; }
                    if (typeof propNamePrefix === "undefined") { propNamePrefix = null; }
                    if (parent == null && (!this.latestData || !this.frameData)) {
                        return;
                    }
                    if (!this._boundClasses) {
                        this._scanBoundClasses();
                    }
                    var propSplitter = '.';
                    var frames = Math.max(1, this.lastDataRequestFrameDiff) * 1.0;
                    var object = parent != null ? parent : this.latestData;
                    for (var propName in object) {
                        if (!Object.prototype.hasOwnProperty.call(object, propName)) {
                            continue;
                        }
                        var fullPropName = propNamePrefix != null ? propNamePrefix + propName : propName;
                        var value = object[propName];

                        if (typeof value === "number") {
                            var $num = this._getElements(fullPropName);
                            if (!$num) {
                                continue;
                            }
                            var prevValue = this.resolveObjectByPath(this.prevData, fullPropName);
                            var frameValue = this.resolveObjectByPath(this.frameData, fullPropName);
                            if (typeof frameValue !== "number" || !isFinite(frameValue)) {
                                frameValue = (typeof prevValue === "number" && isFinite(prevValue)) ? prevValue : value;
                            }
                            if (typeof prevValue !== "number" || !isFinite(prevValue)) {
                                prevValue = frameValue;
                            }
                            value = frameValue + (value - prevValue) / frames;
                            if (propNamePrefix == null) {
                                this.frameData[propName] = value;
                            } else {
                                var parentPropName = fullPropName.substr(0, fullPropName.lastIndexOf(propSplitter));
                                var parentObj = this.resolveObjectByPath(this.frameData, parentPropName);
                                if (!parentObj) {
                                    continue;
                                }
                                parentObj[propName] = value;
                            }
                            var $meters = $num.filter('[data-type="meter"]');
                            if ($meters.length > 0) {
                                var minValue = $meters.data('min');
                                if (/^[a-z\.]+$/i.test(minValue)) {
                                    minValue = this.resolveObjectByPath(this.latestData, minValue);
                                }
                                var maxValue = $meters.data('max');
                                if (/^[a-z\.]+$/i.test(maxValue)) {
                                    maxValue = this.resolveObjectByPath(this.latestData, maxValue);
                                }
                                this.setMeter($meters, value, parseFloat(minValue), parseFloat(maxValue));
                            } else {
                                var $notMeters = $num.not('[data-type="meter"]');
                                if ($notMeters.length > 0) {
                                    $notMeters.html("" + Math.round(value));
                                }
                            }
                            $num.attr('data-value', value);
                        } else if (typeof value === "boolean") {
                            var $bool = this._getElements(fullPropName);
                            if (!$bool) {
                                continue;
                            }
                            if (value) {
                                $bool.addClass('yes');
                            } else {
                                $bool.removeClass('yes');
                            }
                            $bool.attr('data-value', value);
                        } else if (typeof value === "string") {
                            var $str = this._getElements(fullPropName);
                            if (!$str) {
                                continue;
                            }
                            $str.html(value);
                            $str.attr('data-value', value);
                        } else if ($.isArray(value)) {
                            if (!this._hasBoundDescendants(fullPropName)) {
                                continue;
                            }
                            for (var j = 0; j < value.length; j++) {
                                this.internalRender(value[j], fullPropName + propSplitter + j + propSplitter);
                            }
                        } else if (typeof value === "object" && value !== null) {
                            if (!this._hasBoundDescendants(fullPropName)) {
                                continue;
                            }
                            this.internalRender(value, fullPropName + propSplitter);
                        }
                    }
                };

                Dashboard.prototype.setMeter = function ($meter, value, minValue, maxValue) {
                    var maxValue = maxValue ? maxValue : $meter.data('max');
                    var minAngle = $meter.data('min-angle');
                    var maxAngle = $meter.data('max-angle');
                    value = Math.min(value, maxValue);
                    value = Math.max(value, minValue);
                    var offset = (value - minValue) / (maxValue - minValue);
                    var angle = (maxAngle - minAngle) * offset + minAngle;
                    var updateTransform = function (v) {
                        $meter.css({
                            'transform': v,
                            '-webkit-transform': v,
                            '-moz-transform': v,
                            '-ms-transform': v
                        });
                    };
                    updateTransform('rotate(' + angle + 'deg)');
                };

                Dashboard.prototype.utilityFunctions = function (skinConfig) {
                    var _this = this;
                    return {
                        formatInteger: this.formatInteger,
                        formatFloat: this.formatFloat,
                        preloadImages: function (images) {
                            return _this.preloadImages(skinConfig, images);
                        },
                        resolveObjectByPath: this.resolveObjectByPath
                    };
                };

                Dashboard.prototype.preloadImages = function (skinConfig, images) {
                    $(images).each(function () {
                        $('<img/>')[0]['src'] = Telemetry.Configuration.getInstance().getSkinResourceUrl(skinConfig, this);
                    });
                };

                Dashboard.prototype.formatInteger = function (num, digits) {
                    var output = num + "";
                    while (output.length < digits)
                        output = "0" + output;
                    return output;
                };

                Dashboard.prototype.formatFloat = function (num, digits) {
                    var power = Math.pow(10, digits || 0);
                    return String((Math.round(num * power) / power).toFixed(digits));
                };

                Dashboard.prototype.isIso8601 = function (date) {
                    return /(\d{4})-(\d{2})-(\d{2})T(\d{2})\:(\d{2})\:(\d{2})Z/.test(date);
                };

                Dashboard.prototype.timeToReadableString = function (date) {
                    if (this.isIso8601(date)) {
                        var d = new Date(date);
                        return Telemetry.Strings.dayOfTheWeek[d.getUTCDay()] + ' ' + this.formatInteger(d.getUTCHours(), 2) + ':' + this.formatInteger(d.getUTCMinutes(), 2);
                    }
                    return date;
                };

                Dashboard.prototype.timeDifferenceToReadableString = function (date) {
                    if (this.isIso8601(date)) {
                        var d = new Date(date);
                        var dys = d.getUTCDate() - 1;
                        var hrs = d.getUTCHours();
                        var mnt = d.getUTCMinutes();
                        var o = dys > 1 ? dys + ' days ' : (dys != 0 ? dys + ' day ' : '');
                        if (hrs > 0)
                            o += hrs > 1 ? hrs + ' hours ' : hrs + ' hour ';
                        if (mnt > 0)
                            o += mnt > 1 ? mnt + ' minutes' : mnt + ' minute';
                        if (!o)
                            o = Telemetry.Strings.noTimeLeft;
                        return o;
                    }
                    return date;
                };

                Dashboard.prototype.replaceAll = function (input, search, replace) {
                    return input.replace(new RegExp('\\' + search, 'g'), replace);
                };

                Dashboard.prototype.resolveObjectByPath = function (obj, path) {
                    return path.split('.').reduce(function (prev, curr) {
                        if (prev == null) {
                            return undefined;
                        }
                        return prev[curr];
                    }, obj);
                };

                Dashboard.prototype.filter = function (data, utils) {
                    return data;
                };

                Dashboard.prototype.render = function (data, utils) {
                    return;
                };

                Dashboard.prototype.initialize = function (skinConfig, utils) {
                    return;
                };
                Dashboard.reconnectDelay = 1000;
                return Dashboard;
            })();
            Telemetry.Dashboard = Dashboard;
        })(Ets.Telemetry || (Ets.Telemetry = {}));
        var Telemetry = Ets.Telemetry;
    })(Funbit.Ets || (Funbit.Ets = {}));
    var Ets = Funbit.Ets;
})(Funbit || (Funbit = {}));
