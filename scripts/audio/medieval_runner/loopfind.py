import sys, os; sys.path.insert(0, os.path.dirname(__file__)); from au import *
from scipy.signal import stft
def feats(m, hop=1024):
    f,t,Z=stft(m,SR,nperseg=4096,noverlap=4096-hop); S=np.log1p(np.abs(Z[:1200])*50)
    S=(S-S.mean(1,keepdims=True)); S/=np.linalg.norm(S,axis=0,keepdims=True)+1e-9; return S,hop
def find(path, lo, hi, a_lo, a_hi, win=3.0):
    x=load(path,2); m=x.mean(1); S,hop=feats(m); fps=SR/hop; best=[]
    W=int(win*fps)
    for L in range(int(lo*fps),int(hi*fps)):
        for a in range(int(a_lo*fps),int(a_hi*fps),4):
            if a+L+W>=S.shape[1]: break
            c=np.mean(np.sum(S[:,a:a+W]*S[:,a+L:a+L+W],0)); best.append((c,a/fps,L/fps))
    best.sort(reverse=True); return best[:8]
if __name__=='__main__':
    for b in find(sys.argv[1],float(sys.argv[2]),float(sys.argv[3]),float(sys.argv[4]),float(sys.argv[5])): print(np.round(b,3))
