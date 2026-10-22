# -*- coding: utf-8 -*-
"""Генератор спрайтов «Картонного подземелья» (плоские картонные вырезки).

Все PNG кладутся в Assets/Resources/Art. Стиль: плотный тёмный контур,
слой «толщины», бумажное зерно, тёплые картонные цвета.
"""
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Art"
OUT.mkdir(parents=True, exist_ok=True)

S = 2  # суперсэмплинг

INK = (42, 27, 22, 255)
INK_SOFT = (64, 43, 34, 255)
THICK = (88, 60, 44, 255)
CREAM = (242, 227, 198, 255)
PAPER = (232, 217, 184, 255)
PAPER_D = (206, 186, 150, 255)
KRAFT = (168, 126, 82, 255)
KRAFT_D = (122, 88, 56, 255)
BROWN = (96, 64, 44, 255)
RED = (201, 79, 69, 255)
RED_D = (150, 52, 48, 255)
BLUE = (78, 125, 181, 255)
BLUE_D = (50, 84, 128, 255)
TEAL = (79, 179, 166, 255)
PURPLE = (139, 107, 177, 255)
PURPLE_D = (92, 68, 122, 255)
GOLD = (232, 184, 109, 255)
GREEN = (111, 168, 100, 255)
GREEN_D = (72, 118, 66, 255)
BONE = (232, 220, 200, 255)

FONT_BOLD = r"C:\Windows\Fonts\georgiab.ttf"
FONT_SANS = r"C:\Windows\Fonts\segoeuib.ttf"
FONT_SANS_R = r"C:\Windows\Fonts\segoeui.ttf"


def fnt(path, size):
    try:
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.load_default()


class Pic:
    def __init__(self, w, h, s=S):
        self.w, self.h, self.s = w, h, s
        self.im = Image.new("RGBA", (int(w * s), int(h * s)), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im, "RGBA")

    def xy(self, *v):
        return [val * self.s for val in v]

    def poly(self, pts, fill, outline=INK, width=3):
        p = [(int(x * self.s), int(y * self.s)) for x, y in pts]
        if fill is not None:
            self.d.polygon(p, fill=fill)
        if outline is not None and width:
            self.d.line(p + [p[0]], fill=outline, width=max(1, int(width * self.s)), joint="curve")

    def rect(self, x1, y1, x2, y2, fill, outline=None, width=0, r=0):
        box = [int(x1 * self.s), int(y1 * self.s), int(x2 * self.s), int(y2 * self.s)]
        kw = dict(fill=fill)
        if outline is not None and width:
            kw.update(outline=outline, width=max(1, int(width * self.s)))
        if r:
            self.d.rounded_rectangle(box, radius=int(r * self.s), **kw)
        else:
            self.d.rectangle(box, **kw)

    def ell(self, x1, y1, x2, y2, fill, outline=INK, width=3):
        box = [int(x1 * self.s), int(y1 * self.s), int(x2 * self.s), int(y2 * self.s)]
        kw = dict(fill=fill)
        if outline is not None and width:
            kw.update(outline=outline, width=max(1, int(width * self.s)))
        self.d.ellipse(box, **kw)

    def line(self, pts, fill, width, joint="curve"):
        p = [(int(x * self.s), int(y * self.s)) for x, y in pts]
        self.d.line(p, fill=fill, width=max(1, int(width * self.s)), joint=joint)

    def text(self, xy, txt, size, fill, font=FONT_BOLD, anchor="mm", stroke=0, stroke_fill=INK):
        self.d.text((int(xy[0] * self.s), int(xy[1] * self.s)), txt, font=fnt(font, int(size * self.s)),
                    fill=fill, anchor=anchor, stroke_width=int(stroke * self.s), stroke_fill=stroke_fill)

    def glow(self, cx, cy, r, color, alpha=130, blur=10):
        layer = Image.new("RGBA", self.im.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer, "RGBA")
        d.ellipse([int((cx - r) * self.s), int((cy - r) * self.s),
                   int((cx + r) * self.s), int((cy + r) * self.s)],
                  fill=color[:3] + (alpha,))
        layer = layer.filter(ImageFilter.GaussianBlur(int(blur * self.s)))
        self.im.alpha_composite(layer)

    def fibers(self, n=26, color=(255, 255, 255, 14), area=None):
        x1, y1, x2, y2 = area or (0, 0, self.w, self.h)
        for _ in range(n):
            x = random.uniform(x1, x2)
            y = random.uniform(y1, y2)
            ln = random.uniform(4, 18)
            self.line([(x, y), (x + ln, y + random.uniform(-1, 1))], color, random.uniform(0.4, 0.9))

    def grain(self, amount=6.0):
        arr = np.array(self.im).astype(np.int16)
        noise = np.random.normal(0, amount, (arr.shape[0], arr.shape[1], 1))
        arr[..., :3] = np.clip(arr[..., :3] + noise, 0, 255)
        self.im = Image.fromarray(arr.astype(np.uint8))

    def sticker(self, border=3, thickness=4):
        alpha = self.im.split()[3]
        size = max(3, int(border * 2 * self.s) + 1)
        if size % 2 == 0:
            size += 1
        dil = alpha.filter(ImageFilter.MaxFilter(size))
        out = Image.new("RGBA", self.im.size, (0, 0, 0, 0))
        thick = Image.new("RGBA", self.im.size, THICK)
        thick.putalpha(dil)
        out.alpha_composite(thick, (int(thickness * self.s), int(thickness * self.s)))
        rim = Image.new("RGBA", self.im.size, INK)
        rim.putalpha(dil)
        out.alpha_composite(rim)
        out.alpha_composite(self.im)
        self.im = out

    def save(self, name, sticker=False, border=3, thickness=4, grain=5.0):
        if grain:
            self.grain(grain)
        if sticker:
            self.sticker(border, thickness)
        im = self.im.resize((self.w, self.h), Image.LANCZOS)
        im.save(OUT / (name + ".png"))
        return im


def rnd():
    return random.Random(1337)


# ------------------------------------------------------------------ окружение
def floor_tile(name="floor", base=(110, 82, 56), line=(84, 61, 41), boss=False):
    p = Pic(256, 256, 2)
    p.rect(0, 0, 256, 256, base + (255,), None)
    # плиты
    for i in range(0, 257, 64):
        p.line([(i, 0), (i, 256)], line + (235,), 2.2)
        p.line([(0, i), (256, i)], line + (235,), 2.2)
        p.line([(i + 1.5, 0), (i + 1.5, 256)], (140, 108, 76, 55), 1)
    # царапины и пятна
    r = rnd()
    for _ in range(45):
        x, y = r.uniform(2, 254), r.uniform(2, 254)
        p.line([(x, y), (x + r.uniform(4, 22), y + r.uniform(-2, 2))], (70, 50, 34, 55), r.uniform(0.5, 1.4))
    for _ in range(12):
        x, y = r.uniform(8, 248), r.uniform(8, 248)
        p.ell(x - r.uniform(3, 9), y - r.uniform(2, 6), x + r.uniform(3, 9), y + r.uniform(2, 6),
              (90, 64, 42, 45), None)
    if boss:
        p.ell(28, 28, 228, 228, (0, 0, 0, 0), (120, 40, 44, 220), 5)
        p.ell(52, 52, 204, 204, (0, 0, 0, 0), (170, 58, 58, 180), 3)
        for i in range(12):
            a = i * math.pi / 6
            x1 = 128 + math.cos(a) * 72
            y1 = 128 + math.sin(a) * 72
            x2 = 128 + math.cos(a) * 92
            y2 = 128 + math.sin(a) * 92
            p.line([(x1, y1), (x2, y2)], (160, 52, 52, 190), 3)
    p.fibers(30)
    p.save(name, grain=4.0)


