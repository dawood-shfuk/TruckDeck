"""
DirectInput joystick button polling (Windows, stdlib only).
Used by the input bridge for dash-only actions (e.g. screenCycleJoy: joy6.b69).
"""

import ctypes
import re
import threading
import time
from ctypes import (
    POINTER,
    WINFUNCTYPE,
    Structure,
    byref,
    c_long,
    c_ulong,
    c_ubyte,
    c_void_p,
    cast,
    sizeof,
)
from ctypes import wintypes

DI_OK = 0
DIENUM_CONTINUE = 1
DIRECTINPUT_VERSION = 0x0800
DI8DEVCLASS_GAMECTRL = 0x04
DISCL_BACKGROUND = 0x00000008
DISCL_NONEXCLUSIVE = 0x00000010
_dinput_ready = False
_backend = None

# WinMM fallback (legacy joystick API)
MMSYSERR_NOERROR = 0
JOY_RETURNBUTTONS = 0x00000080
MAXPNAMELEN = 32
_winmm = ctypes.windll.winmm


class JOYINFOEX(Structure):
    _fields_ = [
        ("dwSize", wintypes.DWORD),
        ("dwFlags", wintypes.DWORD),
        ("dwXpos", wintypes.DWORD),
        ("dwYpos", wintypes.DWORD),
        ("dwZpos", wintypes.DWORD),
        ("dwRpos", wintypes.DWORD),
        ("dwUpos", wintypes.DWORD),
        ("dwVpos", wintypes.DWORD),
        ("dwButtons", wintypes.DWORD),
        ("dwButtonNumber", wintypes.DWORD),
        ("dwPOV", wintypes.DWORD),
        ("dwReserved1", wintypes.DWORD),
        ("dwReserved2", wintypes.DWORD),
    ]


class JOYCAPSW(Structure):
    _fields_ = [
        ("wMid", wintypes.WORD),
        ("wPid", wintypes.WORD),
        ("szPname", wintypes.WCHAR * MAXPNAMELEN),
        ("wXmin", wintypes.UINT),
        ("wXmax", wintypes.UINT),
        ("wYmin", wintypes.UINT),
        ("wYmax", wintypes.UINT),
        ("wZmin", wintypes.UINT),
        ("wZmax", wintypes.UINT),
        ("wNumButtons", wintypes.UINT),
        ("wPeriodMin", wintypes.UINT),
        ("wPeriodMax", wintypes.UINT),
        ("wRmin", wintypes.UINT),
        ("wRmax", wintypes.UINT),
        ("wUmin", wintypes.UINT),
        ("wUmax", wintypes.UINT),
        ("wVmin", wintypes.UINT),
        ("wVmax", wintypes.UINT),
        ("wCaps", wintypes.UINT),
        ("wMaxAxes", wintypes.UINT),
        ("wNumAxes", wintypes.UINT),
        ("dwSize", wintypes.DWORD),
        ("dwFlags", wintypes.DWORD),
        ("dwDevType", wintypes.DWORD),
        ("dwXSize", wintypes.DWORD),
        ("dwYSize", wintypes.DWORD),
        ("dwZSize", wintypes.DWORD),
        ("dwRSize", wintypes.DWORD),
        ("dwUSize", wintypes.DWORD),
        ("dwVSize", wintypes.DWORD),
    ]


class GUID(Structure):
    _fields_ = [
        ("Data1", c_ulong),
        ("Data2", wintypes.USHORT),
        ("Data3", wintypes.USHORT),
        ("Data4", c_ubyte * 8),
    ]


IID_IDirectInput8W = GUID(0xBF798031, 0x483A, 0x4DA2, (0xAA, 0x99, 0x5D, 0x64, 0xED, 0x36, 0x97, 0x00))
IID_IDirectInputDevice8W = GUID(0x54D41080, 0xDC1E, 0x4DD8, (0xA3, 0x7D, 0x0F, 0x2D, 0xF0, 0x59, 0xC1, 0x2E))
GUID_Joystick = GUID(0x6F1D2B60, 0xD5A0, 0x11CF, (0xBF, 0xC7, 0x44, 0x45, 0x53, 0x54, 0x00, 0x00))


