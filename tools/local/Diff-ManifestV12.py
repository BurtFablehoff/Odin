"""Structural diff of two DEV pack manifest.json files (value and type changes, added/removed keys)."""
import json
import sys


def label(item):
    if isinstance(item, dict):
        for key in ('id', 'key'):
            if key in item:
                return f'{key}={item[key]}'
    return None


def walk(a, b, path, out):
    if type(a) is not type(b):
        out.append(f'TYPE {path}: {a!r} -> {b!r}')
    elif isinstance(a, dict):
        for k in sorted(set(a) | set(b), key=str):
            if k not in b:
                out.append(f'REMOVED {path}.{k}')
            elif k not in a:
                out.append(f'ADDED {path}.{k}')
            else:
                walk(a[k], b[k], f'{path}.{k}', out)
    elif isinstance(a, list):
        if len(a) != len(b):
            out.append(f'LEN {path}: {len(a)} -> {len(b)}')
        for i, (x, y) in enumerate(zip(a, b)):
            walk(x, y, f'{path}[{label(x) or i}]', out)
    elif a != b:
        out.append(f'VALUE {path}: {a!r} -> {b!r}')


old, new = (json.load(open(p, encoding='utf-8')) for p in sys.argv[1:3])
out = []
walk(old, new, '$', out)
print('\n'.join(out) or 'identical')
