"""Archer attacks 2/3 (Rowan, Bonequill): Mixamo Pro Longbow clips (auto-rig keeps the same mixamorig names) retargeted onto the
approved Blender rig by world-space rotation delta from rest. Body bones only (left finger grip kept from rest). Segments are
concatenated (crossfades at seams), hips' horizontal drift removed (net travel 0), optional spine lift (Spine + Spine1 only, never the
bow arm) to angle the aim up, blend in from rest with the left foot stepping forward first, blend out to rest. The bow rides LeftHand at
the locked BlenderBoneGrip and only rolls in the grip (8 deg rest -> draw roll per shot). QC: bow vs body, string vs forearm.
Usage: blender -b <design.blend> --python archer_retarget.py -- params.json"""
import bpy, sys, json, math
from mathutils import Matrix, Vector, Quaternion
from mathutils.bvhtree import BVHTree
PRM=json.load(open(sys.argv[sys.argv.index('--')+1]))
P='mixamorig:'; sc=bpy.context.scene
arm=bpy.data.objects[PRM['rig']]; body=bpy.data.objects[PRM['body']]
ad=arm.animation_data
if ad is None: ad=arm.animation_data_create()
for t in ad.nla_tracks: t.mute=True
names=[b.name for b in arm.data.bones]; B=arm.data.bones
BODY=[n for n in names if n.replace(P,'') in ('Hips','Spine','Spine1','Spine2','Neck','Head','LeftShoulder','LeftArm','LeftForeArm','LeftHand',
      'RightShoulder','RightArm','RightForeArm','RightHand','LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot')]
AW=arm.matrix_world.copy(); AWi=AW.inverted(); AWq=AW.to_quaternion()
def sm(t): t=min(1.0,max(0.0,t)); return t*t*(3-2*t)
ad.action=bpy.data.actions[PRM['rest_action']]; sc.frame_set(PRM.get('rest_frame',0))
REST_BASIS={n:arm.pose.bones[n].matrix_basis.copy() for n in names}
def fk(basis):
    M={}
    for n in names:
        b=B[n]; Rb=b.matrix_local
        M[n]=(M[b.parent.name]@(b.parent.matrix_local.inverted()@Rb)@basis[n]) if b.parent else Rb@basis[n]
    return M
def basis_from(Q,hips_head):
    M={}; out={}
    for n in names:
        b=B[n]; Rb=b.matrix_local
        if b.parent: C=M[b.parent.name]@(b.parent.matrix_local.inverted()@Rb); head=C.translation
        else: C=Rb; head=hips_head
        Mn=Matrix.LocRotScale(head,Q[n],None); M[n]=Mn
        bs=C.inverted()@Mn
        if b.parent: bs=Matrix.LocRotScale(Vector((0,0,0)),bs.to_quaternion(),None)
        out[n]=bs
    return out,M
MR=fk(REST_BASIS); QR={n:MR[n].to_quaternion() for n in names}; HR=MR[P+'Hips'].translation.copy()
TRw={n:(AWq@B[n].matrix_local.to_quaternion()) for n in names}; TRh=AW@B[P+'Hips'].head_local
# ---- sources
cache={}
def load(src):
    if src in cache: return cache[src]
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=src); new=[o for o in bpy.data.objects if o not in before]
    s=[o for o in new if o.type=='ARMATURE'][0]; act=s.animation_data.action; f0,f1=[int(round(x)) for x in act.frame_range]
    SW=s.matrix_world.copy(); SWq=SW.to_quaternion(); SR={n:(SWq@s.data.bones[n].matrix_local.to_quaternion()) for n in BODY}
    # Mixamo re-binds the auto-rig in its T-pose; the Blender rig binds in an A-pose. Reference = target bind turned (minimal
    # rotation, roll kept) so each bone points where the source bind bone points.
    REF={}
    for n in BODY:
        bt=B[n]; bs=s.data.bones[n]
        vt=(AW.to_3x3()@(bt.tail_local-bt.head_local)).normalized(); vs=(SW.to_3x3()@(bs.tail_local-bs.head_local)).normalized()
        REF[n]=vt.rotation_difference(vs)@TRw[n]
    SRh=SW@s.data.bones[P+'Hips'].head_local; k=TRh.z/SRh.z
    fr=[]
    for f in range(f0,f1+1):
        sc.frame_set(f); Q={}
        for n in BODY: Q[n]=AWq.inverted()@(SWq@s.pose.bones[n].matrix.to_quaternion()@SR[n].inverted()@REF[n])
        fr.append((Q,SW@s.pose.bones[P+'Hips'].head))
    for o in new: bpy.data.objects.remove(o,do_unlink=True)
    cache[src]=(fr,SRh,k,f0); return cache[src]
