import bpy, sys, os, json, math
import numpy as np
from mathutils import Vector, Matrix
sys.path.insert(0,'/workspace/attack_20261007')
from atk_lib import setup_scene, aim, render
BASE='/workspace/design/survival-theme-a-fantasy'
cfg=json.load(open(sys.argv[sys.argv.index('--')+1])); MODE=sys.argv[sys.argv.index('--')+2]
NAME=cfg['name']; OUT=f"{BASE}/{cfg['anim_dir']}/clothsplit_20261007"; meta=json.load(open(f'{OUT}/work/build_meta.json'))
P='mixamorig:'; LEGB=[P+n for n in ('LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','RightToeBase')]
ORIG_REST=f"{BASE}/{cfg['rig_blend'].rsplit('/',1)[0]}/{NAME}_blenderig.fbx"
ORIG_WALK=f"{BASE}/{cfg['rig_blend'].rsplit('/',1)[0]}/{NAME}_blenderig_walk.fbx"
ORIG_ATK=f"{BASE}/{cfg['attack_blend'].rsplit('/',1)[0]}/{NAME}_blenderig_attack.fbx"
def fresh(): bpy.ops.wm.read_factory_settings(use_empty=True)
def imp(p):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=p)
    new=[o for o in bpy.data.objects if o not in before]
    return next(o for o in new if o.type=='ARMATURE'),[o for o in new if o.type=='MESH']
def bonepos(arm,names,frames):
    sc=bpy.context.scene; out={}
    for f in frames:
        sc.frame_set(f); out[f]={n:(arm.matrix_world@arm.pose.bones[n].matrix).copy() for n in names}
    return out
if MODE=='verify':
    fresh(); oa,_=imp(ORIG_REST); body22=[b.name for b in oa.data.bones]
    res={}
    for tag,fn in (('rest',f'{NAME}_clothsplit.fbx'),('walk',f'{NAME}_clothsplit_walk.fbx'),('attack',f'{NAME}_clothsplit_attack.fbx')):
        fresh(); arm,meshes=imp(f'{OUT}/{fn}'); names=[b.name for b in arm.data.bones]
        cloth=[n for n in names if n not in body22]
        bodym=next(m for m in meshes if m.name.endswith('_body')); clothm=next(m for m in meshes if m.name.endswith('_cloth'))
        def legw(m):
            gi={g.index:g.name for g in m.vertex_groups}; w=np.zeros(len(m.data.vertices)); 
            for v in m.data.vertices:
                w[v.index]=sum(g.weight for g in v.groups if gi[g.group] in LEGB)
            return w
        cw=legw(clothm); bw=legw(bodym); bco=np.array([bodym.matrix_world@v.co for v in bodym.data.vertices])
        r={'bones':len(names),'body22_present':all(n in names for n in body22),'body22_relative_order_same':[n for n in names if n in body22]==body22,
           'body22_first22_identical':names[:22]==body22,'cloth_bones':cloth,
           'cloth_parents':{n:arm.data.bones[n].parent.name for n in cloth},
           'cloth_mesh_leg_groups':[g.name for g in clothm.vertex_groups if g.name in LEGB],
           'cloth_verts_with_leg_weight':int((cw>1e-6).sum()),
           'cloth_groups':sorted(g.name for g in clothm.vertex_groups),
           'body_torso_verts_with_leg_weight':int(((bw>0.01)&(bco[:,2]>meta['crotch_z']+0.03)).sum()),
           'body_faces':len(bodym.data.polygons),'cloth_faces':len(clothm.data.polygons),
           'body_unweighted':int(sum(1 for v in bodym.data.vertices if not v.groups)),'cloth_unweighted':int(sum(1 for v in clothm.data.vertices if not v.groups))}
        act=arm.animation_data.action if arm.animation_data else None
        if act:
            fr=list(range(int(act.frame_range[0]),int(act.frame_range[1])+1)); r['frame_range']=[fr[0],fr[-1]]
            sc=bpy.context.scene; md=0.0; ma=0.0
            for f in fr:
                sc.frame_set(f)
                for n in cloth:
                    pb=arm.pose.bones[n]; b=pb.bone
                    rel=pb.parent.matrix.inverted()@pb.matrix; rest=b.parent.matrix_local.inverted()@b.matrix_local
                    md=max(md,(rel.translation-rest.translation).length); ma=max(ma,math.degrees(rel.to_quaternion().rotation_difference(rest.to_quaternion()).angle))
            r['cloth_bone_local_max_dev_m']=md; r['cloth_bone_local_max_dev_deg']=ma
            new=bonepos(arm,body22,fr)
            fresh(); oa2,_=imp(ORIG_WALK if tag=='walk' else ORIG_ATK); old=bonepos(oa2,body22,fr)
            r['body_motion_vs_original_max_m']=max((new[f][n].translation-old[f][n].translation).length for f in fr for n in body22)
            r['body_motion_vs_original_max_deg']=max(math.degrees(new[f][n].to_quaternion().rotation_difference(old[f][n].to_quaternion()).angle) for f in fr for n in body22)
        res[tag]=r; print('QC',tag,{k:v for k,v in r.items() if k not in ('cloth_bones','cloth_parents','cloth_groups')})
    json.dump(res,open(f'{OUT}/work/qc.json','w'),indent=1,default=float)
