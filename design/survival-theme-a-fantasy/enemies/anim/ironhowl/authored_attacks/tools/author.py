"""Author Ironhowl's new attack clips on his own Mixamo skeleton (IRONHOWL_rig.fbx, unchanged),
keyposed on top of the Mixamo combat stance (frame 1 of Standing Melee Attack Downward), legs held
by IK on foot targets, then baked out of Blender's FBX exporter.
usage: blender -b --python author.py -- OUTDIR [names...]"""
import sys; sys.path.insert(0,'/workspace/ironhowl/work')
from common import *
import importlib, specs; importlib.reload(specs)
args=sys.argv[sys.argv.index('--')+1:]; out=args[0]; only=args[1:]
arm,meshes=load_body()
sc=bpy.context.scene
# stance = frame 0 of the Mixamo downward clip, evaluated the way Unity plays it
_up=UnityPlayer(arm,F(D+'/IRONHOWL_rig.fbx'))
arm.animation_data_create(); arm.animation_data.action=None
_up.apply(F(D+'/IRONHOWL_mixamo_standing_melee_attack_downward.fbx'),0)
base={b.name:b.matrix_basis.copy() for b in arm.pose.bones}
footM={s:arm.pose.bones['mixamorig:%sFoot'%n].matrix.copy() for s,n in (('L','Left'),('R','Right'))}
arm.animation_data.action=None
P='mixamorig:'
def upd(): bpy.context.view_layer.update()
def reset():
    for b in arm.pose.bones:
        b.rotation_mode='QUATERNION'; b.matrix_basis=base[b.name]
    upd()
def rotw(name, axis, deg):
    b=arm.pose.bones[P+name]; M=b.matrix.copy(); h=M.translation.copy()
    b.matrix=Matrix.Translation(h)@Matrix.Rotation(math.radians(deg),4,Vector(axis).normalized())@Matrix.Translation(-h)@M; upd()
def aim(name, d):
    b=arm.pose.bones[P+name]; M=b.matrix.copy(); h=M.translation.copy()
    q=Vector(M.col[1][:3]).normalized().rotation_difference(Vector(d).normalized())
    b.matrix=Matrix.Translation(h)@q.to_matrix().to_4x4()@Matrix.Translation(-h)@M; upd()
def movew(name, d):
    b=arm.pose.bones[P+name]; M=b.matrix.copy(); M.translation=M.translation+Vector(d); b.matrix=M; upd()
def arm_ik(side,T,pole,hd):
    n='Left' if side=='L' else 'Right'
    A=arm.pose.bones[P+n+'Arm']; Fb=arm.pose.bones[P+n+'ForeArm']; H=arm.pose.bones[P+n+'Hand']
    S=A.head.copy(); a=(Fb.head-A.head).length; b=(H.head-Fb.head).length
    d=T-S; dl=min(d.length,(a+b)*0.999); u=d.normalized()
    v=pole-pole.dot(u)*u; v.normalize()
    ca=(a*a+dl*dl-b*b)/(2*a*dl); ca=max(-1,min(1,ca)); sa=math.sqrt(1-ca*ca)
    E=S+a*(ca*u+sa*v); aim(n+'Arm',E-S); aim(n+'ForeArm',(S+u*dl)-E)
    if hd: aim(n+'Hand',hd)
# foot IK targets (armature space == object local; empties parented to the armature object)
tg={}
for s,n in (('L','Left'),('R','Right')):
    e=bpy.data.objects.new('FT_'+s,None); sc.collection.objects.link(e); e.parent=arm
    e.matrix_parent_inverse=Matrix.Identity(4); e.matrix_basis=footM[s]; tg[s]=e
    leg=arm.pose.bones[P+n+'Leg']; c=leg.constraints.new('IK'); c.target=e; c.chain_count=2; c.use_tail=True
    c.use_stretch=False
    ft=arm.pose.bones[P+n+'Foot']; c2=ft.constraints.new('COPY_TRANSFORMS'); c2.target=e; c2.owner_space='WORLD'; c2.target_space='WORLD'
# leg IK hits the Foot head with the shin tail: shin tail == foot head
def pose(k):
    reset()
    if 'hips' in k: movew('Hips', k['hips'])
    for op in k.get('rot',[]): rotw(*op)
    for op in k.get('aim',[]): aim(*op)
    for it in k.get('ik',[]):
        side,T,pole,hd=it[:4]; T=Vector(T)
        if len(it)>4: T=T+arm.pose.bones[P+it[4]].head  # target relative to a bone head
        arm_ik(side,T,Vector(pole),hd)
    if k.get('print'):
        for n in ('Hips','LeftArm','RightArm','LeftHand','RightHand','Head'): print('POS',k['f'],n,tuple(round(v,1) for v in arm.pose.bones[P+n].head))
def key_feet(f, k, interp):
    for s in 'LR':
        e=tg[s]; d=k.get('feet',{}).get(s,(0,0,0)); r=k.get('footrot',{}).get(s)
        M=footM[s].copy()
        if r:
            h=M.translation.copy(); M=Matrix.Translation(h)@Matrix.Rotation(math.radians(r[1]),4,Vector(r[0]).normalized())@Matrix.Translation(-h)@M
        M.translation=M.translation+Vector(d); e.matrix_basis=M
        e.rotation_mode='QUATERNION'; e.rotation_quaternion=M.to_quaternion()
        e.keyframe_insert('location',frame=f); e.keyframe_insert('rotation_quaternion',frame=f)
def set_interp(fcurves, keys):
    for fc in fcurves:
        for kp in fc.keyframe_points:
            fr=int(round(kp.co[0])); spec=[k for k in keys if k['f']==fr]
            it=spec[0].get('interp','BEZIER') if spec else 'BEZIER'
            if it=='BEZIER': kp.interpolation='BEZIER'; kp.handle_left_type='AUTO_CLAMPED'; kp.handle_right_type='AUTO_CLAMPED'
            else:
                kp.interpolation=it.split(':')[0]
                if ':' in it: kp.easing=it.split(':')[1]
def build(spec):
    keys=spec['keys']
    for o in [arm]+list(tg.values()):
        o.animation_data_create(); 
    act=bpy.data.actions.new(spec['name']); act.use_fake_user=True; arm.animation_data.action=act
    for e in tg.values():
        ea=bpy.data.actions.new(spec['name']+'_'+e.name); e.animation_data.action=ea
    prev=None
    for k in keys:
        pose(k); f=k['f']
        for b in arm.pose.bones:
            q=b.rotation_quaternion.copy()
            if prev is not None and prev[b.name].dot(q)<0: q=-q; b.rotation_quaternion=q
            b.keyframe_insert('rotation_quaternion',frame=f,group=b.name); b.keyframe_insert('location',frame=f,group=b.name)
        prev={b.name:b.rotation_quaternion.copy() for b in arm.pose.bones}
        key_feet(f,k,None)
    set_interp(act.fcurves, keys)
    for e in tg.values(): set_interp(e.animation_data.action.fcurves, keys)
    return act
for spec in specs.SPECS:
    if only and spec['name'] not in only: continue
    act=build(spec)
    sc.frame_start=0; sc.frame_end=spec['keys'][-1]['f']
    path=f"{out}/{spec['name']}_blender.fbx"
    for o in bpy.data.objects: o.select_set(o==arm)
    bpy.context.view_layer.objects.active=arm
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'ARMATURE'}, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, apply_unit_scale=True, global_scale=1.0, primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL', use_armature_deform_only=False)
    print("EXPORTED",path, sc.frame_end)
