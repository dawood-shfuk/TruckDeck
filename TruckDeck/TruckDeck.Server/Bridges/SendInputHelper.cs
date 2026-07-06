using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    static class SendInputHelper
    {
        const int InputKeyboard = 1;
        const int InputMouse = 0;
        const uint KeyeventfExtendedkey = 0x0001;
        const uint KeyeventfKeyup = 0x0002;
        const uint KeyeventfScancode = 0x0008;

        const uint MouseeventfMove = 0x0001;
        const uint MouseeventfLeftdown = 0x0002;
        const uint MouseeventfLeftup = 0x0004;
        const uint MouseeventfRightdown = 0x0008;
        const uint MouseeventfRightup = 0x0010;
        const uint MouseeventfMiddledown = 0x0020;
        const uint MouseeventfMiddleup = 0x0040;
        const uint MouseeventfWheel = 0x0800;
        const uint MouseeventfMoveNocoalesce = 0x2000;
        const int MouseDeltaClamp = 500;

        static readonly Dictionary<string, ushort> Scancodes = BuildScancodes();
        static readonly Dictionary<string, ushort> ExtendedKeys = BuildExtendedKeys();
        static readonly Dictionary<string, Tuple<ushort, bool>> Modifiers = BuildModifiers();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static bool TapCombo(string combo, int holdMs)
        {
            if (!TryParseCombo(combo, out var mainScan, out var mainExt, out var mods))
                return false;

            foreach (var mod in mods)
                SendScancode(mod.Item1, false, mod.Item2);

            SendScancode(mainScan, false, mainExt);
            if (holdMs > 0)
                System.Threading.Thread.Sleep(holdMs);
            SendScancode(mainScan, true, mainExt);

            for (int i = mods.Count - 1; i >= 0; i--)
                SendScancode(mods[i].Item1, true, mods[i].Item2);

            return true;
        }

        public static bool TryParseCombo(string combo, out ushort mainScan, out bool mainExt, out List<Tuple<ushort, bool>> mods)
        {
            mainScan = 0;
            mainExt = false;
            mods = new List<Tuple<ushort, bool>>();

            var parts = (combo ?? "").Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim().ToLowerInvariant();

            if (!TryResolveKey(parts[parts.Length - 1], out mainScan, out mainExt))
                return false;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (Modifiers.TryGetValue(parts[i], out var mod))
                    mods.Add(mod);
                else if (TryResolveKey(parts[i], out var sc, out var ext))
                    mods.Add(Tuple.Create(sc, ext));
                else
                    return false;
            }

            return true;
        }

        public static bool IsValidCombo(string combo) => TryParseCombo(combo, out _, out _, out _);

        public static bool MouseMove(int dx, int dy)
        {
            dx = Clamp(dx);
            dy = Clamp(dy);
            if (dx == 0 && dy == 0)
                return true;
            return SendMouse(MouseeventfMove | MouseeventfMoveNocoalesce, dx, dy, 0);
        }

        public static bool MouseClick(string button, string state)
        {
            uint flags = 0;
            var key = (button?.Trim().ToLowerInvariant(), state?.Trim().ToLowerInvariant());
            if (key == ("left", "down")) flags = MouseeventfLeftdown;
            else if (key == ("left", "up")) flags = MouseeventfLeftup;
            else if (key == ("right", "down")) flags = MouseeventfRightdown;
            else if (key == ("right", "up")) flags = MouseeventfRightup;
            else if (key == ("middle", "down")) flags = MouseeventfMiddledown;
            else if (key == ("middle", "up")) flags = MouseeventfMiddleup;
            else return false;
            return SendMouse(flags, 0, 0, 0);
        }

        public static bool MouseScroll(int delta)
        {
            if (delta == 0)
                return true;
            return SendMouse(MouseeventfWheel, 0, 0, delta);
        }

        static bool TryResolveKey(string name, out ushort scancode, out bool extended)
        {
            scancode = 0;
            extended = false;
            if (string.IsNullOrWhiteSpace(name))
                return false;
            var key = name.Trim().ToLowerInvariant();
            if (ExtendedKeys.TryGetValue(key, out scancode))
            {
                extended = true;
                return true;
            }
            if (Scancodes.TryGetValue(key, out scancode))
                return true;
            return false;
        }

        static void SendScancode(ushort scancode, bool keyUp, bool extended)
        {
            uint flags = KeyeventfScancode;
            if (extended) flags |= KeyeventfExtendedkey;
            if (keyUp) flags |= KeyeventfKeyup;

            var input = new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = scancode,
                        dwFlags = flags
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        static bool SendMouse(uint flags, int dx, int dy, int mouseData)
        {
            var input = new INPUT
            {
                type = InputMouse,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = dx,
                        dy = dy,
                        mouseData = (uint)mouseData,
                        dwFlags = flags
                    }
                }
            };
            return SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT))) == 1;
        }

        static int Clamp(int value)
        {
            if (value > MouseDeltaClamp) return MouseDeltaClamp;
            if (value < -MouseDeltaClamp) return -MouseDeltaClamp;
            return value;
        }

        static Dictionary<string, ushort> BuildScancodes() => new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["esc"] = 0x01, ["1"] = 0x02, ["2"] = 0x03, ["3"] = 0x04, ["4"] = 0x05, ["5"] = 0x06,
            ["6"] = 0x07, ["7"] = 0x08, ["8"] = 0x09, ["9"] = 0x0A, ["0"] = 0x0B, ["-"] = 0x0C,
            ["="] = 0x0D, ["backspace"] = 0x0E, ["tab"] = 0x0F, ["q"] = 0x10, ["w"] = 0x11,
            ["e"] = 0x12, ["r"] = 0x13, ["t"] = 0x14, ["y"] = 0x15, ["u"] = 0x16, ["i"] = 0x17,
            ["o"] = 0x18, ["p"] = 0x19, ["["] = 0x1A, ["]"] = 0x1B, ["enter"] = 0x1C, ["ctrl"] = 0x1D,
            ["lctrl"] = 0x1D, ["a"] = 0x1E, ["s"] = 0x1F, ["d"] = 0x20, ["f"] = 0x21, ["g"] = 0x22,
            ["h"] = 0x23, ["j"] = 0x24, ["k"] = 0x25, ["l"] = 0x26, [";"] = 0x27, ["'"] = 0x28,
            ["`"] = 0x29, ["shift"] = 0x2A, ["lshift"] = 0x2A, ["\\"] = 0x2B, ["z"] = 0x2C,
            ["x"] = 0x2D, ["c"] = 0x2E, ["v"] = 0x2F, ["b"] = 0x30, ["n"] = 0x31, ["m"] = 0x32,
            [","] = 0x33, ["."] = 0x34, ["/"] = 0x35, ["rshift"] = 0x36, ["alt"] = 0x38,
            ["lalt"] = 0x38, ["space"] = 0x39, ["capslock"] = 0x3A,
            ["f1"] = 0x3B, ["f2"] = 0x3C, ["f3"] = 0x3D, ["f4"] = 0x3E, ["f5"] = 0x3F,
            ["f6"] = 0x40, ["f7"] = 0x41, ["f8"] = 0x42, ["f9"] = 0x43, ["f10"] = 0x44,
            ["f11"] = 0x57, ["f12"] = 0x58,
            ["num0"] = 0x52, ["num1"] = 0x4F, ["num2"] = 0x50, ["num3"] = 0x51, ["num4"] = 0x4B,
            ["num5"] = 0x4C, ["num6"] = 0x4D, ["num7"] = 0x47, ["num8"] = 0x48, ["num9"] = 0x49,
            ["num."] = 0x53, ["num*"] = 0x37, ["num-"] = 0x4A, ["num+"] = 0x4E
        };

        static Dictionary<string, ushort> BuildExtendedKeys() => new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["up"] = 0x48, ["down"] = 0x50, ["left"] = 0x4B, ["right"] = 0x4D,
            ["insert"] = 0x52, ["delete"] = 0x53, ["home"] = 0x47, ["end"] = 0x4F,
            ["pageup"] = 0x49, ["pagedown"] = 0x51, ["pgup"] = 0x49, ["pgdn"] = 0x51, ["pgdown"] = 0x51,
            ["rctrl"] = 0x1D, ["ralt"] = 0x38, ["numenter"] = 0x1C, ["num/"] = 0x35
        };

        static Dictionary<string, Tuple<ushort, bool>> BuildModifiers() => new Dictionary<string, Tuple<ushort, bool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ctrl"] = Tuple.Create((ushort)0x1D, false),
            ["control"] = Tuple.Create((ushort)0x1D, false),
            ["lctrl"] = Tuple.Create((ushort)0x1D, false),
            ["rctrl"] = Tuple.Create((ushort)0x1D, true),
            ["shift"] = Tuple.Create((ushort)0x2A, false),
            ["lshift"] = Tuple.Create((ushort)0x2A, false),
            ["rshift"] = Tuple.Create((ushort)0x36, false),
            ["alt"] = Tuple.Create((ushort)0x38, false),
            ["lalt"] = Tuple.Create((ushort)0x38, false),
            ["ralt"] = Tuple.Create((ushort)0x38, true),
            ["win"] = Tuple.Create((ushort)0x5B, true),
            ["lwin"] = Tuple.Create((ushort)0x5B, true),
            ["rwin"] = Tuple.Create((ushort)0x5C, true)
        };

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
