import sys; sys.path.insert(0,'.')
from cutout import *
from PIL import Image, ImageDraw
import os; os.makedirs('out', exist_ok=True)
import numpy as np
icons = ['gold','stone','wood','food','gem','speedup','chest_closed','chest_open','gift','gems_tier_1','gems_tier_2','gems_tier_3','badge_free']
blds = ['castle','farm','lumber','quarry']
ui = ['panel_alt','button_gold','button_green','button_blue','button_red','ribbon_red','ribbon_blue','header','scroll']
params = {'gem':dict(bg_tol=40,grow=False),'speedup':dict(bg_tol=40,grow=False),'gems_tier_2':dict(bg_tol=45,grow=False),'castle':dict(bg_tol=60,grow=False),'scroll':dict(grow=False)}
res={}
for n in icons+blds+ui:
    p=params.get(n,{})
    sh = n not in ui and n!='badge_free'
    res[n]=trim(key_white(f'src/{n}.jpg', shadow=sh, **p), pad=2)
    res[n].save(f'out/{n}_full.png')
res['rays']=key_black('src/rays.jpg'); res['rays'].save('out/rays_full.png')
# preview on dark + checker
W=6; cell=220
names=list(res)
sheet=Image.new('RGBA',(W*cell,((len(names)+W-1)//W)*cell),(40,60,90,255))
d=ImageDraw.Draw(sheet)
for i,n in enumerate(names):
    im=res[n].copy(); im.thumbnail((cell-10,cell-24))
    x,y=(i%W)*cell,(i//W)*cell
    sheet.alpha_composite(im,(x+5,y+5)); d.text((x+5,y+cell-16),n,fill='white')
sheet.save('preview.png')