seq=[]; segmeta=[]
for si,sg in enumerate(PRM['segments']):
    fr,SRh,k,f0=load(sg['src']); a=sg.get('start',f0); b=sg.get('end',f0+len(fr)-1)
    part=[]; sp=sg.get('speed',1.0); fl=float(a)
    while fl<=b+1e-6:
        i0=int(math.floor(fl)); i1=min(i0+1,b); u=fl-i0
        Qa,ha=fr[i0-f0]; Qb,hb=fr[i1-f0]
        Q={n:Qa[n].slerp(Qb[n],u) for n in BODY}; hw=ha.lerp(hb,u); d=(hw-SRh)*k
        part.append((Q,Vector((d.x,d.y,d.z)),sg['tag'],round(fl,3)))
        fl+=sp
    cf=sg.get('crossfade',0)
    if seq and cf>0:
        Qa,ha=seq[-1][0],seq[-1][1]
        for i in range(min(cf,len(part))):
            w=sm((i+1)/(cf+1)); Q,h,t,f=part[i]
            part[i]=({n:Qa[n].slerp(Q[n],w) for n in BODY},ha.lerp(h,w),t,f)
    segmeta.append({'tag':sg['tag'],'src':sg['src'].split('/')[-1],'frames':[a,b],'speed':sg.get('speed',1.0),'out_start':len(seq)+PRM['blend_in']})
    seq+=part
# root strip: horizontal hips offset relative to the first clip frame, linear drift (net travel) removed, weight shift kept
hx=[h.x for _,h,_,_ in seq]; hy=[h.y for _,h,_,_ in seq]; n_=len(seq)
lin=lambda i,arr: arr[0]+(arr[-1]-arr[0])*i/max(1,n_-1)
travel={'hips_dx_range':[min(hx)-hx[0],max(hx)-hx[0]],'hips_dy_range':[min(hy)-hy[0],max(hy)-hy[0]],'net':[hx[-1]-hx[0],hy[-1]-hy[0]]}
body_seq=[]
for i,(Q,h,t,f) in enumerate(seq):
    off=Vector((h.x-lin(i,hx),h.y-lin(i,hy),0))
    stance=Vector((hx[0],hy[0],0))*PRM.get('keep_stance_offset',1.0)
    hw=Vector((TRh.x,TRh.y,TRh.z+h.z))+off+stance
    body_seq.append((Q,AWi@hw,t,f))
# spine lift: rotate Spine (+children) and Spine1 (+children) about the horizontal axis perpendicular to the aim
def descendants(n):
    out=[n]
    for c in B[n].children_recursive: out.append(c.name)
    return out
DS={n:descendants(P+n) for n in ('Spine','Spine1')}
lift=PRM.get('spine_lift')
if lift:
    ax=AWq.inverted()@Vector(lift['axis_world']).normalized()
    for i,(Q,h,t,f) in enumerate(body_seq):
        w=0.0
        for (i0,i1,i2,i3) in lift['keys']:   # seq indices (clip frames only, before blend in)
            if i0<=i<i1: w=max(w,sm((i-i0)/max(1,i1-i0)))
            elif i1<=i<=i2: w=1.0
            elif i2<i<i3: w=max(w,1-sm((i-i2)/max(1,i3-i2)))
        if w<=0: continue
        Q=dict(Q)
        for part in ('Spine','Spine1'):
            R=Quaternion(ax,math.radians(lift['deg'])*0.5*w)
            for n in DS[part]:
                if n in Q: Q[n]=R@Q[n]
        body_seq[i]=(Q,h,t,f)
