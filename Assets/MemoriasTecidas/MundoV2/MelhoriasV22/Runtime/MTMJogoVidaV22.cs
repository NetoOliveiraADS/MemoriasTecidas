using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private MTMVidaV22 vidaV22;
        private MTMRegiao ultimaRegiaoV22;
        private float nomeAteV22;
        private Vector2 rolagemMapaV22, rolagemFalaV22;
        private string falaAnteriorV22;
        private GUIStyle nomeV22, ajudaV22, falaV22;
        public bool V22Bloqueado { get { return Bloqueado || !pronto; } }
        public bool V22Interior { get { return atual != null && atual.dados.interior; } }
        public bool V22FalandoCom(string nome) { return falas != null && locutor == nome; }

        // O instalador insere UMA chamada no OnGUI original. O restante do jogo não é substituído.
        private bool DesenharVidaV22()
        {
            if (vidaV22 == null) vidaV22 = GetComponent<MTMVidaV22>();
            if (vidaV22 == null || !vidaV22.enabled || vidaV22.versaoInstalada < 22) return false;
            if (!pronto || atual == null || cameraDoMapa == null) return true;
            if (ultimaRegiaoV22 != atual) { ultimaRegiaoV22 = atual; nomeAteV22 = Time.unscaledTime + 3.2f; }
            if (!vidaV22.mostrarInterface) return true;
            Estilos();
            if (nomeV22 == null)
            {
                nomeV22 = new GUIStyle(tituloUI) { alignment = TextAnchor.MiddleCenter, fontSize = 23 };
                ajudaV22 = new GUIStyle(pequenoUI) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
                falaV22 = new GUIStyle(textoUI) { fontSize = 21, richText = false };
            }
            Matrix4x4 matriz = GUI.matrix; Color cor = GUI.color;
            Rect px = cameraDoMapa.pixelRect;
            float escala = Mathf.Min(px.width / 1280f, px.height / 720f);
            Vector3 origem = new Vector3(px.x+(px.width-1280f*escala)*.5f,Screen.height-px.yMax+(px.height-720f*escala)*.5f,0);
            GUI.matrix = Matrix4x4.TRS(origem,Quaternion.identity,Vector3.one*escala); GUI.color=Color.white;
            try
            {
                if (falas != null) DialogoVidaV22();
                else if (painel.Length > 0) PainelVidaV22();
                else
                {
                    Fundo(new Rect(22,18,310,34),new Color(.04f,.11f,.12f,.75f));
                    GUI.Label(new Rect(32,22,290,27),atual.dados.nome,pequenoUI);
                    Fundo(new Rect(1070,18,188,34),new Color(.04f,.11f,.12f,.75f));
                    GUI.Label(new Rect(1075,22,178,27),"Dia "+estado.dia+"  •  "+Mathf.FloorToInt(estado.relogio*24f).ToString("00")+"h",ajudaV22);
                    if (Time.unscaledTime < nomeAteV22)
                    {
                        float a=Mathf.Clamp01((nomeAteV22-Time.unscaledTime)/.6f); GUI.color=new Color(1,1,1,a);
                        Fundo(new Rect(365,22,550,48),tinta);GUI.Label(new Rect(377,25,526,41),atual.dados.nome,nomeV22);GUI.color=Color.white;
                    }
                    if (vidaV22.mostrarObjetivo)
                    { Fundo(new Rect(22,62,460,62),tinta);GUI.Label(new Rect(32,68,440,53),estado.Objetivo(),pequenoUI); }
                    if(perto!=null && Disponivel(perto))
                    { Fundo(new Rect(365,616,550,39),tinta);GUI.Label(new Rect(375,620,530,32),"[ E ]  "+perto.dados.nome,centroUI); }
                    // Uma indicação próxima, nunca todos os nomes atravessando a tela.
                    MTMPonto saida=null;float distancia=10f;
                    if(atual.pontos!=null)foreach(var p in atual.pontos)
                    {
                        if(!Disponivel(p)||p.dados.tipo!="saida")continue;
                        float d=Vector2.Distance(transform.position,p.transform.position);
                        if(d<distancia){saida=p;distancia=d;}
                    }
                    if(saida!=null && perto!=saida)
                    { Fundo(new Rect(350,576,580,42),tinta);GUI.Label(new Rect(360,578,560,38),"Saída: "+saida.dados.nome+" • siga o círculo no chão",ajudaV22); }
                    if(avisoAte>Time.unscaledTime)
                    { Fundo(new Rect(235,540,810,58),tinta);GUI.Label(new Rect(249,548,782,48),aviso,ajudaV22); }
                    if(vidaV22.mostrarAjuda)
                    {
                        Fundo(new Rect(300,678,680,27),new Color(.04f,.11f,.12f,.65f));
                        GUI.Label(new Rect(310,680,660,23),"Shift correr  •  E conversar  •  M mapa  •  I bolsa  •  J diário  •  F1 ocultar HUD",ajudaV22);
                    }
                }
            }
            finally { GUI.matrix=matriz;GUI.color=cor; }
            return true;
        }

        private void DialogoVidaV22()
        {
            Rect quadro = new Rect(40,365,1200,350);
            Fundo(new Rect(62,400,1156,228),new Color(.035f,.11f,.12f,.98f));
            if(vidaV22.painelTecido!=null)GUI.DrawTexture(quadro,vidaV22.painelTecido,ScaleMode.StretchToFill,true);
            else { Fundo(new Rect(44,378,1192,280),tinta);Fundo(new Rect(44,378,1192,4),ouro); }
            float x=retratoAtual!=null?246f:86f;
            if(retratoAtual!=null)
            {
                Fundo(new Rect(80,417,144,164),new Color(.14f,.24f,.24f,.8f));
                GUI.DrawTextureWithTexCoords(new Rect(84,421,136,155),retratoAtual,recorteAtual,true);
            }
            GUI.Label(new Rect(x,409,1120-x,35),locutor,tituloUI);
            string fala= falas[pagina] ?? "";
            if(falaAnteriorV22!=fala){rolagemFalaV22=Vector2.zero;falaAnteriorV22=fala;}
            float largura=1134-x;
            float altura=Mathf.Max(103f,falaV22.CalcHeight(new GUIContent(fala),largura-22));
            rolagemFalaV22=GUI.BeginScrollView(new Rect(x,452,largura,103),rolagemFalaV22,new Rect(0,0,largura-22,altura));
            GUI.Label(new Rect(0,0,largura-22,altura),fala,falaV22);GUI.EndScrollView();
            GUI.Label(new Rect(x,558,1130-x,35),contexto??"",pequenoUI);
            GUI.Label(new Rect(86,596,740,28),"E / Enter: continuar   •   Esc: sair   •   "+(pagina+1)+" / "+falas.Length,pequenoUI);
            if(GUI.Button(new Rect(970,589,198,35),pagina+1==falas.Length?"Concluir":"Continuar",botaoUI))Avancar();
        }
        private void PainelVidaV22()
        {
            Fundo(new Rect(44,108,1192,556),tinta);
            Fundo(new Rect(44,108,1192,4),ouro);
            Fundo(new Rect(44,660,1192,4),new Color(.72f,.36f,.23f));
            string titulo=painel=="mapa"?"Caminhos de Jaguaruana":painel=="bolsa"?"Sua bolsa":"Diário de memórias";
            GUI.Label(new Rect(70,126,1015,42),titulo,tituloUI);
            if(GUI.Button(new Rect(1104,126,104,36),"Fechar",botaoUI)){painel="";return;}
            if(painel=="mapa")MapaVidaV22();else if(painel=="bolsa")BolsaUI();else DiarioUI();
        }
        private void MapaVidaV22()
        {
            GUI.Label(new Rect(72,174,1120,30),"Escolha a imagem de um lugar para ver o caminho. Viaje pelas saídas do cenário.",pequenoUI);
            int linhas=Mathf.CeilToInt(regioes.Length/3f);
            rolagemMapaV22=GUI.BeginScrollView(new Rect(70,214,1140,325),rolagemMapaV22,new Rect(0,0,1110,linhas*162));
            for(int i=0;i<regioes.Length;i++)
            {
                var r=regioes[i];if(r==null)continue;
                Rect card=new Rect((i%3)*370,(i/3)*162,351,150);
                Fundo(card,r==atual?new Color(.25f,.39f,.36f):new Color(.13f,.23f,.23f));
                var imagem=vidaV22.Imagem(r.dados.id);
                if(imagem!=null)GUI.DrawTexture(new Rect(card.x+8,card.y+7,335,105),imagem,ScaleMode.ScaleToFit,true);
                GUI.Label(new Rect(card.x+8,card.y+112,335,34),r.dados.nome+(r==atual?" • aqui":""),centroUI);
                if(GUI.Button(card,GUIContent.none,GUIStyle.none))destinoMapa=r.dados.id;
                if(destinoMapa==r.dados.id)Fundo(new Rect(card.x,card.y+147,351,3),ouro);
            }
            GUI.EndScrollView();
            string rota=string.IsNullOrEmpty(destinoMapa)?"As imagens representam os locais. Este é um mapa de jogo, com distâncias e posições adaptadas.":Rota(destinoMapa);
            GUI.Label(new Rect(76,557,1116,92),rota,pequenoUI);
        }
    }
}
