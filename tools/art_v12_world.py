# -*- coding: utf-8 -*-
"""Dark dungeon floors, walls and hand-drawn props for v1.2."""
import math
import random

from PIL import Image, ImageDraw

from art_v12_common import (SS, INK, INK_SOFT, BONE, BONE_D, PALE, GOLD, GOLD_L, GOLD_D,
                            RED, RED_D, BLOOD, STEEL, STEEL_L, STEEL_D, PURPLE, PURPLE_D,
                            CYAN, GREEN, GREEN_D, BROWN, BROWN_D, WOOD, BLACK, FLESH,
                            canvas, Pen, add_glow, add_speckles, finish, noise_panel,
                            paper_cracks, bone_piece, shade, mix)


def _stone_tiles(p, w, h, base, size, mortar, seed, bevel=True):
    for y in range(0, h, size):
        for x in range(0, w, size):
            rnd = random.Random(seed + x * 7 + y * 13)
            xx = (x + (size // 2 if (y // size) % 2 else 0)) % w
            col = shade(base, 0.82 + rnd.random() * 0.28)
            p.rect([xx + 2, y + 2, xx + size - 3, y + size - 3], col, mortar, 2)
            if bevel:
                p.line([(xx + 6, y + 5), (xx + size - 8, y + 5)], shade(col, 1.18), 1.2,
                       outline=None, caps=False)
                p.line([(xx + 5, y + size - 7), (xx + size - 8, y + size - 7)], shade(col, 0.62), 1.2,
                       outline=None, caps=False)


def _moss_blob(p, cx, cy, r, rnd):
    pts = []
    n = 14
    for i in range(n):
        a = math.tau * i / n
        rr = r * (0.65 + rnd.random() * 0.45)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr * 0.62))
    p.poly(pts, (55, 67, 43, 205), (33, 42, 29, 235), 1)
    for i in range(10):
        p.circle(cx + rnd.uniform(-r, r), cy + rnd.uniform(-r * 0.5, r * 0.5),
                 rnd.uniform(1.5, 4.5), (76, 91, 54, 170), None, 0)


def _blood_pool(p, cx, cy, r, rnd):
    pts = []
    for i in range(16):
        a = math.tau * i / 16
        rr = r * (0.6 + rnd.random() * 0.55)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr * 0.55))
    p.poly(pts, (78, 20, 27, 175), (45, 12, 18, 200), 1)
    for i in range(5):
        p.circle(cx + rnd.uniform(-r, r), cy + rnd.uniform(-r * 0.4, r * 0.4),
                 rnd.uniform(1.5, 4), (118, 29, 35, 160), None, 0)


def _web_cluster(p, cx, cy, r, seed):
    rnd = random.Random(seed)
    spokes = 9
    for i in range(spokes):
        a = math.tau * i / spokes + rnd.uniform(-0.1, 0.1)
        p.line([(cx, cy), (cx + math.cos(a) * r, cy + math.sin(a) * r * 0.72)],
               (206, 196, 198, 110), 1.0, outline=None, caps=False)
    for j in range(1, 5):
        rr = r * j / 5.0
        pts = []
        for i in range(spokes + 1):
            a = math.tau * i / spokes
            px = cx + math.cos(a) * rr
            py = cy + math.sin(a) * rr * 0.72
            if i % 2:
                px += rnd.uniform(-2.5, 2.5)
                py += rnd.uniform(-2.5, 2.5)
            pts.append((px, py))
        p.line(pts, (198, 188, 194, 85), 1.0, outline=None, caps=False)
    for i in range(3):
        ex = cx + rnd.uniform(-r * 0.6, r * 0.6)
        ey = cy + rnd.uniform(-r * 0.35, r * 0.35)
        p.ellipse([ex - 4, ey - 5, ex + 4, ey + 5], (190, 176, 170, 200),
                  (91, 78, 82, 230), 1)


