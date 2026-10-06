"""Creature rig pipeline for Nightfang (quad) and Ashwyrm (dragon).
Usage: blender -b --python creature_pipeline.py -- --glb PATH --out DIR --name NAME --kind quad|dragon
"""
import bpy, sys, os, math, time, json
import numpy as np
from mathutils import Vector, Matrix

ARGS=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
def getopt(f,d=None):
    if f in ARGS:
        i=ARGS.index(f); return ARGS[i+1] if i+1<len(ARGS) else d
    return d
GLB=getopt('--glb'); OUT=getopt('--out'); NAME=getopt('--name'); KIND=getopt('--kind','quad')
TARGET=int(getopt('--target-faces','200000')); STAGE=getopt('--stage','all')
assert GLB and OUT and NAME
WORK=os.path.join(OUT,'work'); STILLS=os.path.join(OUT,'stills')
os.makedirs(WORK,exist_ok=True); os.makedirs(STILLS,exist_ok=True)
LOG=[]
def log(m): print(m); LOG.append(str(m))

def clear(): bpy.ops.wm.read_factory_settings(use_empty=True)
def verts_np(me):
    n=len(me.data.vertices); co=np.empty(n*3); me.data.vertices.foreach_get('co',co); return co.reshape(-1,3)
def set_verts(me,co):
    me.data.vertices.foreach_set('co',co.ravel()); me.data.update()

def stage_prep():
    clear(); bpy.ops.import_scene.gltf(filepath=GLB)
    for o in list(bpy.data.objects):
        if o.type=='ARMATURE': bpy.data.objects.remove(o, do_unlink=True)
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    if len(meshes)>1:
        bpy.ops.object.select_all(action='DESELECT')
        for o in meshes: o.select_set(True)
        bpy.context.view_layer.objects.active=meshes[0]; bpy.ops.object.join()
    me=bpy.context.view_layer.objects.active; me.name=f'{NAME}_body'
    bpy.ops.object.select_all(action='DESELECT'); me.select_set(True); bpy.context.view_layer.objects.active=me
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=1e-4); bpy.ops.mesh.delete_loose()
    bpy.ops.object.mode_set(mode='OBJECT')
    nfaces=len(me.data.polygons); log(f'faces {nfaces}')
    if nfaces>TARGET*1.15:
        mod=me.modifiers.new('Dec','DECIMATE'); mod.ratio=max(0.05,TARGET/nfaces)
        bpy.ops.object.modifier_apply(modifier='Dec'); log(f'decimated {len(me.data.polygons)}')
    co=verts_np(me); DZ=-float(co[:,2].min()); co[:,2]+=DZ; set_verts(me,co)
    open(os.path.join(WORK,'dz.txt'),'w').write(str(DZ))
    for i,im in enumerate(list(bpy.data.images)):
        if im.size[0]==0: continue
        kind=['basecolor','metal_rough','normal','other'][min(i,3)]
        im.name=f'{NAME}_{kind}_{i}'; im.filepath_raw=f'//textures/{im.name}.jpg'
        try:
            if im.packed_file is None: im.pack()
        except: pass
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK,'dec.blend'), compress=False)
    np.save(os.path.join(WORK,'verts.npy'), verts_np(me)); log(f'DZ {DZ}')

def body_at(co, mask, z, hz):
    m=mask&(np.abs(co[:,2]-z)<hz)
    if m.sum()<8: m=np.abs(co[:,2]-z)<hz
    return co[m].mean(0) if m.sum() else np.array([0.,0.,z])

