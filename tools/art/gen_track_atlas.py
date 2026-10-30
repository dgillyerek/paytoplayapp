# Kingdom Path detail-pass atlas generator (PIL, procedural). Output: track_atlas.png + track_atlas.txt
import math, random, numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops
random.seed(7); np.random.seed(7)
SS=2
def canvas(w,h): return Image.new("RGBA",(w*SS,h*SS),(0,0,0,0))
def fin(im): return im.resize((im.width//SS,im.height//SS),Image.LANCZOS)
def noise(w,h,scale=8,oct=4):
    acc=np.zeros((h,w))
    amp=1;tot=0
    for o in range(oct):
        s=max(1,scale*(2**o)); g=np.random.rand(h//s+2,w//s+2)
        im=Image.fromarray((g*255).astype(np.uint8)).resize((w+ s*2,h+s*2),Image.BICUBIC).crop((0,0,w,h))
        acc+=np.asarray(im)/255*amp; tot+=amp; amp*=0.5
    return acc/tot
def tex(w,h,c0,c1,scale=6,cracks=0):
    n=noise(w,h,scale)
    a=np.array(c0,float); b=np.array(c1,float)
    rgb=a[None,None,:]*(1-n[...,None])+b[None,None,:]*n[...,None]
    im=Image.fromarray(rgb.clip(0,255).astype(np.uint8),"RGB").convert("RGBA")
    if cracks:
        d=ImageDraw.Draw(im)
        for _ in range(cracks):
            x,y=random.randrange(w),random.randrange(h)
            pts=[(x,y)]
            for _ in range(5): x+=random.randint(-14,14); y+=random.randint(-14,14); pts.append((x,y))
            d.line(pts,fill=tuple(int(v*0.55) for v in c0)+(255,),width=max(1,SS))
    return im
def rr_mask(w,h,r,inset=0):
    m=Image.new("L",(w,h),0); ImageDraw.Draw(m).rounded_rectangle((inset,inset,w-1-inset,h-1-inset),r,fill=255); return m
def grad_v(w,h,top,bot):
    t=np.linspace(0,1,h)[:,None,None]; a=np.array(top,float); b=np.array(bot,float)
    arr=a*(1-t)+b*t; arr=np.repeat(arr,w,axis=1)
    return Image.fromarray(arr.clip(0,255).astype(np.uint8),"RGB" if len(top)==3 else "RGBA").convert("RGBA")
def metal_ring(im,inset,thick,r,light,dark):
    w,h=im.size
    outer=rr_mask(w,h,r,inset); inner=rr_mask(w,h,max(2,r-thick),inset+thick)
    ring=ImageChops.subtract(outer,inner)
    g=grad_v(w,h,light,dark)
    im.paste(g,(0,0),ring)
    hl=ImageChops.subtract(rr_mask(w,h,r,inset),rr_mask(w,h,r,inset+max(2,thick//4)))
    im.paste(Image.new("RGBA",(w,h),tuple(min(255,int(c*1.25)) for c in light)+(255,)),(0,0),hl.point(lambda v:v*0.8))
    sh=ImageChops.subtract(rr_mask(w,h,max(2,r-thick),inset+thick-SS*2),inner)
    im.paste(Image.new("RGBA",(w,h),(0,0,0,255)),(0,0),sh.point(lambda v:v*0.6))
GOLD=((255,231,150),(176,112,30)); SILVER=((245,248,255),(120,132,160)); BRONZE=((228,170,110),(110,62,28))
def filigree(d,cx,cy,s,col,flipx=1,flipy=1,width=3):
    # curly scroll ornament in a corner
    for k in range(3):
        pts=[]
        for i in range(40):
            t=i/39*math.pi*1.6
            rad=s*(0.55-0.15*k)*(1-t/(math.pi*2.2))
            pts.append((cx+flipx*(rad*math.cos(t+k*0.5)+s*0.15*k),cy+flipy*(rad*math.sin(t+k*0.5))))
        d.line(pts,fill=col,width=width*SS,joint="curve")
    d.ellipse((cx-s*0.12,cy-s*0.12,cx+s*0.12,cy+s*0.12),fill=col)
def cell(fill0,fill1,metal,name,size=160,r=26,thick=12,texscale=5,cracks=0,gems=None):
    S=size*SS; im=canvas(size,size)
    body=tex(S,S,fill0,fill1,texscale*SS,cracks)
    # bevel light top-left / dark bottom-right
    sh=grad_v(S,S,(255,255,255,70),(0,0,0,90))
    body=Image.alpha_composite(body,sh)
    im.paste(body,(0,0),rr_mask(S,S,r*SS,2*SS))
    metal_ring(im,2*SS,thick*SS,r*SS,*metal)
    d=ImageDraw.Draw(im)
    if gems:
        for (x,y) in [(r*SS*0.55,r*SS*0.55),(S-r*SS*0.55,r*SS*0.55),(r*SS*0.55,S-r*SS*0.55),(S-r*SS*0.55,S-r*SS*0.55)]:
            d.ellipse((x-7*SS,y-7*SS,x+7*SS,y+7*SS),fill=metal[1]+(255,)); d.ellipse((x-5*SS,y-5*SS,x+5*SS,y+5*SS),fill=gems+(255,)); d.ellipse((x-3*SS,y-4*SS,x,y-1*SS),fill=(255,255,255,200))
    return fin(im)
out={}
# column backdrops (tall-ish, sliced)
out["col_free"]=(cell((92,88,82),(140,134,124),BRONZE,"",160,28,13,4,12,(150,110,70)),34)
out["col_premium"]=(cell((18,38,104),(44,80,170),SILVER,"",160,28,13,6,0,(120,190,255)),34)
out["col_royal"]=(cell((92,14,34),(150,30,60),GOLD,"",160,28,13,6,0,(255,60,90)),34)
# reward cells (chunky beveled)
out["cell_free"]=(cell((70,66,60),(112,106,96),BRONZE,"",128,22,9,3,5),30)
out["cell_premium"]=(cell((14,30,80),(36,64,140),SILVER,"",128,22,9,4),30)
out["cell_royal"]=(cell((70,10,28),(126,24,50),GOLD,"",128,22,9,4),30)
out["cell_claimed"]=(cell((40,40,44),(64,64,70),((150,150,150),(70,70,70)),"",128,22,9,4),30)
# parchment backdrop (full sheet, stretched)
def parchment():
    w,h=224,352; S=(w*SS,h*SS)
    im=tex(S[0],S[1],(196,164,110),(236,214,168),10*SS,0)
    vig=Image.new("L",S,0); ImageDraw.Draw(vig).ellipse((-S[0]*0.2,-S[1]*0.1,S[0]*1.2,S[1]*1.1),fill=255)
    vig=vig.filter(ImageFilter.GaussianBlur(60*SS))
    dark=Image.new("RGBA",S,(90,58,24,255)); im=Image.composite(im,dark,vig)
    ov=Image.new("RGBA",S,(0,0,0,0)); d=ImageDraw.Draw(ov)
    for _ in range(14):
        x,y=random.randrange(S[0]),random.randrange(S[1]); rr=random.randint(14,46)*SS
        d.ellipse((x-rr,y-rr,x+rr,y+rr),outline=(140,96,44,34),width=3*SS)
    for _ in range(40):
        x,y=random.randrange(S[0]),random.randrange(S[1]); d.line([(x,y),(x+random.randint(-40,40)*SS,y+random.randint(-4,4)*SS)],fill=(120,84,40,22),width=SS)
    im=Image.alpha_composite(im,ov.filter(ImageFilter.GaussianBlur(SS)))
    return fin(im)
out["parchment"]=(parchment(),0)
out["stone"]=(fin(Image.alpha_composite(tex(128*SS,128*SS,(52,54,62),(96,98,108),6*SS,30),grad_v(128*SS,128*SS,(255,255,255,20),(0,0,0,60)))),0)
# gold filigree frame (transparent centre, sliced) with corner ornaments
def frame():
    s=256; S=s*SS; im=canvas(s,s)
    metal_ring(im,6*SS,18*SS,34*SS,*GOLD)
    d=ImageDraw.Draw(im)
    d.rounded_rectangle((30*SS,30*SS,S-30*SS,S-30*SS),20*SS,outline=(200,150,50,255),width=2*SS)
    for fx,fy,cx,cy in [(1,1,0,0),(-1,1,S,0),(1,-1,0,S),(-1,-1,S,S)]:
        filigree(d,cx+fx*44*SS,cy+fy*44*SS,40*SS,(255,214,110,255),fx,fy,3)
        d.polygon([(cx+fx*4*SS,cy+fy*4*SS),(cx+fx*40*SS,cy+fy*14*SS),(cx+fx*14*SS,cy+fy*40*SS)],fill=(255,226,140,255),outline=(140,90,20,255))
        d.ellipse((cx+fx*18*SS-9*SS,cy+fy*18*SS-9*SS,cx+fx*18*SS+9*SS,cy+fy*18*SS+9*SS),fill=(200,20,40,255),outline=(255,230,150,255),width=2*SS)
    # mid-edge cartouches
    for (x,y) in [(S/2,12*SS),(S/2,S-12*SS),(12*SS,S/2),(S-12*SS,S/2)]:
        d.ellipse((x-10*SS,y-10*SS,x+10*SS,y+10*SS),fill=(255,226,140,255),outline=(140,90,20,255),width=2*SS)
    return fin(im)
out["frame_gold"]=(frame().resize((192,192),Image.LANCZOS),69)
# header banner (sliced crimson ribbon w/ tails) and crest
def ribbon(c0,c1,name):
    w,h=448,120; S=(w*SS,h*SS); im=Image.new("RGBA",S,(0,0,0,0)); d=ImageDraw.Draw(im)
    for side in (0,1):
        x0=0 if side==0 else S[0]; f=1 if side==0 else -1
        d.polygon([(x0,20*SS),(x0+f*90*SS,20*SS),(x0+f*90*SS,108*SS),(x0,108*SS),(x0+f*30*SS,64*SS)],fill=tuple(int(c*0.6) for c in c0)+(255,))
    body=Image.alpha_composite(tex(S[0],S[1],c0,c1,8*SS),grad_v(S[0],S[1],(255,255,255,60),(0,0,0,90)))
    m=Image.new("L",S,0); ImageDraw.Draw(m).polygon([(60*SS,6*SS),(S[0]-60*SS,6*SS),(S[0]-60*SS,96*SS),(60*SS,96*SS)],fill=255)
    im.paste(body,(0,0),m)
    d=ImageDraw.Draw(im)
    d.line([(60*SS,14*SS),(S[0]-60*SS,14*SS)],fill=(255,214,110,255),width=4*SS)
    d.line([(60*SS,88*SS),(S[0]-60*SS,88*SS)],fill=(255,214,110,255),width=4*SS)
    for x in range(80*SS,S[0]-60*SS,24*SS): d.ellipse((x-2*SS,49*SS,x+2*SS,53*SS),fill=(255,214,110,90))
    return fin(im)
out["ribbon_crimson"]=(ribbon((150,20,40),(200,40,60),"r"),(100,30,100,30))
out["ribbon_blue"]=(ribbon((20,50,140),(50,90,200),"b"),(100,30,100,30))
out["ribbon_stone"]=(ribbon((84,80,74),(130,124,114),"s"),(100,30,100,30))
def shield(c0,c1,metal,crown=True,size=160):
    S=size*SS; im=canvas(size,size); d=ImageDraw.Draw(im)
    pts=[(S*0.18,S*0.22),(S*0.82,S*0.22),(S*0.82,S*0.55),(S*0.5,S*0.95),(S*0.18,S*0.55)]
    d.polygon(pts,fill=metal[1]+(255,))
    inner=[(S*0.24,S*0.27),(S*0.76,S*0.27),(S*0.76,S*0.54),(S*0.5,S*0.87),(S*0.24,S*0.54)]
    m=Image.new("L",(S,S),0); ImageDraw.Draw(m).polygon(inner,fill=255)
    im.paste(Image.alpha_composite(tex(S,S,c0,c1,6*SS),grad_v(S,S,(255,255,255,70),(0,0,0,80))),(0,0),m)
    d.line(pts+[pts[0]],fill=metal[0]+(255,),width=4*SS)
    # lion-ish chevron + star
    d.polygon([(S*0.32,S*0.6),(S*0.5,S*0.44),(S*0.68,S*0.6),(S*0.68,S*0.68),(S*0.5,S*0.52),(S*0.32,S*0.68)],fill=metal[0]+(255,))
    star(d,S*0.5,S*0.36,S*0.07,metal[0]+(255,))
    if crown: crown_draw(d,S*0.5,S*0.13,S*0.42)
    return fin(im)
def star(d,cx,cy,r,col,n=5,inner=0.45):
    pts=[]
    for i in range(n*2):
        a=-math.pi/2+i*math.pi/n; rr=r if i%2==0 else r*inner
        pts.append((cx+rr*math.cos(a),cy+rr*math.sin(a)))
    d.polygon(pts,fill=col)
def crown_draw(d,cx,cy,w):
    h=w*0.5; x0=cx-w/2
    pts=[(x0,cy+h*0.5),(x0,cy-h*0.3),(x0+w*0.25,cy+h*0.1),(cx,cy-h*0.55),(x0+w*0.75,cy+h*0.1),(x0+w,cy-h*0.3),(x0+w,cy+h*0.5)]
    d.polygon(pts,fill=(255,206,70,255),outline=(140,80,10,255))
    d.rectangle((x0,cy+h*0.35,x0+w,cy+h*0.6),fill=(220,160,40,255),outline=(140,80,10,255))
    for i,x in enumerate([x0,cx,x0+w]):
        y=cy-h*0.3 if i!=1 else cy-h*0.55
        d.ellipse((x-w*0.06,y-w*0.06,x+w*0.06,y+w*0.06),fill=(255,240,180,255))
    for x,c in [(cx-w*0.25,(40,90,220)),(cx,(220,30,50)),(cx+w*0.25,(40,180,90))]:
        d.ellipse((x-w*0.05,cy+h*0.38,x+w*0.05,cy+h*0.58),fill=c+(255,))
out["crest_kingdom"]=(shield((150,20,40),(200,50,70),GOLD,True),0)
out["crest_free"]=(shield((84,80,74),(130,124,114),BRONZE,False,128),0)
out["crest_premium"]=(shield((20,50,140),(60,100,210),SILVER,False,128),0)
out["crest_royal"]=(shield((110,16,60),(170,40,90),GOLD,True,128),0)
def crown_icon():
    im=canvas(96,72); d=ImageDraw.Draw(im); crown_draw(d,48*SS,40*SS,84*SS); return fin(im)
out["crown"]=(crown_icon(),0)
# medallions
def medallion(on):
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im)
    metal=GOLD if on else ((170,170,176),(80,80,88))
    g=grad_v(S,S,metal[0]+(255,),metal[1]+(255,))
    m=Image.new("L",(S,S),0); md=ImageDraw.Draw(m)
    # scalloped rim
    for i in range(16):
        a=i/16*math.pi*2; x=S/2+S*0.40*math.cos(a); y=S/2+S*0.40*math.sin(a)
        md.ellipse((x-S*0.08,y-S*0.08,x+S*0.08,y+S*0.08),fill=255)
    md.ellipse((S*0.1,S*0.1,S*0.9,S*0.9),fill=255)
    im.paste(g,(0,0),m)
    inner=(30,50,120) if on else (50,52,60)
    d.ellipse((S*0.2,S*0.2,S*0.8,S*0.8),fill=metal[1]+(255,))
    m2=Image.new("L",(S,S),0); ImageDraw.Draw(m2).ellipse((S*0.23,S*0.23,S*0.77,S*0.77),fill=255)
    im.paste(Image.alpha_composite(tex(S,S,inner,tuple(min(255,c+50) for c in inner),5*SS),grad_v(S,S,(255,255,255,80),(0,0,0,60))),(0,0),m2)
    d.arc((S*0.24,S*0.24,S*0.76,S*0.76),200,320,fill=(255,255,255,140),width=3*SS)
    return fin(im)
out["medallion_on"]=(medallion(True),0); out["medallion_off"]=(medallion(False),0)
# road / spine (vertical, stretched)
def road():
    w,h=64,192; S=(w*SS,h*SS)
    im=Image.new("RGBA",S,(0,0,0,0))
    body=tex(S[0],S[1],(120,90,50),(170,130,80),4*SS,0)
    m=Image.new("L",S,0); ImageDraw.Draw(m).rectangle((14*SS,0,50*SS,S[1]),fill=255)
    im.paste(body,(0,0),m); d=ImageDraw.Draw(im)
    for x in (14*SS,50*SS): d.line([(x,0),(x,S[1])],fill=(255,206,90,255),width=4*SS)
    for y in range(0,S[1],32*SS): d.rounded_rectangle((20*SS,y+4*SS,44*SS,y+26*SS),5*SS,fill=(150,140,128,255),outline=(80,70,60,255),width=SS)
    return fin(im)
out["road"]=(road(),0)
# resource icons
def icon_coin():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im)
    for k,(ox,oy) in enumerate([(-22,18),(20,14),(0,-6)]):
        cx,cy=S/2+ox*SS,S/2+oy*SS; r=36*SS
        d.ellipse((cx-r,cy-r+8*SS,cx+r,cy+r+8*SS),fill=(140,80,10,255))
        d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=(255,200,50,255),outline=(170,110,20,255),width=3*SS)
        d.ellipse((cx-r*0.7,cy-r*0.7,cx+r*0.7,cy+r*0.7),outline=(200,140,30,255),width=3*SS)
        crown_draw(d,cx,cy,r*0.8)
        d.arc((cx-r*0.9,cy-r*0.9,cx+r*0.9,cy+r*0.9),200,260,fill=(255,250,210,255),width=3*SS)
    return fin(im)
def icon_wheat():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im)
    for ang in (-18,0,18):
        a=math.radians(ang); bx,by=S/2,S*0.92; tx,ty=bx+math.sin(a)*S*0.75,by-math.cos(a)*S*0.75
        d.line([(bx,by),(tx,ty)],fill=(160,120,40,255),width=4*SS)
        for i in range(6):
            t=0.45+i*0.09; px,py=bx+(tx-bx)*t,by+(ty-by)*t
            for side in (-1,1):
                ex=px+side*math.cos(a)*10*SS; ey=py+side*math.sin(a)*10*SS-6*SS
                d.ellipse((ex-7*SS,ey-11*SS,ex+7*SS,ey+11*SS),fill=(240,196,80,255),outline=(170,120,30,255),width=2*SS)
    d.rounded_rectangle((S*0.36,S*0.68,S*0.64,S*0.78),4*SS,fill=(170,40,40,255),outline=(110,20,20,255),width=2*SS)
    return fin(im)
def icon_gem():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im); cx=S/2
    top=[(cx-44*SS,44*SS),(cx-24*SS,20*SS),(cx+24*SS,20*SS),(cx+44*SS,44*SS)]; bot=(cx,112*SS)
    d.polygon(top+[bot],fill=(40,120,230,255))
    d.polygon([(cx-44*SS,44*SS),(cx+44*SS,44*SS),bot],fill=(30,90,200,255))
    d.polygon([(cx-24*SS,20*SS),(cx,44*SS),(cx+24*SS,20*SS)],fill=(140,210,255,255))
    d.polygon([(cx-44*SS,44*SS),(cx-24*SS,20*SS),(cx,44*SS)],fill=(90,170,250,255))
    d.polygon([(cx,44*SS),(cx+24*SS,20*SS),(cx+44*SS,44*SS)],fill=(60,140,240,255))
    d.polygon([(cx-20*SS,44*SS),(cx,112*SS),(cx-44*SS,44*SS)],fill=(70,150,245,255))
    d.line(top+[bot,top[0]],fill=(15,50,130,255),width=2*SS)
    star(d,cx-18*SS,34*SS,9*SS,(255,255,255,230),4,0.3)
    return fin(im)
def icon_tome():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im)
    d.polygon([(24*SS,34*SS),(100*SS,24*SS),(108*SS,94*SS),(32*SS,106*SS)],fill=(240,228,200,255))
    d.polygon([(18*SS,30*SS),(96*SS,18*SS),(102*SS,88*SS),(26*SS,100*SS)],fill=(120,40,150,255),outline=(60,15,80,255))
    d.polygon([(18*SS,30*SS),(30*SS,28*SS),(38*SS,98*SS),(26*SS,100*SS)],fill=(80,20,100,255))
    for t in (0.1,0.9):
        d.line([(18*SS+78*SS*t,30*SS-12*SS*t),(26*SS+76*SS*t,100*SS-12*SS*t)],fill=(255,210,90,255),width=3*SS)
    star(d,64*SS,58*SS,18*SS,(255,214,90,255),6,0.5)
    d.ellipse((58*SS,52*SS,70*SS,64*SS),fill=(120,230,255,255))
    return fin(im)