def _floor(name, base, kind, seed):
    w = h = 512
    im = noise_panel(w, h, base, 8.0, seed)
    ov = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    p = Pen(ov, 1)

    if kind == "crypt":
        _stone_tiles(p, w, h, (112, 102, 88), 128, (38, 31, 28, 255), seed)
        for i in range(6):
            rnd = random.Random(seed + 20 + i)
            _moss_blob(p, rnd.uniform(30, 482), rnd.uniform(30, 482), rnd.uniform(22, 48), rnd)
        for i in range(11):
            rnd = random.Random(seed + 80 + i)
            bone_piece(p, rnd.uniform(28, 484), rnd.uniform(28, 484), rnd.uniform(12, 28),
                       rnd.uniform(0, math.tau), rnd.uniform(2.4, 4.0), (190, 174, 143, 220))
        for i in range(10):
            rnd = random.Random(seed + 140 + i)
            _blood_pool(p, rnd.uniform(30, 480), rnd.uniform(30, 480), rnd.uniform(5, 13), rnd)
    elif kind == "web":
        _stone_tiles(p, w, h, (86, 74, 88), 112, (32, 25, 36, 255), seed)
        for i in range(4):
            rnd = random.Random(seed + 30 + i)
            _moss_blob(p, rnd.uniform(30, 480), rnd.uniform(30, 480), rnd.uniform(16, 32), rnd)
        for i in range(7):
            rnd = random.Random(seed + 90 + i)
            _web_cluster(p, rnd.uniform(30, 482), rnd.uniform(30, 482), rnd.uniform(28, 62), seed + i)
        for i in range(24):
            rnd = random.Random(seed + 160 + i)
            cx, cy = rnd.uniform(14, 498), rnd.uniform(14, 498)
            p.ellipse([cx - 4, cy - 6, cx + 4, cy + 6], (197, 184, 173, 190),
                      (92, 80, 82, 220), 1)
        for i in range(12):
            rnd = random.Random(seed + 200 + i)
            x, y = rnd.uniform(10, 500), rnd.uniform(10, 500)
            p.line([(x, y), (x + rnd.uniform(-22, 22), y + rnd.uniform(-16, 16))],
                   (119, 99, 103, 160), 1.4, outline=None, caps=False)
    else:
        _stone_tiles(p, w, h, (80, 73, 91), 170, (25, 21, 32, 255), seed)
        for i in range(16):
            rnd = random.Random(seed + 40 + i)
            x, y = rnd.uniform(0, 512), rnd.uniform(0, 512)
            pts = [(x, y)]
            ang = rnd.uniform(-0.7, 0.7)
            for _ in range(rnd.randint(3, 6)):
                ang += rnd.uniform(-0.4, 0.4)
                x += math.cos(ang) * rnd.uniform(16, 50)
                y += math.sin(ang) * rnd.uniform(10, 26)
                pts.append((x, y))
            p.line(pts, (159, 121, 53, 150), 1.2, outline=(30, 20, 18, 120), caps=False)
        for i in range(7):
            rnd = random.Random(seed + 120 + i)
            _blood_pool(p, rnd.uniform(20, 492), rnd.uniform(20, 492), rnd.uniform(8, 20), rnd)
        for i in range(20):
            rnd = random.Random(seed + 230 + i)
            p.circle(rnd.uniform(0, 512), rnd.uniform(0, 512), rnd.uniform(0.8, 2.3),
                     (34, 29, 38, 210), None, 0)

    for i in range(18):
        paper_cracks(p, 6, 6, 500, 500, (22, 17, 20, 180), 1, seed + 300 + i)
    im.alpha_composite(ov)
    finish(im, name, w, h, grain=1.8, scale=1)


