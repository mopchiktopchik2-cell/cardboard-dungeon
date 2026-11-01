# -*- coding: utf-8 -*-
"""Unique paper-cutout sprites for the v1.5 rift bosses.

Run with:  py tools/gen_v13_bosses.py
"""
import math
import random

from PIL import Image

from art_v12_common import (SS, INK, INK_SOFT, BONE, BONE_D, PALE, GOLD, GOLD_L, GOLD_D,
                            RED, RED_D, BLOOD, STEEL, STEEL_L, STEEL_D, PURPLE, PURPLE_D,
                            CYAN, GREEN, GREEN_D, BROWN, BROWN_D, WOOD, BLACK, FLESH,
                            canvas, Pen, add_glow, finish, shade, mix, bone_piece,
                            add_speckles)


def _actor(name, w, h, fn, glows=()):
    im = canvas(w, h)
    for g in glows:
        add_glow(im, g[0], g[1], g[2], g[3])
    p = Pen(im)
    fn(p, w, h)

    # PaperActor pivots actors at 8% from the bottom; align the feet with it.
    solid = im.getchannel("A").point(lambda v: 255 if v > 128 else 0)
    bbox = solid.getbbox()
    if bbox:
        desired = int(round(im.height * 0.08))
        bottom_pad = im.height - bbox[3]
        shift = max(0, min(desired - bottom_pad, bbox[1]))
        if shift > 0:
            shifted = Image.new("RGBA", im.size, (0, 0, 0, 0))
            shifted.paste(im, (0, -shift))
            im = shifted

    finish(im, name, w, h, grain=2.0)


def _chain(p, x, y, count, dx=2, dy=12, color=STEEL_D):
    for i in range(count):
        p.ellipse([x + i * dx, y + i * dy, x + 15 + i * dx, y + 10 + i * dy], None, color, 2)


def _chain_line(p, x1, y1, x2, y2, count, color=STEEL_D):
    for i in range(count):
        t = i / max(1, count - 1)
        cx = x1 + (x2 - x1) * t
        cy = y1 + (y2 - y1) * t
        p.ellipse([cx - 6, cy - 4, cx + 6, cy + 4], None, color, 2)


def _bone_hand(p, x, y, angle=0.0, size=1.0):
    p.circle(x, y, 7 * size, (198, 185, 157, 255), INK, 2)
    for i in range(3):
        a = angle + (i - 1) * 0.42
        p.line([(x, y), (x + math.cos(a) * 14 * size, y + math.sin(a) * 14 * size)],
               (210, 198, 170, 255), 3.2 * size, outline=INK)


def _cutlass(p, x, y, tip_x, tip_y):
    # grip and pommel
    p.line([(x - 11, y + 15), (x + 3, y + 2)], (76, 51, 34, 255), 8, outline=INK)
    p.circle(x - 13, y + 18, 5, GOLD, INK, 2)
    # basket guard
    p.arc([x - 20, y - 12, x + 18, y + 24], 300, 120, GOLD, 4)
    p.line([(x - 7, y - 1), (x + 9, y + 9)], GOLD_D, 3, outline=None, caps=False)
    # wide curved blade
    p.poly([(x + 4, y - 2), (x + 21, y - 13), (tip_x - 8, tip_y + 34), (tip_x, tip_y),
            (tip_x - 10, tip_y + 10), (tip_x - 23, tip_y + 34), (x - 2, y + 8)],
           STEEL, INK, 3)
    p.line([(x + 9, y - 3), (tip_x - 11, tip_y + 30)], STEEL_L, 2.5, outline=None, caps=False)
    p.line([(x + 2, y + 4), (tip_x - 17, tip_y + 30)], STEEL_D, 1.6, outline=None, caps=False)


