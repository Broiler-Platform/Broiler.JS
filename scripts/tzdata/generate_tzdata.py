#!/usr/bin/env python3
"""Generate the bundled IANA time-zone database resource (tzdata.bin) for Broiler.JS.

Reads the system IANA database (TZif files + the compiled-text `tzdata.zi` zone/link
list) and packs it into a single raw-DEFLATE-compressed blob that the engine's TzifReader
parses at runtime, making Temporal / Date time-zone behaviour independent of the host OS
tz database.

Pack layout (after raw-inflate):
    "TZDB"  (4 bytes magic)
    int32   format version (1)
    int32   tzdata release length + UTF-8 bytes (e.g. "2025b")
    int32   zoneCount
      per zone:   int32 nameLen, name(UTF-8), int32 dataLen, TZif bytes
    int32   linkCount
      per link:   int32 aliasLen, alias, int32 targetLen, target
All integers are little-endian.
"""
import os, sys, struct, zlib

ZONEINFO = sys.argv[1] if len(sys.argv) > 1 else "/usr/share/zoneinfo"
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(
    os.path.dirname(__file__), "..", "..",
    "Broiler.JS/Broiler.JavaScript.BuiltIns/Temporal/Tz/tzdata.bin")

zi_path = os.path.join(ZONEINFO, "tzdata.zi")
zones, links = [], []
version = "unknown"
with open(zi_path, "r", encoding="ascii") as f:
    for line in f:
        if line.startswith("# version"):
            version = line.split()[-1].strip()
        parts = line.split()
        if not parts:
            continue
        if parts[0] == "Z":
            zones.append(parts[1])
        elif parts[0] == "L":
            links.append((parts[2], parts[1]))  # (alias, target)

def read_tzif(name):
    path = os.path.join(ZONEINFO, name)
    with open(path, "rb") as fh:
        data = fh.read()
    if data[:4] != b"TZif":
        raise SystemExit(f"{name} is not a TZif file")
    return data

buf = bytearray()
buf += b"TZDB"
buf += struct.pack("<i", 1)
vb = version.encode("utf-8")
buf += struct.pack("<i", len(vb)) + vb
buf += struct.pack("<i", len(zones))
for z in sorted(zones):
    nb = z.encode("utf-8")
    tzif = read_tzif(z)
    buf += struct.pack("<i", len(nb)) + nb
    buf += struct.pack("<i", len(tzif)) + tzif
buf += struct.pack("<i", len(links))
for alias, target in sorted(links):
    ab, tb = alias.encode("utf-8"), target.encode("utf-8")
    buf += struct.pack("<i", len(ab)) + ab
    buf += struct.pack("<i", len(tb)) + tb

# Raw DEFLATE (wbits=-15) so .NET DeflateStream can inflate it directly.
co = zlib.compressobj(9, zlib.DEFLATED, -15)
packed = co.compress(bytes(buf)) + co.flush()
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "wb") as fh:
    fh.write(packed)
print(f"tzdata {version}: {len(zones)} zones, {len(links)} links")
print(f"raw {len(buf):,} bytes -> packed {len(packed):,} bytes -> {OUT}")
