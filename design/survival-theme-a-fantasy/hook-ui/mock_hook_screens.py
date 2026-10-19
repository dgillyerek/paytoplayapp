"""Mock composites (1080x1920) of the hook slice screens with the Theme A UI kit, laid out with the same
anchors as SurvivalPlayView / SurvivalHookView. Not a Unity capture: a layout + art check.
Run: python3 mock_hook_screens.py [out_dir]"""
import math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_lib import *

ASPECT = 1080 / 1920
K = "theme_a.hook."


def res_icon(res):
    return art(K + "icon." + res)


def bar(dst, r, frac, fill_key):
    sliced(dst, K + "ui.bar_frame", r, 0.5)
    inner = R(r.x0 + 5, r.y0 + 4, r.x1 - 5, r.y1 - 4)
    f = art(fill_key)
    if f is not None and frac > 0:
        full = f.resize((max(1, int(inner.w)), max(1, int(inner.h))), Image.LANCZOS)
        cut = full.crop((0, 0, max(1, int(inner.w * frac)), full.height))
        dst.alpha_composite(cut, (int(inner.x0), int(inner.y0)))


def glow(dst, r, colour, alpha):
    g = art(K + "fx.glow")
    fit(dst, g, r, 1.0, 0, colour + (int(255 * alpha),))


def button(dst, key, r, label, size, scale=0.5, ink=None, tint=None):
    sliced(dst, key, r, scale, tint)
    if label:
        if ink is None:
            ink = INK if key.endswith("button_gold") else (255, 255, 255)
        text(dst, r, label, size, ink, "mc", True, outline=ink != INK, od=2)


def wrap(s, size, width):
    f = font(size)
    words, lines, cur = s.split(" "), [], ""
    for wd in words:
        trial = (cur + " " + wd).strip()
        if f.getlength(trial) > width and cur:
            lines.append(cur)
            cur = wd
        else:
            cur = trial
    lines.append(cur)
    return [ln for ln in lines if ln]


