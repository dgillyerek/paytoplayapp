"""Sample the attack at 60 Hz (+0.6 s held on the last frame) for the ClothSpring settle test: chain targets (animated rest
joints of every cape chain, from the cape rig following its body anchors) and body bone head/tail for the colliders."""
import bpy, json
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape_rig']
crig=d.objects[0]; sc.collection.objects.link(crig)
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
chains=json.load(open(W+'/cape_chains.json'))
arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']
BONES=[b.name for b in arm.data.bones]
samples=[]
for i in range(61+36):
    fr=min(i*0.5,30.0); sc.frame_set(int(fr),subframe=fr-int(fr))
    tg={}
    for ch in chains:
        pts=[list(crig.matrix_world@crig.pose.bones[b].head) for b in ch['bones']]+[list(crig.matrix_world@crig.pose.bones[ch['bones'][-1]].tail)]
        tg[ch['name']]=[round(v,6) for p in pts for v in p]
    bo={b:[round(v,6) for v in list(arm.matrix_world@arm.pose.bones[b].head)+list(arm.matrix_world@arm.pose.bones[b].tail)] for b in BONES}
    samples.append({'t':round(i/60,5),'frame':fr,'targets':tg,'bones':bo})
json.dump({'release_frame':17,'fps':30,'samples':samples},open(W+'/cape_attack_sim.json','w'))
print('SIM',len(samples))
