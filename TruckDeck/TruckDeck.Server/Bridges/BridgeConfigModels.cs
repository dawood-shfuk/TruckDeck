using System.Collections.Generic;
using Newtonsoft.Json;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    public sealed class InputBridgeConfigDto
    {
        [JsonProperty("port")]
        public int Port { get; set; } = 25556;

        [JsonProperty("tap_hold_ms")]
        public int TapHoldMs { get; set; } = 60;

        [JsonProperty("keys")]
        public Dictionary<string, string> Keys { get; set; } = new Dictionary<string, string>();

        [JsonProperty("dashboard")]
        public DashboardConfigDto Dashboard { get; set; } = new DashboardConfigDto();
    }

    public sealed class DashboardConfigDto
    {
        [JsonProperty("screenCycleJoy")]
        public string ScreenCycleJoy { get; set; }
    }

    public sealed class BridgeConfigSnapshot
    {
        public int Port { get; set; } = 25556;
        public int TapHoldMs { get; set; } = 60;
        public string ScreenCycleJoy { get; set; }
        public Dictionary<string, string> Keys { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
    }

    public sealed class JoyDeviceInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
    }

    public sealed class JoyCaptureResult
    {
        public string Binding { get; set; }
        public int DeviceIndex { get; set; }
        public int ButtonIndex { get; set; }
        public string DeviceName { get; set; }
    }

    public sealed class BridgeJoyStatus
    {
        public bool Enabled { get; set; }
        public string Binding { get; set; }
        public string Reason { get; set; }
    }
}
