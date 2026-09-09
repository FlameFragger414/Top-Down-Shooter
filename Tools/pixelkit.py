"""Tiny pixel-art drawing kit: RGBA canvas + shape/shading helpers."""
import math
from PIL import Image

# ---------------------------------------------------------------- palette ---
P = {
    "void":      (10, 8, 16),
    "ink":       (18, 12, 22),
    "ink2":      (32, 22, 38),
    "purple_d":  (44, 22, 62),
    "purple":    (86, 38, 112),
    "magenta":   (200, 42, 128),
    "pink":      (247, 106, 168),
    "cyan":      (66, 224, 226),
    "teal":      (24, 122, 140),
    "lime":      (162, 230, 84),
    "amber":     (247, 178, 52),
    "orange":    (232, 106, 40),
    "cream":     (246, 233, 200),
    "bone":      (214, 198, 170),
    "grey_d":    (52, 48, 60),
    "grey":      (92, 88, 100),
    "grey_l":    (140, 138, 150),
    "steel_d":   (58, 62, 74),
    "steel":     (104, 112, 128),
    "steel_l":   (160, 170, 186),
    "blood_d":   (74, 8, 16),
    "blood":     (138, 16, 26),
    "blood_l":   (186, 34, 40),
    "skin_d":    (150, 92, 66),
    "skin":      (206, 148, 110),
    "skin_l":    (236, 190, 152),
    "hair_d":    (38, 24, 20),
    "hair":      (66, 42, 30),
    "wood_d":    (72, 44, 28),
    "wood":      (118, 74, 42),
    "wood_l":    (158, 108, 62),
    "white":     (240, 240, 240),
    "black":     (6, 5, 9),
}


def c(name, a=255):
    r, g, b = P[name]
    return (r, g, b, a)


def mix(c1, c2, t):
    return tuple(int(round(c1[i] + (c2[i] - c1[i]) * t)) for i in range(4))


def shade(col, amt):
    if amt < 0:
        return mix(col, (0, 0, 0, col[3]), -amt)
    return mix(col, (255, 255, 255, col[3]), amt)


def _cl(v):
    return 0 if v < 0 else (255 if v > 255 else int(v))


# ----------------------------------------------------------------- canvas ---
class Canvas:
    def __init__(self, w, h, fill=(0, 0, 0, 0)):
        self.w, self.h = w, h
        self.d = [list(fill) for _ in range(w * h)]

    def get(self, x, y):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            return tuple(self.d[y * self.w + x])
        return (0, 0, 0, 0)

    def px(self, x, y, col, wrap=False):
        x, y = int(round(x)), int(round(y))
        if wrap:
            x %= self.w
            y %= self.h
        if not (0 <= x < self.w and 0 <= y < self.h):
            return
        if col[3] >= 255:
            self.d[y * self.w + x] = list(col)
        elif col[3] > 0:
            dst = self.d[y * self.w + x]
            a = col[3] / 255.0
            na = col[3] + dst[3] * (1 - a)
            if na <= 0:
                return
            for i in range(3):
                dst[i] = int(round((col[i] * col[3] + dst[i] * dst[3] * (1 - a)) / na))
            dst[3] = int(round(na))

    def rect(self, x0, y0, x1, y1, col, wrap=False):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.px(x, y, col, wrap)

    def frame(self, x0, y0, x1, y1, col):
        self.rect(x0, y0, x1, y0, col)
        self.rect(x0, y1, x1, y1, col)
        self.rect(x0, y0, x0, y1, col)
        self.rect(x1, y0, x1, y1, col)

    def ellipse(self, cx, cy, rx, ry, col, wrap=False):
        rx = max(rx, 0.5)
        ry = max(ry, 0.5)
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                dx = (x - cx) / rx
                dy = (y - cy) / ry
                if dx * dx + dy * dy <= 1.0:
                    self.px(x, y, col, wrap)

    def circle(self, cx, cy, r, col, wrap=False):
        self.ellipse(cx, cy, r, r, col, wrap)

    def line(self, x0, y0, x1, y1, col, thick=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) * 2 + 1
        for i in range(n + 1):
            t = i / n
            x = x0 + (x1 - x0) * t
            y = y0 + (y1 - y0) * t
            if thick <= 1:
                self.px(x, y, col)
            else:
                self.circle(x, y, thick * 0.5, col)

    def capsule(self, x0, y0, x1, y1, r, col):
        self.line(x0, y0, x1, y1, col, thick=r * 2)

    def outline(self, col, diagonal=False):
        offs = [(-1, 0), (1, 0), (0, -1), (0, 1)]
        if diagonal:
            offs += [(-1, -1), (1, -1), (-1, 1), (1, 1)]
        add = []
        for y in range(self.h):
            for x in range(self.w):
                if self.get(x, y)[3] > 0:
                    continue
                for ox, oy in offs:
                    if self.get(x + ox, y + oy)[3] > 128:
                        add.append((x, y))
                        break
        for x, y in add:
            self.px(x, y, col)

    def drop_shadow(self, ox=0, oy=2, col=(0, 0, 0, 90), grow=0):
        src = Canvas(self.w, self.h)
        src.d = [list(v) for v in self.d]
        sh = Canvas(self.w, self.h)
        for y in range(self.h):
            for x in range(self.w):
                if src.get(x, y)[3] > 40:
                    if grow:
                        sh.circle(x + ox, y + oy, grow, col)
                    else:
                        sh.px(x + ox, y + oy, col)
        for y in range(self.h):
            for x in range(self.w):
                s = sh.get(x, y)
                if s[3] and src.get(x, y)[3] == 0:
                    self.px(x, y, s)

    def tint_region(self, x0, y0, x1, y1, col, t):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                cur = self.get(x, y)
                if cur[3]:
                    self.px(x, y, mix(cur, col, t))

    def noise(self, amount, rng, only_opaque=True):
        for y in range(self.h):
            for x in range(self.w):
                cur = self.get(x, y)
                if only_opaque and cur[3] == 0:
                    continue
                k = rng.uniform(-amount, amount)
                self.px(x, y, (_cl(cur[0] + k), _cl(cur[1] + k), _cl(cur[2] + k), cur[3]))

    def dither_grad(self, x0, y0, x1, y1, ca, cb, vertical=True):
        bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                t = ((y - y0) / max(1, y1 - y0)) if vertical else ((x - x0) / max(1, x1 - x0))
                th = (bayer[y % 4][x % 4] + 0.5) / 16.0
                self.px(x, y, cb if t > th else ca)

    def blit(self, other, ox, oy):
        for y in range(other.h):
            for x in range(other.w):
                v = other.get(x, y)
                if v[3]:
                    self.px(x + ox, y + oy, v)

    def flip_h(self):
        out = Canvas(self.w, self.h)
        for y in range(self.h):
            for x in range(self.w):
                out.d[y * self.w + (self.w - 1 - x)] = list(self.d[y * self.w + x])
        return out

    def save(self, path):
        img = Image.new("RGBA", (self.w, self.h))
        img.putdata([tuple(v) for v in self.d])
        img.save(path)
        return path


def wrapped_blob(cv, cx, cy, r, col, rng, lumps=6, wrap=True):
    cv.circle(cx, cy, r, col, wrap)
    for _ in range(lumps):
        a = rng.uniform(0, math.pi * 2)
        d = rng.uniform(r * 0.35, r * 1.0)
        cv.circle(cx + math.cos(a) * d, cy + math.sin(a) * d,
                  r * rng.uniform(0.28, 0.62), col, wrap)
