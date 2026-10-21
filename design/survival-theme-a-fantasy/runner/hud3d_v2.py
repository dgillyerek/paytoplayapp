from PIL import Image, ImageDraw, ImageFont, ImageFilter
import json, random, math
A='/workspace/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/'
R='/workspace/runner/r3d/'
def img(p): return Image.open(A+p).convert('RGBA')
def F(s,b=True): return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf' if b else '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', s)
UI={k:img('art/hook/painted/ui/'+v) for k,v in dict(panel='panel_navy.png',header='header_banner.png',red='button_red.png',green='button_green.png',blue='button_blue.png',ribbon='ribbon_blue.png').items()}
UI['chip']=img('art/hook/ui/chip_frame.png'); UI['plate']=img('art/hook/ui/plate_navy.png'); UI['fillgold']=img('art/hook/ui/bar_fill_gold.png')
UI['rays']=img('art/hook/painted/fx/rays.png'); UI['conf']=img('art/hook/fx/confetti.png'); UI['ring']=img('art/hook/fx/burst_ring.png'); UI['spark']=img('art/hook/fx/sparkle.png')
SW=img('art/world/ui/icon_battle.png')
W,H=540,960
HERO_FRAME=(0x1b,0x30,0x60,255); VIL_FRAME=(0x4a,0x1f,0x6e,255); HERO_FILL=(92,219,107,255); VIL_FILL=(235,77,82,255)
def txt(d,xy,t,size,fill='white',anchor='mm',stroke=2,sf='black'): d.text(xy,t,font=F(size),fill=fill,anchor=anchor,stroke_width=stroke,stroke_fill=sf)
def vp(j):
    import math
    P=j['__cam']['pos']; L=j['__cam']['look']
    f=[L[i]-P[i] for i in range(3)]; n=math.sqrt(sum(x*x for x in f)); f=[x/n for x in f]
    r=[f[1]*1-0, -(f[0]*1), 0]; rn=math.sqrt(r[0]**2+r[1]**2); r=[r[0]/rn,r[1]/rn,0]  # cross(f, z)
    u=[r[1]*f[2]-r[2]*f[1], r[2]*f[0]-r[0]*f[2], r[0]*f[1]-r[1]*f[0]]
    v=[0,1,0]; fp=(H/2)/math.tan(math.radians(26))
    dz=sum(v[i]*f[i] for i in range(3)); return (W/2+fp*sum(v[i]*r[i] for i in range(3))/dz, H/2-fp*sum(v[i]*u[i] for i in range(3))/dz)
def slim(c,a,frac,hp=None):
    # v2: 6 px sliver (3 px at this half-scale preview), short, 70% alpha, only on damaged units; number on boss only
    (lx,ly,_),(hx,hy,_)=a['bar']; h=min(110,max(34,abs(ly-hy)))*0.5; w=3.5
    cx=(lx+hx)/2; cy=(ly+hy)/2; y0=cy-h/2; y1=cy+h/2
    o=Image.new('RGBA',c.size,(0,0,0,0)); d=ImageDraw.Draw(o)
    d.rectangle((cx-w/2,y0,cx+w/2,y1),fill=HERO_FRAME if a.get('hero') else VIL_FRAME)
    d.rectangle((cx-w/2+0.5,y0+0.5,cx+w/2-0.5,y1-0.5),fill=(13,13,20,255))
    d.rectangle((cx-w/2+0.5,y1-0.5-(h-1)*frac,cx+w/2-0.5,y1-0.5),fill=HERO_FILL if a.get('hero') else VIL_FILL)
    a_=o.getchannel('A').point(lambda v:int(v*0.7)); o.putalpha(a_); c.alpha_composite(o)
    if hp is not None: txt(ImageDraw.Draw(c),(cx,y0-7),str(hp),9,stroke=1)
def slimbox(c,a,hp,mx,label):
    x,y,_=a['head']; y-=8; w=55
    o=Image.new('RGBA',c.size,(0,0,0,0)); d=ImageDraw.Draw(o)
    if hp<mx:
        d.rectangle((x-w/2,y,x+w/2,y+4.5),fill=(13,13,20,255)); d.rectangle((x-w/2+0.7,y+0.7,x-w/2+0.7+(w-1.4)*hp/mx,y+3.8),fill=(240,196,72,255))
        o.putalpha(o.getchannel('A').point(lambda v:int(v*0.75))); c.alpha_composite(o); txt(ImageDraw.Draw(c),(x,y-7),str(hp),9,stroke=1)
    p=UI['plate'].resize((96,24)); c.alpha_composite(p,(int(x-48),int(y-40))); txt(ImageDraw.Draw(c),(x,y-28),label,11,fill=(255,226,140))
def straight(c,a,V,frac=0.42,col=(180,230,255)):
    # shots leave the shooter and fly straight down its own lane (toward the road's vanishing point)
    d=ImageDraw.Draw(c); ax=a['head'][0]; ay=a['head'][1]*0.45+a['foot'][1]*0.55
    for k,(t0,t1) in enumerate([(0.06,0.13),(0.2,0.27),(0.34,0.40)][:3]):
        x0,y0=ax+(V[0]-ax)*t0,ay+(V[1]-ay)*t0; x1,y1=ax+(V[0]-ax)*t1,ay+(V[1]-ay)*t1
        d.line((x0,y0,x1,y1),fill=col+(220,),width=3); d.ellipse((x1-3,y1-3,x1+3,y1+3),fill=(255,255,235))
