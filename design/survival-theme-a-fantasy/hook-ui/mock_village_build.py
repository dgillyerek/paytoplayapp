"""Mock of PR #74 round 3: the painted bottom bar (Heroes / Alliance / Bag, room for Battle), the bigger village
(3600x4800, decor, 17 free build spots) and the Build flow (Build button -> menu -> glowing spots -> construction ->
Lv 1 building), from the real Theme A pack art + village_layout.json + base_economy.json buildables.
Run: python3 mock_village_build.py [out_dir]  -> village_build_and_bar.png"""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_lib import *
from mock_hook_screens import main_view, glow, button, painted_button, bar, sheet, nav_slot, K
from mock_world_village import VILLAGE, SPOTS, village_map, camera_view, tier_for

ECON = json.load(open(os.path.join(os.path.dirname(PACK), "..", "Survival", "Resources", "Survival", "base_economy.json")))
TYPES = ECON["buildables"]
CASTLE = 6
BUILT = [("farm", "village.spot_01", 2, None), ("lumber", "village.spot_02", 1, None)]


def compact(v):
    return f"{v / 1e3:.1f}K".replace(".0K", "K") if v >= 1000 else str(v)


def cost_for(t, n):
    k = t.get("cost_growth", 1.6) ** n
    return {r: int(round(a * k / 10.0) * 10) for r, a in t["build_cost"].items()}


def secs_for(t, n):
    return int(round(t["build_seconds"] * t.get("time_growth", 1.5) ** n))


def clock(s):
    return f"{s // 3600}h {(s % 3600) // 60:02d}m" if s >= 3600 else f"{s // 60}:{s % 60:02d}"


def village_shot(built, placing=False, zoom=None, focus=None):
    vd = VILLAGE["village"]
    full = village_map(CASTLE, built, placing)
    hq = VILLAGE["plots"][0]
    cx, cy = focus or (hq["x"] * vd["width"], hq["y"] * vd["height"] + 400)
    return full, camera_view(full, zoom or vd["start_zoom"], cx, cy)


def hud_village(shot, build=True, pressed=None):
    return main_view(shot, None, ("theme_a.ui.icon_world", S("view.toggle.world", "World")), S("view.banner.village"), build=build, pressed=pressed)


def build_menu(base):
    img, p = sheet(base, 0.94, 0.66, S("build.title", "Build"))
    counts = {"farm": 1, "lumber": 1, "quarry": 0, "forge": 0}
    for i, t in enumerate(TYPES):
        typ = t["type"]
        y1 = 0.84 - i * 0.165
        row = p.box(0.04, y1 - 0.165 + 0.012, 0.96, y1)
        sliced(img, K + "ui.plate", row, 0.5)
        fit(img, art(K + "build." + typ), row.box(0.01, 0.04, 0.22, 0.98))
        text(img, row.box(0.24, 0.62, 0.68, 0.94), S("build.type." + typ, typ), 28, REWARD, "lc", True)
        text(img, row.box(0.24, 0.44, 0.70, 0.64), S("build.desc." + typ, ""), 18, MUTE, "lc", outline=False)
        n = counts[typ]
        c = cost_for(t, n)
        line = "  ".join(f"{compact(v)} {S(r + '.name', r)}" for r, v in c.items()) + "   \u23f1 " + clock(secs_for(t, n))
        text(img, row.box(0.24, 0.24, 0.70, 0.44), line, 18, CREAM, "lc")
        mx = sum(1 for lv in t["unlock_at_castle"] if lv <= CASTLE)
        nxt = min([lv for lv in t["unlock_at_castle"] if lv > CASTLE], default=None)
        cl = S("build.count", "Built {0}/{1}").replace("{0}", str(n)).replace("{1}", str(mx))
        if nxt:
            cl += "   " + S("build.more_at", "More at Castle Lv {0}").replace("{0}", str(nxt))
        text(img, row.box(0.24, 0.04, 0.70, 0.24), cl, 18, CREAM, "lc")
        ok = n < mx
        lab = S("build.button", "Build") if ok else S("build.more_at", "More at Castle Lv {0}").replace("{0}", str(nxt))
        button(img, K + "ui.button_gold", row.box(0.71, 0.18, 0.98, 0.82), lab, 26 if ok else 16, 0.5, INK if ok else CREAM, None if ok else (133, 133, 143))
    text(img, p.box(0.05, 0.105, 0.95, 0.155), S("build.free_spots", "{0} free spots in your village").replace("{0}", str(len(SPOTS) - len(BUILT))), 19, CREAM, "mc")
    button(img, K + "ui.button_red", p.box(0.33, 0.025, 0.67, 0.10), S("hook.close", "Close"), 22, 0.5, CREAM)
    return img


