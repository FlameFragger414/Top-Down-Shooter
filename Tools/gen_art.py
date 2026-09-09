"""Generates every sprite the game uses into Assets/Resources/Art as PNG + .meta.

Top-down orthographic view. Characters and weapons all face UP (+Y in Unity),
so gameplay code only ever sets a single Z rotation.
"""
import hashlib
import math
import os
import random
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from pixelkit import Canvas, c, mix, shade, wrapped_blob, P  # noqa: E402
import font5x7  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Art")
PPU = 32
INK = (14, 10, 18, 255)

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
  spritePackingTag:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def emit(cv, name, ppu=PPU):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    cv.save(path)
    guid = hashlib.md5(("hlm/" + name).encode()).hexdigest()
    with open(path + ".meta", "w", newline="\n") as f:
        f.write(META.format(guid=guid, ppu=ppu))
    return path


# =========================================================== characters ====
def draw_head(cv, cx, hy, hr, kit):
    """Top of a skull seen from above: hair dome, thin sliver of face at the front."""
    skin = kit.get("skin", c("skin"))
    hair = kit.get("hair", c("hair"))
    if kit.get("mask"):
        draw_mask(cv, cx, hy, hr, kit["mask"])
        return
    cv.circle(cx, hy, hr + 0.9, shade(kit.get("hair", c("hair")), -0.75))
    if kit.get("bald"):
        cv.circle(cx, hy, hr, skin)
        cv.ellipse(cx, hy + hr * 0.45, hr * 0.95, hr * 0.55, shade(hair, -0.1))
        cv.circle(cx - hr * 0.35, hy - hr * 0.35, hr * 0.30, shade(skin, 0.22))
    else:
        cv.circle(cx, hy, hr, hair)
        cv.circle(cx - hr * 0.34, hy - hr * 0.30, hr * 0.40, shade(hair, 0.20))
        cv.ellipse(cx, hy + hr * 0.55, hr * 0.85, hr * 0.42, shade(hair, -0.35))
    # forward sliver of face, so the facing direction is unmistakable
    cv.ellipse(cx, hy - hr * 0.76, hr * 0.40, hr * 0.24, skin)


def draw_person(cv, cx, cy, kit, scale=1.0):
    """Top-down human, facing up (-y in image space)."""
    jacket = kit["jacket"]
    jacket_d = shade(jacket, -0.42)
    jacket_l = shade(jacket, 0.22)
    shirt = kit["shirt"]
    skin = kit.get("skin", c("skin"))
    pants = kit.get("pants", c("ink2"))
    build = kit.get("build", 1.0)

    sx = scale * build
    sy = scale

    # ground shadow
    cv.ellipse(cx + 1, cy + 4 * sy, 8.8 * sx, 7.4 * sy, (0, 0, 0, 72))

    # feet peeking out behind the torso
    cv.circle(cx - 3.2 * sx, cy + 6.4 * sy, 1.9 * sx, shade(pants, -0.5))
    cv.circle(cx + 3.2 * sx, cy + 6.4 * sy, 1.9 * sx, shade(pants, -0.5))

    # torso mass, then the shoulder band on top of it
    cv.ellipse(cx, cy + 2.6 * sy, 7.2 * sx, 5.2 * sy, jacket_d)
    cv.ellipse(cx, cy - 0.8 * sy, 8.2 * sx, 4.4 * sy, jacket)
    cv.ellipse(cx - 3.6 * sx, cy - 1.2 * sy, 3.6 * sx, 2.8 * sy, jacket_l)
    cv.ellipse(cx + 4.2 * sx, cy + 0.4 * sy, 3.0 * sx, 3.0 * sy, jacket_d)

    # shirt at the collar
    cv.ellipse(cx, cy - 1.0 * sy, 2.0 * sx, 1.6 * sy, shirt)

    # short arm stubs reaching forward past the shoulders
    cv.capsule(cx - 7.0 * sx, cy - 1.2 * sy, cx - 6.0 * sx, cy - 6.2 * sy, 2.1 * sx, jacket)
    cv.capsule(cx + 7.0 * sx, cy - 1.2 * sy, cx + 6.0 * sx, cy - 6.2 * sy, 2.1 * sx, jacket_d)
    cv.circle(cx - 6.0 * sx, cy - 6.8 * sy, 1.6 * sx, skin)
    cv.circle(cx + 6.0 * sx, cy - 6.8 * sy, 1.6 * sx, shade(skin, -0.16))

    # head sits on top of the shoulders and dominates the silhouette
    hr = 5.3 * scale
    draw_head(cv, cx, cy - 3.9 * sy, hr, kit)


def draw_mask(cv, cx, cy, r, kind):
    base = {
        "rooster": c("cream"), "owl": c("bone"), "tiger": c("amber"),
        "wolf": c("steel_l"), "pig": c("pink"), "rabbit": c("white"),
    }[kind]
    cv.circle(cx, cy, r * 0.98, base)
    cv.ellipse(cx, cy - r * 0.45, r * 0.72, r * 0.5, shade(base, 0.22))
    dark = shade(base, -0.55)
    # eye slits
    cv.rect(cx - 3, cy - 3, cx - 2, cy - 2, dark)
    cv.rect(cx + 2, cy - 3, cx + 3, cy - 2, dark)
    if kind == "rooster":
        cv.rect(cx - 1, cy - r - 2, cx + 1, cy - r + 1, c("blood_l"))
        cv.px(cx, cy - r - 3, c("blood"))
        cv.rect(cx - 1, cy - r * 0.9, cx + 1, cy - r * 0.5, c("amber"))
    elif kind == "owl":
        cv.circle(cx - 2.4, cy - 2.4, 2.2, dark)
        cv.circle(cx + 2.4, cy - 2.4, 2.2, dark)
        cv.px(cx - 2, cy - 3, c("amber"))
        cv.px(cx + 2, cy - 3, c("amber"))
    elif kind == "tiger":
        for i in (-3, 0, 3):
            cv.line(cx + i, cy - r * 0.8, cx + i * 1.3, cy - r * 0.1, dark)
    elif kind == "wolf":
        cv.line(cx - r * 0.8, cy - r * 0.4, cx - r * 0.5, cy - r, dark, 2)
        cv.line(cx + r * 0.8, cy - r * 0.4, cx + r * 0.5, cy - r, dark, 2)
    elif kind == "pig":
        cv.circle(cx, cy - r * 0.75, 1.8, shade(base, -0.35))
    elif kind == "rabbit":
        cv.capsule(cx - 2, cy - r * 0.6, cx - 3, cy - r - 3, 1.2, base)
        cv.capsule(cx + 2, cy - r * 0.6, cx + 3, cy - r - 3, 1.2, base)


