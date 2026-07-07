"""Fix TruckDeck NAV assets for Steam Workshop validation."""
from __future__ import annotations

import struct
import sys
from pathlib import Path


def write_rgba_dds(path: Path, size: int = 256, color=(128, 128, 128, 255)) -> None:
    w = h = size
    header = bytearray(128)
    header[0:4] = b"DDS "
    struct.pack_into("<I", header, 4, 124)
    struct.pack_into("<I", header, 8, 0x1007)
    struct.pack_into("<I", header, 12, h)
    struct.pack_into("<I", header, 16, w)
    struct.pack_into("<I", header, 20, w * 4)
    struct.pack_into("<I", header, 28, 1)
    struct.pack_into("<I", header, 76, 32)
    struct.pack_into("<I", header, 80, 0x41)
    struct.pack_into("<I", header, 88, 32)
    struct.pack_into("<I", header, 92, 0x00FF0000)
    struct.pack_into("<I", header, 96, 0x0000FF00)
    struct.pack_into("<I", header, 100, 0x000000FF)
    struct.pack_into("<I", header, 104, 0xFF000000)
    r, g, b, a = color
    px = bytes([b, g, r, a]) * (w * h)
    path.write_bytes(bytes(header) + px)


def write_white_dds(path: Path) -> None:
    write_rgba_dds(path, size=4, color=(255, 255, 255, 255))


def build_2d_tobj(dds_path: str, template: bytes) -> bytes:
    path_bytes = dds_path.encode("ascii")
    out = bytearray(template[:48])
    struct.pack_into("<I", out, 40, len(path_bytes))
    out.extend(path_bytes)
    return bytes(out)


def fix_interior_reflection(ps_gps: Path) -> None:
    template = (ps_gps / "box_128.tobj").read_bytes()
    dds_path = "/vehicle/truck/upgrade/ps_gps/interior_reflection.dds"
    tobj = build_2d_tobj(dds_path, template)
    (ps_gps / "interior_reflection.tobj").write_bytes(tobj)
    write_rgba_dds(ps_gps / "interior_reflection.dds", size=256)
    print("  fixed interior_reflection.tobj (2D) + interior_reflection.dds")


def patch_white_tobj(tobj_path: Path) -> None:
    ref = bytearray((tobj_path.parent / "ps_gps" / "cc_icon.tobj").read_bytes())
    old = b"/material/ui/ps_gps/cc_icon.dds"
    new = b"/material/ui/white.dds"
    idx = ref.find(old)
    if idx < 0:
        raise ValueError("template path not found in cc_icon.tobj")
    ref[idx : idx + len(old)] = new + b"\x00" * (len(old) - len(new))
    tobj_path.write_bytes(ref)


def cleanup(root: Path) -> None:
    for stray in root.rglob("cc_icon_pot.dds"):
        stray.unlink()
        print(f"  removed {stray.relative_to(root)}")


def fix_white_material(root: Path) -> None:
    ui = root / "material" / "ui"
    write_white_dds(ui / "white.dds")
    patch_white_tobj(ui / "white.tobj")
    print("  fixed material/ui/white.dds + white.tobj")


def fix_workshop_assets(root: Path) -> None:
    ps_gps = root / "vehicle" / "truck" / "upgrade" / "ps_gps"
    if ps_gps.is_dir():
        fix_interior_reflection(ps_gps)


if __name__ == "__main__":
    argv = sys.argv[1:]
    workshop = "--workshop" in argv
    argv = [a for a in argv if a != "--workshop"]
    target = Path(argv[0]) if argv else Path(__file__).resolve().parent.parent
    cleanup(target)
    fix_white_material(target)
    if workshop:
        fix_workshop_assets(target)
    print(f"Asset fixes applied under {target}")
