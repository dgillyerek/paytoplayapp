import json, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter
A='/workspace/ptp-three/Assets/ThemePack/fantasy_kingdom_a/'
pack=json.load(open(A+'pack.json')); cat=json.load(open('/workspace/ptp-three/Assets/Survival/Resources/Survival/campaign.json'))
W,H=1080,1920
FB='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'; FR='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
_f={}
def font(s,b=True):
    k=(s,b)
    if k not in _f: _f[k]=ImageFont.truetype(FB if b else FR,s)
    return _f[k]
def art(key):
    p=pack['art'].get(key); 
    return Image.open(A+p).convert('RGBA') if p else None
def nine(key,w,h,fallback=(40,50,80,255),scale=1.0):
    im=art(key); sl=pack['art_slices'].get(key)
    w,h=max(2,int(w)),max(2,int(h))
    if im is None: return Image.new('RGBA',(w,h),fallback)
    if not sl: return im.resize((w,h),Image.LANCZOS)
    l,t,r,b=sl; iw,ih=im.size
    k=min(scale, w/(l+r+1), h/(t+b+1))
    L,T,R,B=[max(1,int(v*k)) for v in (l,t,r,b)]
    out=Image.new('RGBA',(w,h),(0,0,0,0))
    xs=[(0,l,0,L),(l,iw-r,L,w-R),(iw-r,iw,w-R,w)]; ys=[(0,t,0,T),(t,ih-b,T,h-B),(ih-b,ih,h-B,h)]
    for sx0,sx1,dx0,dx1 in xs:
        for sy0,sy1,dy0,dy1 in ys:
            if dx1>dx0 and dy1>dy0 and sx1>sx0 and sy1>sy0:
                out.paste(im.crop((sx0,sy0,sx1,sy1)).resize((dx1-dx0,dy1-dy0),Image.LANCZOS),(dx0,dy0))
    return out
def text(d,xy,s,size,fill=(255,240,210),anchor='mm',bold=True,stroke=3):
    d.text(xy,s,font=font(size,bold),fill=fill,anchor=anchor,stroke_width=stroke,stroke_fill=(10,10,20))
class Frame:
    def __init__(s,bg=None):
        s.im=Image.open(bg).convert('RGBA').resize((W,H)) if bg else Image.new('RGBA',(W,H),(30,40,60,255))
    def dim(s,a=200):
        s.im.alpha_composite(Image.new('RGBA',(W,H),(5,8,12,a)))
    def paste(s,img,x,y): s.im.alpha_composite(img,(int(x),int(y)))
    @property
    def d(s): return ImageDraw.Draw(s.im)
# panel coordinate helper (Unity anchors, y up) inside rect
class R:
    def __init__(s,x0,y0,x1,y1): s.x0,s.y0,s.x1,s.y1=x0,y0,x1,y1
    def sub(s,a,b,c,e): w=s.x1-s.x0; h=s.y1-s.y0; return R(s.x0+a*w, s.y1-e*h, s.x0+c*w, s.y1-b*h)
    @property
    def w(s): return s.x1-s.x0
    @property
    def h(s): return s.y1-s.y0
    @property
    def c(s): return ((s.x0+s.x1)/2,(s.y0+s.y1)/2)
SCREEN=R(0,0,W,H)
def P(r): return SCREEN.sub(*r) if isinstance(r,tuple) else r
HERO=(0x1b,0x30,0x60,255); VIL=(0x4a,0x1f,0x6e,255); GOLDL=(214,170,80,255)
heroes={h['id']:h for h in cat['heroes']}
def stats(h,level=1,stars=1):
    k=1+0.15*max(0,stars-1)
    hp=int(round((h['hp']+h.get('hp_per_level',0)*max(0,level-1))*k)); atk=int(round((h['atk']+h.get('atk_per_level',0)*max(0,level-1))*k))
    return hp,atk,int(round(hp*0.5+atk*3))