def bone_captain(p, w, h):
    add_glow(p.image, 190, 205, 128, (74, 158, 196, 34), p.s)

    # tattered navy coat
    p.poly([(124, 128), (256, 128), (292, 196), (312, 304), (300, 407), (282, 380),
            (266, 420), (244, 386), (226, 424), (204, 388), (184, 424), (162, 386),
            (142, 420), (124, 382), (108, 409), (94, 304), (80, 196)],
           (36, 47, 74, 255), INK, 5)
    p.poly([(134, 140), (186, 150), (180, 296), (164, 360), (120, 338), (108, 236)],
           (48, 62, 94, 255), INK_SOFT, 2)
    p.poly([(246, 140), (194, 150), (200, 296), (216, 360), (260, 338), (272, 236)],
           (48, 62, 94, 255), INK_SOFT, 2)
    for x in (120, 260):
        p.line([(x, 170), (x - 6 if x < 190 else x + 6, 348)], GOLD_D, 3, outline=None, caps=False)
    for y in (196, 232, 268):
        p.circle(190, y, 3.4, GOLD, INK, 1)

    # legs
    p.line([(164, 296), (154, 388)], BONE_D, 13, outline=INK)
    p.line([(216, 296), (226, 388)], BONE_D, 13, outline=INK)
    p.poly([(132, 386), (166, 386), (172, 421), (124, 421)], (31, 27, 34, 255), INK, 3)
    p.poly([(214, 386), (248, 386), (256, 421), (208, 421)], (31, 27, 34, 255), INK, 3)

    # pelvis and ribcage
    p.poly([(160, 276), (220, 276), (228, 304), (190, 316), (152, 302)], BONE, INK, 3)
    p.line([(190, 142), (190, 286)], BONE, 8, outline=INK)
    for i in range(6):
        y = 154 + i * 19
        p.arc([154, y - 16, 226, y + 25], 192, 348, BONE, 6)
    p.line([(190, 156), (190, 268)], BONE_D, 2, outline=None, caps=False)

    # spectral heart
    add_glow(p.image, 190, 198, 34, (92, 224, 235, 95), p.s)
    p.poly([(190, 184), (202, 194), (200, 212), (190, 224), (180, 212), (178, 194)],
           (86, 205, 214, 255), (186, 247, 245, 255), 2)
    p.circle(186, 196, 2.5, (221, 255, 252, 240), None, 0)

    # epaulettes
    for cx in (118, 262):
        p.ellipse([cx - 30, 128, cx + 30, 160], GOLD_D, INK, 3)
        p.ellipse([cx - 24, 132, cx + 24, 154], GOLD, INK, 2)
        for i in range(5):
            p.line([(cx - 20 + i * 10, 152), (cx - 22 + i * 10, 170)], GOLD_D, 2, outline=None, caps=False)

    # arms
    p.line([(124, 156), (96, 224), (86, 262)], BONE_D, 11, outline=INK)
    _bone_hand(p, 84, 268, 1.9, 0.9)
    p.line([(256, 156), (288, 220), (298, 250)], BONE_D, 11, outline=INK)
    _bone_hand(p, 300, 256, 0.3, 0.9)

    # lantern
    p.line([(84, 276), (78, 294)], GOLD_D, 3, outline=INK)
    p.arc([62, 282, 96, 306], 180, 360, GOLD_D, 3)
    p.rect([58, 300, 96, 338], (49, 42, 36, 255), INK, 3)
    p.rect([64, 306, 90, 332], (24, 46, 50, 255), INK_SOFT, 2)
    add_glow(p.image, 77, 319, 24, (104, 232, 223, 150), p.s)
    p.circle(77, 320, 8, (117, 232, 222, 255), (204, 255, 248, 255), 2)
    p.line([(77, 306), (77, 334)], INK, 2, outline=None, caps=False)
    p.line([(64, 319), (90, 319)], INK, 2, outline=None, caps=False)
    p.rect([54, 294, 100, 302], GOLD_D, INK, 2)
    p.rect([54, 336, 100, 344], GOLD_D, INK, 2)

    # cutlass
    _cutlass(p, 302, 250, 352, 112)

    # skull
    p.ellipse([158, 70, 224, 136], BONE, INK, 4)
    p.poly([(166, 108), (176, 134), (190, 144), (207, 138), (219, 112), (209, 92),
            (190, 86), (170, 92)], (222, 207, 178, 255), INK, 2)
    p.ellipse([168, 96, 188, 114], (15, 18, 22, 255), INK, 2)
    add_glow(p.image, 178, 105, 10, (112, 238, 226, 100), p.s)
    p.circle(178, 105, 3.2, (143, 246, 234, 255), None, 0)
    # eyepatch
    p.line([(158, 92), (226, 101)], (29, 24, 26, 255), 4, outline=INK)
    p.poly([(198, 94), (220, 98), (219, 116), (200, 112)], (24, 21, 24, 255), INK, 2)
    p.poly([(190, 116), (181, 128), (196, 128)], (47, 37, 34, 255), INK, 1)
    p.line([(181, 135), (211, 136)], (54, 40, 35, 255), 4, outline=INK)
    for i in range(5):
        p.line([(183 + i * 6, 131), (183 + i * 6, 140)], (79, 61, 51, 255), 1.5, outline=None, caps=False)

    # tricorn hat
    p.poly([(98, 82), (122, 66), (156, 72), (190, 58), (224, 72), (258, 66), (282, 82),
            (258, 94), (190, 86), (122, 94)], (39, 44, 66, 255), INK, 4)
    p.poly([(136, 72), (146, 36), (172, 56), (190, 26), (208, 56), (234, 36), (244, 72)],
           (46, 54, 80, 255), INK, 4)
    p.line([(104, 84), (190, 80), (276, 84)], GOLD_D, 3, outline=None, caps=False)
    p.line([(146, 66), (190, 61), (234, 66)], GOLD_D, 2, outline=None, caps=False)
    # hat emblem: tiny skull
    p.circle(190, 62, 8, BONE, INK, 2)
    p.circle(187, 61, 1.6, INK, INK, 1)
    p.circle(193, 61, 1.6, INK, INK, 1)
    p.line([(187, 66), (193, 66)], INK, 1.4, outline=None, caps=False)

    # tattered spectral plume
    add_glow(p.image, 244, 34, 26, (94, 219, 226, 75), p.s)
    p.poly([(222, 62), (232, 38), (241, 16), (248, 30), (245, 48), (236, 66)],
           (101, 203, 209, 255), INK, 3)
    p.poly([(236, 64), (249, 42), (260, 24), (262, 40), (254, 56), (246, 68)],
           (76, 166, 179, 255), INK, 3)
    p.line([(231, 58), (244, 22)], (185, 246, 240, 220), 2, outline=None, caps=False)
    for x, y in ((238, 44), (247, 40), (254, 46)):
        p.circle(x, y, 1.4, (198, 250, 244, 200), None, 0)

    # coat collar
    p.poly([(150, 124), (170, 136), (190, 128), (210, 136), (230, 124), (238, 142),
            (190, 156), (142, 142)], (30, 40, 64, 255), INK, 3)

    # floating embers
    for x, y, r in ((70, 90, 2), (310, 180, 2), (56, 370, 2), (330, 330, 3), (120, 60, 2)):
        p.circle(x, y, r, (113, 226, 220, 170), None, 0)


