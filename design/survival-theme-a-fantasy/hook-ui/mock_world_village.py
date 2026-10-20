"""Mock composites (1080x1920) of the Survival Play scene's two screens - the World (kingdom map) view and the
Village view at each castle tier - built from the real Theme A pack art and the map/world_layout.json +
map/village_layout.json the Unity views read, laid out with the same sizes/pivots as SurvivalWorldView /
SurvivalVillageView / SurvivalHookView. Not a Unity capture: a layout + art check.
Run: python3 mock_world_village.py [out_dir]"""
import json, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_lib import *
from mock_hook_screens import main_view, glow, button, painted_button, bar, sheet, nav_slot, K

WORLD = json.load(open(os.path.join(PACK, "map", "world_layout.json")))
VILLAGE = json.load(open(os.path.join(PACK, "map", "village_layout.json")))
BLUE, RED, GREEN = (115, 191, 255), (240, 96, 80), (125, 220, 120)


def tile(canvas, key, alpha=1.0):
    t = art(key)
    for y in range(0, canvas.height, t.height):
        for x in range(0, canvas.width, t.width):
            canvas.alpha_composite(t, (x, y))


def point(dst, img, x, yt, w, h, pivot_y, alpha=1.0, tint=None):
    """Unity Point(): (x, y-from-top) with a bottom-up pivot, preserveAspect inside w x h."""
    r = R(x - w / 2, yt - h * (1 - pivot_y), x + w / 2, yt + h * pivot_y)
    fit(dst, img, r, alpha, 0, tint)
    return r


def road(dst, a, b, width):
    """Dirt strip a->b (top-down px) with a darker soft edge, like SurvivalVillageView.Road."""
    (ax, ay), (bx, by) = a, b
    length = int(math.hypot(bx - ax, by - ay))
    ang = math.degrees(math.atan2(-(by - ay), bx - ax))
    for wmul, tint in [(1.18, (60, 42, 24, 150)), (1.0, None)]:
        w = int(width * wmul)
        strip = Image.new("RGBA", (length, w), (0, 0, 0, 0))
        tile(strip, "theme_a.map.tile.dirt")
        if tint:
            strip = tinted(strip, tint)
        mask = Image.new("L", (length, w), 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, length - 1, w - 1), radius=w // 2, fill=255)
        strip.putalpha(Image.composite(strip.getchannel("A"), Image.new("L", strip.size, 0), mask))
        rot = strip.rotate(ang, expand=True, resample=Image.BICUBIC)
        cx, cy = (ax + bx) / 2, (ay + by) / 2
        dst.alpha_composite(rot, (int(cx - rot.width / 2), int(cy - rot.height / 2)))


def dashes(dst, a, b, colour, width=12, dash=26, gap=22):
    d = ImageDraw.Draw(dst)
    (ax, ay), (bx, by) = a, b
    L = math.hypot(bx - ax, by - ay)
    ux, uy = (bx - ax) / L, (by - ay) / L
    s = 0.0
    while s < L:
        e = min(L, s + dash)
        d.line((ax + ux * s, ay + uy * s, ax + ux * e, ay + uy * e), fill=colour + (230,), width=width)
        s += dash + gap


def plate(dst, key, cx, ytop, w, h, name, sub, ink, size=24):
    r = R(cx - w / 2, ytop, cx + w / 2, ytop + h)
    sliced(dst, key, r, 0.38)
    outline = ink != INK
    if sub:
        text(dst, r.box(0, 0.45, 1, 1), name, size, ink, "mc", True, outline=outline)
        text(dst, r.box(0, 0.0, 1, 0.52), sub, size - 4, ink if not outline else REWARD, "mc", True, outline=outline)
    else:
        text(dst, r, name, size, ink, "mc", True, outline=outline)


def compact(v):
    return f"{v / 1e6:.1f}M" if v >= 1e6 else f"{v / 1e3:.1f}K" if v >= 1e3 else str(v)


# ---------------------------------------------------------------- world