def portrait(f,hid,r,dimmed=False):
    h=heroes[hid]; side=HERO if h.get('side','hero')=='hero' else VIL
    d=f.d; d.rectangle([r.x0,r.y0,r.x1,r.y1],fill=side)
    d.rectangle([r.x0+6,r.y0+6,r.x1-6,r.y1-6],fill=GOLDL)
    pid=hid.split('.',1)[1]; im=Image.open(A+f'art/heroes/portraits/portrait_{pid}.png').convert('RGBA').resize((int(r.w-18),int(r.h-18)),Image.LANCZOS)
    if dimmed:
        im=Image.blend(im,Image.new('RGBA',im.size,(60,60,70,255)),0.55)
    f.paste(im,r.x0+9,r.y0+9)
def button(f,key,r,label,size,ink=(60,30,5)):
    f.paste(nine(key,r.w,r.h,scale=0.6),r.x0,r.y0)
    text(f.d,r.c,label,size,fill=(255,250,235) if key.endswith(('blue','red','green')) else (70,38,8),stroke=2 if not key.endswith('gold') else 0)
def stars(f,r,n):
    for i in range(5):
        cx=r.x0+(i+0.5)*r.w/5; text(f.d,(cx,r.c[1]),'\u2605',int(r.h*0.9),fill=(255,210,60) if i<n else (90,85,78))
def bar(f,r,frac,key='theme_a.hook.ui.bar_fill_blue',label=''):
    f.paste(nine('theme_a.hook.ui.bar_frame',r.w,r.h,scale=0.6),r.x0,r.y0)
    fw=int((r.w-12)*max(0.02,frac)); fill=art(key)
    if fill: f.paste(fill.resize((max(2,fw),int(r.h-12))),r.x0+6,r.y0+6)
    if label: text(f.d,r.c,label,int(r.h*0.55))
def screen(f,title):
    f.dim(210)
    pr=P((0.02,0.035,0.98,0.955)); f.paste(nine('theme_a.hook.ui.panel',pr.w,pr.h),pr.x0,pr.y0)
    hw,hh=700,118; f.paste(nine('theme_a.hook.ui.header',hw,hh,scale=0.9),pr.c[0]-hw/2,pr.y0-hh*0.35)
    text(f.d,(pr.c[0],pr.y0-hh*0.35+hh*0.47),title,40,fill=(255,245,220))
    return pr
def icon(key,s): im=art(key); return im.resize((s,s),Image.LANCZOS) if im else None
def hud(f):
    # HUD bits over the village mock: battle button at slot battle_button
    r=P((0.80,1-0.438,0.975,1-0.338))
    g=art('theme_a.hook.fx.glow')
    if g: f.paste(g.resize((int(r.w*1.4),int(r.h*1.4))),r.x0-r.w*0.2,r.y0-r.h*0.25)
    ic=art('theme_a.ui.icon_battle').resize((int(r.w*0.88),int(r.w*0.88)),Image.LANCZOS); f.paste(ic,r.x0+r.w*0.06,r.y0-r.h*0.02)
    rb=R(r.x0-r.w*0.12,r.y1-r.h*0.32,r.x1+r.w*0.12,r.y1+r.h*0.02); f.paste(nine('theme_a.hook.ui.ribbon_red',rb.w,rb.h,scale=0.4),rb.x0,rb.y0)
    text(f.d,(rb.c[0],rb.c[1]-4),'BATTLE',26,fill=(255,255,255))
    d=f.d; d.ellipse([r.x1-36,r.y0-4,r.x1-2,r.y0+30],fill=(230,46,50)); text(d,(r.x1-19,r.y0+13),'!',22,stroke=1)
