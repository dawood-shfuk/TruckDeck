using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    public static class JoyCaptureHelper
    {
        const int JoyReturnButtons = 0x00000080;
        const int MmNoError = 0;

        public static string FormatBinding(int deviceIndex, int buttonIndex)
        {
            return "joy" + (deviceIndex + 1) + ".b" + (buttonIndex + 1);
        }

        public static IList<JoyDeviceInfo> ListDevices()
        {
            var list = new List<JoyDeviceInfo>();
            var count = joyGetNumDevs();
            for (var i = 0; i < count; i++)
            {
                var caps = new JoyCaps { szPname = new string('\0', 32) };
                var name = "Joystick " + (i + 1);
                try
                {
                    if (joyGetDevCaps(i, ref caps, Marshal.SizeOf(typeof(JoyCaps))) == MmNoError
                        && !string.IsNullOrWhiteSpace(caps.szPname))
                    {
                        name = caps.szPname.TrimEnd('\0');
                    }
                }
                catch
                {
                    /* ignore */
                }

                list.Add(new JoyDeviceInfo
                {
                    Index = i,
                    Name = name,
                    Active = IsDeviceActive(i)
                });
            }
            return list;
        }

        public static JoyCaptureResult CaptureNextButton(int timeoutMs, CancellationToken cancel)
        {
            var devices = ListDevices();
            if (devices.Count == 0)
                throw new InvalidOperationException("No joystick devices found.");

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 60000);
            var previous = new uint[devices.Count];
            for (var d = 0; d < devices.Count; d++)
                previous[d] = ReadButtons(devices[d].Index);

            while (DateTime.UtcNow < deadline)
            {
                cancel.ThrowIfCancellationRequested();
                for (var d = 0; d < devices.Count; d++)
                {
                    if (!devices[d].Active)
                        continue;
                    var buttons = ReadButtons(devices[d].Index);
                    var edge = buttons & ~previous[d];
                    previous[d] = buttons;
                    if (edge == 0)
                        continue;
                    for (var b = 0; b < 32; b++)
                    {
                        if ((edge & (1u << b)) == 0)
                            continue;
                        return new JoyCaptureResult
                        {
                            Binding = FormatBinding(devices[d].Index, b),
                            DeviceIndex = devices[d].Index,
                            ButtonIndex = b,
                            DeviceName = devices[d].Name
                        };
                    }
                }
                Thread.Sleep(25);
            }

            throw new TimeoutException("No joystick button press detected.");
        }

        static bool IsDeviceActive(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= joyGetNumDevs())
                return false;
            var info = new JoyInfoEx { dwSize = Marshal.SizeOf(typeof(JoyInfoEx)), dwFlags = JoyReturnButtons };
            return joyGetPosEx(deviceIndex, ref info) == MmNoError;
        }

        static uint ReadButtons(int deviceIndex)
        {
            var info = new JoyInfoEx { dwSize = Marshal.SizeOf(typeof(JoyInfoEx)), dwFlags = JoyReturnButtons };
            if (joyGetPosEx(deviceIndex, ref info) != MmNoError)
                return 0;
            return info.dwButtons;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct JoyCaps
        {
            public ushort wMid;
            public ushort wPid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint wXmin;
            public uint wXmax;
            public uint wYmin;
            public uint wYmax;
            public uint wZmin;
            public uint wZmax;
            public uint wNumButtons;
            public uint wPeriodMin;
            public uint wPeriodMax;
            public uint wRmin;
            public uint wRmax;
            public uint wUmin;
            public uint wUmax;
            public uint wVmin;
            public uint wVmax;
            public uint wCaps;
            public uint wMaxAxes;
            public uint wNumAxes;
            public uint wMaxButtons;
            public uint szRegKey;
            public uint szOEMVxD;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct JoyInfoEx
        {
            public int dwSize;
            public int dwFlags;
            public uint dwXpos;
            public uint dwYpos;
            public uint dwZpos;
            public uint dwRpos;
            public uint dwUpos;
            public uint dwVpos;
            public uint dwButtons;
            public uint dwButtonNumber;
            public uint dwPOV;
            public uint dwReserved1;
            public uint dwReserved2;
        }

        [DllImport("winmm.dll")]
        static extern int joyGetNumDevs();

        [DllImport("winmm.dll")]
        static extern int joyGetPosEx(int uJoyId, ref JoyInfoEx pji);

        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        static extern int joyGetDevCaps(int uJoyId, ref JoyCaps pjc, int cbjc);
    }
}
