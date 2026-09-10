#!/usr/bin/env python3
"""Validate manifest.json for the Jellyfin plugin repository.

Checks (always):
  - manifest.json is valid JSON
  - it is a list containing exactly one plugin object
  - the plugin guid equals the expected constant
  - all required top-level fields are present
  - every version entry has all required fields
  - every checksum matches ^[a-f0-9]{32}$ (md5, lowercase hex)
  - versions are strictly descending by numeric 4-tuple (implies no duplicates)

Checks (release mode, with --new-version and --zip):
  - the newest entry's version equals --new-version
  - the newest entry's checksum equals the md5 of --zip
  - the newest entry's sourceUrl ends with the zip's file name

Exit code 0 on success, 1 on any failure.
"""

import argparse
import hashlib
import json
import re
import sys

EXPECTED_GUID = "e29b0e3d-f15e-47b9-9b3d-ed3df892e33d"
REQUIRED_TOP_LEVEL = ["guid", "name", "overview", "description", "owner", "category", "versions"]
REQUIRED_VERSION_FIELDS = ["version", "targetAbi", "sourceUrl", "checksum", "timestamp", "changelog"]
CHECKSUM_RE = re.compile(r"^[a-f0-9]{32}$")
VERSION_RE = re.compile(r"^\d+\.\d+\.\d+\.\d+$")

errors = []


def err(msg):
    errors.append(msg)


def version_tuple(v):
    return tuple(int(x) for x in v.split("."))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", default="manifest.json")
    parser.add_argument("--new-version", help="tag being released; must be the newest entry")
    parser.add_argument("--zip", help="path to the built zip; md5 must match the newest entry")
    args = parser.parse_args()

    try:
        with open(args.manifest, encoding="utf-8") as f:
            manifest = json.load(f)
    except FileNotFoundError:
        print(f"FAIL: {args.manifest} not found", file=sys.stderr)
        return 1
    except json.JSONDecodeError as e:
        print(f"FAIL: {args.manifest} is not valid JSON: {e}", file=sys.stderr)
        return 1

    if not isinstance(manifest, list) or len(manifest) != 1:
        print("FAIL: manifest must be a list containing exactly one plugin object", file=sys.stderr)
        return 1
    plugin = manifest[0]

    for field in REQUIRED_TOP_LEVEL:
        if field not in plugin:
            err(f"missing required top-level field: {field}")

    if plugin.get("guid") != EXPECTED_GUID:
        err(f"guid mismatch: expected {EXPECTED_GUID}, got {plugin.get('guid')}")

    versions = plugin.get("versions", [])
    if not isinstance(versions, list) or not versions:
        err("versions must be a non-empty list")
        versions = []

    for i, entry in enumerate(versions):
        label = f"versions[{i}] ({entry.get('version', '?')})"
        for field in REQUIRED_VERSION_FIELDS:
            if not entry.get(field):
                err(f"{label}: missing or empty field: {field}")
        v = entry.get("version", "")
        if v and not VERSION_RE.match(v):
            err(f"{label}: version is not a numeric 4-tuple: {v}")
        checksum = entry.get("checksum", "")
        if checksum and not CHECKSUM_RE.match(checksum):
            err(f"{label}: checksum is not lowercase 32-char md5 hex: {checksum}")

    tuples = [version_tuple(e["version"]) for e in versions
              if VERSION_RE.match(e.get("version", ""))]
    for a, b in zip(tuples, tuples[1:]):
        if a <= b:
            err(f"versions not strictly descending: {'.'.join(map(str, a))} "
                f"followed by {'.'.join(map(str, b))}")

    if args.new_version:
        if not versions:
            err("--new-version given but manifest has no version entries")
        else:
            newest = versions[0]
            if newest.get("version") != args.new_version:
                err(f"newest entry is {newest.get('version')}, expected {args.new_version}")
            if args.zip:
                with open(args.zip, "rb") as f:
                    md5 = hashlib.md5(f.read()).hexdigest()
                if newest.get("checksum") != md5:
                    err(f"newest entry checksum {newest.get('checksum')} != md5 of {args.zip} ({md5})")
                zip_name = args.zip.rsplit("/", 1)[-1]
                if not newest.get("sourceUrl", "").endswith(f"/{zip_name}"):
                    err(f"newest entry sourceUrl does not end with /{zip_name}: {newest.get('sourceUrl')}")

    if errors:
        for e in errors:
            print(f"FAIL: {e}", file=sys.stderr)
        return 1

    print(f"OK: manifest valid ({len(versions)} version entries, newest "
          f"{versions[0]['version'] if versions else 'n/a'})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