def _hr_hex(hr):
    return hex(hr & 0xFFFFFFFF)


def _setup_dinput():
    """Configure DirectInput8Create argtypes (required on 64-bit Python)."""
    global _dinput_ready
    if _dinput_ready:
        return
    dinput8 = ctypes.windll.dinput8
    dinput8.DirectInput8Create.argtypes = [
        wintypes.HINSTANCE,
        c_ulong,
        POINTER(GUID),
        POINTER(c_void_p),
        c_void_p,
    ]
    dinput8.DirectInput8Create.restype = c_long
    _dinput_ready = True


def _create_direct_input():
    _setup_dinput()
    ole32 = ctypes.windll.ole32
    ole32.CoInitialize(None)
    hinst = ctypes.windll.kernel32.GetModuleHandleW(None)
    di_ptr = c_void_p()
    hr = ctypes.windll.dinput8.DirectInput8Create(
        hinst,
        DIRECTINPUT_VERSION,
        byref(IID_IDirectInput8W),
        byref(di_ptr),
        None,
    )
    if hr != DI_OK or not di_ptr.value:
        raise OSError(f"DirectInput8Create failed (hr={hr}, {_hr_hex(hr)})")
    return di_ptr


def _release_direct_input_ptr(di_ptr):
    if not di_ptr:
        return
    try:
        release = _vtbl_fn(di_ptr, 2, c_ulong)
        release(di_ptr)
    except Exception:  # noqa: BLE001
        pass


def _probe_direct_input():
    try:
        di_ptr = _create_direct_input()
        _release_direct_input_ptr(di_ptr)
        return True
    except OSError:
        return False


def _get_backend():
    global _backend
    if _backend is None:
        _backend = "dinput" if _probe_direct_input() else "winmm"
    return _backend


def backend_name():
    return _get_backend()


def _winmm_device_active(joy_id):
    info = JOYINFOEX()
    info.dwSize = sizeof(JOYINFOEX)
    info.dwFlags = JOY_RETURNBUTTONS
    return _winmm.joyGetPosEx(joy_id, byref(info)) == MMSYSERR_NOERROR


def _winmm_device_name(joy_id):
    caps = JOYCAPSW()
    caps.dwSize = sizeof(JOYCAPSW)
    if _winmm.joyGetDevCapsW(joy_id, byref(caps), sizeof(caps)) == MMSYSERR_NOERROR:
        name = caps.szPname.strip()
        if name:
            return name
    return f"Joystick {joy_id + 1}"


def _winmm_read_pressed_buttons(joy_id):
    info = JOYINFOEX()
    info.dwSize = sizeof(JOYINFOEX)
    info.dwFlags = JOY_RETURNBUTTONS
    if _winmm.joyGetPosEx(joy_id, byref(info)) != MMSYSERR_NOERROR:
        return []
    pressed = []
    for idx in range(32):
        if info.dwButtons & (1 << idx):
            pressed.append(idx)
    return pressed


def _winmm_read_button(joy_id, button_index):
    if button_index < 0 or button_index >= 32:
        return False
    info = JOYINFOEX()
    info.dwSize = sizeof(JOYINFOEX)
    info.dwFlags = JOY_RETURNBUTTONS
    if _winmm.joyGetPosEx(joy_id, byref(info)) != MMSYSERR_NOERROR:
        return False
    return bool(info.dwButtons & (1 << button_index))


def _list_joystick_devices_winmm():
    devices = []
    for joy_id in range(_winmm.joyGetNumDevs()):
        if _winmm_device_active(joy_id):
            devices.append((joy_id, _winmm_device_name(joy_id)))
    return devices


class DIDEVICEINSTANCEW(Structure):
    _fields_ = [
        ("dwSize", c_ulong),
        ("guidInstance", GUID),
        ("guidProduct", GUID),
        ("dwDevType", c_ulong),
        ("tszInstanceName", wintypes.WCHAR * 260),
        ("tszProductName", wintypes.WCHAR * 260),
        ("guidFFDriver", GUID),
        ("wUsagePage", wintypes.USHORT),
        ("wUsage", wintypes.USHORT),
    ]


