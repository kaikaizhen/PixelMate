"""Catalog loader/validator: assets/catalog/600素材主題清單.csv is the single source of truth.

Scope decision (user, 2026-10-24): the 24 new creature designs (category 角色) are dropped;
every wearable / room asset is drawn for the one existing rig, pet.star_01.
Usage: python3 catalog.py            -> prints counts per batch/category
"""
import collections
import csv
import os

from common import ASSETS

CSV_PATH = os.path.join(ASSETS, "catalog", "600素材主題清單.csv")
DROPPED_CATEGORIES = {"角色"}


def load(path=CSV_PATH):
    with open(path, encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


EXTRA_PATH = os.path.join(ASSETS, "catalog", "擴充設計.csv")     # user-requested additions (batch X*), outside the 600


def load_extra():
    return load(EXTRA_PATH) if os.path.exists(EXTRA_PATH) else []


def in_scope(rows):
    """Rows still planned: not a dropped category, not individually marked status=dropped."""
    return [r for r in rows if r["category"] not in DROPPED_CATEGORIES and r.get("status") != "dropped"]


def validate(rows):
    errs = []
    if len(rows) != 600:
        errs.append(f"expected 600 rows, got {len(rows)}")
    if len({r["assetKey"] for r in rows}) != len(rows):
        errs.append("duplicate assetKey")
    if len({r["design_id"] for r in rows}) != len(rows):
        errs.append("duplicate design_id")
    if len({r["category"] for r in rows}) != 12:
        errs.append("expected 12 categories")
    per = collections.Counter(r["batch"] for r in rows)
    if any(per[b] != 120 for b in ("B1", "B2", "B3", "B4", "B5")):
        errs.append(f"batch sizes {dict(per)}")
    return errs


def by_batch(rows, batch):
    return [r for r in in_scope(rows) if r["batch"] == batch]


if __name__ == "__main__":
    rows = load()
    errs = validate(rows)
    print("catalog:", "OK" if not errs else errs)
    scope = in_scope(rows)
    print(f"spec {len(rows)}  in scope {len(scope)}  dropped {len(rows) - len(scope)}")
    for b in ("B1", "B2", "B3", "B4", "B5"):
        c = collections.Counter(r["category"] for r in by_batch(rows, b))
        print(b, sum(c.values()), dict(c))
