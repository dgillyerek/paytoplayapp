import sys,json; from PIL import Image, ImageDraw
P=json.load(open(sys.argv[1]+'_paths.json')); out=sys.argv[2]; title=sys.argv[3] if len(sys.argv)>3 else ''
rows=[]; [rows.append(r) for r,_,_ in P if r not in rows]; cols=[]; [cols.append(c) for _,c,_ in P if c not in cols]
w,h=Image.open(P[0][2]).size; M=Image.new('RGB',(90+w*len(cols),46+h*len(rows)),'white'); d=ImageDraw.Draw(M); d.text((4,4),title,fill='black')
for r,c,p in P:
    x=90+cols.index(c)*w; y=46+rows.index(r)*h; M.paste(Image.open(p).convert('RGB'),(x,y)); d.text((x+4,28),str(c),fill='black')
for i,r in enumerate(rows): d.text((4,46+i*h+h//2),r,fill='black')
M.save(out,quality=90)