def analyze_quad(co):
    """Wolf-like: spine along +Y (nose) to -Y (tail), +Z up."""
    z0,z1=float(co[:,2].min()),float(co[:,2].max()); H=z1-z0
    y0,y1=float(co[:,1].min()),float(co[:,1].max()); L=y1-y0
    # face forward = +Y (nose tip)
    face_forward=1.0 if abs(y1)>abs(y0) else -1.0
    body=np.abs(co[:,0])<np.percentile(np.abs(co[:,0]),70)*0.55
    # hips: rear of body, mid height
    hips_y = y0 + (0.28 if face_forward>0 else 0.72)*L  # toward tail
    if face_forward>0: hips_y=y0+0.30*L
    else: hips_y=y1-0.30*L
    chest_y = y0 + (0.70 if face_forward>0 else 0.30)*L
    if face_forward>0: chest_y=y0+0.68*L
    else: chest_y=y1-0.68*L
    hips_z=z0+0.45*H; chest_z=z0+0.50*H
    hips=body_at(co,body,hips_z,0.03*H); hips[1]=hips_y; hips[2]=hips_z
    chest=body_at(co,body,chest_z,0.03*H); chest[1]=chest_y; chest[2]=chest_z
    # spine samples along Y
    def along(t):
        y=hips_y + t*(chest_y-hips_y)
        c=body_at(co,body,hips_z+t*0.05*H,0.03*H); c[1]=y; return c
    s1=along(0.33); s2=along(0.66)
    # neck/head toward face
    nose_y = y1 if face_forward>0 else y0
    neck_y = chest_y + 0.55*(nose_y-chest_y)
    head_y = chest_y + 0.85*(nose_y-chest_y)
    neck=np.array([0.,neck_y,z0+0.62*H]); head=np.array([0.,head_y,z0+0.70*H]); head_end=np.array([0.,nose_y-face_forward*0.02*L,z0+0.72*H])
    # tail: from hips opposite face
    tail_dir=-face_forward
    tail_tip_y = y0 if face_forward>0 else y1
    tails=[]
    for i in range(8):
        t=(i+1)/8.0
        y=hips_y + t*(tail_tip_y-hips_y)*0.95
        z=hips_z - 0.05*H*t
        tails.append(np.array([0.,y,z]))
    # legs: four limbs
    def leg(side, fore):
        sign=1.0 if side=='L' else -1.0
        y_c = chest_y if fore else hips_y
        mask=(np.abs(co[:,1]-y_c)<0.18*L)&(sign*co[:,0]>0.05)&(co[:,2]<hips_z+0.05*H)
        if mask.sum()<40:
            x=sign*0.18*H; 
            up=np.array([x,y_c,hips_z]); knee=np.array([x,y_c-face_forward*0.05*L,z0+0.28*H])
            ankle=np.array([x,y_c,z0+0.06*H]); toe=np.array([x,y_c+face_forward*0.08*L,z0+0.02*H])
            return dict(up=up,knee=knee,ankle=ankle,toe=toe,tip=toe+np.array([0,face_forward*0.05*L,0]))
        p=co[mask]
        # knee narrow
        kc=[]
        for z in np.linspace(z0+0.2*H,z0+0.4*H,16):
            m=mask&(np.abs(co[:,2]-z)<0.02*H)
            if m.sum()<10: continue
            kc.append((z,co[m,0].max()-co[m,0].min(),co[m].mean(0)))
        knee=min(kc,key=lambda t:t[1])[2] if kc else p.mean(0)
        up=p[p[:,2]>np.percentile(p[:,2],75)].mean(0) if (p[:,2]>np.percentile(p[:,2],75)).sum()>5 else p.mean(0)
        up[2]=hips_z-0.02*H if not fore else chest_z-0.05*H
        ankle=p[p[:,2]<np.percentile(p[:,2],20)].mean(0); ankle[2]=z0+0.05*H
        tip=p[np.argmax(p[:,1]*face_forward)].copy(); tip[2]=z0+0.015*H
        toe=ankle+0.5*(tip-ankle); toe[2]=z0+0.02*H
        return dict(up=up,knee=knee,ankle=ankle,toe=toe,tip=tip)
    return dict(H=H,L=L,z0=z0,face_forward=face_forward,hips=hips,chest=chest,s1=s1,s2=s2,
                neck=neck,head=head,head_end=head_end,tails=tails,
                FL=leg('L',True),FR=leg('R',True),HL=leg('L',False),HR=leg('R',False))