def draw_corpse(cv, cx, cy, kit, rng):
    """Dead pose: face down, limbs splayed, head lolling to one side."""
    jacket = kit["jacket"]
    jacket_d = shade(jacket, -0.45)
    skin = kit.get("skin", c("skin"))
    pants = kit.get("pants", c("ink2"))
    build = kit.get("build", 1.0)
    tilt = rng.uniform(-0.5, 0.5)

    def rot(dx, dy):
        s, co = math.sin(tilt), math.cos(tilt)
        return cx + dx * co - dy * s, cy + dx * s + dy * co

    # legs splayed
    for side in (-1, 1):
        hip = rot(side * 3.2 * build, 4.0)
        knee = rot(side * 7.5 * build, 10.0)
        foot = rot(side * 10.5 * build, 14.0)
        cv.capsule(hip[0], hip[1], knee[0], knee[1], 2.5 * build, pants)
        cv.capsule(knee[0], knee[1], foot[0], foot[1], 2.1 * build, pants)
        cv.circle(foot[0], foot[1], 2.0 * build, shade(pants, -0.5))

    # arms thrown out
    for side, reach in ((-1, 11.0), (1, 9.0)):
        sh = rot(side * 6.5 * build, -3.0)
        el = rot(side * (reach - 2) * build, 1.0)
        hd = rot(side * reach * build, 5.5)
        cv.capsule(sh[0], sh[1], el[0], el[1], 2.4 * build, jacket)
        cv.capsule(el[0], el[1], hd[0], hd[1], 2.1 * build, jacket_d)
        cv.circle(hd[0], hd[1], 1.9, skin)

    # torso face-down
    tc = rot(0, 1.0)
    cv.ellipse(tc[0], tc[1], 8.4 * build, 7.0, jacket)
    cv.ellipse(tc[0], tc[1] + 2.0, 7.0 * build, 4.6, jacket_d)
    cv.line(tc[0] - 5, tc[1] - 4, tc[0] + 5, tc[1] - 4, shade(jacket, 0.15), 2)

    # head lolled sideways
    hc = rot(rng.choice((-1, 1)) * 3.0, -8.5)
    hkit = dict(kit)
    draw_head(cv, hc[0], hc[1], 4.7, hkit)
    # exit wound
    cv.circle(tc[0] + rng.uniform(-3, 3), tc[1] + rng.uniform(-3, 3), 2.0, c("blood"))


KITS = {
    "player": dict(jacket=(46, 122, 130, 255), shirt=c("cream"), pants=c("ink2"),
                   mask="rooster"),
    "thug": dict(jacket=c("white"), shirt=c("pink"), pants=(60, 56, 66, 255), hair=(44, 30, 24, 255)),
    "suit": dict(jacket=(38, 34, 52, 255), shirt=c("cream"), pants=(30, 26, 42, 255),
                 hair=(112, 88, 66, 255)),
    "heavy": dict(jacket=(150, 34, 44, 255), shirt=c("bone"), pants=c("ink2"),
                  hair=c("hair"), build=1.22, bald=True),
    "boss": dict(jacket=(24, 24, 34, 255), shirt=c("blood"), pants=c("ink"),
                 mask="wolf", build=1.12),
    "goon": dict(jacket=(70, 52, 104, 255), shirt=c("amber"), pants=c("ink2"),
                 hair=(168, 132, 74, 255)),
}


def gen_characters():
    for name, kit in KITS.items():
        cv = Canvas(32, 32)
        draw_person(cv, 16, 16, kit)
        cv.outline(INK, diagonal=True)
        emit(cv, "char_" + name)

    # dog: top-down, snout forward
    cv = Canvas(32, 32)
    fur = (118, 88, 58, 255)
    cv.ellipse(16, 20, 8, 7, (0, 0, 0, 70))
    cv.ellipse(16, 18, 5.4, 8.2, fur)
    cv.ellipse(16, 22, 4.6, 4.6, shade(fur, -0.3))
    cv.capsule(16, 25, 16, 30, 1.4, shade(fur, -0.4))
    cv.circle(16, 11, 4.6, shade(fur, 0.1))
    cv.capsule(16, 9, 16, 5, 1.8, shade(fur, -0.15))
    cv.circle(16, 4, 1.6, c("ink"))
    cv.capsule(12.6, 9.5, 11.5, 6.5, 1.4, shade(fur, -0.35))
    cv.capsule(19.4, 9.5, 20.5, 6.5, 1.4, shade(fur, -0.35))
    cv.px(14, 9, c("blood_l"))
    cv.px(18, 9, c("blood_l"))
    for lx in (-5, 5):
        cv.capsule(16 + lx, 14, 16 + lx * 1.2, 17, 1.4, shade(fur, -0.25))
        cv.capsule(16 + lx, 22, 16 + lx * 1.2, 25, 1.4, shade(fur, -0.25))
    cv.outline(INK, diagonal=True)
    emit(cv, "char_dog")