def _wall(name, base, kind, seed):
    w, h = 512, 128
    im = noise_panel(w, h, base, 7.0, seed)
    ov = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    p = Pen(ov, 1)

    p.rect([0, 0, w, 12], shade(base, 1.45), (30, 23, 24, 255), 2)
    p.rect([0, 12, w, 21], shade(base, 0.58), (25, 19, 22, 255), 1)
    p.rect([0, h - 16, w, h], shade(base, 0.48), (21, 16, 19, 255), 2)
    row_h = (h - 30) / 4.0
    for row in range(4):
        y = 21 + row * row_h
        offset = 0 if row % 2 == 0 else 42
        bw = 84
        for x in range(-bw, w + bw, bw):
            xx = x + offset
            rnd = random.Random(seed + row * 37 + x)
            col = shade(base, 0.84 + rnd.random() * 0.28)
            p.rect([xx + 2, y + 2, xx + bw - 3, y + row_h - 3], col, (31, 24, 26, 255), 2)
            p.line([(xx + 7, y + 5), (xx + bw - 8, y + 5)], shade(col, 1.22), 1.1,
                   outline=None, caps=False)
            p.line([(xx + 6, y + row_h - 7), (xx + bw - 8, y + row_h - 7)], shade(col, 0.58), 1.0,
                   outline=None, caps=False)

    if kind == "crypt":
        for i in range(5):
            rnd = random.Random(seed + 20 + i)
            _moss_blob(p, rnd.uniform(0, w), 27, rnd.uniform(8, 22), rnd)
        for i in range(6):
            rnd = random.Random(seed + 70 + i)
            bx = rnd.uniform(18, 492)
            p.line([(bx, 62), (bx + rnd.uniform(-32, 32), 108)], (181, 162, 128, 220),
                   rnd.uniform(1.6, 3.2), outline=(38, 27, 26, 220))
            p.circle(bx, 62, 4, (206, 188, 153, 230), (45, 31, 28, 230), 1)
        for i in range(4):
            rnd = random.Random(seed + 110 + i)
            _blood_pool(p, rnd.uniform(20, 492), rnd.uniform(30, 115), rnd.uniform(5, 12), rnd)
    elif kind == "web":
        for i in range(5):
            rnd = random.Random(seed + 30 + i)
            _web_cluster(p, rnd.uniform(0, w), rnd.uniform(28, 105), rnd.uniform(20, 38), seed + i)
        for i in range(10):
            rnd = random.Random(seed + 90 + i)
            cx, cy = rnd.uniform(4, 508), rnd.uniform(26, 112)
            p.ellipse([cx - 3, cy - 5, cx + 3, cy + 5], (197, 184, 173, 200),
                      (88, 76, 80, 230), 1)
        for i in range(8):
            rnd = random.Random(seed + 160 + i)
            x = rnd.uniform(0, w)
            p.line([(x, 22), (x + rnd.uniform(-14, 14), 112)],
                   (51, 38, 52, 180), 1.2, outline=None, caps=False)
    else:
        for i in range(6):
            rnd = random.Random(seed + 40 + i)
            cx = rnd.uniform(0, w)
            p.rect([cx - 2, 17, cx + 2, 112], (154, 116, 51, 150), (33, 24, 23, 150), 1)
        for i in range(10):
            rnd = random.Random(seed + 100 + i)
            cx, cy = rnd.uniform(0, w), rnd.uniform(26, 112)
            p.line([(cx, cy), (cx + rnd.uniform(-2, 3), cy + rnd.uniform(10, 28))],
                   (105, 27, 33, 170), rnd.uniform(1.2, 3), outline=None, caps=False)
        for i in range(4):
            rnd = random.Random(seed + 160 + i)
            _blood_pool(p, rnd.uniform(20, 492), rnd.uniform(34, 110), rnd.uniform(6, 15), rnd)

    for i in range(10):
        paper_cracks(p, 5, 20, 502, 96, (18, 13, 17, 180), 1, seed + 200 + i)
    im.alpha_composite(ov)
    finish(im, name, w, h, grain=1.9, scale=1)


def floors_and_walls():
    _floor("floor_crypt_a", (54, 48, 42, 255), "crypt", 11)
    _floor("floor_crypt_b", (60, 52, 44, 255), "crypt", 12)
    _floor("floor_web_a", (49, 43, 50, 255), "web", 21)
    _floor("floor_web_b", (55, 45, 54, 255), "web", 22)
    _floor("floor_throne_a", (42, 37, 48, 255), "throne", 31)
    _floor("floor_throne_b", (48, 42, 54, 255), "throne", 32)
    _wall("wall_crypt", (64, 56, 48, 255), "crypt", 41)
    _wall("wall_web", (56, 47, 58, 255), "web", 42)
    _wall("wall_throne", (44, 39, 51, 255), "throne", 43)


# --------------------------------------------------------------------------- props

def _prop(name, w, h, fn, glow=None):
    im = canvas(w, h)
    if glow:
        add_glow(im, glow[0], glow[1], glow[2], glow[3])
    p = Pen(im)
    fn(p, w, h)
    finish(im, name, w, h, grain=2.0)


