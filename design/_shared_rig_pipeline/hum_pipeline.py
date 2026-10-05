"""
Reusable Mixamo-compatible humanoid blender-rig pipeline.
Usage (from blender -b):
  blender -b --python hum_pipeline.py -- \
    --glb PATH --out DIR --name NAME [--target-faces 200000] [--look JPG]
"""
import bpy, sys, os, math, time, json, shutil
import numpy as np
from mathutils import Vector, Matrix, Quaternion

ARGS=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
def getopt(flag, default=None):
    if flag in ARGS:
        i=ARGS.index(flag); return ARGS[i+1] if i+1<len(ARGS) else default
    return default
def has(flag): return flag in ARGS

GLB=getopt('--glb'); OUT=getopt('--out'); NAME=getopt('--name')
TARGET=int(getopt('--target-faces','200000'))
LOOK=getopt('--look','')
STAGE=getopt('--stage','all')  # all|prep|build|pose|export|verify
assert GLB and OUT and NAME, 'need --glb --out --name'

sys.path.insert(0, '/workspace/design/_shared_rig_pipeline')
sys.path.insert(0, os.path.join(OUT,'work'))
import hum_analyze

WORK=os.path.join(OUT,'work'); STILLS=os.path.join(OUT,'stills')
os.makedirs(WORK,exist_ok=True); os.makedirs(STILLS,exist_ok=True)
LOG=[]
def log(m):
    print(m); LOG.append(str(m))

# ---------- helpers ----------
def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def get_mesh():
    ms=[o for o in bpy.data.objects if o.type=='MESH']
    assert ms, 'no mesh'
    return ms[0] if len(ms)==1 else None

def join_all_meshes():
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    if len(meshes)==1: return meshes[0]
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    bpy.ops.object.join(); return bpy.context.view_layer.objects.active

def apply_all(obj):
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def verts_np(me):
    n=len(me.data.vertices); co=np.empty(n*3); me.data.vertices.foreach_get('co',co); return co.reshape(-1,3)

def set_verts(me,co):
    me.data.vertices.foreach_set('co',co.ravel()); me.data.update()

# ---------- PREP ----------
def stage_prep():
    clear_scene()
    log(f'import {GLB}')
    bpy.ops.import_scene.gltf(filepath=GLB)
    # drop any imported armatures/empties that aren't mesh
    for o in list(bpy.data.objects):
        if o.type=='ARMATURE':
            bpy.data.objects.remove(o, do_unlink=True)
    me=join_all_meshes(); apply_all(me); me.name=f'{NAME}_body'
    # merge by distance + loose
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=1e-4)
    bpy.ops.mesh.delete_loose()
    bpy.ops.object.mode_set(mode='OBJECT')
    nfaces=len(me.data.polygons); log(f'faces after clean {nfaces}')
    if nfaces > TARGET*1.15:
        # decimate
        mod=me.modifiers.new('Dec','DECIMATE')
        mod.ratio=max(0.05, TARGET/max(nfaces,1))
        log(f'decimate ratio {mod.ratio:.4f}')
        bpy.ops.object.modifier_apply(modifier='Dec')
        log(f'faces after decimate {len(me.data.polygons)}')
    # ground shift
    co=verts_np(me); DZ=-float(co[:,2].min()); co[:,2]+=DZ; set_verts(me,co)
    open(os.path.join(WORK,'dz.txt'),'w').write(str(DZ))
    log(f'DZ {DZ} faces {len(me.data.polygons)} verts {len(me.data.vertices)}')
    # pack & rename images uniquely
    for i,im in enumerate(list(bpy.data.images)):
        if im.size[0]==0: continue
        kind=['basecolor','metal_rough','normal','emit','other'][min(i,4)]
        # guess from colorspace / links later; rename uniquely
        new=f'{NAME}_{kind}_{i}'
        im.name=new; im.filepath_raw=f'//textures/{new}.jpg'
        if im.packed_file is None and im.source=='FILE':
            try: im.pack()
            except: pass
        elif im.packed_file is None:
            try: im.pack()
            except: pass
        log(f'image {im.name} {tuple(im.size)} packed={im.packed_file is not None}')
    # fix material image names if nodes reference old
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(WORK,'dec.blend'), compress=False)
    # dump verts for analysis outside if needed
    np.save(os.path.join(WORK,'verts.npy'), verts_np(me))
    open(os.path.join(WORK,'prep.json'),'w').write(json.dumps(dict(faces=len(me.data.polygons),verts=len(me.data.vertices),DZ=DZ)))
    return me

