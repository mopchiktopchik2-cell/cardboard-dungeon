# -*- coding: utf-8 -*-
"""Equipment icons, dungeon emblems, projectiles and soul effects."""
import math
import random

from PIL import Image, ImageDraw

from art_v12_common import (SS, INK, INK_SOFT, BONE, BONE_D, PALE, GOLD, GOLD_L, GOLD_D,
                            RED, RED_D, BLOOD, STEEL, STEEL_L, STEEL_D, PURPLE, PURPLE_D,
                            CYAN, GREEN, GREEN_D, BROWN, BROWN_D, WOOD, BLACK, FLESH,
                            canvas, Pen, add_glow, finish, shade, bone_piece)


def _icon(name, fn, glow=None, size=128):
    im = canvas(size, size)
    if glow:
        add_glow(im, size * 0.5, size * 0.5, size * 0.38, glow)
    p = Pen(im)
    fn(p, size)
    finish(im, name, size, size, grain=2.0)


def _blade(p, x1, y1, x2, y2, width, fill, outline=INK, guard=True, gem=GOLD):
    p.line([(x1, y1), (x2, y2)], fill, width, outline=outline)
    # tapered point
    ang = math.atan2(y2 - y1, x2 - x1)
    nx, ny = -math.sin(ang), math.cos(ang)
    p.poly([(x2 + nx * width * 0.7, y2 + ny * width * 0.7),
            (x2 + math.cos(ang) * 10, y2 + math.sin(ang) * 10),
            (x2 - nx * width * 0.7, y2 - ny * width * 0.7)], fill, outline, 2)
    if guard:
        gx, gy = x1 + math.cos(ang) * 13, y1 + math.sin(ang) * 13
        p.line([(gx - nx * 16, gy - ny * 16), (gx + nx * 16, gy + ny * 16)], gem, 7, outline=outline)
        p.line([(x1, y1), (x1 - math.cos(ang) * 15, y1 - math.sin(ang) * 15)], BROWN_D, 7, outline=outline)
        p.circle(x1 - math.cos(ang) * 18, y1 - math.sin(ang) * 18, 5, gem, outline, 2)


def eq_rusty_blade(p, s):
    _blade(p, 35, 98, 91, 26, 10, (112, 93, 75, 255), INK, True, (136, 105, 57, 255))
    p.line([(57, 74), (66, 65)], (156, 128, 87, 210), 2, outline=None, caps=False)
    p.circle(56, 80, 5, (127, 88, 52, 255), INK, 2)
    for i in range(3):
        p.line([(44 + i * 4, 90 - i * 3), (51 + i * 4, 83 - i * 3)], (59, 42, 34, 220), 1.5, outline=None, caps=False)


def eq_grave_cleaver(p, s):
    p.poly([(42, 84), (72, 31), (97, 20), (106, 68), (76, 99)], (93, 82, 70, 255), INK, 4)
    p.line([(49, 80), (71, 38)], STEEL_L, 3, outline=None, caps=False)
    p.poly([(68, 38), (92, 25), (101, 29), (78, 50)], (139, 34, 38, 170), None, 0)
    p.line([(65, 82), (41, 103)], BROWN_D, 9, outline=INK)
    p.line([(48, 94), (61, 79)], GOLD_D, 5, outline=INK)
    p.circle(40, 105, 5, GOLD, INK, 2)


def eq_widow_fang(p, s):
    p.arc([26, 30, 104, 104], 196, 338, (67, 43, 72, 255), 12)
    p.arc([32, 36, 98, 98], 197, 337, PURPLE, 5)
    p.line([(32, 84), (19, 103)], (65, 45, 56, 255), 8, outline=INK)
    p.line([(38, 80), (28, 95)], GOLD_D, 4, outline=INK)
    p.poly([(95, 48), (112, 31), (108, 54), (101, 61)], (207, 194, 168, 255), INK, 2)
    p.circle(87, 62, 4, RED, INK, 1)
    p.line([(55, 58), (70, 72)], (178, 133, 203, 180), 2, outline=None, caps=False)