def analyze_dragon(co):
    """Bipedal winged dragon like Emberfang/Ashwyrm: -Y forward typically after ground."""
    z0,z1=float(co[:,2].min()),float(co[:,2].max()); H=z1-z0
    y0,y1=float(co[:,1].min()),float(co[:,1].max())
    # detect face: head high verts extreme Y
    head_m=co[:,2]>z0+0.75*H
    face_forward = -1.0  # Emberfang style -Y
    if head_m.sum()>50:
        hy=co[head_m,1]
        face_forward = 1.0 if abs(hy.max())>=abs(hy.min()) else -1.0
    body=np.abs(co[:,0])<0.18
    hips_z=z0+0.48*H; sh_z=z0+0.78*H
    hips=body_at(co,body,hips_z,0.025*H); hips[2]=hips_z
    # spine toward face
    def spine_pt(t):
        z=hips_z+t*(sh_z-hips_z)
        c=body_at(co,body,z,0.025*H); c[2]=z
        c[1]=hips[1]+t*face_forward*(-0.15*H)  # slight forward lean of chest
        return c
    # better: sample body centroids along Y toward face at rising Z
    s0=spine_pt(0.2); s1=spine_pt(0.45); s2=spine_pt(0.7); chest=spine_pt(0.9)
    neck_z=z0+0.88*H; head_z=z0+0.95*H
    neck=body_at(co,body,neck_z,0.02*H); neck[2]=neck_z; neck[1]=chest[1]+face_forward*0.05*H
    head=np.array([0., neck[1]+face_forward*0.08*H, head_z])
    head_end=np.array([0., head[1]+face_forward*0.12*H, head_z+0.02*H])
    jaw=np.array([0., head[1]+face_forward*0.06*H, head_z-0.04*H])
    jaw_end=np.array([0., jaw[1]+face_forward*0.08*H, jaw[2]-0.03*H])
    # tail away from face
    tails=[]
    for i in range(8):
        t=(i+1)/8
        tails.append(np.array([0., hips[1]-face_forward*(0.05+0.55*t)*H, hips_z-0.08*t*H]))
    # hind legs
    def hind(side):
        sign=1.0 if side=='L' else -1.0
        mask=(sign*co[:,0]>0.04)&(co[:,2]<hips_z)&(np.abs(co[:,1]-hips[1])<0.25*H)
        if mask.sum()<50:
            x=sign*0.12*H
            return dict(up=np.array([x,hips[1],hips_z]),knee=np.array([x,hips[1]-face_forward*0.05*H,z0+0.28*H]),
                        ankle=np.array([x,hips[1],z0+0.06*H]),toe=np.array([x,hips[1]+face_forward*0.08*H,z0+0.02*H]),
                        tip=np.array([x,hips[1]+face_forward*0.12*H,z0+0.015*H]))
        p=co[mask]; up=p[p[:,2]>np.percentile(p[:,2],70)].mean(0); up[2]=hips_z-0.02*H
        kc=[]
        for z in np.linspace(z0+0.2*H,z0+0.38*H,14):
            m=mask&(np.abs(co[:,2]-z)<0.02*H)
            if m.sum()>8: kc.append((z,co[m,0].max()-co[m,0].min(),co[m].mean(0)))
        knee=min(kc,key=lambda t:t[1])[2] if kc else p.mean(0)
        ankle=p[p[:,2]<np.percentile(p[:,2],15)].mean(0); ankle[2]=z0+0.05*H
        tip=p[np.argmax(p[:,1]*face_forward)].copy(); tip[2]=z0+0.015*H
        toe=ankle+0.55*(tip-ankle); toe[2]=z0+0.02*H
        return dict(up=up,knee=knee,ankle=ankle,toe=toe,tip=tip)
    # small arms
    def arm(side):
        sign=1.0 if side=='L' else -1.0
        mask=(sign*co[:,0]>0.08)&(co[:,2]>hips_z)&(co[:,2]<sh_z+0.05*H)&(np.abs(co[:,1]-chest[1])<0.35*H)
        sh=np.array([sign*0.12*H, chest[1], sh_z])
        if mask.sum()<40:
            return dict(sh=sh,elbow=sh+np.array([sign*0.05*H,face_forward*0.05*H,-0.15*H]),
                        wrist=sh+np.array([sign*0.06*H,face_forward*0.1*H,-0.28*H]),
                        hand=sh+np.array([sign*0.07*H,face_forward*0.14*H,-0.35*H]))
        p=co[mask]; mu=p.mean(0); X=p-mu;_,_,vh=np.linalg.svd(X,full_matrices=False); d=vh[0]
        if d[2]>0: d=-d
        if sign*d[0]<0: d=-d
        t=(p-mu)@d; 
        def at(f):
            tt=t.min()+f*(t.max()-t.min()); m=np.abs(t-tt)<0.08*(t.max()-t.min())
            return p[m].mean(0) if m.sum()>=5 else mu+d*tt
        return dict(sh=sh,elbow=at(0.45),wrist=at(0.8),hand=at(0.95))
    # wings: verts with large |x|
    def wing(side):
        sign=1.0 if side=='L' else -1.0
        mask=(sign*co[:,0]>0.15)&(co[:,2]>hips_z)
        if mask.sum()<100:
            root=np.array([sign*0.05*H,chest[1],sh_z]); arm_=root+np.array([sign*0.2*H,0,0.05*H])
            fore=arm_+np.array([sign*0.2*H,-face_forward*0.05*H,0.1*H])
            tips=[fore+np.array([sign*0.15*H,face_forward*k*0.1*H,0.05*H]) for k in range(4)]
            return dict(root=root,arm=arm_,fore=fore,tips=tips)
        p=co[mask]
        # root near body
        root_m=mask&(np.abs(co[:,0])<0.25)
        root=co[root_m].mean(0) if root_m.sum()>20 else p[np.argmin(np.abs(p[:,0]))].copy()
        tip=p[np.argmax(sign*p[:,0])]
        mid=p.mean(0)
        # PCA wing plane
        X=p-p.mean(0);_,_,vh=np.linalg.svd(X,full_matrices=False)
        # along span = max |x| direction
        arm_pt=root+0.35*(tip-root); fore_pt=root+0.65*(tip-root)
        tips=[]
        for k in range(4):
            # fan tips
            tips.append(fore_pt + (tip-fore_pt)*(0.4+0.2*k)/1.2 + np.array([0, face_forward*(k-1.5)*0.08*H, (1-k)*0.03*H]))
        return dict(root=root,arm=arm_pt,fore=fore_pt,tips=tips,tip=tip)
    return dict(H=H,z0=z0,face_forward=face_forward,hips=hips,s0=s0,s1=s1,s2=s2,chest=chest,
                neck=neck,head=head,head_end=head_end,jaw=jaw,jaw_end=jaw_end,tails=tails,
                HL=hind('L'),HR=hind('R'),AL=arm('L'),AR=arm('R'),WL=wing('L'),WR=wing('R'),sh_z=sh_z)

