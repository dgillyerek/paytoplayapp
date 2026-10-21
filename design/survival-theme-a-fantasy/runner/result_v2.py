from PIL import Image, ImageDraw, ImageFont, ImageEnhance
A='/workspace/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/art/hook/'
def img(p): return Image.open(A+p).convert('RGBA')
BOLD='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
def F(s): return ImageFont.truetype(BOLD,s)
UI={k:img('painted/ui/'+k+'.png') for k in ['panel_navy','ribbon_blue','button_green','button_blue']}
PLATE=img('ui/plate_navy.png')
GOLD=img('painted/icons/gold.png'); SHARD=img('painted/icons/hero_shard.png'); RAYS=img('painted/fx/rays.png')
CREAM=(237,230,209); VGOLD=(255,214,97); TITLE=(255,230,115)
def text(d,box,s,size,minsize,fill,align='c',stroke=2):
    x0,y0,x1,y1=box; w=x1-x0; h=y1-y0
    while size>minsize and (d.textlength(s,font=F(size))>w or size>h): size-=1
    f=F(size); tw=d.textlength(s,font=f); asc,desc=f.getmetrics(); th=asc
    x={'l':x0,'r':x1-tw,'c':x0+(w-tw)/2}[align]; y=y0+(h-th)/2-2
    d.text((x+0,y+stroke*1.6),s,font=f,fill=(0,0,0,150))
    d.text((x,y),s,font=f,fill=fill,stroke_width=stroke,stroke_fill=(10,8,15))
def screen(W,H,s):
    bg=Image.open('/workspace/runner/runner_3d_v2.png').convert('RGBA')
    r=max(W/bg.width,H/bg.height); bg=bg.resize((int(bg.width*r),int(bg.height*r))); bg=bg.crop((0,0,W,H))
    c=Image.alpha_composite(bg,Image.new('RGBA',(W,H),(5,8,13,200)))
    def R(ax0,ay0,ax1,ay1,par):  # unity anchors (y from bottom) inside parent pixel rect
        px0,py0,px1,py1=par; pw=px1-px0; ph=py1-py0
        return (px0+ax0*pw, py1-ay1*ph, px0+ax1*pw, py1-ay0*ph)
    def put(im,b):
        b=tuple(int(v) for v in b); c.alpha_composite(im.resize((max(1,b[2]-b[0]),max(1,b[3]-b[1])),Image.LANCZOS),(b[0],b[1]))
    full=(0,0,W,H)
    rays=RAYS.copy(); rays.putalpha(rays.getchannel('A').point(lambda a:int(a*0.55))); put(rays,R(-0.2,0.42,1.2,1.02,full))
    panel=R(0.06,0.16,0.94,0.79,full); put(UI['panel_navy'],panel)
    rib=R(-0.06,0.86,1.06,1.04,panel); put(UI['ribbon_blue'],rib)
    d=ImageDraw.Draw(c)
    text(d,(rib[0]+40*s,rib[1],rib[2]-40*s,rib[3]-8*s),'VICTORY!',int(58*s),20,TITLE,stroke=3)
    rows=[('Enemies defeated','24'),('Boxes broken','10 / 10'),('Biggest squad','×15'),('Distance','139 m')]
    for i,(k,v) in enumerate(rows):
        y=0.79-i*0.07
        text(d,R(0.08,y-0.032,0.64,y+0.032,panel),k,int(34*s),int(20*s),CREAM,'l')
        text(d,R(0.64,y-0.032,0.92,y+0.032,panel),v,int(36*s),int(20*s),VGOLD,'r')
    for x,ic,amt in [(0.34,GOLD,'+1,240'),(0.66,SHARD,'+12')]:
        sl=R(x-0.14,0.375,x+0.14,0.52,panel)
        if PLATE: put(PLATE,sl)
        else: d.rounded_rectangle(sl,int(12*s),fill=(28,40,70,255),outline=(200,170,90),width=2)
        put(ic,R(0.2,0.38,0.8,0.98,sl)); text(d,R(0,0.02,1,0.4,sl),amt,int(28*s),12,(255,255,255))
    text(d,R(0.05,0.325,0.95,0.365,panel),'Level 2 unlocked: Bramble Pass',int(24*s),12,TITLE)
    def row(btns,y0,y1):
        left=0.18 if len(btns)==1 else 0.06; w=(1-2*left)/len(btns)
        for i,(lab,key) in enumerate(btns):
            b=R(left+i*w+0.015,y0,left+(i+1)*w-0.015,y1,panel); put(UI[key],b)
            bw=b[2]-b[0]; bh=b[3]-b[1]
            text(d,(b[0]+bw*0.08,b[1]+bh*0.14,b[2]-bw*0.08,b[3]-bh*0.14),lab,int(34*s),int(16*s),(255,255,255))
    row([('Next level','button_green'),('Continue','button_green')],0.165,0.28)
    row([('Levels','button_blue'),('Retry','button_blue')],0.035,0.15)
    return c
a=screen(540,960,1.0); b=screen(432,960,0.8)  # 9:16 and a narrow 20:9 phone
out=Image.new('RGBA',(540+432+24,960+40),(20,20,24,255)); out.alpha_composite(a,(0,40)); out.alpha_composite(b,(564,40))
d=ImageDraw.Draw(out); d.text((10,10),'9:16 phone',font=F(18),fill='white'); d.text((574,10),'20:9 phone (narrow)',font=F(18),fill='white')
out.convert('RGB').save('/workspace/runner/result_screen_v2.png')
