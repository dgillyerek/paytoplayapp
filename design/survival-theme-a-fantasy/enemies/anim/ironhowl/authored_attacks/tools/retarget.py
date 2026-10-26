"""Write a Mixamo-layout clip: take the Mixamo template FBX (same 33-bone mixamorig skeleton,
PreRotations, take 'mixamo.com', curve layout) and replace its curves with the motion of a
Blender-exported clip, evaluated per frame as local matrices.
usage: retarget.py TEMPLATE SRC OUT"""
import sys, math, array
sys.path.insert(0,"/workspace/ironhowl/work")
import numpy as np
from fbxlib.parse_fbx import parse, FBXElem
import fbxwrite
from fbxeval import F, child, props, eul, KTIME
def euler_xyz_from(R, prev):
    # R = Rz(z) Ry(y) Rx(x); return degrees, the solution nearest prev
    sy=-R[2,0]; sy=max(-1,min(1,sy)); y=math.asin(sy)
    if abs(math.cos(y))>1e-6:
        x=math.atan2(R[2,1],R[2,2]); z=math.atan2(R[1,0],R[0,0])
    else:
        x=math.atan2(-R[1,2],R[1,1]); z=0.0
    sols=[(x,y,z),(x+math.pi,math.pi-y,z+math.pi)]
    best=None
    for s in sols:
        d=[math.degrees(v) for v in s]
        if prev is not None:
            d=[v+360*round((p-v)/360) for v,p in zip(d,prev)]
            cost=sum((v-p)**2 for v,p in zip(d,prev))
        else: cost=sum(v*v for v in d)
        if best is None or cost<best[0]: best=(cost,d)
    return best[1]
def set_props70(el, key, val):
    for c in el.elems:
        if c.id==b'Properties70':
            for p in c.elems:
                if p.props[0]==key: p.props[-1]=val
def main(tpath, spath, opath):
    root,ver=parse(tpath)
    src=F(spath); tm=F(tpath)
    objs=child(root,b'Objects'); conns=child(root,b'Connections')
    # src frame count: keys of Hips rotation
    node=src.anim['mixamorig:Hips']['Lcl Rotation']; t,_=src.cv[src.cn[node]['c']['d|X']]
    f0=int(round(t[0]*30/KTIME)); f1=int(round(t[-1]*30/KTIME)); frames=list(range(f0,f1+1)); n=len(frames)
    times=np.array([int(round(i*KTIME/30)) for i in range(n)],dtype=np.int64)
    # values per (model, prop) as (n,3)
    vals={}
    for mname,chans in tm.anim.items():
        pr=[p for p in tm.models.values() if p[0]==mname][0][1]
        pre=eul(pr.get('PreRotation',[0,0,0,0,0,0])[-3:]) if 'PreRotation' in pr else np.eye(3)
        out_r=[];prev=None;out_t=[]
        for f in frames:
            M=src.local(mname,f)
            R=pre.T@M[:3,:3]; prev=euler_xyz_from(R,prev); out_r.append(prev); out_t.append(list(M[:3,3]))
        vals[(mname,'Lcl Rotation')]=np.array(out_r); vals[(mname,'Lcl Translation')]=np.array(out_t)
    # map curve uid -> (model,prop,axis); node uid -> (model,prop)
    node_of={}; 
    for mname,chans in tm.anim.items():
        for prop,nid in chans.items(): node_of[nid]=(mname,prop)
    keep_curves={}
    for nid,(mname,prop) in node_of.items():
        for ax,cid in tm.cn[nid]['c'].items(): keep_curves[cid]=(mname,prop,'XYZ'.index(ax[-1]))
    newobjs=[]
    for o in objs.elems:
        uid=o.props[0]
        if o.id==b'AnimationCurve':
            if uid not in keep_curves: continue
            mname,prop,i=keep_curves[uid]; v=vals[(mname,prop)][:,i].astype(np.float32)
            for c in o.elems:
                if c.id==b'Default': c.props[0]=float(v[0])
                elif c.id==b'KeyTime': c.props[0]=array.array('q',times.tolist())
                elif c.id==b'KeyValueFloat': c.props[0]=array.array('f',v.tolist())
                elif c.id==b'KeyAttrRefCount': c.props[0]=array.array('i',[n])
        elif o.id==b'AnimationCurveNode':
            if uid not in node_of: continue
            v=vals[node_of[uid]]
            for i,ax in enumerate('XYZ'): set_props70(o,('d|'+ax).encode(),float(v[0,i]))
        elif o.id==b'AnimationStack':
            set_props70(o,b'LocalStop',int(times[-1]))
        newobjs.append(o)
    objs.elems[:]=newobjs
    alive={o.props[0] for o in objs.elems}|{0}
    conns.elems[:]=[c for c in conns.elems if c.props[1] in alive and c.props[2] in alive]
    tk=child(root,b'Takes')
    for t in tk.elems:
        if t.id==b'Take':
            for c in t.elems:
                if c.id in (b'LocalTime',b'ReferenceTime'): c.props[1]=int(times[-1])
    fbxwrite.write(root,ver,opath)
    # verify
    o=F(opath); err=0.0; terr=0.0
    for mname in tm.anim:
        for k,f in enumerate(frames):
            A=src.local(mname,f); B=o.local(mname,k)
            err=max(err,np.abs(A[:3,:3]-B[:3,:3]).max()); terr=max(terr,np.abs(A[:3,3]-B[:3,3]).max())
    # bones the template does not animate must stay at their bind locals in the source
    serr=0.0
    for mname,_ in tm.models.values():
        if mname in tm.anim or mname not in [p[0] for p in src.models.values()]: continue
        for f in frames[::5]:
            serr=max(serr,np.abs(src.local(mname,f)-o.local(mname,0)).max())
    print(f"frames {n} (src {f0}-{f1}) rot err {err:.2e} pos err {terr:.2e} unanimated-bone drift {serr:.2e}")
if __name__=='__main__': main(*sys.argv[1:4])