def icon_banner():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im)
    d.line([(30*SS,10*SS),(30*SS,120*SS)],fill=(120,80,40,255),width=6*SS)
    d.ellipse((24*SS,4*SS,36*SS,16*SS),fill=(255,206,70,255))
    d.polygon([(33*SS,18*SS),(104*SS,18*SS),(104*SS,96*SS),(68*SS,80*SS),(33*SS,96*SS)],fill=(190,30,50,255),outline=(110,10,25,255))
    d.line([(33*SS,24*SS),(104*SS,24*SS)],fill=(255,206,70,255),width=3*SS)
    d.line([(52*SS,50*SS),(68*SS,40*SS),(84*SS,50*SS),(84*SS,62*SS),(68*SS,52*SS),(52*SS,62*SS)],fill=(255,206,70,255),width=4*SS)
    for x in (88*SS,100*SS): d.line([(x,90*SS),(x+4*SS,108*SS)],fill=(255,206,70,255),width=2*SS)
    return fin(im)
def icon_hourglass():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im); cx=S/2
    d.polygon([(cx-30*SS,24*SS),(cx+30*SS,24*SS),(cx+4*SS,64*SS),(cx+30*SS,104*SS),(cx-30*SS,104*SS),(cx-4*SS,64*SS)],fill=(200,230,255,170),outline=(230,245,255,255))
    d.polygon([(cx-18*SS,40*SS),(cx+18*SS,40*SS),(cx,60*SS)],fill=(240,190,80,255))
    d.polygon([(cx-26*SS,100*SS),(cx+26*SS,100*SS),(cx,80*SS)],fill=(240,190,80,255))
    d.line([(cx,60*SS),(cx,84*SS)],fill=(240,190,80,255),width=2*SS)
    for y in (16*SS,104*SS): d.rounded_rectangle((cx-40*SS,y,cx+40*SS,y+10*SS),4*SS,fill=(150,90,40,255),outline=(255,206,70,255),width=2*SS)
    for x in (cx-36*SS,cx+32*SS): d.rectangle((x,24*SS,x+4*SS,104*SS),fill=(150,90,40,255))
    return fin(im)
