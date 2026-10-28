using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Desenhos vetoriais rasterizados uma vez e reutilizados. Não giram a matriz da GUI.
    public sealed class MTMIconesTecido : IDisposable
    {
        public static readonly string[] Nomes={"algodao","carretel","novelo","corda","punho","pano","palha","rede","cera","moedas","fita","bandeiras","padrao","caminho","vaso","cesto","sol","folha","sino","caixa","flores","banco","lanterna","madeira","bilhete","amostra","papel","pedra","foto","fragmento","chave","documento","simbolo","medalhao","mapa","bolsa","diario","pessoa","check","noite"};
        private readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        private Texture2D tecido;
        public Texture2D Tecido
        {
            get
            {
                if(tecido!=null)return tecido;
                tecido=new Texture2D(48,48,TextureFormat.RGBA32,false){name="MT_Tecido_UI",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat,hideFlags=HideFlags.DontSave};
                var pixels=new Color32[48*48];
                for(int y=0;y<48;y++)for(int x=0;x<48;x++)
                {int a=x%4==0?32:y%4==0?20:0;if((x/4+y/4)%2==0&&x%4==0)a=48;pixels[y*48+x]=new Color32(255,255,255,(byte)a);}
                tecido.SetPixels32(pixels);tecido.Apply(false,true);return tecido;
            }
        }
        public Texture2D Obter(string nome)
        {
            if(string.IsNullOrEmpty(nome)||!Array.Exists(Nomes,n=>n==nome))nome="caixa";
            Texture2D textura;if(cache.TryGetValue(nome,out textura))return textura;
            var d=new Desenho();
            switch(nome)
            {
                case "algodao":
                    d.C(28,44,15);d.C(50,29,18);d.C(72,44,15);d.C(50,54,17);d.L(50,70,50,92);d.P(50,82,30,72,35,87);d.P(50,83,71,71,65,88);break;
                case "carretel":
                    d.R(23,13,54,8);d.R(23,79,54,8);d.L(31,21,31,79);d.L(69,21,69,79);
                    for(int y=27;y<76;y+=8)d.L(32,y,68,y+5);d.P(68,70,85,77,88,92);break;
                case "novelo":
                    d.C(47,47,29);d.P(23,31,54,27,74,42);d.P(19,44,53,38,76,52);d.P(22,58,56,49,72,64);d.P(34,21,40,47,58,73);d.P(60,72,65,87,85,87,92,81);break;
                case "corda":
                    d.C(48,45,31);d.C(48,45,22);d.C(48,45,13);d.P(47,45,71,69,80,85,93,89);d.P(38,16,32,9,23,13);break;
                case "punho":
                    d.C(50,24,11);d.P(43,33,19,78,81,78,57,33);for(int x=28;x<=72;x+=11)d.L(50,36,x,78);d.L(18,85,82,85);break;
                case "pano":case "amostra":case "simbolo":
                    d.P(15,20,78,14,86,77,24,86,15,20);d.P(24,86,21,31,78,24);d.L(30,34,77,28);d.L(28,41,77,35);
                    if(nome=="simbolo")d.Simbolo(42,48,22);else if(nome=="amostra"){d.L(36,45,73,70);d.L(36,70,73,45);}else{for(int x=30;x<81;x+=10)d.L(x,83,x+1,92);}break;
                case "palha":case "folha":
                    d.P(50,91,48,43,50,13);d.P(50,13,18,37,24,65,48,77,78,60,84,35,50,13);
                    for(int y=35;y<=66;y+=10){d.L(49,y+7,27,y-6);d.L(50,y+6,74,y-8);}break;
                case "rede":
                    d.P(6,21,24,60,50,77,76,60,94,21);d.P(6,21,27,38,50,44,73,38,94,21);
                    for(int i=0;i<5;i++){float x=25+i*12;float top=38+6*(1-Mathf.Abs(x-50)/25f);float bottom=77-Mathf.Abs(x-50)*.65f;d.L(x,top,50,bottom);d.L(x,top,x+10,bottom-5);}
                    d.L(6,21,20,10);d.L(94,21,80,10);for(int i=0;i<6;i++){float x=25+i*10,y=77-Mathf.Abs(x-50)*.65f;d.L(x,y,x,y+11);}break;
                case "cera":
                    d.P(22,37,47,22,79,34,54,49,22,37,22,71,54,86,79,69,79,34);d.L(54,49,54,86);d.L(29,48,29,65);break;
                case "moedas":
                    d.C(44,45,28);d.C(44,45,21);d.P(71,37,85,45,87,70,65,86,47,81);d.L(36,45,52,45);d.L(44,37,44,53);break;
                case "fita":
                    d.C(50,31,16);d.C(50,31,8);d.P(37,40,25,89,43,78,51,89,53,48);d.P(60,44,77,82,61,78,54,89);break;
                case "bandeiras":
                    d.P(9,22,33,32,66,32,91,22);d.P(16,25,17,54,33,45,39,65,44,32);d.P(55,32,61,65,69,45,84,54,84,25);d.L(13,75,87,75);break;
                case "padrao":
                    d.R(15,16,70,68);for(int x=26;x<=74;x+=16)d.L(x,18,x,82);for(int y=28;y<=76;y+=16)d.L(17,y,83,y);d.P(25,50,50,26,75,50,50,74,25,50);break;
                case "caminho":case "mapa":case "fragmento":
                    d.P(12,23,37,14,63,25,88,13,88,77,62,88,37,77,12,88,12,23);d.L(37,14,37,77);d.L(63,25,63,88);
                    d.P(23,64,32,56,45,63,55,51,76,39);d.C(76,39,5);if(nome=="fragmento")d.P(12,23,20,38,12,44);break;
                case "vaso":
                    d.P(34,14,66,14,63,30,77,48,79,69,68,86,32,86,21,69,23,48,37,30,34,14);d.L(31,20,69,20);d.L(25,55,75,55);d.P(25,62,38,71,50,62,62,71,75,62);break;
                case "cesto":case "bolsa":
                    d.P(17,37,83,37,77,86,23,86,17,37);d.P(30,36,32,17,68,17,70,36);d.L(20,48,80,48);
                    for(int x=28;x<=72;x+=11)d.L(x,49,x,83);for(int y=57;y<84;y+=9)d.L(23,y,77,y);if(nome=="bolsa"){d.R(36,51,28,22);d.C(50,60,3);}break;
                case "sol":
                    d.C(50,50,21);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;d.L(50+30*Mathf.Cos(a),50+30*Mathf.Sin(a),50+40*Mathf.Cos(a),50+40*Mathf.Sin(a));}break;
                case "sino":
                    d.C(50,17,5);d.P(21,74,29,60,30,40,39,25,61,25,70,40,71,60,79,74,21,74);d.C(50,81,6);d.L(30,65,70,65);break;
                case "caixa":
                    d.P(15,30,49,15,85,31,50,47,15,30,15,73,50,89,85,73,85,31);d.L(50,47,50,89);d.P(30,23,65,39,65,55);d.P(32,79,32,38,69,24);break;
                case "flores":
                    foreach(var v in new[]{new Vector2(31,34),new Vector2(66,36),new Vector2(50,19)}){d.C(v.x,v.y,11);d.C(v.x,v.y,4);d.L(v.x,v.y+11,50,85);}d.P(33,66,50,74,73,61);d.P(39,79,60,79,55,88,45,88,39,79);break;
                case "banco":
                    d.R(17,24,66,22);d.R(11,55,78,10);d.L(21,65,18,87);d.L(79,65,82,87);d.L(25,46,25,55);d.L(75,46,75,55);d.L(20,35,80,35);break;
                case "lanterna":
                    d.C(50,15,7);d.P(34,26,66,26,78,39,72,82,28,82,22,39,34,26);d.L(22,39,78,39);d.L(33,39,36,82);d.L(67,39,64,82);d.P(45,73,42,62,50,49,58,62,55,73,45,73);break;
                case "madeira":
                    d.P(16,31,28,17,87,71,74,85,16,31);d.L(28,29,74,72);d.L(34,47,63,74);d.C(55,53,5);break;
                case "bilhete":case "papel":case "documento":case "diario":
                    d.P(22,13,71,13,82,25,82,87,22,87,22,13);d.P(71,13,71,26,82,26);d.L(32,38,69,38);d.L(32,49,67,49);d.L(32,60,60,60);
                    if(nome=="bilhete")d.Simbolo(42,67,12);else if(nome=="documento")d.C(61,74,7);else if(nome=="diario")d.L(29,14,29,86);else d.L(33,72,52,72);break;
                case "pedra":
                    d.P(15,60,28,27,65,16,85,42,77,76,39,87,15,60);d.P(28,27,38,45,15,60);d.P(65,16,61,35,85,42);d.Simbolo(40,48,21);break;
                case "foto":
                    d.R(15,14,70,73);d.R(22,22,56,46);d.C(62,34,6);d.P(23,61,40,41,56,61,66,52,77,63);d.L(30,77,66,77);break;
                case "chave":
                    d.C(30,29,17);d.C(30,29,8);d.P(42,41,82,81,90,73,82,65,74,72);d.L(67,66,73,59);break;
                case "medalhao":
                    d.P(36,10,44,31,56,31,65,10);d.C(50,57,28);d.C(50,57,22);d.Simbolo(39,46,22);break;
                case "pessoa":d.C(50,29,16);d.P(17,86,23,62,38,53,62,53,77,62,83,86,17,86);break;
                case "check":d.P(18,50,39,72,83,25);break;
                case "noite":d.P(62,13,41,18,25,38,25,60,40,79,64,84,84,70,61,68,48,51,49,30,62,13);d.C(78,28,3);break;
            }
            textura=d.Textura("MT_Icone_"+nome);cache.Add(nome,textura);return textura;
        }
        public void Dispose()
        {foreach(var t in cache.Values)Liberar(t);cache.Clear();if(tecido!=null)Liberar(tecido);tecido=null;}
        private static void Liberar(Texture2D t)
        {if(Application.isPlaying)UnityEngine.Object.Destroy(t);else UnityEngine.Object.DestroyImmediate(t);}

        private sealed class Desenho
        {
            private const int N=128;
            private readonly Color32[] pixels=new Color32[N*N];
            private void Pixel(int x,int y,float a)
            {if(x<0||y<0||x>=N||y>=N)return;int i=(N-1-y)*N+x;byte alpha=(byte)Mathf.RoundToInt(Mathf.Clamp01(a)*255);if(alpha>pixels[i].a)pixels[i]=new Color32(255,255,255,alpha);}
            public void L(float x,float y,float xx,float yy)
            {
                Vector2 a=new Vector2(x,y)*1.28f,b=new Vector2(xx,yy)*1.28f,d=b-a;float len=d.sqrMagnitude;
                for(int py=Mathf.FloorToInt(Mathf.Min(a.y,b.y)-3);py<=Mathf.CeilToInt(Mathf.Max(a.y,b.y)+3);py++)
                for(int px=Mathf.FloorToInt(Mathf.Min(a.x,b.x)-3);px<=Mathf.CeilToInt(Mathf.Max(a.x,b.x)+3);px++)
                {Vector2 p=new Vector2(px+.5f,py+.5f);float t=len>0?Mathf.Clamp01(Vector2.Dot(p-a,d)/len):0;Pixel(px,py,1.9f-Vector2.Distance(p,a+t*d));}
            }
            public void C(float x,float y,float r)
            {
                x*=1.28f;y*=1.28f;r*=1.28f;
                for(int py=Mathf.FloorToInt(y-r-3);py<=Mathf.CeilToInt(y+r+3);py++)for(int px=Mathf.FloorToInt(x-r-3);px<=Mathf.CeilToInt(x+r+3);px++)
                Pixel(px,py,1.9f-Mathf.Abs(Vector2.Distance(new Vector2(px+.5f,py+.5f),new Vector2(x,y))-r));
            }
            public void P(params float[] v){for(int i=2;i<v.Length;i+=2)L(v[i-2],v[i-1],v[i],v[i+1]);}
            public void R(float x,float y,float w,float h){P(x,y,x+w,y,x+w,y+h,x,y+h,x,y);}
            public void Simbolo(float x,float y,float s){P(x,y+s,x+s*.5f,y,x+s,y+s,x,y+s*.4f,x+s,y+s*.4f);}
            public Texture2D Textura(string nome)
            {var t=new Texture2D(N,N,TextureFormat.RGBA32,false){name=nome,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};t.SetPixels32(pixels);t.Apply(false,true);return t;}
        }
    }
}
