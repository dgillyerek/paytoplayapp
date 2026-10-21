import bpy, math, mathutils, json, sys
from bpy_extras.object_utils import world_to_camera_view
ROOT="/workspace/paytoplayapp/Assets/ThemePack/fantasy_kingdom_a/art/"
H="heroes/3d/"; E="enemies/3d/"
# roster name -> (walk fbx, attack fbx, design height, fallback albedo, battle scale)
U={
 "Lyra":(H+"lyra/LYRA_blenderig_walk.fbx",H+"lyra/LYRA_blenderig_attack.fbx",1.885,None,1),
 "Rowan":(H+"rowan/ROWAN_blenderig_walk.fbx",H+"rowan/ROWAN_blenderig_attack.fbx",1.8,None,1),
 "Oakenshield":(H+"oakenshield/OAKENSHIELD_blenderig_walk.fbx",H+"oakenshield/OAKENSHIELD_blenderig_attack.fbx",1.8,None,1),
 "Sir Aldric":(H+"pilot/SirAldric_body_holefixed_walk.fbx",H+"pilot/SirAldric_body_holefixed_slash_SWORD_SHIELD_ATTACK.fbx",1.8,H+"pilot/mixamo_tex/Meshy_AI_Lionheart_Sentinel_0929004215_texture.png",1),
 "Bonequill":(E+"bonequill/BONEQUILL_blenderig_walk.fbx",E+"bonequill/BONEQUILL_blenderig_attack.fbx",1.8,None,1),
 "Nightfang":(E+"nightfang/NIGHTFANG_blenderig_trot.fbx",E+"nightfang/NIGHTFANG_blenderig_attack.fbx",1.09,None,1),
 "Blightroot":(E+"blightroot/creature_pack/Blightroot_mutant_walking.fbx",E+"blightroot/creature_pack/Blightroot_mutant_swiping.fbx",1.9,E+"blightroot/Blightroot_basecolor.jpg",1),
 "Ironhowl":(E+"ironhowl/IRONHOWL_rig_walk.fbx",E+"ironhowl/IRONHOWL_rig_attack.fbx",1.9,E+"ironhowl/ironhowl_basecolor.png",1),
}
BODY={"Blightroot":E+"blightroot/Blightroot_Dual_Weapon_Combo.fbx"}
spec=json.load(open(sys.argv[sys.argv.index('--')+1]))
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene
def B(x,y,z): return mathutils.Vector((x,z,y))  # unity (x,y,z) -> blender (x,z_fwd,y_up)
def img_mat(name,path,alpha=False,emit=0.0,tile=(1,1)):
    m=bpy.data.materials.new(name); m.use_nodes=True; nt=m.node_tree
    bs=nt.nodes["Principled BSDF"]; tx=nt.nodes.new("ShaderNodeTexImage"); tx.image=bpy.data.images.load(path)
    if tile!=(1,1):
        mp=nt.nodes.new("ShaderNodeMapping"); tc=nt.nodes.new("ShaderNodeTexCoord"); mp.inputs[3].default_value=(tile[0],tile[1],1)
        nt.links.new(tc.outputs[2],mp.inputs[0]); nt.links.new(mp.outputs[0],tx.inputs[0])
    nt.links.new(tx.outputs[0],bs.inputs["Base Color"]); bs.inputs["Roughness"].default_value=0.9
    if alpha:
        nt.links.new(tx.outputs[1],bs.inputs["Alpha"]); m.blend_method='HASHED'
    if emit>0:
        nt.links.new(tx.outputs[0],bs.inputs["Emission Color"]); bs.inputs["Emission Strength"].default_value=emit
    return m
def plane(name,w,l,loc,mat,rot=(0,0,0)):
    bpy.ops.mesh.primitive_plane_add(size=1,location=loc,rotation=rot); o=bpy.context.object; o.name=name; o.scale=(w,l,1); o.data.materials.append(mat); return o
