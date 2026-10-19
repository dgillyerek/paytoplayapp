import sys, numpy as np
from PIL import Image
from scipy import ndimage as ndi

def key_white(path, sat_tol=22, bg_tol=110, hard=10, shadow=True, grow=True):
    im = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    mx, mn = im.max(2), im.min(2)
    d = 255 - mn                      # distance from white (worst channel)
    sat = mx - mn
    cand = (d < bg_tol) & (sat < sat_tol)
    lab, n = ndi.label(cand)
    border = np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))
    bg = np.isin(lab, border[border > 0])
    if grow:
        c2 = ((d < 55) & (sat < 48)) | bg
        l2, _ = ndi.label(c2)
        ids = np.unique(l2[bg]); bg = np.isin(l2, ids[ids > 0])
    # alpha: object=1; bg region = shadow (black, alpha ~ darkness) or 0
    a = np.ones(d.shape, np.float32)
    if shadow:
        sh = np.clip((d - hard) / 255.0 * 1.6, 0, 0.55)
        a[bg] = sh[bg]
    else:
        a[bg] = 0
    rgb = im.copy()
    rgb[bg] = 0  # shadow colour: black-ish (premult fix below)
    # tint shadows a warm dark navy rather than pure black
    rgb[bg] = np.array([20, 22, 34], np.float32)
    # edge cleanup: object pixels within 2px of bg -> unmix from white
    dist = ndi.distance_transform_edt(~bg)
    edge = (~bg) & (dist <= 2.5)
    # estimate interior colour via grey dilation of 'd' in object
    dobj = np.where(bg, 0, d)
    dref = ndi.maximum_filter(dobj, size=7)
    ea = np.clip(d / np.maximum(dref, 1), 0, 1)
    ea = np.where(edge, ea, 1.0)
    a = np.where(bg, a, ea)
    # unmix: c = a*f + (1-a)*255  -> f = (c - (1-a)*255)/a
    m = edge & (ea < 0.999) & (ea > 0.02)
    f = (im - (1 - ea[..., None]) * 255) / np.maximum(ea[..., None], 1e-3)
    rgb[m] = np.clip(f[m], 0, 255)
    # light 1px alpha soften on outer object edge
    out = np.dstack([rgb, a * 255]).clip(0, 255).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')

def key_black(path):
    im = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    a = im.max(2) / 255.0
    a = np.clip((a - 0.04) / 0.96, 0, 1)
    rgb = np.clip(im / np.maximum(im.max(2, keepdims=True), 1) * 255, 0, 255)
    return Image.fromarray(np.dstack([rgb, a * 255]).astype(np.uint8), 'RGBA')

def trim(img, pad=4, thr=8):
    al = np.asarray(img)[..., 3]
    ys, xs = np.where(al > thr)
    box = (max(xs.min() - pad, 0), max(ys.min() - pad, 0), min(xs.max() + pad + 1, img.width), min(ys.max() + pad + 1, img.height))
    return img.crop(box)

def fit(img, maxw, maxh=None, square=False):
    maxh = maxh or maxw
    s = min(maxw / img.width, maxh / img.height)
    img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    if square:
        c = Image.new('RGBA', (maxw, maxh), (0, 0, 0, 0))
        c.paste(img, ((maxw - img.width) // 2, (maxh - img.height) // 2))
        return c
    return img
