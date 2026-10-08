import bpy, sys, os, math, json
from mathutils import Vector, Matrix
sys.path.insert(0,'/workspace/attack_20261007')
from atk_lib import setup_scene, aim, render
BASE='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan'
NEW=f'{BASE}/walkfix_male_20261007/ROWAN_male_blenderig_walk.fbx'
OLD=f'{BASE}/blender_rig_male_20261007/ROWAN_male_blenderig_walk.fbx'
REST=f'{BASE}/blender_rig_male_20261007/ROWAN_male_blenderig.fbx'
MODE=sys.argv[sys.argv.index('--')+1]
def fresh():
    bpy.ops.wm.read_factory_settings(use_empty=True)
def imp(p):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=p)
    new=[o for o in bpy.data.objects if o not in before]
    arm=[o for o in new if o.type=='ARMATURE'][0]; mesh=[o for o in new if o.type=='MESH']
    return arm,mesh
def bbox(objs):
    dg=bpy.context.evaluated_depsgraph_get(); pts=[]
    for o in objs:
        ev=o.evaluated_get(dg); m=ev.to_mesh()
        mw=ev.matrix_world; vs=m.vertices; pts+=[mw@vs[i].co for i in range(0,len(vs),25)]; ev.to_mesh_clear()
    return [min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]
def bw(arm,n): return arm.matrix_world@arm.pose.bones[n].matrix
def info(path, rest=None):
    fresh(); arm,mesh=imp(path); sc=bpy.context.scene
    act=arm.animation_data.action if arm.animation_data else None
    r={'action':act.name if act else None,'frame_range':list(act.frame_range) if act else None,
       'bones':[b.name for b in arm.data.bones],'faces':sum(len(m.data.polygons) for m in mesh),
       'vgroups':sorted({g.name for m in mesh for g in m.vertex_groups})}
    if not act: 
        r['rest_hips']=[list(r_) for r_ in bw(arm,'mixamorig:Hips')]; r['rest_bbox']=bbox(mesh)
        r['rest_heads']={b.name:list(arm.matrix_world@b.head_local) for b in arm.data.bones}
        return r
    P='mixamorig:'; fr=range(int(act.frame_range[0]),int(act.frame_range[1])+1)
    rows=[]; poses={}
    for f in fr:
        sc.frame_set(f)
        mn,mx=bbox(mesh) if (f-fr[0]) in (0,8,15,23,30) else (None,None)
        H=bw(arm,P+'Hips'); lf=bw(arm,P+'LeftFoot').translation; rf=bw(arm,P+'RightFoot').translation
        rows.append({'f':f,'Lfoot_y':lf.y,'Rfoot_y':rf.y,'Lfoot_z':lf.z,'Rfoot_z':rf.z,'hips_z':H.translation.z,'bbox':[mn,mx] if mn else None})
        poses[f]={b.name:b.matrix.copy() for b in arm.pose.bones}
        if f==fr[0]: r['hips_f0']=[list(x) for x in H]
    r['rows']=rows
    r['loop_maxdelta']=max((poses[fr[0]][n].translation-poses[fr[-1]][n].translation).length for n in poses[fr[0]])
    r['loop_maxangle_deg']=max(math.degrees(poses[fr[0]][n].to_quaternion().rotation_difference(poses[fr[-1]][n].to_quaternion()).angle) for n in poses[fr[0]])
    return r
if MODE=='verify':
    rest=info(REST); new=info(NEW); old=info(OLD)
    def hipsdiff(a,b):
        A=Matrix(a); B=Matrix(b)
        return (A.translation-B.translation).length, math.degrees(A.to_quaternion().rotation_difference(B.to_quaternion()).angle)
    out={'rest_bbox':rest['rest_bbox']}
    for k,d in (('new',new),('old',old)):
        dl,da=hipsdiff(d['hips_f0'],rest['rest_hips'])
        ly=[r_['Lfoot_y'] for r_ in d['rows']]; ry=[r_['Rfoot_y'] for r_ in d['rows']]
        n=len(ly); ml=sum(ly)/n; mr=sum(ry)/n
        cov=sum((a-ml)*(b-mr) for a,b in zip(ly,ry)); corr=cov/math.sqrt(sum((a-ml)**2 for a in ly)*sum((b-mr)**2 for b in ry)+1e-12)
        lead=['L' if a<b-0.02 else ('R' if b<a-0.02 else '=') for a,b in zip(ly,ry)]  # -Y = forward
        out[k]={'action':d['action'],'frame_range':d['frame_range'],'bones_same_as_rest':d['bones']==rest['bones'],
           'faces':d['faces'],'rest_faces':rest['faces'],'vgroups_same':d['vgroups']==rest['vgroups'],
           'hips_f0_vs_rest_m':dl,'hips_f0_vs_rest_deg':da,
           'bbox_samples':{r_['f']:r_['bbox'] for r_ in d['rows'] if r_['bbox']},
           'foot_y_corr_LR':corr,'lead_seq':''.join(lead),
           'Lfoot_y_range':[min(ly),max(ly)],'Rfoot_y_range':[min(ry),max(ry)],
           'min_foot_z':min(min(r_['Lfoot_z'],r_['Rfoot_z']) for r_ in d['rows']),
           'hips_z_range':[min(r_['hips_z'] for r_ in d['rows']),max(r_['hips_z'] for r_ in d['rows'])],
           'loop_f0_vs_f30_max_m':d['loop_maxdelta'],'loop_f0_vs_f30_max_deg':d['loop_maxangle_deg'],
           'Lfoot_y':ly,'Rfoot_y':ry}
    json.dump(out,open('/workspace/attack_20261007/walkfix/verify.json','w'),indent=1,default=float)
    for k in ('new','old'):
        o=out[k]; print(k.upper(), {x:o[x] for x in o if x not in ('Lfoot_y','Rfoot_y')})
    print('REST_BBOX',rest['rest_bbox'])
else:
    fresh(); arm,mesh=imp(NEW); sc=bpy.context.scene; sc.render.fps=30
    cam=setup_scene('EEVEE',(480,640))
    try: sc.eevee.taa_render_samples=8
    except Exception: pass
    sc.frame_set(0); mn,mx=bbox(mesh); c=Vector([(a+b)/2 for a,b in zip(mn,mx)]); H=mx[2]-mn[2]
    # ground
    bpy.ops.mesh.primitive_plane_add(size=6,location=(0,0,mn[2])); g=bpy.context.object
    gm=bpy.data.materials.new('g'); gm.use_nodes=True; gm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.025,0.027,0.032,1); gm.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=0.9; g.data.materials.append(gm)
    arm.hide_render=True
    V={'rear':((c.x, c.y+2.3*H, mn[2]+1.35*H),(c.x, c.y-0.2*H, mn[2]+0.45*H)),
       'q34':((c.x-1.55*H, c.y-1.95*H, mn[2]+0.85*H),(c.x, c.y, mn[2]+0.48*H))}
    od='/workspace/attack_20261007/walkfix/frames'; os.makedirs(od,exist_ok=True)
    for f in range(0,30,3):
        sc.frame_set(f)
        for v in V: aim(cam,*V[v],lens=50); render(f'{od}/c_{v}_{f:02d}.png')
    sc.render.resolution_x,sc.render.resolution_y=480,640
    for f in range(0,30):
        sc.frame_set(f); aim(cam,*V['rear'],lens=50); render(f'{od}/m_{f:02d}.png')
    print('RENDER DONE')