def gen_corpses():
    rng = random.Random(7)
    for name, kit in KITS.items():
        cv = Canvas(48, 48)
        # blood pool first so it sits under the body
        for _ in range(9):
            wrapped_blob(cv, 24 + rng.uniform(-9, 9), 26 + rng.uniform(-8, 8),
                         rng.uniform(4, 9), c("blood_d"), rng, 5, wrap=False)
        cv.ellipse(24, 26, 13, 11, c("blood_d"))
        for _ in range(5):
            wrapped_blob(cv, 24 + rng.uniform(-7, 7), 26 + rng.uniform(-6, 6),
                         rng.uniform(2.5, 5), c("blood"), rng, 4, wrap=False)
        body = Canvas(48, 48)
        draw_corpse(body, 24, 25, kit, rng)
        body.outline(INK, diagonal=True)
        cv.blit(body, 0, 0)
        for _ in range(14):
            a = rng.uniform(0, math.pi * 2)
            d = rng.uniform(12, 21)
            cv.circle(24 + math.cos(a) * d, 26 + math.sin(a) * d,
                      rng.uniform(0.6, 1.8), c("blood"))
        emit(cv, "corpse_" + name)

    # dog corpse: same pool treatment, limp body on its side
    cv = Canvas(48, 48)
    for _ in range(8):
        wrapped_blob(cv, 24 + rng.uniform(-8, 8), 26 + rng.uniform(-7, 7),
                     rng.uniform(4, 8), c("blood_d"), rng, 5, wrap=False)
    cv.ellipse(24, 26, 11, 9, c("blood_d"))
    fur = (118, 88, 58, 255)
    cv.ellipse(24, 26, 9.5, 5.2, fur)
    cv.ellipse(28, 26, 5.0, 4.2, shade(fur, -0.28))
    cv.circle(14, 24, 4.4, shade(fur, 0.08))
    cv.capsule(11, 23, 7, 21, 1.7, shade(fur, -0.12))
    cv.circle(6, 20.5, 1.5, c("ink"))
    cv.capsule(33, 27, 40, 31, 1.4, shade(fur, -0.4))
    for lx, ly in ((20, 33), (28, 33), (20, 19), (29, 19)):
        cv.capsule(lx, 26, lx + rng.uniform(-2, 2), ly, 1.5, shade(fur, -0.25))
    cv.circle(22, 25, 2.0, c("blood"))
    cv.outline(INK, diagonal=True)
    for _ in range(12):
        a = rng.uniform(0, math.pi * 2)
        d = rng.uniform(12, 20)
        cv.circle(24 + math.cos(a) * d, 26 + math.sin(a) * d, rng.uniform(0.6, 1.6), c("blood"))
    emit(cv, "corpse_dog")


# ============================================================== weapons ====
def _steel(cv, x0, y0, x1, y1, base=None):
    base = base or c("steel_d")
    cv.rect(x0, y0, x1, y1, base)
    cv.rect(x0, y0, x0, y1, shade(base, 0.28))
    cv.rect(x1, y0, x1, y1, shade(base, -0.35))