def place_bar(img, secs):
    bb = SCREEN.box(0.03, 0.115, 0.97, 0.235)
    sliced(img, K + "ui.panel", bb, 0.6)
    text(img, bb.box(0.03, 0.56, 0.97, 0.94), S("build.place_hint", "Tap a glowing spot for your {0}").replace("{0}", S("build.type.farm")), 26, REWARD, "mc", True)
    button(img, K + "ui.button_green", bb.box(0.05, 0.10, 0.60, 0.54), S("build.auto", "Auto-place ({0})").replace("{0}", str(secs)), 22, 0.5, (255, 255, 255))
    button(img, K + "ui.button_red", bb.box(0.63, 0.10, 0.95, 0.54), S("build.cancel", "Cancel"), 22, 0.5, CREAM)


def tap_marker(img, x, y):
    d = ImageDraw.Draw(img)
    for r_, a in [(54, 120), (36, 200)]:
        d.ellipse((x - r_, y - r_, x + r_, y + r_), outline=(255, 255, 255, a), width=6)


def overview(full, h):
    vd = VILLAGE["village"]
    k = h / full.height
    im = full.resize((int(full.width * k), h), Image.LANCZOS)
    d = ImageDraw.Draw(im)
    # old 2160x2880 village footprint (now the centre of the map)
    ox, oy = 720 * k, 960 * k
    for i in range(0, int(2160 * k), 18):
        d.line((ox + i, oy, ox + min(i + 10, 2160 * k), oy), fill=(255, 255, 255, 230), width=3)
        d.line((ox + i, oy + 2880 * k, ox + min(i + 10, 2160 * k), oy + 2880 * k), fill=(255, 255, 255, 230), width=3)
    for i in range(0, int(2880 * k), 18):
        d.line((ox, oy + i, ox, oy + min(i + 10, 2880 * k)), fill=(255, 255, 255, 230), width=3)
        d.line((ox + 2160 * k, oy + i, ox + 2160 * k, oy + min(i + 10, 2880 * k)), fill=(255, 255, 255, 230), width=3)
    d.text((ox + 8, oy + 6), "old 2160x2880", font=font(18, True), fill=(255, 255, 255))
    for idx, (sid, sp) in enumerate(SPOTS.items()):
        x, y = sp["x"] * vd["width"] * k, sp["y"] * vd["height"] * k
        d.ellipse((x - 13, y - 13, x + 13, y + 13), fill=(255, 214, 80, 235), outline=(60, 40, 10))
        d.text((x, y), str(idx + 1), font=font(15, True), fill=(40, 25, 5), anchor="mm")
    return im


def bar_strip(width):
    """Nav bar close-ups: shipped 3 buttons, the 4-button layout once Battle is added, and the pressed squash."""
    rows = []
    for ids, pressed, cap in [(["nav.heroes", "nav.alliance", "nav.bag"], None, "Shipped: Heroes / Alliance / Bag (evenly spaced, room for Battle)"),
                              (["nav.battle", "nav.heroes", "nav.alliance", "nav.bag"], None, "After AddNavButton(Battle, index 0): bar re-spaces evenly"),
                              (["nav.heroes", "nav.alliance", "nav.bag"], "nav.heroes", "Pressed: squash wide-and-short while held")]:
        n = R(0, 34, W, 34 + 0.1 * H)
        img = Image.new("RGBA", (W, int(n.y1) + 6), (18, 22, 34, 255))
        sliced(img, "theme_a.ui.nav_bar", n, 0.5)
        icons = {"nav.heroes": "theme_a.ui.icon_heroes", "nav.alliance": "theme_a.ui.icon_alliance", "nav.bag": "theme_a.ui.icon_bag", "nav.battle": "theme_a.ui.icon_battle"}
        for i, key in enumerate(ids):
            x0, x1 = nav_slot(i, len(ids))
            painted_button(img, n.box(x0, 0.10, x1, 0.86), icons[key], S(key, key), 22, pressed=(key == pressed))
        ImageDraw.Draw(img).text((12, 6), cap, font=font(22, True), fill=(237, 214, 115))
        rows.append(img)
    out = Image.new("RGBA", (W, sum(r.height for r in rows)), (18, 22, 34, 255))
    y = 0
    for r_ in rows:
        out.alpha_composite(r_, (0, y))
        y += r_.height
    k = width / W
    return out.resize((width, int(out.height * k)), Image.LANCZOS)


