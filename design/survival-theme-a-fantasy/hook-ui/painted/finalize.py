import sys, os, json; sys.path.insert(0,'.')
from cutout import *
import numpy as np
from PIL import Image
import pathlib; R=str(pathlib.Path(__file__).resolve().parents[4] / "Assets/ThemePack/fantasy_kingdom_a")+"/"
OUT='art/hook/painted/'
L=lambda n: Image.open(f'out/{n}_full.png').convert('RGBA')
files={}
def save(img, rel):
    p=R+OUT+rel; os.makedirs(os.path.dirname(p),exist_ok=True); img.save(p,optimize=True); files[rel]=img.size
for n,f in [('castle','castle'),('farm','farm'),('lumber','lumber_mill'),('quarry','quarry')]:
    save(fit(L(n),512,512,True),f'buildings/{f}.png')
for n in ['gold','stone','wood','food','gem','speedup','gift','gems_tier_1','gems_tier_2','gems_tier_3']:
    save(fit(L(n),248,248,True).resize((256,256)) if False else fit(L(n),256,256,True),f'icons/{n}.png')
for n in ['chest_closed','chest_open']:
    save(fit(L(n),320,320,True),f'icons/{n}.png')
save(fit(L('badge_free'),200,200,True),'ui/badge_free.png')
# panel
save(fit(L('panel_alt'),420,1000),'ui/panel_navy.png')
for c in ['gold','green','blue','red']:
    b=L('button_'+c); save(b.resize((round(b.width*112/b.height),112),Image.LANCZOS),f'ui/button_{c}.png')
for c in ['red','blue']:
    save(fit(L('ribbon_'+c),420,1000),f'ui/ribbon_{c}.png')
save(fit(L('scroll'),440,1000),'ui/scroll_parchment.png')
# header: erase crown/flourish by replicating a plain column
h=np.asarray(L('header')).copy(); H,W=h.shape[:2]
x0,x1,ref=int(W*0.27),int(W*0.75),int(W*0.20)
# rows above inner fill: find first row where ref column is navy-ish opaque below border
yy=int(H*0.42)
h[:yy,x0:x1]=h[:yy,ref:ref+1]
hi=trim(Image.fromarray(h,'RGBA'),pad=2)
save(fit(hi,720,1000),'ui/header_banner.png')
# rays square with radial fade
r=L('rays'); cx,cy=r.width//2,r.height//2; s=min(cx,cy)
r=r.crop((cx-s,cy-s,cx+s,cy+s)).resize((512,512),Image.LANCZOS)
a=np.asarray(r).astype(np.float32); yy,xx=np.mgrid[0:512,0:512]; d=np.hypot(xx-255.5,yy-255.5)/256
a[...,3]*=np.clip((1-d)/0.35,0,1); save(Image.fromarray(a.clip(0,255).astype(np.uint8),'RGBA'),'fx/rays.png')
for k,v in files.items(): print(k,v)
