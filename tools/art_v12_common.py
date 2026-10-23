# -*- coding: utf-8 -*-
"""Shared drawing helpers for the v1.2 paper-horror art set."""
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Art"
OUT.mkdir(parents=True, exist_ok=True)

SS = 2
INK = (27, 18, 22, 255)
INK_SOFT = (42, 28, 30, 255)
BONE = (216, 198, 164, 255)
BONE_D = (164, 145, 116, 255)
PALE = (242, 227, 198, 255)
GOLD = (214, 166, 88, 255)
GOLD_L = (244, 204, 124, 255)
GOLD_D = (133, 96, 48, 255)
RED = (164, 55, 54, 255)
RED_D = (92, 28, 34, 255)
BLOOD = (112, 31, 36, 255)
STEEL = (109, 126, 142, 255)
STEEL_L = (158, 176, 188, 255)
STEEL_D = (65, 76, 88, 255)
PURPLE = (104, 72, 132, 255)
PURPLE_D = (55, 36, 74, 255)
CYAN = (105, 221, 218, 255)
GREEN = (107, 158, 94, 255)
GREEN_D = (47, 78, 52, 255)
BROWN = (101, 70, 42, 255)
BROWN_D = (60, 41, 28, 255)
WOOD = (139, 96, 53, 255)
BLACK = (18, 14, 18, 255)
FLESH = (183, 143, 112, 255)


def shade(color, factor):
    """Multiply RGB while keeping alpha."""
    return (max(0, min(255, int(color[0] * factor))),
            max(0, min(255, int(color[1] * factor))),
            max(0, min(255, int(color[2] * factor))),
            color[3] if len(color) > 3 else 255)


def mix(a, b, t):
    return (int(a[0] * (1 - t) + b[0] * t),
            int(a[1] * (1 - t) + b[1] * t),
            int(a[2] * (1 - t) + b[2] * t),
            int((a[3] if len(a) > 3 else 255) * (1 - t) + (b[3] if len(b) > 3 else 255) * t))


def canvas(w, h, scale=SS):
    return Image.new("RGBA", (int(w * scale), int(h * scale)), (0, 0, 0, 0))


class Pen:
    """Draw in final-pixel coordinates on a supersampled canvas."""

    def __init__(self, image, scale=SS):
        self.image = image
        self.draw = ImageDraw.Draw(image)
        self.s = scale

    def pts(self, points):
        return [(x * self.s, y * self.s) for x, y in points]

    def box(self, box):
        return [v * self.s for v in box]

    def poly(self, points, fill, outline=INK, width=4):
        self.draw.polygon(self.pts(points), fill=fill, outline=outline,
                          width=max(1, int(width * self.s)))

    def ellipse(self, box, fill, outline=INK, width=4):
        self.draw.ellipse(self.box(box), fill=fill, outline=outline,
                          width=max(1, int(width * self.s)))

    def circle(self, cx, cy, r, fill, outline=INK, width=4):
        self.ellipse([cx - r, cy - r, cx + r, cy + r], fill, outline, width)

    def line(self, points, fill, width=4, outline=None, caps=True):
        if outline is not None:
            self.draw.line(self.pts(points), fill=outline,
                           width=max(1, int((width + 6) * self.s)), joint="curve")
            if caps:
                for x, y in (points[0], points[-1]):
                    self.circle(x, y, width * 0.5 + 3, outline, outline, 1)
        self.draw.line(self.pts(points), fill=fill,
                       width=max(1, int(width * self.s)), joint="curve")
        if caps:
            for x, y in (points[0], points[-1]):
                self.circle(x, y, width * 0.5, fill, fill, 1)

    def rect(self, box, fill, outline=INK, width=4):
        self.draw.rectangle(self.box(box), fill=fill, outline=outline,
                            width=max(1, int(width * self.s)))

    def rrect(self, box, radius, fill, outline=INK, width=4):
        self.draw.rounded_rectangle(self.box(box), radius=radius * self.s, fill=fill,
                                    outline=outline, width=max(1, int(width * self.s)))

    def arc(self, box, start, end, fill, width=4):
        self.draw.arc(self.box(box), start, end, fill=fill,
                      width=max(1, int(width * self.s)))

    def pieslice(self, box, start, end, fill, outline=INK, width=4):
        self.draw.pieslice(self.box(box), start, end, fill=fill, outline=outline,
                           width=max(1, int(width * self.s)))


def add_glow(im, cx, cy, radius, color, scale=SS, strength=1.0):
    """Soft additive-ish glow on a separate blurred layer."""
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    x, y, r = cx * scale, cy * scale, radius * scale
    alpha = int(color[3] * strength) if len(color) > 3 else int(255 * strength)
    d.ellipse([x - r, y - r, x + r, y + r],
              fill=(color[0], color[1], color[2], max(0, min(255, alpha))))
    layer = layer.filter(ImageFilter.GaussianBlur(max(1.0, radius * scale * 0.42)))
    im.alpha_composite(layer)


def add_speckles(pen, x, y, w, h, color, count=30, rmin=0.6, rmax=2.0, seed=0):
    rnd = random.Random(seed)
    for _ in range(count):
        px = x + rnd.random() * w
        py = y + rnd.random() * h
        r = rmin + rnd.random() * (rmax - rmin)
        pen.circle(px, py, r, color, color, 1)


def finish(im, name, w, h, grain=2.2, scale=SS):
    im = im.resize((int(w), int(h)), Image.LANCZOS)
    if grain > 0:
        arr = np.array(im).astype(np.float32)
        mask = arr[..., 3] > 0
        noise = np.random.normal(0.0, grain, (int(h), int(w), 1))
        rgb = arr[..., :3] + noise
        arr[..., :3] = np.clip(rgb, 0, 255)
        arr[..., 3] = np.where(mask, arr[..., 3], 0)
        im = Image.fromarray(arr.astype(np.uint8), "RGBA")
    im.save(OUT / (name + ".png"))
    print("wrote", name, int(w), int(h))


def noise_panel(w, h, base, amount=7.0, seed=1, alpha=255):
    rng = np.random.default_rng(seed)
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    n = rng.normal(0, amount, (h, w, 1))
    rgb = np.clip(np.array(base[:3], dtype=np.float32) + n, 0, 255)
    arr[..., :3] = rgb.astype(np.uint8)
    arr[..., 3] = alpha
    return Image.fromarray(arr, "RGBA")


def paper_cracks(pen, x, y, w, h, color, count=8, seed=0):
    rnd = random.Random(seed)
    for _ in range(count):
        px = x + rnd.random() * w
        py = y + rnd.random() * h
        pts = [(px, py)]
        ang = rnd.uniform(0, math.tau)
        for _ in range(rnd.randint(2, 5)):
            ang += rnd.uniform(-0.8, 0.8)
            px += math.cos(ang) * rnd.uniform(4, 13)
            py += math.sin(ang) * rnd.uniform(4, 13)
            pts.append((px, py))
        pen.line(pts, color, 1.3, outline=None, caps=False)


def bone_piece(pen, x, y, length, angle, thick=5, color=BONE):
    dx = math.cos(angle) * length * 0.5
    dy = math.sin(angle) * length * 0.5
    pen.line([(x - dx, y - dy), (x + dx, y + dy)], color, thick, outline=INK)
    pen.circle(x - dx, y - dy, thick * 0.62, color, INK, 2)
    pen.circle(x + dx, y + dy, thick * 0.62, color, INK, 2)
