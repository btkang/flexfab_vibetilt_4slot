# -*- coding: utf-8 -*-
import re, sys, io, glob
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
from collections import defaultdict

with open('_paths.txt', encoding='utf-8') as pf:
    files = [ln.strip() for ln in pf if ln.strip()]
print('# files:', files, file=sys.stderr)
pat = re.compile(r'VL=(-?\d+\.\d+),\s*\S+=(-?\d+\.\d+)\s*\[(-?\d+\.\d+)~(-?\d+\.\d+)\]\s*OK')
cycle_pat = re.compile(r'전체 반복 \[(\d+)/300\]')

data = []
cur = 0
for fn in files:
    with open(fn, encoding='utf-8') as f:
        for line in f:
            cm = cycle_pat.search(line)
            if cm:
                cur = int(cm.group(1))
                continue
            if 'TILT' not in line:
                continue
            m = pat.search(line)
            if not m: continue
            vl = float(m.group(1)); err = float(m.group(2))
            t = round(vl/5)*5
            data.append((cur, t, err))

bins = defaultdict(lambda: defaultdict(list))
for c, t, e in data:
    bi = (c-1)//30
    bins[bi][t].append(abs(e))

out = []
out.append("=== 30-cycle bin: |err| mean (deg) ===")
out.append(f"{'range':>10} | {'-45':>7} {'-20':>7} {'+20':>7} {'+45':>7} | {'all':>8}")
for bi in sorted(bins.keys()):
    row = bins[bi]
    cells = []
    all_e = []
    for t in [-45, -20, 20, 45]:
        v = row.get(t, [])
        if v:
            cells.append(f"{sum(v)/len(v):>7.3f}")
            all_e.extend(v)
        else:
            cells.append(f"{'-':>7}")
    avg = sum(all_e)/len(all_e) if all_e else 0
    rng = f"{bi*30+1:>3}-{(bi+1)*30:>3}"
    out.append(f"  {rng:>10} | {' '.join(cells)} | {avg:>8.3f}")

out.append("")
out.append("=== Y-45 (worst-bias case) ===")
out.append(f"{'range':>10} | {'mean':>7} {'max':>7} {'min':>7}")
for bi in sorted(bins.keys()):
    v = bins[bi].get(-45, [])
    if not v: continue
    rng = f"{bi*30+1:>3}-{(bi+1)*30:>3}"
    out.append(f"  {rng:>10} | {sum(v)/len(v):>7.3f} {max(v):>7.3f} {min(v):>7.3f}")

# also: signed mean (bias direction)
out.append("")
out.append("=== bias (signed mean, deg) per target across bins ===")
out.append(f"{'range':>10} | {'-45':>8} {'-20':>8} {'+20':>8} {'+45':>8}")
signed = defaultdict(lambda: defaultdict(list))
for c, t, e in data:
    bi = (c-1)//30
    signed[bi][t].append(e)
for bi in sorted(signed.keys()):
    row = signed[bi]
    cells = []
    for t in [-45, -20, 20, 45]:
        v = row.get(t, [])
        cells.append(f"{sum(v)/len(v):>+8.3f}" if v else f"{'-':>8}")
    rng = f"{bi*30+1:>3}-{(bi+1)*30:>3}"
    out.append(f"  {rng:>10} | {' '.join(cells)}")

print("\n".join(out))