def eq_kingsbane(p, s):
    add_glow(p.image, 66, 61, 32, (237, 186, 83, 55), p.s)
    p.line([(29, 103), (92, 21)], (38, 30, 32, 255), 13, outline=INK)
    p.line([(31, 101), (90, 23)], (218, 204, 170, 255), 6, outline=INK)
    p.line([(46, 83), (83, 30)], (243, 235, 207, 210), 2, outline=None, caps=False)
    p.line([(35, 92), (54, 106)], GOLD, 7, outline=INK)
    p.line([(26, 108), (39, 94)], GOLD_D, 8, outline=INK)
    p.poly([(95, 18), (104, 9), (108, 24), (102, 29)], GOLD, INK, 2)
    p.circle(38, 106, 5, RED, INK, 2)
    p.line([(25, 79), (38, 66)], GOLD_D, 3, outline=INK)


def eq_patched_cloak(p, s):
    p.poly([(45, 24), (70, 15), (92, 28), (107, 73), (100, 108), (85, 94), (73, 112),
            (60, 95), (45, 111), (33, 77)], (86, 62, 43, 255), INK, 4)
    p.poly([(53, 36), (66, 29), (88, 36), (94, 70), (84, 91), (70, 82), (59, 96), (47, 74)],
           (111, 80, 51, 255), INK_SOFT, 2)
    p.poly([(59, 48), (77, 43), (81, 60), (62, 65)], (61, 86, 59, 255), INK, 2)
    p.poly([(79, 73), (96, 69), (99, 86), (82, 90)], (97, 73, 100, 255), INK, 2)
    for i in range(4):
        p.line([(65 + i * 4, 54), (67 + i * 4, 62)], (178, 163, 130, 210), 1.2, outline=None, caps=False)
    p.line([(38, 89), (50, 101)], (53, 38, 28, 220), 2, outline=None, caps=False)


def eq_web_carapace(p, s):
    p.poly([(42, 29), (65, 20), (91, 30), (101, 69), (82, 102), (48, 102), (29, 68)],
           (61, 37, 69, 255), INK, 4)
    p.poly([(48, 36), (65, 28), (86, 37), (90, 65), (74, 89), (52, 87), (39, 63)],
           (92, 56, 105, 255), INK_SOFT, 2)
    for i in range(3):
        p.arc([46 + i * 7, 40 + i * 12, 91 - i * 7, 81 + i * 9], 196, 344, (160, 126, 174, 200), 2)
    p.line([(65, 30), (65, 91)], (48, 28, 56, 220), 3)
    for i in range(5):
        p.line([(45 + i * 10, 99), (37 + i * 13, 115)], (183, 174, 160, 180), 2, outline=None, caps=False)


def eq_royal_shroud(p, s):
    p.poly([(45, 22), (69, 13), (93, 26), (108, 77), (101, 111), (85, 94), (73, 112),
            (60, 94), (44, 111), (28, 75)], (43, 31, 58, 255), INK, 4)
    p.poly([(54, 35), (68, 27), (87, 36), (93, 71), (83, 90), (70, 82), (58, 96), (46, 73)],
           (76, 51, 88, 255), INK_SOFT, 2)
    p.line([(52, 35), (46, 92)], GOLD_D, 4, outline=INK)
    p.line([(87, 36), (95, 87)], GOLD_D, 4, outline=INK)
    p.poly([(65, 22), (70, 38), (80, 27), (77, 47), (86, 40), (79, 58), (59, 50), (62, 34)],
           (213, 181, 124, 255), INK, 2)
    p.circle(70, 57, 4, RED, INK, 1)
    p.line([(40, 80), (53, 93)], (186, 147, 82, 190), 2, outline=None, caps=False)


