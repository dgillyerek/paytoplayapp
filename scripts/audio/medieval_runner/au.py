import subprocess, numpy as np, sys
SR=44100
def load(path, ch=1, rate=1.0):
    af=[]
    if rate!=1.0: af=['-af',f'asetrate={int(SR*rate)},aresample={SR}']
    # decode at SR first then pitch-shift by asetrate
    cmd=['ffmpeg','-v','error','-i',path,'-ar',str(SR),'-ac',str(ch),'-f','f32le','-']
    raw=subprocess.run(cmd,capture_output=True,check=True).stdout
    x=np.frombuffer(raw,np.float32).reshape(-1,ch).copy()
    if rate!=1.0:
        # resample: playing back faster = higher pitch. rate<1 lowers pitch & lengthens
        n=int(len(x)/rate); t=np.arange(n)*rate
        x=np.stack([np.interp(t,np.arange(len(x)),x[:,c]) for c in range(ch)],1).astype(np.float32)
    return x
def env(x,win=0.05):
    m=np.abs(x).max(1); w=int(SR*win); n=len(m)//w
    return 20*np.log10(np.maximum(1e-6, np.sqrt((m[:n*w].reshape(n,w)**2).mean(1))))
def onset(x,db=-30):
    m=np.abs(x).max(1); pk=m.max(); i=np.argmax(m>pk*10**(db/20)); return i
def trim(x, start=None, dur=None, pre=0.005, fade=0.05, db=-30):
    s=onset(x,db) if start is None else int(start*SR)
    s=max(0,s-int(pre*SR)); y=x[s:]
    if dur: y=y[:int(dur*SR)]
    y=y.copy(); f=min(len(y),int(fade*SR))
    if f>0: y[-f:]*=np.linspace(1,0,f)[:,None]**2
    fi=int(0.003*SR); y[:fi]*=np.linspace(0,1,fi)[:,None]
    return y
def tail_trim(y, db=-50):
    m=np.abs(y).max(1); pk=m.max(); idx=np.where(m>pk*10**(db/20))[0]
    e=idx[-1]+int(0.02*SR) if len(idx) else len(y)
    y=y[:e].copy(); f=min(len(y),int(0.03*SR)); y[-f:]*=np.linspace(1,0,f)[:,None]; return y
def mix(layers, ch=1):
    n=max(int(o*SR)+len(l) for l,o,g in layers); out=np.zeros((n,ch),np.float32)
    for l,o,g in layers:
        if l.shape[1]!=ch: l=np.repeat(l.mean(1,keepdims=True),ch,1)
        s=int(o*SR); out[s:s+len(l)]+=l*g
    return out
def lowpass(x, fc):
    from scipy.signal import butter, sosfilt
    return sosfilt(butter(4,fc,'low',fs=SR,output='sos'),x,axis=0).astype(np.float32)
def highpass(x, fc):
    from scipy.signal import butter, sosfilt
    return sosfilt(butter(2,fc,'high',fs=SR,output='sos'),x,axis=0).astype(np.float32)
def comp(x, amount=0.5):
    # soft saturation for grit/density
    k=1+amount*4; return (np.tanh(x*k)/np.tanh(k)).astype(np.float32)
def norm_rms(y, rms_db=-16, peak_db=-1.0):
    m=np.abs(y).max(1); act=y[m>m.max()*0.05] if (m>m.max()*0.05).any() else y
    r=np.sqrt((act**2).mean()); g=10**(rms_db/20)/max(r,1e-9)
    pk=np.abs(y).max()*g; 
    if pk>10**(peak_db/20): g*=10**(peak_db/20)/pk
    return (y*g).astype(np.float32)
def write(y, path, q=5):
    ch=y.shape[1]
    subprocess.run(['ffmpeg','-v','error','-y','-f','f32le','-ar',str(SR),'-ac',str(ch),'-i','-','-c:a','libvorbis','-q:a',str(q),path],input=y.astype(np.float32).tobytes(),check=True)