# ---------- BUILD ARMATURE ----------
def make_bone(ad, name, head, tail, parent=None, connect=False, deform=True):
    eb=ad.edit_bones.new(name)
    eb.head=Vector(head); eb.tail=Vector(tail)
    if parent:
        eb.parent=ad.edit_bones[parent]; eb.use_connect=connect
    eb.use_deform=deform
    return eb

def build_mixamo_armature(J):
    ad=bpy.data.armatures.new(f'{NAME}_rig_data'); ad.display_type='OCTAHEDRAL'
    arm=bpy.data.objects.new(f'{NAME}_rig', ad)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm; arm.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    P='mixamorig:'
    hips=J['hips']; 
    # Hips: from hips center slightly down to spine0-ish — Mixamo hips is a short bone pointing up
    s0=np.array(J['spine0']); s1=np.array(J['spine1']); s2=np.array(J['spine2'])
    make_bone(ad, P+'Hips', hips, s0, None, False)
    make_bone(ad, P+'Spine', s0, s1, P+'Hips', True)
    make_bone(ad, P+'Spine1', s1, s2, P+'Spine', True)
    s2t=np.array(J.get('spine2_tail', J['neck']))
    neck=np.array(J['neck'])
    make_bone(ad, P+'Spine2', s2, s2t, P+'Spine1', True)
    make_bone(ad, P+'Neck', neck, J['neck_end'], P+'Spine2', False)
    make_bone(ad, P+'Head', J['head'], J['head_end'], P+'Neck', True)
    # Legs — Mixamo: UpLeg from hips to knee, Leg knee to ankle, Foot ankle to toe, ToeBase toe to tip
    for side,leg,tag in (('Left',J['L'],'Left'),('Right',J['R'],'Right')):
        make_bone(ad, P+tag+'UpLeg', leg['upleg'], leg['knee'], P+'Hips', False)
        make_bone(ad, P+tag+'Leg', leg['knee'], leg['ankle'], P+tag+'UpLeg', True)
        make_bone(ad, P+tag+'Foot', leg['ankle'], leg['toe'], P+tag+'Leg', True)
        make_bone(ad, P+tag+'ToeBase', leg['toe'], leg['toe_tip'], P+tag+'Foot', True)
    # Arms
    for tag,A in (('Left',J['LA']),('Right',J['RA'])):
        make_bone(ad, P+tag+'Shoulder', A['clav_in'], A['shoulder'], P+'Spine2', False)
        make_bone(ad, P+tag+'Arm', A['arm'], A['elbow'], P+tag+'Shoulder', False)
        make_bone(ad, P+tag+'ForeArm', A['elbow'], A['wrist'], P+tag+'Arm', True)
        # Hand: wrist to hand tip
        hand_tail=np.array(A['hand'])
        # ensure nonzero length
        if np.linalg.norm(hand_tail-np.array(A['wrist']))<1e-4:
            hand_tail=np.array(A['wrist'])+np.array([0.03 if tag=='Left' else -0.03,0,-0.02])
        make_bone(ad, P+tag+'Hand', A['wrist'], hand_tail, P+tag+'ForeArm', True)
    # roll: align roughly
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.object.mode_set(mode='EDIT')
    for eb in ad.edit_bones:
        # prefer Z-up roll for spine, X-out for limbs
        try:
            if 'Arm' in eb.name or 'ForeArm' in eb.name or 'Hand' in eb.name or 'Shoulder' in eb.name:
                eb.align_roll(Vector((0,0,1)))
            elif 'UpLeg' in eb.name or 'Leg' in eb.name or 'Foot' in eb.name or 'Toe' in eb.name:
                eb.align_roll(Vector((0,1,0)))
            else:
                eb.align_roll(Vector((0,1,0)))
        except: pass
    bpy.ops.object.mode_set(mode='OBJECT')
    log(f'bones {len(ad.bones)} deform {sum(b.use_deform for b in ad.bones)}')
    return arm

