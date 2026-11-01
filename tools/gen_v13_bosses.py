# -*- coding: utf-8 -*-
"""Generate the v1.5 rift boss sprites.

Run with:  py tools/gen_v13_bosses.py [name ...]
"""
import random
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

import art_v13_bosses as art  # noqa: E402


SPRITES = {
    "bone_captain": (380, 450, art.bone_captain),
    "brood_mother": (420, 380, art.brood_mother),
    "plague_baron": (320, 400, art.plague_baron),
    "abyss_knight": (360, 430, art.abyss_knight),
    "lich_archivist": (400, 460, art.lich_archivist),
    "abyss_colossus": (420, 450, art.abyss_colossus),
}


def main():
    random.seed(1505)
    np.random.seed(1505)
    names = sys.argv[1:] or list(SPRITES)
    for name in names:
        if name not in SPRITES:
            print("unknown sprite:", name)
            continue
        w, h, fn = SPRITES[name]
        art._actor(name, w, h, fn)
    print("v1.5 boss art complete ->", ROOT / "Assets" / "Resources" / "Art")


if __name__ == "__main__":
    main()
