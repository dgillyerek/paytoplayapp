"""Builds the Theme A hook UI kit PNGs into the ThemePack. Run: python3 gen_hook_ui.py [out_dir]"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hookart_ui import *
from hookart_icons import *

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Assets", "ThemePack", "fantasy_kingdom_a", "art", "hook")

JOBS = {
    "ui/panel_navy.png": lambda: panel(),
    "ui/plate_navy.png": lambda: plate(),
    "ui/button_gold.png": lambda: button("gold"),
    "ui/button_green.png": lambda: button("green"),
    "ui/button_blue.png": lambda: button("blue"),
    "ui/button_red.png": lambda: button("red"),
    "ui/chip_frame.png": lambda: chip(),
    "ui/bar_frame.png": lambda: bar_frame(),
    "ui/bar_fill_green.png": lambda: bar_fill("green"),
    "ui/bar_fill_gold.png": lambda: bar_fill("gold"),
    "ui/bar_fill_blue.png": lambda: bar_fill("blue"),
    "ui/scroll_parchment.png": lambda: scroll(),
    "ui/ribbon_red.png": lambda: ribbon(),
    "ui/ribbon_blue.png": lambda: ribbon(colour=((90, 140, 240), (40, 74, 160), (18, 34, 84))),
    "ui/badge_free.png": lambda: badge_free(),
    "ui/header_banner.png": lambda: header(),
    "icons/gold.png": lambda: icon_gold(),
    "icons/gem.png": lambda: icon_gem(),
    "icons/wood.png": lambda: icon_wood(),
    "icons/stone.png": lambda: icon_stone(),
    "icons/food.png": lambda: icon_food(),
    "icons/speedup.png": lambda: icon_speedup(),
    "icons/gift.png": lambda: gift(),
    "icons/chest_closed.png": lambda: chest_closed(),
    "icons/chest_open.png": lambda: chest_open(),
    "icons/gems_tier_1.png": lambda: gems_tier(1),
    "icons/gems_tier_2.png": lambda: gems_tier(2),
    "icons/gems_tier_3.png": lambda: gems_tier(3),
    "fx/rays.png": lambda: fx_rays(),
    "fx/glow.png": lambda: fx_glow(),
    "fx/sparkle.png": lambda: fx_sparkle(),
    "fx/burst_ring.png": lambda: fx_burst_ring(),
    "fx/confetti.png": lambda: fx_confetti(),
}

if __name__ == "__main__":
    only = set(sys.argv[2:])
    for rel, fn in JOBS.items():
        if only and rel not in only:
            continue
        path = os.path.join(OUT, rel)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        fn().save(path, optimize=True)
        print("wrote", rel)
