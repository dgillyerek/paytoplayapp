import bpy, bmesh, math
import numpy as np
from mathutils import Vector

def world_bm(obj):
    dg=bpy.context.evaluated_depsgraph_get()
    bm=bmesh.new(); bm.from_mesh(obj.data); bm.transform(obj.matrix_world); return bm

def slice_loops(bm, z):
    """Return list of loops at height z: dict(pts Nx2 ordered, closed, area, centroid)."""
    c=bm.copy()
    r=bmesh.ops.bisect_plane(c,geom=c.verts[:]+c.edges[:]+c.faces[:],plane_co=(0,0,z),plane_no=(0,0,1))
    edges=[e for e in r['geom_cut'] if isinstance(e,bmesh.types.BMEdge)]
    adj={}
    for e in edges:
        a,b=e.verts
        adj.setdefault(a,[]).append(b); adj.setdefault(b,[]).append(a)
    seen=set(); loops=[]
    for v0 in list(adj):
        if v0 in seen: continue
        # walk
        comp=[v0]; seen.add(v0); st=[v0]
        while st:
            x=st.pop()
            for y in adj[x]:
                if y not in seen: seen.add(y); comp.append(y); st.append(y)
        closed=all(len(adj[v])==2 for v in comp)
        # order
        if closed and len(comp)>=3:
            order=[comp[0]]; prev=None; cur=comp[0]
            while True:
                nx=[y for y in adj[cur] if y is not prev]
                if not nx: break
                n=nx[0]
                if n is order[0]: break
                order.append(n); prev,cur=cur,n
                if len(order)>len(comp): break
            pts=np.array([[v.co.x,v.co.y] for v in order])
            x,y=pts[:,0],pts[:,1]; area=0.5*abs(np.dot(x,np.roll(y,1))-np.dot(y,np.roll(x,1)))
            loops.append({'pts':pts,'closed':len(order)==len(comp),'area':area,'centroid':pts.mean(0)})
        else:
            pts=np.array([[v.co.x,v.co.y] for v in comp]); loops.append({'pts':pts,'closed':False,'area':0.0,'centroid':pts.mean(0)})
    c.free(); return loops

def point_in_poly(px,py,poly):
    x=poly[:,0]; y=poly[:,1]; x2=np.roll(x,-1); y2=np.roll(y,-1)
    inside=np.zeros(px.shape,bool)
    for i in range(len(x)):
        cond=((y[i]>py)!=(y2[i]>py))
        xi=(x2[i]-x[i])*(py-y[i])/(y2[i]-y[i]+1e-12)+x[i]
        inside^=cond&(px<xi)
    return inside

def dist_to_poly(px,py,poly):
    a=poly; b=np.roll(poly,-1,0); d=np.full(px.shape,1e9)
    for i in range(len(a)):
        ab=b[i]-a[i]; L=(ab**2).sum()+1e-12
        t=np.clip(((px-a[i,0])*ab[0]+(py-a[i,1])*ab[1])/L,0,1)
        qx=a[i,0]+t*ab[0]; qy=a[i,1]+t*ab[1]
        d=np.minimum(d,np.hypot(px-qx,py-qy))
    return d

def boundary_cycles(bm):
    """Trace boundary loops via BMLoop walking (handles pinch verts). Returns list of vertex lists (face-winding order)."""
    done=set(); cycles=[]
    for e in bm.edges:
        if not e.is_boundary or e in done: continue
        l=e.link_loops[0]; start=l; cyc=[]; guard=0
        while True:
            done.add(l.edge); cyc.append(l.vert)
            ll=l.link_loop_next
            while not ll.edge.is_boundary:
                ll=ll.link_loop_radial_next.link_loop_next
                guard+=1
                if guard>10**6: break
            l=ll
            if l is start or l.edge in done or guard>10**6: break
        cycles.append(cyc)
    return cycles

def split_pinches(cyc):
    out=[]; stack=[cyc]
    while stack:
        c=stack.pop(); pos={}
        rep=None
        for i,v in enumerate(c):
            if v in pos: rep=(pos[v],i); break
            pos[v]=i
        if rep is None: out.append(c); continue
        a,b=rep; stack.append(c[a:b]); stack.append(c[:a]+c[b:])
    return [c for c in out if len(c)>=3]

def fill_cycles(bm, uv_layer=None, max_len=5000):
    import bmesh
    vuv={}
    cycles=[s for c in boundary_cycles(bm) for s in split_pinches(c)]
    new=[]; fails=0
    for c in cycles:
        if len(c)>max_len: fails+=1; continue
        if uv_layer is not None:
            for v in c:
                if v not in vuv and v.link_loops: vuv[v]=v.link_loops[0][uv_layer].uv.copy()
        try:
            f=bm.faces.new(list(reversed(c))); new.append(f)
        except ValueError:
            fails+=1
    tri=bmesh.ops.triangulate(bm,faces=new,quad_method='BEAUTY',ngon_method='BEAUTY')['faces'] if new else []
    if uv_layer is not None:
        for f in tri:
            for l in f.loops:
                if l.vert in vuv: l[uv_layer].uv=vuv[l.vert]
    return {'cycles':len(cycles),'filled':len(new),'fill_tris':len(tri),'fails':fails}
