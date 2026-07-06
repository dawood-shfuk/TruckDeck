using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Funbit.Ets.Telemetry.Server.Bridges;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Funbit.Ets.Telemetry.Server.Services
{
    public sealed class BridgeConfigService
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static BridgeConfigService Instance { get; } = new BridgeConfigService();

        InputBridgeHost _host;
        JObject _document;
        string _configPath;
        string _pythonMirrorPath;

        BridgeConfigService() { }

        public void RegisterHost(InputBridgeHost host) => _host = host;

        public string ConfigPath => _configPath;

        public int EffectivePort
        {
            get
            {
                if (int.TryParse(ConfigurationManager.AppSettings["InputBridgePort"], out var p))
                    return p;
                return _document?.Value<int?>("port") ?? 25556;
            }
        }

        public BridgeConfigSnapshot Load()
        {
            ResolvePaths();
            if (File.Exists(_configPath))
            {
                var text = File.ReadAllText(_configPath);
                _document = string.IsNullOrWhiteSpace(text)
                    ? CreateDefaultDocument()
                    : JObject.Parse(text);
            }
            else
            {
                _document = CreateDefaultDocument();
            }

            return SnapshotFromDocument(_document);
        }

        public void Save(BridgeConfigSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            ResolvePaths();

            if (_document == null)
                _document = CreateDefaultDocument();

            _document["tap_hold_ms"] = snapshot.TapHoldMs > 0 ? snapshot.TapHoldMs : 60;
            if (_document["port"] == null)
                _document["port"] = snapshot.Port > 0 ? snapshot.Port : 25556;

            var dash = _document["dashboard"] as JObject;
            if (dash == null)
            {
                dash = new JObject();
                _document["dashboard"] = dash;
            }
            if (dash["_comment"] == null)
            {
                dash["_comment"] =
                    "Dash screen-cycle joystick (ETS2-style, e.g. joy1.b1). Auto-disabled if device not found.";
            }
            if (string.IsNullOrWhiteSpace(snapshot.ScreenCycleJoy))
                dash.Remove("screenCycleJoy");
            else
                dash["screenCycleJoy"] = snapshot.ScreenCycleJoy.Trim();

            var keysObj = _document["keys"] as JObject;
            if (keysObj == null)
            {
                keysObj = new JObject();
                _document["keys"] = keysObj;
            }

            foreach (var pair in snapshot.Keys)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    keysObj.Remove(pair.Key);
                else
                    keysObj[pair.Key] = pair.Value.Trim();
            }

            if (_document["_comment"] == null)
            {
                _document["_comment"] =
                    "Map each dashboard action (left) to the in-game key or combo (right). Set these to match YOUR bindings in ETS2/ATS > Options > Keys & Buttons.";
            }

            var json = _document.ToString(Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath) ?? ".");
            File.WriteAllText(_configPath, json + Environment.NewLine);

            try
            {
                if (!string.IsNullOrEmpty(_pythonMirrorPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_pythonMirrorPath) ?? ".");
                    File.WriteAllText(_pythonMirrorPath, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Could not mirror bridge config to Python path", ex);
            }

            _host?.ReloadConfig();
        }

        public void ExportTo(string path)
        {
            if (_document == null)
                Load();
            File.WriteAllText(path, _document.ToString(Formatting.Indented) + Environment.NewLine);
        }

        public BridgeConfigSnapshot ImportFrom(string path)
        {
            var text = File.ReadAllText(path);
            _document = JObject.Parse(text);
            return SnapshotFromDocument(_document);
        }

        public BridgeConfigSnapshot CreateDefaultsSnapshot()
        {
            return new BridgeConfigSnapshot
            {
                Port = 25556,
                TapHoldMs = 60,
                ScreenCycleJoy = "joy1.b1",
                Keys = BridgeActionCatalog.DefaultKeys()
            };
        }

        public void ExportSnapshot(BridgeConfigSnapshot snapshot, string path)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (_document == null)
                Load();
            var doc = (JObject)_document.DeepClone();
            doc["tap_hold_ms"] = snapshot.TapHoldMs > 0 ? snapshot.TapHoldMs : 60;
            var dash = doc["dashboard"] as JObject ?? new JObject();
            doc["dashboard"] = dash;
            if (string.IsNullOrWhiteSpace(snapshot.ScreenCycleJoy))
                dash.Remove("screenCycleJoy");
            else
                dash["screenCycleJoy"] = snapshot.ScreenCycleJoy.Trim();
            var keysObj = doc["keys"] as JObject ?? new JObject();
            doc["keys"] = keysObj;
            foreach (var pair in snapshot.Keys)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    keysObj.Remove(pair.Key);
                else
                    keysObj[pair.Key] = pair.Value.Trim();
            }
            File.WriteAllText(path, doc.ToString(Formatting.Indented) + Environment.NewLine);
        }

        public BridgeJoyStatus GetJoyStatus() => _host?.GetJoyStatus() ?? new BridgeJoyStatus();

        public void TriggerScreenCycleTest() => _host?.TriggerScreenCycleTest();

        public bool TrySendCommand(string action, int tapHoldMs, out string combo)
        {
            combo = null;
            if (_host == null)
                return false;
            return _host.TrySendCommand(action, out combo, tapHoldMs);
        }

        static void ResolvePathsInstance(BridgeConfigService svc)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            svc._configPath = Path.Combine(baseDir, "Bridges", "InputBridgeConfig.json");
            if (!File.Exists(svc._configPath))
                svc._configPath = Path.Combine(baseDir, "InputBridgeConfig.json");
            svc._pythonMirrorPath = Path.Combine(baseDir, "Html", "input_bridge", "bridge_config.json");
        }

        void ResolvePaths() => ResolvePathsInstance(this);

        static JObject CreateDefaultDocument()
        {
            var snap = new BridgeConfigService().CreateDefaultsSnapshot();
            return JObject.FromObject(new
            {
                port = snap.Port,
                tap_hold_ms = snap.TapHoldMs,
                _comment =
                    "Map each dashboard action (left) to the in-game key or combo (right). Set these to match YOUR bindings in ETS2/ATS > Options > Keys & Buttons.",
                keys = snap.Keys,
                dashboard = new
                {
                    _comment =
                        "Dash screen-cycle joystick (ETS2-style, e.g. joy1.b1). Auto-disabled if device not found.",
                    screenCycleJoy = snap.ScreenCycleJoy
                }
            });
        }

        static BridgeConfigSnapshot SnapshotFromDocument(JObject doc)
        {
            var snap = new BridgeConfigSnapshot
            {
                Port = doc.Value<int?>("port") ?? 25556,
                TapHoldMs = doc.Value<int?>("tap_hold_ms") ?? 60,
                ScreenCycleJoy = doc["dashboard"]?["screenCycleJoy"]?.Value<string>()
            };

            var keys = doc["keys"] as JObject;
            if (keys != null)
            {
                foreach (var prop in keys.Properties())
                {
                    if (prop.Name.StartsWith("_", StringComparison.Ordinal))
                        continue;
                    snap.Keys[prop.Name] = prop.Value?.Value<string>();
                }
            }

            foreach (var entry in BridgeActionCatalog.All)
            {
                if (!snap.Keys.ContainsKey(entry.Id) && keys?[entry.Id] != null)
                    snap.Keys[entry.Id] = keys[entry.Id].Value<string>();
            }

            return snap;
        }
    }
}
