using System.Collections.Generic;
using System.Linq;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    public sealed class BridgeActionEntry
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Category { get; set; }
        public string DefaultCombo { get; set; }
    }

    public static class BridgeActionCatalog
    {
        static readonly BridgeActionEntry[] Entries =
        {
            new BridgeActionEntry { Id = "engine", Label = "Engine start/stop", Category = "Engine", DefaultCombo = "e" },
            new BridgeActionEntry { Id = "engineBrake", Label = "Engine brake toggle", Category = "Engine", DefaultCombo = "ctrl+b" },
            new BridgeActionEntry { Id = "engineBrakeUp", Label = "Engine brake up", Category = "Engine", DefaultCombo = "ctrl+[" },
            new BridgeActionEntry { Id = "engineBrakeDown", Label = "Engine brake down", Category = "Engine", DefaultCombo = "ctrl+]" },
            new BridgeActionEntry { Id = "retarderUp", Label = "Retarder up", Category = "Engine", DefaultCombo = ";" },
            new BridgeActionEntry { Id = "retarderDown", Label = "Retarder down", Category = "Engine", DefaultCombo = "'" },
            new BridgeActionEntry { Id = "autoRetarder", Label = "Auto retarder", Category = "Engine", DefaultCombo = "ctrl+r" },
            new BridgeActionEntry { Id = "cruiseControl", Label = "Cruise control", Category = "Engine", DefaultCombo = "c" },
            new BridgeActionEntry { Id = "light", Label = "Parking / side lights", Category = "Lights", DefaultCombo = "l" },
            new BridgeActionEntry { Id = "highBeam", Label = "High beam", Category = "Lights", DefaultCombo = "k" },
            new BridgeActionEntry { Id = "beacons", Label = "Beacon lights", Category = "Lights", DefaultCombo = "o" },
            new BridgeActionEntry { Id = "hazards", Label = "Hazard lights", Category = "Lights", DefaultCombo = "f" },
            new BridgeActionEntry { Id = "wipers", Label = "Wipers", Category = "Lights", DefaultCombo = "p" },
            new BridgeActionEntry { Id = "parkingBrake", Label = "Parking brake", Category = "Brakes", DefaultCombo = "space" },
            new BridgeActionEntry { Id = "blinkerLeft", Label = "Left turn signal", Category = "Signals", DefaultCombo = "," },
            new BridgeActionEntry { Id = "blinkerRight", Label = "Right turn signal", Category = "Signals", DefaultCombo = "." },
            new BridgeActionEntry { Id = "horn", Label = "Horn", Category = "Signals", DefaultCombo = "h" },
            new BridgeActionEntry { Id = "airHorn", Label = "Air horn", Category = "Signals", DefaultCombo = "n" },
            new BridgeActionEntry { Id = "diffLock", Label = "Differential lock", Category = "Truck", DefaultCombo = "v" },
            new BridgeActionEntry { Id = "liftAxle", Label = "Truck lift axle", Category = "Truck", DefaultCombo = "ctrl+a" },
            new BridgeActionEntry { Id = "trailer", Label = "Attach / detach trailer", Category = "Trailer", DefaultCombo = "t" },
            new BridgeActionEntry { Id = "trailerLiftAxle", Label = "Trailer lift axle", Category = "Trailer", DefaultCombo = "ctrl+t" },
            new BridgeActionEntry { Id = "suspFrontUp", Label = "Front suspension up", Category = "Suspension", DefaultCombo = "ctrl+1" },
            new BridgeActionEntry { Id = "suspFrontDown", Label = "Front suspension down", Category = "Suspension", DefaultCombo = "ctrl+2" },
            new BridgeActionEntry { Id = "suspRearUp", Label = "Rear suspension up", Category = "Suspension", DefaultCombo = "ctrl+3" },
            new BridgeActionEntry { Id = "suspRearDown", Label = "Rear suspension down", Category = "Suspension", DefaultCombo = "ctrl+4" },
            new BridgeActionEntry { Id = "suspTrailerUp", Label = "Trailer suspension up", Category = "Suspension", DefaultCombo = "ctrl+5" },
            new BridgeActionEntry { Id = "suspTrailerDown", Label = "Trailer suspension down", Category = "Suspension", DefaultCombo = "ctrl+6" },
            new BridgeActionEntry { Id = "suspReset", Label = "Reset suspension", Category = "Suspension", DefaultCombo = "ctrl+0" },
            new BridgeActionEntry { Id = "windowLeftOpen", Label = "Open left window", Category = "Cab", DefaultCombo = "ctrl+left" },
            new BridgeActionEntry { Id = "windowLeftClose", Label = "Close left window", Category = "Cab", DefaultCombo = "ctrl+right" },
            new BridgeActionEntry { Id = "windowRightOpen", Label = "Open right window", Category = "Cab", DefaultCombo = "ctrl+up" },
            new BridgeActionEntry { Id = "windowRightClose", Label = "Close right window", Category = "Cab", DefaultCombo = "ctrl+down" },
            new BridgeActionEntry { Id = "dashMode", Label = "Dashboard display mode", Category = "Cab", DefaultCombo = "i" },
            new BridgeActionEntry { Id = "photoMode", Label = "Photo mode", Category = "Cab", DefaultCombo = "ctrl+u" },
            new BridgeActionEntry { Id = "cabAdjust", Label = "Cab adjustment", Category = "Cab", DefaultCombo = "f4" },
            new BridgeActionEntry { Id = "services", Label = "Services / repair", Category = "Navigation", DefaultCombo = "f7" },
            new BridgeActionEntry { Id = "navZoomOut", Label = "Nav map zoom out", Category = "Navigation", DefaultCombo = "f5" },
            new BridgeActionEntry { Id = "navZoomIn", Label = "Nav map zoom in", Category = "Navigation", DefaultCombo = "f6" },
            new BridgeActionEntry { Id = "map", Label = "World map", Category = "Navigation", DefaultCombo = "m" },
            new BridgeActionEntry { Id = "menu", Label = "Pause / menu", Category = "Navigation", DefaultCombo = "esc" },
            new BridgeActionEntry { Id = "quickSave", Label = "Quick save", Category = "Navigation", DefaultCombo = "ctrl+f5" },
            new BridgeActionEntry { Id = "radio", Label = "Radio on/off", Category = "Radio", DefaultCombo = "r" },
            new BridgeActionEntry { Id = "radioNext", Label = "Next station", Category = "Radio", DefaultCombo = "pgdown" },
            new BridgeActionEntry { Id = "radioPrev", Label = "Previous station", Category = "Radio", DefaultCombo = "pgup" },
            new BridgeActionEntry { Id = "volumeUp", Label = "Radio volume up", Category = "Radio", DefaultCombo = "ctrl+pgup" },
            new BridgeActionEntry { Id = "volumeDown", Label = "Radio volume down", Category = "Radio", DefaultCombo = "ctrl+pgdown" },
            new BridgeActionEntry { Id = "radioStar", Label = "Favorite station", Category = "Radio", DefaultCombo = "ctrl+'" },
            new BridgeActionEntry { Id = "cam1", Label = "Camera 1", Category = "Cameras", DefaultCombo = "1" },
            new BridgeActionEntry { Id = "cam2", Label = "Camera 2", Category = "Cameras", DefaultCombo = "2" },
            new BridgeActionEntry { Id = "cam3", Label = "Camera 3", Category = "Cameras", DefaultCombo = "3" },
            new BridgeActionEntry { Id = "cam4", Label = "Camera 4", Category = "Cameras", DefaultCombo = "4" },
            new BridgeActionEntry { Id = "cam5", Label = "Camera 5", Category = "Cameras", DefaultCombo = "5" },
            new BridgeActionEntry { Id = "cam6", Label = "Camera 6", Category = "Cameras", DefaultCombo = "6" },
            new BridgeActionEntry { Id = "cam7", Label = "Camera 7", Category = "Cameras", DefaultCombo = "7" },
            new BridgeActionEntry { Id = "cam8", Label = "Camera 8", Category = "Cameras", DefaultCombo = "8" },
            new BridgeActionEntry { Id = "cam9", Label = "Camera 9", Category = "Cameras", DefaultCombo = "9" },
            new BridgeActionEntry { Id = "f5", Label = "Route Advisor tab 1", Category = "Route Advisor", DefaultCombo = "f5" },
            new BridgeActionEntry { Id = "f6", Label = "Route Advisor tab 2", Category = "Route Advisor", DefaultCombo = "f6" },
            new BridgeActionEntry { Id = "f7", Label = "Route Advisor tab 3", Category = "Route Advisor", DefaultCombo = "f7" },
            new BridgeActionEntry { Id = "f8", Label = "Route Advisor tab 4", Category = "Route Advisor", DefaultCombo = "f8" },
            new BridgeActionEntry { Id = "kbEnter", Label = "Keyboard overlay Enter", Category = "Keyboard overlay", DefaultCombo = "enter" },
            new BridgeActionEntry { Id = "kbEsc", Label = "Keyboard overlay Esc", Category = "Keyboard overlay", DefaultCombo = "esc" },
            new BridgeActionEntry { Id = "kbTab", Label = "Keyboard overlay Tab", Category = "Keyboard overlay", DefaultCombo = "tab" },
            new BridgeActionEntry { Id = "kbBackspace", Label = "Keyboard overlay Backspace", Category = "Keyboard overlay", DefaultCombo = "backspace" },
            new BridgeActionEntry { Id = "kbSpace", Label = "Keyboard overlay Space", Category = "Keyboard overlay", DefaultCombo = "space" }
        };

        public static IReadOnlyList<BridgeActionEntry> All => Entries;

        public static BridgeActionEntry Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            return Entries.FirstOrDefault(e => string.Equals(e.Id, id, System.StringComparison.OrdinalIgnoreCase));
        }

        public static Dictionary<string, string> DefaultKeys()
        {
            var dict = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var entry in Entries)
                dict[entry.Id] = entry.DefaultCombo;
            return dict;
        }

        public static bool IsValidCombo(string combo)
        {
            return SendInputHelper.IsValidCombo(combo);
        }
    }
}