def world_map():
    wd = WORLD["world"]
    Wm, Hm = wd["width"], wd["height"]
    img = Image.new("RGBA", (Wm, Hm), (77, 92, 46, 255))
    tile(img, "theme_a.map.tile.meadow")
    hd = WORLD["heart"]
    hx0 = hd["x"] * Wm - hd["width"] / 2
    hy0 = hd["y"] * Hm - hd["height"] / 2
    paste(img, art(hd["art"]), R(hx0, hy0, hx0 + hd["width"], hy0 + hd["height"]))
    pos = {}
    ents = []
    for v in WORLD["villages"]:
        ents.append(dict(v, k="village"))
    for c in WORLD["camps"]:
        ents.append(dict(c, k="camp"))
    for r in WORLD["resources"]:
        ents.append(dict(r, k="res"))
    for e in ents:
        pos[e["id"]] = (e["x"] * Wm, e["y"] * Hm)
    for p in PINS:  # catalog world nodes on the painted crusade map
        x, y = hx0 + p["pin_x"] * hd["width"], hy0 + p["pin_y_from_top"] * hd["height"]
        pos[p["node_id"]] = (x, y)
        ents.append(dict(id=p["node_id"], k="landmark", key=p["content_key"], name=S("pin." + p["node_id"], p["node_id"]), type=p["node_type"]))
    player = pos["village.player"]
    road(img, player, pos["world.node.home_01"], 70)
    # marches: dashed path + marker part way along
    for i, m in enumerate(WORLD["marches"]):
        a, b = pos.get(m["owner"]), pos.get(m["target"])
        if a is None or b is None:
            continue
        kind = next(v["kind"] for v in WORLD["villages"] if v["id"] == m["owner"])
        col = GREEN if kind == "ally" else RED
        dashes(img, a, b, col)
    player_marches = [("camp.goblin_01", 0.55, "00:12"), ("res.gold_01", 0.3, "00:21")]
    for tgt, _, _ in player_marches:
        dashes(img, player, pos[tgt], BLUE, 14)
    for p in sorted(WORLD["props"], key=lambda p: p["y"]):
        size = (440 if p["type"] == "mountains" else 360 if p["type"] == "lake" else 340) * p["scale"]
        point(img, art("theme_a.map.prop." + p["type"]), p["x"] * Wm, p["y"] * Hm, size, size, 0.3)
    labels = []
    for e in sorted(ents, key=lambda e: pos[e["id"]][1]):
        x, y = pos[e["id"]]
        if e["k"] == "village":
            size = 330 if e["kind"] == "player" else 270
            key = "theme_a.world.village." + e["kind"]
            if e["kind"] == "player":
                glow(img, R(x - size * 0.72, y - size * 0.5, x + size * 0.72, y + size * 0.5), (255, 219, 107), 0.55)
                ImageDraw.Draw(img).ellipse((x - size * 0.52, y - size * 0.14, x + size * 0.52, y + size * 0.14), outline=(115, 194, 255, 230), width=8)
            pk = {"player": K + "ui.button_gold", "ally": K + "ui.button_green"}.get(e["kind"], K + "ui.chip")
            nm = S("hud.player_name", e["name"]) if e["kind"] == "player" else e["name"]
            labels.append((pk, x, y + size * 0.30, nm, S("world.card.power", "Power {0}").replace("{0}", compact(e["power"])), INK if e["kind"] == "player" else CREAM))
        elif e["k"] == "camp":
            size = 330 if e["type"] == "dragon" else 270 if e["level"] >= 10 else 240
            key = "theme_a.world.camp." + e["type"]
            labels.append((K + "ui.ribbon_red", x, y + size * 0.30, S("world.camp." + e["type"], e["type"]), "Lv " + str(e["level"]), CREAM))
        elif e["k"] == "res":
            size = 220
            key = "theme_a.world.node." + e["type"]
            labels.append((K + "ui.chip", x, y + size * 0.30, S("world.node." + e["type"], e["type"]), compact(e["amount"]), CREAM))
        else:
            size = 320 if e["type"].endswith("fight") else 260
            key = e["key"]
            if key.endswith("_map") and art(key[:-4]) is not None:
                key = key[:-4]
            labels.append((K + "ui.chip", x, y + size * 0.30, e["name"], "", CREAM))
        point(img, art(key), x, y, size, size, 0.28)
    # march markers on top of entities
    for i, m in enumerate(WORLD["marches"]):
        a, b = pos.get(m["owner"]), pos.get(m["target"])
        if a is None or b is None:
            continue
        kind = next(v["kind"] for v in WORLD["villages"] if v["id"] == m["owner"])
        t = (0.25 + 0.17 * i) % 0.9 + 0.05
        x, y = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
        key = "theme_a.world.march.player" if kind == "ally" else "theme_a.world.march.rival"
        point(img, art(key), x, y, 140, 140, 0.2, tint=(209, 255, 209) if kind == "ally" else None)
    for tgt, t, eta in player_marches:
        b = pos[tgt]
        x, y = player[0] + (b[0] - player[0]) * t, player[1] + (b[1] - player[1]) * t
        r = point(img, art("theme_a.world.march.player"), x, y, 170, 170, 0.2)
        cr = R(r.x0 + 0.08 * r.w, r.y1 - 1.26 * r.h, r.x0 + 0.92 * r.w, r.y1 - 0.98 * r.h)
        sliced(img, K + "ui.chip", cr, 0.3)
        text(img, cr, eta, 22, CREAM, "mc", True)
    for pk, x, y, nm, sub, ink in labels:
        plate(img, pk, x, y, 290, 78 if sub else 52, nm, sub, ink)
    return img, pos