else:
    fd=f'/workspace/attack_20261007/clothsplit/frames_{NAME}'; os.makedirs(fd,exist_ok=True)
    fresh(); arm,meshes=imp(f'{OUT}/{NAME}_clothsplit_walk.fbx'); sc=bpy.context.scene; sc.render.fps=30
    bodym=next(m for m in meshes if m.name.endswith('_body')); clothm=next(m for m in meshes if m.name.endswith('_cloth'))
    cam=setup_scene('EEVEE',(600,800))
    try: sc.eevee.taa_render_samples=12
    except Exception: pass
    act=arm.animation_data.action; f0=int(act.frame_range[0])
    arm.animation_data.action=None
    for pb in arm.pose.bones: pb.matrix_basis.identity()
    bpy.context.view_layer.update()
    vs=[bodym.matrix_world@v.co for v in bodym.data.vertices]+[clothm.matrix_world@v.co for v in clothm.data.vertices]
    mn=Vector([min(v[i] for v in vs) for i in range(3)]); mx=Vector([max(v[i] for v in vs) for i in range(3)]); c=(mn+mx)/2; H=mx.z-mn.z
    arm.hide_render=True
    V={'rear':((c.x, c.y+1.75*H, mn.z+1.05*H),(c.x, c.y-0.1*H, mn.z+0.47*H)),
       'q34':((c.x-1.05*H, c.y-1.35*H, mn.z+0.75*H),(c.x, c.y, mn.z+0.49*H)),
       'front':((c.x, c.y-1.85*H, mn.z+0.55*H),(c.x, c.y, mn.z+0.49*H)),
       'side':((c.x+1.85*H, c.y, mn.z+0.55*H),(c.x, c.y, mn.z+0.49*H))}
    for hide,tag in ((True,'bodyonly'),(False,'withcloth')):
        clothm.hide_render=hide
        for v in ('front','q34','side','rear'):
            aim(cam,*V[v]); render(f'{fd}/rest_{tag}_{v}.png')
    clothm.hide_render=False; arm.animation_data.action=act
    sc.render.resolution_x,sc.render.resolution_y=360,480
    try: sc.eevee.taa_render_samples=8
    except Exception: pass
    for f in range(0,30,3):
        sc.frame_set(f0+f)
        for v in ('rear','q34'): aim(cam,*V[v]); render(f'{fd}/c_{v}_{f:02d}.png')
    for f in range(0,30):
        sc.frame_set(f0+f); aim(cam,*V['rear']); render(f'{fd}/m_{f:02d}.png')
    print('RENDER DONE',NAME)
