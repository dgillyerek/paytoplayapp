import sys; sys.path.insert(0,'.')
from cutout import *
from PIL import Image, ImageDraw
import os
os.makedirs('out',exist_ok=True)
objs = [f'castle_lv{i:02d}' for i in range(1, 21)] + ['forge','village_player','village_rival','village_ally',
        'camp_goblin','camp_bandit','camp_undead','camp_dragon','node_gold','node_wood','node_food',
        'march_player','march_rival','plot_empty','prop_bushes','prop_forest','prop_lake','prop_mountains',
        'level_badge','icon_world','icon_upgrade',
        # bottom bar + Build (PR #74 round 3) and the bigger village's decor
        'nav_bar','nav_button','icon_heroes','icon_alliance','icon_bag','icon_build','icon_battle',
        'prop_rocks','prop_field','prop_tree']
# Castle stages: 1-4 wood -> stone, 5/10/15/20 are the original four anchor paintings.
params = {f'castle_lv{i:02d}':dict(smooth_grow=True,bg_tol={15:60,20:35}.get(i,50),grow=False) for i in range(1, 21)}
params.update({'prop_mountains':dict(smooth_grow=True,bg_tol=22,grow=False,sat_tol=14),
          'camp_dragon':dict(smooth_grow=True,bg_tol=40,grow=False),'camp_undead':dict(smooth_grow=True,bg_tol=40,grow=False),
          'march_player':dict(smooth_grow=True,bg_tol=50,grow=False),'march_rival':dict(smooth_grow=True,bg_tol=50,grow=False),'icon_world':dict(grow=False),'prop_lake':dict(smooth_grow=True)})
for n in ['nav_bar','nav_button']:
    params[n] = dict(smooth_grow=True, bg_tol=40, grow=False)
for n in ['icon_heroes','icon_alliance','icon_bag','icon_build','icon_battle','prop_rocks','prop_field','prop_tree']:
    params[n] = dict(smooth_grow=True, bg_tol=50, grow=False)
only = sys.argv[1:]  # optional name prefixes, e.g. `python3 cut.py castle_lv`
if only:
    objs = [o for o in objs if any(o.startswith(p) for p in only)]
noshadow={'level_badge','icon_upgrade','march_player','march_rival','nav_bar','nav_button','icon_heroes','icon_alliance','icon_bag','icon_build','icon_battle'}
res={}
for n in objs:
    res[n]=trim(key_white(f'src/{n}.jpg', shadow=n not in noshadow, **params.get(n,{})), pad=2)
    res[n].save(f'out/{n}.png')
W=6; cell=220
sheet=Image.new('RGBA',(W*cell,((len(objs)+W-1)//W)*cell),(40,60,90,255)); d=ImageDraw.Draw(sheet)
for i,n in enumerate(objs):
    im=res[n].copy(); im.thumbnail((cell-10,cell-24)); x,y=(i%W)*cell,(i//W)*cell
    sheet.alpha_composite(im,(x+5,y+5)); d.text((x+5,y+cell-16),n,fill='white')
sheet.save('preview.png')