def clean_weights(me, arm):
    """Limit 4 influences, normalize, suppress cross-body bleed. Keep heat weights (A-pose safe)."""
    names=[g.name for g in me.vertex_groups]
    idx={n:i for i,n in enumerate(names)}
    n=len(me.data.vertices); G=len(names)
    W=np.zeros((n,G),np.float32)
    for v in me.data.vertices:
        for g in v.groups:
            if g.group < G: W[v.index,g.group]=g.weight
    co=verts_np(me)
    s=W.sum(1,keepdims=True); s[s<1e-12]=1; W/=s
    def sm(x):
        x=np.clip(x,0,1); return x*x*(3-2*x)
    x=co[:,0]
    left_bones=[i for n,i in idx.items() if 'Left' in n]
    right_bones=[i for n,i in idx.items() if 'Right' in n]
    if right_bones:
        fac=sm((-x+0.015)/0.05)
        for i in right_bones: W[:,i]*=fac
    if left_bones:
        fac=sm((x+0.015)/0.05)
        for i in left_bones: W[:,i]*=fac
    # continuous 4-influence limit
    for vi in range(n):
        w=W[vi]
        if (w>0).sum()<=4: continue
        thr=np.partition(w,-5)[-5]
        W[vi]=np.maximum(w-thr,0)
    s=W.sum(1,keepdims=True); miss=(s[:,0]<1e-8)
    if miss.any():
        hi=idx.get('mixamorig:Hips',0); W[miss,hi]=1; s=W.sum(1,keepdims=True)
    s[s<1e-12]=1; W/=s
    for g in list(me.vertex_groups): me.vertex_groups.remove(g)
    vgs=[me.vertex_groups.new(name=name) for name in names]
    order=np.argsort(-W, axis=1)[:,:4]
    for gi,vg in enumerate(vgs):
        sel=[]; wts=[]
        for vi in range(n):
            for k in range(4):
                j=int(order[vi,k]); w=float(W[vi,j])
                if j==gi and w>1e-6:
                    sel.append(vi); wts.append(w); break
        if not sel: continue
        buckets={}
        for vi,w in zip(sel,wts):
            key=round(w,4); buckets.setdefault(key,[]).append(vi)
        for w,idxs_ in buckets.items():
            for a in range(0,len(idxs_),8000):
                vg.add(idxs_[a:a+8000], float(w), 'REPLACE')
    log(f'weights cleaned maxinfl={(W>0).sum(1).max()} unweighted={int(((W>0).sum(1)==0).sum())} max|sum-1|={float(np.abs(W.sum(1)-1).max())}')
    return W, names