def camera_view(full, zoom, cx, cy):
    """Crop what a 1080x1920 viewport shows at `zoom` centred on (cx, cy) top-down, clamped like MapCamera."""
    vw, vh = W / zoom, H / zoom
    x0 = min(max(cx - vw / 2, 0), full.width - vw)
    y0 = min(max(cy - vh / 2, 0), full.height - vh)
    return full.crop((int(x0), int(y0), int(x0 + vw), int(y0 + vh))).resize((W, H), Image.LANCZOS)


def world_card(img, pos):
    c = hud("map_card")
    sliced(img, K + "ui.panel", c, 0.55)
    fit(img, art("theme_a.world.camp.goblin"), c.box(0.02, 0.08, 0.26, 0.96))
    text(img, c.box(0.28, 0.70, 0.98, 0.94), S("world.camp.goblin", "Goblin Warband"), 34, REWARD, "lc", True)
    text(img, c.box(0.28, 0.50, 0.98, 0.70), S("world.card.camp", "Enemy camp \u00b7 Lv {0}").replace("{0}", "3"), 24, CREAM, "lc")
    text(img, c.box(0.28, 0.32, 0.98, 0.50), S("world.card.camp_power", "Troops {0} \u00b7 Loot {1} Gold").replace("{0}", "4.2K").replace("{1}", "1.2K"), 22, MUTE, "lc", outline=False)
    button(img, K + "ui.button_red", c.box(0.28, 0.05, 0.62, 0.30), S("world.action.attack", "Attack"), 26, 0.42, CREAM)
    button(img, K + "ui.button_blue", c.box(0.64, 0.05, 0.96, 0.30), S("world.action.scout", "Scout"), 26, 0.42, CREAM)


# ---------------------------------------------------------------- village

LEVELS = {"base.plot.farm_01": 3, "base.plot.lumber_01": 2, "base.plot.farm_02": 1, "base.plot.lumber_02": 4, "base.plot.forge_01": 2}


def tier_for(level):
    tiers = sorted(VILLAGE["castle_tiers"], key=lambda t: t["min_level"])
    cur = tiers[0]
    for t in tiers:
        if level >= t["min_level"]:
            cur = t
    return cur


PROP_SIZE = {"bushes": 200, "tree": 280, "rocks": 260, "field": 340}
SPOTS = {sp["id"]: sp for sp in VILLAGE["build_spots"]}
BUILD_TYPES = {"farm": K + "build.farm", "lumber": K + "build.lumber", "quarry": K + "build.quarry", "forge": K + "build.forge"}


def badge(img, host, lv):
    br = host.box(0.0, 0.62, 0.26, 0.92)
    s_ = min(br.w, br.h)
    br = R(br.x0, br.y0 + (br.h - s_) / 2, br.x0 + s_, br.y0 + (br.h + s_) / 2)
    fit(img, art("theme_a.ui.level_badge"), br)
    text(img, br.box(0, 0, 1, 0.86), str(lv), 40, (255, 255, 255), "mc", True, od=3)


def name_plate(img, x, host, name, pw=260):
    pr = R(x - pw / 2, host.y1 - 0.05 * host.h - 29, x + pw / 2, host.y1 - 0.05 * host.h + 29)
    sliced(img, K + "ui.chip", pr, 0.4)
    text(img, pr, name, 26, CREAM, "mc", True)
    return pr