by=PRM.get('body_yaw')
if by:
    # whole-body yaw about world Z through the hips (side-on stance), optional upper-body counter-yaw; constant over the clip
    axz=AWq.inverted()@Vector((0,0,1)); DH=descendants(P+'Hips'); DU=descendants(P+by.get('counter_bone','Spine2'))
    for i,(Q,h,t,f) in enumerate(body_seq):
        Q=dict(Q); R=Quaternion(axz,math.radians(by['deg'])); Rc=Quaternion(axz,math.radians(by.get('counter_deg',0)))
        for n in DH:
            if n in Q: Q[n]=R@Q[n]
        for n in DU:
            if n in Q: Q[n]=Rc@Q[n]
        body_seq[i]=(Q,h,t,f)
# fingers & any non-body bone: rest
for i,(Q,h,t,f) in enumerate(body_seq):
    Q=dict(Q)
    for n in names:
        if n not in Q: Q[n]=QR[n]
    body_seq[i]=(Q,h,t,f)
# ---- frame schedule
BI=PRM['blend_in']; BO=PRM['blend_out']; frames=[]
def lerpQ(Qa,Qb,w): return {n:Qa[n].slerp(Qb[n],w) for n in names}
for i in range(BI):
    w=i/BI; frames.append(('in',w,lerpQ(QR,body_seq[0][0],sm(w)),HR.lerp(body_seq[0][1],sm(w)),'in',-1))
for (Q,h,t,f) in body_seq: frames.append(('clip',0,Q,h,t,f))
for i in range(1,BO+1):
    w=i/BO; frames.append(('out',w,lerpQ(body_seq[-1][0],QR,sm(w)),body_seq[-1][1].lerp(HR,sm(w)),'out',-1))
N=len(frames)
def rot_between(a,b): return a.normalized().rotation_difference(b.normalized())
def ik2(Q,h,up,lo,end,T,Qend,w,pole_hint=None):
    _,M=basis_from(Q,h); S=M[up].translation; E0=M[lo].translation; W0=M[end].translation
    l1=(E0-S).length; l2=(W0-E0).length; Tt=W0.lerp(T,w); D=Tt-S; d=min(D.length,l1+l2-1e-4); d=max(d,abs(l1-l2)+1e-4); u=D.normalized()
    pv=(pole_hint if pole_hint is not None else (E0-S)); p=pv-u*pv.dot(u)
    if p.length<1e-6: p=u.orthogonal()
    p.normalize(); a=(l1*l1-l2*l2+d*d)/(2*d); hh=math.sqrt(max(l1*l1-a*a,0)); E=S+u*a+p*hh
    Q[up]=rot_between(E0-S,E-S)@Q[up]; _,M=basis_from(Q,h); W1=M[end].translation; E1=M[lo].translation
    Q[lo]=rot_between(W1-E1,(S+u*d)-E1)@Q[lo]
    if Qend is not None: Q[end]=Q[end].slerp(Qend,w)
# feet step during blend in/out (left first in, right first out)
_,Mr=basis_from(QR,HR); _,Ms=basis_from(body_seq[0][0],body_seq[0][1]); _,Me=basis_from(body_seq[-1][0],body_seq[-1][1])
feet={s:(Mr[P+s+'Foot'].translation.copy(),Ms[P+s+'Foot'].translation.copy(),Me[P+s+'Foot'].translation.copy(),Mr[P+s+'Foot'].to_quaternion(),Ms[P+s+'Foot'].to_quaternion(),Me[P+s+'Foot'].to_quaternion()) for s in ('Left','Right')}
LIFT=PRM.get('lift',0.07)
def foot_u(kind,w,side):
    first='Left' if kind=='in' else 'Right'; a,b=(0.0,0.6) if side==first else (0.4,1.0); return min(1,max(0,(w-a)/(b-a)))
