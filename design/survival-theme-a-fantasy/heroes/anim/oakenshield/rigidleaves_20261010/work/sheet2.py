from PIL import Image, ImageDraw, ImageFont
import os
try: F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',15)
except Exception: F=ImageFont.load_default()
im0=Image.open('r2/front_rest_f00.png'); w,h=im0.size
BOX=(95,100,265,540); cw,ch=int((BOX[2]-BOX[0])*1.0),int((BOX[3]-BOX[1])*1.0); LW=130
rows=[('BEFORE b6e75ab\nwalk front','r0','front',['rest_f00','walk_f00','walk_f08','walk_f15','walk_f23']),
      ('AFTER\nwalk front','r2','front',['rest_f00','walk_f00','walk_f08','walk_f15','walk_f23']),
      ('AFTER\nwalk side','r2','side',['rest_f00','walk_f00','walk_f08','walk_f15','walk_f23']),
      ('AFTER\nattack front','r2','front',['atk_f06','atk_f10','atk_f14','atk_f20']),
      ('AFTER\nattack side','r2','side',['atk_f06','atk_f10','atk_f14','atk_f20'])]
S=Image.new('RGB',(LW+cw*5,22+ch*len(rows)),(30,30,34)); d=ImageDraw.Draw(S)
d.text((6,3),'Oakenshield on Derek blend 329ae0c: hip-width walk, smoothed hips, head forward (2026-10-11)',fill=(230,230,230),font=F)
for r,(lab,dr,v,cols) in enumerate(rows):
    y=22+ch*r; d.multiline_text((6,y+ch//2-18),lab,fill=(240,220,120),font=F)
    for i,c in enumerate(cols):
        p=f'{dr}/{v}_{c}.png'
        if not os.path.exists(p): continue
        S.paste(Image.open(p).convert('RGB').crop(BOX),(LW+cw*i,y)); d.text((LW+cw*i+4,y+4),c.replace('_f',' f'),fill=(200,200,200),font=F)
S.save('contact_full.jpg',quality=88)
s=S.copy(); s.thumbnail((1000,1400)); s.save('contact_small.jpg',quality=82); print(S.size,s.size)