def stage_build():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(WORK,'dec.blend'))
    me=bpy.data.objects[f'{NAME}_body']
    co=verts_np(me)
    J=hum_analyze.analyze_humanoid(co)
    # save joints
    def ser(o):
        if isinstance(o,np.ndarray): return o.tolist()
        if isinstance(o,dict): return {k:ser(v) for k,v in o.items()}
        if isinstance(o,(np.floating,)): return float(o)
        if isinstance(o,(np.integer,)): return int(o)
        return o
    open(os.path.join(WORK,'joints.json'),'w').write(json.dumps(ser(J),indent=2))
    log(f"H={J['H']:.3f} hips_z={J['hips_z']:.3f} sh_z={J['sh_z']:.3f} face_fwd={J['face_forward']}")
    arm=build_mixamo_armature(J)
    # parent with auto weights
    bpy.ops.object.select_all(action='DESELECT')
    me.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
    t=time.time()
    # heat may fail; catch
    ok=True; msg=''
    try:
        r=bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        log(f'ARMATURE_AUTO {r} {time.time()-t:.1f}s')
    except Exception as e:
        ok=False; msg=str(e); log(f'ARMATURE_AUTO exception {e}')
    # detect failure: no vertex groups or many unweighted
    unw=sum(1 for v in me.data.vertices if not any(g.weight>1e-6 for g in v.groups))
    log(f'unweighted after heat {unw}/{len(me.data.vertices)} groups {len(me.vertex_groups)}')
    if unw > 0.05*len(me.data.vertices) or len(me.vertex_groups)==0:
        log('HEAT FAIL — trying merge+retry then proxy transfer')
        # merge more aggressively and retry
        bpy.ops.object.select_all(action='DESELECT'); me.select_set(True); bpy.context.view_layer.objects.active=me
        # clear parent
        bpy.ops.object.parent_clear(type='CLEAR_KEEP_TRANSFORM')
        for g in list(me.vertex_groups): me.vertex_groups.remove(g)
        for m in list(me.modifiers):
            if m.type=='ARMATURE': me.modifiers.remove(m)
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.mesh.remove_doubles(threshold=5e-4); bpy.ops.object.mode_set(mode='OBJECT')
        me.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
        try:
            bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        except Exception as e:
            log(f'retry heat fail {e}')
        unw=sum(1 for v in me.data.vertices if not any(g.weight>1e-6 for g in v.groups))
        log(f'unweighted after retry {unw}')
        if unw > 0.05*len(me.data.vertices) or len(me.vertex_groups)==0:
            # voxel proxy
            log('building voxel remesh proxy for weight transfer')
            proxy=me.copy(); proxy.data=me.data.copy(); proxy.name='proxy'
            bpy.context.scene.collection.objects.link(proxy)
            mod=proxy.modifiers.new('Vox','REMESH'); mod.mode='VOXEL'; mod.voxel_size=max(0.01, J['H']/80)
            bpy.context.view_layer.objects.active=proxy; proxy.select_set(True); me.select_set(False); arm.select_set(False)
            bpy.ops.object.modifier_apply(modifier='Vox')
            # skin proxy
            bpy.ops.object.select_all(action='DESELECT')
            proxy.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
            bpy.ops.object.parent_set(type='ARMATURE_AUTO')
            # transfer to me
            bpy.ops.object.select_all(action='DESELECT')
            # clear me armature mod/parent first
            for m in list(me.modifiers):
                if m.type=='ARMATURE': me.modifiers.remove(m)
            me.parent=None
            for g in list(me.vertex_groups): me.vertex_groups.remove(g)
            me.select_set(True); bpy.context.view_layer.objects.active=me
            dt=me.modifiers.new('DT','DATA_TRANSFER'); dt.object=proxy
            dt.use_vert_data=True; dt.data_types_verts={'VGROUP_WEIGHTS'}
            dt.vert_mapping='POLYINTERP_NEAREST'
            bpy.ops.object.datalayout_transfer(modifier='DT')
            bpy.ops.object.modifier_apply(modifier='DT')
            # parent me to arm without rebinding
            me.parent=arm; me.parent_type='OBJECT'
            am=me.modifiers.new('Armature','ARMATURE'); am.object=arm
            bpy.data.objects.remove(proxy, do_unlink=True)
            unw=sum(1 for v in me.data.vertices if not any(g.weight>1e-6 for g in v.groups))
            log(f'unweighted after proxy transfer {unw}')
            if unw > 0.2*len(me.data.vertices):
                open(os.path.join(OUT,'FAIL.md'),'w').write(f'# FAIL {NAME}\n\nHeat weighting and proxy transfer both failed. Unweighted verts: {unw}/{len(me.data.vertices)}\n')
                raise SystemExit(2)
    clean_weights(me, arm)
    # ensure armature modifier named
    for m in me.modifiers:
        if m.type=='ARMATURE': m.name='Armature'; m.object=arm
    # pack images again
    for im in bpy.data.images:
        if im.packed_file is None:
            try: im.pack()
            except: pass
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'), compress=False)
    log('saved blend')

# ---------- POSES / ANIM ----------
def pb(arm,name): return arm.pose.bones[name]
def reset(arm):
    for b in arm.pose.bones:
        b.location=(0,0,0); b.rotation_mode='QUATERNION'; b.rotation_quaternion=(1,0,0,0); b.scale=(1,1,1)
    bpy.context.view_layer.update()