def main_view():
    img = art("map.terrain").resize((W, H)).copy()
    # world pins (labels + dots); quarry has its painted sprite
    for p in PINS:
        x, yt = p["pin_x"] * W, p["pin_y_from_top"] * H
        if p["sprite_size"] > 0.01:
            a = art(p["content_key"])
            w = p["sprite_size"] * W
            h = w / (a.width / a.height)
            fit(img, a, R(x - w / 2, yt - h * 0.38, x + w / 2, yt + h * 0.62))
        lr = R(x - 0.09 * W, yt + 0.018 * H, x + 0.09 * W, yt + 0.055 * H)
        sliced(img, K + "ui.chip", lr, 0.32)
        text(img, lr, S("pin." + p["node_id"], p["node_type"]), 14, CREAM, outline=False)
    # top bar
    sliced(img, K + "ui.panel", hud("top_bar"), 0.5)
    text(img, hud("player_name"), S("hud.player_name"), 28, CREAM, "lc", outline=False)
    text(img, hud("player_power"), S("hud.player_power"), 18, MUTE, "lc", outline=False)
    for key, res, val in [("res_soft", "res.soft", "245.6K"), ("res_wood", "res.wood", "178.3K"), ("res_stone", "res.stone", "96.7K"), ("res_food", "res.food", "64.2K"), ("res_hard", "res.hard", "50")]:
        r = hud(key)
        sliced(img, K + "ui.chip", r, 0.45)
        fit(img, res_icon(res), r.box(-0.06, -0.12, 0.40, 1.12))
        text(img, r.box(0.36, 0, 0.96, 1), val, 21, CREAM, "mc", True)
    v = hud("vip")
    sliced(img, K + "ui.button_gold", v, 0.35)
    text(img, v, S("hud.vip"), 20, INK, "mc", True, outline=False)
    b = hud("banner")
    sliced(img, K + "ui.header", b, 0.42)
    text(img, b, S("theme_a.map.label", S("hud.banner")), 22, CREAM, outline=False)
    e = hud("events")
    sliced(img, K + "ui.panel", e, 0.5)
    yy = e.y0 + 16
    for line in S("hud.events_body").split("\n"):
        text(img, R(e.x0 + 18, yy, e.x1, yy + 24), line, 18, CREAM, "lc", outline=False)
        yy += 24
    hc = hud("hero_card")
    sliced(img, K + "ui.panel", hc, 0.45)
    fit(img, art("theme_a.hero.knight_01"), hc.box(0.04, 0.08, 0.28, 0.92))
    text(img, hc.box(0.32, 0.45, 0.98, 0.92), S("hud.hero_name"), 22, CREAM, "lc", outline=False)
    text(img, hc.box(0.32, 0.08, 0.98, 0.48), S("hud.hero_status"), 16, (115, 217, 140), "lc", outline=False)
    n = hud("nav")
    sliced(img, K + "ui.panel", n, 0.5)
    for i, key in enumerate(["nav.base", "nav.heroes", "nav.alliance", "nav.world", "nav.bag"]):
        r = n.box(i / 5 + 0.01, 0.12, (i + 1) / 5 - 0.01, 0.88)
        act = key == "nav.world"
        button(img, K + ("ui.button_gold" if act else "ui.button_blue"), r, S(key, key), 20, 0.4, INK if act else (237, 230, 209))
    # hook: quest tracker
    q = hud("quest_tracker")
    sliced(img, K + "ui.scroll", q, 0.8)
    text(img, q.box(0.11, 0.68, 0.74, 0.90), S("quest.tracker.title") + "  1/3", 17, PTITLE, "lc", True, outline=False)
    text(img, q.box(0.11, 0.34, 0.74, 0.68), S("quest.starter.collect.title"), 17, PINK, "lt", outline=False)
    bar(img, q.box(0.10, 0.13, 0.56, 0.30), 0.0, K + "ui.bar_fill_gold")
    text(img, q.box(0.58, 0.10, 0.74, 0.32), "0/1", 16, PINK, "lc", True, outline=False)
    fit(img, art(K + "icon.chest_closed"), q.box(0.74, 0.16, 0.92, 0.84), 0.55)
    # gem shop button + FREE
    g = hud("gem_shop")
    sliced(img, K + "ui.button_blue", g, 0.42)
    fit(img, art("theme_a.store.icon"), g.box(-0.04, -0.25, 0.32, 1.25))
    text(img, g.box(0.31, 0, 1, 1), S("store.gem_shop.button"), 21, CREAM, "lc", True)
    glow(img, g.box(0.60, 0.05, 1.16, 1.80), (140, 255, 115), 0.9)
    fit(img, art(K + "ui.badge_free"), g.box(0.68, 0.30, 1.08, 1.55), rot=6)
    text(img, g.box(0.68, 0.30, 1.08, 1.55), S("hook.free"), 17, (255, 255, 255), "mc", True)
    # base strip
    strip = hud("base_strip")
    cards = [
        ("base.plot.hq_01", None, "Lv 1", "2K Gold  1.5K Stone  1.2K Wood", 0.0, "ui.bar_fill_blue", ("ui.button_gold", S("hook.upgrade")), None, None),
        ("base.plot.farm_01", "res.food", "Lv 1", "600 / 600", 1.0, "ui.bar_fill_gold", ("ui.button_gold", S("hook.upgrade")), "+600", None),
        ("base.plot.lumber_01", "res.wood", "Lv 1", S("hook.upgrading") + "  02:41", 0.18, "ui.bar_fill_blue", ("ui.button_blue", S("hook.speedup.title")), None, None),
    ]
    for i, (pid, res, lv, status, frac, fillk, act, bubble, badge) in enumerate(cards):
        c = strip.box(i / 3 + 0.006, 0, (i + 1) / 3 - 0.006, 1)
        sliced(img, K + "ui.panel", c, 0.55)
        glow(img, c.box(-0.04, 0.18, 0.46, 1.12), (255, 219, 115), 0.35)
        fit(img, art(K + "plot." + pid.split("base.plot.")[1]), c.box(-0.02, 0.16, 0.44, 1.16))
        text(img, c.box(0.43, 0.74, 0.80, 0.94), S("hook.plot." + pid), 22, CREAM, "lc")
        text(img, c.box(0.70, 0.74, 0.94, 0.94), lv, 18, REWARD, "rc")
        sx = 0.43
        if res:
            fit(img, res_icon(res), c.box(0.42, 0.50, 0.53, 0.74))
            sx = 0.54
        sr = c.box(sx, 0.50, 0.95, 0.74)
        lines = wrap(status, 15, sr.w)
        for li, line in enumerate(lines):
            text(img, R(sr.x0, sr.y0 + sr.h / 2 + (li - (len(lines) - 1) / 2) * 17 - 9, sr.x1, sr.y0 + sr.h / 2 + (li - (len(lines) - 1) / 2) * 17 + 9), line, 15, CREAM, "lc", od=1)
        bar(img, c.box(0.43, 0.36, 0.94, 0.49), frac, K + fillk)
        button(img, K + act[0], c.box(0.42, 0.05, 0.95, 0.34), act[1], 18, 0.42)
        if bubble:
            br = c.box(0.02, 0.86, 0.40, 1.14)
            sliced(img, K + "ui.chip", br, 0.4)
            fit(img, res_icon(res), br.box(-0.08, -0.25, 0.40, 1.25))
            text(img, br.box(0.32, 0, 1, 1), bubble, 18, REWARD, "mc")
    for slot, label in [("debug_gift", S("debug.gift", "DEBUG: Gift now (G)")), ("debug_skip", S("debug.skip", "DEBUG: +5 min (T)"))]:
        button(img, K + "ui.button_red", hud(slot), label, 16, 0.34, CREAM)
    return img


