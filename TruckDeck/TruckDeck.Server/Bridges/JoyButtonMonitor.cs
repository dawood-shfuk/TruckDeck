using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Funbit.Ets.Telemetry.Server.Bridges
{
    /// <summary>
    /// Optional WinMM joystick button monitor for dash-only actions (e.g. screen cycle).
    /// Fails soft: if no device or invalid binding, monitoring stays disabled and the bridge keeps running.
    /// </summary>
    sealed class JoyButtonMonitor : IDisposable
    {
        static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JoyButtonMonitor));

        const int JoyReturnButtons = 0x00000080;
        const int MmNoError = 0;

        readonly object _gate = new object();
        Thread _thread;
        volatile bool _running;
        int _deviceIndex;
        int _buttonIndex;
        bool _wasDown;
        string _binding;
        string _disabledReason;

        public bool IsEnabled { get; private set; }
        public string Binding => _binding;
        public string DisabledReason => _disabledReason;

        public event Action OnPress;

        public void ApplyBinding(string binding)
        {
            Stop();
            _binding = string.IsNullOrWhiteSpace(binding) ? null : binding.Trim();
            IsEnabled = false;
            _disabledReason = null;

            if (string.IsNullOrEmpty(_binding))
            {
                _disabledReason = "not configured";
                return;
            }

            if (!TryParseBinding(_binding, out _deviceIndex, out _buttonIndex))
            {
                _disabledReason = "invalid binding";
                Log.WarnFormat("Input bridge: invalid screenCycleJoy binding '{0}' — joy buttons disabled", _binding);
                return;
            }

            if (!IsDeviceActive(_deviceIndex))
            {
                _disabledReason = "no joystick device";
                Log.WarnFormat(
                    "Input bridge: joystick device {0} not found for '{1}' — joy buttons disabled",
                    _deviceIndex + 1, _binding);
                return;
            }

            if (_buttonIndex < 0 || _buttonIndex >= 32)
            {
                _disabledReason = "button out of range";
                Log.WarnFormat(
                    "Input bridge: button {0} out of WinMM range for '{1}' — joy buttons disabled",
                    _buttonIndex + 1, _binding);
                return;
            }

            _running = true;
            _wasDown = false;
            IsEnabled = true;
            _thread = new Thread(PollLoop)
            {
                IsBackground = true,
                Name = "TruckDeck.JoyButtonMonitor"
            };
            _thread.Start();
            Log.InfoFormat(
                "Input bridge: joy monitor active on device {0}, button {1} ({2})",
                _deviceIndex + 1, _buttonIndex + 1, _binding);
        }

        public void Stop()
        {
            _running = false;
            var t = _thread;
            if (t != null && t.IsAlive && t != Thread.CurrentThread)
            {
                try { t.Join(1500); } catch { /* ignore */ }
            }
            _thread = null;
            IsEnabled = false;
        }

        public void Dispose() => Stop();

        void PollLoop()
        {
            while (_running)
            {
                try
                {
                    if (!IsDeviceActive(_deviceIndex))
                    {
                        if (_disabledReason == null)
                        {
                            _disabledReason = "device disconnected";
                            IsEnabled = false;
                            Log.WarnFormat("Input bridge: joystick {0} disconnected — joy buttons disabled", _deviceIndex + 1);
                        }
                        break;
                    }

                    var down = ReadButton(_deviceIndex, _buttonIndex);
                    if (down && !_wasDown)
                    {
                        var handler = OnPress;
                        if (handler != null)
                        {
                            try { handler(); } catch (Exception ex) { Log.Error("Joy button callback failed", ex); }
                        }
                    }
                    _wasDown = down;
                }
                catch (Exception ex)
                {
                    Log.Warn("Joy monitor read error — disabling joy buttons", ex);
                    _disabledReason = "read error";
                    IsEnabled = false;
                    break;
                }
                Thread.Sleep(40);
            }
            _running = false;
        }

        static bool TryParseBinding(string spec, out int deviceIndex, out int buttonIndex)
        {
            deviceIndex = 0;
            buttonIndex = 0;
            if (string.IsNullOrWhiteSpace(spec))
                return false;

            var text = spec.Trim().ToLowerInvariant();
            var m = Regex.Match(text, @"^joy(\d+)\.b(\d+)$");
            if (m.Success)
            {
                deviceIndex = int.Parse(m.Groups[1].Value) - 1;
                buttonIndex = int.Parse(m.Groups[2].Value) - 1;
                return deviceIndex >= 0 && buttonIndex >= 0;
            }
            m = Regex.Match(text, @"^joy\.b(\d+)$");
            if (m.Success)
            {
                buttonIndex = int.Parse(m.Groups[1].Value) - 1;
                return buttonIndex >= 0;
            }
            m = Regex.Match(text, @"^b(\d+)$");
            if (m.Success)
            {
                buttonIndex = int.Parse(m.Groups[1].Value) - 1;
                return buttonIndex >= 0;
            }
            m = Regex.Match(text, @"^(\d+)$");
            if (m.Success)
            {
                buttonIndex = int.Parse(m.Groups[1].Value) - 1;
                return buttonIndex >= 0;
            }
            return false;
        }

        static bool IsDeviceActive(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= joyGetNumDevs())
                return false;
            var info = new JoyInfoEx { dwSize = Marshal.SizeOf(typeof(JoyInfoEx)), dwFlags = JoyReturnButtons };
            return joyGetPosEx(deviceIndex, ref info) == MmNoError;
        }

        static bool ReadButton(int deviceIndex, int buttonIndex)
        {
            var info = new JoyInfoEx { dwSize = Marshal.SizeOf(typeof(JoyInfoEx)), dwFlags = JoyReturnButtons };
            if (joyGetPosEx(deviceIndex, ref info) != MmNoError)
                return false;
            if (buttonIndex < 0 || buttonIndex >= 32)
                return false;
            return (info.dwButtons & (1u << buttonIndex)) != 0;
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
    }
}
