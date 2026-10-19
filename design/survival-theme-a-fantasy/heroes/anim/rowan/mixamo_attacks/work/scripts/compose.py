import sys,os
from PIL import Image,ImageDraw,ImageFont
d,frames,title,out,contacts=sys.argv[1],[int(x) for x in sys.argv[2].split(',')],sys.argv[3],sys.argv[4],{int(x.split(':')[0]):(x.split(':')[1] if ':' in x else 'HIT') for x in sys.argv[5].split(',')}
rows=sys.argv[6].split(',') if len(sys.argv)>6 else ['side','q34','hand']
labels={'side':'side','q34':'3/4 front','hand':'bow-hand close-up (from behind the draw shoulder)'}
W,H=420,560; pad=36
font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18); big=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',24)
img=Image.new('RGB',(W*len(frames),pad+len(rows)*(H+pad)),(24,26,30)); dr=ImageDraw.Draw(img)
dr.text((10,6),title,fill=(235,235,240),font=big)
for r,v in enumerate(rows):
    y=pad+r*(H+pad); dr.text((10,y+4),labels.get(v,v),fill=(200,210,255),font=font)
    for i,f in enumerate(frames):
        im=Image.open(os.path.join(d,f'{v}_{f:03d}.png')).convert('RGB'); img.paste(im,(i*W,y+pad))
        c=f in contacts; dr.text((i*W+8,y+pad+6),f'f{f}'+(('  '+contacts[f]) if c else ''),fill=(255,230,120) if c else (255,255,255),font=font)
        if c: dr.rectangle([i*W+1,y+pad+1,i*W+W-2,y+pad+H-2],outline=(80,150,255),width=4)
img.save(out,quality=85); print(out,img.size)
