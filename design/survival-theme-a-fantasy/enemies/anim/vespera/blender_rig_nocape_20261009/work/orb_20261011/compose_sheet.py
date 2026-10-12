from PIL import Image, ImageDraw, ImageFont, ImageFilter
import numpy as np, sys, os
D=sys.argv[1]; OUT=sys.argv[2]; S=sys.argv[3].split(';'); title=sys.argv[4]
F=lambda s,b=False: ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans%s.ttf'%('-Bold' if b else ''),s)
def comp(p):
    im=Image.open(p).convert('RGBA'); w,h=im.size
    t=np.linspace(0,1,h)[:,None]; bg=(np.array([168,165,178])*(1-t)+np.array([118,116,128])*t)[:,None,:].repeat(w,1)
    B=Image.fromarray(bg.astype('uint8'),'RGB'); B.paste(im,(0,0),im)
    a=np.asarray(im).astype(float)/255; rgb=a[...,:3]; m=((rgb.max(-1)>0.62)&(rgb[...,2]>=rgb[...,1]+0.18)&(rgb[...,2]>0.6)).astype(float)*a[...,3]
    col=Image.fromarray((rgb*m[...,None]*255).astype('uint8'),'RGB')
    g=np.asarray(col.filter(ImageFilter.GaussianBlur(4))).astype(float)/255*1.0+np.asarray(col.filter(ImageFilter.GaussianBlur(11))).astype(float)/255*0.8
    return Image.fromarray((np.clip(np.asarray(B).astype(float)/255+g,0,1)*255).astype('uint8'))
lab={'attack:0':'rest f0','attack:4':'orb forms f4','attack:20':'RELEASE f20','attack:16':'circle done f16','attack:19':'push f19'}
cells=[]
for s in S:
    t,f=s.split(':'); cells.append((s,[comp(f'{D}/{v}_{t}_f{int(f):02d}.png') for v in ('front','side')]))
w,h=cells[0][1][0].size; n=len(cells)
M=Image.new('RGB',(w*n,h*2+70),(22,20,28)); d=ImageDraw.Draw(M)
d.text((10,6),title,font=F(18,True),fill=(235,225,255))
for i,(s,ims) in enumerate(cells):
    t,f=s.split(':'); L=lab.get(s,f'{t} f{int(f)}')
    d.text((i*w+6,40),L,font=F(14,True),fill=(255,230,120) if t=='attack' else (150,220,255))
    for j,im in enumerate(ims): M.paste(im,(i*w,62+j*h))
d.text((4,62+4),'front',font=F(13),fill=(30,30,40)); d.text((4,62+h+4),'side',font=F(13),fill=(30,30,40))
M.save(OUT.replace('.jpg','.png'))
q=88
while True:
    M.save(OUT,quality=q,optimize=True)
    if os.path.getsize(OUT)<700_000 or q<40: break
    q-=8
print(M.size,os.path.getsize(OUT))