def shard_frame():
    # crystal shard with a hole where the hero face shows through
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im); cx=S/2
    outer=[(cx,4*SS),(cx+50*SS,40*SS),(cx+40*SS,108*SS),(cx,124*SS),(cx-40*SS,108*SS),(cx-50*SS,40*SS)]
    d.polygon(outer,fill=(150,80,230,255),outline=(255,214,110,255))
    d.line(outer+[outer[0]],fill=(255,214,110,255),width=4*SS)
    d.polygon([(cx,4*SS),(cx+50*SS,40*SS),(cx,30*SS)],fill=(210,170,255,255))
    d.ellipse((cx-32*SS,34*SS,cx+32*SS,98*SS),fill=(0,0,0,0))
    d.ellipse((cx-34*SS,32*SS,cx+34*SS,100*SS),outline=(255,214,110,255),width=4*SS)
    return fin(im)
def face_mask():
    s=128; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im); d.ellipse((S/2-32*SS,34*SS,S/2+32*SS,98*SS),fill=(255,255,255,255)); return fin(im)
out["icon_coin"]=(icon_coin(),0); out["icon_wheat"]=(icon_wheat(),0); out["icon_gem"]=(icon_gem(),0)
out["icon_tome"]=(icon_tome(),0); out["icon_banner"]=(icon_banner(),0); out["icon_hourglass"]=(icon_hourglass(),0)
out["shard_frame"]=(shard_frame(),0); out["shard_mask"]=(face_mask(),0)
# padlock + chains
def padlock():
    s=96; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im)
    d.arc((26*SS,8*SS,70*SS,58*SS),180,360,fill=(170,170,180,255),width=9*SS)
    d.line([(26*SS+4*SS,33*SS),(26*SS+4*SS,46*SS)],fill=(170,170,180,255),width=9*SS); d.line([(66*SS,33*SS),(66*SS,46*SS)],fill=(170,170,180,255),width=9*SS)
    g=grad_v(S,S,(255,220,110,255),(160,100,20,255)); m=rr_mask(S,S,10*SS,0); m=Image.new("L",(S,S),0); ImageDraw.Draw(m).rounded_rectangle((16*SS,40*SS,80*SS,90*SS),10*SS,fill=255)
    im.paste(g,(0,0),m); d=ImageDraw.Draw(im)
    d.rounded_rectangle((16*SS,40*SS,80*SS,90*SS),10*SS,outline=(110,60,10,255),width=3*SS)
    d.ellipse((42*SS,54*SS,54*SS,66*SS),fill=(50,30,10,255)); d.polygon([(45*SS,62*SS),(51*SS,62*SS),(53*SS,80*SS),(43*SS,80*SS)],fill=(50,30,10,255))
    return fin(im)
