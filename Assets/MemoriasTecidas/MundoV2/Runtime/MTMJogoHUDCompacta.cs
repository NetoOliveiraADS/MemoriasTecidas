using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private Texture2D losangoCompacta;
        private Vector2 CoordenadaHUDCompacta(Vector3 p)
        {
            Rect px=cameraDoMapa.pixelRect;float s=Mathf.Min(px.width/1280f,px.height/720f);
            return new Vector2((p.x-px.x-(px.width-1280*s)*.5f)/s,(px.yMax-p.y-(px.height-720*s)*.5f)/s);
        }
        private void LosangoCompacta(Rect r,Color cor)
        {
            if(losangoCompacta==null)
            {
                const int n=64;losangoCompacta=new Texture2D(n,n,TextureFormat.RGBA32,false){name="No_de_memoria",hideFlags=HideFlags.DontSave,filterMode=FilterMode.Bilinear};
                var pixels=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)
                    pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(32-Mathf.Abs(x-31.5f)-Mathf.Abs(y-31.5f)));
                losangoCompacta.SetPixels(pixels);losangoCompacta.Apply(false,true);
            }
            Color antiga=GUI.color;GUI.color=cor;GUI.DrawTexture(r,losangoCompacta);GUI.color=antiga;
        }
        private void HUDCompacta()
        {
            Color fundo=new Color(.14f,.13f,.11f,contrasteUI?1f:.94f);
            Color claro=new Color(.97f,.92f,.8f),dourado=new Color(.91f,.70f,.43f),fraco=new Color(.8f,.75f,.65f);
            var missao=MissaoAtualTecido();bool primeiro=GuiaPrimeiroFioAtivo();
            Fundo(new Rect(24,22,340,mostrarObjetivoTecido?220:132),fundo);
            string[] atalhos={"receita","diario","mapa","bolsa"},icones={"fio","diario","mapa","bolsa"},rotulos={"Rede","Caderno","Mapa","Bolsa"};
            for(int i=0;i<4;i++)
            {
                Rect r=new Rect(34+i*55,30,44,44);int foco=EstadoAcaoUI(r,"hudcompacta:"+atalhos[i],GUI.enabled);
                LosangoCompacta(r,foco>0?dourado:fraco);LosangoCompacta(new Rect(r.x+2,r.y+2,40,40),foco>0?new Color(.36f,.25f,.16f):fundo);
                IconeTecido(icones[i],new Rect(r.x+11,r.y+11,22,22),claro);
                TextoTecido(new Rect(r.x-3,76,50,18),rotulos[i],12,claro,false,TextAnchor.MiddleCenter);
                if(AcaoTecido(r,"hudcompacta:"+atalhos[i]))AlternarPainel(atalhos[i]);
            }
            int concluidas=0,total=tramaTecida.missoes==null?0:tramaTecida.missoes.Length;
            if(tramaTecida.missoes!=null)foreach(var m in tramaTecida.missoes)if(MTMTrama.Concluida(estado,m))concluidas++;
            TextoTecido(new Rect(264,32,88,20),"MEMÓRIAS",12,dourado,true);
            TextoTecido(new Rect(264,52,88,24),concluidas+" / "+total,19,claro,true);
            Fundo(new Rect(36,109,316,3),new Color(.36f,.31f,.24f));
            Fundo(new Rect(36,109,316*(total==0?0:concluidas/(float)total),3),dourado);
            if(mostrarObjetivoTecido)
            {
                string titulo=primeiro?aberturaMemorias.titulo:missao==null?"As histórias continuam":missao.titulo;
                TextoTecido(new Rect(38,127,312,24),ResumoHUDClaroUI(titulo,312,24,17,true),17,claro,true);
                TextoTecido(new Rect(38,157,312,44),ResumoHUDClaroUI(primeiro?ObjetivoPrimeiroFio():ObjetivoTecido(),312,44,14),14,claro);
                if(AcaoTecido(new Rect(30,122,330,84),"hudcompacta:objetivo"))
                {if(primeiro)AbrirRedeMemorias();else{abaCaderno="missoes";missaoSelecionadaTecido=missao==null?"":missao.id;AlternarPainel("diario");}}
            }
            string[] materiais={"fio","corda","pano","punho","enfeite"};
            for(int i=0;i<5;i++)
            {
                bool completo=estado.rede||MTMTrama.Quantidade(estado,materiais[i])>=MTMAutoriaRoteiro.Receita(materiais[i]);
                LosangoCompacta(new Rect(38+i*22,mostrarObjetivoTecido?214:122,12,12),completo?dourado:fraco);
                if(!completo)LosangoCompacta(new Rect(40+i*22,mostrarObjetivoTecido?216:124,8,8),fundo);
            }
            Rect receita=new Rect(28,mostrarObjetivoTecido?210:118,330,27);
            TextoTecido(new Rect(154,receita.y+2,170,20),"Materiais da rede",12,claro);
            if(AcaoTecido(receita,"hudcompacta:materiais"))AlternarPainel("receita");
            Fundo(new Rect(1104,22,152,60),fundo);IconeTecido(Noite?"noite":"sol",new Rect(1115,38,24,24),dourado);
            int minutos=Mathf.FloorToInt(estado.relogio*1440f);
            TextoTecido(new Rect(1150,29,96,25),(minutos/60).ToString("00")+":"+(minutos%60).ToString("00"),20,claro,true);
            TextoTecido(new Rect(1150,56,96,18),"Dia "+estado.dia,12,fraco);
            Fundo(new Rect(24,644,310,54),fundo);Fundo(new Rect(24,644,3,54),dourado);
            TextoTecido(new Rect(39,651,280,29),ResumoHUDClaroUI(atual.dados.nome,280,29,20,true),20,claro,true);
            TextoTecido(new Rect(39,680,280,16),"JAGUARUANA • REDE DE MEMÓRIAS",12,fraco);
            string[] nomes={"B  Bolsa","M  Mapa","J  Caderno"};
            for(int i=0;i<3;i++)
            {
                Rect r=new Rect(866+i*132,660,126,38);int foco=EstadoAcaoUI(r,"hudcompacta:rodape:"+i,true);
                Fundo(r,foco>0?new Color(.38f,.27f,.17f):fundo);TextoTecido(r,nomes[i],14,claro,true,TextAnchor.MiddleCenter);
                if(AcaoTecido(r,"hudcompacta:rodape:"+i))AlternarPainel(i==0?"bolsa":i==1?"mapa":"diario");
            }
            if(perto!=null&&Disponivel(perto))
            {
                Vector3 proj=cameraDoMapa.WorldToScreenPoint(perto.transform.position+Vector3.up*2.2f);Vector2 pos=CoordenadaHUDCompacta(proj);
                if(proj.z>0)
                {
                    Rect r=new Rect(Mathf.Clamp(pos.x-145,370,940),Mathf.Clamp(pos.y-42,250,avisoAte>Time.unscaledTime?476:570),290,38);
                    Fundo(r,fundo);LosangoCompacta(new Rect(r.x+7,r.y+5,28,28),dourado);
                    TextoTecido(new Rect(r.x+7,r.y+5,28,28),"E",14,new Color(.16f,.13f,.1f),true,TextAnchor.MiddleCenter);
                    TextoTecido(new Rect(r.x+44,r.y+8,236,24),ResumoHUDClaroUI(perto.dados.nome,236,24,14,true),14,claro,true);
                    if(AcaoTecido(r,"hudcompacta:interagir"))Interagir(perto);
                }
            }
            else
            {
                var local=primeiro?LocalPrimeiroFio():null;
                string destino=!string.IsNullOrEmpty(destinoGuiadoTecido)?destinoGuiadoTecido:local!=null?local.dados.id:RegiaoDaEtapaTecido(EtapaAtualTecido(missao));
                MTMPonto alvo=ProximaSaidaTecido(destino);
                if(alvo==null&&destino==atual.dados.id)alvo=primeiro&&string.IsNullOrEmpty(destinoGuiadoTecido)?AlvoPrimeiroFio():AlvoDaEtapaTecido(EtapaAtualTecido(missao));
                if(alvo!=null&&alvo.gameObject.activeInHierarchy)
                {
                    Fundo(new Rect(440,608,400,32),fundo);
                    string texto=(alvo.dados.tipo=="saida"?"Caminho para "+NomeMapaGuia(alvo.dados.destino):alvo.dados.nome)+" · "+Mathf.CeilToInt(Vector2.Distance(transform.position,alvo.transform.position))+" m";
                    TextoTecido(new Rect(451,613,378,22),ResumoHUDClaroUI(texto,378,22,13),13,claro,false,TextAnchor.MiddleCenter);
                    if(AcaoTecido(new Rect(440,608,400,32),"hudcompacta:rota"))AlternarPainel("mapa");
                }
            }
            if(avisoAte>Time.unscaledTime)
            {
                Fundo(new Rect(390,524,500,62),fundo);
                TextoTecido(new Rect(405,533,470,46),ResumoHUDClaroUI(aviso,470,46,14),14,claro,false,TextAnchor.MiddleCenter);
            }
        }
    }
}