# ---------- frame 1: Play HUD ----------
f1=Frame('/workspace/world_quest/mock_village_castle_lv5_1080x1920.png')
nav=P((0.0,1-1.0,1.0,1-0.90)); f1.paste(nine('theme_a.ui.nav_bar',nav.w,nav.h,scale=0.5),nav.x0,nav.y0)
ids=[('Heroes','theme_a.ui.icon_heroes'),('Battle','theme_a.ui.icon_battle'),('Alliance','theme_a.ui.icon_alliance'),('Bag','theme_a.ui.icon_bag')]
n=len(ids); avail=1-2*0.035; w=min(0.22,(avail-0.03*(n-1))/n); tot=w*n+0.03*(n-1)
for i,(lab,ik) in enumerate(ids):
    x0=(1-tot)/2+i*(w+0.03); r=nav.sub(x0,0.10,x0+w,0.86)
    f1.paste(nine('theme_a.ui.nav_button',r.w,r.h,scale=0.42),r.x0,r.y0)
    ic=art(ik); iw=int(r.w*0.76); ih=int(r.h*0.84)
    if ic: ic.thumbnail((iw,ih),Image.LANCZOS); f1.paste(ic,r.c[0]-ic.size[0]/2,r.y0-r.h*0.14)
    text(f1.d,(r.c[0],r.y1-r.h*0.17),lab,22)
    if lab in ('Heroes','Battle'):
        f1.d.ellipse([r.x0+r.w*0.92-17,r.y0+r.h*0.12-17,r.x0+r.w*0.92+17,r.y0+r.h*0.12+17],fill=(230,46,50)); text(f1.d,(r.x0+r.w*0.92,r.y0+r.h*0.12),'!',22,stroke=1)
# ---------- frame 2: Heroes ----------
f2=Frame('/workspace/world_quest/mock_village_castle_lv5_1080x1920.png'); pr=screen(f2,'HEROES')
button(f2,'theme_a.hook.ui.button_gold',pr.sub(0.04,0.893,0.27,0.937),'Heroes',24)
button(f2,'theme_a.hook.ui.button_blue',pr.sub(0.29,0.893,0.52,0.937),'Foes',24)
text(f2.d,(pr.sub(0.54,0.895,0.96,0.935).x1,pr.sub(0,0.915,1,0.915).y0),'Training 1/2',22,anchor='rm')
owned={'hero.sir_aldric':(7,2,12),'hero.lyra':(5,1,6),'hero.rowan':(4,1,3)}
hl=[h for h in cat['heroes'] if h.get('side','hero')=='hero']
for i,h in enumerate(hl[:6]):
    col,row=i%3,i//3; x0=0.035+col*0.315; y1=0.885-row*0.228
    card=pr.sub(x0,y1-0.215,x0+0.30,y1)
    if i==0:
        g=art('theme_a.hook.fx.glow'); f2.paste(g.resize((int(card.w*1.16),int(card.h*1.12))),card.x0-card.w*0.08,card.y0-card.h*0.06)
    pt=card.sub(0,0.22,1,1); o=owned.get(h['id']); portrait(f2,h['id'],pt,dimmed=o is None)
    pl=card.sub(0,0,1,0.24); f2.d.rectangle([pl.x0,pl.y0,pl.x1,pl.y1],fill=HERO)
    text(f2.d,pl.sub(0,0.4,1,0.98).c,h['fighter'],24)
    if o:
        lv,st,_=o; text(f2.d,pl.sub(0,0.02,1,0.46).c,f"Lv {lv}  \u00b7  \u2694 {stats(h,lv,st)[2]:,}",19,fill=(255,214,120)); stars(f2,pt.sub(0.08,0.02,0.92,0.16),st)
        if i==1: bar(f2,pt.sub(0.06,0.19,0.94,0.29),0.62)
    else:
        lk=pt.sub(0,0.36,1,0.62); ov=Image.new('RGBA',(int(lk.w),int(lk.h)),(0,0,0,158)); f2.paste(ov,lk.x0,lk.y0)
        text(f2.d,lk.c,f"\U0001F512 {h['gems']:,} \u25c6".replace('\U0001F512 ',''),24)
        text(f2.d,pl.sub(0,0.02,1,0.46).c,f"6/{h['shards']} shards",19,fill=(255,214,120))