def chains():
    w,h=192,96; S=(w*SS,h*SS); im=Image.new("RGBA",S,(0,0,0,0)); d=ImageDraw.Draw(im)
    for (x0,y0,x1,y1) in [(0,0,S[0],S[1]),(0,S[1],S[0],0)]:
        n=11
        for i in range(n):
            t=i/(n-1); x=x0+(x1-x0)*t; y=y0+(y1-y0)*t
            ang=math.atan2(y1-y0,x1-x0); lw=12*SS; lh=7*SS
            if i%2: lh,lw=4*SS,12*SS
            box=[(x+math.cos(ang)*dx-math.sin(ang)*dy, y+math.sin(ang)*dx+math.cos(ang)*dy) for dx,dy in [(-lw,-lh),(lw,-lh),(lw,lh),(-lw,lh)]]
            d.polygon(box,outline=(60,60,66,255),fill=None); d.line(box+[box[0]],fill=(150,150,160,255),width=4*SS)
    return fin(im)
out["padlock"]=(padlock(),0); out["chains"]=(chains(),0)
# fx: glow, shine, sparkle, burst
def glow():
    s=128; y,x=np.mgrid[0:s,0:s]; r=np.hypot(x-s/2+0.5,y-s/2+0.5)/(s/2); a=np.clip(1-r,0,1)**1.8
    arr=np.zeros((s,s,4),np.uint8); arr[...,:3]=255; arr[...,3]=(a*255).astype(np.uint8); return Image.fromarray(arr,"RGBA")
