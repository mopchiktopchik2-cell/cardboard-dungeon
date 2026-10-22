# -*- coding: utf-8 -*-
"""Additional atmosphere art for the v1.1 menu and dungeon mood."""
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets" / "Resources" / "Art"
OUT.mkdir(parents=True, exist_ok=True)

INK = (20, 13, 20, 255)
GOLD = (220, 172, 98, 255)
GOLD_D = (140, 102, 58, 255)
PALE = (242, 227, 198, 255)


def lerp(a, b, k):
    return tuple(int(a[i] + (b[i] - a[i]) * k) for i in range(len(a)))


def grain(im, amount=3.0):
    arr = np.array(im).astype(np.int16)
    noise = np.random.normal(0, amount, (arr.shape[0], arr.shape[1], 1))
    arr[..., :3] = np.clip(arr[..., :3] + noise, 0, 255)
    return Image.fromarray(arr.astype(np.uint8))


def menu_bg():
    w, h = 1600, 900
    y, x = np.mgrid[0:h, 0:w]
    top = np.array([7, 6, 12], dtype=np.float32)
    bottom = np.array([23, 15, 22], dtype=np.float32)
    k = (y / h) ** 0.72
    arr = top[None, None, :] * (1 - k[..., None]) + bottom[None, None, :] * k[..., None]

    # Cold light spill from the moonlit arch.
    cx, cy = 1110.0, 260.0
    d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2) / 390.0
    glow = np.clip(1.0 - d, 0, 1) ** 2.4
    arr[..., 0] += glow * 42
    arr[..., 1] += glow * 50
    arr[..., 2] += glow * 76

    # Subtle purple haze in the lower half.
    fog = np.clip((y / h - 0.42) * 1.7, 0, 1) * (0.5 + 0.5 * np.sin(x / 190.0))
    arr[..., 0] += fog * 15
    arr[..., 1] += fog * 5
    arr[..., 2] += fog * 28

    im = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).convert("RGBA")
    d = ImageDraw.Draw(im, "RGBA")
    rng = random.Random(41)

    # Stone wall blocks behind the arch.
    for row in range(11):
        yy = 40 + row * 58
        off = 0 if row % 2 == 0 else 60
        for col in range(-1, 15):
            xx = off + col * 120
            shade = rng.randint(-7, 8)
            fill = (20 + shade // 2, 17 + shade // 2, 26 + shade // 2, 150)
            d.rectangle([xx, yy, xx + 114, yy + 50], outline=(8, 6, 12, 110), width=2, fill=fill)
            if rng.random() < 0.28:
                d.line([(xx + 12, yy + 12), (xx + 90, yy + 16)], fill=(45, 36, 50, 42), width=1)

    # Dark vignette corners.
    edge = np.zeros((h, w, 4), dtype=np.uint8)
    vx = np.sqrt(((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2)
    va = np.clip((vx - 0.36) * 155, 0, 205).astype(np.uint8)
    edge[..., 0] = 3
    edge[..., 1] = 2
    edge[..., 2] = 6
    edge[..., 3] = va
    im.alpha_composite(Image.fromarray(edge))

    # Arch: rough stone frame with a deep, cold interior.
    arch = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ad = ImageDraw.Draw(arch, "RGBA")
    ax, ay, aw, ah = 795, 146, 500, 710
    ad.rounded_rectangle([ax, ay, ax + aw, ay + ah], radius=250, fill=(10, 8, 16, 220),
                         outline=(49, 39, 52, 255), width=26)
    ad.rounded_rectangle([ax + 30, ay + 26, ax + aw - 30, ay + ah - 30], radius=225,
                         outline=(92, 72, 61, 155), width=3)
    ad.arc([ax + 48, ay + 44, ax + aw - 48, ay + 326], 180, 360,
           fill=(147, 115, 70, 65), width=2)
    # Stones on the arch frame.
    for i in range(15):
        a = math.pi + i * math.pi / 14
        px = ax + aw / 2 + math.cos(a) * (aw / 2 - 16)
        py = ay + 250 + math.sin(a) * 245
        ad.arc([px - 11, py - 9, px + 11, py + 9], 0, 360, fill=(15, 11, 18, 145), width=2)

    # Cracked moon / occult seal inside the arch.
    mx, my, mr = 1045, 300, 118
    ad.ellipse([mx - mr, my - mr, mx + mr, my + mr], fill=(203, 185, 146, 38),
               outline=(183, 151, 98, 62), width=3)
    ad.ellipse([mx - mr + 15, my - mr + 20, mx + mr - 23, my + mr - 12], fill=(10, 9, 17, 232))
    ad.arc([mx - mr + 12, my - mr + 16, mx + mr - 16, my + mr - 14], 205, 342,
           fill=(200, 188, 159, 44), width=5)
    for i in range(9):
        a = i * math.tau / 9
        ad.line([(mx + math.cos(a) * 72, my + math.sin(a) * 72),
                 (mx + math.cos(a) * 96, my + math.sin(a) * 96)],
                fill=(190, 154, 96, 44), width=2)

    im.alpha_composite(arch)

    # Floor planks / shadows.
    floor = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    fd = ImageDraw.Draw(floor, "RGBA")
    for i in range(0, w, 96):
        fd.line([(i + 20, 760), (i + 380, 900)], fill=(12, 9, 15, 110), width=2)
    fd.rectangle([0, 758, w, h], fill=(15, 11, 18, 96))
    for i in range(0, w, 170):
        fd.line([(i, 762), (i - 90, 900)], fill=(62, 42, 35, 45), width=2)
    im.alpha_composite(floor)

    # Hanging chains.
    for sx in (282, 1310):
        for j in range(9):
            yy = 0 + j * 56
            fd.ellipse([sx - 4 - j * 0.2, yy, sx + 4 + j * 0.2, yy + 30],
                       outline=(70, 62, 67, 100), width=2)
    # Candles at the bottom.
    for sx, sh in ((205, 118), (1370, 96)):
        fd.rectangle([sx - 8, h - 82 - sh, sx + 8, h - 82], fill=(211, 190, 151, 188),
                     outline=(86, 60, 47, 190), width=2)
        gx = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        gd = ImageDraw.Draw(gx)
        gd.ellipse([sx - 46, h - 104 - sh, sx + 46, h - 32 - sh], fill=(255, 167, 68, 52))
        gx = gx.filter(ImageFilter.GaussianBlur(22))
        im.alpha_composite(gx)
        fd.ellipse([sx - 3, h - 99 - sh, sx + 3, h - 82 - sh], fill=(255, 214, 132, 240))

    # Scattered paper scraps.
    for i in range(16):
        px = rng.randint(60, w - 60)
        py = rng.randint(640, h - 40)
        ww, hh = rng.randint(8, 26), rng.randint(4, 12)
        fd.polygon([(px, py), (px + ww, py + rng.randint(-4, 4)),
                    (px + ww - 3, py + hh), (px - 2, py + hh)],
                   fill=(174, 151, 115, rng.randint(14, 34)))

    im.alpha_composite(floor)

    # Fog bands.
    fog = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    fgd = ImageDraw.Draw(fog)
    for i in range(14):
        fx = rng.randint(-100, w + 100)
        fy = rng.randint(540, 820)
        fw = rng.randint(180, 520)
        fh = rng.randint(25, 80)
        fgd.ellipse([fx, fy, fx + fw, fy + fh], fill=(92, 103, 133, rng.randint(7, 16)))
    fog = fog.filter(ImageFilter.GaussianBlur(24))
    im.alpha_composite(fog)

    # Final cold, dusty grade.
    im.alpha_composite(Image.new("RGBA", (w, h), (4, 3, 9, 54)))

    im = grain(im, 3.2)
    im.save(OUT / "ui_menu_bg.png")


def menu_frame():
    w, h = 1600, 900
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, "RGBA")
    gold = (189, 149, 87, 118)
    gold_soft = (132, 101, 62, 68)
    d.rectangle([14, 14, w - 14, h - 14], outline=(16, 11, 17, 160), width=3)
    d.rectangle([23, 23, w - 23, h - 23], outline=gold_soft, width=1)
    d.rectangle([30, 30, w - 30, h - 30], outline=gold, width=2)
    d.rectangle([38, 38, w - 38, h - 38], outline=(204, 164, 100, 34), width=1)

    def flourish(cx, cy, sx, sy):
        d.line([(cx, cy), (cx + sx * 44, cy)], fill=gold, width=2)
        d.line([(cx, cy), (cx, cy + sy * 44)], fill=gold, width=2)
        d.arc([cx + sx * 44 - 18, cy + sy * 44 - 18, cx + sx * 44 + 18, cy + sy * 44 + 18],
              0, 360, fill=gold, width=2)
        d.polygon([(cx, cy - 7), (cx + 7, cy), (cx, cy + 7), (cx - 7, cy)], fill=(201, 158, 94, 150))
        d.ellipse([cx + sx * 84 - 3, cy + sy * 84 - 3, cx + sx * 84 + 3, cy + sy * 84 + 3],
                  fill=(205, 164, 102, 110))

    flourish(34, 34, 1, 1)
    flourish(w - 34, 34, -1, 1)
    flourish(34, h - 34, 1, -1)
    flourish(w - 34, h - 34, -1, -1)

    # Top center crest.
    d.line([(w / 2 - 210, 27), (w / 2 - 46, 27)], fill=gold_soft, width=1)
    d.line([(w / 2 + 46, 27), (w / 2 + 210, 27)], fill=gold_soft, width=1)
    d.polygon([(w / 2, 19), (w / 2 + 13, 32), (w / 2, 45), (w / 2 - 13, 32)],
              outline=gold, fill=(80, 56, 38, 90))
    d.ellipse([w / 2 - 3, 29, w / 2 + 3, 35], fill=(216, 176, 108, 145))
    return im


def rune():
    size = 512
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, "RGBA")
    cx = cy = size / 2
    gold = (214, 171, 102, 150)
    gold_soft = (172, 132, 76, 60)
    for r, wdt in ((205, 2), (178, 1), (140, 2), (92, 1)):
        d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=gold_soft if r != 178 else gold, width=wdt)
    for i in range(12):
        a = i * math.tau / 12
        ca, sa = math.cos(a), math.sin(a)
        d.line([(cx + ca * 178, cy + sa * 178), (cx + ca * 196, cy + sa * 196)], fill=gold, width=2)
        # Angular rune.
        rx, ry = cx + ca * 155, cy + sa * 155
        ux, uy = -sa, ca
        pts = [(rx - ux * 5, ry - uy * 5), (rx + ux * 5, ry + uy * 5),
               (rx - ux * 2 + ca * 7, ry - uy * 2 + sa * 7),
               (rx + ux * 2 - ca * 4, ry + uy * 2 - sa * 4)]
        d.line(pts, fill=(198, 156, 96, 110), width=2)
    # Central eye / diamond.
    for i, r in enumerate((64, 47, 29)):
        d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)],
                  outline=(gold if i == 0 else gold_soft), width=2)
    d.ellipse([cx - 15, cy - 15, cx + 15, cy + 15], outline=gold, width=2)
    d.ellipse([cx - 5, cy - 5, cx + 5, cy + 5], fill=(216, 174, 108, 80))
    glow = im.filter(ImageFilter.GaussianBlur(9))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.alpha_composite(glow)
    out.alpha_composite(im)
    out.save(OUT / "ui_rune.png")