class DIJOYSTATE2(Structure):
    _fields_ = [
        ("lX", c_long),
        ("lY", c_long),
        ("lZ", c_long),
        ("lRx", c_long),
        ("lRy", c_long),
        ("lRz", c_long),
        ("rglSlider", c_long * 2),
        ("rgdwPOV", c_ulong * 4),
        ("rgbButtons", c_ubyte * 128),
    ]


class DIDATAFORMAT(Structure):
    _fields_ = [
        ("dwSize", c_ulong),
        ("dwObjSize", c_ulong),
        ("dwFlags", c_ulong),
        ("dwDataSize", c_ulong),
        ("dwNumObjs", c_ulong),
        ("rgodf", c_void_p),
    ]


def _vtbl_fn(obj, index, restype, *argtypes):
    vtbl = cast(cast(obj, c_void_p), POINTER(c_void_p))[index]
    return WINFUNCTYPE(restype, c_void_p, *argtypes)(vtbl)


def parse_joy_binding(spec):
    """
    Parse ETS2-style joystick bindings.
      joy6.b69  -> device 5 (0-based), button 68 (0-based)
      joy.b12   -> device 0, button 11
      b69 / 69  -> device 0, button 68
    Returns (device_index, button_index) or None if empty/invalid.
    """
    if spec is None:
        return None
    text = str(spec).strip().lower()
    if not text:
        return None
    m = re.match(r"^joy(\d+)\.b(\d+)$", text)
    if m:
        return int(m.group(1)) - 1, int(m.group(2)) - 1
    m = re.match(r"^joy\.b(\d+)$", text)
    if m:
        return 0, int(m.group(1)) - 1
    m = re.match(r"^b(\d+)$", text)
    if m:
        return 0, int(m.group(1)) - 1
    m = re.match(r"^(\d+)$", text)
    if m:
        return 0, int(m.group(1)) - 1
    return None


_joy_warned_bindings = set()


def _warn_joy_once(binding, message):
    if binding in _joy_warned_bindings:
        return
    _joy_warned_bindings.add(binding)
    print(message)