def sheet(base, width, height, title):
    img = base.copy()
    dim(img, 199)
    p = SCREEN.center_box(0.5, 0.52, width, height)
    sliced(img, K + "ui.panel", p, 1.0)
    hw, hh = min(width * W * 0.82, 700), 118
    hr = R(p.x0 + p.w / 2 - hw / 2, p.y0 - hh / 2 + hh * 0.12, p.x0 + p.w / 2 + hw / 2, p.y0 + hh / 2 + hh * 0.12)
    sliced(img, K + "ui.header", hr, 0.75)
    text(img, hr, title, 34, REWARD, "mc", True, od=3)
    return img, p


def speedup_view(base):
    img, p = sheet(base, 0.84, 0.36, S("hook.speedup.title"))
    pid = "base.plot.lumber_01"
    fit(img, art(K + "plot.lumber_01"), p.box(0.05, 0.60, 0.30, 0.88))
    text(img, p.box(0.33, 0.78, 0.95, 0.87), S("hook.plot." + pid), 26, CREAM, "lc")
    fit(img, res_icon("res.speedup_m"), p.box(0.32, 0.645, 0.43, 0.775))
    text(img, p.box(0.45, 0.64, 0.95, 0.78), "02:41", 46, REWARD, "lc", True, od=3)
    bar(img, p.box(0.07, 0.53, 0.93, 0.60), 0.18, K + "ui.bar_fill_blue")
    r = p.box(0.08, 0.35, 0.92, 0.50)
    button(img, K + "ui.button_gold", r, S("hook.speedup.rush").replace("{0}", "3"), 26)
    fit(img, res_icon("res.hard"), r.box(0.03, 0.05, 0.15, 0.95))
    r = p.box(0.08, 0.19, 0.92, 0.33)
    button(img, K + "ui.button_blue", r, S("hook.speedup.items").replace("{0}", "15"), 22)
    fit(img, res_icon("res.speedup_m"), r.box(0.03, 0.05, 0.15, 0.95))
    text(img, p.box(0, 0.115, 1, 0.18), S("hook.speedup.free_hint"), 19, CREAM, od=1)
    button(img, K + "ui.button_red", p.box(0.33, 0.03, 0.67, 0.115), S("hook.close"), 20)
    return img


