import sys, os; sys.path.insert(0, os.path.dirname(__file__)); from au import *
os.chdir(os.environ.get('AUDIO_SRC', '.')); O='out/'
def wrap(x, a, L, k):
    """sub-loop x[a:a+L] whose end crossfades (equal power, k samples) into its start"""
    y=x[a:a+L].copy(); t=np.linspace(0,np.pi/2,k)[:,None]
    y[:k]=x[a:a+k]*np.sin(t)+x[a+L:a+L+k]*np.cos(t); return y
# village/hub: RandomMind "The Old Tower Inn" (author loop, used whole) + isaiah658 birds + distant blacksmith
v=load('v/Loop_The_Old_Tower_Inn.wav',2); L=len(v)
r=256; d=v[0]-v[-1]; v[-r:]+=np.linspace(0,1,r)[:,None]*d[None,:]*(r/(r+1))   # remove seam click
b=load('v/birds-isaiah658_0.ogg',2); k=SR
T=np.concatenate([b,b,b]); bl=wrap(T,0,L,k)
v=norm_rms(v,-23,-1.5); bl=norm_rms(bl,-38,-12)
W='x_100-CC0-wood-metal-SFX/'
def hit(f,rate=1.0): return lowpass(trim(load(W+f,2,rate),db=-30,fade=0.05),2200)
smith=[]
for t0 in (13.2,30.5):
    for i,(f,g) in enumerate([('hammer_02.ogg',1.0),('hammer_03.ogg',0.8),('hammer_02.ogg',0.9),('hammer_04.ogg',0.7)]):
        smith.append((hit(f,0.9),t0+i*0.62,g*0.05)); smith.append((hit('metal_hit_02.ogg',0.9),t0+i*0.62,g*0.03))
sm=mix(smith,2)
y=v.copy(); y[:len(bl)]+=bl[:L]; y[:min(L,len(sm))]+=sm[:L]
y=norm_rms(y,-22.5,-1.5); write(y,O+'amb_village_loop.ogg',q=0)
# battle: RandomMind "Medieval Battle", 16-bar (36.57 s) sub-loop with 1.2 s crossfade
x=load('battle_1.wav',2); a=int(8.0*SR); L=int(round(36.571*SR)); y=wrap(x,a,L,int(1.2*SR))
write(norm_rms(y,-21.5,-1.0),O+'mus_battle_loop.ogg',q=0)
for f in ['amb_village_loop','mus_battle_loop']: print(f, round(len(load(O+f+'.ogg',2))/SR,2), os.path.getsize(O+f+'.ogg'))