class JoyPoller:
    """Poll one joystick button; calls on_press on rising edge."""

    def __init__(self, binding, on_press, interval_ms=40):
        parsed = parse_joy_binding(binding)
        if parsed is None:
            raise ValueError("invalid or empty joy binding")
        self.binding = str(binding).strip()
        self.device_index, self.button_index = parsed
        self.on_press = on_press
        self.interval = max(10, interval_ms) / 1000.0
        self._stop = threading.Event()
        self._thread = None
        self._was_down = False
        self._di = None
        self._dev = None
        self._backend = None

    def start(self):
        if self._thread and self._thread.is_alive():
            return
        self._stop.clear()
        self._thread = threading.Thread(target=self._run, name="joy-poller", daemon=True)
        self._thread.start()

    def stop(self):
        self._stop.set()
        if self._thread:
            self._thread.join(timeout=2)
            self._thread = None
        self._release_device()

    def _release_device(self):
        if self._dev:
            try:
                unacquire = _vtbl_fn(self._dev, 2 + 5, c_long)  # Unacquire
                unacquire(self._dev)
            except Exception:  # noqa: BLE001
                pass
            try:
                release = _vtbl_fn(self._dev, 2, c_ulong)  # Release
                release(self._dev)
            except Exception:  # noqa: BLE001
                pass
            self._dev = None
        if self._di:
            try:
                release = _vtbl_fn(self._di, 2, c_ulong)
                release(self._di)
            except Exception:  # noqa: BLE001
                pass
            self._di = None

    def _open_device(self):
        global _backend
        dinput_error = None

        if _get_backend() == "dinput":
            try:
                self._backend = "dinput"
                self._di = _create_direct_input()
                instances = _enum_joystick_instances(self._di)
                if self.device_index < 0 or self.device_index >= len(instances):
                    raise OSError(
                        f"joystick device index {self.device_index + 1} not found "
                        f"({len(instances)} device(s) detected)"
                    )
                guid, _name = instances[self.device_index]
                self._dev = _open_joystick_device(self._di, guid)
                return
            except OSError as exc:
                dinput_error = exc
                self._release_device()
                _backend = "winmm"

        self._backend = "winmm"
        if self.button_index >= 32:
            msg = (
                f"button {self.button_index + 1} needs DirectInput "
                f"(WinMM supports buttons 1-32 only)"
            )
            if dinput_error:
                msg = f"{dinput_error}; {msg}"
            raise OSError(msg)
        if not _winmm_device_active(self.device_index):
            msg = (
                f"joystick device index {self.device_index + 1} not found "
                f"(WinMM backend)"
            )
            if dinput_error:
                msg = f"{dinput_error}; {msg}"
            raise OSError(msg)

    def _read_button(self):
        if self._backend == "winmm":
            return _winmm_read_button(self.device_index, self.button_index)

        state = DIJOYSTATE2()
        get_state = _vtbl_fn(self._dev, 7, c_long, c_ulong, c_void_p)
        hr = get_state(self._dev, sizeof(state), byref(state))
        if hr != DI_OK:
            return False
        idx = self.button_index
        if idx < 0 or idx >= len(state.rgbButtons):
            return False
        return bool(state.rgbButtons[idx] & 0x80)

    def _run(self):
        try:
            ctypes.windll.ole32.CoInitialize(None)
        except Exception:  # noqa: BLE001
            pass
        try:
            self._open_device()
            backend = self._backend or _get_backend()
            print(
                f"[bridge] joy poll ({backend}): device {self.device_index + 1}, "
                f"button {self.button_index + 1}"
            )
            _joy_warned_bindings.discard(self.binding)
        except Exception as exc:  # noqa: BLE001
            _warn_joy_once(self.binding, f"[bridge] joy poll failed: {exc}")
            return

        while not self._stop.is_set():
            try:
                down = self._read_button()
                if down and not self._was_down:
                    try:
                        self.on_press()
                    except Exception as exc:  # noqa: BLE001
                        print(f"[bridge] joy callback error: {exc}")
                self._was_down = down
            except Exception as exc:  # noqa: BLE001
                print(f"[bridge] joy read error: {exc}")
            time.sleep(self.interval)

        if self._backend != "winmm":
            self._release_device()


def format_joy_binding(device_index, button_index):
    """Format 0-based indices as ETS2-style joy6.b69 (1-based in output)."""
    return f"joy{device_index + 1}.b{button_index + 1}"


def _enum_joystick_instances(di_ptr):
    devices = []

    def enum_cb(instance_ptr, _arg):
        inst = cast(instance_ptr, POINTER(DIDEVICEINSTANCEW)).contents
        devices.append((inst.guidInstance, inst.tszProductName))
        return DIENUM_CONTINUE

    enum_devices = _vtbl_fn(
        di_ptr, 4, c_long, POINTER(DIDEVICEINSTANCEW), c_void_p, c_ulong
    )
    enum_devices(di_ptr, enum_cb, None, DI8DEVCLASS_GAMECTRL, 0)
    return devices


def list_joystick_devices():
    """Return [(index, product_name), ...] for connected game controllers."""
    if _get_backend() == "winmm":
        return _list_joystick_devices_winmm()

    di_ptr = _create_direct_input()
    try:
        instances = _enum_joystick_instances(di_ptr)
        return [(idx, name) for idx, (_guid, name) in enumerate(instances)]
    finally:
        _release_direct_input_ptr(di_ptr)


