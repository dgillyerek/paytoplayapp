import sys,os
from PIL import Image,ImageDraw,ImageFont
d,frames,title,out,contact=sys.argv[1],[int(x) for x in sys.argv[2].split(',')],sys.argv[3],sys.argv[4],int(sys.argv[5])
W,H=420,560; pad=36
try: font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18); big=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',24)
except Exception: font=big=ImageFont.load_default()
img=Image.new('RGB',(W*len(frames),pad+2*(H+pad)),(24,26,30)); dr=ImageDraw.Draw(img)
dr.text((10,6),title,fill=(235,235,240),font=big)
for r,v in enumerate(('side','q34')):
    y=pad+r*(H+pad)
    dr.text((10,y+4),'side (her left)' if v=='side' else '3/4 front-left',fill=(200,210,255),font=font)
    for i,f in enumerate(frames):
        im=Image.open(os.path.join(d,f'{v}_{f:03d}.png')).convert('RGB'); img.paste(im,(i*W,y+pad))
        lab=f'f{f}'+('  CONTACT' if f==contact else '')
        dr.text((i*W+8,y+pad+6),lab,fill=(255,230,120) if f==contact else (255,255,255),font=font)
        if f==contact: dr.rectangle([i*W+1,y+pad+1,i*W+W-2,y+pad+H-2],outline=(80,150,255),width=4)
img.save(out,quality=88); print(out,img.size)