def wall(name, w, h, vertical=False):
    p = Pic(w, h, 2)
    p.rect(2, 2, w - 2, h - 2, KRAFT, INK, 3, r=4)
    p.rect(6, 5, w - 6, h * 0.34, (196, 152, 102, 255), None, r=3)
    # гофра по нижнему/правому краю
    if not vertical:
        for x in range(6, w - 6, 14):
            p.ell(x, h - 16, x + 12, h - 4, (150, 110, 72, 255), INK_SOFT, 1.2)
    else:
        for y in range(6, h - 6, 14):
            p.ell(w - 16, y, w - 4, y + 12, (150, 110, 72, 255), INK_SOFT, 1.2)
    # трещины/скотч
    p.line([(w * .18, h * .55), (w * .3, h * .62), (w * .26, h * .78)], (108, 76, 50, 120), 1.2)
    p.rect(w * .62, h * .42, w * .8, h * .53, (238, 226, 200, 120), (255, 255, 255, 70), 1, r=1)
    p.fibers(20)
    p.save(name, grain=4.0)


def prop(name, w, h, draw):
    p = Pic(w, h, 2)
    draw(p)
    p.fibers(12)
    p.save(name, sticker=True, border=2, thickness=3, grain=4.0)


def draw_rock(p):
    p.poly([(8, 62), (18, 30), (38, 14), (66, 18), (82, 38), (86, 64), (60, 74), (28, 74)],
           (128, 118, 104, 255))
    p.poly([(20, 34), (38, 20), (54, 24), (44, 42), (26, 46)], (156, 144, 128, 255), None)
    p.line([(40, 46), (36, 60)], (90, 82, 72, 255), 2)


def draw_bones(p):
    p.line([(14, 54), (44, 30)], BONE, 8)
    p.line([(34, 58), (62, 36)], BONE, 7)
    p.ell(8, 48, 24, 62, BONE, INK, 2)
    p.ell(38, 24, 52, 38, BONE, INK, 2)
    p.ell(56, 30, 70, 44, BONE, INK, 2)
    p.poly([(58, 46), (76, 36), (82, 48), (66, 58)], BONE)


def draw_barrel(p):
    p.poly([(16, 18), (48, 18), (54, 74), (10, 74)], (150, 104, 64, 255))
    p.line([(12, 32), (52, 32)], (84, 58, 38, 255), 4)
    p.line([(10, 60), (54, 60)], (84, 58, 38, 255), 4)
    p.ell(16, 10, 48, 26, (170, 122, 78, 255), INK, 2)


def draw_column(p):
    p.rect(14, 22, 58, 156, (150, 132, 110, 255), INK, 3)
    p.rect(6, 8, 66, 28, (168, 150, 126, 255), INK, 3)
    p.rect(6, 150, 66, 168, (168, 150, 126, 255), INK, 3)
    p.line([(24, 34), (22, 144)], (120, 104, 86, 200), 2)
    p.line([(46, 34), (48, 144)], (120, 104, 86, 200), 2)


def draw_chest(p):
    p.rect(10, 34, 86, 74, (150, 104, 60, 255), INK, 3, r=4)
    p.poly([(10, 38), (14, 18), (82, 18), (86, 38)], (176, 126, 74, 255))
    p.rect(10, 38, 86, 46, (110, 76, 44, 255), INK, 2)
    p.rect(42, 42, 54, 62, GOLD, INK, 2, r=2)
    p.ell(44, 46, 52, 54, (120, 86, 40, 255), None)


def draw_campfire(p):
    p.line([(14, 70), (46, 52), (78, 70)], (96, 64, 40, 255), 8)
    p.line([(18, 58), (52, 72), (76, 56)], (120, 82, 50, 255), 7)
    p.glow(48, 44, 22, (255, 170, 60), 120, 10)
    p.poly([(48, 8), (62, 36), (56, 42), (66, 54), (44, 56), (30, 44), (38, 32), (36, 22)],
           (235, 130, 54, 255), INK, 2)
    p.poly([(48, 22), (55, 40), (48, 50), (41, 40)], (255, 216, 120, 255), None)


def draw_torch(p):
    p.rect(18, 26, 30, 78, (96, 64, 40, 255), INK, 2)
    p.poly([(12, 26), (36, 26), (30, 14), (18, 14)], (120, 84, 52, 255), INK, 2)
    p.glow(24, 16, 16, (255, 160, 50), 130, 8)


def draw_flame(p):
    p.glow(32, 40, 22, (255, 150, 40), 150, 10)
    p.poly([(32, 6), (46, 32), (40, 40), (50, 56), (32, 66), (14, 54), (22, 40), (20, 26)],
           (240, 140, 56, 255), INK, 2)
    p.poly([(32, 24), (39, 42), (32, 56), (25, 42)], (255, 222, 140, 255), None)


def draw_door(name, w=140, h=210):
    p = Pic(w, h, 2)
    p.poly([(10, h - 4), (10, 40), (20, 20), (w / 2, 6), (w - 20, 20), (w - 10, 40), (w - 10, h - 4)],
           (122, 82, 48, 255), INK, 3)
    for i in range(1, 4):
        x = 10 + i * (w - 20) / 4
        p.line([(x, 18), (x, h - 6)], (90, 58, 34, 220), 2)
    p.rect(14, h * .30, w - 14, h * .36, (86, 56, 34, 255), None)
    p.rect(14, h * .66, w - 14, h * .72, (86, 56, 34, 255), None)
    p.ell(w - 30, h * .52, w - 18, h * .56, GOLD, INK, 2)
    p.rect(w * .18, h * .16, w * .82, h * .27, (238, 226, 200, 210), (180, 160, 128, 160), 1, r=2)
    p.text((w / 2, h * .215), "ВЫХОД", 11, (70, 46, 30, 255))
    p.fibers(18)
    p.save(name, sticker=True, border=2, thickness=3, grain=4.0)


def draw_door_frame(name, w=170, h=240):
    p = Pic(w, h, 2)
    path = [(10, h), (10, 46), (22, 24), (w / 2, 8), (w - 22, 24), (w - 10, 46), (w - 10, h)]
    p.line(path, INK, 24)
    p.line(path, (120, 86, 54, 255), 17)
    inner = [(26, h), (26, 52), (36, 33), (w / 2, 20), (w - 36, 33), (w - 26, 52), (w - 26, h)]
    p.line(inner, INK, 2.5)
    p.line([(18, 48), (18, h - 2)], (152, 112, 72, 160), 2)
    p.fibers(14)
    p.save(name, sticker=True, border=2, thickness=3, grain=4.0)


# ------------------------------------------------------------------ эффекты
def draw_bolt(name, color, size=(32, 48), glow=True):
    p = Pic(*size, 2)
    w, h = size
    if glow:
        p.glow(w / 2, h / 2, w * .5, color, 120, 8)
    p.ell(w * .18, h * .18, w * .82, h * .82, color, (255, 255, 255, 220), 1.5)
    p.ell(w * .38, h * .3, w * .62, h * .55, (255, 255, 255, 220), None)
    p.save(name, grain=2.0)


def draw_slash_fx():
    p = Pic(160, 160, 2)
    for off, alpha, wdt in ((0, 220, 9), (10, 170, 6), (20, 110, 3)):
        p.d.arc([int((18 + off) * p.s), int((18 + off) * p.s),
                 int((142 - off) * p.s), int((142 - off) * p.s)],
                200, 340, fill=(255, 245, 220, alpha), width=max(1, int(wdt * p.s)))
    for i in range(3):
        x, y = 118 + i * 8, 30 + i * 6
        p.ell(x - 3, y - 3, x + 3, y + 3, (255, 245, 220, 200), None)
    p.save("fx_slash", grain=0)


