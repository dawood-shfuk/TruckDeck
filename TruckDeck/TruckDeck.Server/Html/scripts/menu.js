var Funbit;
(function (Funbit) {
    (function (Ets) {
        (function (Telemetry) {
            var Menu = (function () {
                function skinAssetUrl(name, file) {
                    var path = String(name || '').split('/').map(function (p) {
                        return encodeURIComponent(p);
                    }).join('/');
                    return Telemetry.Configuration.getUrl('/skins/' + path + '/' + file);
                }

                function Menu() {
                    var _this = this;
                    $.when(Telemetry.Configuration.getInstance().initialized).done(function (config) {
                        _this.config = config;
                        _this.initializeEvents();
                        _this.buildSkinTable([]);
                        if (!config.serverIp) {
                            _this.promptServerIp();
                        } else {
                            $('.server-ip').html(config.serverIp);
                            _this.connectToServer();
                        }
                    });
                }
                Menu.prototype.buildSkinTable = function (skins) {
                    var skinTemplateDo = doT.template($('#skin-row-template').html());
                    var groups = [
                        { key: 'TruckDeck skins', table: $('table.skins-truckdeck') },
                        { key: 'Original Funbit skins', table: $('table.skins-funbit') }
                    ];
                    var byGroup = {};
                    for (var g = 0; g < groups.length; g++) {
                        byGroup[groups[g].key] = [];
                    }
                    for (var i = 0; i < skins.length; i++) {
                        var skin = skins[i];
                        var groupKey = skin.group || 'TruckDeck skins';
                        if (!byGroup[groupKey]) {
                            groupKey = 'TruckDeck skins';
                        }
                        byGroup[groupKey].push(skin);
                    }
                    var anySkins = skins.length > 0;
                    for (var gi = 0; gi < groups.length; gi++) {
                        var section = groups[gi];
                        var list = byGroup[section.key] || [];
                        section.table.empty();
                        if (list.length === 0) {
                            section.table.addClass('skin-section-empty');
                            if (!anySkins && gi === 0) {
                                section.table.removeClass('skin-section-empty');
                                section.table.append(doT.template($('#skin-empty-row-template').html())({}));
                            }
                        } else {
                            section.table.removeClass('skin-section-empty');
                            var html = '';
                            for (var si = 0; si < list.length; si++) {
                                var skinConfig = list[si];
                                skinConfig.splashUrl = skinAssetUrl(skinConfig.name, 'dashboard.jpg');
                                html += skinTemplateDo(skinConfig);
                            }
                            section.table.append(html);
                        }
                    }
                };

                Menu.prototype.connectToServer = function () {
                    var _this = this;
                    var serverIp = $('.server-ip').html();
                    if (!serverIp)
                        return;
                    var $serverStatus = $('.server-status');
                    $serverStatus.removeClass('connected').addClass('disconnected').html(Telemetry.Strings.connecting);
                    this.buildSkinTable([]);
                    this.config.reload(serverIp, function () {
                        $serverStatus.removeClass('disconnected').addClass('connected').html(Telemetry.Strings.connected);
                        _this.buildSkinTable(_this.config.skins);
                    }, function () {
                        $serverStatus.removeClass('connected').addClass('disconnected').html(Telemetry.Strings.disconnected);
                        _this.buildSkinTable(_this.config.skins);
                        _this.reconnectionTimer = setTimeout(_this.connectToServer.bind(_this, [$('.server-ip').html()]), 3000);
                    });
                };

                Menu.prototype.promptServerIp = function () {
                    var ip = prompt(Telemetry.Strings.enterServerIpMessage, this.config.serverIp);
                    if (!ip)
                        return;
                    var correct = /^[a-zA-Z0-9\.\-]+$/.test(ip);
                    if (!correct) {
                        alert(Telemetry.Strings.incorrectServerIpFormat);
                    } else {
                        $('.server-ip').html(ip);
                        this.connectToServer();
                    }
                };

                Menu.prototype.initializeEvents = function () {
                    var _this = this;
                    $(document).on('click', 'td.skin-image,td.skin-desc,.skin-view', function (e) {
                        var $this = $(e.currentTarget);
                        var skinName = $this.closest('tr').data('name');
                        var dashboardHostUrl = Telemetry.Configuration.getUrl(
                            "/dashboard-host.html?skin=" + encodeURIComponent(skinName) +
                            "&ip=" + encodeURIComponent(_this.config.serverIp));
                        window.location.href = dashboardHostUrl;
                    });
                    $('.edit-server-ip').click(function () {
                        _this.promptServerIp();
                    });
                };
                return Menu;
            })();
            Telemetry.Menu = Menu;
        })(Ets.Telemetry || (Ets.Telemetry = {}));
        var Telemetry = Ets.Telemetry;
    })(Funbit.Ets || (Funbit.Ets = {}));
    var Ets = Funbit.Ets;
})(Funbit || (Funbit = {}));

if (Funbit.Ets.Telemetry.Configuration.isCordovaAvailable()) {
    $(document).on('deviceready', function () {
        Funbit.Ets.Telemetry.Menu.instance = new Funbit.Ets.Telemetry.Menu();
    });
} else {
    Funbit.Ets.Telemetry.Menu.instance = new Funbit.Ets.Telemetry.Menu();
}