def eq_bone_charm(p, s):
    add_glow(p.image, 65, 68, 29, (185, 228, 170, 45), p.s)
    p.arc([31, 20, 99, 100], 198, 342, (144, 123, 90, 255), 4)
    p.line([(42, 43), (37, 69)], (132, 111, 81, 255), 3, outline=INK)
    p.line([(87, 44), (92, 69)], (132, 111, 81, 255), 3, outline=INK)
    p.ellipse([48, 54, 82, 87], BONE, INK, 3)
    p.circle(57, 67, 4, INK, INK, 1)
    p.circle(72, 67, 4, INK, INK, 1)
    p.poly([(64, 72), (58, 81), (70, 81)], INK, INK, 1)
    p.line([(55, 86), (74, 86)], BONE_D, 3, outline=INK)
    p.line([(51, 91), (45, 105)], BONE_D, 4, outline=INK)
    p.line([(76, 91), (82, 105)], BONE_D, 4, outline=INK)


def eq_ember_eye(p, s):
    add_glow(p.image, 64, 64, 40, (241, 125, 61, 85), p.s)
    p.poly([(18, 64), (42, 37), (86, 37), (110, 64), (86, 91), (42, 91)], (70, 49, 36, 255), INK, 4)
    p.poly([(28, 64), (46, 45), (82, 45), (100, 64), (82, 83), (46, 83)], (125, 59, 46, 255), INK, 2)
    p.ellipse([47, 49, 81, 79], (46, 20, 18, 255), GOLD_D, 3)
    p.circle(64, 64, 10, (227, 85, 45, 255), (255, 203, 122, 255), 2)
    p.circle(64, 64, 4, (255, 224, 166, 255), None, 0)
    p.line([(35, 37), (25, 23)], GOLD_D, 4, outline=INK)
    p.line([(93, 37), (103, 23)], GOLD_D, 4, outline=INK)
    p.circle(24, 21, 4, GOLD, INK, 1)
    p.circle(104, 21, 4, GOLD, INK, 1)


def eq_spider_heart(p, s):
    add_glow(p.image, 64, 66, 32, (192, 48, 56, 62), p.s)
    p.line([(64, 34), (64, 22)], (170, 156, 134, 255), 3, outline=INK)
    p.poly([(64, 28), (74, 42), (91, 50), (80, 65), (92, 82), (72, 88), (64, 104),
            (55, 88), (35, 82), (46, 65), (35, 49), (52, 41)], (151, 36, 43, 255), INK, 4)
    p.poly([(64, 38), (80, 48), (75, 61), (88, 72), (68, 78), (64, 92), (59, 78),
            (40, 71), (52, 59), (47, 48)], (191, 58, 58, 255), INK_SOFT, 2)
    p.line([(64, 44), (64, 69)], (91, 23, 30, 220), 2, outline=None, caps=False)
    for i in range(4):
        p.line([(49 + i * 2, 46 + i * 4), (25 - i * 2, 31 + i * 10)], (50, 35, 43, 255), 3, outline=INK)
        p.line([(79 - i * 2, 46 + i * 4), (103 + i * 2, 31 + i * 10)], (50, 35, 43, 255), 3, outline=INK)


def eq_lich_seal(p, s):
    add_glow(p.image, 64, 64, 37, (108, 230, 180, 62), p.s)
    p.circle(64, 64, 44, (38, 54, 52, 255), INK, 4)
    p.circle(64, 64, 37, (48, 76, 68, 255), (145, 216, 176, 180), 2)
    p.ellipse([43, 42, 85, 88], (203, 191, 165, 255), INK, 3)
    p.circle(53, 61, 4, INK, INK, 1)
    p.circle(75, 61, 4, INK, INK, 1)
    p.poly([(64, 67), (58, 77), (70, 77)], INK, INK, 1)
    p.line([(53, 82), (75, 82)], BONE_D, 3, outline=INK)
    for i in range(4):
        a = math.tau * i / 4 + math.pi / 4
        p.line([(64 + math.cos(a) * 28, 64 + math.sin(a) * 28),
                (64 + math.cos(a) * 40, 64 + math.sin(a) * 40)], (130, 219, 177, 200), 2, outline=INK)
    p.circle(64, 64, 34, None, (110, 198, 158, 110), 1)