def draw_ring(name, color):
    p = Pic(192, 192, 2)
    p.ell(12, 12, 180, 180, (0, 0, 0, 0), color[:3] + (230,), 10)
    p.ell(34, 34, 158, 158, (0, 0, 0, 0), color[:3] + (120,), 4)
    p.save(name, grain=0)


def draw_explosion():
    p = Pic(192, 192, 2)
    p.glow(96, 96, 80, (255, 160, 50), 170, 18)
    r = rnd()
    for i in range(12):
        a = i * math.pi / 6
        rr = r.uniform(52, 86)
        x, y = 96 + math.cos(a) * rr, 96 + math.sin(a) * rr
        p.ell(x - 18, y - 18, x + 18, y + 18, (250, 150, 52, 220), (255, 220, 150, 180), 2)
    p.ell(54, 54, 138, 138, (255, 210, 120, 230), (255, 245, 210, 200), 3)
    p.save("fx_explosion", grain=0)


def draw_dust():
    p = Pic(128, 96, 2)
    r = rnd()
    for _ in range(10):
        x, y = r.uniform(20, 108), r.uniform(30, 80)
        s = r.uniform(8, 20)
        p.ell(x - s, y - s * .7, x + s, y + s * .7, (210, 190, 150, r.randint(50, 110)), None)
    p.save("fx_dust", grain=2)


def draw_poison_puddle():
    p = Pic(160, 96, 2)
    p.ell(10, 22, 150, 82, (86, 140, 76, 190), (60, 100, 54, 220), 3)
    p.ell(30, 30, 90, 66, (120, 180, 100, 120), None)
    for i in range(4):
        x = 30 + i * 26
        p.ell(x, 12, x + 12, 26, (150, 205, 120, 150), None)
    p.save("fx_poison", grain=2)


def draw_shield_fx():
    p = Pic(160, 190, 2)
    p.glow(80, 95, 70, (90, 160, 220), 90, 14)
    p.poly([(80, 10), (140, 36), (132, 110), (80, 178), (28, 110), (20, 36)],
           (90, 160, 220, 70), (160, 220, 255, 200), 4)
    p.poly([(80, 28), (122, 46), (116, 104), (80, 152), (44, 104), (38, 46)],
           (0, 0, 0, 0), (200, 240, 255, 140), 2)
    p.save("fx_shield", grain=0)


def draw_meteor():
    p = Pic(120, 160, 2)
    p.glow(60, 106, 48, (255, 120, 40), 150, 14)
    p.poly([(60, 20), (92, 70), (88, 120), (60, 146), (30, 120), (26, 70)], (150, 96, 70, 255), INK, 3)
    p.poly([(58, 40), (74, 78), (64, 100), (46, 76)], (190, 130, 94, 255), None)
    p.poly([(26, 40), (6, 96), (16, 120), (34, 84)], (255, 170, 70, 190), None)
    p.poly([(94, 42), (114, 100), (104, 122), (86, 86)], (255, 170, 70, 190), None)
    p.save("fx_meteor", grain=2)


def draw_arrow():
    p = Pic(64, 18, 2)
    p.line([(6, 9), (50, 9)], (196, 160, 110, 255), 3)
    p.poly([(50, 2), (62, 9), (50, 16)], (220, 220, 210, 255), INK, 1)
    p.line([(8, 4), (16, 9), (8, 14)], (220, 220, 210, 255), 2)
    p.save("proj_arrow", grain=0)


def draw_skull_proj():
    p = Pic(48, 48, 2)
    p.glow(24, 24, 20, (150, 230, 255), 110, 6)
    p.ell(8, 6, 40, 34, BONE, INK, 2)
    p.ell(14, 16, 21, 25, INK, None)
    p.ell(27, 16, 34, 25, INK, None)
    p.ell(15, 18, 19, 23, (150, 230, 255, 255), None)
    p.ell(28, 18, 32, 23, (150, 230, 255, 255), None)
    p.rect(17, 31, 31, 41, BONE, INK, 1)
    p.save("proj_skull", grain=0)


# ------------------------------------------------------------------ UI
def panel(name, fill=(42, 32, 51, 245), border=GOLD, radius=18, size=96):
    p = Pic(size, size, 2)
    p.rect(3, 3, size - 3, size - 3, fill, border, 2.5, r=radius)
    p.rect(8, 8, size - 8, size - 8, (0, 0, 0, 0), (255, 255, 255, 18), 1, r=radius - 5)
    p.fibers(8, (255, 255, 255, 10))
    p.save(name, grain=2.5)


def button(name, hover=False):
    p = Pic(160, 56, 2)
    base = (58, 44, 68, 255) if not hover else (82, 62, 92, 255)
    p.rect(3, 3, 157, 53, base, GOLD if hover else (170, 132, 80, 255), 2.5, r=12)
    p.rect(8, 8, 152, 48, (0, 0, 0, 0), (255, 255, 255, 22), 1, r=9)
    p.save(name, grain=2)


def card(name, dark=False):
    p = Pic(260, 340, 2)
    p.rect(10, 14, 250, 334, (0, 0, 0, 90), None, r=12)
    base = PAPER if not dark else (188, 168, 138, 255)
    p.rect(4, 4, 244, 324, base, INK, 3, r=12)
    p.rect(12, 12, 236, 316, (0, 0, 0, 0), (190, 168, 132, 255), 1.5, r=8)
    # скотч
    p.rect(70, 0, 146, 18, (238, 230, 205, 170), (210, 200, 170, 120), 1, r=2)
    p.rect(158, 308, 236, 330, (238, 230, 205, 150), (210, 200, 170, 100), 1, r=2)
    p.fibers(24, (120, 90, 60, 18))
    p.save(name, grain=3)


def slot():
    p = Pic(92, 92, 2)
    p.rect(3, 3, 89, 89, (32, 24, 40, 245), (170, 132, 80, 255), 2.5, r=12)
    p.rect(8, 8, 84, 84, (0, 0, 0, 0), (255, 255, 255, 16), 1, r=8)
    p.save("ui_slot", grain=2)


def heart():
    p = Pic(48, 48, 2)
    p.glow(24, 26, 18, (220, 70, 70), 80, 5)
    p.poly([(24, 42), (8, 26), (8, 14), (16, 8), (24, 14), (32, 8), (40, 14), (40, 26)], RED, INK, 2.5)
    p.ell(15, 14, 22, 20, (255, 180, 170, 160), None)
    p.save("ui_heart", grain=1)


def vignette():
    w, h = 1024, 576
    y, x = np.mgrid[0:h, 0:w]
    cx, cy = w / 2, h / 2
    d = np.sqrt(((x - cx) / cx) ** 2 + ((y - cy) / cy) ** 2)
    a = np.clip((d - 0.45) * 1.35, 0, 0.86)
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    arr[..., 0] = 6
    arr[..., 1] = 4
    arr[..., 2] = 8
    arr[..., 3] = (a * 230).astype(np.uint8)
    Image.fromarray(arr).save(OUT / "ui_vignette.png")


def grain_overlay():
    arr = np.random.randint(0, 255, (256, 256, 1), dtype=np.uint8)
    rgba = np.zeros((256, 256, 4), dtype=np.uint8)
    rgba[..., :3] = arr
    rgba[..., 3] = np.random.randint(8, 26, (256, 256), dtype=np.uint8)
    Image.fromarray(rgba).save(OUT / "ui_grain.png")


def white():
    Image.new("RGBA", (8, 8), (255, 255, 255, 255)).save(OUT / "white.png")


def shadow():
    p = Pic(128, 64, 2)
    p.ell(8, 20, 120, 56, (0, 0, 0, 90), None)
    p.im = p.im.filter(ImageFilter.GaussianBlur(6))
    p.save("fx_shadow", grain=0)