def main(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    vd = VILLAGE["village"]
    frames = []
    full, shot = village_shot(BUILT)
    f1 = hud_village(shot)
    frames.append(("1. Village: new bar + Build", f1))
    f2 = build_menu(hud_village(shot, pressed="build"))
    frames.append(("2. Build menu (Castle Lv 6)", f2))
    spot = SPOTS["village.spot_03"]
    foc = (spot["x"] * vd["width"] - 250, spot["y"] * vd["height"] + 150)
    fullp, shotp = village_shot(BUILT, placing=True, zoom=0.6, focus=foc)
    f3 = hud_village(shotp, build=False)
    place_bar(f3, 8)
    # tap ring on the spot we "tap"
    vw, vh = W / 0.6, H / 0.6
    x0 = min(max(foc[0] - vw / 2, 0), vd["width"] - vw)
    y0 = min(max(foc[1] - vh / 2, 0), vd["height"] - vh)
    tap_marker(f3, (spot["x"] * vd["width"] - x0) * 0.6, (spot["y"] * vd["height"] - y0) * 0.6)
    frames.append(("3. Tap a glowing spot (or auto)", f3))
    b4 = BUILT + [("farm", "village.spot_03", 1, "1:12")]
    _, shot4 = village_shot(b4, zoom=0.6, focus=foc)
    f4 = hud_village(shot4)
    frames.append(("4. Building: timer, FREE finish", f4))
    b5 = BUILT + [("farm", "village.spot_03", 1, None)]
    full5, shot5 = village_shot(b5, zoom=0.6, focus=foc)
    f5 = hud_village(shot5)
    frames.append(("5. Built: Lv 1 badge (up to Lv 6)", f5))
    for name, im in frames:
        im.convert("RGB").save(os.path.join(out_dir, "mock_build_" + name.split(".")[0] + "_1080x1920.png"))

    tw, th, pad, lab = 400, 711, 22, 48
    ov = overview(village_map(CASTLE, b5, placing=True), th + lab + 10)
    top_w = pad + (tw + pad) * len(frames) + ov.width + pad
    strip = bar_strip(int((top_w - 2 * pad) * 0.5))
    sheet_img = Image.new("RGBA", (top_w, pad + lab + th + pad + lab + strip.height + pad), (18, 22, 34, 255))
    d = ImageDraw.Draw(sheet_img)
    for i, (name, im) in enumerate(frames):
        x = pad + i * (tw + pad)
        d.text((x + tw / 2, pad + lab / 2), name, font=font(22, True), fill=(237, 214, 115), anchor="mm")
        sheet_img.alpha_composite(im.convert("RGBA").resize((tw, th), Image.LANCZOS), (x, pad + lab))
    ox = pad + len(frames) * (tw + pad)
    d.text((ox + ov.width / 2, pad + lab / 2), f"Whole village {vd['width']}x{vd['height']} (was 2160x2880): {len(SPOTS)} build spots", font=font(20, True), fill=(237, 214, 115), anchor="mm")
    sheet_img.alpha_composite(ov.crop((0, 0, ov.width, th)), (ox, pad + lab))
    y = pad + lab + th + pad
    d.text((pad + strip.width / 2, y + lab / 2), "Bottom bar (painted nav_bar / nav_button / icons, label under icon)", font=font(24, True), fill=(237, 214, 115), anchor="mm")
    sheet_img.alpha_composite(strip, (pad, y + lab))
    # Build button + spot close-ups beside the bar strip
    cx0 = pad + strip.width + pad
    crop = frames[0][1].crop((580, 1460, 1080, 1730)).resize((int(strip.height * 500 / 270), strip.height), Image.LANCZOS)
    sheet_img.alpha_composite(crop.convert("RGBA"), (cx0, y + lab))
    d.text((cx0 + crop.width / 2, y + lab - 4), "Build + toggle (same painted style)", font=font(20, True), fill=(237, 214, 115), anchor="mb")
    crop2 = frames[2][1].crop((200, 300, 1080, 1180)).resize((strip.height, strip.height), Image.LANCZOS)
    cx1 = cx0 + crop.width + pad
    if cx1 + crop2.width <= sheet_img.width:
        sheet_img.alpha_composite(crop2.convert("RGBA"), (cx1, y + lab))
        d.text((cx1 + crop2.width / 2, y + lab - 4), "Free spots glow while placing", font=font(20, True), fill=(237, 214, 115), anchor="mb")
    out = os.path.join(out_dir, "village_build_and_bar.png")
    sheet_img.convert("RGB").save(out)
    print("wrote", out, sheet_img.size)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "/workspace/world_quest")