def prop_crate(p, w, h):
    p.poly([(12, 74), (48, 86), (84, 74), (84, 22), (48, 8), (12, 22)], WOOD)
    p.poly([(12, 22), (48, 36), (84, 22), (84, 10), (48, 4), (12, 10)], shade(WOOD, 1.18))
    p.line([(20, 26), (20, 72)], BROWN_D, 5)
    p.line([(48, 34), (48, 84)], BROWN_D, 5)
    p.line([(76, 26), (76, 72)], BROWN_D, 5)
    p.line([(17, 48), (79, 58)], (58, 40, 25, 255), 4)
    p.line([(48, 18), (48, 28)], (30, 22, 18, 255), 3)
    p.line([(20, 68), (40, 80)], (30, 22, 18, 220), 2, outline=None, caps=False)
    p.line([(56, 82), (78, 68)], (30, 22, 18, 220), 2, outline=None, caps=False)


def prop_moss(p, w, h):
    rnd = random.Random(7)
    for i in range(18):
        x = rnd.uniform(8, 120)
        y = rnd.uniform(14, 56)
        r = rnd.uniform(9, 22)
        p.ellipse([x - r, y - r * 0.6, x + r, y + r * 0.6],
                  rnd.choice([(65, 86, 56, 220), (51, 69, 46, 220), (78, 100, 62, 200)]),
                  (34, 48, 34, 220), 2)
    for i in range(9):
        x, y = rnd.uniform(10, 118), rnd.uniform(14, 56)
        p.line([(x, y), (x + rnd.uniform(-5, 5), y - rnd.uniform(7, 15))],
               (104, 126, 82, 220), 2, outline=None, caps=False)


def prop_sarcophagus(p, w, h):
    p.poly([(15, 88), (44, 99), (132, 99), (156, 86), (143, 22), (118, 12), (35, 12), (9, 24)],
           (64, 60, 53, 255))
    p.poly([(24, 80), (46, 89), (127, 89), (147, 78), (136, 30), (115, 22), (41, 22), (19, 31)],
           (92, 82, 70, 255))
    p.poly([(39, 29), (116, 29), (126, 37), (126, 57), (112, 64), (44, 64), (29, 55), (29, 37)],
           (55, 50, 47, 255), INK, 3)
    p.ellipse([58, 34, 98, 58], (119, 105, 86, 255), INK, 2)
    p.circle(72, 44, 4, INK, INK, 1)
    p.circle(85, 44, 4, INK, INK, 1)
    p.poly([(76, 47), (72, 54), (81, 54)], INK, INK, 1)
    p.line([(55, 58), (102, 58)], (146, 131, 108, 220), 2)
    p.line([(15, 82), (34, 88)], (120, 108, 88, 200), 2, outline=None, caps=False)


def prop_rack(p, w, h):
    p.line([(21, 28), (23, 160)], (78, 53, 32, 255), 8, outline=INK)
    p.line([(117, 28), (119, 160)], (78, 53, 32, 255), 8, outline=INK)
    p.line([(18, 50), (122, 58)], (96, 66, 40, 255), 7, outline=INK)
    p.line([(20, 126), (120, 134)], (96, 66, 40, 255), 7, outline=INK)
    # swords and spears
    p.line([(38, 132), (47, 30)], STEEL, 5, outline=INK)
    p.line([(33, 42), (54, 44)], GOLD_D, 4, outline=INK)
    p.line([(50, 132), (61, 22)], STEEL_L, 4, outline=INK)
    p.poly([(61, 18), (56, 35), (66, 35)], STEEL, INK, 2)
    p.line([(75, 128), (82, 24)], (96, 66, 40, 255), 5, outline=INK)
    p.poly([(82, 17), (76, 33), (88, 33)], STEEL, INK, 2)
    p.line([(95, 132), (92, 42)], STEEL_D, 5, outline=INK)
    p.poly([(80, 42), (104, 42), (99, 56), (85, 56)], STEEL, INK, 2)
    p.line([(105, 128), (116, 36)], WOOD, 4, outline=INK)
    p.line([(111, 41), (122, 39)], BONE_D, 3, outline=INK)