def reticle():
    p = Pic(64, 64, 2)
    p.ell(8, 8, 56, 56, (0, 0, 0, 0), (255, 240, 210, 220), 2)
    for a in (0, 90, 180, 270):
        r = math.radians(a)
        p.line([(32 + math.cos(r) * 15, 32 + math.sin(r) * 15),
                (32 + math.cos(r) * 26, 32 + math.sin(r) * 26)], (255, 240, 210, 220), 2)
    p.save("ui_reticle", grain=0)


def glow_sprite(name="fx_glow", size=256, power=2.2):
    y, x = np.mgrid[0:size, 0:size]
    cx = cy = (size - 1) / 2
    d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / (size / 2)
    a = np.clip(1 - d, 0, 1) ** power
    arr = np.zeros((size, size, 4), dtype=np.uint8)
    arr[..., :3] = 255
    arr[..., 3] = (a * 255).astype(np.uint8)
    Image.fromarray(arr).save(OUT / (name + ".png"))


def bars():
    p = Pic(64, 24, 2)
    p.rect(1, 1, 63, 23, (30, 22, 34, 245), INK, 2, r=10)
    p.rect(4, 4, 60, 20, (0, 0, 0, 0), (255, 255, 255, 20), 1, r=7)
    p.save("ui_bar_bg", grain=1.5)
    p = Pic(64, 24, 2)
    p.rect(2, 2, 62, 22, (255, 255, 255, 255), (255, 255, 255, 255), 1, r=9)
    p.save("ui_bar_fill", grain=0)


def table_bg():
    W, H = 1024, 576
    p = Pic(W, H, 1)
    p.rect(0, 0, W, H, (30, 22, 26, 255), None)
    r = random.Random(5)
    for plank in range(0, W, 128):
        p.rect(plank, 0, plank + 124, H, (38 + r.randint(-4, 4), 27 + r.randint(-3, 3), 29, 255), None)
        p.line([(plank + 124, 0), (plank + 124, H)], (18, 12, 16, 255), 2)
    for _ in range(180):
        x, y = r.uniform(0, W), r.uniform(0, H)
        p.line([(x, y), (x + r.uniform(10, 90), y + r.uniform(-3, 3))], (60, 42, 40, 40), r.uniform(0.5, 1.4))
    # бумажный мусор на столе
    for _ in range(18):
        x, y = r.uniform(20, W - 20), r.uniform(20, H - 20)
        w, h = r.uniform(14, 64), r.uniform(10, 42)
        pts = [(x, y), (x + w, y + r.uniform(-8, 8)), (x + w + r.uniform(-8, 8), y + h),
               (x + r.uniform(-8, 8), y + h)]
        p.poly(pts, (210, 190, 150, r.randint(20, 50)), (150, 130, 100, 25), 1)
    # виньетка
    y, x = np.mgrid[0:H, 0:W]
    d = np.sqrt(((x - W / 2) / (W / 2)) ** 2 + ((y - H / 2) / (H / 2)) ** 2)
    a = np.clip((d - 0.35) * 1.4, 0, 0.9)
    arr = np.array(p.im).astype(np.float32)
    shade = 1 - a * 0.75
    arr[..., :3] *= shade[..., None]
    p.im = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    p.grain(3.0)
    p.im.save(OUT / "table_bg.png")


def oline(p, pts, color, width, outline=INK, ow=2.5):
    p.line(pts, outline, width + ow)
    p.line(pts, color, width)


def actor(name, w, h, draw, border=3, thickness=4):
    p = Pic(w, h + 20, 2)
    draw(p)
    p.fibers(18)
    p.save(name, sticker=True, border=border, thickness=thickness, grain=4.5)


# ------------------------------------------------------------------ персонажи
def draw_wizard(p):
    # посох за спиной
    oline(p, [(196, 300), (184, 124)], (132, 92, 54, 255), 7)
    p.ell(170, 96, 200, 128, TEAL, INK, 3)
    p.ell(178, 103, 188, 113, (210, 255, 246, 255), None)
    # мантия
    p.poly([(80, 304), (94, 182), (108, 158), (132, 158), (146, 182), (160, 304),
            (148, 310), (92, 310)], BLUE)
    p.poly([(86, 292), (154, 292), (158, 306), (82, 306)], BLUE_D)
    p.line([(108, 168), (104, 296)], (238, 227, 198, 120), 2)
    p.line([(132, 168), (136, 296)], (238, 227, 198, 90), 2)
    p.rect(92, 218, 148, 238, GOLD, INK, 2.5, r=2)
    p.rect(112, 220, 128, 236, (150, 108, 52, 255), INK, 1.5, r=2)
    # рукава
    p.poly([(96, 180), (64, 236), (82, 252), (108, 210)], BLUE)
    p.poly([(144, 180), (180, 228), (164, 248), (132, 212)], BLUE)
    p.ell(60, 240, 84, 264, (226, 184, 146, 255))
    p.ell(166, 236, 190, 260, (226, 184, 146, 255))
    # голова
    p.ell(96, 84, 144, 138, (226, 184, 146, 255))
    p.poly([(92, 112), (96, 148), (110, 172), (130, 172), (144, 148), (148, 112),
            (138, 130), (120, 138), (102, 130)], (238, 232, 216, 255))
    p.ell(106, 108, 114, 117, INK, None)
    p.ell(126, 108, 134, 117, INK, None)
    p.ell(117, 117, 124, 126, (206, 156, 122, 255), None)
    p.line([(108, 100), (115, 98)], INK, 1.5)
    p.line([(126, 98), (133, 100)], INK, 1.5)
    # шляпа
    p.ell(56, 84, 184, 112, BLUE_D)
    p.poly([(74, 100), (104, 24), (118, 14), (126, 30), (142, 100)], BLUE)
    p.poly([(72, 90), (144, 90), (146, 104), (70, 104)], GOLD)
    p.poly([(114, 64), (118, 54), (122, 64), (132, 66), (124, 73), (126, 84), (117, 77), (108, 84),
            (110, 72), (102, 66)], (238, 227, 198, 255), INK, 1.2)
    # ботинки
    p.ell(78, 296, 110, 314, (96, 64, 40, 255))
    p.ell(130, 296, 162, 314, (96, 64, 40, 255))


def draw_imp(p):
    # крылья
    p.poly([(52, 62), (14, 30), (26, 76), (8, 88), (46, 104)], RED_D, INK, 2.5)
    p.poly([(128, 62), (166, 30), (154, 76), (172, 88), (134, 104)], RED_D, INK, 2.5)
    # хвост
    p.line([(88, 162), (70, 184), (86, 196), (96, 186)], RED_D, 4)
    p.poly([(96, 180), (104, 190), (92, 194)], RED_D, INK, 1.5)
    # тело
    p.ell(50, 86, 130, 182, RED)
    p.ell(64, 126, 116, 170, (224, 108, 92, 255), None)
    # ноги
    p.ell(52, 166, 76, 194, RED_D)
    p.ell(104, 166, 128, 194, RED_D)
    # голова
    p.ell(40, 26, 140, 118, RED)
    p.ell(34, 42, 56, 92, (224, 108, 92, 255), INK, 2)
    p.ell(124, 42, 146, 92, (224, 108, 92, 255), INK, 2)
    # рога
    p.poly([(52, 34), (40, 2), (66, 20)], BONE, INK, 2)
    p.poly([(128, 34), (140, 2), (114, 20)], BONE, INK, 2)
    # глаза и ухмылка
    p.poly([(62, 62), (84, 66), (64, 76)], (255, 220, 110, 255), INK, 1.5)
    p.poly([(118, 62), (96, 66), (116, 76)], (255, 220, 110, 255), INK, 1.5)
    p.ell(68, 66, 74, 72, INK, None)
    p.ell(106, 66, 112, 72, INK, None)
    p.line([(66, 90), (90, 98), (114, 90)], INK, 2)
    for i in range(3):
        x = 74 + i * 12
        p.poly([(x, 91), (x + 5, 101), (x + 10, 91)], (255, 245, 220, 255), INK, 1)
    # когти
    for x in (50, 62, 108, 120):
        p.line([(x, 178), (x - 3, 190)], INK, 2)