dt=pr.sub(0.025,0.095,0.975,0.43); f2.paste(nine('theme_a.hook.ui.plate',dt.w,dt.h,scale=0.6),dt.x0,dt.y0)
a=heroes['hero.sir_aldric']; portrait(f2,'hero.sir_aldric',dt.sub(0.025,0.24,0.345,0.975)); stars(f2,dt.sub(0.04,0.04,0.33,0.2),2)
text(f2.d,(dt.sub(0.37,0,0,0).x0,dt.sub(0,0.86,0,0.98).c[1]),'Sir Aldric   Lv 7/20',36,anchor='lm')
text(f2.d,(dt.sub(0.37,0,0,0).x0,dt.sub(0,0.78,0,0.86).c[1]),f"{a.get('rarity','epic').title()}  \u00b7  {a.get('role','tank').title()}",22,fill=(205,150,255),anchor='lm',stroke=2)
n,nx=stats(a,7,2),stats(a,8,2)
for lab,y,iv in (('HP',0.69,0),('ATK',0.60,1),('POWER',0.51,2)):
    yy=dt.sub(0,y,0,y+0.09).c[1]; x=dt.sub(0.37,0,0,0).x0
    text(f2.d,(x,yy),lab,28,fill=(230,215,160),anchor='lm'); x2=x+150
    text(f2.d,(x2,yy),f"{n[iv]:,}",28,anchor='lm'); text(f2.d,(x2+130,yy),f"\u2192 {nx[iv]:,}  (+{nx[iv]-n[iv]:,})",26,fill=(126,224,138),anchor='lm')
L=7; gold=int(round(2000*1.45**(L-1)/10)*10); food=int(round(800*1.42**(L-1)/10)*10); secs=int(round(30*1.6**(L-1)))
def compact(v): return f"{v/1000:.1f}K".replace('.0K','K') if v>=1000 else str(v)
button(f2,'theme_a.hook.ui.button_gold',dt.sub(0.37,0.27,0.975,0.46),f"LEVEL UP  {compact(gold)} Gold \u00b7 {compact(food)} Food \u00b7 {secs//60}:{secs%60:02d}",22)
ic=icon('theme_a.ui.icon_shard',int(dt.h*0.17)); f2.paste(ic,dt.sub(0.36,0,0,0).x0,dt.sub(0,0.05,0,0.22).y0)
bar(f2,dt.sub(0.44,0.08,0.66,0.19),12/20,'theme_a.hook.ui.bar_fill_gold','12/20')
button(f2,'theme_a.hook.ui.button_blue',dt.sub(0.68,0.04,0.975,0.23),'STAR UP \u2605',24)
button(f2,'theme_a.hook.ui.button_red',pr.sub(0.30,0.025,0.70,0.082),'Close',26)
# ---------- frame 3: Battle setup ----------
f3=Frame('/workspace/world_quest/mock_village_castle_lv5_1080x1920.png'); pr=screen(f3,'BATTLE')
st=cat['stages']; w=0.95/len(st)
for i,s in enumerate(st):
    x0=0.025+i*w; key='theme_a.hook.ui.button_gold' if i==1 else 'theme_a.hook.ui.button_blue'
    r=pr.sub(x0+0.004,0.862,x0+w-0.004,0.938); f3.paste(nine(key,r.w,r.h,scale=0.5),r.x0,r.y0)
    if i>2: f3.paste(Image.new('RGBA',(int(r.w),int(r.h)),(40,40,48,170)),r.x0,r.y0)
    lab=f"{i+1}{' \u2713' if i==0 else ''}"; text(f3.d,(r.c[0],r.c[1]-14),lab,19,fill=(70,38,8) if i==1 else (255,255,255),stroke=0 if i==1 else 2)
    text(f3.d,(r.c[0],r.c[1]+12),s['name'],15,fill=(70,38,8) if i==1 else (230,230,240),stroke=0 if i==1 else 2)