def _open_joystick_device(di_ptr, guid):
    dev_ptr = c_void_p()
    create_device = _vtbl_fn(di_ptr, 3, c_long, POINTER(GUID), POINTER(c_void_p), c_void_p)
    hr = create_device(di_ptr, byref(guid), byref(dev_ptr), None)
    if hr != DI_OK or not dev_ptr.value:
        raise OSError(f"CreateDevice failed (hr={hr})")

    fmt = DIDATAFORMAT()
    fmt.dwSize = sizeof(DIDATAFORMAT)
    fmt.dwObjSize = 16
    fmt.dwFlags = 0x00000002
    fmt.dwDataSize = sizeof(DIJOYSTATE2)
    fmt.dwNumObjs = 0
    fmt.rgodf = None

    set_format = _vtbl_fn(dev_ptr, 9, c_long, POINTER(DIDATAFORMAT))
    hr = set_format(dev_ptr, byref(fmt))
    if hr != DI_OK:
        raise OSError(f"SetDataFormat failed (hr={hr})")

    set_coop = _vtbl_fn(dev_ptr, 11, c_long, wintypes.HWND, c_ulong)
    hr = set_coop(dev_ptr, None, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE)
    if hr != DI_OK:
        raise OSError(f"SetCooperativeLevel failed (hr={hr})")

    acquire = _vtbl_fn(dev_ptr, 2 + 6, c_long)
    hr = acquire(dev_ptr)
    if hr != DI_OK and hr != 0x8007001E:
        raise OSError(f"Acquire failed (hr={hr})")
    return dev_ptr


def _release_joystick_device(dev_ptr):
    if not dev_ptr:
        return
    try:
        unacquire = _vtbl_fn(dev_ptr, 2 + 5, c_long)
        unacquire(dev_ptr)
    except Exception:  # noqa: BLE001
        pass
    try:
        release = _vtbl_fn(dev_ptr, 2, c_ulong)
        release(dev_ptr)
    except Exception:  # noqa: BLE001
        pass


def _read_pressed_buttons(dev_ptr):
    state = DIJOYSTATE2()
    get_state = _vtbl_fn(dev_ptr, 7, c_long, c_ulong, c_void_p)
    hr = get_state(dev_ptr, sizeof(state), byref(state))
    if hr != DI_OK:
        return []
    pressed = []
    for idx, val in enumerate(state.rgbButtons):
        if val & 0x80:
            pressed.append(idx)
    return pressed


def capture_next_button(poll_ms=30):
    """
    Wait for the next joystick button press on any connected device.
    Returns (device_index, button_index, device_name) using 0-based indices.
    """
    if _get_backend() == "winmm":
        return _capture_next_button_winmm(poll_ms)

    di_ptr = _create_direct_input()
    opened = []
    try:
        instances = _enum_joystick_instances(di_ptr)
        if not instances:
            raise OSError("No joystick / game controller devices found")

        for idx, (guid, name) in enumerate(instances):
            try:
                dev_ptr = _open_joystick_device(di_ptr, guid)
                opened.append((idx, dev_ptr, name))
            except OSError as exc:
                print(f"[capture] skipped device {idx + 1} ({name}): {exc}")

        if not opened:
            raise OSError("Could not open any joystick devices")

        prev = {idx: set() for idx, _dev, _name in opened}
        interval = max(10, poll_ms) / 1000.0

        while True:
            for dev_idx, dev_ptr, name in opened:
                current = set(_read_pressed_buttons(dev_ptr))
                rising = current - prev[dev_idx]
                prev[dev_idx] = current
                if rising:
                    button_index = min(rising)
                    return dev_idx, button_index, name
            time.sleep(interval)
    finally:
        for _idx, dev_ptr, _name in opened:
            _release_joystick_device(dev_ptr)
        _release_direct_input_ptr(di_ptr)


def _capture_next_button_winmm(poll_ms=30):
    devices = _list_joystick_devices_winmm()
    if not devices:
        raise OSError("No joystick / game controller devices found (WinMM)")

    prev = {joy_id: set() for joy_id, _name in devices}
    interval = max(10, poll_ms) / 1000.0

    while True:
        for joy_id, name in devices:
            current = set(_winmm_read_pressed_buttons(joy_id))
            rising = current - prev[joy_id]
            prev[joy_id] = current
            if rising:
                return joy_id, min(rising), name
        time.sleep(interval)
