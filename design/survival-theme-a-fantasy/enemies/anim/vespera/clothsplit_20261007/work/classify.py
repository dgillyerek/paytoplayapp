"""Leg-tube detection via vertically linked compact slice loops; classify verts below z_cut into LEG vs CLOTH."""
import bpy, sys, os, json, math
sys.path.insert(0,'/workspace/attack_20261007/clothsplit')
import numpy as np, cs_lib as L
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; src,label=args[0],args[1]
zcut_override=float(args[2]) if len(args)>2 and args[2]!='auto' else None
RELAX=(args[3]=='1') if len(args)>3 else True
COMP=float(args[4]) if len(args)>4 else 0.40
AMIN=float(args[5]) if len(args)>5 else 0.0020
MISS=int(args[6]) if len(args)>6 else 12
BDK=float(args[7]) if len(args)>7 else 0.006
bpy.ops.wm.open_mainfile(filepath=src)
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE'); body=next(o for o in bpy.data.objects if o.type=='MESH')
if arm.animation_data: arm.animation_data.action=None
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.view_layer.update()
bm=L.world_bm(body); bm.verts.ensure_lookup_table()
co=np.array([v.co[:] for v in bm.verts]); z0,z1=co[:,2].min(),co[:,2].max(); H=z1-z0
step=0.01; zs=np.arange(z0+0.006,z0+0.72*H,step)
S=[]
for z in zs:
    ls=[]
    for l in L.slice_loops(bm,z):
        if not l['closed'] or l['area']<AMIN or l['area']>0.06: continue
        p=l['pts']; per=np.hypot(*(np.roll(p,-1,0)-p).T).sum(); comp=4*math.pi*l['area']/(per*per+1e-9)
        if comp<COMP: continue
        l['comp']=comp; ls.append(l)
    S.append(ls)
# link chains bottom->top (gap tolerant: a chain survives up to 5 slices without a match)
chains=[]; active=[]   # each: {'pts':[(z,l)], 'miss':0}
for i,(z,ls) in enumerate(zip(zs,S)):
    used=set(); keep=[]
    for ch in sorted(active,key=lambda c:-len(c['pts'])):
        last=ch['pts'][-1][1]; best=None; bd=0.035+BDK*ch['miss']
        for j,l in enumerate(ls):
            if j in used: continue
            d=np.hypot(*(l['centroid']-last['centroid'])); r=l['area']/last['area']
            lo_r,hi_r=(0.4,2.5) if (ch['miss']==0 or not RELAX) else (0.2,5.0)
            if d<bd and lo_r<r<hi_r: best,bd=j,d
        if best is not None: used.add(best); ch['pts'].append((z,ls[best])); ch['miss']=0; keep.append(ch)
        elif ch['miss']<MISS: ch['miss']+=1; keep.append(ch)
        else: chains.append(ch['pts'])
    for j,l in enumerate(ls):
        if j not in used: keep.append({'pts':[(z,l)],'miss':0})
    active=keep