def shine():
    w,h=48,192; x=np.linspace(-1,1,w)[None,:]; a=np.exp(-(x*2.6)**2)*np.ones((h,1))
    arr=np.zeros((h,w,4),np.uint8); arr[...,:3]=255; arr[...,3]=(a*200).astype(np.uint8); return Image.fromarray(arr,"RGBA")
def sparkle():
    s=64; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im); star(d,S/2,S/2,S*0.48,(255,255,255,255),4,0.16); star(d,S/2,S/2,S*0.22,(255,255,255,255),4,0.5)
    return fin(im).filter(ImageFilter.GaussianBlur(0.6))
def burst():
    s=192; y,x=np.mgrid[0:s,0:s]-s/2+0.5; ang=np.arctan2(y,x); r=np.hypot(x,y)/(s/2)
    rays=(np.cos(ang*12)>0.55).astype(float)*np.clip(1-r,0,1)**1.2 + np.clip(1-r*2.2,0,1)
    arr=np.zeros((s,s,4),np.uint8); arr[...,:3]=255; arr[...,3]=(np.clip(rays,0,1)*255).astype(np.uint8); return Image.fromarray(arr,"RGBA")
def check():
    s=64; S=s*SS; im=canvas(s,s); d=ImageDraw.Draw(im); d.ellipse((2*SS,2*SS,S-2*SS,S-2*SS),fill=(40,150,60,255),outline=(255,214,110,255),width=4*SS)
    d.line([(18*SS,34*SS),(28*SS,44*SS),(48*SS,20*SS)],fill=(255,255,255,255),width=7*SS,joint="curve"); return fin(im)