def prop_shield(p, w, h):
    p.poly([(45, 8), (80, 18), (80, 64), (45, 111), (10, 64), (10, 18)], STEEL_D)
    p.poly([(45, 17), (71, 24), (71, 60), (45, 96), (19, 60), (19, 24)], STEEL)
    p.line([(45, 18), (45, 95)], GOLD_D, 3)
    p.line([(21, 53), (69, 53)], GOLD_D, 3)
    p.circle(45, 48, 12, GOLD, INK, 2)
    p.poly([(45, 37), (51, 48), (45, 59), (39, 48)], RED_D, INK, 1)
    p.line([(24, 71), (35, 82)], (174, 190, 196, 180), 2, outline=None, caps=False)


def prop_chain(p, w, h):
    for i in range(9):
        y = 15 + i * 18
        p.ellipse([28, y, 52, y + 18], None, STEEL_D, 4)
        p.ellipse([32, y + 3, 48, y + 15], None, STEEL, 2)
        p.line([(30, y + 9), (26, y + 9)], STEEL_D, 3, outline=None, caps=False)
    p.line([(40, 8), (40, 1)], STEEL_D, 3)
    p.circle(40, 172, 7, STEEL_D, INK, 2)


def prop_cage(p, w, h):
    p.line([(60, 4), (60, 25)], STEEL_D, 4, outline=INK)
    p.ellipse([26, 22, 94, 42], None, STEEL_D, 4)
    p.line([(30, 28), (30, 138)], STEEL_D, 4, outline=INK)
    p.line([(90, 28), (90, 138)], STEEL_D, 4, outline=INK)
    p.line([(44, 30), (40, 138)], STEEL_D, 3, outline=INK)
    p.line([(76, 30), (80, 138)], STEEL_D, 3, outline=INK)
    p.ellipse([26, 132, 94, 150], None, STEEL_D, 5)
    p.circle(46, 128, 8, BONE, INK, 2)
    p.circle(67, 130, 7, BONE_D, INK, 2)
    p.line([(42, 123), (38, 134)], BONE_D, 2, outline=None, caps=False)
    p.line([(71, 124), (75, 134)], BONE_D, 2, outline=None, caps=False)


def prop_skullpile(p, w, h):
    rnd = random.Random(13)
    for i in range(11):
        x, y = rnd.uniform(18, 102), rnd.uniform(36, 72)
        r = rnd.uniform(8, 14)
        p.circle(x, y, r, BONE_D if i % 3 else BONE, INK, 2)
        p.circle(x - r * 0.35, y - 1, r * 0.17, INK, INK, 1)
        p.circle(x + r * 0.35, y - 1, r * 0.17, INK, INK, 1)
        p.line([(x - 3, y + r * 0.4), (x + 3, y + r * 0.4)], INK, 1.2, outline=None, caps=False)
    for i in range(7):
        x, y = rnd.uniform(12, 108), rnd.uniform(58, 78)
        p.line([(x, y), (x + rnd.uniform(8, 20), y + rnd.uniform(-4, 4))], BONE_D, 3, outline=INK)


def prop_ribs(p, w, h):
    p.line([(18, 148), (36, 34), (90, 13), (151, 30), (174, 146)], BONE_D, 12, outline=INK)
    for i in range(5):
        y = 42 + i * 18
        p.arc([25, y - 18, 165, y + 46], 198, 342, BONE, 8)
    p.line([(36, 32), (24, 20)], BONE_D, 6, outline=INK)
    p.line([(151, 30), (168, 17)], BONE_D, 6, outline=INK)
    for i in range(9):
        p.circle(42 + i * 14, 151 + (i % 3) * 3, 4, BONE_D, INK, 1)


def prop_candles(p, w, h):
    for cx, base, top, col in [(24, 100, 43, PALE), (45, 101, 28, (198, 184, 153, 255)),
                               (65, 100, 50, PALE), (82, 101, 37, (210, 190, 154, 255))]:
        p.poly([(cx - 6, base), (cx - 4, top), (cx + 4, top), (cx + 6, base)], col)
        p.ellipse([cx - 6, base - 4, cx + 6, base + 4], shade(col, 0.8), INK, 2)
        add_glow(p.image, cx, top - 9, 15, (255, 177, 73, 95), p.s)
        p.poly([(cx, top - 18), (cx - 4, top - 5), (cx, top), (cx + 4, top - 5)], (255, 207, 112, 255),
               (231, 128, 48, 255), 1)
    p.ellipse([13, 94, 94, 109], (84, 66, 46, 220), INK, 3)


