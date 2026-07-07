"""Pad NPOT UI DDS textures to power-of-two for Steam Workshop validation."""
from __future__ import annotations

import struct
from pathlib import Path

from dds.dds import decode_dds
from PIL import Image


def next_pot(n: int) -> int:
    p = 1
    while p < n:
        p <<= 1
    return p


def write_rgba_dds(path: Path, img: Image.Image) -> None:
    img = img.convert("RGBA")
    w, h = img.size
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
    path.write_bytes(bytes(header) + img.tobytes())


def is_pot(n: int) -> bool:
    return n > 0 and (n & (n - 1)) == 0


def fix_tree(root: Path) -> int:
  fixed = 0
  ui = root / "material" / "ui" / "ps_gps"
  if not ui.is_dir():
      return 0

  for dds in sorted(ui.glob("*.dds")):
      raw = dds.read_bytes()
      if raw[:4] != b"DDS ":
          continue
      w = struct.unpack_from("<I", raw, 16)[0]
      h = struct.unpack_from("<I", raw, 12)[0]
      if is_pot(w) and is_pot(h):
          continue

      img = decode_dds(raw)
      nw = next_pot(w)
      nh = next_pot(h)
      canvas = Image.new("RGBA", (nw, nh), (0, 0, 0, 0))
      canvas.paste(img, ((nw - w) // 2, (nh - h) // 2))
      write_rgba_dds(dds, canvas)
      print(f"  POT {w}x{h} -> {nw}x{nh}  {dds.name}")
      fixed += 1

  return fixed


if __name__ == "__main__":
    import sys

    target = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent / "upload" / "universal"
    if not target.is_dir():
        raise SystemExit(f"Not found: {target}")
    count = fix_tree(target)
    print(f"Fixed {count} texture(s) in {target}")