def divider():
    w, h = 700, 48
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im, "RGBA")
    gold = (202, 161, 99, 135)
    d.line([(16, 24), (300, 24)], fill=gold, width=1)
    d.line([(400, 24), (w - 16, 24)], fill=gold, width=1)
    d.line([(300, 21), (334, 21)], fill=(130, 98, 60, 65), width=1)
    d.line([(366, 27), (400, 27)], fill=(130, 98, 60, 65), width=1)
    d.polygon([(350, 10), (365, 24), (350, 38), (335, 24)], outline=gold,
              fill=(76, 52, 36, 80))
    d.polygon([(350, 17), (358, 24), (350, 31), (342, 24)], fill=(211, 169, 104, 55))
    for x in (20, w - 20):
        d.ellipse([x - 3, 21, x + 3, 27], fill=(206, 165, 101, 80))
    return im


def soft_dot(name, size, color, radius, blur):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = size / 2
    d.ellipse([c - radius, c - radius, c + radius, c + radius], fill=color)
    im = im.filter(ImageFilter.GaussianBlur(blur))
    im.save(OUT / (name + ".png"))


def main():
    menu_bg()
    menu_frame().save(OUT / "ui_menu_frame.png")
    rune()
    divider().save(OUT / "ui_divider.png")
    soft_dot("fx_ember", 32, (255, 166, 68, 235), 6, 5)
    soft_dot("fx_mote", 24, (205, 215, 232, 180), 3, 4)
    print("v1.1 atmosphere art written to", OUT)


if __name__ == "__main__":
    main()