b=pr.sub(0.04,0.795,0.96,0.852); f3.paste(nine('theme_a.hook.ui.plate',b.w,b.h,scale=0.5),b.x0,b.y0)
text(f3.d,(b.x0+b.w*0.03,b.c[1]),'BOUNTY: Clear Iron Pass  0/1',22,anchor='lm',stroke=2)
s=st[1]; enemies=s['enemies'] if 'enemies' in s else s['lanes']; n=len(enemies)
text(f3.d,pr.sub(0.3,0.528,0.7,0.578).c,'VS',56,fill=(255,214,120))
aspect=(pr.w)/(pr.h)
lineup=['hero.sir_aldric','hero.lyra','hero.rowan'][:n]
hp=0; ep=0
for lane in range(n):
    cx=0.5+(lane-(n-1)*0.5)*0.315; x0,x1=cx-0.135,cx+0.135
    e=enemies[lane]; eid=e['id']; elv=e.get('level',1)
    portrait(f3,eid,pr.sub(x0,0.62,x1,0.62+0.27*aspect)); es=stats(heroes[eid],elv,e.get('stars',1)); ep+=es[2]
    text(f3.d,pr.sub(x0,0.598,x1,0.62).c,f"{heroes[eid]['fighter']}  Lv {elv}",21,fill=(255,200,210),stroke=2)
    text(f3.d,pr.sub(x0,0.578,x1,0.60).c,f"\u2694 {es[2]:,}",21,fill=(255,200,210),stroke=2)
    text(f3.d,pr.sub(x0,0.515,x1,0.535).c,f"LANE {lane+1}",18,fill=(170,170,180),stroke=2)
    hr=pr.sub(x0,0.345,x1,0.345+0.27*aspect)
    if lane<2:
        hid=lineup[lane]; portrait(f3,hid,hr); lv,stv,_=owned[hid]; hs=stats(heroes[hid],lv,stv); hp+=hs[2]
        text(f3.d,pr.sub(x0,0.323,x1,0.345).c,f"{heroes[hid]['fighter']}  Lv {lv}",21,fill=(200,225,255),stroke=2)
        text(f3.d,pr.sub(x0,0.30,x1,0.323).c,f"\u2694 {hs[2]:,}",21,fill=(200,225,255),stroke=2)
    else:
        f3.d.rectangle([hr.x0,hr.y0,hr.x1,hr.y1],fill=(27,48,96,150)); f3.d.rectangle([hr.x0+8,hr.y0+8,hr.x1-8,hr.y1-8],outline=(102,158,255,140),width=4)
        text(f3.d,hr.c,'+\nTap a hero',26)
ratio=hp/(hp+ep)
text(f3.d,(pr.sub(0.05,0,0,0).x0,pr.sub(0,0.262,0,0.312).c[1]),f"YOU \u2694 {hp:,}",26,fill=(102,158,255),anchor='lm')
text(f3.d,(pr.sub(0,0,0.95,0).x1,pr.sub(0,0.262,0,0.312).c[1]),f"{ep:,} \u2694 FOES",26,fill=(184,107,255),anchor='rm')
o=pr.sub(0.40,0.266,0.60,0.308); col=(77,199,97) if ratio>0.58 else (219,173,56) if ratio>0.45 else (230,107,56)
f3.d.rounded_rectangle([o.x0,o.y0,o.x1,o.y1],radius=16,fill=col); text(f3.d,o.c,'EASY' if ratio>0.58 else 'EVEN' if ratio>0.45 else 'HARD',22)
bb=pr.sub(0.05,0.246,0.95,0.258); f3.d.rectangle([bb.x0,bb.y0,bb.x1,bb.y1],fill=VIL); f3.d.rectangle([bb.x0,bb.y0,bb.x0+bb.w*ratio,bb.y1],fill=(102,158,255))
text(f3.d,(pr.sub(0.05,0,0,0).x0,pr.sub(0,0.205,0,0.235).c[1]),'YOUR HEROES \u00b7 tap to place',20,anchor='lm',stroke=2)
button(f3,'theme_a.hook.ui.button_blue',pr.sub(0.74,0.200,0.95,0.240),'AUTO',20)
for i,hid in enumerate(['hero.sir_aldric','hero.lyra','hero.rowan']):
    px=0.04+i*0.155; r=pr.sub(px,0.10,px+0.145,0.10+0.145*aspect); portrait(f3,hid,r,dimmed=i<2)
    text(f3.d,(r.c[0],r.y1-18),f"Lv {owned[hid][0]}",18,stroke=2)
    if i<2: f3.d.ellipse([r.x1-44,r.y0+4,r.x1-4,r.y0+44],fill=(214,170,60)); text(f3.d,(r.x1-24,r.y0+24),str(i+1),24,fill=(70,38,8),stroke=0)