def draw_skeleton(p):
    # плащ
    p.poly([(58, 78), (124, 66), (166, 250), (40, 250)], (110, 82, 60, 180), INK, 2.5)
    p.poly([(64, 92), (70, 240), (46, 240), (52, 96)], (140, 104, 70, 140), None)
    # ноги
    oline(p, [(88, 196), (78, 244)], BONE, 7)
    oline(p, [(108, 196), (120, 244)], BONE, 7)
    p.ell(66, 238, 84, 252, BONE, INK, 2)
    p.ell(112, 238, 130, 252, BONE, INK, 2)
    # таз и рёбра
    p.ell(86, 178, 116, 204, BONE, INK, 2)
    oline(p, [(100, 108), (100, 184)], BONE, 7)
    for i in range(4):
        y = 120 + i * 15
        p.line([(82, y), (118, y + 3)], BONE, 4)
        p.line([(82, y), (72, y + 9)], BONE, 3)
    # череп
    p.ell(74, 42, 126, 100, BONE)
    p.ell(82, 62, 96, 78, INK, None)
    p.ell(104, 62, 118, 78, INK, None)
    p.ell(86, 66, 92, 74, TEAL, None)
    p.ell(108, 66, 114, 74, TEAL, None)
    p.poly([(94, 84), (106, 84), (100, 92)], INK, None)
    p.rect(88, 88, 112, 96, BONE, INK, 1.5)
    p.line([(90, 90), (110, 90)], INK, 1)
    p.line([(96, 90), (96, 95)], INK, 1)
    p.line([(104, 90), (104, 95)], INK, 1)
    # лук
    p.line([(146, 66), (166, 108), (146, 154)], (140, 96, 52, 255), 6)
    p.line([(146, 66), (146, 154)], (238, 227, 198, 180), 1.5)
    oline(p, [(138, 104), (168, 104)], BONE, 3)
    p.poly([(168, 98), (180, 104), (168, 110)], BONE, INK, 1.5)
    # колчан
    p.poly([(42, 92), (60, 84), (72, 150), (54, 156)], (90, 60, 38, 255))
    for i in range(3):
        p.line([(48 + i * 5, 92), (44 + i * 6, 70)], BONE, 3)
        p.poly([(40 + i * 6, 70), (46 + i * 6, 62), (49 + i * 6, 72)], RED, INK, 1)


def draw_boar(p):
    # тело
    p.ell(28, 58, 208, 168, (128, 92, 62, 255))
    p.ell(40, 74, 120, 146, (150, 110, 74, 255), None)
    # щетина
    for i in range(7):
        x = 44 + i * 22
        p.poly([(x, 70), (x + 6, 44), (x + 12, 72)], (96, 66, 44, 255), INK, 1.5)
    # ноги
    for x in (52, 82, 148, 178):
        p.rect(x, 156, x + 18, 188, (110, 76, 52, 255), INK, 2, r=3)
        p.rect(x, 180, x + 20, 190, INK, None, r=2)
    # голова
    p.ell(126, 44, 224, 150, (146, 104, 68, 255))
    p.ell(174, 82, 226, 134, (180, 136, 98, 255))
    p.ell(190, 96, 202, 108, INK, None)
    p.ell(208, 96, 220, 108, INK, None)
    p.ell(186, 78, 204, 94, (255, 220, 110, 255), INK, 1.5)
    p.ell(210, 78, 228, 94, (255, 220, 110, 255), INK, 1.5)
    p.ell(192, 82, 199, 90, INK, None)
    p.ell(216, 82, 223, 90, INK, None)
    # клыки
    p.poly([(184, 122), (180, 154), (196, 130)], BONE, INK, 2)
    p.poly([(218, 122), (222, 154), (206, 130)], BONE, INK, 2)
    p.ell(198, 110, 208, 120, INK, None)
    p.ell(210, 110, 220, 120, INK, None)
    # уши
    p.poly([(138, 52), (124, 16), (158, 36)], (128, 92, 62, 255), INK, 2)
    p.poly([(198, 48), (214, 14), (216, 52)], (128, 92, 62, 255), INK, 2)


def draw_bomber(p):
    # ноги
    p.line([(70, 166), (62, 198)], (90, 62, 44, 255), 7)
    p.line([(110, 166), (120, 198)], (90, 62, 44, 255), 7)
    p.ell(52, 192, 68, 206, (60, 44, 34, 255))
    p.ell(114, 192, 130, 206, (60, 44, 34, 255))
    # бомба
    p.ell(34, 54, 146, 174, (52, 42, 52, 255))
    p.ell(48, 68, 96, 108, (92, 78, 92, 255), None)
    # фитиль
    p.line([(90, 56), (104, 30), (88, 14), (76, 24)], (160, 120, 76, 255), 5)
    p.glow(72, 18, 12, (255, 160, 50), 160, 5)
    p.ell(66, 12, 78, 24, (255, 200, 90, 255), INK, 1.5)
    # глаза
    p.ell(56, 92, 80, 120, (255, 246, 224, 255), INK, 2)
    p.ell(100, 92, 124, 120, (255, 246, 224, 255), INK, 2)
    p.ell(64, 102, 74, 114, INK, None)
    p.ell(108, 102, 118, 114, INK, None)
    p.ell(66, 104, 70, 108, (255, 80, 60, 255), None)
    p.ell(110, 104, 114, 108, (255, 80, 60, 255), None)
    # злая ухмылка
    p.line([(66, 140), (90, 148), (116, 138)], INK, 3)
    for i in range(4):
        x = 72 + i * 10
        p.poly([(x, 143), (x + 4, 153), (x + 8, 143)], BONE, INK, 1)
    p.line([(58, 84), (76, 90)], INK, 2)
    p.line([(104, 90), (122, 84)], INK, 2)


def draw_guard(p):
    # плащ
    p.poly([(72, 96), (148, 96), (166, 268), (54, 268)], (120, 66, 62, 255), INK, 2.5)
    # ноги
    p.rect(76, 218, 108, 282, (110, 104, 92, 255), INK, 2.5, r=3)
    p.rect(116, 218, 148, 282, (110, 104, 92, 255), INK, 2.5, r=3)
    p.rect(70, 270, 110, 286, (70, 64, 58, 255), INK, 2, r=2)
    p.rect(114, 270, 154, 286, (70, 64, 58, 255), INK, 2, r=2)
    # корпус
    p.poly([(76, 104), (146, 104), (158, 212), (64, 212)], (142, 132, 114, 255))
    p.rect(92, 112, 130, 150, (120, 110, 96, 255), INK, 2, r=3)
    p.line([(82, 170), (150, 170)], (90, 82, 72, 255), 4)
    for i in range(4):
        p.ell(84 + i * 18, 120, 90 + i * 18, 126, GOLD, INK, 1)
    # голова/шлем
    p.poly([(84, 104), (84, 62), (100, 40), (130, 40), (146, 62), (146, 104)], (150, 140, 122, 255))
    p.rect(88, 66, 142, 82, (54, 48, 44, 255), INK, 2, r=3)
    p.ell(100, 70, 108, 78, (255, 210, 110, 255), None)
    p.ell(122, 70, 130, 78, (255, 210, 110, 255), None)
    p.poly([(112, 40), (124, 8), (132, 44)], RED, INK, 2)
    p.line([(104, 96), (112, 104), (122, 100)], (90, 82, 72, 255), 2)
    # щит
    p.poly([(4, 108), (62, 94), (74, 140), (58, 220), (22, 200), (2, 148)],
           (110, 82, 58, 255), INK, 3)
    p.poly([(12, 120), (56, 108), (63, 142), (50, 202), (24, 186), (10, 144)],
           (138, 106, 72, 255), (70, 46, 32, 255), 2)
    p.poly([(36, 122), (48, 156), (38, 176), (26, 154)], GOLD, INK, 2)
    # меч
    oline(p, [(156, 210), (190, 84)], (190, 190, 180, 255), 8)
    p.line([(144, 196), (166, 208)], GOLD, 6)
    p.line([(144, 196), (166, 208)], INK, 2)
    p.poly([(188, 88), (196, 66), (202, 92)], (220, 220, 210, 255), INK, 2)