def make_bone(ad,name,head,tail,parent=None,connect=False):
    eb=ad.edit_bones.new(name)
    eb.head=Vector(np.array(head,float)); eb.tail=Vector(np.array(tail,float))
    if parent: eb.parent=ad.edit_bones[parent]; eb.use_connect=connect
    eb.use_deform=(name!='root')
    return eb

def build_quad(J):
    ad=bpy.data.armatures.new(f'{NAME}_rig_data'); ad.display_type='OCTAHEDRAL'
    arm=bpy.data.objects.new(f'{NAME}_rig',ad); bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
    hips=J['hips']; chest=J['chest']
    make_bone(ad,'root',(hips[0],hips[1],J['z0']),(hips[0],hips[1]-0.1,J['z0']))
    make_bone(ad,'hips',hips,J['s1'],'root',False)
    make_bone(ad,'spine_01',J['s1'],J['s2'],'hips',True)
    make_bone(ad,'spine_02',J['s2'],chest,'spine_01',True)
    make_bone(ad,'chest',chest,J['neck'],'spine_02',True)
    make_bone(ad,'neck_01',J['neck'],(J['neck']+J['head'])/2,'chest',True)
    make_bone(ad,'neck_02',(J['neck']+J['head'])/2,J['head'],'neck_01',True)
    make_bone(ad,'head',J['head'],J['head_end'],'neck_02',True)
    # jaw optional short
    make_bone(ad,'jaw',J['head']*0.7+J['head_end']*0.3+np.array([0,0,-0.03*J['H']]), J['head_end']+np.array([0,0,-0.05*J['H']]),'head',False)
    prev='hips'
    for i,tpos in enumerate(J['tails']):
        name=f'tail_0{i+1}'
        head = J['hips'] if i==0 else J['tails'][i-1]
        make_bone(ad,name,head,tpos, prev if i==0 else f'tail_0{i}', i>0)
        prev=name
    for tag,leg,parent in (('front_L',J['FL'],'chest'),('front_R',J['FR'],'chest'),('hind_L',J['HL'],'hips'),('hind_R',J['HR'],'hips')):
        side='L' if tag.endswith('L') else 'R'
        pref='shoulder' if 'front' in tag else 'thigh'
        if 'front' in tag:
            make_bone(ad,f'upperarm.{side}',leg['up'],leg['knee'],parent,False)
            make_bone(ad,f'forearm.{side}',leg['knee'],leg['ankle'],f'upperarm.{side}',True)
            make_bone(ad,f'hand.{side}',leg['ankle'],leg['toe'],f'forearm.{side}',True)
            make_bone(ad,f'toe.{side}',leg['toe'],leg['tip'],f'hand.{side}',True)
        else:
            make_bone(ad,f'thigh.{side}',leg['up'],leg['knee'],parent,False)
            make_bone(ad,f'shin.{side}',leg['knee'],leg['ankle'],f'thigh.{side}',True)
            make_bone(ad,f'foot.{side}',leg['ankle'],leg['toe'],f'shin.{side}',True)
            make_bone(ad,f'toe_h.{side}',leg['toe'],leg['tip'],f'foot.{side}',True)
    bpy.ops.object.mode_set(mode='OBJECT'); log(f'quad bones {len(ad.bones)}'); return arm