button(f3,'theme_a.hook.ui.button_red',pr.sub(0.04,0.022,0.32,0.085),'Close',24)
button(f3,'theme_a.hook.ui.button_gold',pr.sub(0.36,0.018,0.96,0.090),'FIGHT!',40)
# ---------- frame 4: result ----------
f4=Frame('/workspace/world_quest/mock_village_castle_lv5_1080x1920.png'); pr=screen(f4,'VICTORY!')
g=art('theme_a.hook.fx.glow'); r=pr.sub(0.1,0.62,0.9,0.95); f4.paste(g.resize((int(r.w),int(r.h))),r.x0,r.y0)
for i,hid in enumerate(lineup[:2]):
    cx=0.5+(i-0.5)*0.27; portrait(f4,hid,pr.sub(cx-0.12,0.70,cx+0.12,0.70+0.24*aspect))
text(f4.d,pr.sub(0.05,0.635,0.95,0.685).c,s['name'],34,fill=(255,214,120))
text(f4.d,pr.sub(0.03,0.595,0.97,0.635).c,'Foes defeated 2  \u00b7  Heroes standing 2  \u00b7  0:41',22,stroke=2)
fr=pr.sub(0.28,0.545,0.72,0.592); f4.paste(nine('theme_a.hook.ui.ribbon_red',fr.w,fr.h,scale=0.45),fr.x0,fr.y0); text(f4.d,(fr.c[0],fr.c[1]-3),'FIRST CLEAR!',24)
rw=s.get('rewards',{}); rows=[(k,v) for k,v in rw.items()]+[('res.hard',s.get('first_gems',0)),('shard',s.get('shards',0))]
y=0.53
for k,v in rows:
    r=pr.sub(0.22,y-0.05,0.78,y); f4.paste(nine('theme_a.hook.ui.chip',r.w,r.h,scale=0.6),r.x0,r.y0)
    key='theme_a.ui.icon_shard' if k=='shard' else 'theme_a.hook.icon.'+k; ic=icon(key,int(r.h*1.25))
    if ic: f4.paste(ic,r.x0+8,r.y0-r.h*0.12)
    nm={'res.soft':'Gold','res.food':'Food','res.hard':'Gems','shard':f"{heroes[s.get('shard_hero','hero.oakenshield')]['fighter']} shards"}.get(k,k)
    text(f4.d,(r.x0+r.w*0.24,r.c[1]),f"+{v:,}  {nm}",26,fill=(255,214,120),anchor='lm'); y-=0.058
