import sys,json; from PIL import Image, ImageDraw
P=json.load(open(sys.argv[1]+'_paths.json')); out=sys.argv[2]
views=[]; [views.append(v) for v,_,_ in P if v not in views]; frs=sorted({f for _,f,_ in P})
w,h=Image.open(P[0][2]).size; M=Image.new('RGB',(60+w*len(frs),24+h*len(views)),'white'); d=ImageDraw.Draw(M)
for v,f,p in P:
    x=60+frs.index(f)*w; y=24+views.index(v)*h; M.paste(Image.open(p).convert('RGB'),(x,y)); d.text((x+4,4),f'f{f}',fill='black')
for i,v in enumerate(views): d.text((4,24+i*h+h//2),v,fill='black')
M.save(out,quality=90)
