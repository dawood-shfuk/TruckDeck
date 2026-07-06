using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    public sealed class InputBridgeHost : IDisposable
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        HttpListener _listener;
        Thread _thread;
        volatile bool _running;
        InputBridgeConfigDto _config;
        string _configPath;
        DateTime _configLoadedAt;
        JoyButtonMonitor _joyMonitor;
        Timer _joyRetryTimer;
        readonly object _dashboardLock = new object();
        readonly DashboardEvents _dashboardEvents = new DashboardEvents();

        public bool IsRunning => _running;
        public string BindPrefix { get; private set; }

        public int Port => _config?.Port ?? 25556;

        public int TapHoldMs => _config?.TapHoldMs > 0 ? _config.TapHoldMs : 60;

        public int KeyCount => _config?.Keys?.Count ?? 0;

        public void Start()
        {
            if (_running)
                return;

            ReloadConfig();

            var prefixes = new[]
            {
                $"http://+:{Port}/",
                $"http://*:{Port}/",
                $"http://localhost:{Port}/",
                $"http://127.0.0.1:{Port}/"
            };

            Exception lastError = null;
            foreach (var prefix in prefixes)
            {
                HttpListener listener = null;
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add(prefix);
                    listener.Start();
                    _listener = listener;
                    _running = true;
                    BindPrefix = prefix;
                    _thread = new Thread(ListenLoop) { IsBackground = true, Name = "TruckDeck.InputBridge" };
                    _thread.Start();
                    StartJoyMonitor();
                    StartJoyRetryTimer();
                    Log.InfoFormat("Input bridge listening on {0}", prefix);
                    if (prefix.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0
                        || prefix.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Warn("Input bridge bound to localhost only — run TruckDeck setup as Administrator for phone/LAN access on port " + Port);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    try { listener?.Close(); } catch { /* ignore */ }
                }
            }

            Log.Warn("Input bridge could not bind on port " + Port + " (run TruckDeck setup as Administrator for http://+ URL ACL): "
                     + (lastError?.Message ?? "unknown error"));
        }

        public void Stop()
        {
            _running = false;
            BindPrefix = null;
            StopJoyRetryTimer();
            StopJoyMonitor();
            try { _listener?.Stop(); } catch { /* ignore */ }
            try { _listener?.Close(); } catch { /* ignore */ }
            _listener = null;
        }

        public void Dispose()
        {
            Stop();
            _joyMonitor?.Dispose();
            _joyMonitor = null;
        }

        void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(ctx));
                }
                catch (HttpListenerException)
                {
                    if (!_running)
                        break;
                }
                catch (Exception ex)
                {
                    Log.Error("Input bridge listener error", ex);
                }
            }
        }

        void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var path = ctx.Request.Url.AbsolutePath.TrimEnd('/');
                if (ctx.Request.HttpMethod == "OPTIONS")
                {
                    WriteResponse(ctx, 204, null);
                    return;
                }

                if (path == "/health" && ctx.Request.HttpMethod == "GET")
                {
                    WriteJson(ctx, new
                    {
                        ok = true,
                        port = Port,
                        keys = _config?.Keys?.Count ?? 0,
                        mouse = true,
                        joy = BuildJoyStatus()
                    });
                    return;
                }

                if (path == "/api/dashboard/events" && ctx.Request.HttpMethod == "GET")
                {
                    WriteJson(ctx, new { ok = true, events = TakeDashboardEvents() });
                    return;
                }

                if (path == "/api/dashboard/screenCycle" && ctx.Request.HttpMethod == "POST")
                {
                    QueueDashboardEvent("screenCycle");
                    WriteJson(ctx, new { ok = true, action = "screenCycle" });
                    return;
                }

                if (path == "/reload" && ctx.Request.HttpMethod == "POST")
                {
                    ReloadConfig();
                    WriteJson(ctx, new { ok = true, reloaded = true });
                    return;
                }

                if (path.StartsWith("/api/command/", StringComparison.OrdinalIgnoreCase)
                    && ctx.Request.HttpMethod == "POST")
                {
                    var action = Uri.UnescapeDataString(path.Substring("/api/command/".Length));
                    HandleCommand(ctx, action);
                    return;
                }

                if (path == "/api/mouse/move" && ctx.Request.HttpMethod == "POST")
                {
                    var body = ReadBody(ctx);
                    var jo = JObject.Parse(body);
                    var ok = SendInputHelper.MouseMove(jo.Value<int?>("dx") ?? 0, jo.Value<int?>("dy") ?? 0);
                    WriteJson(ctx, new { ok });
                    return;
                }

                if (path == "/api/mouse/click" && ctx.Request.HttpMethod == "POST")
                {
                    var body = ReadBody(ctx);
                    var jo = JObject.Parse(body);
                    var ok = SendInputHelper.MouseClick(jo.Value<string>("button"), jo.Value<string>("state"));
                    WriteJson(ctx, new { ok });
                    return;
                }

                if (path == "/api/mouse/scroll" && ctx.Request.HttpMethod == "POST")
                {
                    var body = ReadBody(ctx);
                    var jo = JObject.Parse(body);
                    var ok = SendInputHelper.MouseScroll(jo.Value<int?>("delta") ?? 0);
                    WriteJson(ctx, new { ok });
                    return;
                }

                WriteResponse(ctx, 404, "Not found");
            }
            catch (Exception ex)
            {
                Log.Error("Input bridge request error", ex);
                try { WriteJson(ctx, new { ok = false, error = ex.Message }); } catch { /* ignore */ }
            }
        }

        void HandleCommand(HttpListenerContext ctx, string action)
        {
            if (_config?.Keys == null || !_config.Keys.TryGetValue(action, out var combo))
            {
                WriteJson(ctx, new { ok = false, error = "unknown_action", action });
                return;
            }

            var ok = SendInputHelper.TapCombo(combo, _config.TapHoldMs);
            WriteJson(ctx, new { ok, action, combo });
        }

        public void ReloadConfig()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bridges", "InputBridgeConfig.json");
            if (!File.Exists(_configPath))
                _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InputBridgeConfig.json");

            if (File.Exists(_configPath))
            {
                _config = JsonConvert.DeserializeObject<InputBridgeConfigDto>(File.ReadAllText(_configPath));
                _configLoadedAt = File.GetLastWriteTimeUtc(_configPath);
            }
            else
            {
                _config = new InputBridgeConfigDto();
            }

            _config.Port = int.TryParse(ConfigurationManager.AppSettings["InputBridgePort"], out var p) ? p : (_config.Port > 0 ? _config.Port : 25556);
            _config.TapHoldMs = _config.TapHoldMs > 0 ? _config.TapHoldMs : 60;
            if (_config.Keys == null)
                _config.Keys = new Dictionary<string, string>();
            if (_config.Dashboard == null)
                _config.Dashboard = new DashboardConfigDto();
            if (_running)
                StartJoyMonitor();
        }

        public BridgeJoyStatus GetJoyStatus()
        {
            if (_joyMonitor == null)
            {
                return new BridgeJoyStatus
                {
                    Enabled = false,
                    Binding = _config?.Dashboard?.ScreenCycleJoy,
                    Reason = _running ? "not started" : "bridge stopped"
                };
            }

            return new BridgeJoyStatus
            {
                Enabled = _joyMonitor.IsEnabled,
                Binding = _joyMonitor.Binding,
                Reason = _joyMonitor.IsEnabled ? null : _joyMonitor.DisabledReason
            };
        }

        public void TriggerScreenCycleTest() => QueueDashboardEvent("screenCycle");

        public bool TrySendCommand(string action, out string combo, int? tapHoldMs = null)
        {
            combo = null;
            if (_config?.Keys == null || !_config.Keys.TryGetValue(action, out combo))
                return false;
            return SendInputHelper.TapCombo(combo, tapHoldMs ?? _config.TapHoldMs);
        }

        void OnJoyScreenCyclePress()
        {
            QueueDashboardEvent("screenCycle");
        }

        void StartJoyMonitor()
        {
            StopJoyMonitor();
            var binding = _config?.Dashboard?.ScreenCycleJoy;
            _joyMonitor = new JoyButtonMonitor();
            _joyMonitor.OnPress += OnJoyScreenCyclePress;
            _joyMonitor.ApplyBinding(binding);
        }

        void StopJoyMonitor()
        {
            if (_joyMonitor == null)
                return;
            _joyMonitor.OnPress -= OnJoyScreenCyclePress;
            _joyMonitor.Stop();
            _joyMonitor.Dispose();
            _joyMonitor = null;
        }

        void StartJoyRetryTimer()
        {
            StopJoyRetryTimer();
            _joyRetryTimer = new Timer(_ => MaybeRetryJoyMonitor(), null, 30000, 30000);
        }

        void StopJoyRetryTimer()
        {
            _joyRetryTimer?.Dispose();
            _joyRetryTimer = null;
        }

        void MaybeRetryJoyMonitor()
        {
            if (!_running)
                return;
            var binding = _config?.Dashboard?.ScreenCycleJoy;
            if (string.IsNullOrWhiteSpace(binding))
                return;
            if (_joyMonitor != null && _joyMonitor.IsEnabled)
                return;
            StartJoyMonitor();
        }

        object BuildJoyStatus()
        {
            if (_joyMonitor == null)
                return new { enabled = false, binding = _config?.Dashboard?.ScreenCycleJoy, reason = "not started" };
            return new
            {
                enabled = _joyMonitor.IsEnabled,
                binding = _joyMonitor.Binding,
                reason = _joyMonitor.IsEnabled ? null : _joyMonitor.DisabledReason
            };
        }

        void QueueDashboardEvent(string name)
        {
            lock (_dashboardLock)
            {
                _dashboardEvents.Increment(name);
            }
        }

        object TakeDashboardEvents()
        {
            lock (_dashboardLock)
            {
                var snap = _dashboardEvents.SnapshotAndClear();
                return snap;
            }
        }

        static string ReadBody(HttpListenerContext ctx)
        {
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                return reader.ReadToEnd();
        }

        static void WriteJson(HttpListenerContext ctx, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            WriteResponse(ctx, 200, json, "application/json");
        }

        static void WriteResponse(HttpListenerContext ctx, int status, string body, string contentType = "text/plain")
        {
            ctx.Response.StatusCode = status;
            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                ctx.Response.ContentType = contentType + "; charset=utf-8";
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            ctx.Response.OutputStream.Close();
        }

        class DashboardEvents
        {
            int _screenCycle;

            public void Increment(string name)
            {
                if (name == "screenCycle")
                    _screenCycle++;
            }

            public object SnapshotAndClear()
            {
                var snap = new { screenCycle = _screenCycle };
                _screenCycle = 0;
                return snap;
            }
        }
    }
}