def draw_shaman(p):
    # посох
    oline(p, [(172, 272), (166, 92)], (112, 78, 48, 255), 6)
    p.ell(150, 62, 184, 96, BONE, INK, 2.5)
    p.ell(158, 72, 166, 80, INK, None)
    p.ell(170, 72, 178, 80, INK, None)
    p.ell(160, 74, 164, 79, (255, 120, 90, 255), None)
    p.ell(172, 74, 176, 79, (255, 120, 90, 255), None)
    # мантия
    p.poly([(64, 272), (84, 136), (104, 116), (126, 116), (146, 136), (164, 272), (146, 280), (78, 280)],
           PURPLE, INK, 3)
    p.poly([(74, 258), (156, 258), (160, 276), (70, 276)], PURPLE_D, INK, 2)
    p.line([(104, 128), (100, 264)], (238, 227, 198, 90), 2)
    p.rect(88, 176, 142, 192, (190, 150, 90, 255), INK, 2)
    for i in range(4):
        p.ell(94 + i * 12, 180, 102 + i * 12, 188, TEAL if i % 2 else GOLD, INK, 1)
    # руки
    p.line([(88, 148), (62, 196)], GREEN, 12)
    p.line([(62, 196), (72, 210)], GREEN, 10)
    p.ell(56, 190, 74, 208, GREEN, INK, 2)
    p.line([(142, 148), (166, 190)], GREEN, 12)
    p.ell(158, 184, 176, 202, GREEN, INK, 2)
    # голова
    p.ell(88, 62, 140, 122, GREEN)
    p.ell(82, 76, 100, 102, (96, 150, 88, 255), INK, 2)
    p.ell(128, 76, 146, 102, (96, 150, 88, 255), INK, 2)
    # маска
    p.poly([(88, 68), (106, 54), (124, 60), (140, 68), (136, 98), (114, 112), (92, 98)], BONE, INK, 2.5)
    p.ell(96, 74, 108, 90, INK, None)
    p.ell(118, 74, 130, 90, INK, None)
    p.ell(100, 78, 104, 84, (255, 120, 90, 255), None)
    p.ell(122, 78, 126, 84, (255, 120, 90, 255), None)
    p.poly([(108, 90), (104, 102), (112, 104), (118, 94)], INK, None)
    p.line([(96, 96), (86, 104), (88, 116)], BONE, 2)
    p.line([(130, 96), (140, 104), (138, 116)], BONE, 2)
    p.poly([(92, 54), (84, 38), (100, 46)], RED, INK, 1.5)
    p.poly([(128, 52), (138, 34), (136, 52)], RED, INK, 1.5)


def draw_spider(p):
    # ноги
    for side in (-1, 1):
        for i in range(4):
            sx = 104 + side * 22
            sy = 78 + i * 6
            kx = 104 + side * (52 + i * 14)
            ky = 40 + i * 26
            ex = 104 + side * (78 + i * 10)
            ey = 46 + i * 30
            p.line([(sx, sy), (kx, ky), (ex, ey)], INK, 7)
            p.line([(sx, sy), (kx, ky), (ex, ey)], (92, 68, 108, 255), 4)
    # брюшко
    p.ell(52, 70, 156, 156, PURPLE_D)
    p.ell(66, 78, 142, 142, PURPLE, None)
    p.poly([(104, 88), (126, 112), (104, 132), (82, 112)], (72, 50, 88, 255), INK, 2)
    # голова
    p.ell(76, 42, 132, 98, (92, 68, 108, 255))
    for x, y, s in ((88, 58, 5), (104, 54, 6), (120, 58, 5), (96, 72, 4), (112, 72, 4)):
        p.ell(x - s, y - s, x + s, y + s, (255, 90, 80, 255), INK, 1)
        p.ell(x - 2, y - 2, x + 2, y + 2, (255, 230, 200, 255), None)
    # клыки
    p.poly([(94, 92), (90, 110), (100, 96)], BONE, INK, 1.5)
    p.poly([(114, 92), (118, 110), (108, 96)], BONE, INK, 1.5)


def draw_lich(p):
    # посох
    oline(p, [(318, 386), (300, 110)], (86, 62, 44, 255), 9)
    p.ell(272, 66, 324, 118, (90, 220, 190, 255), INK, 3)
    p.ell(286, 80, 300, 94, (220, 255, 240, 255), None)
    p.line([(292, 58), (304, 34)], (90, 220, 190, 200), 2)
    # мантия
    p.poly([(108, 386), (128, 210), (158, 168), (224, 160), (260, 196), (282, 386),
            (258, 398), (130, 398)], (74, 56, 96, 255))
    p.poly([(116, 366), (272, 366), (280, 392), (108, 392)], (52, 38, 70, 255))
    p.line([(188, 190), (190, 368)], (150, 120, 180, 90), 3)
    p.poly([(156, 250), (226, 250), (232, 268), (150, 268)], (120, 90, 60, 255), INK, 2)
    p.poly([(184, 252), (198, 252), (198, 266), (184, 266)], (90, 220, 190, 255), INK, 1.5)
    # руки
    p.line([(160, 210), (112, 280)], BONE, 10)
    p.line([(112, 280), (96, 318)], BONE, 8)
    p.ell(86, 310, 108, 334, BONE, INK, 2)
    p.line([(244, 210), (296, 260)], BONE, 10)
    p.line([(296, 260), (312, 296)], BONE, 8)
    p.ell(302, 288, 326, 314, BONE, INK, 2)
    # плечи
    p.ell(118, 168, 168, 208, BONE, INK, 2)
    p.ell(218, 168, 268, 208, BONE, INK, 2)
    # череп
    p.ell(150, 66, 246, 168, BONE)
    p.poly([(150, 130), (156, 168), (180, 186), (216, 186), (240, 168), (246, 130)], BONE, INK, 2.5)
    p.ell(164, 100, 194, 136, INK, None)
    p.ell(202, 100, 232, 136, INK, None)
    p.ell(172, 108, 186, 126, (90, 220, 190, 255), None)
    p.ell(210, 108, 224, 126, (90, 220, 190, 255), None)
    p.ell(178, 110, 182, 116, (240, 255, 245, 255), None)
    p.ell(216, 110, 220, 116, (240, 255, 245, 255), None)
    p.poly([(190, 140), (206, 140), (198, 154)], INK, None)
    p.rect(176, 156, 220, 172, BONE, INK, 2)
    for i in range(5):
        p.line([(182 + i * 9, 158), (182 + i * 9, 170)], INK, 1.5)
    # корона
    p.poly([(148, 62), (156, 18), (176, 44), (190, 8), (204, 44), (226, 16), (236, 62)], GOLD, INK, 2.5)
    p.ell(180, 36, 188, 44, RED, INK, 1)
    p.ell(204, 30, 212, 38, TEAL, INK, 1)
    p.ell(158, 40, 164, 46, TEAL, INK, 1)
    p.ell(228, 40, 234, 46, RED, INK, 1)