# ground
G=ROOT+"map/"
plane("grass",70,90,(0,spec["focus_z"]+30,0),img_mat("g",G+"THEME_A_BATTLE_GRASS_TILE_512.png",tile=(70/3,90/3)))
plane("road",9.4,90,(0,spec["focus_z"]+30,0.004),img_mat("d",G+"THEME_A_BATTLE_DIRT_TRACK_256x512.png",alpha=True,tile=(1,90/8)))
# camera (same numbers as RunnerLevelView)
f=spec["focus_x"]; fz=spec["focus_z"]; boss=spec.get("boss",False)
pos=B(f-1.2,7.6+(0.8 if boss else 0),fz-10.0-(1.4 if boss else 0)); look=B(f,0.9+(0.6 if boss else 0),fz+5.0)
cam=bpy.data.cameras.new("c"); cam.sensor_fit='VERTICAL'; cam.angle_y=math.radians(52); cam.clip_end=200
co=bpy.data.objects.new("c",cam); sc.collection.objects.link(co); sc.camera=co; co.location=pos
co.rotation_euler=(look-pos).to_track_quat('-Z','Y').to_euler()
def billboard(name,path,x,z,width,y0=0.0):
    im=bpy.data.images.load(path); a=im.size[1]/im.size[0]
    m=img_mat(name,path,alpha=True,emit=0.35)
    yaw=math.atan2(pos.x-x, pos.y-z)  # face the camera on the ground plane
    bpy.ops.mesh.primitive_plane_add(size=1,location=(x,z,y0+width*a/2),rotation=(math.pi/2,0,-yaw+math.pi)); o=bpy.context.object
    o.scale=(width,width*a,1); o.data.materials.append(m)
    if name=="decor": o.visible_shadow=False
    return o
for d in spec.get("decor",[]): billboard("decor",ROOT+d[0],d[1],d[2],d[3])
anchors={}
def widest(meshes,x):
    dg=bpy.context.evaluated_depsgraph_get(); w=0
    for o in meshes:
        ev=o.evaluated_get(dg); me=ev.to_mesh(); mw=ev.matrix_world
        for i in range(0,len(me.vertices),11): w=max(w,abs((mw@me.vertices[i].co).x-x))
        ev.to_mesh_clear()
    return min(w,2.5)