# ---- events in output frames
def ofr(tag,f):
    for i,(kind,w,Q,h,t,ff) in enumerate(frames):
        if t==tag and ff>=f-1e-6: return i
    raise KeyError((tag,f))
shots=[]
for s in PRM.get('shots',[]):
    shots.append({k:ofr(*v) for k,v in s.items()})
meta={'frames':N,'blend_in':BI,'blend_out':BO,'segments':segmeta,'travel_src_m':travel,'shots':shots,'roll_rest':PRM.get('roll_rest'),'roll_draw':PRM.get('roll_draw')}
def roll_at(fi):
    r=PRM['roll_rest']
    for s in shots:
        a,b,c,e=s['roll_a'],s['roll_b'],s['roll_c'],s['roll_e']
        if a<fi<b: r=PRM['roll_rest']+(PRM['roll_draw']-PRM['roll_rest'])*sm((fi-a)/(b-a))
        elif b<=fi<=c: r=PRM['roll_draw']
        elif c<fi<e: r=PRM['roll_draw']+(PRM['roll_rest']-PRM['roll_draw'])*sm((fi-c)/(e-c))
    return r
def nocked(fi): return any(s['nock']<=fi<s['release'] for s in shots)
def held(fi): return any(s.get('arrow_in_hand',s['nock'])<=fi<s['release'] for s in shots)
BOW='grip' in PRM
if BOW:
    G=PRM['grip']; gq=G['rot']; RFIX=Quaternion((gq[3],gq[0],gq[1],gq[2])).to_matrix().to_4x4(); GP=Matrix.Translation(Vector(G['pos']))
    NL=Vector(PRM['nock_rhand_local']); REST=Vector(PRM['arrow_rest_prop']); LMAX=PRM.get('draw_max_m',0.74)
def draw_w(fi):
    w=0.0
    for s_ in shots:
        if s_['nock']-3<=fi<s_['nock']: w=max(w,sm((fi-s_['nock']+3)/3))
        elif s_['nock']<=fi<s_['release']: w=1.0
    return w
draw_log=[]
out=[]
for fi,(kind,w,Q,h,t,f) in enumerate(frames):
    Q=dict(Q)
    if kind in ('in','out'):
        for side in ('Left','Right'):
            r0,s0,e0,qr,qs,qe=feet[side]; u=sm(foot_u(kind,w,side))
            A,Bp,qa,qb=(r0,s0,qr,qs) if kind=='in' else (e0,r0,qe,qr)
            T=A.lerp(Bp,u)+Vector((0,0,LIFT*math.sin(math.pi*u))); _,M=basis_from(Q,h)
            ik2(Q,h,P+side+'UpLeg',P+side+'Leg',P+side+'Foot',T,qa.slerp(qb,u),1.0,M[P+side+'Leg'].translation-M[P+side+'UpLeg'].translation)
    dw=draw_w(fi) if BOW else 0.0
    if dw>0:
        # draw length cap: the arrow (nock on the string at the draw hand) must reach past the arrow rest on the riser
        _,M=basis_from(Q,h); Mb=AW@M[P+'LeftHand']@GP@RFIX@Matrix.Rotation(math.radians(roll_at(fi)),4,'Z')
        rest=Mb@REST; nock=AW@M[P+'RightHand']@NL; L=(nock-rest).length
        if L>LMAX:
            want=rest+(nock-rest).normalized()*LMAX; T=AWi@((AW@M[P+'RightHand'].translation)+(want-nock))
            ik2(Q,h,P+'RightArm',P+'RightForeArm',P+'RightHand',T,M[P+'RightHand'].to_quaternion(),dw,M[P+'RightForeArm'].translation-M[P+'RightArm'].translation)
        draw_log.append((fi,round(L,3)))
    out.append(basis_from(Q,h)[0])