def rotw(arm, name, axis, deg):
    """Rotate pose bone around world axis through its head."""
    b=pb(arm,name)
    M=b.matrix.copy(); h=M.translation.copy()
    b.matrix=Matrix.Translation(h)@Matrix.Rotation(math.radians(deg),4,Vector(axis).normalized())@Matrix.Translation(-h)@M
    bpy.context.view_layer.update()

def pose_rest(arm): reset(arm)

def pose_arms_up(arm):
    """Torso twist + mild shoulder (avoids A-pose armpit webbing on extreme abduction)."""
    reset(arm)
    P='mixamorig:'
    rotw(arm,P+'Spine',(0,0,1),12); rotw(arm,P+'Spine1',(0,0,1),10); rotw(arm,P+'Spine2',(0,0,1),8)
    rotw(arm,P+'Neck',(0,0,1),-6); rotw(arm,P+'Head',(0,0,1),-10)
    rotw(arm,P+'LeftShoulder',(0,1,0),-8); rotw(arm,P+'RightShoulder',(0,1,0),12)
    rotw(arm,P+'LeftArm',(1,0,0),15); rotw(arm,P+'RightArm',(1,0,0),-20)
    rotw(arm,P+'LeftForeArm',(0,0,1),-20); rotw(arm,P+'RightForeArm',(0,0,1),30)

def pose_walk(arm, phase=0.0):
    reset(arm)
    P='mixamorig:'
    rotw(arm,P+'LeftUpLeg',(1,0,0),28); rotw(arm,P+'LeftLeg',(1,0,0),-18); rotw(arm,P+'LeftFoot',(1,0,0),-8)
    rotw(arm,P+'RightUpLeg',(1,0,0),-22); rotw(arm,P+'RightLeg',(1,0,0),20); rotw(arm,P+'RightFoot',(1,0,0),12)
    rotw(arm,P+'LeftArm',(1,0,0),-30); rotw(arm,P+'LeftForeArm',(0,0,1),-35)
    rotw(arm,P+'RightArm',(1,0,0),25); rotw(arm,P+'RightForeArm',(0,0,1),40)
    rotw(arm,P+'Spine',(0,0,1),5); rotw(arm,P+'Hips',(0,1,0),4)

def pose_wave(arm):
    reset(arm)
    P='mixamorig:'
    rotw(arm,P+'RightShoulder',(0,1,0),20)
    rotw(arm,P+'RightArm',(0,1,0),55); rotw(arm,P+'RightForeArm',(0,0,1),35)
    rotw(arm,P+'LeftArm',(1,0,0),12)
    rotw(arm,P+'Spine1',(0,0,1),-8); rotw(arm,P+'Neck',(0,0,1),-10); rotw(arm,P+'Head',(0,0,1),-12)

POSES=[('A_rest',pose_rest),('B_twist',pose_arms_up),('C_walk',pose_walk),('D_wave',pose_wave)]

def make_walk_clip(arm, nf=30):
    if not arm.animation_data: arm.animation_data_create()
    act=bpy.data.actions.new(f'{NAME}_walk'); act.use_fake_user=True
    arm.animation_data.action=act
    P='mixamorig:'
    for f in range(nf+1):
        p=2*math.pi*f/nf
        reset(arm)
        a=24*math.sin(p); b=24*math.sin(p+math.pi)
        rotw(arm,P+'LeftUpLeg',(1,0,0),a)
        rotw(arm,P+'RightUpLeg',(1,0,0),b)
        rotw(arm,P+'LeftLeg',(1,0,0), (-abs(a)*0.75 if a>0 else 10))
        rotw(arm,P+'RightLeg',(1,0,0), (-abs(b)*0.75 if b>0 else 10))
        rotw(arm,P+'LeftArm',(1,0,0),-0.85*a); rotw(arm,P+'RightArm',(1,0,0),-0.85*b)
        rotw(arm,P+'LeftForeArm',(0,0,1),-25-12*math.sin(p))
        rotw(arm,P+'RightForeArm',(0,0,1),25+12*math.sin(p+math.pi))
        rotw(arm,P+'Spine',(0,0,1),4*math.sin(p))
        rotw(arm,P+'Hips',(0,1,0),3*math.sin(p))
        bpy.context.view_layer.update()
        for bone in arm.pose.bones:
            bone.rotation_mode='QUATERNION'
            # derive quat from matrix
            if bone.parent:
                local = bone.parent.matrix.inverted() @ bone.matrix
            else:
                local = bone.matrix
            bone.rotation_quaternion = local.to_quaternion()
            bone.keyframe_insert('rotation_quaternion',frame=f,group=bone.name)
            bone.keyframe_insert('location',frame=f,group=bone.name)
    for fc in act.fcurves:
        for k in fc.keyframe_points: k.interpolation='LINEAR'
        try: fc.modifiers.new('CYCLES')
        except: pass
    return act

