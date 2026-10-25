import bpy, math, sys, os
from mathutils import Vector, Matrix, Quaternion
sys.path.insert(0,'/workspace/attack_20261007')
import atk_lib as L
D='/workspace/ptp-ironhowl/Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl'
def load_body():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc=bpy.context.scene; sc.render.fps=30; sc.unit_settings.scale_length=0.01
    bpy.ops.import_scene.fbx(filepath=D+'/IRONHOWL_rig.fbx', automatic_bone_orientation=False, ignore_leaf_bones=False)
    arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; arm.name='Armature'
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    img=bpy.data.images.load(D+'/ironhowl_basecolor.png')
    for m in meshes:
        for slot in m.material_slots:
            mat=slot.material
            if not mat: continue
            mat.use_nodes=True; nt=mat.node_tree; p=nt.nodes.get('Principled BSDF')
            for n in list(nt.nodes):
                if n.type=='TEX_IMAGE': nt.nodes.remove(n)
            if p:
                t=nt.nodes.new('ShaderNodeTexImage'); t.image=img
                nt.links.new(t.outputs['Color'],p.inputs['Base Color'])
                p.inputs['Metallic'].default_value=0.2; p.inputs['Roughness'].default_value=0.6
    return arm, meshes
def load_clip_action(path):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, automatic_bone_orientation=False, ignore_leaf_bones=False)
    new=[o for o in bpy.data.objects if o not in before]
    a=[o for o in new if o.type=='ARMATURE'][0].animation_data.action
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
    return a
# --- Unity-like playback: evaluate an FBX's local node transforms (T*Pre*R*Post^-1) per frame
# and drive the body's pose bones with them, the way Unity drives the transforms of the
# prefab built from IRONHOWL_rig.fbx. Independent of Blender's FBX animation import.
import numpy as np
sys.path.insert(0,'/workspace/ironhowl/work')
from fbxeval import F
def _world(f, names, frame):
    W={}
    def w(n):
        if n in W: return W[n]
        L_=np.array(f.local(n,frame)); p=f.parent.get(n)
        W[n]=(w(p)@L_) if p in names else L_
        return W[n]
    for n in names: w(n)
    return W
class UnityPlayer:
    def __init__(self, arm, rigF):
        self.arm=arm; self.names=[b.name for b in arm.pose.bones]
        # body bind: rig.fbx's own take at time 0 == Blender's imported body action at frame 1
        bpy.context.scene.frame_set(1); bpy.context.view_layer.update()
        Wr=_world(rigF,self.names,0)
        self.C={n:np.linalg.inv(Wr[n])@np.array(arm.pose.bones[n].matrix) for n in self.names}
        self.order=[b.name for b in arm.pose.bones]  # parents before children
    def apply(self, f, frame):
        W=_world(f,self.names,frame)
        for n in self.order:
            pb=self.arm.pose.bones[n]; pb.matrix=Matrix((W[n]@self.C[n]).tolist())
            bpy.context.view_layer.update()