out["glow"]=(glow(),0); out["shine"]=(shine(),0); out["sparkle"]=(sparkle(),0); out["burst"]=(burst(),0); out["check"]=(check(),0)
# pack: skyline bottom-left into 1024 wide, 2px padding
items=sorted(out.items(),key=lambda kv:(-kv[1][0].width*kv[1][0].height))
W=1024; pad=2; sky=[0]*W; atlas=Image.new("RGBA",(W,2048),(0,0,0,0)); lines=[]; H=0
for name,(img,sl) in items:
    w,h=img.width+pad,img.height+pad; best=None
    for x in range(0,W-w+1,2):
        y=max(sky[x:x+w])
        if best is None or y<best[1]: best=(x,y)
    x,y=best
    for i in range(x,x+w): sky[i]=y+h
    atlas.paste(img,(x,y)); H=max(H,y+h)
    l=b=r=t=(sl if isinstance(sl,int) else 0)
    if isinstance(sl,tuple): l,t,r,b=sl
    lines.append(f"{name} {x} {y} {img.width} {img.height} {l} {b} {r} {t}")
Hp=1<<(H-1).bit_length()
atlas=atlas.crop((0,0,W,Hp))
# rows are stored top-left origin; loader flips to Unity bottom-left
import sys
dst=sys.argv[1]
atlas.save(dst+"/track_atlas.png",optimize=True)
open(dst+"/track_atlas.txt","w").write("# Kingdom Path atlas: name x y w h (top-left px) border l b r t\n"+"\n".join(lines)+"\n")
print(W,Hp,len(lines))