def draw_golem(p):
    # ноги
    p.rect(54, 200, 92, 252, (128, 112, 94, 255), INK, 3, r=6)
    p.rect(108, 200, 146, 252, (128, 112, 94, 255), INK, 3, r=6)
    # корпус
    p.rect(46, 92, 154, 214, (150, 132, 110, 255), INK, 3, r=10)
    p.rect(60, 106, 140, 190, (128, 112, 94, 255), (90, 78, 66, 255), 2, r=6)
    p.line([(100, 118), (92, 156), (108, 176)], (90, 220, 190, 210), 3)
    p.line([(100, 118), (112, 130)], (90, 220, 190, 210), 3)
    # голова
    p.rect(72, 36, 128, 96, (142, 124, 104, 255), INK, 3, r=8)
    p.rect(82, 54, 100, 70, (90, 220, 190, 255), INK, 2, r=2)
    p.rect(104, 54, 122, 70, (90, 220, 190, 255), INK, 2, r=2)
    p.line([(86, 80), (116, 86)], INK, 2)
    # руки
    p.rect(10, 104, 44, 196, (138, 120, 100, 255), INK, 3, r=8)
    p.rect(156, 104, 190, 196, (138, 120, 100, 255), INK, 3, r=8)
    p.rect(6, 180, 48, 218, (120, 104, 86, 255), INK, 3, r=6)
    p.rect(152, 180, 194, 218, (120, 104, 86, 255), INK, 3, r=6)
    p.ell(24, 128, 38, 142, (90, 220, 190, 255), None)
    p.ell(162, 140, 176, 154, (90, 220, 190, 255), None)


def actors():
    actor("wizard", 240, 320, draw_wizard)
    actor("imp", 180, 210, draw_imp)
    actor("skeleton", 200, 270, draw_skeleton)
    actor("boar", 240, 200, draw_boar)
    actor("bomber", 180, 220, draw_bomber)
    actor("guard", 220, 300, draw_guard)
    actor("shaman", 200, 290, draw_shaman)
    actor("spider", 210, 170, draw_spider)
    actor("lich", 360, 420, draw_lich, border=4, thickness=5)
    actor("golem", 200, 260, draw_golem)


# ------------------------------------------------------------------ иконки
def icon(name, draw, bg=(40, 30, 48, 255), ring=(170, 132, 80, 255)):
    p = Pic(96, 96, 2)
    p.ell(7, 7, 89, 89, bg, ring, 2.5)
    p.ell(13, 13, 83, 83, (0, 0, 0, 0), (255, 255, 255, 20), 1)
    draw(p)
    p.save("icon_" + name, sticker=True, border=1.2, thickness=1.6, grain=2)


def sym_flame(p, color=(245, 140, 54, 255), inner=(255, 224, 140, 255)):
    p.poly([(48, 16), (64, 40), (58, 48), (70, 64), (58, 78), (38, 78), (26, 64), (34, 50),
            (38, 34)], color, INK, 2.2)
    p.poly([(48, 36), (55, 54), (48, 70), (40, 54)], inner, None)


def sym_shield(p, color=(90, 150, 210, 255)):
    p.poly([(48, 16), (74, 26), (72, 54), (48, 80), (24, 54), (22, 26)], color, INK, 2.5)
    p.poly([(48, 26), (64, 33), (63, 52), (48, 68), (33, 52), (32, 33)], (0, 0, 0, 0), (210, 240, 255, 200), 1.8)
    p.line([(48, 34), (48, 58)], CREAM, 3)
    p.line([(37, 44), (59, 44)], CREAM, 3)


def sym_snow(p, color=(160, 220, 245, 255)):
    for i in range(6):
        a = math.radians(i * 60)
        x, y = 48 + math.cos(a) * 26, 48 + math.sin(a) * 26
        p.line([(48, 48), (x, y)], color, 2.5)
        x1, y1 = 48 + math.cos(a) * 16, 48 + math.sin(a) * 16
        for da in (-0.5, 0.5):
            p.line([(x1, y1), (x1 + math.cos(a + da) * 9, y1 + math.sin(a + da) * 9)], color, 2)
    p.ell(43, 43, 53, 53, CREAM, INK, 1.5)


def sym_bolt(p):
    p.poly([(54, 12), (30, 46), (46, 46), (38, 82), (66, 44), (50, 44), (60, 12)], (255, 226, 120, 255), INK, 2.2)


def sym_heart(p, color=RED):
    p.poly([(48, 80), (20, 50), (20, 32), (30, 22), (42, 28), (48, 36), (54, 28), (66, 22), (76, 32),
            (76, 50)], color, INK, 2.4)
    p.ell(28, 28, 42, 42, (255, 190, 180, 160), None)


def sym_drop(p, color=(100, 170, 100, 255)):
    p.poly([(48, 14), (68, 44), (64, 66), (48, 80), (32, 66), (28, 44)], color, INK, 2.4)
    p.ell(36, 50, 48, 64, (210, 245, 200, 130), None)


def sym_meteor(p):
    p.poly([(26, 54), (10, 82), (34, 70), (28, 86), (52, 70)], (255, 160, 60, 210), None)
    p.ell(40, 24, 72, 56, (140, 96, 70, 255), INK, 2.5)
    p.ell(46, 30, 60, 42, (190, 140, 100, 255), None)


def sym_blink(p):
    p.line([(24, 66), (44, 46), (24, 26)], (140, 230, 220, 255), 5)
    p.line([(50, 66), (70, 46), (50, 26)], (140, 230, 220, 255), 5)
    p.ell(74, 20, 82, 28, CREAM, None)


def sym_missiles(p):
    for i, y in enumerate((30, 48, 66)):
        p.poly([(28, y - 6), (62, y), (28, y + 6), (34, y)], CREAM, INK, 1.8)


def sym_clock(p):
    p.ell(20, 20, 76, 76, (58, 44, 68, 255), CREAM, 2.5)
    p.line([(48, 48), (48, 33)], CREAM, 3)
    p.line([(48, 48), (60, 54)], CREAM, 3)
    p.ell(44, 44, 52, 52, GOLD, INK, 1.5)


def sym_golem(p):
    p.rect(26, 30, 70, 74, (144, 126, 106, 255), INK, 2.5, r=5)
    p.rect(33, 40, 45, 52, (90, 220, 190, 255), INK, 1.5, r=1)
    p.rect(51, 40, 63, 52, (90, 220, 190, 255), INK, 1.5, r=1)
    p.line([(34, 62), (62, 66)], INK, 2)


def sym_rage(p):
    sym_flame(p, (220, 78, 62, 255), (255, 200, 120, 255))
    p.line([(48, 24), (48, 6)], INK, 3)
    p.poly([(40, 10), (48, 2), (56, 10)], GOLD, INK, 1.5)


def sym_sword(p):
    p.line([(26, 70), (62, 30)], INK, 8)
    p.line([(26, 70), (62, 30)], (200, 200, 190, 255), 5)
    p.line([(22, 56), (40, 74)], GOLD, 4)
    p.line([(22, 56), (40, 74)], INK, 1)
    p.line([(20, 76), (28, 68)], (140, 100, 60, 255), 5)


def sym_boot(p):
    p.poly([(40, 16), (58, 16), (58, 48), (72, 62), (72, 74), (34, 74), (34, 54)], (150, 104, 64, 255), INK, 2.4)
    p.line([(38, 58), (60, 58)], (90, 60, 40, 255), 2)


def sym_star(p):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        r = 28 if i % 2 == 0 else 12
        pts.append((48 + math.cos(a) * r, 48 + math.sin(a) * r))
    p.poly(pts, (255, 220, 120, 255), INK, 2)