def build_dragon(J):
    ad=bpy.data.armatures.new(f'{NAME}_rig_data'); ad.display_type='OCTAHEDRAL'
    arm=bpy.data.objects.new(f'{NAME}_rig',ad); bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
    hips=J['hips']
    make_bone(ad,'root',(0,hips[1],J['z0']),(0,hips[1]-0.15,J['z0']))
    make_bone(ad,'hips',hips,J['s0'],'root',False)
    make_bone(ad,'spine_01',J['s0'],J['s1'],'hips',True)
    make_bone(ad,'spine_02',J['s1'],J['s2'],'spine_01',True)
    make_bone(ad,'spine_03',J['s2'],J['chest'],'spine_02',True)
    make_bone(ad,'chest',J['chest'],J['neck'],'spine_03',True)
    make_bone(ad,'neck_01',J['neck'],(J['neck']+J['head'])/2,'chest',True)
    make_bone(ad,'neck_02',(J['neck']+J['head'])/2,J['head'],'neck_01',True)
    make_bone(ad,'head',J['head'],J['head_end'],'neck_02',True)
    make_bone(ad,'jaw',J['jaw'],J['jaw_end'],'head',False)
    for i,tpos in enumerate(J['tails']):
        head=J['hips'] if i==0 else J['tails'][i-1]
        make_bone(ad,f'tail_0{i+1}',head,tpos,'hips' if i==0 else f'tail_0{i}', i>0)
    for side,leg in (('L',J['HL']),('R',J['HR'])):
        make_bone(ad,f'thigh.{side}',leg['up'],leg['knee'],'hips',False)
        make_bone(ad,f'shin.{side}',leg['knee'],leg['ankle'],f'thigh.{side}',True)
        make_bone(ad,f'foot.{side}',leg['ankle'],leg['toe'],f'shin.{side}',True)
        make_bone(ad,f'toe.{side}',leg['toe'],leg['tip'],f'foot.{side}',True)
    for side,A in (('L',J['AL']),('R',J['AR'])):
        make_bone(ad,f'upperarm.{side}',A['sh'],A['elbow'],'chest',False)
        make_bone(ad,f'forearm.{side}',A['elbow'],A['wrist'],f'upperarm.{side}',True)
        make_bone(ad,f'hand.{side}',A['wrist'],A['hand'],f'forearm.{side}',True)
    for side,W in (('L',J['WL']),('R',J['WR'])):
        make_bone(ad,f'wing_root.{side}',W['root'],W['arm'],'chest',False)
        make_bone(ad,f'wing_arm.{side}',W['arm'],W['fore'],f'wing_root.{side}',True)
        make_bone(ad,f'wing_forearm.{side}',W['fore'],W['tips'][0],f'wing_arm.{side}',True)
        for i,tip in enumerate(W['tips']):
            prev=W['fore'] if i==0 else W['tips'][i-1]
            # finger from forearm
            make_bone(ad,f'wing_f{i+1}.{side}',W['fore'],tip,f'wing_forearm.{side}',False)
    bpy.ops.object.mode_set(mode='OBJECT'); log(f'dragon bones {len(ad.bones)}'); return arm

def clean_simple(me, arm):
    names=[g.name for g in me.vertex_groups]; idx={n:i for i,n in enumerate(names)}
    n=len(me.data.vertices); G=len(names)
    W=np.zeros((n,G),np.float32)
    for v in me.data.vertices:
        for g in v.groups:
            if g.group<G: W[v.index,g.group]=g.weight
    co=verts_np(me); x=co[:,0]
    def sm(v):
        v=np.clip(v,0,1); return v*v*(3-2*v)
    left=[i for n,i in idx.items() if n.endswith('.L') or '.L' in n]
    right=[i for n,i in idx.items() if n.endswith('.R') or '.R' in n]
    if right:
        fac=sm((-x+0.02)/0.06); 
        for i in right: W[:,i]*=fac
    if left:
        fac=sm((x+0.02)/0.06)
        for i in left: W[:,i]*=fac
    for vi in range(n):
        w=W[vi]
        if (w>0).sum()<=4: continue
        thr=np.partition(w,-5)[-5]; W[vi]=np.maximum(w-thr,0)
    s=W.sum(1,keepdims=True); miss=s[:,0]<1e-8
    if miss.any():
        hi=idx.get('hips',0); W[miss,hi]=1; s=W.sum(1,keepdims=True)
    s[s<1e-12]=1; W/=s
    for g in list(me.vertex_groups): me.vertex_groups.remove(g)
    vgs=[me.vertex_groups.new(name=n) for n in names]
    order=np.argsort(-W,axis=1)[:,:4]
    for gi,vg in enumerate(vgs):
        buckets={}
        for vi in range(n):
            for k in range(4):
                j=int(order[vi,k]); w=float(W[vi,j])
                if j==gi and w>1e-6:
                    buckets.setdefault(round(w,4),[]).append(vi); break
        for w,idxs in buckets.items():
            for a in range(0,len(idxs),8000): vg.add(idxs[a:a+8000],float(w),'REPLACE')
    log(f'weights maxinfl={(W>0).sum(1).max()} unw={int(((W>0).sum(1)==0).sum())}')
    return W