def village_map(castle_level, built=(), placing=False, glow_t=0.0):
    """built: [(type, spot_id, level, timer_text or None)]; placing: light up the free build spots."""
    vd = VILLAGE["village"]
    Wm, Hm = vd["width"], vd["height"]
    img = Image.new("RGBA", (Wm, Hm), (97, 140, 61, 255))
    tile(img, "theme_a.village.tile.lawn")
    taken = {sid for _, sid, _, _ in built}
    items = []
    # No roads in the village (Derek, PR #74): grass, decor and buildings only.
    for p in VILLAGE["props"]:
        items.append((p["y"], "prop", p))
    for spot in VILLAGE["plots"]:
        items.append((spot["y"], "plot", spot))
    for b in built:
        items.append((SPOTS[b[1]]["y"], "built", b))
    if placing:
        for sid, sp in SPOTS.items():
            if sid not in taken:
                items.append((sp["y"], "spot", sp))
    items.sort(key=lambda it: it[0])
    for _, kind, it in items:
        if kind == "prop":
            p = it
            key = "theme_a.village.prop." + p["type"] if art("theme_a.village.prop." + p["type"]) is not None else "theme_a.map.prop." + p["type"]
            size = PROP_SIZE.get(p["type"], 380) * p["scale"]
            point(img, art(key), p["x"] * Wm, p["y"] * Hm, size, size, 0.35)
        elif kind == "spot":
            sp = it
            px = sp["size"] * Wm
            x, y = sp["x"] * Wm, sp["y"] * Hm
            r = R(x - px / 2, y - px * 0.375, x + px / 2, y + px * 0.375)
            glow(img, r.box(-0.18, -0.25, 1.18, 1.25), (255, 219, 89), 0.75)
            fit(img, art("theme_a.village.plot_empty"), r)
            fit(img, art("theme_a.ui.icon_build"), r.box(0.30, 0.30, 0.70, 0.95))
        elif kind == "built":
            typ, sid, lv, timer = it
            sp = SPOTS[sid]
            px = sp["size"] * 1.06 * Wm
            x, y = sp["x"] * Wm, sp["y"] * Hm
            host = R(x - px / 2, y - px * 0.68, x + px / 2, y + px * 0.32)
            fit(img, art(BUILD_TYPES[typ]), host, 0.55 if timer else 1.0)
            pr = name_plate(img, x, host, S("build.type." + typ, typ))
            if timer:
                fit(img, art("theme_a.ui.icon_build"), host.box(0.34, 0.62, 0.66, 0.98))
                tr = R(x - 140, pr.y1 + 6, x + 140, pr.y1 + 70)
                text(img, tr.box(-0.2, 0.45, 1.2, 1.1), S("build.building", "Building") + " " + timer, 22, CREAM, "mc", True)
                bar(img, tr.box(0, 0, 1, 0.5), 0.35, K + "ui.bar_fill_green")
                fit(img, art(K + "ui.badge_free"), host.box(0.72, 0.66, 1.0, 0.98))
                text(img, host.box(0.72, 0.66, 1.0, 0.98), S("hook.free", "FREE"), 22, (255, 255, 255), "mc", True)
            else:
                badge(img, host, lv)
        else:
            spot = it
            pid = spot["plot"]
            x, y = spot["x"] * Wm, spot["y"] * Hm
            castle = pid == "base.plot.hq_01"
            if castle:
                t = tier_for(castle_level)
                px = spot["size"] * Wm * t["scale"]
                key, lv = t["art"], castle_level
            else:
                px = spot["size"] * Wm
                key, lv = K + "plot." + pid.split("base.plot.")[1], min(LEVELS.get(pid, 1), castle_level)
            host = R(x - px / 2, y - px * 0.68, x + px / 2, y + px * 0.32)  # pivot (0.5, 0.32)
            glow(img, host.box(-0.1, -0.05, 1.1, 1.05), (255, 219, 115), 0.18)
            fit(img, art(key), host)
            name = S("hook.plot." + pid, pid)
            if castle:
                name = f"{name}  Lv {lv}"
            name_plate(img, x, host, name, 330 if castle else 260)
            if not castle:
                badge(img, host, lv)
                if pid == "base.plot.farm_01":
                    bb = host.box(0.22, 0.88, 0.78, 1.08)
                    sliced(img, K + "ui.chip", bb, 0.4)
                    fit(img, art(K + "icon.res.food"), bb.box(-0.08, -0.25, 0.40, 1.25))
                    text(img, bb.box(0.32, 0, 1, 1), "+600", 26, REWARD, "mc", True)
                if pid == "base.plot.lumber_02":
                    fit(img, art("theme_a.ui.icon_upgrade"), host.box(0.74, 0.62, 0.98, 0.92))
    return img


def village_view(level):
    vd = VILLAGE["village"]
    full = village_map(level)
    hq = VILLAGE["plots"][0]
    cx, cy = hq["x"] * vd["width"], hq["y"] * vd["height"] + 400
    shot = camera_view(full, vd["start_zoom"], cx, cy)
    return main_view(shot, None, ("theme_a.ui.icon_world", S("view.toggle.world", "World")), S("view.banner.village"), build=True)