def prop_web(p, w, h):
    cx, cy = 16, 18
    for j in range(11):
        ang = math.tau * j / 11
        p.line([(cx, cy), (cx + math.cos(ang) * 180, cy + math.sin(ang) * 175)],
               (196, 186, 190, 150), 1.4, outline=None, caps=False)
    for r in (32, 62, 94, 128, 162):
        p.arc([cx - r, cy - r, cx + r, cy + r], 0, 360, (196, 186, 190, 135), 1.2)
    p.ellipse([102, 54, 138, 110], (188, 178, 174, 195), (105, 91, 94, 220), 3)
    p.line([(112, 72), (126, 82)], (118, 104, 106, 220), 2, outline=None, caps=False)
    p.line([(118, 96), (130, 102)], (118, 104, 106, 220), 2, outline=None, caps=False)


def prop_cocoon(p, w, h):
    p.line([(55, 2), (55, 24)], (188, 178, 180, 220), 2)
    p.ellipse([26, 22, 84, 160], (184, 174, 162, 255), INK, 4)
    for i in range(6):
        p.arc([31 + i * 3, 32 + i * 13, 79 - i * 3, 80 + i * 14], 190, 350,
              (204, 195, 184, 220), 2)
    p.ellipse([38, 142, 75, 180], (144, 130, 124, 255), INK, 3)
    p.poly([(50, 165), (57, 177), (64, 165)], (70, 54, 58, 220), INK, 1)


def prop_altar(p, w, h):
    p.poly([(15, 122), (42, 132), (132, 132), (158, 120), (150, 67), (34, 67)], (67, 59, 62, 255))
    p.poly([(20, 68), (42, 57), (130, 57), (154, 68), (154, 79), (20, 79)], (94, 83, 79, 255))
    p.rect([43, 84, 126, 117], (50, 45, 48, 255), INK, 3)
    p.poly([(39, 54), (58, 47), (77, 51), (83, 43), (103, 47), (121, 55), (130, 62),
            (55, 62)], (88, 74, 82, 255), INK, 2)
    add_glow(p.image, 84, 42, 20, (124, 224, 165, 72), p.s)
    p.ellipse([66, 38, 102, 52], (44, 92, 64, 220), (128, 220, 158, 230), 2)
    p.line([(24, 110), (28, 125)], (114, 98, 86, 220), 2, outline=None, caps=False)
    p.line([(146, 108), (141, 125)], (114, 98, 86, 220), 2, outline=None, caps=False)


def prop_banner(p, w, h):
    p.line([(50, 4), (50, 190)], (83, 58, 35, 255), 6, outline=INK)
    p.line([(14, 18), (86, 18)], GOLD_D, 5, outline=INK)
    p.poly([(22, 23), (78, 23), (75, 132), (64, 118), (52, 138), (41, 119), (29, 134)],
           (66, 26, 35, 255), INK, 4)
    p.poly([(35, 43), (61, 43), (58, 92), (49, 82), (42, 96), (31, 92)], (122, 39, 47, 255),
           (40, 17, 22, 220), 2)
    p.circle(48, 59, 8, GOLD_D, INK, 2)
    p.line([(48, 52), (48, 67)], (40, 24, 24, 200), 2)
    p.line([(39, 59), (57, 59)], (40, 24, 24, 200), 2)


def prop_throne(p, w, h):
    p.poly([(30, 225), (52, 62), (75, 26), (95, 13), (117, 25), (141, 62), (161, 225)], (43, 32, 43, 255))
    p.poly([(51, 210), (68, 68), (88, 39), (96, 61), (104, 38), (128, 69), (143, 210)],
           (82, 48, 46, 255), INK, 3)
    p.rect([54, 168, 141, 223], (50, 35, 40, 255), INK, 3)
    p.poly([(48, 35), (55, 4), (66, 20), (80, 0), (93, 20), (110, 3), (121, 22), (134, 8),
            (138, 43)], GOLD, INK, 3)
    p.circle(95, 27, 12, (68, 24, 32, 255), GOLD_D, 2)
    p.ellipse([65, 73, 89, 96], BONE, INK, 3)
    p.circle(72, 82, 2, INK, INK, 1)
    p.circle(83, 82, 2, INK, INK, 1)
    p.line([(85, 60), (123, 60)], GOLD_D, 2)
    p.line([(86, 66), (121, 66)], GOLD_D, 2)