def eq_crown_of_ashes(p, s):
    add_glow(p.image, 64, 62, 36, (232, 148, 59, 60), p.s)
    p.poly([(30, 88), (34, 40), (49, 57), (63, 28), (78, 57), (94, 40), (98, 88)],
           (74, 55, 40, 255), INK, 4)
    p.poly([(38, 82), (41, 51), (53, 64), (63, 42), (75, 64), (88, 51), (91, 82)],
           GOLD_D, INK, 2)
    for x, y in ((34, 40), (63, 28), (94, 40)):
        p.circle(x, y, 4, (240, 192, 98, 255), INK, 1)
        add_glow(p.image, x, y, 10, (244, 134, 48, 50), p.s)
    p.circle(63, 65, 7, (45, 25, 39, 255), GOLD, 2)
    p.line([(41, 89), (89, 89)], (47, 32, 28, 255), 6, outline=INK)
    for i in range(5):
        x = 35 + i * 14
        p.circle(x, 98, 3, (223, 95, 41, 180), None, 0)


def eq_icons():
    _icon("eq_rusty_blade", eq_rusty_blade)
    _icon("eq_grave_cleaver", eq_grave_cleaver)
    _icon("eq_widow_fang", eq_widow_fang)
    _icon("eq_kingsbane", eq_kingsbane)
    _icon("eq_patched_cloak", eq_patched_cloak)
    _icon("eq_web_carapace", eq_web_carapace)
    _icon("eq_royal_shroud", eq_royal_shroud)
    _icon("eq_bone_charm", eq_bone_charm)
    _icon("eq_ember_eye", eq_ember_eye)
    _icon("eq_spider_heart", eq_spider_heart)
    _icon("eq_lich_seal", eq_lich_seal)
    _icon("eq_crown_of_ashes", eq_crown_of_ashes)


# ------------------------------------------------------------------ emblems

def _emblem(name, fn, rim):
    im = canvas(128, 128)
    add_glow(im, 64, 64, 44, (rim[0], rim[1], rim[2], 55))
    p = Pen(im)
    p.circle(64, 64, 52, (20, 17, 24, 255), INK, 4)
    p.circle(64, 64, 46, (43, 34, 45, 255), (rim[0], rim[1], rim[2], 230), 3)
    for i in range(12):
        a = math.tau * i / 12
        p.circle(64 + math.cos(a) * 46, 64 + math.sin(a) * 46, 3, rim, INK, 1)
    p.circle(64, 64, 38, None, (rim[0], rim[1], rim[2], 80), 1)
    fn(p)
    finish(im, name, 128, 128, grain=2.0)


def emblem_crypt(p):
    p.line([(32, 101), (39, 49), (55, 33), (73, 33), (89, 49), (96, 101)], BONE_D, 9, outline=INK)
    p.ellipse([43, 39, 85, 82], BONE, INK, 3)
    p.circle(54, 58, 4, INK, INK, 1)
    p.circle(74, 58, 4, INK, INK, 1)
    p.poly([(64, 62), (58, 71), (70, 71)], INK, INK, 1)
    p.line([(54, 77), (74, 77)], BONE_D, 3, outline=INK)
    p.line([(53, 87), (49, 101)], BONE_D, 4, outline=INK)
    p.line([(75, 87), (79, 101)], BONE_D, 4, outline=INK)
    for x, y in ((29, 91), (99, 91)):
        p.circle(x, y, 6, (54, 43, 42, 255), GOLD_D, 2)


def emblem_web(p):
    cx, cy = 64, 60
    for i in range(9):
        a = math.tau * i / 9
        p.line([(cx, cy), (cx + math.cos(a) * 39, cy + math.sin(a) * 39)],
               (194, 184, 190, 150), 1.2, outline=None, caps=False)
    for r in (13, 24, 35):
        p.arc([cx - r, cy - r, cx + r, cy + r], 0, 360, (194, 184, 190, 140), 1.2)
    p.ellipse([47, 50, 81, 77], (35, 24, 36, 255), INK, 3)
    p.circle(64, 58, 8, (91, 42, 71, 255), INK, 2)
    for i in range(4):
        p.line([(54 + i * 2, 64), (36 - i * 2, 76 + i * 6)], (35, 24, 36, 255), 3, outline=INK)
        p.line([(74 - i * 2, 64), (92 + i * 2, 76 + i * 6)], (35, 24, 36, 255), 3, outline=INK)
    for x, y in ((59, 53), (69, 53), (54, 60), (74, 60)):
        p.circle(x, y, 2, RED, None, 0)