# ---------- RENDER ----------
def setup_render(res=900):
    sc=bpy.context.scene
    engines={e.identifier for e in sc.render.bl_rna.properties['engine'].enum_items}
    if 'BLENDER_EEVEE_NEXT' in engines: sc.render.engine='BLENDER_EEVEE_NEXT'
    elif 'BLENDER_EEVEE' in engines: sc.render.engine='BLENDER_EEVEE'
    else: sc.render.engine='BLENDER_WORKBENCH'
    # fallback workbench if eevee fails later
    sc.render.resolution_x=sc.render.resolution_y=res
    sc.render.film_transparent=False
    sc.render.image_settings.file_format='PNG'
    w=bpy.data.worlds.new('W') if 'W' not in bpy.data.worlds else bpy.data.worlds['W']
    sc.world=w
    w.use_nodes=True
    bg=w.node_tree.nodes.get('Background')
    if bg: bg.inputs[0].default_value=(0.06,0.06,0.07,1); bg.inputs[1].default_value=0.4
    # lights
    for name,loc,energy in (('key',(2.2,-1.8,2.8),80),('fill',(-2.5,-1.0,1.8),30),('rim',(0,2.5,2.0),40)):
        if name in bpy.data.objects: continue
        l=bpy.data.lights.new(name, 'AREA'); l.energy=energy; l.size=2.0
        o=bpy.data.objects.new(name,l); bpy.context.scene.collection.objects.link(o); o.location=loc
        o.rotation_euler=(Vector((0,0,1.2))-Vector(loc)).to_track_quat('-Z','Y').to_euler()
    cam_data=bpy.data.cameras.new('cam'); cam=bpy.data.objects.new('cam',cam_data)
    bpy.context.scene.collection.objects.link(cam); sc.camera=cam; cam_data.lens=50
    return cam

def aim_cam(cam, target, direction, dist):
    tgt=Vector(target); d=Vector(direction).normalized()
    cam.location=tgt+d*dist
    cam.rotation_euler=(tgt-cam.location).to_track_quat('-Z','Y').to_euler()

def draw_bones_overlay(png_path, out_path, arm, cam, scene):
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        shutil.copy(png_path, out_path); return
    from bpy_extras.object_utils import world_to_camera_view
    im=Image.open(png_path).convert('RGBA'); draw=ImageDraw.Draw(im)
    W,H=im.size
    deps=bpy.context.evaluated_depsgraph_get()
    for b in arm.pose.bones:
        h=arm.matrix_world @ b.head; t=arm.matrix_world @ b.tail
        ch=world_to_camera_view(scene,cam,h); ct=world_to_camera_view(scene,cam,t)
        if not (0<=ch.z and 0<=ct.z): continue
        x1,y1=ch.x*W, (1-ch.y)*H; x2,y2=ct.x*W,(1-ct.y)*H
        if min(x1,x2)<-50 or max(x1,x2)>W+50: continue
        draw.line([(x1,y1),(x2,y2)], fill=(255,140,40,255), width=3)
        r=4; draw.ellipse([x1-r,y1-r,x1+r,y1+r], fill=(255,200,80,255))
    im.convert('RGB').save(out_path, quality=95)