def prop_bone_arch(p, w, h):
    p.line([(18, 200), (33, 46), (70, 15), (115, 9), (164, 17), (201, 48), (216, 200)],
           BONE_D, 14, outline=INK)
    p.line([(25, 200), (39, 53), (74, 24), (115, 18), (158, 27), (194, 58), (208, 200)],
           BONE, 8, outline=INK)
    for i in range(6):
        x = 48 + i * 24
        p.line([(x, 26), (x + 12, 9)], BONE_D, 6, outline=INK)
    for i in range(9):
        p.circle(25 + i * 21, 197, 6, BONE_D, INK, 2)
    p.circle(115, 46, 15, BONE, INK, 3)
    p.circle(109, 43, 3, INK, INK, 1)
    p.circle(121, 43, 3, INK, INK, 1)
    p.line([(110, 53), (120, 53)], INK, 2, outline=None, caps=False)


def prop_eggs(p, w, h):
    rnd = random.Random(33)
    for i in range(12):
        x, y = rnd.uniform(18, 112), rnd.uniform(36, 88)
        r = rnd.uniform(8, 17)
        p.ellipse([x - r, y - r * 1.25, x + r, y + r * 1.1], (179, 165, 160, 235),
                  (87, 70, 82, 240), 2)
        p.ellipse([x - r * 0.3, y - r * 0.8, x + r * 0.15, y - r * 0.2],
                  (224, 207, 198, 180), None, 0)
    p.ellipse([30, 70, 105, 102], (81, 60, 80, 190), None, 0)


def prop_roots(p, w, h):
    rnd = random.Random(44)
    for i in range(9):
        x = 15 + i * 17
        pts = [(x, 102), (x + rnd.uniform(-12, 12), 68), (x + rnd.uniform(-24, 24), 38),
               (x + rnd.uniform(-30, 30), 12)]
        p.line(pts, (59, 45, 33, 255), 7, outline=INK)
        p.line([(pts[1][0], pts[1][1]), (pts[1][0] + 16, pts[1][1] - 9)],
               (70, 52, 37, 255), 4, outline=INK)
        p.line([(pts[2][0], pts[2][1]), (pts[2][0] - 14, pts[2][1] - 12)],
               (70, 52, 37, 255), 4, outline=INK)
    p.line([(20, 104), (150, 104)], (38, 28, 22, 255), 6, outline=INK)


def prop_acid(p, w, h):
    add_glow(p.image, 85, 54, 54, (115, 214, 95, 80), p.s)
    p.ellipse([14, 40, 156, 82], (54, 108, 49, 220), (36, 64, 35, 240), 3)
    p.ellipse([27, 43, 141, 73], (101, 187, 76, 225), (67, 126, 56, 230), 2)
    for i in range(6):
        x = 36 + i * 19
        p.circle(x, 52 + (i % 3) * 6, 4 + (i % 2) * 3, (159, 221, 111, 220), None, 0)
    for i in range(5):
        x = 28 + i * 28
        p.ellipse([x, 25, x + 7, 42], (125, 212, 105, 150), None, 0)


def prop_web_throne(p, w, h):
    prop_throne(p, w, h)
    for i in range(9):
        x, y = 22 + i * 19, 18 + (i % 4) * 26
        p.line([(x, y), (x + 62, y + 90)], (198, 188, 194, 90), 1.2, outline=None, caps=False)
        p.line([(x, y), (x - 20, y + 80)], (198, 188, 194, 80), 1.2, outline=None, caps=False)
    p.ellipse([92, 185, 124, 216], (178, 163, 164, 220), (74, 58, 72, 230), 2)


def prop_gate(p, w, h):
    p.poly([(17, 248), (17, 52), (44, 18), (94, 7), (146, 17), (173, 51), (173, 248)],
           (32, 29, 38, 255), INK, 5)
    p.poly([(31, 238), (31, 59), (52, 31), (94, 20), (137, 30), (158, 58), (158, 238)],
           (17, 16, 22, 255), INK, 4)
    for i in range(7):
        x = 35 + i * 20
        p.line([(x, 230), (x, 30)], STEEL_D, 3, outline=INK)
        p.poly([(x - 3, 30), (x, 20), (x + 3, 30)], STEEL_D, INK, 1)
    for y in (60, 102, 150, 198):
        p.line([(31, y), (158, y)], STEEL_D, 4, outline=INK)
    p.circle(94, 108, 12, (55, 42, 48, 255), GOLD_D, 3)
    p.circle(94, 108, 4, INK, INK, 1)