text(f4.d,pr.sub(0.04,y-0.045,0.96,y).c,'BOUNTY: Clear Iron Pass  1/1  \u2714 claim it in Battle',22,fill=(77,209,97),stroke=2)
button(f4,'theme_a.hook.ui.button_gold',pr.sub(0.25,0.06,0.75,0.15),'COLLECT',34)
# ---------- frame 5: recruit ----------
f5=Frame('/workspace/world_quest/mock_village_castle_lv5_1080x1920.png'); f5.im=f2.im.copy(); f5.dim(200)
sr=R(W*0.05,H*0.25,W*0.95,H*0.75); f5.paste(nine('theme_a.hook.ui.panel',sr.w,sr.h),sr.x0,sr.y0)
hw,hh=640,110; f5.paste(nine('theme_a.hook.ui.header',hw,hh,scale=0.9),sr.c[0]-hw/2,sr.y0-hh*0.35); text(f5.d,(sr.c[0],sr.y0-hh*0.35+hh*0.47),'Recruit Oakenshield',34)
oak=heroes['hero.oakenshield']; portrait(f5,'hero.oakenshield',sr.sub(0.05,0.50,0.36,0.50+0.31*sr.w/sr.h))
text(f5.d,(sr.sub(0.40,0,0,0).x0,sr.sub(0,0.74,0,0.82).c[1]),f"{oak.get('rarity','rare').title()} {oak.get('role','tank').title()}",26,fill=(140,200,255),anchor='lm',stroke=2)
os_=stats(oak); text(f5.d,(sr.sub(0.40,0,0,0).x0,sr.sub(0,0.56,0,0.73).c[1]),f"HP {os_[0]:,}  \u00b7  ATK {os_[1]}\nPOWER {os_[2]:,}",24,anchor='lm')
text(f5.d,sr.sub(0,0.47,1,0.50).c,'SANDBOX \u00b7 no real charge',16,fill=(255,140,115),stroke=2)
for (yy0,yy1,ik,lab,bk,bl) in ((0.31,0.46,'theme_a.hook.icon.res.hard','Recruit now','theme_a.hook.ui.button_gold',f"{oak['gems']:,} \u25c6"),(0.15,0.30,'theme_a.ui.icon_shard',f"6/{oak['shards']} shards",'theme_a.hook.ui.button_green','Unlock')):
    r=sr.sub(0.04,yy0,0.96,yy1); f5.paste(nine('theme_a.hook.ui.plate',r.w,r.h,scale=0.5),r.x0,r.y0)
    ic=icon(ik,int(r.h*0.85)); f5.paste(ic,r.x0+12,r.y0+r.h*0.075)
    text(f5.d,(r.x0+r.w*0.2,r.c[1]),lab,24,anchor='lm',stroke=2)
    br=r.sub(0.64,0.14,0.98,0.86); button(f5,bk,br,bl,24)
    if bk.endswith('green'): f5.paste(Image.new('RGBA',(int(br.w),int(br.h)),(40,40,48,150)),br.x0,br.y0)
button(f5,'theme_a.hook.ui.button_blue',sr.sub(0.06,0.03,0.48,0.12),'Get Gems',22)
button(f5,'theme_a.hook.ui.button_red',sr.sub(0.52,0.03,0.94,0.12),'Close',22)
# ---------- sheet ----------
frames=[(f1,'1 · Play: painted Battle + Heroes in the bottom bar'),(f2,'2 · Heroes: roster, Lv-up for gold/food, star-up'),(f5,'3 · Recruit (sandbox, gems or shards)'),(f3,'4 · Battle Setup: stage, lanes, power, FIGHT'),(f4,'5 · Result back in Play: rewards + bounty')]
sw,sh=540,960; pad=24; top=70
sheet=Image.new('RGB',(pad+len(frames)*(sw+pad),top+sh+pad),(18,20,26)); sd=ImageDraw.Draw(sheet)
sd.text((pad,18),'PayToPlay Theme A — Heroes screen + Battle Setup (mock from code layout, real pack art)',font=font(30),fill=(240,230,200))
for i,(f,lab) in enumerate(frames):
    x=pad+i*(sw+pad); sheet.paste(f.im.convert('RGB').resize((sw,sh),Image.LANCZOS),(x,top))
    sd.rectangle([x,top+sh-40,x+sw,top+sh],fill=(0,0,0)); sd.text((x+10,top+sh-32),lab,font=font(17),fill=(255,230,170))
sheet.save('/workspace/world_quest/heroes_and_battle_setup.png'); print(sheet.size)
for i,(f,_) in enumerate(frames): f.im.convert('RGB').save(f'/workspace/world_quest/mock_heroes_battle_{i+1}_1080x1920.png')
