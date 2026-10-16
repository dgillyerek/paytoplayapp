"""Rowan inner-forearm carry check (port of Bonequill #63 carrychk.py): every half-frame of rest, walk and attack, bow limb vertices
inside or within 1 cm of the body (legs included; left arm/hand excluded) or within 1 cm of the coat tails (rigid on Hips), left forearm
vertices inside the bow, string distance to body (bow forearm/hand exempt; draw hand exempt f8-f14) and coat, string-to-bow-forearm gap and string-up z in the carry, bow tilt from vertical, string-outward x, wrist bend."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages"); sys.path.insert(0,'/workspace/rowan_fresh/w2/bowgrip')
import bpy,json,math,os,numpy as np
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
from bowroll import bowrot
W='/workspace/rowan_fresh/w2'; P='mixamorig:'
bpy.ops.wm.open_mainfile(filepath=os.environ.get('INBL',W+'/anim.blend'))
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; coat=bpy.data.objects['ROWAN_coat']; crig=bpy.data.objects['ROWAN_coat_rig']
if crig.animation_data: crig.animation_data.action=None
for pb in crig.pose.bones: pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0)
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bow=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]; bow.parent=None
BV=np.array([v.co[:] for v in bow.data.vertices]); STR=(np.abs(BV[:,0])<0.003)&(BV[:,1]>0.125)&(np.abs(BV[:,2])>0.65)
LIMB=BV[~STR]
bowbvh=BVHTree.FromPolygons([v.co.copy() for v in bow.data.vertices],[p.vertices[:] for p in bow.data.polygons])
GL=Vector(json.load(open(W+'/grip.json'))['grip_local'])
ex=set(body.vertex_groups[P+n].index for n in ('LeftHand','LeftForeArm','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2'))
excl=np.array([any(g.group in ex and g.weight>0.3 for g in v.groups) for v in body.data.vertices])
rh=set(g.index for g in body.vertex_groups if g.name.startswith(P+'RightHand'))
isrh=np.array([sum(g.weight for g in v.groups if g.group in rh)>0.3 for v in body.data.vertices])
fa=body.vertex_groups[P+'LeftForeArm'].index
isfa=[v.index for v in body.data.vertices if any(g.group==fa and g.weight>0.5 for g in v.groups)]
polys=[p.vertices[:] for p in body.data.polygons if not excl[list(p.vertices)].any()]
FPOL=[p.vertices[:] for p in body.data.polygons]   # full skin incl. the bow arm (string-vs-skin test)
CPOL=[p.vertices[:] for p in coat.data.polygons]
def evalco(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear()
    M=np.array(o.matrix_world); return Q.reshape(-1,3)@M[:3,:3].T+M[:3,3]
clips=os.environ.get('CLIPS','rest,walk,attack').split(','); STEP=int(os.environ.get('STEP','1')); rep={}
def inside(t,p):
    v=0
    for dr in (Vector((0.31,0.52,0.79)),Vector((-0.6,0.2,-0.77)),Vector((0.1,-0.95,0.3))):
        dr=dr.normalized(); q=p.copy(); c=0
        for _ in range(60):
            loc,n,i,d=t.ray_cast(q,dr)
            if loc is None: break
            c+=1; q=loc+dr*1e-5
        v+=c%2
    return v>=2
for an in clips:
    arm.animation_data.action=bpy.data.actions['ROWAN_'+an]; w=dict(limb=0,coat=0,fa_in_bow=0,str_body=9,str_coat=9,str_fa_min=9,str_fa_max=0,up=1,tilt=0,out=1,wrist=0,bow_fa=0)
    for k in range(0,61,STEP):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f))
        crig.pose.bones['coat_Hips'].matrix=arm.pose.bones[P+'Hips'].matrix.copy(); bpy.context.view_layer.update()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; R=M.to_3x3().normalized(); B=R@bowrot(an,f); g=M.to_translation()+R@GL
        BW=Matrix.Translation(g)@B.to_4x4(); Bi=BW.inverted()
        Va=evalco(body); Ca=evalco(coat)
        bvh=BVHTree.FromPolygons([Vector(v) for v in Va],polys); cb=BVHTree.FromPolygons([Vector(v) for v in Ca],CPOL)
        Pb=LIMB@np.array(B)[:3,:3].T+np.array(g); nl=0; nc=0
        for p in Pb:
            p=Vector(p); loc,nrm,_,dd=bvh.find_nearest(p)
            if loc is not None and ((p-loc).dot(nrm)<0 and dd<0.15 or dd<0.01): nl+=1
            loc,nrm,_,dd=cb.find_nearest(p)
            if loc is not None and dd<0.01: nc+=1
        nf=0
        for i in isfa:
            q=Bi@Vector(Va[i]); loc,nrm,_,dd=bowbvh.find_nearest(q)
            if loc is not None and (q-loc).dot(nrm)<0 and dd<0.05 and loc.y<0.125: nf+=1   # limb only: the thin string's back faces are not a volume (forearm sits past the string by design)
        draw=an=='attack' and 8<=f<=14
        a=np.array(g+B@Vector((0,0.13,-0.66))); b=np.array(g+B@Vector((0,0.13,0.66)))
        def seg(Q):
            d=b-a; t=np.clip(((Q-a)@d)/(d@d),0,1); return float(np.linalg.norm(Q-(a+t[:,None]*d),axis=1).min())
        sb=seg(Va[~isrh&~excl] if draw else Va[~excl]); scp=seg(Ca)
        fbv=BVHTree.FromPolygons([Vector(v) for v in Va],FPOL); sfa=9; sin=0   # string vs the skin surface (all of the body, ray-parity inside test): no penetration, gap to the forearm
        for z in np.linspace(-0.66,0.66,221):
            p=Vector(a+(b-a)*(z+0.66)/1.32); loc,_n,_i,dd=fbv.find_nearest(p)
            if loc is None: continue
            if draw: continue   # nock-to-release: the string is in the draw fingers
            if dd<sfa: sfa=dd; pc=p
            if dd<0.08 and inside(fbv,p): sin+=1
        w['str_in_body']=max(w.get('str_in_body',0),sin)
        if not draw and an!='attack':   # v5d: which side of the forearm the string touches (+1 = straight at the body, -x) and limb further out than the string
            fb=arm.pose.bones[P+'LeftForeArm']; h0=arm.matrix_world@fb.head; t0=arm.matrix_world@fb.tail; dv_=t0-h0
            tt=max(0.0,min(1.0,(pc-h0).dot(dv_)/dv_.dot(dv_))); vin=(pc-(h0+dv_*tt)); vin.z=0
            w['inner']=round(min(w.get('inner',1.0),-vin.normalized().x),2)
            sm=Vector(((a+b)/2).tolist()); w['limb_out']=round(min(w.get('limb_out',1.0),g.x-sm.x),3); upz=(B@Vector((0,1,0))).z   # string vs the bow forearm (pressed against it, 0.5-1 cm), string side up
        ax=B@Vector((0,0,1)); tilt=math.degrees(math.acos(abs(ax.z))); out=(B@Vector((0,1,0))).x
        fy=(arm.pose.bones[P+'LeftForeArm'].matrix.to_3x3()).col[1].normalized(); hy=R.col[1].normalized(); wr=math.degrees(fy.angle(hy))
        mid=(arm.pose.bones[P+'LeftForeArm'].head+arm.pose.bones[P+'LeftForeArm'].tail)/2; dv=mid-g; bfa=(dv-ax*dv.dot(ax)).length
        w['limb']=max(w['limb'],nl); w['coat']=max(w['coat'],nc); w['fa_in_bow']=max(w['fa_in_bow'],nf); w['str_body']=min(w['str_body'],round(sb,4)); w['str_coat']=min(w['str_coat'],round(scp,4))
        if an!='attack': w['str_fa_min']=min(w['str_fa_min'],round(sfa,4)); w['str_fa_max']=max(w['str_fa_max'],round(sfa,4)); w['up']=min(w['up'],round(upz,2)); w['tilt']=max(w['tilt'],round(tilt,1)); w['out']=min(w['out'],round(out,2)); w['wrist']=max(w['wrist'],round(wr,1)); w['bow_fa']=max(w['bow_fa'],round(bfa,3))
        if nl or nc or nf or sb<0.02 or scp<0.02: w.setdefault('bad',[]).append((f,nl,nc,nf,round(sb,3),round(scp,3)))
    rep[an]=w
print('CARRY',json.dumps(rep))
json.dump(rep,open(os.environ.get('OUTCC',W+'/bowgrip/carry_check.json'),'w'),indent=1)