meta['draw_len_before_cap']=draw_log
# ---- write action
act=bpy.data.actions.new(PRM['action']); ad.action=act
for fi,bs in enumerate(out):
    for n in names:
        pb=arm.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=bs[n]
        pb.keyframe_insert('rotation_quaternion',frame=fi)
        if n==P+'Hips': pb.keyframe_insert('location',frame=fi)
sc.frame_start=0; sc.frame_end=N-1; sc.render.fps=30
if BOW:
    # ---- props: bow on LeftHand at the locked grip, arrow nock on RightHand (attack f10 binding), aimed through the arrow rest
    def imp(fp,name):
        before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fp)
        o=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
        for x in set(bpy.data.objects)-before:
            if x is not o: bpy.data.objects.remove(x)
        M=o.matrix_world.copy(); o.parent=None; o.data.transform(M); o.matrix_world=Matrix(); o.name=name; return o
    bow=imp(PRM['bow_fbx'],'QC_bow'); arrow=imp(PRM['arrow_fbx'],'QC_arrow')
    def bow_mw(fi):
        pb=arm.pose.bones[P+'LeftHand']
        return AW@pb.matrix@GP@RFIX@Matrix.Rotation(math.radians(roll_at(fi)),4,'Z')
    # arrow nock in RightHand space
    ARROW_AXIS=Vector(PRM.get('arrow_axis',(0,-1,0)))
    def arrow_mw(fi):
        nock=AW@arm.pose.bones[P+'RightHand'].matrix@NL
        rest=bow_mw(fi)@REST
        d=(rest-nock).normalized(); q=ARROW_AXIS.rotation_difference(d)
        return Matrix.LocRotScale(nock,q,None),nock,rest
    anim_props=[]
    for fi in range(N):
        sc.frame_set(fi)
        bow.matrix_world=bow_mw(fi); bow.keyframe_insert('location',frame=fi); bow.rotation_mode='QUATERNION'; bow.keyframe_insert('rotation_quaternion',frame=fi)
        Ma,nock,rest=arrow_mw(fi)
        arrow.matrix_world=Ma; arrow.rotation_mode='QUATERNION'; arrow.keyframe_insert('location',frame=fi); arrow.keyframe_insert('rotation_quaternion',frame=fi)
        arrow.hide_render=not held(fi); arrow.keyframe_insert('hide_render',frame=fi)
        anim_props.append({'f':fi,'roll':round(roll_at(fi),2),'nocked':nocked(fi),'nock':[round(x,3) for x in nock],'rest':[round(x,3) for x in rest],
            'arrow_dir':[round(x,3) for x in (rest-nock).normalized()],'nock_rest_m':round((rest-nock).length,3)})
    meta['props']=anim_props
    # ---- QC: bow vs body, string vs left forearm, arrow side
    def bvh_of(o):
        dg=bpy.context.evaluated_depsgraph_get(); oe=o.evaluated_get(dg); me=oe.to_mesh()
        vs=[oe.matrix_world@v.co for v in me.vertices]; ps=[tuple(p.vertices) for p in me.polygons]; oe.to_mesh_clear(); return BVHTree.FromPolygons(vs,ps),vs
    ST=[Vector(PRM['string_top']),Vector(PRM['string_bottom'])]
    HANDG={g.index for g in body.vertex_groups if any(k in g.name for k in ('LeftHand','LeftForeArm'))}
    handv=set()
    for v in body.data.vertices:
        if sum(g.weight for g in v.groups if g.group in HANDG)>0.3: handv.add(v.index)
    handp={p.index for p in body.data.polygons if any(i in handv for i in p.vertices)}
    qc=[]
    for fi in range(0,N,PRM.get('qc_step',2)):
        sc.frame_set(fi); bow.matrix_world=bow_mw(fi)
        tb,_=bvh_of(body); bb,bvs=bvh_of(bow)
        pairs=tb.overlap(bb)
        Mb=bow.matrix_world; s0=Mb@ST[0]; s1=Mb@ST[1]
        fa=AW@arm.pose.bones[P+'LeftForeArm'].head; hd=AW@arm.pose.bones[P+'LeftHand'].head
        # forearm segment vs string segment closest distance (centreline)
        def segseg(p1,q1,p2,q2):
            best=9
            for i in range(21):
                a=p1.lerp(q1,i/20); u=q2-p2; t=max(0,min(1,(a-p2).dot(u)/u.length_squared)); best=min(best,(a-(p2+u*t)).length)
            return best
        strd=segseg(fa.lerp(hd,0.5),hd,s0,s1)
        # bow side the string is on vs forearm: string mid relative to bow grip, projected on the body-facing axis
        up=(Mb.to_3x3()@Vector((0,0,1))).normalized()
        qc.append({'f':fi,'bow_body_overlap_pairs':len(pairs),'bow_body_overlap_pairs_not_left_hand':len([1 for a,b in pairs if a not in handp]),'string_forearm_centreline_m':round(strd,3),'bow_up_z':round(up.z,3),'roll':round(roll_at(fi),1)})
    meta['qc']=qc

