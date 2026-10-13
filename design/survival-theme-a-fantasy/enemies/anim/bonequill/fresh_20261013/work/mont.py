import glob
from PIL import Image,ImageDraw
W='/workspace/bonequill_fresh/work'; O='/workspace/bonequill_fresh'
for k in ('rest','walk','attack'):
    fs=sorted(glob.glob(f'{W}/sh_{k}_*_full.png')); n=len(fs); cw=360
    rows=[('full',640),('face',360),('side',640)]+([] if k=='attack' else [('back',640)])
    S=Image.new('RGB',(cw*n,30+sum(h for _,h in rows)),'white'); d=ImageDraw.Draw(S)
    for i,f in enumerate(fs):
        y=30
        for v,h in rows: S.paste(Image.open(f.replace('_full',f'_{v}')).convert('RGB'),(i*cw,y)); y+=h
        d.text((i*cw+8,8),f"{k} f{f.split('_')[-2]}",fill='black')
    S.save(f'{O}/BONEQUILL_fresh_{k}_sheet.jpg',quality=88); print(k,S.size)
