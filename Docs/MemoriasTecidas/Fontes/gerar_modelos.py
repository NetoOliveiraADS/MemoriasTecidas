"""Geometria original de Jaguaruana. Python 3, biblioteca padrão, sem imagens de terceiros.
Coordenadas: X lateral, Y altura, Z profundidade. Fachada em Z=0; piso em Y=0.
Execute na pasta de trabalho ou passe --projeto e --saida. Unidades iguais às da cena 2D.
"""
import argparse, json, math, random
from pathlib import Path

P=argparse.ArgumentParser(); P.add_argument('--projeto',type=Path,default=Path(__file__).parent/'projeto')
P.add_argument('--saida',type=Path,default=Path(__file__).parent/'pacote/Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Sertao3D/modelos.json')
args=P.parse_args(); base=args.projeto/'Assets/MemoriasTecidas/MundoV2'
CREME='#e4d3aa'; CAL='#f0e6cf'; OCRE='#c89860'; TELHA='#a94e36'; BARRO='#bd7651'; TEAL='#366f69'; MADEIRA='#71503a'; ESCURO='#344c48'; FOLHA='#71845b'; VERDE='#869563'; OURO='#d0a460'
def rgb(h):return tuple(int(h[i:i+2],16)/255 for i in (1,3,5))
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b):return sum(x*y for x,y in zip(a,b))
class Model:
    def __init__(self,id):self.id=id;self.v=[];self.c=[];self.t=[];self.r=random.Random(id)
    def face(self,points,color,normal=None,wind=0):
        points=list(points)
        if normal and dot(cross(sub(points[1],points[0]),sub(points[2],points[0])),normal)<0:points.reverse()
        i=len(self.v)//3; c=rgb(color) if isinstance(color,str) else color
        for p in points:self.v.extend(round(x,5) for x in p);self.c.extend([*c,wind])
        for k in range(1,len(points)-1):self.t.extend([i,i+k,i+k+1])
    def box(self,x,y,z,w,h,d,c):
        a,b=x-w/2,x+w/2;u,v=y,y+h;s,t=z-d/2,z+d/2
        self.face([(a,u,s),(a,v,s),(b,v,s),(b,u,s)],c,(0,0,-1))
        self.face([(a,u,t),(b,u,t),(b,v,t),(a,v,t)],c,(0,0,1))
        self.face([(a,u,s),(a,u,t),(a,v,t),(a,v,s)],c,(-1,0,0))
        self.face([(b,u,s),(b,v,s),(b,v,t),(b,u,t)],c,(1,0,0))
        self.face([(a,v,s),(a,v,t),(b,v,t),(b,v,s)],c,(0,1,0))
        self.face([(a,u,s),(b,u,s),(b,u,t),(a,u,t)],c,(0,-1,0))
    def cylinder(self,x,y,z,r,h,c,n=10,r2=None):
        r2=r if r2 is None else r2
        for i in range(n):
            a,b=i*math.tau/n,(i+1)*math.tau/n
            p=(x+math.cos(a)*r,y,z+math.sin(a)*r);q=(x+math.cos(b)*r,y,z+math.sin(b)*r)
            s=(x+math.cos(a)*r2,y+h,z+math.sin(a)*r2);t=(x+math.cos(b)*r2,y+h,z+math.sin(b)*r2)
            self.face([p,q,t,s],c,(math.cos((a+b)/2),0,math.sin((a+b)/2)))
            if r2>0:self.face([(x,y+h,z),s,t],c,(0,1,0))
            self.face([(x,y,z),q,p],c,(0,-1,0))
    def branch(self,a,b,r,c):
        d=sub(b,a);l=math.sqrt(dot(d,d));axis=tuple(x/l for x in d)
        side=cross(axis,(0,1,0) if abs(axis[1])<.95 else (1,0,0));sl=math.sqrt(dot(side,side));side=tuple(x/sl for x in side);other=cross(axis,side)
        ring=[]
        for i in range(7):ring.append(tuple(r*(math.cos(i*math.tau/7)*side[j]+math.sin(i*math.tau/7)*other[j]) for j in range(3)))
        for i in range(7):
            q=ring[i];t=ring[(i+1)%7]
            self.face([tuple(a[j]+q[j] for j in range(3)),tuple(a[j]+t[j] for j in range(3)),tuple(b[j]+t[j]*.65 for j in range(3)),tuple(b[j]+q[j]*.65 for j in range(3))],c,q)
    def globe(self,x,y,z,rx,ry,rz,c,wind=0):
        col=rgb(c)
        for j in range(7):
            lat1=-math.pi/2+j*math.pi/7;lat2=lat1+math.pi/7
            for i in range(11):
                a=i*math.tau/11;b=a+math.tau/11
                def p(lat,lon):return(x+rx*math.cos(lat)*math.cos(lon),y+ry*math.sin(lat),z+rz*math.cos(lat)*math.sin(lon))
                points=[p(lat1,a),p(lat1,b),p(lat2,b),p(lat2,a)]
                if j==0:points=points[1:]
                if j==6:points=points[:3]
                middle=tuple(sum(p[k] for p in points)/len(points)-(x,y,z)[k] for k in range(3))
                shade=self.r.uniform(.92,1.06)
                self.face(points,tuple(min(1,q*shade) for q in col),middle,wind)
    def arch(self,x,y,z,w,h,c):
        r=w/2;pts=[(x-r,y,z),(x+r,y,z),(x+r,y+h-r,z)]
        for j in range(1,13):
            a=j*math.pi/12;pts.append((x+math.cos(a)*r,y+h-r+math.sin(a)*r,z))
        self.face(pts,c,(0,0,-1))
    def window(self,x,y,z,w,h):
        self.arch(x,y-.07,z-.012,w+.18,h+.18,OCRE);self.arch(x,y,z-.026,w,h,ESCURO)
        self.box(x,y+.04,z-.04,.055,h-.14,.035,CREME);self.box(x,y+h*.5,z-.04,w-.1,.055,.035,CREME)
        self.box(x,y-.11,z-.04,w+.32,.11,.21,CAL)
    def roof(self,w,d,y,rise):
        a,b=-w/2,w/2;z0,z1=-.17,d+.17
        self.face([(a,y,z0),(a,y,z1),(0,y+rise,z1),(0,y+rise,z0)],TELHA,(-1,1,0))
        self.face([(0,y+rise,z0),(0,y+rise,z1),(b,y,z1),(b,y,z0)],BARRO,(1,1,0))
        self.face([(a,y,z0),(0,y+rise,z0),(b,y,z0)],CREME,(0,0,-1))
        self.face([(a,y,z1),(b,y,z1),(0,y+rise,z1)],CREME,(0,0,1))
        self.box(0,y+rise-.015,d/2,.18,.13,d+.6,TELHA)
        # Fileiras de telha modeladas, sem textura esticada na cobertura.
        for j in range(max(2,int(d/.34))):
            z=j*.34
            self.branch((a,y+.018,z),(0,y+rise+.018,z),.026,BARRO)
            self.branch((0,y+rise+.018,z),(b,y+.018,z),.026,TELHA)
        for x in (-w/2,w/2):self.box(x,y-.1,d/2,.14,.18,d+.6,OCRE)
    def house(self,w,h,d,color=CREME,civic=False):
        self.box(0,0,d/2,w+.18,.18,d+.18,OCRE);self.box(0,.18,d/2,w,h,d,color)
        self.box(0,.22,-.045,w,.26,.12,BARRO);self.box(0,h-.22,-.08,w+.2,.17,.18,CAL)
        self.box(0,h+.06,d/2,w+.35,.16,d+.3,CAL)
        self.roof(w+.45,d,h+.22,min(h*.32,1.3))
        for x in (-w/2+.12,w/2-.12):self.box(x,.25,-.055,.22,h-.3,.17,CAL)
        door=min(1.2,w*.23);self.arch(0,.2,-.11,door,min(2.5,h*.76),MADEIRA)
        self.box(0,.22,-.135,.035,min(2.2,h*.65),.03,OCRE)
        for x in [-w*.33,w*.33]:self.window(x,h*.33,-.1,min(1.1,w*.19),min(1.5,h*.46))
        if h>4.8:
            for x in [-w*.33,0,w*.33]:self.window(x,h*.69,-.1,min(1,w*.19),h*.22)
        if civic:
            for x in [-w*.42,-w*.21,w*.21,w*.42]:
                self.cylinder(x,.2,-.23,.13,h-.3,CAL);self.box(x,.16,-.23,.42,.18,.42,CREME)
            self.box(0,h*.73,-.19,w*.31,.4,.12,TEAL)
    def church(self,w,h,d):
        nave=h*.52;self.house(w,nave,d,CAL)
        tw=w*.28;th=h*.94;td=max(1.1,d*.62)
        self.box(0,.15,td/2-.35,tw,th,td,CAL)
        for y in [nave*.84,nave+1.2,th-.95,th]:self.box(0,y,td/2-.35,tw+.28,.14,td+.22,TELHA)
        self.arch(0,.12,-.36,tw*.65,nave*.58,MADEIRA)
        self.window(0,nave+.45,-.37,tw*.58,1.6)
        self.cylinder(0,th+.14,td/2-.35,tw*.7,.8,CREME,n=4,r2=.08)
        self.box(0,th+.85,td/2-.35,.11,.85,.11,ESCURO);self.box(0,th+1.38,td/2-.35,.58,.1,.1,ESCURO)
        for x in [-w*.34,w*.34]:self.window(x,nave*.47,-.12,w*.14,nave*.32)
        for i in range(3):self.box(0,i*.08,-.24-i*.13,tw+1-i*.16,.08,.75-i*.13,CREME)
    def tree(self,h,palm=False,dry=False):
        self.cylinder(0,0,0,.18,h*.75,MADEIRA,r2=.095)
        if palm:
            for i in range(12):
                a=i*math.tau/12;r=h*.32
                tip=(math.cos(a)*r,h*.84+(.25 if i%2 else -.2),math.sin(a)*r)
                mid=(math.cos(a)*r*.6,h,math.sin(a)*r*.6)
                self.branch((0,h*.75,0),mid,.045,MADEIRA)
                for j in range(5):
                    f=j/5+.2;x=mid[0]*f;z=mid[2]*f;y=h*.75+(h*.25)*f
                    for s in (-1,1):
                        side=(x+math.cos(a+s*.9)*r*.43,y-.18,z+math.sin(a+s*.9)*r*.43)
                        self.face([(x,y,z),side,tip],VERDE if (i+j)%2 else FOLHA,wind=.8)
        else:
            for i in range(5):
                a=i*math.tau/5;r=h*.19
                end=(math.cos(a)*r,h*(.68+.04*(i%2)),math.sin(a)*r)
                self.branch((0,h*.35,0),end,.09,MADEIRA)
                if not dry:self.globe(*end,h*.22,h*.2,h*.22,FOLHA if i%2 else VERDE,.7)
            if not dry:self.globe(0,h*.83,0,h*.24,h*.21,h*.24,VERDE,.8)
    def bench(self,w=1.8):
        for x in [-w*.37,w*.37]:
            self.box(x,0,0,.1,.48,.55,ESCURO);self.box(x,.45,.22,.09,.46,.1,ESCURO)
        for z in [-.19,-.04,.11]:self.box(0,.48,z,w,.075,.12,TEAL)
        for y in [.66,.83]:self.box(0,y,.25,w,.1,.075,TEAL)
    def lamp(self,h=4.2):
        self.cylinder(0,0,0,.18,.15,OCRE);self.cylinder(0,.12,0,.06,h,ESCURO)
        self.box(0,h-.15,0,.39,.42,.39,OURO)
        for x in [-.2,.2]:
            for z in [-.2,.2]:self.box(x,h-.18,z,.045,.5,.045,ESCURO)
        self.cylinder(0,h+.27,0,.36,.22,ESCURO,4,r2=.03)
    def stall(self,w,d,h,color=TEAL):
        for x in [-w*.46,w*.46]:
            for z in [.08,d-.08]:self.box(x,0,z,.09,h,.09,MADEIRA)
        self.box(0,.7,d/2,w,.6,d*.7,MADEIRA)
        self.face([(-w*.55,h,0),(-w*.55,h,d),(0,h+.45,d),(0,h+.45,0)],color,(-1,1,0),.15)
        self.face([(0,h+.45,0),(0,h+.45,d),(w*.55,h,d),(w*.55,h,0)],CREME,(1,1,0),.15)
        for i in range(6):
            x=-w*.4+i*w*.16
            self.box(x,1.3,d*.5,w*.14,.12,d*.6,OCRE)
            for j in range(3):self.globe(x,1.49,d*.35+j*d*.15,.12,.12,.12,OURO if i%2 else BARRO)
    def monument(self,h=3,clock=False):
        self.box(0,0,.2,1.4,.2,1.1,CREME);self.box(0,.2,.2,1,.23,.8,OCRE)
        self.box(0,.43,.2,.68,h*.55,.65,CAL)
        self.box(0,h*.55+.43,.2,.85,.14,.8,TELHA)
        if clock:
            self.box(0,h*.55+.57,.2,1.1,1.2,.9,CAL)
            self.globe(0,h*.55+1.18,-.27,.39,.39,.012,ESCURO)
            self.box(0,h*.55+.98,-.29,.035,.36,.025,CREME);self.box(.11,h*.55+1.15,-.29,.25,.035,.025,CREME)
        else:
            y=h*.55+.57;self.cylinder(0,y,.2,.2,h*.25,TEAL,n=8,r2=.27);self.globe(0,y+h*.32,.2,.2,.25,.19,TEAL)
            self.branch((-.15,y+h*.2,.2),(-.35,y+h*.12,.2),.09,TEAL);self.branch((.15,y+h*.2,.2),(.32,y+h*.33,.2),.09,TEAL)
    def loom(self,w=2.4):
        for x in [-w/2,w/2]:
            for z in [0,1.2]:self.box(x,0,z,.14,1.8,.14,MADEIRA)
        for y in [.35,1.55]:self.box(0,y,.6,w+.15,.14,1.3,MADEIRA)
        for i in range(25):self.branch((-w*.45+i*w*.9/24,.55,.02),(-w*.45+i*w*.9/24,1.5,1.12),.012,CREME if i%4 else TEAL)
        for j in range(10):self.box(0,.55+j*.04,.04+j*.048,w*.9,.016,.018,BARRO if j%3 else TEAL)
    def crate(self,w=1):
        self.box(0,0,0,w,.7,w*.75,MADEIRA)
        for y in [.1,.35,.6]:
            for z in [-w*.38,w*.38]:self.box(0,y,z,w+.04,.16,.04,OCRE)

