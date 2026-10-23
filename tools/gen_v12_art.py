# -*- coding: utf-8 -*-
"""Generate every v1.2 art asset.

Run with:  py tools/gen_v12_art.py
"""
import random
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
sys.path.insert(0, str(TOOLS))

from art_v12_world import floors_and_walls, props              # noqa: E402
from art_v12_actors import actors                              # noqa: E402
from art_v12_items import eq_icons, emblems, projectiles       # noqa: E402

try:
    from art_v12_bestiary import legacy_actors
except ImportError:
    legacy_actors = None


def main():
    random.seed(1202)
    np.random.seed(1202)
    floors_and_walls()
    props()
    actors()
    if legacy_actors is not None:
        legacy_actors()
    eq_icons()
    emblems()
    projectiles()
    print("v1.2 art complete ->", ROOT / "Assets" / "Resources" / "Art")


if __name__ == "__main__":
    main()