def emblem_throne(p):
    p.poly([(38, 100), (44, 47), (56, 37), (64, 48), (72, 36), (85, 47), (91, 100)],
           (41, 31, 43, 255), INK, 4)
    p.poly([(39, 46), (43, 27), (53, 36), (64, 20), (75, 36), (86, 27), (90, 46)],
           GOLD, INK, 3)
    for x, y in ((43, 27), (64, 20), (86, 27)):
        p.circle(x, y, 3, GOLD_L, INK, 1)
    p.circle(64, 44, 7, (49, 25, 43, 255), GOLD_D, 2)
    p.rect([45, 72, 84, 101], (60, 41, 43, 255), INK, 3)
    p.line([(50, 77), (79, 77)], GOLD_D, 2)
    p.line([(50, 86), (79, 86)], GOLD_D, 2)


def emblems():
    _emblem("ui_dungeon_crypt", emblem_crypt, (185, 173, 154))
    _emblem("ui_dungeon_web", emblem_web, (146, 105, 164))
    _emblem("ui_dungeon_throne", emblem_throne, (190, 104, 92))


# ------------------------------------------------------------------ projectiles

def proj_soul(p, s):
    add_glow(p.image, s * 0.5, s * 0.5, s * 0.38, (103, 229, 227, 105))
    p.circle(s * 0.5, s * 0.54, s * 0.24, (31, 71, 86, 255), (151, 235, 227, 230), 2)
    p.ellipse([s * 0.32, s * 0.38, s * 0.49, s * 0.56], (12, 21, 29, 255), None, 0)
    p.ellipse([s * 0.51, s * 0.38, s * 0.68, s * 0.56], (12, 21, 29, 255), None, 0)
    p.circle(s * 0.405, s * 0.47, s * 0.035, (168, 255, 239, 255), None, 0)
    p.circle(s * 0.595, s * 0.47, s * 0.035, (168, 255, 239, 255), None, 0)
    p.poly([(s * 0.43, s * 0.62), (s * 0.5, s * 0.72), (s * 0.57, s * 0.62)],
           (128, 224, 215, 220), (151, 235, 227, 220), 1)
    for i in range(3):
        p.line([(s * (0.28 - i * 0.06), s * (0.7 + i * 0.08)),
                (s * (0.1 - i * 0.05), s * (0.76 + i * 0.08))],
               (120, 222, 216, 100), 2, outline=None, caps=False)


def proj_bone(p, s):
    p.poly([(8, 36), (30, 23), (52, 31), (69, 42), (53, 50), (28, 49)], BONE, INK, 2)
    p.line([(22, 38), (49, 38)], BONE_D, 2, outline=None, caps=False)
    p.circle(11, 35, 4, BONE, INK, 1)
    p.circle(67, 42, 3, BONE, INK, 1)
    p.line([(31, 29), (26, 20)], BONE_D, 2, outline=INK)
    p.line([(42, 46), (47, 55)], BONE_D, 2, outline=INK)


def fx_soul(p, s):
    add_glow(p.image, s * 0.5, s * 0.5, s * 0.4, (103, 224, 224, 120))
    p.circle(s * 0.5, s * 0.52, s * 0.16, (122, 235, 226, 210), (184, 250, 241, 200), 1)
    p.circle(s * 0.5, s * 0.5, s * 0.07, (229, 255, 250, 230), None, 0)
    p.arc([s * 0.2, s * 0.24, s * 0.8, s * 0.76], 20, 150, (120, 226, 218, 100), 2)
    p.arc([s * 0.25, s * 0.3, s * 0.75, s * 0.7], 200, 330, (120, 226, 218, 85), 2)


def projectiles():
    for name, fn, sz in (("proj_soul", proj_soul, 48), ("proj_bone", proj_bone, 72),
                         ("fx_soul", fx_soul, 48)):
        im = canvas(sz, sz)
        p = Pen(im)
        fn(p, sz)
        finish(im, name, sz, sz, grain=1.6)