def sidebar(c,a,frac,hp):
    (lx,ly,_),(hx,hy,_)=a['bar']; h=min(460,max(90,abs(ly-hy)))*0.5  # 1080x1920 reference canvas -> 540x960 preview
    w=10; cx=(lx+hx)/2; cy=(ly+hy)/2; y0=cy-h/2; y1=cy+h/2
    d=ImageDraw.Draw(c); d.rectangle((cx-w/2,y0,cx+w/2,y1),fill=HERO_FRAME if a.get('hero') else VIL_FRAME)
    d.rectangle((cx-w/2+1.5,y0+1.5,cx+w/2-1.5,y1-1.5),fill=(13,13,20,230))
    fy=y1-1.5-(h-3)*frac; d.rectangle((cx-w/2+1.5,fy,cx+w/2-1.5,y1-1.5),fill=HERO_FILL if a.get('hero') else VIL_FILL)
    txt(d,(cx,y0-9),str(hp),12)
def boxbar(c,a,hp,mx,label):
    x,y,_=a['head']; y-=8; w=74; d=ImageDraw.Draw(c)
    d.rounded_rectangle((x-w/2-2,y-2,x+w/2+2,y+11),4,fill=(30,22,16,235)); c.alpha_composite(UI['fillgold'].resize((max(1,int(w*hp/mx)),9)),(int(x-w/2),int(y)))
    txt(d,(x,y-10),str(hp),14)
    p=UI['plate'].resize((110,30)); c.alpha_composite(p,(int(x-55),int(y-56))); txt(d,(x,y-41),label,13,fill=(255,226,140))
def badge(c,a,n):
    x,y,_=a['head']; y-=34; d=ImageDraw.Draw(c); c.alpha_composite(UI['chip'].resize((84,32)),(int(x-42),int(y-16)))
    if SW: c.alpha_composite(SW.resize((26,26)),(int(x-38),int(y-13)))
    txt(d,(x+10,y),f'×{n}',18,fill=(255,236,150))
def hud(c,dist,kills,dmg,spd,banner=None,length=150):
    d=ImageDraw.Draw(c)
    c.alpha_composite(UI['red'].resize((64,36)),(8,10)); txt(d,(40,28),'Leave',12)
    c.alpha_composite(UI['header'].resize((300,54)),(120,8)); txt(d,(270,34),'Thornwood Road',18,sf=(40,20,10))
    x0,x1,y=76,430,92; d.rounded_rectangle((x0-3,y-3,x1+3,y+15),6,fill=(30,22,16,235)); c.alpha_composite(UI['fillgold'].resize((max(1,int((x1-x0)*dist/length)),12)),(x0,y))
    txt(d,((x0+x1)/2,y+6),f'{dist} m / {length} m',11)
    if SW:
        t=SW.resize((34,34)); r,g,b,al=t.split(); t=Image.merge('RGBA',(r.point(lambda v:min(255,v+60)),g.point(lambda v:v*0.55),b.point(lambda v:v*0.5),al)); c.alpha_composite(t,(x1+4,y-11))
    txt(d,(x1+21,y+30),'BOSS',11,fill=(255,110,95))
    c.alpha_composite(UI['chip'].resize((96,30)),(W-104,128)); 
    if SW: c.alpha_composite(SW.resize((24,24)),(W-100,131))
    txt(d,(W-50,143),str(kills),15)
    for i,(t,on) in enumerate([(f'+{dmg}% DMG',dmg),(f'+{spd}% SPD',spd)]):
        if on: c.alpha_composite(UI['chip'].resize((98,30)),(10+i*104,128)); txt(d,(59+i*104,143),t,12)
    if banner: c.alpha_composite(UI['ribbon'].resize((380,58)),(80,180)); txt(d,(W/2,206),banner,19,sf=(10,20,60))
def shot(c,a,b,col=(255,236,170)):
    d=ImageDraw.Draw(c); ax,ay=a['head'][0],(a['head'][1]*0.4+a['foot'][1]*0.6); bx,by=b['head'][0],(b['head'][1]*0.5+b['foot'][1]*0.5)
    ex,ey=ax+(bx-ax)*0.7,ay+(by-ay)*0.7; sx,sy=ax+(bx-ax)*0.45,ay+(by-ay)*0.45
    d.line((sx,sy,ex,ey),fill=col+(200,),width=3); d.ellipse((ex-4,ey-4,ex+4,ey+4),fill=(255,255,230))
def floater(c,a,t,big=False,col=None):
    x,y,_=a['head']; d=ImageDraw.Draw(c); txt(d,(x,y-30),t,26 if big else 18,fill=col or ((255,215,60) if big else 'white'),stroke=3)