def shop_view(base):
    img, p = sheet(base, 0.92, 0.56, S("store.gem_shop.title"))
    bc = p.box(0.30, 0.855, 0.70, 0.905)
    sliced(img, K + "ui.chip", bc, 0.5)
    fit(img, res_icon("res.hard"), bc.box(0.02, -0.2, 0.22, 1.2))
    text(img, bc.box(0.20, 0, 0.98, 1), "50", 22, HARD, "mc", od=1)
    text(img, p.box(0, 0.82, 1, 0.85), S("store.sandbox"), 15, WARN, od=1)
    offers = [
        ("offer.free_gift", "free", None, None, S("offer.free_gift.ready"), S("hook.claim")),
        ("offer.gems_s", "sku", K + "icon.gems_tier_1", None, "+80 Gems", "$0.99"),
        ("offer.gems_m", "sku", K + "icon.gems_tier_3", "best_value", "+500 Gems", "$4.99"),
        ("offer.speedup_15", "currency", None, None, "+15 Speed-up (min)", "12 Gems"),
    ]
    rowH = 0.70 / 4
    for i, (oid, kind, tier, tag, sub, price) in enumerate(offers):
        top = 0.81 - i * rowH
        y0, y1 = top - rowH + 0.012, top
        if tag:
            pad = (y1 - y0) * 0.25
            glow(img, p.box(0, y0 - pad, 1, y1 + pad), (255, 209, 89), 0.45)
        row = p.box(0.04, y0, 0.96, y1)
        sliced(img, K + "ui.plate", row, 0.45)
        ic = row.box(0.01, -0.04, 0.22, 1.04)
        if kind == "free":
            fit(img, art(K + "icon.gift"), ic)
        elif tier:
            fit(img, art(tier), ic)
        else:
            fit(img, res_icon("res.speedup_m"), ic)
        text(img, row.box(0.24, 0.50, 0.64, 0.92), S(oid + ".title"), 24, CREAM, "lc", True)
        text(img, row.box(0.24, 0.10, 0.64, 0.50), sub, 19, FREE if kind == "free" else (HARD if tier else (122, 168, 255)), "lc", od=1)
        if tag:
            tr = row.box(0.24, 0.72, 0.76, 1.24)
            paste(img, art(K + "ui.ribbon_red"), tr)
            text(img, tr.box(0.15, 0.30, 0.85, 0.95), S("offer.tag." + tag), 18, (255, 255, 255), "mc", True, od=1)
        br = row.box(0.66, 0.16, 0.97, 0.84)
        if kind == "free":
            br2 = R(br.x0 - br.w * 0.03, br.y0 - br.h * 0.03, br.x1 + br.w * 0.03, br.y1 + br.h * 0.03)
            button(img, K + "ui.button_green", br2, price, 22)
            bb = br.box(0.76, 0.40, 1.14, 1.45)
            fit(img, art(K + "ui.badge_free"), bb, rot=8)
            text(img, bb, S("hook.free"), 14, (255, 255, 255), "mc", True, od=1)
        else:
            button(img, K + "ui.button_gold", br, price, 22)
    button(img, K + "ui.button_red", p.box(0.35, 0.025, 0.65, 0.085), S("hook.close"), 19)
    return img