def stage_build():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(WORK,'dec.blend'))
    me=bpy.data.objects[f'{NAME}_body']; co=verts_np(me)
    if KIND=='quad': J=analyze_quad(co); arm=build_quad(J)
    else: J=analyze_dragon(co); arm=build_dragon(J)
    def ser(o):
        if isinstance(o,np.ndarray): return o.tolist()
        if isinstance(o,(np.floating,)): return float(o)
        if isinstance(o,(np.integer,)): return int(o)
        if isinstance(o,dict): return {k:ser(v) for k,v in o.items()}
        if isinstance(o,(list,tuple)): return [ser(v) for v in o]
        return o
    open(os.path.join(WORK,'joints.json'),'w').write(json.dumps(ser(J),indent=2))
    bpy.ops.object.select_all(action='DESELECT'); me.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
    try:
        r=bpy.ops.object.parent_set(type='ARMATURE_AUTO'); log(f'ARMATURE_AUTO {r}')
    except Exception as e:
        log(f'heat fail {e}')
    unw=sum(1 for v in me.data.vertices if not any(g.weight>1e-6 for g in v.groups))
    log(f'unweighted {unw}')
    if unw>0.2*len(me.data.vertices) or len(me.vertex_groups)==0:
        open(os.path.join(OUT,'FAIL.md'),'w').write(f'# FAIL {NAME}\n\nHeat weighting failed. unweighted={unw}\n'); raise SystemExit(2)
    clean_simple(me,arm)
    for m in me.modifiers:
        if m.type=='ARMATURE': m.name='Armature'; m.object=arm
    for im in bpy.data.images:
        if im.source in {'VIEWER','GENERATED'}: continue
        try:
            if im.packed_file is None: im.pack()
        except: pass
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'), compress=False)

def reset(arm):
    for b in arm.pose.bones:
        b.location=(0,0,0); b.rotation_mode='QUATERNION'; b.rotation_quaternion=(1,0,0,0); b.scale=(1,1,1)
    bpy.context.view_layer.update()
def rotw(arm,name,axis,deg):
    if name not in arm.pose.bones: return
    b=arm.pose.bones[name]; M=b.matrix.copy(); h=M.translation.copy()
    b.matrix=Matrix.Translation(h)@Matrix.Rotation(math.radians(deg),4,Vector(axis).normalized())@Matrix.Translation(-h)@M
    bpy.context.view_layer.update()

def make_clip(arm):
    if not arm.animation_data: arm.animation_data_create()
    act=bpy.data.actions.new(f'{NAME}_clip'); act.use_fake_user=True
    arm.animation_data.action=act; nf=30
    for f in range(nf+1):
        p=2*math.pi*f/nf; reset(arm)
        if KIND=='quad':
            a=18*math.sin(p); b=18*math.sin(p+math.pi)
            for nm,ang in (('thigh.L',a),('thigh.R',b),('upperarm.L',b),('upperarm.R',a)):
                rotw(arm,nm,(1,0,0),ang)
            for i in range(1,9):
                if f'tail_0{i}' in arm.pose.bones: rotw(arm,f'tail_0{i}',(0,0,1),4*math.sin(p-0.3*i))
        else:
            # wing flap
            c=math.cos(p)
            rotw(arm,'wing_arm.L',(0,-1,0),25*c); rotw(arm,'wing_arm.R',(0,-1,0),25*c)
            rotw(arm,'wing_forearm.L',(0,-1,0),15*c); rotw(arm,'wing_forearm.R',(0,-1,0),15*c)
            rotw(arm,'hips',(1,0,0),3*math.sin(p))
        for bone in arm.pose.bones:
            bone.rotation_mode='QUATERNION'
            if bone.parent: local=bone.parent.matrix.inverted()@bone.matrix
            else: local=bone.matrix
            bone.rotation_quaternion=local.to_quaternion()
            bone.keyframe_insert('rotation_quaternion',frame=f,group=bone.name)
            bone.keyframe_insert('location',frame=f,group=bone.name)
    for fc in act.fcurves:
        for k in fc.keyframe_points: k.interpolation='LINEAR'
        try: fc.modifiers.new('CYCLES')
        except: pass
    return act

