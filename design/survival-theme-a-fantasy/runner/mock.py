from PIL import Image, ImageDraw, ImageFont, ImageFilter
import math, random
A='/workspace/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/'
def img(p): return Image.open(A+p).convert('RGBA')
def F(s,b=True):
    try: return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf' if b else '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', s)
    except: return ImageFont.load_default()
P={n:img(f'art/heroes/portraits/portrait_{n}.png') for n in ['lyra','rowan','sir_aldric','oakenshield','bonequill','nightfang','blightroot','ironhowl']}
UI={k:img('art/hook/painted/ui/'+v) for k,v in dict(panel='panel_navy.png',header='header_banner.png',red='button_red.png',green='button_green.png',blue='button_blue.png',ribbon='ribbon_blue.png',ribbonr='ribbon_red.png').items()}
UI['chip']=img('art/hook/ui/chip_frame.png'); UI['fillg']=img('art/hook/ui/bar_fill_green.png'); UI['fillgold']=img('art/hook/ui/bar_fill_gold.png'); UI['frame']=img('art/hook/ui/bar_frame.png')
UI['rays']=img('art/hook/painted/fx/rays.png'); UI['conf']=img('art/hook/fx/confetti.png'); UI['ring']=img('art/hook/fx/burst_ring.png'); UI['spark']=img('art/hook/fx/sparkle.png')
CH={'hero':img('art/hook/painted/icons/chest_closed.png'),'open':img('art/hook/painted/icons/chest_open.png'),'weapon':img('art/runner/crate_weapon.png'),'speed':img('art/runner/crate_speed.png')}
TREE=img('art/world/village/prop_tree.png'); BUSH=img('art/world/village/prop_bushes.png'); ROCK=img('art/world/village/prop_rocks.png')
ICON=img('art/runner/runner_icon.png')
W,H=540,960
HZ=300  # horizon
def proj(x,z):  # x metres from centre, z metres ahead of camera
    z=max(z,0.5); s=760/(z+3); return W/2+x*s*1.0, HZ+ (9.2*s)*0.42, s
def paste(c,im,cx,by,h):
    r=im.width/im.height; w=int(h*r); h=int(h)
    if w<2 or h<2: return
    t=im.resize((w,h),Image.LANCZOS); c.alpha_composite(t,(int(cx-w/2),int(by-h)))
def round_portrait(im,d,ring=(255,215,120)):
    t=im.resize((d,d),Image.LANCZOS); m=Image.new('L',(d,d),0); ImageDraw.Draw(m).ellipse((0,0,d-1,d-1),fill=255)
    o=Image.new('RGBA',(d+6,d+6),(0,0,0,0)); dd=ImageDraw.Draw(o); dd.ellipse((0,0,d+5,d+5),fill=ring+(255,)); o.paste(t,(3,3),m); return o