def prop_blood(p, w, h):
    p.ellipse([12, 38, 140, 76], (91, 22, 30, 225), (46, 13, 18, 240), 3)
    p.ellipse([28, 43, 118, 67], (124, 30, 36, 210), None, 0)
    for i in range(7):
        x = 28 + i * 17
        p.circle(x, 36 + (i % 3) * 12, 3 + (i % 3), (101, 24, 32, 220), None, 0)
    p.line([(48, 55), (64, 60)], (150, 53, 53, 170), 2, outline=None, caps=False)


def prop_mirror(p, w, h):
    p.poly([(55, 8), (92, 22), (94, 154), (55, 184), (16, 154), (18, 22)], GOLD_D, INK, 4)
    p.poly([(55, 18), (83, 29), (85, 148), (55, 172), (25, 148), (27, 29)], (35, 42, 53, 255), INK, 2)
    add_glow(p.image, 55, 92, 42, (104, 207, 206, 65), p.s)
    p.line([(55, 26), (55, 158)], (116, 196, 199, 100), 2, outline=None, caps=False)
    p.line([(33, 74), (55, 54), (78, 86)], (174, 218, 216, 120), 2, outline=None, caps=False)
    p.line([(41, 122), (61, 105), (73, 130)], (174, 218, 216, 105), 2, outline=None, caps=False)
    p.line([(55, 182), (55, 189)], GOLD_D, 4, outline=INK)


def prop_cauldron(p, w, h):
    add_glow(p.image, 75, 52, 50, (111, 208, 113, 70), p.s)
    p.ellipse([19, 48, 131, 112], (42, 42, 44, 255), INK, 5)
    p.ellipse([25, 45, 125, 75], (62, 61, 60, 255), INK, 3)
    p.ellipse([33, 47, 117, 67], (82, 164, 78, 240), (52, 109, 53, 240), 2)
    p.line([(24, 78), (17, 118)], STEEL_D, 5, outline=INK)
    p.line([(126, 78), (133, 118)], STEEL_D, 5, outline=INK)
    p.line([(34, 112), (50, 121)], STEEL_D, 4, outline=INK)
    p.line([(116, 112), (100, 121)], STEEL_D, 4, outline=INK)
    for i in range(5):
        x = 48 + i * 13
        p.ellipse([x, 27 + (i % 2) * 5, x + 5, 41], (146, 222, 126, 120), None, 0)


def props():
    _prop("prop_crate", 96, 90, prop_crate)
    _prop("prop_moss", 128, 64, prop_moss)
    _prop("prop_sarcophagus", 170, 110, prop_sarcophagus)
    _prop("prop_rack", 140, 170, prop_rack)
    _prop("prop_shield", 90, 120, prop_shield)
    _prop("prop_chain", 80, 190, prop_chain)
    _prop("prop_cage", 120, 170, prop_cage)
    _prop("prop_skullpile", 120, 88, prop_skullpile)
    _prop("prop_ribs", 190, 170, prop_ribs)
    _prop("prop_candles", 100, 110, prop_candles)
    _prop("prop_web", 190, 190, prop_web)
    _prop("prop_cocoon", 110, 190, prop_cocoon)
    _prop("prop_altar", 170, 140, prop_altar)
    _prop("prop_banner", 100, 200, prop_banner)
    _prop("prop_throne", 190, 235, prop_throne)
    _prop("prop_bone_arch", 235, 215, prop_bone_arch)
    _prop("prop_eggs", 130, 115, prop_eggs)
    _prop("prop_roots", 170, 115, prop_roots)
    _prop("prop_acid", 170, 95, prop_acid)
    _prop("prop_web_throne", 210, 240, prop_web_throne)
    _prop("prop_gate", 190, 250, prop_gate)
    _prop("prop_blood", 150, 85, prop_blood)
    _prop("prop_mirror", 115, 195, prop_mirror)
    _prop("prop_cauldron", 150, 135, prop_cauldron)