def stage_pose_render():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'))
    arm=bpy.data.objects[f'{NAME}_rig']; me=bpy.data.objects[f'{NAME}_body']
    if arm.animation_data: arm.animation_data.action=None
    co=verts_np(me); z0,z1=float(co[:,2].min()),float(co[:,2].max()); H=z1-z0
    cy=float(co[:,1].mean()); cx=0.0; cz=z0+0.55*H
    cam=setup_render(900)
    sc=bpy.context.scene
    views=dict(
        front=((0,-1,0.08), 2.4*H),
        q34=((0.85,-1.0,0.2), 2.6*H),
        side=((1.0,0.05,0.08), 2.5*H),
    )
    # try eevee; on failure switch workbench
    def render_still(path):
        sc.render.filepath=path
        try:
            bpy.ops.render.render(write_still=True)
        except Exception as e:
            log(f'eevee fail {e}, workbench')
            sc.render.engine='BLENDER_WORKBENCH'
            sh=sc.display.shading; sh.light='STUDIO'; sh.color_type='TEXTURE'
            bpy.ops.render.render(write_still=True)
    for pname,fn in POSES:
        if arm.animation_data: arm.animation_data.action=None
        fn(arm)
        for vname,(dire,dist) in views.items():
            aim_cam(cam,(cx,cy,cz),dire,dist)
            path=os.path.join(STILLS,f'{pname}_{vname}.png')
            render_still(path)
            draw_bones_overlay(path, os.path.join(STILLS,f'{pname}_{vname}_bones.png'), arm, cam, sc)
            log(f'rendered {pname}_{vname}')
    # walk clip action
    act=make_walk_clip(arm,30)
    arm.animation_data.action=act
    sc.frame_start=0; sc.frame_end=30; sc.render.fps=30; sc.frame_set(0)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'), compress=False)
    # render a few walk frames
    aim_cam(cam,(cx,cy,cz),(0.85,-1.0,0.2),2.6*H)
    for f in (0,8,15,23):
        sc.frame_set(f); path=os.path.join(STILLS,f'walk_f{f:02d}_q34.png'); render_still(path)
    log('pose/render done')

# ---------- EXPORT ----------
def stage_export():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'))
    arm=bpy.data.objects[f'{NAME}_rig']; me=bpy.data.objects[f'{NAME}_body']
    # ensure unique image paths
    for i,im in enumerate(bpy.data.images):
        if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}:
            continue
        if not im.filepath_raw:
            im.filepath_raw=f'//textures/{im.name}.jpg'
        if im.packed_file is None:
            try: im.pack()
            except: pass
        log(f'export img {im.name} {im.filepath_raw} packed={im.packed_file is not None}')
    common=dict(use_selection=True, object_types={'ARMATURE','MESH'},
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False,
        path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
        mesh_smooth_type='FACE', use_mesh_modifiers=False)
    # rest pose FBX
    if arm.animation_data: arm.animation_data.action=None
    reset(arm)
    bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); me.select_set(True)
    bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'{NAME}_blenderig.fbx'), bake_anim=False, **common)
    # walk clip
    walk=None
    for a in bpy.data.actions:
        if 'walk' in a.name.lower(): walk=a; break
    if walk:
        arm.animation_data_create(); arm.animation_data.action=walk
        sc=bpy.context.scene; sc.frame_start=0; sc.frame_end=30; sc.render.fps=30
        bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'{NAME}_blenderig_walk.fbx'), bake_anim=True,
            bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
            bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **common)
        log('exported walk fbx')
    log('export done')