def gen_weapons():
    W, H = 18, 30
    mid = W // 2

    def new():
        return Canvas(W, H)

    # --- pistol
    cv = new()
    _steel(cv, mid - 2, 6, mid + 2, 17)
    cv.rect(mid - 1, 4, mid + 1, 6, c("steel"))
    cv.rect(mid - 2, 17, mid + 2, 22, shade(c("ink2"), 0.1))
    cv.rect(mid - 3, 20, mid + 1, 25, c("hair_d"))
    cv.px(mid - 2, 8, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_pistol")

    # --- silenced pistol
    cv = new()
    _steel(cv, mid - 2, 8, mid + 2, 18)
    cv.rect(mid - 2, 1, mid + 2, 8, c("ink2"))
    cv.rect(mid - 2, 1, mid - 2, 8, c("grey"))
    cv.rect(mid - 2, 18, mid + 2, 22, shade(c("ink2"), 0.1))
    cv.rect(mid - 3, 20, mid + 1, 25, c("hair_d"))
    cv.outline(INK)
    emit(cv, "wpn_silenced")

    # --- shotgun
    cv = new()
    _steel(cv, mid - 2, 1, mid + 2, 16)
    cv.rect(mid - 3, 13, mid + 3, 17, c("steel"))
    cv.rect(mid - 2, 16, mid + 2, 27, c("wood"))
    cv.rect(mid - 2, 16, mid - 2, 27, c("wood_l"))
    cv.rect(mid + 2, 16, mid + 2, 27, c("wood_d"))
    cv.rect(mid - 1, 2, mid - 1, 12, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_shotgun")

    # --- uzi
    cv = new()
    _steel(cv, mid - 2, 5, mid + 2, 15)
    cv.rect(mid - 1, 2, mid + 1, 5, c("steel"))
    cv.rect(mid - 3, 15, mid + 3, 19, c("ink2"))
    cv.rect(mid - 1, 19, mid + 1, 26, c("grey_d"))
    cv.px(mid - 2, 7, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_uzi")

    # --- assault rifle
    cv = new()
    _steel(cv, mid - 2, 0, mid + 2, 14)
    cv.rect(mid - 3, 12, mid + 3, 20, c("ink2"))
    cv.rect(mid - 2, 20, mid + 2, 28, c("grey_d"))
    cv.rect(mid + 2, 15, mid + 4, 22, c("ink"))
    cv.rect(mid - 1, 1, mid - 1, 11, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_rifle")

    # --- bat
    cv = new()
    for y in range(3, 25):
        t = (y - 3) / 22.0
        w = 3.2 - 2.2 * t
        cv.rect(mid - w, y, mid + w, y, c("wood"))
        cv.px(mid - w, y, c("wood_l"))
        cv.px(mid + w, y, c("wood_d"))
    cv.rect(mid - 1, 25, mid + 1, 28, c("hair_d"))
    cv.outline(INK)
    emit(cv, "wpn_bat")

    # --- knife
    cv = new()
    for y in range(4, 17):
        t = (y - 4) / 13.0
        w = 0.6 + 1.6 * t
        cv.rect(mid - w, y, mid + w, y, c("steel_l"))
        cv.px(mid + w, y, c("steel_d"))
    cv.rect(mid - 3, 17, mid + 3, 18, c("steel"))
    cv.rect(mid - 2, 18, mid + 2, 25, c("hair_d"))
    cv.outline(INK)
    emit(cv, "wpn_knife")

    # --- katana
    cv = new()
    cv.rect(mid - 1, 0, mid, 18, c("steel_l"))
    cv.rect(mid, 0, mid, 18, c("white"))
    cv.rect(mid - 3, 18, mid + 3, 19, c("amber"))
    cv.rect(mid - 1, 19, mid + 1, 28, c("ink2"))
    for y in range(20, 28, 2):
        cv.px(mid, y, c("amber"))
    cv.outline(INK)
    emit(cv, "wpn_katana")

    # --- lead pipe
    cv = new()
    _steel(cv, mid - 2, 3, mid + 2, 26, c("steel"))
    cv.rect(mid - 2, 3, mid + 2, 4, c("steel_l"))
    cv.px(mid - 1, 12, c("grey_l"))
    cv.outline(INK)
    emit(cv, "wpn_pipe")

    # --- fists (held-item placeholder, also the pickup icon for "unarmed")
    cv = new()
    cv.circle(mid, 12, 3.2, c("skin"))
    cv.circle(mid - 1, 11, 1.4, c("skin_l"))
    cv.outline(INK)
    emit(cv, "wpn_fist")

    # --- revolver
    cv = new()
    _steel(cv, mid - 2, 7, mid + 2, 16, c("steel"))
    cv.rect(mid - 1, 4, mid + 1, 7, c("steel_l"))
    cv.ellipse(mid, 17, 3.4, 2.6, c("steel_d"))          # cylinder
    cv.px(mid - 2, 17, c("steel_l"))
    cv.px(mid + 2, 17, c("grey_d"))
    cv.rect(mid - 3, 20, mid + 1, 26, c("wood"))
    cv.rect(mid - 3, 20, mid - 3, 26, c("wood_l"))
    cv.outline(INK)
    emit(cv, "wpn_revolver")

    # --- double barrel
    cv = new()
    _steel(cv, mid - 3, 2, mid - 1, 16, c("steel"))
    _steel(cv, mid + 1, 2, mid + 3, 16, c("steel"))
    cv.rect(mid - 3, 16, mid + 3, 19, c("steel_d"))
    cv.rect(mid - 2, 19, mid + 2, 27, c("wood_d"))
    cv.rect(mid - 2, 19, mid - 2, 27, c("wood"))
    cv.outline(INK)
    emit(cv, "wpn_double")

    # --- sniper
    cv = new()
    _steel(cv, mid - 1, 0, mid + 1, 15, c("steel_d"))
    cv.rect(mid - 2, 12, mid + 2, 20, c("ink2"))
    cv.rect(mid - 3, 13, mid + 3, 15, c("grey_d"))       # scope
    cv.rect(mid - 3, 13, mid + 3, 13, c("steel_l"))
    cv.px(mid + 2, 14, c("cyan"))
    cv.rect(mid - 2, 20, mid + 2, 28, c("wood_d"))
    cv.outline(INK)
    emit(cv, "wpn_sniper")

    # --- light machine gun
    cv = new()
    _steel(cv, mid - 2, 0, mid + 2, 13, c("steel_d"))
    for y in range(2, 12, 3):
        cv.rect(mid - 3, y, mid + 3, y, c("grey_d"))      # barrel shroud vents
    cv.rect(mid - 4, 13, mid + 4, 21, c("ink2"))
    cv.rect(mid - 6, 15, mid - 4, 22, c("grey_d"))        # belt box
    cv.rect(mid - 2, 21, mid + 2, 28, c("grey_d"))
    cv.line(mid - 4, 6, mid - 7, 10, c("steel"), 1)       # bipod
    cv.line(mid + 4, 6, mid + 7, 10, c("steel"), 1)
    cv.outline(INK)
    emit(cv, "wpn_lmg")

    # --- minigun
    cv = new()
    for i, off in enumerate((-3, 0, 3)):
        _steel(cv, mid + off - 1, 1, mid + off + 1, 14,
               c("steel") if i == 1 else c("steel_d"))
    cv.ellipse(mid, 15, 5.0, 3.0, c("grey_d"))
    cv.rect(mid - 5, 16, mid + 5, 23, c("ink2"))
    cv.rect(mid - 5, 16, mid + 5, 17, c("grey"))
    cv.rect(mid + 4, 18, mid + 7, 25, c("amber"))         # ammo drum
    cv.rect(mid - 2, 23, mid + 2, 28, c("grey_d"))
    cv.outline(INK)
    emit(cv, "wpn_minigun")

    # --- rocket launcher
    cv = new()
    cv.rect(mid - 3, 6, mid + 3, 24, c("lime"))
    cv.rect(mid - 3, 6, mid - 3, 24, shade(c("lime"), 0.25))
    cv.rect(mid + 3, 6, mid + 3, 24, shade(c("lime"), -0.4))
    for y in range(9, 23, 5):
        cv.rect(mid - 3, y, mid + 3, y, shade(c("lime"), -0.5))
    # warhead poking out of the tube
    cv.rect(mid - 2, 2, mid + 2, 6, c("blood"))
    cv.line(mid - 2, 2, mid, -1, c("blood_l"))
    cv.line(mid + 2, 2, mid, -1, c("blood_l"))
    cv.rect(mid - 1, 24, mid + 1, 28, c("ink2"))
    cv.outline(INK)
    emit(cv, "wpn_rpg")

    # --- compact smg
    cv = new()
    _steel(cv, mid - 2, 6, mid + 2, 14, c("steel_d"))
    cv.rect(mid - 1, 3, mid + 1, 6, c("steel"))
    cv.rect(mid - 3, 14, mid + 3, 18, c("ink2"))
    cv.rect(mid - 4, 17, mid - 1, 25, c("grey_d"))       # canted magazine
    cv.rect(mid, 18, mid + 2, 25, c("hair_d"))
    cv.px(mid - 2, 8, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_smg")

    # --- grenade launcher
    cv = new()
    _steel(cv, mid - 3, 3, mid + 3, 13, c("steel"))
    cv.rect(mid - 2, 1, mid + 2, 3, c("grey_d"))
    cv.ellipse(mid, 17, 5.2, 4.0, c("ink2"))             # drum
    cv.ellipse(mid, 17, 2.4, 2.0, c("grey"))
    for a in (0, 1, 2, 3):
        cv.px(mid - 3 + a * 2, 14 + (a % 2), c("amber"))
    cv.rect(mid - 2, 21, mid + 2, 28, c("wood_d"))
    cv.outline(INK)
    emit(cv, "wpn_gl")

    # --- automatic shotgun
    cv = new()
    _steel(cv, mid - 2, 1, mid + 2, 13, c("steel_d"))
    cv.rect(mid - 3, 13, mid + 3, 18, c("ink2"))
    cv.ellipse(mid, 21, 4.4, 3.6, c("grey_d"))           # drum magazine
    cv.ellipse(mid, 21, 2.0, 1.6, c("amber"))
    cv.rect(mid - 1, 24, mid + 1, 28, c("hair_d"))
    cv.rect(mid - 1, 2, mid - 1, 11, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_autoshotgun")

    # --- twin pistols
    cv = new()
    for off in (-4, 4):
        _steel(cv, mid + off - 1, 7, mid + off + 1, 16, c("steel_d"))
        cv.rect(mid + off - 1, 5, mid + off, 7, c("steel"))
        cv.rect(mid + off - 2, 17, mid + off + 1, 23, c("hair_d"))
    cv.px(mid - 5, 9, c("steel_l"))
    cv.px(mid + 3, 9, c("steel_l"))
    cv.outline(INK)
    emit(cv, "wpn_akimbo")

    # --- flamethrower
    cv = new()
    _steel(cv, mid - 1, 1, mid + 1, 12, c("steel_d"))
    cv.rect(mid - 2, 3, mid + 2, 5, c("grey_d"))
    cv.rect(mid - 3, 12, mid + 3, 18, c("blood"))        # fuel tank
    cv.rect(mid - 3, 12, mid - 3, 18, c("blood_l"))
    cv.rect(mid + 3, 12, mid + 3, 18, c("blood_d"))
    cv.rect(mid - 2, 18, mid + 2, 20, c("amber"))
    cv.rect(mid - 1, 20, mid + 1, 27, c("ink2"))
    cv.outline(INK)
    emit(cv, "wpn_flamer")


# ================================================================== fx =====
def gen_fx():
    rng = random.Random(11)

    # muzzle flashes, pointing up, pivot centre
    for i, sz in enumerate((5.0, 7.5, 5.5)):
        cv = Canvas(20, 20)
        cx, cy = 10, 12
        cv.ellipse(cx, cy - sz * 0.5, sz * 0.55, sz, c("amber", 210))
        cv.ellipse(cx, cy - sz * 0.4, sz * 0.34, sz * 0.7, c("cream", 235))
        for _ in range(5 + i * 3):
            a = rng.uniform(-1.9, -1.25)
            d = rng.uniform(sz * 0.7, sz * 1.7)
            cv.circle(cx + math.cos(a) * d, cy + math.sin(a) * d,
                      rng.uniform(0.6, 1.7), c("amber", 190))
        cv.ellipse(cx, cy, sz * 0.5, sz * 0.45, c("white", 240))
        emit(cv, "fx_muzzle_%d" % i)

    # blood splats
    for i in range(6):
        cv = Canvas(28, 28)
        n = 4 + i
        for _ in range(n):
            wrapped_blob(cv, 14 + rng.uniform(-6, 6), 14 + rng.uniform(-6, 6),
                         rng.uniform(2.5, 6.0), c("blood_d"), rng, 5, wrap=False)
        for _ in range(n):
            wrapped_blob(cv, 14 + rng.uniform(-4, 4), 14 + rng.uniform(-4, 4),
                         rng.uniform(1.5, 3.4), c("blood"), rng, 4, wrap=False)
        for _ in range(10 + i * 2):
            a = rng.uniform(0, math.pi * 2)
            d = rng.uniform(7, 13)
            cv.circle(14 + math.cos(a) * d, 14 + math.sin(a) * d,
                      rng.uniform(0.5, 1.4), c("blood"))
        emit(cv, "fx_blood_%d" % i)

    # big pool
    cv = Canvas(40, 40)
    for _ in range(16):
        wrapped_blob(cv, 20 + rng.uniform(-8, 8), 20 + rng.uniform(-8, 8),
                     rng.uniform(5, 10), c("blood_d"), rng, 6, wrap=False)
    for _ in range(8):
        wrapped_blob(cv, 20 + rng.uniform(-6, 6), 20 + rng.uniform(-6, 6),
                     rng.uniform(2, 5), c("blood"), rng, 4, wrap=False)
    emit(cv, "fx_pool")

    # gore chunks
    for i in range(4):
        cv = Canvas(10, 10)
        wrapped_blob(cv, 5, 5, rng.uniform(1.6, 2.8), c("blood"), rng, 4, wrap=False)
        cv.px(4, 4, c("blood_l"))
        cv.outline((40, 4, 10, 255))
        emit(cv, "fx_gore_%d" % i)

    cv = Canvas(8, 8)
    cv.circle(4, 4, 2.1, (0, 0, 0, 220))
    cv.circle(4, 4, 1.1, (0, 0, 0, 255))
    cv.circle(3, 3, 0.8, (90, 84, 96, 160))
    emit(cv, "fx_hole")

    cv = Canvas(6, 6)
    cv.rect(2, 1, 3, 4, c("amber"))
    cv.px(2, 1, c("cream"))
    cv.outline((60, 40, 8, 255))
    emit(cv, "fx_shell")

    cv = Canvas(4, 14)
    cv.rect(1, 0, 2, 13, c("amber", 200))
    cv.rect(1, 0, 2, 4, c("cream", 235))
    emit(cv, "fx_tracer")

    cv = Canvas(10, 10)
    for _ in range(7):
        a = rng.uniform(0, math.pi * 2)
        d = rng.uniform(0, 4)
        cv.px(5 + math.cos(a) * d, 5 + math.sin(a) * d, c("cream", 230))
    cv.circle(5, 5, 1.4, c("amber", 220))
    emit(cv, "fx_spark")

    cv = Canvas(16, 16)
    for _ in range(10):
        wrapped_blob(cv, 8 + rng.uniform(-3, 3), 8 + rng.uniform(-3, 3),
                     rng.uniform(2, 4), (150, 148, 160, 60), rng, 4, wrap=False)
    emit(cv, "fx_smoke")

    # flamethrower tongue
    for i in range(3):
        cv = Canvas(16, 18)
        for _ in range(10 + i * 4):
            a = rng.uniform(-2.5, -0.65)
            d = rng.uniform(0, 6 + i)
            cv.circle(8 + math.cos(a) * d * 0.6, 12 + math.sin(a) * d,
                      rng.uniform(1.6, 3.4), c("orange", 190))
        for _ in range(6):
            a = rng.uniform(-2.2, -0.95)
            d = rng.uniform(0, 4)
            cv.circle(8 + math.cos(a) * d * 0.6, 12 + math.sin(a) * d,
                      rng.uniform(1.0, 2.2), c("amber", 225))
        cv.circle(8, 12, 1.8, c("cream", 235))
        emit(cv, "fx_flame_%d" % i)

    # rocket in flight, nose up, flame behind
    cv = Canvas(12, 22)
    cv.rect(5, 3, 6, 14, c("steel_l"))
    cv.rect(6, 3, 6, 14, c("steel_d"))
    cv.rect(4, 6, 7, 12, c("blood"))
    cv.line(6, 2, 4, 6, c("blood_l"))
    cv.line(6, 2, 8, 6, c("blood_l"))
    cv.rect(3, 12, 4, 15, c("grey_d"))
    cv.rect(8, 12, 9, 15, c("grey_d"))
    cv.outline(INK)
    for i in range(9):
        yy = 16 + i * 0.6
        r = 2.4 - i * 0.22
        cv.ellipse(6, yy, r, r * 1.2, c("amber", 200 - i * 18))
        cv.ellipse(6, yy, r * 0.5, r * 0.7, c("cream", 210 - i * 20))
    emit(cv, "fx_rocket")

    # explosion frames: fireball collapsing into smoke
    for i in range(4):
        cv = Canvas(96, 96)
        cx = cy = 48
        grow = 0.42 + i * 0.20
        for _ in range(22 + i * 8):
            a = rng.uniform(0, math.pi * 2)
            d = rng.uniform(0, 42 * grow)
            r = rng.uniform(6, 15) * (1.0 - i * 0.12)
            fade = 1.0 - (d / (42 * grow + 1e-3))
            col = c("orange", int(190 * fade * (1 - i * 0.18)))
            cv.circle(cx + math.cos(a) * d, cy + math.sin(a) * d, r, col)
        for _ in range(14):
            a = rng.uniform(0, math.pi * 2)
            d = rng.uniform(0, 26 * grow)
            cv.circle(cx + math.cos(a) * d, cy + math.sin(a) * d,
                      rng.uniform(5, 12) * (1.0 - i * 0.2), c("amber", 220 - i * 45))
        if i < 2:
            cv.circle(cx, cy, 15 - i * 5, c("cream", 240 - i * 60))
        for _ in range(10 + i * 6):
            a = rng.uniform(0, math.pi * 2)
            d = rng.uniform(20 * grow, 46 * grow)
            cv.circle(cx + math.cos(a) * d, cy + math.sin(a) * d,
                      rng.uniform(3, 9), (90, 84, 96, 60 + i * 30))
        emit(cv, "fx_explosion_%d" % i)

    # vision cone: apex at the bottom centre, opening upward
    cv = Canvas(64, 64)
    import math as _m
    apex_x, apex_y = 32.0, 63.0
    half = _m.radians(52)
    for y in range(64):
        for x in range(64):
            dx = x + 0.5 - apex_x
            dy = apex_y - (y + 0.5)
            if dy <= 0:
                continue
            d = _m.hypot(dx, dy)
            if d > 62:
                continue
            ang = abs(_m.atan2(dx, dy))
            if ang > half:
                continue
            edge = 1.0 - (ang / half) ** 3
            fade = 1.0 - (d / 62.0) ** 1.4
            a = int(max(0.0, min(1.0, edge * fade)) * 210)
            if a > 2:
                cv.px(x, y, (255, 255, 255, a))
    emit(cv, "fx_cone")

    # soft radial glow used for lights / neon bloom seeds
    cv = Canvas(64, 64)
    for y in range(64):
        for x in range(64):
            d = math.hypot(x - 31.5, y - 31.5) / 31.5
            if d < 1:
                a = int((1 - d) ** 2 * 255)
                cv.px(x, y, (255, 255, 255, a))
    emit(cv, "fx_glow")


# =============================================================== tiles =====
def gen_tiles():
    rng = random.Random(3)

    def grime(cv, n, col, r=(1, 4)):
        for _ in range(n):
            wrapped_blob(cv, rng.uniform(0, 32), rng.uniform(0, 32),
                         rng.uniform(*r), col, rng, 4, wrap=True)

    # checkerboard ceramic
    cv = Canvas(32, 32)
    a = (206, 198, 186, 255)
    b = (150, 142, 136, 255)
    for gy in range(2):
        for gx in range(2):
            col = a if (gx + gy) % 2 == 0 else b
            cv.rect(gx * 16, gy * 16, gx * 16 + 15, gy * 16 + 15, col)
    for i in range(0, 33, 16):
        cv.rect(i - 1, 0, i - 1, 31, (108, 102, 100, 255), wrap=True)
        cv.rect(0, i - 1, 31, i - 1, (108, 102, 100, 255), wrap=True)
    grime(cv, 10, (120, 116, 112, 60))
    cv.noise(9, rng)
    emit(cv, "tile_check")

    # wood floor
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, c("wood"))
    for y in range(0, 32, 8):
        cv.rect(0, y, 31, y, c("wood_d"))
        for x in range(32):
            k = rng.uniform(-16, 16)
            for yy in range(y + 1, y + 8):
                cur = cv.get(x, yy % 32)
                cv.px(x, yy % 32, (max(0, cur[0] + int(k)), max(0, cur[1] + int(k * 0.7)),
                                   max(0, cur[2] + int(k * 0.5)), 255))
    for _ in range(6):
        x = rng.randrange(32)
        cv.rect(x, 0, x, 31, c("wood_d", 90), wrap=True)
    cv.noise(7, rng)
    emit(cv, "tile_wood")

    # deep pile carpet
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, (96, 26, 62, 255))
    for _ in range(700):
        x, y = rng.randrange(32), rng.randrange(32)
        cv.px(x, y, shade((96, 26, 62, 255), rng.uniform(-0.35, 0.32)))
    grime(cv, 6, (60, 14, 40, 70))
    emit(cv, "tile_carpet")

    # concrete
    cv = Canvas(32, 32)
    cv.rect(0, 0, 31, 31, (86, 84, 92, 255))
    cv.noise(16, rng)
    grime(cv, 14, (64, 62, 72, 80))
    for _ in range(3):
        x0, y0 = rng.randrange(32), rng.randrange(32)
        for i in range(rng.randrange(6, 16)):
            cv.px((x0 + i) % 32, (y0 + rng.randrange(-1, 2) * (i % 2)) % 32,
                  (66, 64, 74, 255))
    emit(cv, "tile_concrete")

    # neon dance floor
    cv = Canvas(32, 32)
    for gy in range(4):
        for gx in range(4):
            col = c("magenta") if (gx + gy) % 2 == 0 else c("purple_d")
            cv.rect(gx * 8, gy * 8, gx * 8 + 7, gy * 8 + 7, col)
            cv.rect(gx * 8, gy * 8, gx * 8 + 7, gy * 8, shade(col, 0.25))
    cv.noise(8, rng)
    emit(cv, "tile_neon")

    # walls (top-down: solid mass, lit from top-left)
    cv = Canvas(32, 32)
    brick = (104, 56, 66, 255)
    cv.rect(0, 0, 31, 31, brick)
    for row in range(0, 32, 8):
        off = 0 if (row // 8) % 2 == 0 else 8
        cv.rect(0, row, 31, row, shade(brick, -0.4), wrap=True)
        for x in range(off, off + 32, 16):
            cv.rect(x % 32, row, x % 32, row + 7, shade(brick, -0.4), wrap=True)
    cv.noise(11, rng)
    cv.rect(0, 0, 31, 0, shade(brick, 0.2))
    cv.rect(0, 0, 0, 31, shade(brick, 0.12))
    emit(cv, "wall_brick")

    cv = Canvas(32, 32)
    base = (60, 50, 78, 255)
    cv.rect(0, 0, 31, 31, base)
    cv.noise(10, rng)
    cv.rect(0, 0, 31, 1, shade(base, 0.22))
    cv.rect(0, 30, 31, 31, shade(base, -0.35))
    emit(cv, "wall_dark")

    cv = Canvas(32, 32)
    base = (80, 72, 102, 255)
    cv.rect(0, 0, 31, 31, base)
    for x in range(0, 32, 6):
        cv.rect(x, 0, x + 2, 31, shade(base, 0.14))
    cv.noise(8, rng)
    cv.rect(0, 0, 31, 1, shade(base, 0.26))
    emit(cv, "wall_stripe")

    # 1x1 white pixel (bars, flashes, letterboxing)
    cv = Canvas(4, 4, (255, 255, 255, 255))
    emit(cv, "ui_pixel")

    # scanline overlay tile, kept large so the tiled overlay stays a few hundred quads
    cv = Canvas(64, 64)
    for y in range(0, 64, 2):
        cv.rect(0, y, 63, y, (0, 0, 0, 74))
        cv.rect(0, y + 1, 63, y + 1, (0, 0, 0, 22))
    emit(cv, "ui_scanline")


# =============================================================== props =====
def gen_props():
    rng = random.Random(23)

    def base_shadow(cv, x0, y0, x1, y1):
        cv.rect(x0 + 2, y0 + 3, x1 + 2, y1 + 3, (0, 0, 0, 70))

    cv = Canvas(36, 28)
    base_shadow(cv, 2, 2, 31, 23)
    cv.rect(2, 2, 31, 23, c("wood"))
    cv.rect(2, 2, 31, 3, c("wood_l"))
    cv.rect(2, 22, 31, 23, c("wood_d"))
    for x in range(4, 31, 6):
        cv.rect(x, 4, x, 21, c("wood_d", 80))
    cv.noise(8, rng)
    cv.outline(INK)
    emit(cv, "prop_table")

    cv = Canvas(52, 32)
    base_shadow(cv, 2, 2, 47, 27)
    body = (52, 40, 86, 255)
    cv.rect(2, 2, 47, 27, body)
    cv.rect(2, 2, 47, 9, shade(body, 0.18))
    for x in (10, 24, 38):
        cv.rect(x - 7, 11, x + 7, 25, shade(body, -0.18))
        cv.rect(x - 7, 11, x + 7, 12, shade(body, 0.1))
    cv.rect(2, 2, 3, 27, shade(body, -0.3))
    cv.rect(46, 2, 47, 27, shade(body, -0.3))
    cv.noise(7, rng)
    cv.outline(INK)
    emit(cv, "prop_sofa")

    cv = Canvas(24, 20)
    base_shadow(cv, 2, 2, 21, 17)
    cv.rect(2, 2, 21, 17, c("ink2"))
    cv.rect(4, 4, 19, 14, (26, 40, 52, 255))
    cv.rect(4, 4, 19, 5, (60, 92, 110, 255))
    for _ in range(30):
        cv.px(rng.randrange(4, 20), rng.randrange(4, 15), (90, 130, 150, 90))
    cv.rect(9, 15, 14, 16, c("grey_d"))
    cv.outline(INK)
    emit(cv, "prop_tv")

    cv = Canvas(24, 24)
    base_shadow(cv, 6, 12, 17, 21)
    cv.rect(6, 12, 17, 21, c("orange"))
    cv.rect(6, 12, 17, 13, shade(c("orange"), 0.2))
    for _ in range(16):
        a = rng.uniform(0, math.pi * 2)
        d = rng.uniform(1, 7)
        cv.circle(12 + math.cos(a) * d, 9 + math.sin(a) * d * 0.8,
                  rng.uniform(1.6, 3.2), shade(c("lime"), rng.uniform(-0.4, 0.15)))
    cv.outline(INK)
    emit(cv, "prop_plant")

    cv = Canvas(28, 28)
    base_shadow(cv, 2, 2, 25, 25)
    cv.rect(2, 2, 25, 25, c("wood"))
    cv.frame(2, 2, 25, 25, c("wood_d"))
    cv.line(3, 3, 24, 24, c("wood_d"))
    cv.line(24, 3, 3, 24, c("wood_d"))
    cv.noise(9, rng)
    cv.outline(INK)
    emit(cv, "prop_crate")

    cv = Canvas(24, 24)
    base_shadow(cv, 3, 3, 20, 20)
    cv.circle(12, 12, 9.4, (54, 92, 62, 255))
    cv.circle(12, 12, 9.4, (0, 0, 0, 0))
    cv.circle(12, 12, 9.2, (54, 92, 62, 255))
    cv.circle(10, 10, 5.5, (78, 122, 84, 255))
    cv.circle(12, 12, 3.0, (34, 60, 40, 255))
    cv.outline(INK)
    emit(cv, "prop_barrel")

    cv = Canvas(36, 48)
    base_shadow(cv, 2, 2, 33, 45)
    cv.rect(2, 2, 33, 45, (188, 180, 196, 255))
    cv.rect(2, 2, 33, 12, (222, 216, 226, 255))
    cv.rect(4, 4, 31, 10, c("cream"))
    cv.rect(2, 14, 33, 45, (128, 40, 72, 255))
    cv.rect(2, 14, 33, 15, (168, 60, 96, 255))
    cv.noise(6, rng)
    cv.outline(INK)
    emit(cv, "prop_bed")

    cv = Canvas(52, 24)
    base_shadow(cv, 2, 2, 47, 19)
    cv.rect(2, 2, 47, 19, (42, 34, 30, 255))
    cv.rect(2, 2, 47, 6, c("wood_l"))
    cv.rect(2, 6, 47, 7, c("wood_d"))
    cv.noise(7, rng)
    cv.outline(INK)
    emit(cv, "prop_counter")

    # ammo crate
    cv = Canvas(24, 20)
    base_shadow(cv, 2, 2, 21, 17)
    cv.rect(2, 2, 21, 17, (72, 86, 58, 255))
    cv.rect(2, 2, 21, 4, (104, 122, 82, 255))
    cv.rect(2, 15, 21, 17, (48, 60, 40, 255))
    for x in range(5, 19, 4):
        cv.rect(x, 6, x + 1, 13, c("amber"))
        cv.rect(x, 6, x + 1, 7, c("cream"))
    cv.frame(2, 2, 21, 17, (40, 50, 34, 255))
    cv.noise(6, rng)
    cv.outline(INK)
    emit(cv, "prop_ammo")

    # medkit
    cv = Canvas(20, 18)
    base_shadow(cv, 2, 2, 17, 15)
    cv.rect(2, 2, 17, 15, (226, 226, 232, 255))
    cv.rect(2, 2, 17, 3, c("white"))
    cv.rect(2, 14, 17, 15, (168, 168, 178, 255))
    cv.rect(8, 5, 11, 12, c("blood_l"))
    cv.rect(5, 7, 14, 10, c("blood_l"))
    cv.outline(INK)
    emit(cv, "prop_medkit")

    # exit marker
    cv = Canvas(32, 32)
    cv.rect(1, 1, 30, 30, c("lime", 60))
    cv.frame(1, 1, 30, 30, c("lime"))
    cv.frame(2, 2, 29, 29, c("lime", 120))
    for i in range(6):
        cv.line(16, 22 - i, 16, 10, c("lime"))
    cv.line(16, 8, 10, 15, c("lime"), 2)
    cv.line(16, 8, 22, 15, c("lime"), 2)
    emit(cv, "prop_exit")

    # door: horizontal slab, hinge on the left
    cv = Canvas(36, 12)
    cv.rect(0, 2, 35, 9, c("wood"))
    cv.rect(0, 2, 35, 3, c("wood_l"))
    cv.rect(0, 8, 35, 9, c("wood_d"))
    cv.rect(30, 4, 32, 7, c("amber"))
    cv.noise(8, rng)
    cv.outline(INK)
    emit(cv, "prop_door")


# ================================================================== ui =====
def gen_ui():
    emit(font5x7.build_atlas(), "ui_font")

    cv = Canvas(20, 20)
    col = c("cream", 235)
    cv.circle(10, 10, 6.6, (0, 0, 0, 0))
    for a in range(0, 360, 6):
        r = math.radians(a)
        cv.px(10 + math.cos(r) * 6.4, 10 + math.sin(r) * 6.4, col)
    cv.rect(9, 0, 10, 3, col)
    cv.rect(9, 16, 10, 19, col)
    cv.rect(0, 9, 3, 10, col)
    cv.rect(16, 9, 19, 10, col)
    cv.px(10, 10, c("magenta"))
    emit(cv, "ui_crosshair")

    # scope reticle, drawn at the aim point while scoped
    cv = Canvas(52, 52)
    col = c("blood_l", 235)
    faint = c("blood_l", 110)
    for a_deg in range(0, 360, 3):
        r = math.radians(a_deg)
        cv.px(26 + math.cos(r) * 22, 26 + math.sin(r) * 22, col)
    for a_deg in range(0, 360, 6):
        r = math.radians(a_deg)
        cv.px(26 + math.cos(r) * 15, 26 + math.sin(r) * 15, faint)
    for d in range(4, 23):
        if d % 3 != 0:
            continue
        cv.px(26 + d, 26, faint)
        cv.px(26 - d, 26, faint)
        cv.px(26, 26 + d, faint)
        cv.px(26, 26 - d, faint)
    cv.rect(26 - 26, 26, 26 - 23, 26, col)
    cv.rect(26 + 23, 26, 26 + 26, 26, col)
    cv.rect(26, 26 - 26, 26, 26 - 23, col)
    cv.rect(26, 26 + 23, 26, 26 + 26, col)
    cv.px(26, 26, c("cream", 250))
    emit(cv, "ui_scope")

    for i, kind in enumerate(("rooster", "owl", "tiger", "wolf", "pig", "rabbit")):
        cv = Canvas(28, 28)
        cv.circle(14, 15, 10.5, (0, 0, 0, 90))
        from pixelkit import Canvas as _C
        draw_mask(cv, 14, 14, 10.0, kind)
        cv.outline(INK, diagonal=True)
        emit(cv, "ui_mask_%d" % i)


def main():
    os.makedirs(OUT, exist_ok=True)
    gen_characters()
    gen_corpses()
    gen_weapons()
    gen_fx()
    gen_tiles()
    gen_props()
    gen_ui()
    n = len([f for f in os.listdir(OUT) if f.endswith(".png")])
    print("generated %d sprites -> %s" % (n, OUT))


if __name__ == "__main__":
    main()