def sym_arrow(p):
    p.line([(18, 62), (68, 34)], INK, 7)
    p.line([(18, 62), (68, 34)], (222, 210, 180, 255), 4)
    p.poly([(64, 24), (80, 30), (66, 42)], CREAM, INK, 1.8)
    p.line([(18, 54), (28, 64)], CREAM, 2)


def sym_multi(p):
    for dy in (-14, 0, 14):
        p.line([(20, 62 + dy * .3), (58, 40 + dy)], INK, 5)
        p.line([(20, 62 + dy * .3), (58, 40 + dy)], CREAM, 2.5)
        p.poly([(56, 34 + dy), (72, 40 + dy), (58, 48 + dy)], CREAM, INK, 1.5)


def sym_bomb(p):
    p.ell(24, 32, 70, 78, (52, 42, 52, 255), INK, 2.5)
    p.line([(52, 36), (62, 20), (52, 12)], (160, 120, 76, 255), 4)
    p.ell(46, 6, 58, 18, (255, 190, 80, 255), INK, 1.5)
    p.ell(36, 44, 46, 56, (92, 78, 92, 255), None)


def sym_slot(p):
    p.rect(24, 24, 72, 72, (0, 0, 0, 0), CREAM, 3, r=5)
    p.line([(48, 32), (48, 64)], GOLD, 5)
    p.line([(32, 48), (64, 48)], GOLD, 5)


def sym_thorns(p):
    p.ell(30, 30, 66, 66, (110, 90, 70, 255), INK, 2.5)
    for i in range(8):
        a = i * math.pi / 4
        x1, y1 = 48 + math.cos(a) * 18, 48 + math.sin(a) * 18
        x2, y2 = 48 + math.cos(a) * 32, 48 + math.sin(a) * 32
        p.line([(x1, y1), (x2, y2)], CREAM, 3)


def sym_cross(p):
    p.rect(38, 20, 58, 76, (240, 230, 205, 255), INK, 2.5, r=3)
    p.rect(20, 38, 76, 58, (240, 230, 205, 255), INK, 2.5, r=3)


def sym_phoenix(p):
    p.poly([(48, 82), (26, 52), (32, 30), (46, 44), (48, 18), (54, 44), (68, 30), (70, 54)],
           (230, 100, 70, 255), INK, 2.4)
    p.poly([(48, 68), (38, 50), (48, 36), (58, 50)], (255, 210, 120, 255), None)


def icons():
    icon("fireball", sym_flame)
    icon("shield", sym_shield)
    icon("nova", sym_snow)
    icon("chain", sym_bolt)
    icon("heal", sym_heart)
    icon("poison", sym_drop)
    icon("meteor", sym_meteor)
    icon("blink", sym_blink)
    icon("missiles", sym_missiles)
    icon("clock", sym_clock)
    icon("golem", sym_golem)
    icon("rage", sym_rage)
    icon("perk_damage", sym_sword)
    icon("perk_speed", sym_boot)
    icon("perk_hp", sym_heart)
    icon("perk_lifesteal", lambda p: sym_drop(p, (200, 70, 80, 255)))
    icon("perk_crit", sym_star)
    icon("perk_pierce", sym_arrow)
    icon("perk_multi", sym_multi)
    icon("perk_bomb", sym_bomb)
    icon("perk_shield", sym_shield)
    icon("perk_cd", sym_clock)
    icon("perk_slot", sym_slot)
    icon("perk_freeze", sym_snow)
    icon("perk_poison", lambda p: sym_drop(p, (100, 170, 100, 255)))
    icon("perk_thorns", sym_thorns)
    icon("perk_heal", sym_cross)
    icon("perk_phoenix", sym_phoenix)
    icon("perk_armor", lambda p: sym_shield(p, (150, 140, 120, 255)))


# ------------------------------------------------------------------ логотип
def logo():
    W, H = 1400, 340
    p = Pic(W, H, 2)
    # тень
    p.text((W / 2 + 7, 218 + 7), "ПОДЗЕМЕЛЬЕ", 122, (0, 0, 0, 180), stroke=10, stroke_fill=(0, 0, 0, 170))
    p.text((W / 2, 218), "ПОДЗЕМЕЛЬЕ", 122, (242, 193, 102, 255), stroke=10, stroke_fill=INK)
    p.text((W / 2, 72), "КАРТОННОЕ", 56, CREAM, stroke=5, stroke_fill=INK)
    # бумажные звездочки
    for x, y, r in ((150, 130, 8), (1250, 120, 7), (1285, 205, 4), (112, 215, 5)):
        pts = []
        for i in range(8):
            a = i * math.pi / 4
            rr = r if i % 2 == 0 else r * .45
            pts.append((x + math.cos(a) * rr, y + math.sin(a) * rr))
        p.poly(pts, GOLD, INK, 1.5)
    p.fibers(20, (255, 255, 255, 20))
    p.save("ui_logo", grain=2.5)


def contact_sheet():
    files = sorted(OUT.glob("*.png"))
    cols = 8
    cell = 180
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * (cell + 26)), (26, 20, 30, 255))
    d = ImageDraw.Draw(sheet)
    font = fnt(FONT_SANS_R, 14)
    for i, f in enumerate(files):
        im = Image.open(f).convert("RGBA")
        im.thumbnail((cell - 16, cell - 16), Image.LANCZOS)
        x = (i % cols) * cell + (cell - im.width) // 2
        y = (i // cols) * (cell + 26) + (cell - im.height) // 2
        sheet.alpha_composite(im, (x, y))
        d.text(((i % cols) * cell + 6, (i // cols) * (cell + 26) + cell + 2), f.stem, font=font,
               fill=(235, 220, 190, 255))
    sheet.save(ROOT / "tools" / "_contact.png")


def main():
    random.seed(20261022)
    floor_tile("floor")
    floor_tile("floor_boss", base=(88, 62, 50), line=(66, 44, 38), boss=True)
    wall("wall_h", 256, 96)
    wall("wall_v", 96, 256, True)
    prop("prop_rock", 96, 80, draw_rock)
    prop("prop_bones", 84, 68, draw_bones)
    prop("prop_barrel", 64, 84, draw_barrel)
    prop("prop_column", 72, 176, draw_column)
    prop("prop_chest", 96, 84, draw_chest)
    prop("prop_campfire", 96, 84, draw_campfire)
    prop("prop_torch", 48, 84, draw_torch)
    prop("fx_flame", 64, 72, draw_flame)
    draw_door("door_leaf")
    draw_door_frame("door_frame")
    draw_bolt("proj_bolt", TEAL, (32, 48))
    draw_bolt("proj_fire", (245, 145, 60, 255), (36, 52))
    draw_bolt("proj_ice", (130, 210, 240, 255), (32, 48))
    draw_bolt("proj_poison", (120, 190, 100, 255), (32, 48))
    draw_arrow()
    draw_skull_proj()
    draw_slash_fx()
    draw_ring("fx_ring", (140, 230, 220, 255))
    draw_ring("fx_ring_gold", (255, 210, 120, 255))
    draw_explosion()
    draw_dust()
    draw_poison_puddle()
    draw_shield_fx()
    draw_meteor()
    panel("ui_panel")
    panel("ui_panel_dark", fill=(26, 20, 32, 235), border=(120, 92, 58, 255))
    button("ui_button")
    button("ui_button_hover", True)
    card("ui_card")
    card("ui_card_dark", True)
    slot()
    heart()
    vignette()
    grain_overlay()
    white()
    shadow()
    reticle()
    glow_sprite()
    bars()
    table_bg()
    actors()
    icons()
    logo()
    contact_sheet()
    print("generated", len(list(OUT.glob("*.png"))), "files in", OUT)


if __name__ == "__main__":
    main()
