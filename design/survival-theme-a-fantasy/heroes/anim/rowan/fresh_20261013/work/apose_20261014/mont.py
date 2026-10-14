import glob
from PIL import Image,ImageDraw
W='/workspace/rowan_fresh/w2'; O=W+'/out'
import os; os.makedirs(O,exist_ok=True)
ROWS={'rest':[('full',640),('face',360),('side',640),('back',640)],
      'walk':[('full',640),('face',360),('side',640),('back',640),('handLF',360),('handRF',360),('handLS',360),('handRS',360)],
      'attack':[('full',640),('face',360),('side',640),('back',640)]}
for k,rows in ROWS.items():
    fs=sorted(glob.glob(f'{W}/sh_{k}_*_full.png')); n=len(fs); cw=360
    S=Image.new('RGB',(cw*n+90,30+sum(h for _,h in rows)),'white'); d=ImageDraw.Draw(S)
    for i,f in enumerate(fs):
        y=30
        for v,h in rows: S.paste(Image.open(f.replace('_full',f'_{v}')).convert('RGB'),(90+i*cw,y)); y+=h
        d.text((90+i*cw+8,8),f"{k} f{f.split('_')[-2]}",fill='black')
    y=30
    for v,h in rows: d.text((6,y+h//2),{'full':'front','handLF':'L hand\nfront','handRF':'R hand\nfront','handLS':'L hand\nside','handRS':'R hand\nside'}.get(v,v),fill='black'); y+=h
    S.save(f'{O}/ROWAN_fresh_{k}_sheet.jpg',quality=88); print(k,S.size)
a=Image.open(f'{W}/sh_attack_10_bigfront.png').convert('RGB'); b=Image.open(f'{W}/sh_attack_10_bigside.png').convert('RGB')
S=Image.new('RGB',(1440,930),'white'); S.paste(a,(0,30)); S.paste(b,(720,30)); d=ImageDraw.Draw(S)
d.text((8,8),'attack f10 full draw: front',fill='black'); d.text((728,8),'attack f10 full draw: side (from his left)',fill='black')
S.save(f'{O}/ROWAN_fresh_attack_fulldraw_front_side.jpg',quality=90); print('fulldraw')