def unit(u):
    name,x,z=u["roster"],u["x"],u["z"]; walk,atk,h,alb,_=U[name]
    path=atk if u.get("pose")=="attack" else walk
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=ROOT+(BODY[name] if name in BODY else path))
    new=[o for o in bpy.data.objects if o not in before]
    if name in BODY:
        # animation-only clip (Mixamo, same bones): put its action on the body's armature
        b2=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=ROOT+path)
        anim=[o for o in bpy.data.objects if o not in b2]
        act=next((o.animation_data.action for o in anim if o.type=='ARMATURE' and o.animation_data and o.animation_data.action),None)
        for o in new:
            if o.type=='ARMATURE' and act:
                o.animation_data_create(); o.animation_data.action=act
        for o in anim: bpy.data.objects.remove(o,do_unlink=True)
    arm=[o for o in new if o.type=='ARMATURE']; meshes=[o for o in new if o.type=='MESH']
    e=bpy.data.objects.new(name,None); sc.collection.objects.link(e)
    for o in new:
        if o.parent is None: o.parent=e
    frame=u.get("frame",10)
    for a in arm:
        if a.animation_data and a.animation_data.action:
            a.animation_data.action.use_fake_user=True
    # make sure every mesh shows its painted albedo
    for o in meshes:
        if alb and not o.material_slots:
            o.data.materials.append(bpy.data.materials.new(name+"_alb"))
            o.data.materials[0].use_nodes=True
        for slot in o.material_slots:
            m=slot.material
            if m is None: continue
            ok=m.use_nodes and any(n.type=='TEX_IMAGE' and n.image and n.image.has_data for n in m.node_tree.nodes)
            if not ok and alb:
                m.use_nodes=True; nt=m.node_tree; bs=[n for n in nt.nodes if n.type=='BSDF_PRINCIPLED']
                if bs:
                    tx=nt.nodes.new("ShaderNodeTexImage"); tx.image=bpy.data.images.load(ROOT+alb); nt.links.new(tx.outputs[0],bs[0].inputs["Base Color"])
            for n in m.node_tree.nodes if m.use_nodes else []:
                if n.type=='BSDF_PRINCIPLED':
                    n.inputs["Metallic"].default_value=min(n.inputs["Metallic"].default_value,0.3)
                    if u.get("tint"): n.inputs["Base Color"].default_value=u["tint"]+[1]
    sc.frame_set(frame); bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get()
    def bounds():
        pts=[]
        for o in meshes:
            ev=o.evaluated_get(dg); me=ev.to_mesh()
            mw=ev.matrix_world; pts+= [mw@me.vertices[i].co for i in range(0,len(me.vertices),7)]; ev.to_mesh_clear()
        return pts
    pts=bounds(); print("UNIT",name,path,len(meshes),len(pts))
    mn=min(p.z for p in pts); mx=max(p.z for p in pts)
    s=h*u.get("scale",1)/(mx-mn); e.scale=(s,s,s)
    yaw=(math.pi if u["hero"] else 0)+math.radians(u.get("yaw",0)); e.rotation_euler=(0,0,yaw)
    bpy.context.view_layer.update(); dg=bpy.context.evaluated_depsgraph_get()
    pts=bounds(); cx=sum(p.x for p in pts)/len(pts); cy=sum(p.y for p in pts)/len(pts); mn=min(p.z for p in pts)
    e.location=(x-cx, z-cy, u.get("y",0)-mn)
    for a in arm: a.hide_render=True; a.hide_viewport=True
    # contact shadow handled by the sun; record anchors
    H_=h*u.get("scale",1)
    anchors[u.get("id",name)]={"foot":[x,u.get("y",0),z],"height":H_}
    if u.get("bar"):
        # RunnerSideBar.WorldOffset: the hero's own left / a foe's own right, behind the body
        sc_=u.get("scale",1); hw=max(widest(meshes,x),0.32*H_); side=hw+0.18*sc_; back=0.3*sc_
        bx=x-side; bz=z-back if u["hero"] else z+back
        anchors[u.get("id",name)]["bar"]=[bx,bz,H_*0.12,H_*0.88]
for u in spec["units"]: unit(u)
for b in spec.get("boxes",[]):
    billboard("box",ROOT+b["art"],b["x"],b["z"],1.7)
    anchors[b["id"]]={"foot":[b["x"],0,b["z"]],"height":1.7*0.95}
# light
sun=bpy.data.lights.new("s",'SUN'); sun.energy=3.2; sun.angle=math.radians(8); so=bpy.data.objects.new("s",sun); sc.collection.objects.link(so); so.rotation_euler=(math.radians(32),0,math.radians(-30))
w=bpy.data.worlds.new("w"); sc.world=w; w.use_nodes=True; bg=w.node_tree.nodes["Background"]; bg.inputs[0].default_value=(0.62,0.72,0.82,1); bg.inputs[1].default_value=0.9
sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=48; sc.cycles.use_denoising=True; sc.cycles.max_bounces=4
sc.view_settings.view_transform='Standard'
sc.render.resolution_x=540; sc.render.resolution_y=960; sc.render.film_transparent=False
sc.render.filepath=spec["out"]
bpy.ops.render.render(write_still=True)
# export screen projections (y down, pixels) for the HUD pass
def px(v):
    c=world_to_camera_view(sc,co,v); return [c.x*540,(1-c.y)*960,c.z]
out={}
for k,a in anchors.items():
    fx,fy,fz_=a["foot"]; out[k]={"foot":px(B(fx,fy,fz_)),"head":px(B(fx,fy+a["height"],fz_)),"height":a["height"],"x":fx,"z":fz_,"y":fy}
    if "bar" in a:
        bx,bz,lo,hi=a["bar"]; out[k]["bar"]=[px(B(bx,lo,bz)),px(B(bx,hi,bz))]
out["__cam"]={"pos":list(pos),"look":list(look)}
json.dump(out,open(spec["out"].replace(".png",".json"),"w"))