def stage_pose():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'))
    arm=bpy.data.objects[f'{NAME}_rig']; me=bpy.data.objects[f'{NAME}_body']
    if arm.animation_data: arm.animation_data.action=None
    co=verts_np(me); H=float(co[:,2].max()); cy=float(co[:,1].mean()); cz=0.45*H
    sc=bpy.context.scene
    engines={e.identifier for e in sc.render.bl_rna.properties['engine'].enum_items}
    sc.render.engine='BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in engines else 'BLENDER_WORKBENCH'
    sc.render.resolution_x=sc.render.resolution_y=900
    w=bpy.data.worlds.new('W'); sc.world=w; w.use_nodes=True
    w.node_tree.nodes['Background'].inputs[0].default_value=(0.06,0.06,0.07,1)
    w.node_tree.nodes['Background'].inputs[1].default_value=0.35
    for name,loc,e in (('k',(2.5,-2,2.2),70),('f',(-2,-1,1.5),28),('r',(0,2.5,2),35)):
        l=bpy.data.lights.new(name,'AREA'); l.energy=e; l.size=2
        o=bpy.data.objects.new(name,l); sc.collection.objects.link(o); o.location=loc
    camd=bpy.data.cameras.new('cam'); cam=bpy.data.objects.new('cam',camd); sc.collection.objects.link(cam); sc.camera=cam; camd.lens=50
    def aim(dire,dist):
        tgt=Vector((0,cy,cz)); d=Vector(dire).normalized(); cam.location=tgt+d*dist
        cam.rotation_euler=(tgt-cam.location).to_track_quat('-Z','Y').to_euler()
    def render(path):
        sc.render.filepath=path; bpy.ops.render.render(write_still=True)
    def overlay(src,dst):
        try:
            from PIL import Image,ImageDraw
            from bpy_extras.object_utils import world_to_camera_view
            im=Image.open(src).convert('RGBA'); dr=ImageDraw.Draw(im); W,Hpix=im.size
            for b in arm.pose.bones:
                h=arm.matrix_world@b.head; t=arm.matrix_world@b.tail
                ch=world_to_camera_view(sc,cam,h); ct=world_to_camera_view(sc,cam,t)
                x1,y1=ch.x*W,(1-ch.y)*Hpix; x2,y2=ct.x*W,(1-ct.y)*Hpix
                dr.line([(x1,y1),(x2,y2)],fill=(255,140,40,255),width=3)
            im.convert('RGB').save(dst)
        except Exception as e: log(f'overlay {e}')
    poses=[]
    def pose_rest(arm):
        reset(arm)
    def pose_a(arm):
        reset(arm)
        if KIND=='quad':
            rotw(arm,'thigh.L',(1,0,0),25); rotw(arm,'thigh.R',(1,0,0),-20)
            rotw(arm,'upperarm.L',(1,0,0),-20); rotw(arm,'upperarm.R',(1,0,0),25)
            for i in range(1,9): rotw(arm,f'tail_0{i}',(0,0,1),6)
        else:
            rotw(arm,'wing_arm.L',(0,-1,0),30); rotw(arm,'wing_arm.R',(0,-1,0),30)
            rotw(arm,'neck_01',(1,0,0),-8)
    def pose_b(arm):
        reset(arm)
        if KIND=='quad':
            rotw(arm,'neck_01',(1,0,0),-15); rotw(arm,'head',(1,0,0),10)
            rotw(arm,'spine_01',(0,0,1),8)
            for i in range(1,9): rotw(arm,f'tail_0{i}',(0,0,1),-8)
        else:
            rotw(arm,'wing_arm.L',(0,1,0),25); rotw(arm,'wing_arm.R',(0,1,0),25)
            rotw(arm,'thigh.L',(1,0,0),20); rotw(arm,'thigh.R',(1,0,0),-15)
            rotw(arm,'jaw',(1,0,0),25)
    poses=[('A_rest',pose_rest),('B_action',pose_a),('C_alt',pose_b)]
    for pname,fn in poses:
        if arm.animation_data: arm.animation_data.action=None
        fn(arm)
        for vname,dire,dist in (('front',(0,-1,0.15),2.6*H),('q34',(0.9,-1,0.25),2.8*H)):
            aim(dire,dist); path=os.path.join(STILLS,f'{pname}_{vname}.png'); render(path)
            overlay(path, os.path.join(STILLS,f'{pname}_{vname}_bones.png'))
            log(f'rendered {pname}_{vname}')
    act=make_clip(arm); arm.animation_data.action=act
    sc.frame_start=0; sc.frame_end=30; sc.render.fps=30
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'), compress=False)
    # sheet
    try:
        from PIL import Image,ImageDraw,ImageFont
        Image.MAX_IMAGE_PIXELS=None
        F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',18)
        FT=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',24)
        rows=[]
        for pname,_ in poses:
            ims=[f'{pname}_front',f'{pname}_q34',f'{pname}_front_bones',f'{pname}_q34_bones']
            ims=[i for i in ims if os.path.exists(os.path.join(STILLS,i+'.png'))]
            if ims: rows.append((pname,ims))
        T=400; cols=4; W=T*cols; Ht=50+len(rows)*(30+T)
        sheet=Image.new('RGB',(W,Ht),(28,28,31)); d=ImageDraw.Draw(sheet)
        d.text((10,10),f'{NAME} {KIND} rig | orange=bones',font=FT,fill=(240,240,240))
        y=50
        for lab,ims in rows:
            d.text((8,y),lab,font=F,fill=(255,190,90)); y+=28
            for i,n in enumerate(ims[:4]):
                im=Image.open(os.path.join(STILLS,n+'.png')).convert('RGB').resize((T,T))
                sheet.paste(im,(i*T,y))
            y+=T
        sheet.save(os.path.join(OUT,f'{NAME}_blenderig_sheet.jpg'),quality=92)
        log('sheet written')
    except Exception as e: log(f'sheet fail {e}')