specs={}
for folder in ['LugaresV23','ExpansaoUrbanaV24']:
    for a in json.loads((base/folder/'Data/artes.json').read_text())['artes']:
        if a.get('tipo')=='personagem' or a['id'].startswith(('feirante_','barraqueiro_')):continue
        specs[a['id']]=a
base_art=json.loads((base/'Data/mundo.json').read_text())['artes']
for a in base_art:
    if a.get('piso') or a.get('personagem') or a.get('folha'):continue
    if a['id'] not in specs:specs[a['id']]={**a,'alturaMundo':6,'largura':max(1,a.get('colW',2))*100,'altura':600}

models=[];coverage=[]
for id,a in specs.items():
    h=a.get('alturaMundo',6);w=max(.5,a.get('colW',2));d=max(.5,a.get('colH',1));m=Model(id);kind=''
    if id in ('igreja','igreja_matriz'):m.church(w*1.12,h*.66,d);kind='igreja'
    elif any(k in id for k in ['carnauba']):m.tree(h*.82,True);kind='palmeira'
    elif any(k in id for k in ['arvore','arbusto','caatinga','algodoeiro']):m.tree(h*(.4 if 'arbusto' in id else .85),False,'caatinga' in id);kind='vegetacao'
    elif any(k in id for k in ['prefeitura','mercado','fabrica','memorial','casario','casa_','sobrado','loja','mercearia','oficina_varanda','urb_casa']):
        color=TEAL if 'verde' in id else BARRO if 'salmao' in id else OCRE if 'amarela' in id else CREME
        m.house(w,max(2.6,h*.58),d,color,any(k in id for k in ['prefeitura','mercado','memorial']));kind='edificio'
    elif 'banco' in id:m.bench(max(1.6,w));kind='banco'
    elif 'poste' in id:m.lamp(h*.77);kind='poste'
    elif 'torre_relogio' in id:m.monument(h*.6,True);kind='relogio'
    elif id=='urb_monumento_entrada_duas_figuras':
        m.box(0,0,0,1.6,.22,.8,CREME)
        for x in [-.35,.35]:
            m.cylinder(x,.22,0,.17,h*.36,TEAL,n=8,r2=.23);m.globe(x,h*.44,0,.18,.23,.17,TEAL)
            m.branch((x,h*.34,0),(0,h*.47,0),.08,TEAL)
        kind='monumento'
    elif any(k in id for k in ['monumento','busto','escultura']):m.monument(max(2.2,h*.7));kind='monumento'
    elif any(k in id for k in ['barraca','banca','quiosque','abrigo','cobertura']):m.stall(max(2.7,w),max(1.1,d),max(2.2,h*.55));kind='feira'
    elif id=='tear':m.loom(max(2.4,w));kind='tear'
    elif 'caixote' in id:m.crate(max(.7,w));kind='caixote'
    elif id in ['crucifixo','cruzeiro']:
        m.box(0,0,0,1,.25,.9,CREME);m.box(0,.25,0,.18,h*.7,.18,MADEIRA);m.box(0,h*.5,0,h*.32,.18,.18,MADEIRA);kind='cruz'
    elif id=='muro_jardim':m.box(0,0,d/2,w,1,d,OCRE);m.box(0,1,d/2,w+.1,.12,d+.1,CAL);kind='muro'
    elif id=='lixeiras':
        for i in range(3):m.cylinder((i-1)*.48,0,0,.19,.75,[TEAL,BARRO,OURO][i],8);m.cylinder((i-1)*.48,.75,0,.21,.06,ESCURO,8)
        kind='lixeiras'
    if not kind:continue # Imagem original permanece nos itens únicos sem modelo autoral.
    models.append({'id':id,'tipo':kind,'vertices':m.v,'cores':[round(c,4) for c in m.c],'triangulos':m.t})
    coverage.append({'id':id,'tipo':kind,'vertices':len(m.v)//3,'triangulos':len(m.t)//3})
bridge=Model('travessia_baixa');bridge.box(0,-.23,0,6.9,.23,34,OCRE)
for x in [-3.35,3.35]:
    bridge.box(x,.56,0,.15,.12,34,CREME)
    for j in range(15):
        z=-16.8+j*2.4;bridge.box(x,0,z,.22,.74,.22,CREME);bridge.box(x,.72,z,.29,.08,.29,CAL)
models.append({'id':bridge.id,'tipo':'ponte','vertices':bridge.v,'cores':[round(c,4) for c in bridge.c],'triangulos':bridge.t})
coverage.append({'id':bridge.id,'tipo':'ponte','vertices':len(bridge.v)//3,'triangulos':len(bridge.t)//3})
catalog={'versao':1,'autor':'Geometria original para Memórias Tecidas; referências arquitetônicas do projeto','modelos':models}
args.saida.parent.mkdir(parents=True,exist_ok=True);args.saida.write_text(json.dumps(catalog,separators=(',',':')),encoding='utf-8')
print(json.dumps({'modelos':len(models),'vertices':sum(x['vertices'] for x in coverage),'bytes':args.saida.stat().st_size,'cobertura':coverage},ensure_ascii=False,indent=2))