def brood_mother(p, w, h):
    add_glow(p.image, 170, 230, 150, (150, 60, 110, 45), p.s)

    # web backdrop
    for i in range(11):
        ang = -0.35 + i * 0.30
        p.line([(185, 190), (185 + math.cos(ang) * 230, 190 + math.sin(ang) * 210)],
               (188, 170, 188, 34), 1.1, outline=None, caps=False)
    for r in (80, 140, 200):
        p.arc([185 - r, 185 - r, 185 + r, 185 + r], 0, 360, (188, 170, 188, 28), 1)

    # back legs
    back_legs = [
        [(225, 215), (150, 105), (58, 146)],
        [(218, 235), (120, 170), (28, 258)],
        [(215, 258), (110, 250), (34, 340)],
        [(220, 278), (140, 330), (92, 370)],
    ]
    for leg in back_legs:
        p.line(leg, (43, 27, 46, 255), 13, outline=INK)
        p.line([leg[1], leg[2]], (66, 40, 66, 255), 6, outline=INK)
        p.poly([(leg[2][0], leg[2][1]), (leg[2][0] + 7, leg[2][1] + 13),
                (leg[2][0] - 6, leg[2][1] + 10)], (27, 19, 27, 255), INK, 1)

    # abdomen
    p.ellipse([48, 135, 258, 310], (45, 25, 46, 255), INK, 5)
    p.ellipse([62, 150, 242, 296], (78, 42, 73, 255), INK_SOFT, 2)
    for i in range(4):
        p.arc([70, 150 + i * 26, 232, 205 + i * 26], 198, 342, (51, 30, 52, 170), 3)

    # egg clusters on the abdomen
    for cx, cy, r in ((118, 196, 16), (154, 218, 20), (108, 246, 14), (176, 178, 13)):
        add_glow(p.image, cx, cy, r * 2.2, (224, 76, 138, 55), p.s)
        p.ellipse([cx - r, cy - r * 0.8, cx + r, cy + r * 0.8], (108, 42, 76, 255),
                  (206, 91, 139, 230), 2)
        for k in range(4):
            ex = cx - r * 0.5 + (k % 2) * r * 0.9
            ey = cy - r * 0.3 + (k // 2) * r * 0.7
            p.circle(ex, ey, r * 0.24, (238, 166, 191, 240), (126, 61, 87, 220), 1)

    # hanging egg sac
    add_glow(p.image, 152, 318, 44, (240, 137, 170, 65), p.s)
    p.ellipse([108, 282, 198, 354], (207, 171, 177, 245), INK, 4)
    p.ellipse([118, 292, 188, 344], (229, 197, 199, 235), (138, 98, 108, 220), 2)
    for i in range(6):
        ex = 128 + (i % 3) * 21
        ey = 302 + (i // 3) * 22
        p.ellipse([ex, ey, ex + 15, ey + 14], (243, 218, 214, 245), (145, 102, 111, 220), 1)
        p.circle(ex + 7, ey + 7, 2.2, (96, 56, 65, 220), None, 0)
    p.line([(122, 276), (118, 288)], (211, 190, 195, 180), 2, outline=None, caps=False)
    p.line([(180, 276), (186, 288)], (211, 190, 195, 180), 2, outline=None, caps=False)

    # thorax
    p.ellipse([228, 168, 316, 256], (55, 32, 58, 255), INK, 4)
    p.ellipse([240, 180, 304, 244], (88, 49, 84, 255), INK_SOFT, 2)
    p.poly([(250, 196), (286, 192), (296, 220), (272, 240), (248, 226)], (116, 32, 52, 255), INK, 2)

    # front legs
    front_legs = [
        [(250, 200), (322, 128), (394, 150)],
        [(256, 224), (348, 202), (414, 236)],
        [(258, 244), (340, 280), (402, 318)],
        [(246, 258), (294, 322), (316, 370)],
    ]
    for leg in front_legs:
        p.line(leg, (52, 31, 55, 255), 14, outline=INK)
        p.line([leg[1], leg[2]], (81, 47, 81, 255), 7, outline=INK)
        p.circle(leg[1][0], leg[1][1], 9, (29, 19, 30, 255), INK, 2)
        p.poly([(leg[2][0], leg[2][1]), (leg[2][0] + 9, leg[2][1] + 14),
                (leg[2][0] - 5, leg[2][1] + 11)], (30, 21, 28, 255), INK, 1)

    # head
    p.ellipse([296, 166, 378, 238], (42, 25, 44, 255), INK, 4)
    p.ellipse([307, 177, 368, 228], (70, 38, 68, 255), INK_SOFT, 2)
    # many eyes
    eyes = ((322, 188, 6), (343, 184, 7), (361, 192, 5), (330, 203, 5),
            (350, 203, 6), (365, 207, 4), (316, 204, 3), (355, 216, 3))
    for x, y, r in eyes:
        add_glow(p.image, x, y, r * 2.1, (233, 73, 118, 70), p.s)
        p.circle(x, y, r, (226, 74, 114, 255), (54, 16, 28, 255), 1)
        p.circle(x - 1, y - 1, max(1.2, r * 0.26), (255, 198, 196, 235), None, 0)
    # mandibles
    p.poly([(318, 224), (326, 252), (334, 226)], (203, 192, 168, 255), INK, 2)
    p.poly([(352, 224), (344, 252), (338, 225)], (203, 192, 168, 255), INK, 2)
    add_glow(p.image, 336, 242, 14, (137, 220, 124, 50), p.s)

    # spiderlings riding on the mother
    def spiderling(cx, cy, scale=1.0):
        for side in (-1, 1):
            for i in range(4):
                a = (0.55 + i * 0.55) * side
                jx = cx + math.cos(a) * 9 * scale
                jy = cy + math.sin(a) * 8 * scale
                p.line([(cx, cy), (jx, jy)], (31, 20, 31, 255), 2.4 * scale, outline=None, caps=False)
                p.line([(jx, jy), (jx + math.cos(a) * 6 * scale, jy + math.sin(a) * 5 * scale + 3 * scale)],
                       (43, 27, 43, 255), 1.8 * scale, outline=None, caps=False)
        p.ellipse([cx - 6 * scale, cy - 5 * scale, cx + 7 * scale, cy + 6 * scale],
                  (63, 37, 60, 255), INK, 2)
        p.circle(cx + 7 * scale, cy - 1 * scale, 4 * scale, (43, 26, 43, 255), INK, 1)
        p.circle(cx + 8.5 * scale, cy - 2 * scale, 1.5 * scale, (231, 91, 124, 255), None, 0)
        p.circle(cx + 5.5 * scale, cy - 2.5 * scale, 1.2 * scale, (231, 91, 124, 255), None, 0)

    spiderling(104, 126, 1.45)
    spiderling(294, 138, 1.15)
    spiderling(204, 118, 1.05)

    # dangling web threads
    for x, y in ((92, 310), (214, 300), (370, 230), (320, 350)):
        p.line([(x, y), (x - 6, y + 46)], (203, 190, 201, 45), 1, outline=None, caps=False)

    # floating embers
    for x, y, r in ((78, 120, 2), (392, 186, 2), (42, 320, 3), (300, 348, 2), (238, 120, 2)):
        p.circle(x, y, r, (231, 121, 157, 180), None, 0)


def plague_baron(p, w, h):
    add_glow(p.image, 160, 210, 118, (92, 178, 92, 34), p.s)

    # tattered cape
    p.poly([(108, 118), (212, 118), (248, 180), (272, 300), (262, 376), (244, 350),
            (230, 386), (210, 352), (192, 388), (172, 352), (152, 386), (132, 350),
            (112, 380), (96, 320), (74, 250), (88, 170)], (29, 39, 33, 255), INK, 5)
    for i in range(4):
        p.line([(104 + i * 30, 150), (88 + i * 44, 344)], (21, 28, 24, 210), 3,
               outline=None, caps=False)

    # legs and boots
    p.line([(142, 262), (136, 360)], (43, 51, 44, 255), 14, outline=INK)
    p.line([(178, 262), (184, 360)], (43, 51, 44, 255), 14, outline=INK)
    p.poly([(114, 356), (150, 356), (156, 390), (106, 390)], (27, 24, 23, 255), INK, 3)
    p.poly([(170, 356), (206, 356), (214, 390), (164, 390)], (27, 24, 23, 255), INK, 3)
    p.line([(126, 372), (150, 372)], (77, 67, 54, 180), 2, outline=None, caps=False)
    p.line([(176, 372), (202, 372)], (77, 67, 54, 180), 2, outline=None, caps=False)

    # tailcoat
    p.poly([(118, 128), (202, 128), (218, 214), (206, 276), (180, 262), (160, 282),
            (140, 262), (114, 276), (102, 214)], (39, 49, 40, 255), INK, 4)
    p.poly([(128, 140), (192, 140), (202, 216), (188, 258), (160, 246), (132, 258),
            (118, 216)], (51, 63, 50, 255), INK_SOFT, 2)
    # coat tails
    p.poly([(118, 232), (144, 238), (132, 330), (108, 310)], (35, 45, 37, 255), INK, 3)
    p.poly([(202, 232), (176, 238), (188, 330), (212, 310)], (35, 45, 37, 255), INK, 3)
    # gold trim and buttons
    p.line([(128, 144), (118, 250)], GOLD_D, 2.5, outline=None, caps=False)
    p.line([(192, 144), (202, 250)], GOLD_D, 2.5, outline=None, caps=False)
    for y in (160, 186, 212):
        p.circle(160, y, 3.6, GOLD, INK, 1)
    # belt
    p.rect([112, 238, 208, 254], (55, 39, 28, 255), INK, 3)
    p.rect([150, 240, 170, 252], GOLD_D, INK, 2)

    # cravat
    p.poly([(142, 126), (160, 136), (178, 126), (184, 146), (160, 158), (136, 146)],
           (221, 207, 176, 255), INK, 3)
    p.line([(160, 140), (160, 154)], (168, 150, 121, 255), 2, outline=None, caps=False)

    # left arm and censer
    p.line([(118, 146), (96, 208), (92, 238)], (43, 51, 44, 255), 12, outline=INK)
    p.circle(92, 244, 7, (61, 45, 35, 255), INK, 2)
    p.line([(92, 250), (84, 286)], GOLD_D, 2.4, outline=INK)
    p.arc([70, 278, 100, 306], 180, 360, GOLD_D, 3)
    p.ellipse([66, 298, 104, 332], (58, 46, 34, 255), INK, 3)
    add_glow(p.image, 85, 315, 30, (106, 221, 105, 120), p.s)
    p.circle(85, 315, 11, (55, 106, 58, 255), (151, 235, 143, 255), 2)
    p.circle(82, 312, 3.5, (198, 250, 177, 240), None, 0)
    p.line([(74, 310), (96, 310)], INK, 1.6, outline=None, caps=False)
    p.line([(74, 320), (96, 320)], INK, 1.6, outline=None, caps=False)

    # right arm and cane
    p.line([(202, 146), (224, 208), (228, 240)], (43, 51, 44, 255), 12, outline=INK)
    p.circle(229, 246, 7, (61, 45, 35, 255), INK, 2)
    p.circle(229, 240, 8, GOLD, INK, 2)
    p.line([(229, 252), (252, 366)], (57, 39, 27, 255), 6, outline=INK)
    p.line([(233, 258), (252, 360)], (118, 84, 50, 255), 2, outline=None, caps=False)
    p.line([(247, 338), (258, 370)], STEEL_D, 3, outline=INK)

    # green fumes
    for x, y, r in ((94, 286, 8), (104, 274, 6), (112, 262, 4), (74, 288, 5), (66, 276, 3)):
        p.circle(x, y, r, (112, 209, 112, 38), None, 0)

    # rat on the right shoulder
    p.ellipse([200, 112, 236, 136], (58, 52, 45, 255), INK, 2)
    p.circle(236, 122, 10, (65, 58, 50, 255), INK, 2)
    p.poly([(228, 114), (232, 100), (240, 113)], (65, 58, 50, 255), INK, 1)
    p.poly([(241, 113), (249, 102), (250, 116)], (65, 58, 50, 255), INK, 1)
    p.circle(240, 121, 2.2, (139, 240, 124, 255), None, 0)
    p.line([(202, 128), (188, 150), (194, 174)], (109, 84, 72, 255), 2, outline=INK)
    p.line([(246, 126), (254, 130)], (223, 198, 180, 255), 1.2, outline=None, caps=False)
    p.circle(234, 136, 2, GOLD, None, 0)

    # head / hood
    p.poly([(120, 92), (134, 70), (186, 70), (200, 94), (194, 122), (160, 132),
            (126, 120)], (34, 40, 34, 255), INK, 4)
    p.ellipse([128, 84, 192, 124], (207, 192, 163, 255), INK, 4)
    # beak mask
    p.poly([(176, 98), (246, 110), (252, 122), (188, 126), (170, 114)],
           (216, 201, 171, 255), INK, 3)
    p.line([(184, 108), (240, 116)], (166, 150, 123, 220), 2, outline=None, caps=False)
    p.circle(244, 117, 2, (46, 38, 30, 255), None, 0)
    # glass eye
    add_glow(p.image, 164, 100, 14, (102, 229, 106, 95), p.s)
    p.circle(164, 100, 9, (29, 48, 33, 255), (153, 227, 145, 255), 2)
    p.circle(162, 98, 3.2, (190, 250, 176, 240), None, 0)
    p.line([(134, 100), (154, 100)], (57, 44, 35, 255), 3, outline=None, caps=False)
    # mask straps
    p.line([(130, 108), (118, 118)], (62, 47, 37, 255), 3, outline=None, caps=False)
    p.line([(190, 122), (198, 132)], (62, 47, 37, 255), 3, outline=None, caps=False)

    # top hat
    p.poly([(104, 76), (120, 70), (196, 64), (218, 72), (214, 84), (112, 88)],
           (28, 32, 29, 255), INK, 4)
    p.poly([(124, 70), (134, 24), (204, 30), (210, 70)], (35, 40, 36, 255), INK, 4)
    p.poly([(134, 28), (202, 34), (204, 52), (132, 48)], (45, 51, 45, 255), INK_SOFT, 2)
    p.rect([128, 56, 208, 70], (117, 88, 44, 255), INK, 2)
    p.circle(168, 63, 4, (105, 218, 104, 255), (39, 74, 40, 255), 1)
    p.line([(136, 36), (150, 70)], (22, 27, 23, 180), 2, outline=None, caps=False)

    # floating spores
    for x, y, r in ((86, 110, 2), (236, 88, 2), (66, 350, 2), (266, 210, 3), (58, 160, 2)):
        p.circle(x, y, r, (133, 224, 123, 160), None, 0)


def abyss_knight(p, w, h):
    add_glow(p.image, 180, 220, 140, (96, 78, 176, 45), p.s)

    # tattered cape
    p.poly([(118, 118), (242, 118), (286, 190), (312, 310), (300, 398), (280, 366),
            (264, 408), (242, 372), (224, 410), (202, 372), (182, 410), (160, 370),
            (140, 404), (120, 366), (100, 396), (84, 310), (76, 190)],
           (44, 34, 66, 255), INK, 5)
    for i in range(5):
        p.line([(116 + i * 26, 146), (92 + i * 40, 370)], (33, 25, 50, 210), 3,
               outline=None, caps=False)

    # legs
    p.line([(154, 280), (148, 382)], (58, 66, 84, 255), 17, outline=INK)
    p.line([(206, 280), (212, 382)], (58, 66, 84, 255), 17, outline=INK)
    p.line([(150, 320), (146, 370)], (92, 103, 124, 255), 3, outline=None, caps=False)
    p.line([(210, 320), (214, 370)], (92, 103, 124, 255), 3, outline=None, caps=False)
    p.poly([(124, 378), (166, 378), (172, 414), (116, 414)], (40, 44, 56, 255), INK, 3)
    p.poly([(194, 378), (236, 378), (244, 414), (188, 414)], (40, 44, 56, 255), INK, 3)
    p.line([(126, 396), (168, 396)], (92, 103, 124, 180), 2, outline=None, caps=False)
    p.line([(196, 396), (238, 396)], (92, 103, 124, 180), 2, outline=None, caps=False)

    # torso plate
    p.poly([(134, 136), (226, 136), (240, 208), (230, 282), (180, 300), (130, 282),
            (120, 208)], (55, 62, 80, 255), INK, 4)
    p.poly([(144, 148), (216, 148), (226, 210), (216, 268), (180, 282), (144, 268),
            (134, 210)], (67, 75, 95, 255), INK_SOFT, 2)
    # void core
    add_glow(p.image, 180, 206, 34, (154, 92, 255, 105), p.s)
    p.poly([(180, 184), (196, 198), (192, 220), (180, 232), (168, 220), (164, 198)],
           (92, 54, 140, 255), (188, 141, 255, 255), 2)
    p.circle(178, 204, 3.4, (224, 192, 255, 240), None, 0)
    # belt
    p.rect([128, 266, 232, 282], (45, 38, 40, 255), INK, 3)
    p.poly([(168, 266), (192, 266), (188, 286), (172, 286)], (111, 83, 48, 255), INK, 2)

    # pauldrons
    p.poly([(100, 132), (140, 126), (154, 158), (142, 180), (112, 174), (92, 154)],
           (72, 80, 101, 255), INK, 4)
    p.poly([(260, 132), (220, 126), (206, 158), (218, 180), (248, 174), (268, 154)],
           (72, 80, 101, 255), INK, 4)
    for x, y, dx in ((106, 136, -18), (254, 136, 18)):
        p.poly([(x, y), (x + dx, y - 30), (x + dx * 0.45, y - 4)], (96, 104, 126, 255), INK, 3)

    # left arm / shield
    p.line([(122, 160), (104, 220), (104, 248)], (58, 66, 84, 255), 13, outline=INK)
    p.circle(105, 252, 8, (78, 86, 106, 255), INK, 2)
    # tower shield
    p.poly([(38, 154), (122, 146), (136, 226), (104, 330), (62, 306), (30, 226)],
           (48, 55, 72, 255), INK, 5)
    p.poly([(48, 166), (112, 158), (124, 224), (98, 310), (66, 292), (42, 224)],
           (66, 74, 94, 255), INK_SOFT, 2)
    p.line([(48, 166), (112, 158)], (139, 150, 173, 255), 3, outline=None, caps=False)
    p.line([(66, 292), (98, 310)], (139, 150, 173, 255), 3, outline=None, caps=False)
    # shield void eye
    add_glow(p.image, 84, 222, 26, (166, 92, 255, 110), p.s)
    p.ellipse([62, 204, 108, 240], (30, 24, 40, 255), (172, 121, 255, 220), 3)
    p.circle(84, 222, 9, (106, 61, 156, 255), (204, 166, 255, 255), 2)
    p.circle(82, 220, 3, (238, 218, 255, 245), None, 0)
    # shield rivets and chains
    for x, y in ((50, 180), (116, 172), (118, 238), (56, 272), (76, 300)):
        p.circle(x, y, 3, STEEL_L, INK, 1)
    _chain(p, 104, 280, 5, 2, 13, STEEL_D)
    # right arm / greatsword
    p.line([(238, 160), (266, 214), (276, 244)], (58, 66, 84, 255), 13, outline=INK)
    p.circle(278, 250, 9, (78, 86, 106, 255), INK, 2)
    p.poly([(268, 246), (286, 240), (292, 256), (276, 264)], (88, 78, 54, 255), INK, 3)
    # broad greatsword blade
    p.poly([(270, 244), (294, 236), (322, 88), (330, 56), (340, 88), (312, 244)],
           (112, 122, 146, 255), INK, 3)
    p.line([(284, 238), (334, 86)], (181, 194, 216, 255), 3, outline=None, caps=False)
    p.line([(276, 242), (318, 236)], (72, 81, 100, 220), 2, outline=None, caps=False)
    # crossguard
    p.poly([(256, 250), (302, 234), (308, 248), (262, 264)], (106, 92, 56, 255), INK, 3)
    p.circle(282, 254, 6, (151, 128, 74, 255), INK, 2)
    # runes on the blade
    for y in (120, 156, 192):
        p.circle(303 + (192 - y) * 0.22, y, 2, (185, 142, 255, 210), None, 0)

    # helmet
    p.poly([(144, 76), (160, 58), (200, 58), (216, 76), (220, 124), (202, 146),
            (158, 146), (140, 124)], (62, 70, 89, 255), INK, 4)
    p.poly([(150, 82), (162, 66), (198, 66), (210, 82), (214, 120), (198, 138),
            (162, 138), (146, 120)], (78, 87, 108, 255), INK_SOFT, 2)
    # visor slit
    add_glow(p.image, 180, 104, 28, (146, 88, 255, 120), p.s)
    p.poly([(154, 98), (206, 98), (202, 110), (158, 110)], (22, 18, 28, 255), INK, 2)
    p.line([(160, 104), (200, 104)], (191, 141, 255, 255), 3, outline=None, caps=False)
    # helmet ridges and rivets
    p.line([(180, 66), (180, 96)], (46, 51, 64, 220), 2.5, outline=None, caps=False)
    for x, y in ((154, 122), (206, 122), (180, 134)):
        p.circle(x, y, 2.6, STEEL_L, INK, 1)
    # horns
    p.poly([(146, 78), (130, 50), (120, 20), (140, 38), (156, 68)], (92, 84, 108, 255), INK, 3)
    p.poly([(214, 78), (230, 50), (240, 20), (220, 38), (204, 68)], (92, 84, 108, 255), INK, 3)
    p.line([(134, 42), (146, 70)], (160, 150, 184, 200), 2, outline=None, caps=False)
    p.line([(226, 42), (214, 70)], (160, 150, 184, 200), 2, outline=None, caps=False)

    # chains at the waist
    _chain(p, 128, 286, 5, 2, 12, STEEL_D)
    _chain(p, 208, 286, 5, 2, 12, STEEL_D)

    # floating void embers
    for x, y, r in ((70, 120, 2), (300, 150, 2), (48, 340, 3), (320, 340, 2), (260, 70, 2)):
        p.circle(x, y, r, (168, 118, 255, 170), None, 0)


def _rot_pt(cx, cy, x, y, angle):
    ca, sa = math.cos(angle), math.sin(angle)
    return (cx + x * ca - y * sa, cy + x * sa + y * ca)


def _book(p, cx, cy, w, h, angle, cover=(69, 47, 37, 255), page=(224, 207, 172, 255)):
    def rp(x, y):
        return _rot_pt(cx, cy, x, y, angle)

    p.poly([rp(-w / 2, -h / 2), rp(w / 2, -h / 2), rp(w / 2, h / 2), rp(-w / 2, h / 2)],
           cover, INK, 3)
    p.poly([rp(-w / 2 + 11, -h / 2 + 4), rp(w / 2 - 4, -h / 2 + 4),
            rp(w / 2 - 4, h / 2 - 4), rp(-w / 2 + 11, h / 2 - 4)],
           page, (120, 94, 70, 220), 1)
    p.line([rp(-w / 2 + 4, -h / 2 + 2), rp(-w / 2 + 4, h / 2 - 2)], GOLD_D, 3,
           outline=None, caps=False)
    for y in (-h / 2 + 7, h / 2 - 7):
        p.line([rp(-w / 2 + 2, y), rp(-w / 2 + 10, y)], GOLD, 2,
               outline=None, caps=False)
    for i in range(3):
        y = -h / 2 + 8 + i * (h - 16) / 3
        p.line([rp(-w / 2 + 14, y), rp(w / 2 - 6, y)], (160, 135, 105, 180), 1,
               outline=None, caps=False)
    cx, cy = rp(w / 2 - 5, 0)
    p.circle(cx, cy, 2.5, GOLD, INK, 1)


def _open_book(p, cx, cy, w, h, angle, cover=(74, 48, 38, 255), page=(228, 212, 178, 255)):
    def rp(x, y):
        return _rot_pt(cx, cy, x, y, angle)

    outline = [(-w / 2, -h / 2), (0, -h / 2 + 6), (w / 2, -h / 2),
               (w / 2, h / 2), (0, h / 2 - 6), (-w / 2, h / 2)]
    p.poly([rp(x, y) for x, y in outline], cover, INK, 3)
    left = [(-w / 2 + 4, -h / 2 + 4), (-2, -h / 2 + 9), (-2, h / 2 - 9), (-w / 2 + 4, h / 2 - 4)]
    right = [(2, -h / 2 + 9), (w / 2 - 4, -h / 2 + 4), (w / 2 - 4, h / 2 - 4), (2, h / 2 - 9)]
    p.poly([rp(x, y) for x, y in left], page, (122, 96, 72, 230), 2)
    p.poly([rp(x, y) for x, y in right], page, (122, 96, 72, 230), 2)
    for i in range(4):
        y = -h / 2 + 12 + i * (h - 24) / 4
        p.line([rp(-w / 2 + 8, y), rp(-4, y + 2)], (156, 130, 100, 180), 1,
               outline=None, caps=False)
        p.line([rp(4, y + 2), rp(w / 2 - 8, y)], (156, 130, 100, 180), 1,
               outline=None, caps=False)
    p.line([rp(0, -h / 2 + 6), rp(0, h / 2 - 6)], (120, 90, 66, 255), 2,
           outline=None, caps=False)
    p.line([rp(-w / 2, h / 2), rp(0, h / 2 - 6), rp(w / 2, h / 2)], (120, 90, 66, 220), 2,
           outline=None, caps=False)


def _page(p, cx, cy, w, h, angle):
    pts = [_rot_pt(cx, cy, x, y, angle)
           for x, y in ((-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2))]
    p.poly(pts, (228, 212, 178, 255), (122, 96, 72, 230), 1)
    for i in range(2):
        y = -h / 2 + 4 + i * h * 0.35
        p.line([_rot_pt(cx, cy, -w / 2 + 3, y, angle),
                _rot_pt(cx, cy, w / 2 - 3, y, angle)], (150, 124, 96, 190), 1,
               outline=None, caps=False)


def _quill(p, x0, y0, x1, y1):
    dx, dy = x1 - x0, y1 - y0
    ln = math.hypot(dx, dy) or 1.0
    px, py = -dy / ln, dx / ln
    p.line([(x0, y0), (x1, y1)], (208, 194, 164, 255), 4, outline=INK)
    left = []
    right = []
    for i in range(9):
        t = 0.12 + i * 0.085
        w = 20.0 * math.sin(math.pi * min(1.0, t * 1.08))
        cx = x0 + dx * t
        cy = y0 + dy * t
        left.append((cx + px * w, cy + py * w))
        right.append((cx - px * w * 0.82, cy - py * w * 0.82))
    p.poly(left + right[::-1], (222, 209, 181, 255), (128, 108, 82, 230), 2)
    for i in range(7):
        t = 0.2 + i * 0.09
        cx = x0 + dx * t
        cy = y0 + dy * t
        w = 18.0 * math.sin(math.pi * min(1.0, t * 1.08))
        p.line([(cx, cy), (cx + px * w, cy + py * w)], (164, 146, 120, 200), 1.2,
               outline=None, caps=False)
        p.line([(cx, cy), (cx - px * w * 0.8, cy - py * w * 0.8)], (164, 146, 120, 200), 1.2,
               outline=None, caps=False)
    p.line([(x0, y0), (x1, y1)], (232, 222, 198, 220), 1.6, outline=None, caps=False)


def lich_archivist(p, w, h):
    add_glow(p.image, 200, 230, 150, (102, 74, 156, 42), p.s)
    add_glow(p.image, 200, 300, 70, (100, 214, 202, 38), p.s)

    # robe
    p.poly([(140, 132), (260, 132), (292, 206), (310, 318), (300, 428), (280, 400),
            (264, 438), (242, 404), (222, 440), (200, 406), (178, 440), (158, 404),
            (138, 438), (118, 400), (102, 428), (90, 318), (108, 206)],
           (43, 33, 58, 255), INK, 5)
    p.poly([(152, 146), (248, 146), (270, 220), (280, 318), (260, 388), (200, 410),
            (140, 388), (120, 318), (130, 220)], (57, 44, 76, 255), INK_SOFT, 2)
    for i in range(5):
        p.line([(146 + i * 25, 170), (120 + i * 38, 398)], (34, 27, 46, 210), 3,
               outline=None, caps=False)
    # robe trim and tiny archive runes
    p.line([(152, 150), (126, 388)], GOLD_D, 2.4, outline=None, caps=False)
    p.line([(248, 150), (274, 388)], GOLD_D, 2.4, outline=None, caps=False)
    for x, y in ((134, 250), (266, 262), (144, 372), (258, 364)):
        p.circle(x, y, 5, None, (109, 216, 200, 170), 2)
        p.line([(x - 3, y), (x + 3, y)], (109, 216, 200, 190), 1.4, outline=None, caps=False)
        p.line([(x, y - 3), (x, y + 3)], (109, 216, 200, 190), 1.4, outline=None, caps=False)

    # high collar
    p.poly([(134, 136), (156, 86), (178, 140), (160, 158)], (37, 29, 50, 255), INK, 3)
    p.poly([(266, 136), (244, 86), (222, 140), (240, 158)], (37, 29, 50, 255), INK, 3)

    # chest opening and ribs
    p.poly([(172, 158), (228, 158), (238, 238), (218, 268), (200, 276), (182, 268),
            (162, 238)], (31, 41, 48, 255), INK_SOFT, 2)
    for i in range(5):
        y = 166 + i * 16
        p.arc([174, y - 13, 226, y + 22], 196, 344, (190, 182, 159, 220), 3)
    p.line([(200, 160), (200, 258)], (193, 184, 160, 220), 4, outline=INK)
    add_glow(p.image, 200, 214, 28, (112, 235, 203, 85), p.s)
    p.circle(200, 214, 10, (73, 170, 144, 255), (173, 242, 216, 255), 2)

    # arms
    p.line([(150, 164), (124, 236), (134, 276)], (48, 38, 66, 255), 13, outline=INK)
    p.line([(134, 276), (144, 300)], (200, 191, 166, 255), 8, outline=INK)
    p.line([(250, 164), (278, 224), (284, 252)], (48, 38, 66, 255), 13, outline=INK)
    p.line([(284, 252), (282, 270)], (200, 191, 166, 255), 8, outline=INK)

    # open tome
    add_glow(p.image, 200, 300, 62, (109, 224, 210, 55), p.s)
    p.poly([(126, 286), (198, 274), (202, 346), (120, 334)], (69, 47, 37, 255), INK, 3)
    p.poly([(202, 274), (276, 286), (282, 334), (202, 346)], (69, 47, 37, 255), INK, 3)
    p.poly([(132, 288), (196, 280), (198, 336), (128, 328)], (226, 210, 176, 255),
           (122, 96, 72, 230), 2)
    p.poly([(204, 280), (270, 288), (274, 328), (204, 336)], (226, 210, 176, 255),
           (122, 96, 72, 230), 2)
    p.line([(200, 278), (200, 344)], (120, 90, 66, 255), 3, outline=INK)
    p.poly([(200, 342), (209, 346), (204, 372), (196, 364)], (126, 38, 44, 255), INK, 1)
    for i in range(5):
        y = 292 + i * 8
        p.line([(138, y), (192, y - 4)], (150, 124, 96, 170), 1, outline=None, caps=False)
        p.line([(208, y - 4), (264, y)], (150, 124, 96, 170), 1, outline=None, caps=False)
    p.line([(128, 290), (122, 330)], (120, 94, 70, 220), 2, outline=None, caps=False)
    p.line([(272, 290), (278, 326)], (120, 94, 70, 220), 2, outline=None, caps=False)
    # glowing sigil over the book
    p.arc([172, 292, 228, 330], 0, 360, (130, 236, 218, 170), 2)
    p.poly([(200, 296), (210, 310), (200, 324), (190, 310)], (105, 221, 208, 255),
           (203, 255, 244, 255), 1)
    # candle on the book
    p.rect([248, 264, 260, 284], (219, 202, 168, 255), INK, 2)
    add_glow(p.image, 254, 254, 18, (117, 238, 213, 110), p.s)
    p.poly([(254, 244), (260, 258), (254, 264), (248, 258)], (137, 242, 220, 255),
           (215, 255, 246, 255), 1)

    # quill in the right hand
    _quill(p, 282, 270, 336, 122)
    add_glow(p.image, 338, 118, 16, (118, 235, 211, 100), p.s)
    p.circle(338, 118, 4, (155, 247, 227, 255), None, 0)

    # floating books and pages
    _open_book(p, 92, 168, 88, 38, -0.34, (84, 50, 42, 255))
    _book(p, 100, 362, 66, 26, 0.20, (92, 52, 46, 255))
    _book(p, 326, 372, 58, 24, -0.16, (74, 52, 86, 255))
    _page(p, 322, 344, 30, 38, -0.25)
    _page(p, 86, 268, 26, 32, 0.42)
    _page(p, 300, 250, 24, 30, 0.12)

    # paper crown
    p.poly([(164, 76), (154, 36), (174, 54), (188, 20), (204, 54), (226, 34), (236, 76)],
           (224, 208, 174, 255), INK, 3)
    p.line([(168, 66), (232, 66)], (126, 99, 72, 230), 2, outline=None, caps=False)
    for x, y in ((160, 50), (186, 42), (212, 46), (230, 58)):
        p.line([(x, y), (x + 6, y + 8)], (78, 58, 44, 210), 1.4, outline=None, caps=False)

    # skull and spectacles
    p.ellipse([164, 70, 236, 142], (209, 196, 170, 255), INK, 4)
    p.poly([(168, 112), (178, 138), (198, 150), (221, 141), (232, 112), (218, 94),
            (196, 88), (176, 94)], (219, 205, 178, 255), INK, 2)
    p.ellipse([172, 100, 196, 120], (14, 18, 22, 255), INK, 2)
    p.ellipse([204, 100, 228, 120], (14, 18, 22, 255), INK, 2)
    add_glow(p.image, 184, 110, 11, (116, 243, 204, 95), p.s)
    add_glow(p.image, 216, 110, 11, (116, 243, 204, 95), p.s)
    p.circle(184, 110, 3.5, (150, 248, 213, 255), None, 0)
    p.circle(216, 110, 3.5, (150, 248, 213, 255), None, 0)
    # spectacles
    p.arc([166, 94, 202, 128], 0, 360, GOLD_D, 3)
    p.arc([198, 94, 234, 128], 0, 360, GOLD_D, 3)
    p.line([(202, 110), (198, 110)], GOLD_D, 2, outline=None, caps=False)
    p.line([(166, 106), (158, 102)], GOLD_D, 2, outline=None, caps=False)
    p.poly([(196, 120), (187, 132), (202, 132)], (55, 43, 43, 255), INK, 1)
    p.line([(186, 139), (214, 140)], (54, 38, 36, 255), 4, outline=INK)
    for i in range(5):
        p.line([(188 + i * 6, 135), (188 + i * 6, 144)], (79, 61, 51, 255), 1.5,
               outline=None, caps=False)

    # belt, chain and keys
    p.rect([128, 272, 272, 288], (48, 36, 32, 255), INK, 3)
    p.circle(200, 280, 5, GOLD, INK, 2)
    _chain(p, 136, 290, 4, 2, 12, STEEL_D)
    p.circle(146, 342, 5, GOLD_D, INK, 2)
    p.line([(146, 346), (146, 356)], GOLD_D, 3, outline=INK)
    p.poly([(146, 356), (151, 362), (146, 368), (141, 362)], GOLD_D, INK, 1)

    # floating embers
    for x, y, r in ((120, 92, 2), (292, 112, 2), (64, 310, 3), (344, 300, 2), (238, 60, 2)):
        p.circle(x, y, r, (122, 235, 207, 170), None, 0)


def _crack(p, pts, color, width=2.2):
    p.line(pts, (26, 22, 28, 255), width + 2, outline=None, caps=False)
    p.line(pts, color, width, outline=None, caps=False)


def abyss_colossus(p, w, h):
    stone = (78, 72, 82, 255)
    stone_d = (52, 48, 57, 255)
    stone_l = (108, 101, 112, 255)
    void = (150, 72, 178, 255)
    void_l = (226, 148, 255, 255)

    add_glow(p.image, 210, 240, 160, (120, 54, 150, 42), p.s)

    # legs
    p.poly([(142, 286), (196, 286), (190, 414), (126, 414)], stone_d, INK, 4)
    p.poly([(224, 286), (278, 286), (294, 414), (230, 414)], stone_d, INK, 4)
    p.poly([(150, 300), (186, 300), (182, 390), (142, 390)], stone, INK_SOFT, 2)
    p.poly([(234, 300), (270, 300), (278, 390), (238, 390)], stone, INK_SOFT, 2)
    p.poly([(112, 408), (196, 408), (204, 442), (104, 442)], (44, 40, 48, 255), INK, 3)
    p.poly([(224, 408), (308, 408), (316, 442), (216, 442)], (44, 40, 48, 255), INK, 3)
    _crack(p, [(160, 316), (172, 344), (164, 372), (176, 396)], void, 2)
    _crack(p, [(252, 318), (242, 348), (250, 380)], void, 2)

    # torso
    p.poly([(126, 156), (294, 156), (320, 232), (312, 330), (270, 356), (210, 366),
            (150, 356), (108, 330), (100, 232)], stone, INK, 5)
    p.poly([(140, 172), (280, 172), (300, 236), (292, 318), (258, 340), (210, 348),
            (162, 340), (128, 318), (120, 236)], stone_l, INK_SOFT, 2)
    # chest plates
    p.poly([(146, 180), (210, 190), (204, 260), (136, 244)], (92, 86, 97, 255), INK_SOFT, 2)
    p.poly([(274, 180), (210, 190), (216, 260), (284, 244)], (88, 82, 93, 255), INK_SOFT, 2)
    # void core
    add_glow(p.image, 210, 246, 52, (190, 92, 240, 120), p.s)
    p.poly([(210, 214), (238, 236), (232, 272), (210, 294), (188, 272), (182, 236)],
           (44, 26, 54, 255), (206, 138, 255, 255), 3)
    p.circle(210, 250, 13, (96, 42, 122, 255), (228, 168, 255, 255), 2)
    p.circle(206, 246, 4, (248, 222, 255, 245), None, 0)
    _crack(p, [(160, 210), (174, 232), (166, 258), (180, 278)], void, 2.4)
    _crack(p, [(260, 200), (246, 226), (254, 250), (240, 276)], void, 2.4)
    _crack(p, [(132, 300), (152, 314), (146, 336)], void, 2)

    # belt / chains
    p.rect([122, 326, 298, 344], (48, 42, 42, 255), INK, 3)
    p.circle(210, 335, 7, (92, 74, 54, 255), INK, 2)
    _chain(p, 126, 346, 5, 2, 13, STEEL_D)
    _chain(p, 278, 346, 5, 2, 13, STEEL_D)
    # chains across the chest
    for i in range(6):
        p.ellipse([168 + i * 10, 300 + (i % 2) * 5, 182 + i * 10, 312 + (i % 2) * 5],
                  None, STEEL_D, 2)

    # shoulders
    p.poly([(94, 164), (146, 148), (164, 188), (150, 218), (108, 212), (82, 190)],
           stone_d, INK, 4)
    p.poly([(326, 164), (274, 148), (256, 188), (270, 218), (312, 212), (338, 190)],
           stone_d, INK, 4)
    p.poly([(96, 176), (70, 116), (104, 136), (118, 168)], stone_l, INK, 3)
    p.poly([(324, 176), (350, 116), (316, 136), (302, 168)], stone_l, INK, 3)

    # left arm and fist
    p.poly([(102, 200), (74, 250), (62, 310), (86, 336), (114, 318), (122, 258)], stone_d, INK, 4)
    p.poly([(88, 306), (126, 300), (136, 348), (94, 356)], stone, INK, 4)
    p.poly([(90, 342), (138, 336), (146, 378), (98, 386)], stone_l, INK, 3)
    for x in (102, 118, 134):
        p.line([(x, 346), (x + 2, 380)], (58, 53, 62, 220), 2, outline=None, caps=False)
    _crack(p, [(80, 258), (94, 282), (86, 304)], void, 2)
    _chain(p, 70, 332, 4, 2, 12, STEEL_D)

    # right arm (behind the pillar)
    p.poly([(318, 200), (348, 246), (354, 300), (330, 318), (306, 300), (302, 254)],
           stone_d, INK, 4)
    # broken pillar club
    p.poly([(311, 338), (349, 350), (409, 152), (371, 140)], (96, 88, 94, 255), INK, 4)
    p.poly([(321, 340), (349, 348), (401, 156), (379, 148)], (116, 107, 114, 255),
           INK_SOFT, 2)
    p.poly([(362, 144), (372, 118), (384, 132), (396, 112), (414, 150), (402, 158)],
           (104, 96, 102, 255), INK, 3)
    for t in (0.24, 0.47, 0.70):
        ax = 330 + (390 - 330) * t
        ay = 344 + (146 - 344) * t
        p.line([(ax - 19, ay - 6), (ax + 19, ay + 6)], (72, 66, 72, 230), 2,
               outline=None, caps=False)
    _crack(p, [(380, 160), (388, 190), (382, 218), (394, 246)], void, 2)
    _chain_line(p, 341, 239, 379, 251, 4)
    # fist gripping the pillar
    p.poly([(326, 306), (368, 298), (380, 344), (338, 354)], stone, INK, 4)
    p.poly([(330, 344), (382, 336), (388, 372), (336, 380)], stone_l, INK, 3)
    for x in (340, 356, 372):
        p.line([(x, 350), (x + 2, 376)], (58, 53, 62, 220), 2, outline=None, caps=False)
    _chain(p, 348, 300, 5, 2, 12, STEEL_D)

    # small head between the shoulders
    p.poly([(182, 120), (164, 92), (188, 108)], stone_l, INK, 2)
    p.poly([(238, 120), (256, 92), (232, 108)], stone_l, INK, 2)
    p.poly([(178, 118), (190, 96), (230, 96), (242, 118), (238, 152), (218, 166),
            (202, 166), (182, 152)], (66, 60, 70, 255), INK, 4)
    p.poly([(186, 124), (194, 106), (226, 106), (234, 124), (230, 146), (210, 156),
            (190, 146)], (88, 81, 92, 255), INK_SOFT, 2)
    p.poly([(186, 114), (198, 108), (222, 108), (234, 114), (230, 120), (190, 120)],
           stone_d, INK, 2)
    add_glow(p.image, 198, 128, 12, (196, 110, 255, 110), p.s)
    add_glow(p.image, 222, 128, 12, (196, 110, 255, 110), p.s)
    p.ellipse([192, 122, 204, 134], (36, 20, 46, 255), (222, 160, 255, 240), 2)
    p.ellipse([216, 122, 228, 134], (36, 20, 46, 255), (222, 160, 255, 240), 2)
    p.circle(198, 128, 2.4, (246, 216, 255, 240), None, 0)
    p.circle(222, 128, 2.4, (246, 216, 255, 240), None, 0)
    p.line([(198, 148), (222, 148)], (46, 40, 48, 255), 4, outline=INK)
    for i in range(4):
        p.line([(200 + i * 7, 144), (200 + i * 7, 152)], (120, 110, 120, 220), 1.5,
               outline=None, caps=False)

    # floating void embers
    for x, y, r in ((66, 120, 3), (360, 90, 2), (44, 380, 2), (356, 392, 3), (120, 70, 2)):
        p.circle(x, y, r, (206, 128, 255, 180), None, 0)
