"""Parse JSON files and optionally require dotted keys.

Usage: Check-Json.py <file.json> [dotted.key ...]
Exits non-zero if the file does not parse or a key is missing.
"""
import json
import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    path = Path(sys.argv[1])
    data = json.loads(path.read_text(encoding="utf-8"))
    missing = []
    for dotted in sys.argv[2:]:
        node = data
        for part in dotted.split("."):
            if isinstance(node, dict) and part in node:
                node = node[part]
            else:
                missing.append(dotted)
                break
    if missing:
        print(f"{path}: parses, missing keys: {', '.join(missing)}")
        return 1
    print(f"{path}: parses; {len(sys.argv) - 2} required key(s) present")
    return 0


if __name__ == "__main__":
    sys.exit(main())