def tier_strip(out):
    """All 20 castle stages in order (2 rows x 10) at their footprint scale, on the village lawn."""
    tiers = sorted(VILLAGE["castle_tiers"], key=lambda t: t["min_level"])
    per_row, cw, ch = 5, 520, 600
    rows = (len(tiers) + per_row - 1) // per_row
    img = Image.new("RGBA", (cw * per_row, ch * rows), (18, 22, 34, 255))
    lawn = Image.new("RGBA", img.size, (0, 0, 0, 0))
    tile(lawn, "theme_a.village.tile.lawn")
    img.alpha_composite(tinted(lawn, (255, 255, 255, 140)))
    base = cw * 0.92 / max(t["scale"] for t in tiers)
    anchors = {5, 10, 15, 20}
    for i, t in enumerate(tiers):
        cx, row = (i % per_row) * cw + cw / 2, i // per_row
        px = base * t["scale"]
        foot = row * ch + ch - 92
        r = R(cx - px / 2, foot - px * 0.80, cx + px / 2, foot + px * 0.20)
        fit(img, art(t["art"]), r)
        lab = f"Lv {t['min_level']}" + ("  (anchor)" if t["min_level"] in anchors else "")
        text(img, R(cx - cw / 2, row * ch + ch - 70, cx + cw / 2, row * ch + ch - 38), lab, 28, REWARD if t["min_level"] in anchors else CREAM, "mc", True)
        text(img, R(cx - cw / 2, row * ch + ch - 38, cx + cw / 2, row * ch + ch - 10), f"x{t['scale']:.2f}", 22, MUTE, "mc", True)
    img.convert("RGB").save(out)
    return out


def contact(frames, strip_path, out):
    tw, th, pad, lab = 432, 768, 22, 52
    n = len(frames)
    sw = pad + (tw + pad) * n
    strip = Image.open(strip_path).convert("RGBA")
    sh_strip = int(strip.height * (sw - 2 * pad) / strip.width)
    sheet = Image.new("RGBA", (sw, pad + lab + th + pad + lab + sh_strip + pad), (18, 22, 34, 255))
    d = ImageDraw.Draw(sheet)
    for i, (name, path) in enumerate(frames):
        x = pad + i * (tw + pad)
        d.text((x + tw / 2, pad + lab / 2), name, font=font(24, True), fill=(237, 214, 115), anchor="mm")
        sheet.alpha_composite(Image.open(path).convert("RGBA").resize((tw, th), Image.LANCZOS), (x, pad + lab))
    y = pad + lab + th + pad
    d.text((sw / 2, y + lab / 2), "All 20 castle stages in order (village_layout.json castle_tiers, real art at footprint scale)", font=font(26, True), fill=(237, 214, 115), anchor="mm")
    sheet.alpha_composite(strip.resize((sw - 2 * pad, sh_strip), Image.LANCZOS), (pad, y + lab))
    sheet.convert("RGB").save(out)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "/workspace/world_quest"
    os.makedirs(out, exist_ok=True)
    wfull, pos = world_map()
    wd = WORLD["world"]
    px, py = pos["village.player"]
    frames = []
    toggle_v = ("theme_a.village.castle.tier_10", S("view.toggle.village", "Village"))
    w1 = main_view(camera_view(wfull, wd["start_zoom"], px + 180, py + 120), "nav.world", toggle_v, S("view.banner.world"))
    w2 = camera_view(wfull, 0.62, px + 300, py + 200)
    w2 = main_view(w2, "nav.world", toggle_v, S("view.banner.world"))
    world_card(w2, pos)
    wfull.convert("RGB").resize((wfull.width // 3, wfull.height // 3), Image.LANCZOS).save(os.path.join(out, "world_full_map_third.png"))
    for name, im in [("World view (start zoom)", w1), ("World - zoomed out, camp card", w2)]:
        p = os.path.join(out, "mock_" + name.split()[0].lower() + ("_card" if "card" in name else "") + "_1080x1920.png")
        im.convert("RGB").save(p)
        frames.append((name, p))
    for lv in (1, 5, 12, 20):
        p = os.path.join(out, f"mock_village_castle_lv{lv}_1080x1920.png")
        village_view(lv).convert("RGB").save(p)
        frames.append((f"Village (no roads) - Castle Lv {lv}", p))
    strip = tier_strip(os.path.join(out, "castle_stages_1_to_20.png"))
    contact(frames, strip, os.path.join(out, "world_village_contact_sheet.png"))
    for _, p in frames:
        print("wrote", p)
    print("wrote", os.path.join(out, "world_village_contact_sheet.png"))