chains+=[c['pts'] for c in active]
def span(ch): return ch[-1][0]-ch[0][0]
cands=[c for c in chains if span(c)>0.22*H and (c[0][0]-z0)<0.22*H]
cands.sort(key=lambda c:len(c),reverse=True)
legs=[]
def cxy(c): return np.mean([l['centroid'] for z,l in c[:max(3,len(c)//3)]],0)
for c in cands:
    if all(np.hypot(*(cxy(c)-cxy(d)))>0.08 for d in legs): legs.append(c)
    if len(legs)==2: break
info=[{'x':float(np.mean([l['centroid'][0] for z,l in c])),'z_lo':float((c[0][0]-z0)/H),'z_hi':float((c[-1][0]-z0)/H),'n':len(c),
       'area_med':float(np.median([l['area'] for z,l in c]))} for c in legs]
print('LEGCHAINS',label,len(chains),'cands',len(cands),info)
def solid_top(c):
    zl=[z for z,l in c]
    for k in range(len(zl)-1):
        if zl[k]>z0+0.30*H and zl[k+1]-zl[k]>0.03: return zl[k]
    return zl[-1]
ztop=min(solid_top(c) for c in legs) if len(legs)==2 else None
zcut=zcut_override*H+z0 if zcut_override else (ztop+0.01 if ztop else None)
leg=np.zeros(len(co),bool); below=co[:,2]<zcut
margin=0.015
for c in legs:
    zl=np.array([z for z,l in c])
    for k,(z,l) in enumerate(c):
        lo=(z+zl[k-1])/2 if k>0 else -1e9
        hi=z+step/2 if k<len(c)-1 else zcut
        if k<len(c)-1 and zl[k+1]-z<=1.5*step: hi=(z+zl[k+1])/2
        m=below&(co[:,2]>=lo)&(co[:,2]<hi)
        if m.any():
            poly=l['pts']; px,py=co[m,0],co[m,1]
            if k==0:
                cx,cy=l['centroid']; ins=(np.abs(px-cx)<0.11)&(np.abs(py-cy)<0.26)&(np.hypot(px-cx,py-cy)<0.28)
            else:
                ins=L.point_in_poly(px,py,poly)|(L.dist_to_poly(px,py,poly)<margin)
            idx=np.where(m)[0]; leg[idx[ins]]=True
        # gap to next chain point: interpolate (translate both end polygons toward the interpolated centroid)
        if k<len(c)-1 and zl[k+1]-z>1.5*step:
            l2=c[k+1][1]; za,zb=z+step/2,(zl[k+1]+z)/2+ (zl[k+1]-z)/2 - step/2
            m=below&(co[:,2]>=za)&(co[:,2]<zb)
            if not m.any(): continue
            idx=np.where(m)[0]; t=(co[idx,2]-z)/(zl[k+1]-z)
            cA=l['centroid']; cB=l2['centroid']; ins=np.zeros(len(idx),bool)
            for poly,cP in ((l['pts'],cA),(l2['pts'],cB)):
                for tb in np.linspace(0,1,6):
                    sel=np.abs(t-tb)<=0.1+1e-9
                    if not sel.any(): continue
                    shift=(cA*(1-tb)+cB*tb)-cP; pp=poly+shift
                    q=idx[sel]; ins[sel]|=L.point_in_poly(co[q,0],co[q,1],pp)|(L.dist_to_poly(co[q,0],co[q,1],pp)<0.03)
            leg[idx[ins]]=True
hand=np.zeros(len(co),bool)
for hn in ('mixamorig:LeftHand','mixamorig:RightHand'):
    b=arm.data.bones.get(hn)
    if not b: continue
    h=np.array(arm.matrix_world@b.head_local); t=np.array(arm.matrix_world@b.tail_local); ab=t-h
    tt=np.clip(((co-h)@ab)/max(ab@ab,1e-9),0,1.6); q=h+tt[:,None]*ab
    hand|=np.linalg.norm(co-q,axis=1)<0.13
cloth=below&~leg&~hand
# arm/hand chains crossing the seam (geometry based; covers rigs whose hand bones are misplaced)
armch=[c for c in chains if not any(c is d for d in legs) and len(c)>=6 and np.mean([abs(l['centroid'][0]) for z,l in c])>0.26
       and c[0][0]<zcut and c[-1][0]>zcut-0.02]
for c in armch:
    zl=np.array([z for z,l in c])
    for k,(z,l) in enumerate(c):
        lo=(z+zl[k-1])/2 if k>0 else z-0.15; hi=(z+zl[k+1])/2 if k<len(c)-1 else z+0.01
        m=below&(co[:,2]>=lo)&(co[:,2]<hi)
        if not m.any(): continue
        poly=l['pts']; px,py=co[m,0],co[m,1]
        ins=L.point_in_poly(px,py,poly)|(L.dist_to_poly(px,py,poly)<0.025)
        if k==0: ins=L.dist_to_poly(px,py,poly)<0.07
        idx=np.where(m)[0]; hand[idx[ins]]=True
cloth=below&~leg&~hand
print('HANDEXCL',int((below&~leg&hand).sum()),'armchains',[(round(float(np.mean([l['centroid'][0] for z,l in c])),2),round((c[0][0]-z0)/H,2),round((c[-1][0]-z0)/H,2)) for c in armch])
np.savez(f'cls_{label}.npz',cloth=cloth,leg=leg,zcut=zcut,z0=z0,H=H)
json.dump({'legs':info,'zcut':float(zcut),'zcut_frac':float((zcut-z0)/H),'z0':float(z0),'H':float(H),
           'chains':[[[float(z),l['pts'].tolist()] for z,l in c] for c in legs]},open(f'legs_{label}.json','w'))
print('CLASS',label,'verts',len(co),'below',int(below.sum()),'leg',int(leg.sum()),'cloth',int(cloth.sum()),'zcut_frac',round((zcut-z0)/H,3))
me=body.data
ca=me.color_attributes.new('cls','BYTE_COLOR','POINT')
cols=np.tile([0.75,0.75,0.75,1.0],(len(co),1)); cols[leg]=[0.2,0.5,1.0,1]; cols[cloth]=[1.0,0.35,0.1,1]
ca.data.foreach_set('color',cols.ravel())
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='VERTEX'
sc.render.resolution_x,sc.render.resolution_y=500,800
for o in list(bpy.data.objects):
    if o.type in {'CAMERA','LIGHT'}: bpy.data.objects.remove(o)
cd=bpy.data.cameras.new('c'); cam=bpy.data.objects.new('c',cd); sc.collection.objects.link(cam); sc.camera=cam; cd.lens=50
os.makedirs('dbg',exist_ok=True)
c=Vector((float(co[:,0].mean()),float(co[:,1].mean()),z0+0.40*H))
for name,d in (('front',(0,-1,0.15)),('rear',(0,1,0.15)),('side',(1,0,0.1)),('below',(0.25,-0.35,-1))):
    cam.location=c+Vector(d).normalized()*H*2.2; cam.rotation_euler=(c-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'dbg/{label}_{name}.png'; bpy.ops.render.render(write_still=True)