def stage_export():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'))
    arm=bpy.data.objects[f'{NAME}_rig']; me=bpy.data.objects[f'{NAME}_body']
    for im in bpy.data.images:
        if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
        if not im.filepath_raw: im.filepath_raw=f'//textures/{im.name}.jpg'
        try:
            if im.packed_file is None: im.pack()
        except: pass
    common=dict(use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL',apply_unit_scale=True,add_leaf_bones=False,
        path_mode='COPY',embed_textures=True,use_mesh_modifiers=False,mesh_smooth_type='FACE')
    if arm.animation_data: arm.animation_data.action=None
    reset(arm)
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); me.select_set(True)
    bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'{NAME}_blenderig.fbx'),bake_anim=False,**common)
    clip=None
    for a in bpy.data.actions:
        if 'clip' in a.name.lower() or 'walk' in a.name.lower() or 'flap' in a.name.lower() or 'trot' in a.name.lower():
            clip=a; break
    if not clip and bpy.data.actions: clip=bpy.data.actions[0]
    clip_name='trot' if KIND=='quad' else 'wingflap'
    if clip:
        arm.animation_data_create(); arm.animation_data.action=clip
        sc=bpy.context.scene; sc.frame_start=0; sc.frame_end=30
        bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'{NAME}_blenderig_{clip_name}.fbx'),bake_anim=True,
            bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,
            bake_anim_force_startend_keying=True,bake_anim_step=1.0,bake_anim_simplify_factor=0.0,**common)
    log('export done')

def stage_verify():
    results={}
    for fname,tag in [(f'{NAME}_blenderig.fbx','rig'),(f'{NAME}_blenderig_trot.fbx','clip'),(f'{NAME}_blenderig_wingflap.fbx','clip')]:
        path=os.path.join(OUT,fname)
        if not os.path.exists(path): continue
        clear(); bpy.ops.import_scene.fbx(filepath=path)
        arms=[o for o in bpy.data.objects if o.type=='ARMATURE']; meshes=[o for o in bpy.data.objects if o.type=='MESH']
        info=dict(file=fname,bones=len(arms[0].data.bones) if arms else 0,meshes=[])
        for m in meshes:
            unw=sum(1 for v in m.data.vertices if not any(g.weight>1e-6 for g in v.groups))
            info['meshes'].append(dict(name=m.name,faces=len(m.data.polygons),verts=len(m.data.vertices),unweighted=unw,groups=len(m.vertex_groups)))
        info['images']=[(im.name,tuple(im.size),im.packed_file is not None) for im in bpy.data.images if im.size[0]>0]
        info['actions']=[a.name for a in bpy.data.actions]
        if tag=='clip' and meshes and bpy.data.actions:
            m=meshes[0]; sc=bpy.context.scene
            def evco(f):
                sc.frame_set(int(f)); dg=bpy.context.evaluated_depsgraph_get(); e=m.evaluated_get(dg).to_mesh()
                c=np.empty(len(e.vertices)*3); e.vertices.foreach_get('co',c); m.evaluated_get(dg).to_mesh_clear(); return c.reshape(-1,3)
            fr=bpy.data.actions[0].frame_range; c0=evco(fr[0]); c1=evco(0.5*(fr[0]+fr[1]))
            d=np.linalg.norm(c1-c0,axis=1); info['deform_max']=float(d.max()); info['deform_mean']=float(d.mean())
        results[tag]=info; log(f'verify {tag} {info}')
    open(os.path.join(WORK,'verify.json'),'w').write(json.dumps(results,indent=2,default=str))

if __name__=='__main__':
    log(f'=== {NAME} {KIND} stage={STAGE} ===')
    if STAGE in ('all','prep'): stage_prep()
    if STAGE in ('all','build'): stage_build()
    if STAGE in ('all','pose'): stage_pose()
    if STAGE in ('all','export'): stage_export()
    if STAGE in ('all','verify'): stage_verify()
    log('DONE')
