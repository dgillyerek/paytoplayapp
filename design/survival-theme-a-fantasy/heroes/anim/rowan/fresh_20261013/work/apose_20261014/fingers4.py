"""Rowan left-hand finger bones + closed bow grip (2026-10-14 v4; same approach as Bonequill #63 1347972 fingers.py).
Original A-pose Meshy hand (rig_nofist.blend, no sculpt). Adds mixamorig:LeftHandFingers1-3 (one 3-segment chain for the four fingers)
and mixamorig:LeftHandThumb1-2 parented to LeftHand; finger/thumb vertices weighted smoothly along each chain (NIT smoothing passes),
palm stays on LeftHand. Grip searched with rigid bone rotations about the knuckle axis (hand z): best angular wrap of the hand round a
bow-grip cylinder (2 cm + 2 mm clearance) with zero penetration and finger length change under 3%. Writes rig.blend + grip.json.
Run used: NIT=200 TR0=-0.5 TR1=0.7 YK=0.135 TARGET=15,35,20,35 MINCOV=270 PMAX=1.3 FG=15,35,20 THL=60,20 (picked from a full grid search; then coat.py, anim.py, pen.py, gripcheck.py, export.py, sheet.py)."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import os, bpy, json, math, itertools, numpy as np
from mathutils import Vector, Matrix, Quaternion
from scipy.spatial import cKDTree
W=os.environ.get('WD','/workspace/rowan_fresh/w2'); P_='mixamorig:'
bpy.ops.wm.open_mainfile(filepath=os.environ.get('SRC','/workspace/rowan_fresh/w2/rig_nofist.blend'))
arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; ad=arm.data
B=ad.bones[P_+'LeftHand']; Mh=np.array(B.matrix_local); Mi=np.linalg.inv(Mh)
gi=body.vertex_groups[P_+'LeftHand'].index
V=np.array([v.co[:] for v in body.data.vertices]); N=len(V); WH=np.zeros(N)
for v in body.data.vertices:
    for g in v.groups:
        if g.group==gi: WH[v.index]=g.weight
L=(np.c_[V,np.ones(N)]@Mi.T)[:,:3]; hand=WH>0.01
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
YK=float(os.environ.get('YK','0.088'))
thumb=hand&(L[:,0]>0.010)&(L[:,2]>0.040)&(L[:,1]>0.045)
fing=hand&~thumb&(L[:,1]>YK-0.02)
zc=float(np.median(L[fing&(L[:,1]>0.12),2]))
ytip=float(np.percentile(L[fing,1],99.5)); ys=np.linspace(YK,ytip,12)
cl=np.array([[np.median(L[fing&(np.abs(L[:,1]-yy)<0.008),0]),yy] for yy in ys])
seg=np.linalg.norm(np.diff(cl,axis=0),axis=1); arc=np.r_[0,np.cumsum(seg)]; FL=arc[-1]
def at(s): i=np.clip(np.searchsorted(arc,s)-1,0,len(seg)-1); t=(s-arc[i])/seg[i]; return cl[i]+(cl[i+1]-cl[i])*t
JF=[at(FL*r) for r in (0.0,0.45,0.75,1.0)]
tb=L[thumb]; base=tb[tb[:,1]<np.percentile(tb[:,1],12)].mean(0); far=tb[np.argmax(np.linalg.norm(tb-base,axis=1))]
tip=far; mid=base+(tip-base)*0.55; JT=[base,mid,tip]
def proj(Q,a,b): ab=b-a; return np.clip(((Q-a)@ab)/(ab@ab),-1,2)
BW=0.012
d2=((L[:,None,:2]-cl[None])**2).sum(-1); k=np.argmin(d2,1); s=arc[k]
sF=np.where(fing,s,-1.0); rampF=ss(-BW*0.5,BW*0.5,L[:,1]-YK)*fing
s1,s2=FL*0.45,FL*0.75
wF=np.zeros((N,3)); wF[:,0]=rampF*(1-ss(s1-BW,s1+BW,sF)); wF[:,1]=rampF*ss(s1-BW,s1+BW,sF)*(1-ss(s2-BW,s2+BW,sF)); wF[:,2]=rampF*ss(s2-BW,s2+BW,sF)
tt=proj(L,base,tip); rampT=ss(float(os.environ.get('TR0','0.0')),float(os.environ.get('TR1','0.5')),tt)*thumb
wT=np.zeros((N,2)); wT[:,0]=rampT*(1-ss(0.5,0.62,tt)); wT[:,1]=rampT*ss(0.5,0.62,tt)
frac=np.c_[wF,wT]
E0=np.array([e.vertices[:] for e in body.data.edges]); E0=E0[hand[E0].all(1)]
dg=np.bincount(E0.ravel(),minlength=N).astype(float)
for it in range(int(os.environ.get('NIT','25'))):
    acc=np.zeros_like(frac); np.add.at(acc,E0[:,0],frac[E0[:,1]]); np.add.at(acc,E0[:,1],frac[E0[:,0]])
    sm=acc/np.maximum(dg,1)[:,None]; frac=np.where((dg>0)[:,None],0.5*frac+0.5*sm,frac)
frac[~hand]=0; palm=1-frac.sum(1)
def Rz(a): c,s_=math.cos(a),math.sin(a); return np.array([[c,-s_,0],[s_,c,0],[0,0,1]])
def Rax(axis,a):
    axis=np.asarray(axis,float); axis/=np.linalg.norm(axis); K=np.array([[0,-axis[2],axis[1]],[axis[2],0,-axis[0]],[-axis[1],axis[0],0]])
    return np.eye(3)+math.sin(a)*K+(1-math.cos(a))*K@K
P3F=[np.r_[JF[i],zc] for i in range(4)]
def chain_T(joints,rots):
    T=[]; Racc=np.eye(3); tacc=np.zeros(3)
    for j,R in zip(joints,rots):
        jw=Racc@j+tacc; Racc=R@Racc; tacc=jw-Racc@j; T.append((Racc.copy(),tacc.copy()))
    return T
hidx=np.nonzero(hand)[0]; LH=L[hidx]; fr=frac[hidx]; pl=palm[hidx]
TAX=tuple(float(v) for v in os.environ.get('TAX','0,0,-1').split(','))
def pose(a,th):
    TF=chain_T(P3F[:3],[Rz(-math.radians(x)) for x in a]); TT=chain_T(JT[:2],[Rax(TAX,math.radians(x)) for x in th])
    out=LH*pl[:,None]
    for j,(R,t) in enumerate(TF+TT): out=out+fr[:,j:j+1]*(LH@R.T+t)
    return out
PL=np.array([np.r_[at(FL*u_),zc] for u_ in np.linspace(0,1,40)]); s_=np.array([u_*FL for u_ in np.linspace(0,1,40)])
wc=np.c_[1-ss(s1-BW,s1+BW,s_),ss(s1-BW,s1+BW,s_)*(1-ss(s2-BW,s2+BW,s_)),ss(s2-BW,s2+BW,s_)]
def flen(a):
    TF=chain_T(P3F[:3],[Rz(-math.radians(x)) for x in a]); Pc=sum(wc[:,j:j+1]*(PL@R.T+t) for j,(R,t) in enumerate(TF)); return np.linalg.norm(np.diff(Pc,axis=0),axis=1).sum()/FL-1
E=np.array([e.vertices[:] for e in body.data.edges]); E=E[np.isin(E,hidx).all(1)]; pos=np.full(N,-1); pos[hidx]=np.arange(len(hidx))
el0=np.linalg.norm(LH[pos[E[:,0]]]-LH[pos[E[:,1]]],axis=1); movedE=(fr[pos[E[:,0]]].sum(1)+fr[pos[E[:,1]]].sum(1))>0.02
def stretch(Q):
    el1=np.linalg.norm(Q[pos[E[:,0]]]-Q[pos[E[:,1]]],axis=1); r=el1[movedE]/np.maximum(el0[movedE],1e-6); return float(np.percentile(r,99)),float(r.max())
RIN=float(os.environ.get('RIN','0.022'))
za=np.abs(LH[:,2]-zc)<0.09; zr=np.abs(LH[:,2]-zc)<0.05
gx,gy=np.meshgrid(np.arange(-0.02,0.10,0.002),np.arange(0.03,0.17,0.002)); G=np.c_[gx.ravel(),gy.ravel()]
def covr(v):
    if len(v)==0: return 0
    ang=np.degrees(np.arctan2(v[:,1],v[:,0]))%360; return len(np.unique((ang//10).astype(int)))*10
if os.environ.get('DUMP'):
    for tag,a_ in (('r',(0,0,0)),('p',tuple(map(int,os.environ['DUMP'].split(','))))): np.save(W+'/dump_'+tag+'.npy',pose(a_,(40,0)))
    np.save(W+'/dump_fr.npy',fr); np.save(W+'/dump_jf.npy',np.array(P3F)); raise SystemExit
from mathutils.bvhtree import BVHTree
HS={int(v):i for i,v in enumerate(hidx)}
HP=[tuple(HS[v] for v in p.vertices) for p in body.data.polygons if all(v in HS for v in p.vertices)]
def outside(bvh,p):
    loc,nrm,_,d=bvh.find_nearest(Vector(p))
    return loc is not None and (Vector(p)-loc).dot(nrm)>0
pm_=(fr.sum(1)<0.05)&zr; palm2d=LH[pm_][:,:2]
res=[]
FG=[tuple(map(int,x.split(','))) for x in os.environ['FG'].split(';')] if os.environ.get('FG') else list(itertools.product(range(5,46,5),range(10,71,10),range(0,61,10)))
for a in FG:
    lc=flen(a)
    if abs(lc)>float(os.environ.get('LCMAX','0.025')): continue
    for th in ([tuple(map(int,x.split(','))) for x in os.environ['THL'].split(';')] if os.environ.get('THL') else [(t1,t2) for t1 in range(0,61,10) for t2 in range(0,46,15)]):
        Q=pose(a,th); qa=Q[za][:,:2]; tr=cKDTree(qa); dm,_=tr.query(G)
        ok=np.nonzero((dm>=RIN)&(dm<=RIN+0.004))[0]
        if not len(ok): continue
        qr=Q[zr][:,:2]; trr=cKDTree(qr); cands=[]
        for i in ok[::2]:
            idx=trr.query_ball_point(G[i],dm[i]+0.018); cands.append((covr(qr[idx]-G[i]),float(dm[i]),G[i]))
        cands.sort(key=lambda c_:-c_[0]); bvh=None; bestc=None
        for c_ in cands:
            if bestc is not None and c_[0]<=bestc[0]: break
            if bvh is None: bvh=BVHTree.FromPolygons([tuple(v) for v in Q],HP)
            if os.environ.get('PALM') and float(np.min(np.linalg.norm(palm2d-c_[2],axis=1)))>RIN+float(os.environ['PALM']): continue
            if all(outside(bvh,(c_[2][0],c_[2][1],zc+dz)) for dz in (-0.04,-0.02,0,0.02,0.04)): bestc=c_; break
        if bestc is None: continue
        res.append((bestc[0],round(bestc[1],4),a,th,bestc[2].round(4).tolist(),round(lc,4),round(stretch(Q)[0],3)))
res.sort(key=lambda r:(-r[0],sum(r[2])))
if os.environ.get('XMIN'): res=[r for r in res if r[4][0]>=float(os.environ['XMIN'])]
for r in res[:12]: print('CAND',r,flush=True)
for r in [x for x in res if x[6]<=float(os.environ.get('PMAX','1.3'))][:15]: print('LOWSTRETCH',r,flush=True)
if os.environ.get('SEARCHONLY'): raise SystemExit
if os.environ.get('TARGET'):
    tg=[float(v) for v in os.environ['TARGET'].split(',')]; good=[r for r in res if r[0]>=int(os.environ.get('MINCOV','300')) and r[6]<=float(os.environ.get('PMAX','1.3'))]
    good.sort(key=lambda r:sum(abs(x-y) for x,y in zip(list(r[2])+[r[3][0]],tg))+0.01*(360-r[0])); res=good+res
    print('PICK',res[0],flush=True)
cov,rr,a,th,C,lc=res[0][:6]; C=np.array(C)
Q=pose(a,th); p99,smax=stretch(Q)
# per-finger length: centreline polylines on 4 z-slices of the finger band (each real finger) rest vs posed
fl_slices=[]
zq=np.percentile(LH[fr[:,:3].sum(1)>0.3,2],[0,25,50,75,100]); fm=fr[:,:3].sum(1)>0.05
for kq in range(4):
    m_=fm&(LH[:,2]>=zq[kq])&(LH[:,2]<=zq[kq+1]); yb=np.linspace(YK-0.01,np.percentile(LH[m_,1],98),10)
    sets=[m_&(LH[:,1]>=y0)&(LH[:,1]<y1) for y0,y1 in zip(yb[:-1],yb[1:])]; sets=[x for x in sets if x.sum()>=3]
    l0=sum(np.linalg.norm(LH[sets[i+1]].mean(0)-LH[sets[i]].mean(0)) for i in range(len(sets)-1)); l1=sum(np.linalg.norm(Q[sets[i+1]].mean(0)-Q[sets[i]].mean(0)) for i in range(len(sets)-1))
    fl_slices.append([round(l0,4),round(l1,4),round(l1/l0-1,4)])
# per-finger length along mesh paths (4 fingers by knuckle-axis quartile: shortest rest edge path knuckle -> tip, same path posed)
from scipy.sparse import coo_matrix; from scipy.sparse.csgraph import dijkstra
ea,eb_=pos[E[:,0]],pos[E[:,1]]; path_len=[]
for kq in range(4):
    m_=fm&(LH[:,2]>=zq[kq])&(LH[:,2]<=zq[kq+1]); ok_=m_[ea]&m_[eb_]
    Gq=coo_matrix((el0[ok_],(ea[ok_],eb_[ok_])),shape=(len(LH),)*2).tocsr()
    ids=np.nonzero(m_)[0]; r0=ids[np.argmin(np.abs(LH[ids,1]-YK)+np.abs(LH[ids,2]-np.median(LH[ids,2])))]
    dist,pred=dijkstra(Gq,directed=False,indices=r0,return_predecessors=True); reach=ids[np.isfinite(dist[ids])]; tp=reach[np.argmax(LH[reach,1])]
    pth=[tp]
    while pth[-1]!=r0: pth.append(pred[pth[-1]])
    pth=np.array(pth); l0=float(np.linalg.norm(np.diff(LH[pth],axis=0),axis=1).sum()); l1=float(np.linalg.norm(np.diff(Q[pth],axis=0),axis=1).sum())
    path_len.append([round(l0,4),round(l1,4),round(l1/l0-1,4)])
print('PATHLEN',path_len,flush=True)
# ---- create bones + weights
def W3(p): p=np.asarray(p); p3=np.r_[p[0],p[1],zc] if len(p)==2 else p; return Vector((Mh@np.r_[p3,1])[:3])
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT'); eb=ad.edit_bones
zax=Vector(Mh[:3,2])
FN=[P_+'LeftHandFingers1',P_+'LeftHandFingers2',P_+'LeftHandFingers3']; TN=[P_+'LeftHandThumb1',P_+'LeftHandThumb2']
par=eb[P_+'LeftHand']
for i,n in enumerate(FN):
    b=eb.new(n); b.head=W3(JF[i]); b.tail=W3(JF[i+1]); b.align_roll(zax); b.parent=par; b.use_connect=i>0; par=b
par=eb[P_+'LeftHand']
for i,n in enumerate(TN):
    b=eb.new(n); b.head=W3(JT[i]); b.tail=W3(JT[i+1]); b.align_roll(zax); b.parent=par; b.use_connect=i>0; par=b
bpy.ops.object.mode_set(mode='OBJECT')
vgH=body.vertex_groups[P_+'LeftHand']
for j,n in enumerate(FN+TN):
    vg=body.vertex_groups.new(name=n); col=WH*frac[:,j]
    for i in np.nonzero(col>1e-4)[0]: vg.add([int(i)],float(col[i]),'REPLACE')
for i in np.nonzero(hand)[0]:
    w=WH[i]*palm[i]
    if w>1e-4: vgH.add([int(i)],float(w),'REPLACE')
    else: vgH.remove([int(i)])
PB=arm.pose.bones; Mh3=B.matrix_local.to_3x3(); quats={}
def frot(n,ax,deg):
    pb=PB[n]; pb.rotation_mode='QUATERNION'; R=pb.bone.matrix_local.to_3x3(); q=Quaternion((Mh3@Vector(ax)).normalized(),math.radians(deg))
    quats[n]=list((R.inverted()@q.to_matrix()@R).to_quaternion())
for i,d in enumerate(a): frot(FN[i],(0,0,1),-d)
for i,d in enumerate(th): frot(TN[i],TAX,d)
GRL=[[-1,0,0],[0,-1,0],[0,0,1]]   # bow: long axis (Z) along the knuckle axis, string (+Y) toward the wrist
out={'names':FN+TN,'finger_curl_deg':list(a),'thumb_axis_hand':list(TAX),'thumb_deg':list(th),'finger_local_quat':quats,
     'grip_local':[float(C[0]),float(C[1]),zc],'grip_rot_local':GRL,'min_hand_to_grip_axis':rr,'wrap_deg_bind':int(cov),
     'finger_chain_length_change':float(lc),'finger_surface_path_rest_posed_ratio':path_len,'finger_surface_path_change_max':max(abs(x[2]) for x in path_len),'max_finger_length_change':abs(float(lc)),'finger_slice_centroid_ratio_info':fl_slices,
     'edge_stretch_p99':p99,'max_edge_stretch':smax,'finger_verts':int(fing.sum()),'thumb_verts':int(thumb.sum()),'palm_verts':int((hand&(frac.sum(1)<0.01)).sum()),
     'joints_fingers':[list(map(float,j)) for j in JF],'joints_thumb':[list(map(float,j)) for j in JT],'knuckle_y':YK,'source':'fingers4.py (posed finger bones on the original Meshy hand, no sculpt)'}
json.dump(out,open(W+'/grip.json','w'),indent=1); print('GRIP',json.dumps({k:v for k,v in out.items() if not k.startswith(('joints','finger_local'))}),flush=True)
bpy.ops.wm.save_as_mainfile(filepath=W+'/rig.blend',compress=True)