def imp(fp,name):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fp)
    o=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
    for x in set(bpy.data.objects)-before:
        if x is not o: bpy.data.objects.remove(x)
    M=o.matrix_world.copy(); o.parent=None; o.data.transform(M); o.matrix_world=Matrix(); o.name=name; return o
def bvh_of(o):
    dg=bpy.context.evaluated_depsgraph_get(); oe=o.evaluated_get(dg); me=oe.to_mesh()
    vs=[oe.matrix_world@v.co for v in me.vertices]; ps=[tuple(p.vertices) for p in me.polygons]; oe.to_mesh_clear(); return BVHTree.FromPolygons(vs,ps),vs
if PRM.get('held'):
    H=PRM['held']; hp=imp(H['fbx'],'QC_held'); HL=Matrix(H['local']); hb=P+H['bone']
    HG={g.index for g in body.vertex_groups if any(k in g.name for k in H.get('exclude_groups',[]))}
    hv={v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in HG)>0.3}
    hpoly={p.index for p in body.data.polygons if any(i in hv for i in p.vertices)}
    tip=Vector(H.get('tip_local',(0,-0.775,0)))
    hq=[]; path=[]
    for fi in range(N):
        sc.frame_set(fi); hp.matrix_world=AW@arm.pose.bones[hb].matrix@HL
        hp.rotation_mode='QUATERNION'; hp.keyframe_insert('location',frame=fi); hp.keyframe_insert('rotation_quaternion',frame=fi)
        vis=H.get('visible',[0,N])[0]<=fi<H.get('visible',[0,N])[1]; hp.hide_render=not vis; hp.keyframe_insert('hide_render',frame=fi)
        tw=hp.matrix_world@tip; ax=(hp.matrix_world.to_3x3()@tip).normalized()
        path.append({'f':fi,'tip':[round(x,3) for x in tw],'tip_dir':[round(x,3) for x in ax]})
        if fi%PRM.get('qc_step',2)==0:
            tb,_=bvh_of(body); bb,_=bvh_of(hp); pr=tb.overlap(bb)
            hq.append({'f':fi,'overlap_not_hand':len([1 for a,b in pr if a not in hpoly])})
    meta['held_path']=path; meta['held_qc']=hq
json.dump(meta,open(PRM['meta_out'],'w'),indent=1)
if PRM.get('blend_out_path'): bpy.ops.wm.save_as_mainfile(filepath=PRM['blend_out_path'],compress=True)
if PRM.get('fbx_out'):
    base=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE', use_armature_deform_only=False)
    for o in bpy.context.view_layer.objects: o.select_set(False)
    for o in (arm,body): o.select_set(True)
    bpy.context.view_layer.objects.active=arm; sc.frame_set(0)
    bpy.ops.export_scene.fbx(filepath=PRM['fbx_out'], use_mesh_modifiers=True, bake_anim=True, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **base)
print('RETARGET OK',N,shots)