def unit(c,name,x,z,hp=None,mx=None,ring=(120,200,255),foe=False,big=1.0):
    cx,by,s=proj(x,z); d=int(s*0.9*big)
    sh=Image.new('RGBA',(d,d//3),(0,0,0,0)); ImageDraw.Draw(sh).ellipse((0,0,d-1,d//3-1),fill=(0,0,0,90)); c.alpha_composite(sh,(int(cx-d/2),int(by-d//6)))
    d=int(d*0.8); p=round_portrait(P[name],d,(230,80,70) if foe else ring); c.alpha_composite(p,(int(cx-p.width/2),int(by-p.height-d*0.1)))
    if hp is not None: bar(c,cx,by-p.height-d*0.1-10,max(44,d*1.1),hp,mx,foe)
def bar(c,cx,y,w,hp,mx,foe=True,num=True):
    dr=ImageDraw.Draw(c); w=int(w); x0=int(cx-w/2); h=11
    dr.rounded_rectangle((x0-2,y-2,x0+w+2,y+h+2),4,fill=(30,22,16,230))
    f=UI['fillg'] if not foe else UI['fillgold']
    fw=max(1,int(w*hp/mx)); c.alpha_composite(f.resize((fw,h)),(x0,int(y)))
    if num:
        t=f'{hp}'; ft=F(13); tw=dr.textlength(t,font=ft); dr.text((cx-tw/2,y-16),t,font=ft,fill='white',stroke_width=2,stroke_fill='black')
def box(c,kind,x,z,hp,mx,tag,tagp=None):
    cx,by,s=proj(x,z); h=s*1.5; paste(c,CH[kind],cx,by,h)
    bar(c,cx,by-h-12,max(60,s*1.6),hp,mx,False)
    dr=ImageDraw.Draw(c); ft=F(int(max(14,s*0.42)))
    tw=dr.textlength(tag,font=ft); bx=cx-tw/2+(12 if tagp else 0); ty=by-h-40-s*0.2
    if tagp: rp=round_portrait(P[tagp],int(s*0.5)+6,(255,215,120)); c.alpha_composite(rp,(int(bx-rp.width-4),int(ty-2)))
    dr.text((bx,ty),tag,font=ft,fill=(255,236,150),stroke_width=3,stroke_fill=(60,30,10))
def scene(dist):
    c=Image.new('RGBA',(W,H)); dr=ImageDraw.Draw(c)
    for y in range(HZ): k=y/HZ; dr.line((0,y,W,y),fill=(int(150+60*k),int(190+40*k),int(220+15*k)))
    dr.rectangle((0,HZ,W,H),fill=(92,140,60))
    for z in range(0,60):
        x0,y0,_=proj(-4,z); x1,_,_=proj(4,z); x2,y2,_=proj(-4,z+1); x3,_,_=proj(4,z+1)
        col=(196,168,118) if (z+dist)%4<2 else (186,158,108)
        dr.polygon([(x0,y0),(x1,y0),(x3,y2),(x2,y2)],fill=col)
    for z in [40,30,22,15,10,6]:
        for side in (-1,1):
            cx,by,s=proj(side*(6+(z%3)),z); paste(c,TREE if z%2 else BUSH,cx,by,s*(3.2 if z%2 else 1.4))
            cx,by,s=proj(side*4.8,z+2); paste(c,ROCK,cx,by,s*0.7)
    fog=Image.new('RGBA',(W,H),(0,0,0,0)); fd=ImageDraw.Draw(fog)
    for y in range(HZ,HZ+80): fd.line((0,y,W,y),fill=(205,225,235,int(170*(1-(y-HZ)/80))))
    c.alpha_composite(fog); return c
def hud(c,dist,kills,dmg,spd,length=150,banner=None):
    dr=ImageDraw.Draw(c)
    paste(c,UI['header'],W/2,70,62); t='Thornwood Road'; ft=F(22); dr.text((W/2-dr.textlength(t,font=ft)/2,26),t,font=ft,fill='white',stroke_width=2,stroke_fill=(40,20,10))
    x0,x1,y=70,W-90,92; dr.rounded_rectangle((x0-3,y-3,x1+3,y+17),6,fill=(30,22,16,230)); c.alpha_composite(UI['fillgold'].resize((max(1,int((x1-x0)*dist/length)),14)),(x0,y))
    rp=round_portrait(P['ironhowl'],34,(230,80,70)); c.alpha_composite(rp,(x1-4,y-12))
    t=f'{dist} m / {length} m'; ft=F(13); dr.text((W/2-dr.textlength(t,font=ft)/2,y+20),t,font=ft,fill='white',stroke_width=2,stroke_fill='black')
    for i,(lab) in enumerate([f'Kills {kills}',f'DMG +{dmg}%',f'SPD +{spd}%']):
        cx=20+i*112; c.alpha_composite(UI['chip'].resize((104,36)),(cx,128)); dr.text((cx+14,136),lab,font=F(15),fill='white',stroke_width=2,stroke_fill='black')
    c.alpha_composite(UI['red'].resize((92,40)),(W-106,126)); dr.text((W-86,135),'Leave',font=F(16),fill='white',stroke_width=2,stroke_fill='black')
    if banner:
        c.alpha_composite(UI['ribbon'].resize((380,64)),(80,190)); ft=F(22); dr.text((W/2-dr.textlength(banner,font=ft)/2,207),banner,font=ft,fill='white',stroke_width=2,stroke_fill=(10,20,60))
def badge(c,n,x,z):
    cx,by,s=proj(x,z); dr=ImageDraw.Draw(c); y=by-s*0.9-58
    c.alpha_composite(UI['chip'].resize((110,40)),(int(cx-55),int(y))); rp=round_portrait(P['lyra'],28); c.alpha_composite(rp,(int(cx-50),int(y+3)))
    dr.text((cx-12,y+9),f'×{n}',font=F(19),fill=(255,236,150),stroke_width=2,stroke_fill='black')
def shot(c,x0,z0,x1,z1,col=(255,230,140)):
    a=proj(x0,z0); b=proj(x1,z1); dr=ImageDraw.Draw(c); dr.line((a[0],a[1]-a[2]*1.0,b[0],b[1]-b[2]*1.0),fill=col+(220,),width=4); dr.ellipse((b[0]-5,b[1]-b[2]-5,b[0]+5,b[1]-b[2]+5),fill=(255,255,220))
def floater(c,t,x,z,big=False):
    cx,by,s=proj(x,z); dr=ImageDraw.Draw(c); ft=F(30 if big else 20); dr.text((cx-dr.textlength(t,font=ft)/2,by-s*2.6),t,font=ft,fill=(255,90,60) if big else (255,240,200),stroke_width=3,stroke_fill='black')
def burst(c,x,z):
    cx,by,s=proj(x,z); d=int(s*3.4); r=UI['ring'].resize((d,d)); c.alpha_composite(r,(int(cx-d/2),int(by-s*1.2-d/2)))
    for i in range(14):
        a=i/14*6.28; rr=s*1.3; sp=UI['spark'].resize((18,18)); c.alpha_composite(sp,(int(cx+math.cos(a)*rr-9),int(by-s*1.2+math.sin(a)*rr-9)))
def form(n):
    out=[];r=0;i=1
    while len(out)<n:
        for k in range(r+1):
            if len(out)>=n: break
            out.append(((k-r/2)*1.15,-(r+1)*1.15))
        r+=1
    return out
# Panel 1: early lane — boxes ahead
c1=scene(10); 
box(c1,'hero',-1.6,13,34,60,'+2',tagp='rowan'); box(c1,'weapon',1.8,15,110,110,'+25% DMG')
unit(c1,'bonequill',-1,24,70,70,foe=True); unit(c1,'bonequill',1.2,24.5,70,70,foe=True)
shot(c1,-0.3,4,-1.5,12.6); floater(c1,'-26',-1.6,13)
unit(c1,'lyra',-0.3,4,420,420,ring=(255,215,120)); badge(c1,1,-0.3,4)
hud(c1,10,0,0,0); dr=ImageDraw.Draw(c1); t='Drag or A / D to steer'; dr.text((W/2-dr.textlength(t,font=F(18))/2,H-60),t,font=F(18),fill='white',stroke_width=2,stroke_fill='black')
# Panel 2: mid run — box breaks, squad grows, nightfang charge
c2=scene(38)
burst(c2,0,9); paste(c2,CH['open'],*proj(0,9)[:2],proj(0,9)[2]*1.4); floater(c2,'+3 Lyra',0,9)
for i,(nx) in enumerate([-1.2,0.3,1.6]): unit(c2,'nightfang',nx,16+i,110-i*40,110,foe=True)
floater(c2,'-61',0.3,17,True)
slots=form(6)
for (ox,oz),nm in zip(reversed(slots),['lyra','lyra','rowan','lyra','rowan','sir_aldric']): unit(c2,nm,0.4+ox*0.9,4+oz*0.6+1.4,ring=(120,200,255))
unit(c2,'lyra',0.4,4.6,420,420,ring=(255,215,120)); badge(c2,7,0.4,4.6)
for (ox,oz) in slots[:3]: shot(c2,0.4+ox,4+oz+2,0.3,16.5,(180,230,255))
hud(c2,38,5,25,0,banner='+3 Lyra joined!')
# Panel 3: boss
c3=scene(140)
unit(c3,'ironhowl',0,15,2600,4200,foe=True,big=4.2)
cx,by,s=proj(0,15); ImageDraw.Draw(c3).text((cx-28,by-s*3.6-30),'BOSS',font=F(22),fill=(255,90,60),stroke_width=3,stroke_fill='black')
slots=form(9)
for (ox,oz),nm in zip(reversed(slots),['lyra','rowan','sir_aldric','oakenshield','rowan','lyra','lyra','rowan','sir_aldric']): unit(c3,nm,ox*0.9,4+oz*0.6+1.4)
unit(c3,'lyra',0,4.6,388,420,ring=(255,215,120)); badge(c3,10,0,4.6)
for (ox,oz) in slots[:5]: shot(c3,ox,4+oz+2,0,14.5,(180,230,255))
floater(c3,'-92',0.4,15.5,True)
hud(c3,142,22,60,30,banner='Ironhowl blocks the road!')
# Panel 4: result
c4=c3.copy().filter(ImageFilter.GaussianBlur(3)); dim=Image.new('RGBA',(W,H),(0,0,10,150)); c4.alpha_composite(dim)
r=UI['rays'].resize((620,620)); c4.alpha_composite(r,(W//2-310,120))
c4.alpha_composite(UI['panel'].resize((460,520)),(40,260)); c4.alpha_composite(UI['ribbon'].resize((400,80)),(70,230))
d=ImageDraw.Draw(c4); t='Victory!'; d.text((W/2-d.textlength(t,font=F(30))/2,248),t,font=F(30),fill='white',stroke_width=3,stroke_fill=(10,20,60))
for i,(k,v) in enumerate([('Foes defeated','24'),('Boxes broken','10 / 10'),('Squad','10'),('Distance','142 m')]):
    y=340+i*42; d.text((90,y),k,font=F(20,False),fill=(230,230,240)); d.text((450-d.textlength(v,font=F(20)),y),v,font=F(20),fill=(255,226,140))
gold_icon=img('art/hook/painted/icons/chest_closed.png')
c4.alpha_composite(UI['chip'].resize((180,56)),(80,530)); d.text((100,546),'2,380 gold',font=F(18),fill='white',stroke_width=2,stroke_fill='black')
rp=round_portrait(P['lyra'],40); c4.alpha_composite(UI['chip'].resize((180,56)),(280,530)); c4.alpha_composite(rp,(288,535)); d.text((338,546),'+10 shards',font=F(18),fill='white',stroke_width=2,stroke_fill='black')
c4.alpha_composite(UI['blue'].resize((170,62)),(80,640)); d.text((128,657),'Retry',font=F(22),fill='white',stroke_width=2,stroke_fill='black')
c4.alpha_composite(UI['green'].resize((170,62)),(290,640)); d.text((312,657),'Continue',font=F(22),fill='white',stroke_width=2,stroke_fill='black')
for i in range(30):
    random.seed(i); cf=UI['conf'].resize((26,26)).rotate(random.randint(0,360)); col=random.choice([(255,90,90),(255,220,90),(110,200,255),(140,240,120)]); tint=Image.new('RGBA',cf.size,col+(255,)); tint.putalpha(cf.getchannel('A')); cf=tint; c4.alpha_composite(cf,(random.randint(0,W-26),random.randint(150,900)))
# Panel 0: Play entry button
sheet=Image.new('RGBA',(W*4+50,H+130),(24,20,30,255)); sd=ImageDraw.Draw(sheet)
sd.text((20,18),'Runner — Walk & Fight (mock contact sheet, not a Unity capture)',font=F(30),fill=(255,226,140))
sd.text((20,60),'Play → painted Runner button  ·  Survival ▸ Runner: Walk & Fight  ·  steer: drag / A-D / arrows; squad walks and fires on its own',font=F(18,False),fill=(220,220,230))
c4i=ICON.resize((90,90)); sheet.alpha_composite(c4i,(W*4+50-110,10))
caps=['1  Shoot the boxes down (HP shown) — free pickups don\'t exist','2  Box breaks → followers join the formation','3  Boss Ironhowl blocks the road at 142 m','4  Result: gold + hero shards via celebration, Retry']
for i,cv in enumerate([c1,c2,c3,c4]):
    x=10+i*(W+10); sheet.alpha_composite(cv,(x,110)); sd.text((x+8,H+114-0),caps[i],font=F(15),fill='white')
sheet.convert('RGB').save('/workspace/runner/runner_slice.png'); print(sheet.size)