def celebration_view(base):
    img = base.copy()
    dim(img, 199)
    cy = 0.55
    c = SCREEN.center_box(0.5, cy, 0, 0)
    cx, cyp = c.x0, c.y0
    rays = art(K + "fx.rays")
    fit(img, rays, R(cx - 750, cyp - 750, cx + 750, cyp + 750), 1.0, 12, (255, 224, 128, 191))
    fit(img, rays, R(cx - 475, cyp - 475, cx + 475, cyp + 475), 1.0, -20, (255, 242, 191, 140))
    glow(img, SCREEN.center_box(0.5, cy, 0.9 * 1.25, 0.9 * ASPECT * 1.25), (255, 217, 102), 0.7)
    chest = SCREEN.center_box(0.5, 0.53, 0.46, 0.46 * ASPECT)
    fit(img, art(K + "icon.chest_open"), chest)
    rnd = random.Random(3)
    cols = [(250, 209, 64), (242, 92, 107), (102, 204, 255), (115, 230, 115), (204, 128, 255), (255, 255, 255)]
    conf = art(K + "fx.confetti")
    for i in range(150):
        x = rnd.uniform(0.05, 0.95) * W
        y = rnd.uniform(0.12, 0.8) * H
        ln = rnd.uniform(22, 40)
        sz = (16, 16) if i % 4 == 0 else (ln * 0.45, ln)
        piece = tinted(conf, cols[i % 6]).resize((int(sz[0]), int(sz[1])))
        piece = piece.rotate(rnd.uniform(0, 360), expand=True, resample=Image.BICUBIC)
        img.alpha_composite(piece, (int(x), int(y)))
    sp = art(K + "fx.sparkle")
    for i in range(10):
        ang = i / 10 * math.pi * 2 + 0.4
        dist = 0.20 + 0.08 * ((i * 37) % 5) / 4
        px, py = 0.5 + math.cos(ang) * dist, 0.54 + math.sin(ang) * dist * ASPECT * 1.3
        s = (0.05 + 0.03 * (i % 3)) * (0.5 + 0.7 * abs(math.sin(i * 1.7)))
        fit(img, sp, SCREEN.center_box(px, py, s, s * ASPECT), 1.0, i * 20, (255, 242, 191))
    th = SCREEN.center_box(0.5, 0.765, 0.94, 0.085)
    paste(img, art(K + "ui.ribbon_blue"), th)
    text(img, th.box(0.12, 0.22, 0.88, 0.98), S("quest.claim.title"), 50, REWARD, "mc", True, od=3)
    rewards = [("res.soft", "+1,500 Gold"), ("res.hard", "+20 Gems")]
    for k, (res, label) in enumerate(rewards):
        rr = SCREEN.center_box(0.5, 0.36 - k * 0.058, 0.66, 0.05)
        sliced(img, K + "ui.chip", rr, 0.6)
        fit(img, res_icon(res), rr.box(0.02, -0.35, 0.24, 1.35))
        text(img, rr.box(0.27, 0, 1, 1), label, 36, REWARD, "lc", True, od=2)
    text(img, SCREEN.center_box(0.5, 0.12, 0.8, 0.04), S("hook.continue"), 24, CREAM)
    # coin-fly preview toward the gold chip
    target = hud("res_soft")
    tx, ty = (target.x0 + target.x1) / 2, (target.y0 + target.y1) / 2
    sx, sy = W * 0.36, H * (1 - 0.36)
    icon = res_icon("res.soft")
    for i in range(7):
        u = 0.15 + i * 0.12
        ctrl = (sx + (tx - sx) * 0.35, sy + (ty - sy) * 0.35 - 0.12 * H)
        e = u * u
        px = (1 - e) ** 2 * sx + 2 * (1 - e) * e * ctrl[0] + e * e * tx
        py = (1 - e) ** 2 * sy + 2 * (1 - e) * e * ctrl[1] + e * e * ty
        sz = 0.06 * W * (1.2 - 0.6 * u)
        fit(img, icon, R(px - sz / 2, py - sz / 2, px + sz / 2, py + sz / 2), 0.95)
    return img


def contact(paths, out):
    ims = [Image.open(p).convert("RGBA") for p in paths]
    tw, th = 540, 960
    pad, lab = 24, 56
    sheet = Image.new("RGBA", (pad + (tw + pad) * len(ims), pad + lab + th + pad), (18, 22, 34, 255))
    names = ["Main view", "Speed Up sheet", "Gem Shop", "Celebration"]
    d = ImageDraw.Draw(sheet)
    for i, im in enumerate(ims):
        x = pad + i * (tw + pad)
        d.text((x + tw / 2, pad + lab / 2), names[i], font=font(30, True), fill=(237, 214, 115), anchor="mm")
        sheet.alpha_composite(im.resize((tw, th), Image.LANCZOS), (x, pad + lab))
    sheet.convert("RGB").save(out)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "/workspace/world_quest"
    os.makedirs(out, exist_ok=True)
    base = main_view()
    shots = {
        "hook_mock_main_1080x1920.png": base,
        "hook_mock_speedup_1080x1920.png": speedup_view(base),
        "hook_mock_shop_1080x1920.png": shop_view(base),
        "hook_mock_celebration_1080x1920.png": celebration_view(base),
    }
    paths = []
    for name, im in shots.items():
        path = os.path.join(out, name)
        im.convert("RGB").save(path)
        paths.append(path)
        print("wrote", path)
    contact(paths, os.path.join(out, "hook_visual_contact_sheet.png"))
    print("wrote", os.path.join(out, "hook_visual_contact_sheet.png"))