# ---------- CONTACT SHEET ----------
def stage_sheet():
    try:
        from PIL import Image, ImageDraw, ImageFont
        Image.MAX_IMAGE_PIXELS=None
    except ImportError:
        log('no PIL'); return
    def font(sz):
        for p in ['/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf']:
            if os.path.exists(p): return ImageFont.truetype(p,sz)
        return ImageFont.load_default()
    F=font(18); FT=font(26); FR=font(20)
    rows=[
        ('A  rest',['A_rest_front','A_rest_q34','A_rest_front_bones','A_rest_q34_bones']),
        ('B  torso twist',['B_twist_front','B_twist_q34','B_twist_front_bones','B_twist_q34_bones']),
        ('C  walk stride',['C_walk_front','C_walk_q34','C_walk_front_bones','C_walk_q34_bones']),
        ('D  wave / turn',['D_wave_front','D_wave_q34','D_wave_front_bones','D_wave_q34_bones']),
    ]
    T=400; HDR=50; RH=30
    # filter existing
    rows2=[]
    for lab,ims in rows:
        ok=[i for i in ims if os.path.exists(os.path.join(STILLS,i+'.png'))]
        if len(ok)>=2: rows2.append((lab,ok[:4]))
    if not rows2: log('no stills for sheet'); return
    cols=max(len(r[1]) for r in rows2)
    W=T*cols; Ht=HDR+len(rows2)*(RH+T)
    sheet=Image.new('RGB',(W,Ht),(28,28,31)); d=ImageDraw.Draw(sheet)
    d.text((12,12),f'{NAME} blender humanoid rig  |  Mixamo bones  |  EEVEE, orange=bones',font=FT,fill=(240,240,240))
    y=HDR
    for lab,ims in rows2:
        d.text((10,y+4),lab,font=FR,fill=(255,190,90)); y+=RH
        for i,n in enumerate(ims):
            im=Image.open(os.path.join(STILLS,n+'.png')).convert('RGB').resize((T,T),Image.LANCZOS)
            sheet.paste(im,(i*T,y))
            d.text((i*T+8,y+6),n.split('_',1)[-1],font=F,fill=(230,230,230))
        y+=T
    path=os.path.join(OUT,f'{NAME}_blenderig_sheet.jpg')
    sheet.save(path, quality=92); log(f'sheet {path} {sheet.size}')

# ---------- VERIFY ----------
def stage_verify():
    results={}
    for fname,tag in [(f'{NAME}_blenderig.fbx','rig'),(f'{NAME}_blenderig_walk.fbx','walk')]:
        path=os.path.join(OUT,fname)
        if not os.path.exists(path): continue
        clear_scene()
        bpy.ops.import_scene.fbx(filepath=path)
        arms=[o for o in bpy.data.objects if o.type=='ARMATURE']
        meshes=[o for o in bpy.data.objects if o.type=='MESH']
        info=dict(file=fname, bones=len(arms[0].data.bones) if arms else 0,
                  bone_names=[b.name for b in arms[0].data.bones] if arms else [],
                  meshes=[])
        for m in meshes:
            n=len(m.data.vertices)
            unw=sum(1 for v in m.data.vertices if not any(g.weight>1e-6 for g in v.groups))
            info['meshes'].append(dict(name=m.name, faces=len(m.data.polygons), verts=n,
                unweighted=unw, groups=len(m.vertex_groups),
                mods=[(md.type, getattr(md.object,'name',None)) for md in m.modifiers],
                parent=m.parent.name if m.parent else None))
        info['images']=[(im.name, tuple(im.size), im.packed_file is not None) for im in bpy.data.images if im.size[0]>0]
        info['actions']=[(a.name, tuple(a.frame_range), len(a.fcurves)) for a in bpy.data.actions]
        if tag=='walk' and arms and meshes and bpy.data.actions:
            m=meshes[0]; sc=bpy.context.scene
            def evco(f):
                sc.frame_set(int(f)); dg=bpy.context.evaluated_depsgraph_get(); e=m.evaluated_get(dg).to_mesh()
                c=np.empty(len(e.vertices)*3); e.vertices.foreach_get('co',c); m.evaluated_get(dg).to_mesh_clear(); return c.reshape(-1,3)
            fr=bpy.data.actions[0].frame_range
            c0=evco(fr[0]); c1=evco(0.5*(fr[0]+fr[1]))
            d=np.linalg.norm(c1-c0,axis=1)
            info['deform_mean']=float(d.mean()); info['deform_max']=float(d.max())
        results[tag]=info; log(f'verify {tag}: {json.dumps(info,default=str)[:500]}')
    open(os.path.join(WORK,'verify.json'),'w').write(json.dumps(results,indent=2,default=str))
    open(os.path.join(WORK,'pipeline.log'),'w').write('\n'.join(LOG))
    return results

# ---------- MAIN ----------
if __name__=='__main__':
    log(f'=== {NAME} stage={STAGE} ===')
    if STAGE in ('all','prep'): stage_prep()
    if STAGE in ('all','build'): stage_build()
    if STAGE in ('all','pose'): stage_pose_render(); stage_sheet()
    if STAGE in ('all','export'): stage_export()
    if STAGE in ('all','verify'): stage_verify()
    log('DONE')