def burst(c,a):
    x=a['foot'][0]; y=(a['foot'][1]+a['head'][1])/2; r=UI['ring'].resize((110,110)); c.alpha_composite(r,(int(x-55),int(y-55)))
    for i in range(10):
        ang=i/10*6.28; c.alpha_composite(UI['spark'].resize((16,16)),(int(x+math.cos(ang)*48-8),int(y+math.sin(ang)*48-8)))
def frame(name):
    c=Image.open(R+name+'.png').convert('RGBA'); j=json.load(open(R+name+'.json')); return c,j
# --- A (v2)
cA,j=frame('a'); V=vp(j)
for k in j:
    if k.startswith('nf') or k=='bl': j[k]['hero']=False
j['hero']['hero']=True
for k in ['hero','f0','f1','f2','f3','f4']: straight(cA,j[k],V,col=(255,236,170) if k in ('f0','f3') else (180,230,255))
slim(cA,j['nf0'],0.35)   # damaged: thin bar, no number
slimbox(cA,j['box_ald'],290,530,'Sir Aldric joins!')
floater(cA,j['nf0'],'-61',True); floater(cA,j['box_ald'],'-26')
badge(cA,j['hero'],6); hud(cA,38,5,25,0,banner='+3 Lyra joined!')
# --- B (v2)
cB,k=frame('b'); V=vp(k); k['hero']['hero']=True; k['boss']['hero']=False
for f in ['hero','f0','f1','f3','f4','f5','f6','f8']: straight(cB,k[f],V)
slim(cB,k['boss'],0.62,8190)
floater(cB,k['boss'],'-92',True); badge(cB,k['hero'],10); hud(cB,142,22,60,30,banner='Ironhowl blocks the road!')
d=ImageDraw.Draw(cB); bx,by,_=k['boss']['head']; txt(d,(bx,by-58),'BOSS',20,fill=(255,95,80),stroke=3)
# --- C result over B
cC=Image.open(R+'b.png').convert('RGBA').filter(ImageFilter.GaussianBlur(3)); cC.alpha_composite(Image.new('RGBA',(W,H),(0,0,10,150)))
cC.alpha_composite(UI['rays'].resize((620,620)),(W//2-310,120)); cC.alpha_composite(UI['panel'].resize((460,520)),(40,260)); cC.alpha_composite(UI['ribbon'].resize((400,80)),(70,230))
d=ImageDraw.Draw(cC); txt(d,(W/2,268),'Victory!',30,sf=(10,20,60),stroke=3)
for i,(a,b) in enumerate([('Foes defeated','24'),('Boxes broken','10 / 10'),('Squad','10'),('Distance','142 m')]):
    y=350+i*42; d.text((90,y),a,font=F(20,False),fill=(230,230,240),anchor='lm'); d.text((450,y),b,font=F(20),fill=(255,226,140),anchor='rm')
cC.alpha_composite(UI['chip'].resize((180,56)),(80,530)); txt(d,(170,558),'2,380 gold',17)
cC.alpha_composite(UI['chip'].resize((180,56)),(280,530)); txt(d,(370,558),'+10 Lyra shards',15)
cC.alpha_composite(UI['blue'].resize((170,62)),(80,640)); txt(d,(165,671),'Retry',22)
cC.alpha_composite(UI['green'].resize((170,62)),(290,640)); txt(d,(375,671),'Continue',22)
for i in range(30):
    random.seed(i); cf=UI['conf'].resize((24,24)).rotate(random.randint(0,360)); col=random.choice([(255,90,90),(255,220,90),(110,200,255),(140,240,120)])
    t=Image.new('RGBA',cf.size,col+(255,)); t.putalpha(cf.getchannel('A')); cC.alpha_composite(t,(random.randint(0,W-24),random.randint(150,900)))
sheet=Image.new('RGBA',(W*3+40,H+130),(24,20,30,255)); sd=ImageDraw.Draw(sheet)
sd.text((20,16),'Runner v2 — straight shots, faster fire, quiet HP bars (Blender render of the battle FBXs + HUD; not a Unity capture)',font=F(22),fill=(255,226,140))
sd.text((20,52),'Shots fly straight down each shooter\'s lane (steer to aim) · ~2.75x fire rate · HP bars: 6 px, 70% alpha, only while damaged, fade 2 s after the last hit',font=F(15,False),fill=(220,220,230))
sd.text((20,76),'Full-HP units show no bar · numbers only on boxes and the boss · boxes keep their reward tag (Nightfang at left is mid-damage; Ironhowl 8190 / 12600)',font=F(15,False),fill=(220,220,230))
caps=['1  38 m: Nightfangs, Blightroot, Aldric on a 530 HP chest','2  142 m: Ironhowl (1.5x) blocks the road','3  Result: gold + Lyra shards, Retry / Continue']
for i,cv in enumerate([cA,cB,cC]):
    x=10+i*(W+10); sheet.alpha_composite(cv,(x,105)); sd.text((x+6,H+112),caps[i],font=F(14),fill='white')
sheet.convert('RGB').save('/workspace/runner/runner_3d_v2.png'); print(sheet.size)
