"""Regenerate the maximum-stat golden fixture from v1's own formula.

FR-06 requires v2's maximum stats to equal v1's for all 309 fighters. This script runs the
Python formula from v1's bot_ver2.py over v1's fighters.json and writes the result as the
fixture the C# golden test asserts against, so the proof does not depend on v1 still running.

    python tools/golden/generate-max-stats-golden.py         .old/fighters.json         tests/Stanley.Core.Tests/Fixtures/max-stats.golden.json

It also prints the launch volumes of the database design, section 5.3, and checks the worked
examples of FRD Table B-1. slugify must stay identical to Stanley.Core's NameNormalizer.
"""
import json
import math
import re
import sys
import collections
import unicodedata

EVOS = {"bronze": 3, "silver": 2, "gold": 1, "diamond": 0}


def evoscale(stat, evos):
    for _ in range(evos):
        stat = round(stat * 1.95)
    return stat


def slugify(name):
    decomposed = unicodedata.normalize('NFD', name)
    folded = ''.join(c for c in decomposed if unicodedata.category(c) != 'Mn')
    return re.sub(r'[^a-z0-9]', '', folded.lower())


src, out = sys.argv[1], sys.argv[2]
fighters = json.load(open(src, encoding='utf-8'))

rows = []
slugs = collections.Counter()
chars = collections.Counter()
tiers = collections.Counter()
elements = collections.Counter()
sa_counts = collections.Counter()
longest = 0
raid = []

for key, f in fighters.items():
    rarity = f["rarity"]
    atk, hp, fs = (int(f["stats"][k]) for k in ("atk", "hp", "fs"))
    max_atk = math.ceil(evoscale(atk, EVOS[rarity]) * 19.2)
    max_hp = math.ceil(evoscale(hp, EVOS[rarity]) * 19.2)
    max_fs = math.ceil(round((6 * max_atk + max_hp) * 7 / 60, 3) * 1.688)

    slug = slugify(f["name"])
    slugs[slug] += 1
    character = re.sub(r'<:[^>]+>', '', f["character"]).strip()
    chars[character] += 1
    tiers[rarity] += 1
    elements[f["element"]] += 1

    sas = [f[k] for k in sorted(f) if re.fullmatch(r'SA\d+', k)]
    sa_counts[len(sas)] += 1
    longest = max([longest] + [len(s) for s in sas])
    if len(sas) > 2:
        raid.append(f["name"])

    rows.append({
        "slug": slug,
        "name": f["name"],
        "tier": rarity.capitalize(),
        "baseAtk": atk,
        "baseHp": hp,
        "baseFs": fs,
        "maxAtk": max_atk,
        "maxHp": max_hp,
        "maxFs": max_fs,
    })

rows.sort(key=lambda r: r["slug"])
with open(out, 'w', encoding='utf-8', newline='\n') as fh:
    json.dump(rows, fh, indent=2, ensure_ascii=False)
    fh.write('\n')

dupes = {s: n for s, n in slugs.items() if n > 1}
print(f"fighters      : {len(rows)}")
print(f"slug collisions: {dupes or 'none'}")
print(f"characters    : {len(chars)}")
print(f"tiers         : {dict(tiers)}")
print(f"elements      : {dict(elements)}")
print(f"SA counts     : {dict(sa_counts)}  total={sum(k*v for k,v in sa_counts.items())}")
print(f"longest SA    : {longest} chars")
print(f"raid bosses   : {raid}")
print("characters list:", sorted(chars))

# Cross-check FRD Table B-1 worked examples.
expect = {
    "beatbox": (9428, 64090, 23762),
    "bluebomber": (9620, 59079, 23002),
    "luckylure": (11117, 63572, 25656),
    "brassbandit": (11847, 80045, 29762),
}
by_slug = {r["slug"]: r for r in rows}
for slug, (a, h, f_) in expect.items():
    r = by_slug[slug]
    got = (r["maxAtk"], r["maxHp"], r["maxFs"])
    print(f"FRD B-1 {r['name']:<14} expected {(a,h,f_)} got {got} "
          f"{'OK' if got == (a,h,f_) else 'MISMATCH'}")
