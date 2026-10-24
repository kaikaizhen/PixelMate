"""Statistics: spec / generated / validated / awaiting human review / failed — per batch and category.
Generated = catalog row has a manifest entry with the same designId. Approved designs are listed in
assets/catalog/approved.txt (one designId per line); only the user adds to it."""
import collections
import json
import os
import subprocess
import sys

import catalog
from common import ASSETS

man = json.load(open(os.path.join(ASSETS, "manifest.json"), encoding="utf-8"))["assets"]
gen = {a["designId"]: k for k, a in man.items() if "designId" in a}
ap_path = os.path.join(ASSETS, "catalog", "approved.txt")
approved = set(open(ap_path).read().split()) if os.path.exists(ap_path) else set()
valid = subprocess.run([sys.executable, os.path.join(os.path.dirname(__file__), "validate_assets.py")],
                       capture_output=True, text=True).returncode == 0

rows = catalog.in_scope(catalog.load()) + catalog.load_extra()
lines = ["# 素材統計（由 report.py 產生）", "",
         f"驗證器：{'通過' if valid else '失敗'}；範圍內規格 {len(catalog.in_scope(catalog.load()))}（原 600 − 角色 24）＋追加 {len(catalog.load_extra())}", "",
         "| 批次 | 規格 | 已生成 | 自動驗證合格 | 人工待審 | 已確認 | 失敗 |", "|---|---:|---:|---:|---:|---:|---:|"]
by = collections.defaultdict(list)
for r in rows:
    by[r["batch"]].append(r)
tot = [0] * 6
for b in sorted(by):
    spec = len(by[b]); g = sum(1 for r in by[b] if r["design_id"] in gen)
    ok = g if valid else 0
    appr = sum(1 for r in by[b] if r["design_id"] in approved)
    pend = g - appr
    fail = 0 if valid else g
    for i, v in enumerate((spec, g, ok, pend, appr, fail)):
        tot[i] += v
    lines.append(f"| {b} | {spec} | {g} | {ok} | {pend} | {appr} | {fail} |")
lines.append("| **合計** | " + " | ".join(f"**{v}**" for v in tot) + " |")
lines += ["", "## B1 分類", "", "| 類別 | 規格 | 已生成 |", "|---|---:|---:|"]
cat = collections.defaultdict(lambda: [0, 0])
for r in by["B1"]:
    cat[r["category"]][0] += 1
    cat[r["category"]][1] += r["design_id"] in gen
for c, (a, b) in cat.items():
    lines.append(f"| {c} | {a} | {b} |")
out = "\n".join(lines) + "\n"
open(os.path.join(ASSETS, "preview", "stats.md"), "w", encoding="utf-8").write(out)
print(out)
