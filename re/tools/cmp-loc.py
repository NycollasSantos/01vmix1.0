import re, sys, io, os
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# usage: cmp-loc.py [dump-file]
# The dump is produced by dump-ui.ps1 (English pass + PT-BR pass).
default = os.path.join(os.environ.get("TEMP", "."), "ui-dump.txt")
path = sys.argv[1] if len(sys.argv) > 1 else default
lines = io.open(path, encoding="utf-8").read().splitlines()
i = lines.index("### PT-BR ###")
en, pt = lines[:i], lines[i+1:]

def parse(block):
    out = []
    for ln in block:
        if ln.startswith("--- ") or ln.startswith("### "):
            out.append((None, ln)); continue
        if "  =  " not in ln: continue
        k, v = ln.split("  =  ", 1)
        out.append((k.strip(), v))
    return out

en, pt = parse(en), parse(pt)
print("EN entries: %d   PT entries: %d" % (len(en), len(pt)))

cur = None
stats = {}
untranslated = []
for idx, (k, v) in enumerate(en):
    if k is None:
        cur = v.strip("- ").split(" ")[0]
        stats.setdefault(cur, [0, 0])
        continue
    if idx >= len(pt): break
    pk, pv = pt[idx]
    if pk != k:
        continue
    stats.setdefault(cur, [0, 0])
    if pv == v:
        if re.search(r"[A-Za-z]", v):
            stats[cur][1] += 1
            untranslated.append((cur, k, v))
    else:
        stats[cur][0] += 1

print()
print("%-16s %10s %14s" % ("form", "translated", "still EN"))
for f in sorted(stats):
    print("%-16s %10d %14d" % (f, stats[f][0], stats[f][1]))
print()
print("=== STILL ENGLISH (%d) ===" % len(untranslated))
seen = set()
for f, k, v in untranslated:
    key = (f, v)
    if key in seen: continue
    seen.add(key)
    print("  %-14s %-52s %s" % (f, k, v))
