using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable] public sealed class MTMMapaGuiaDados
    { public MTMNoMapaGuia[] locais; }
    [Serializable] public sealed class MTMNoMapaGuia
    { public string id,nome; public float x,y; }

    public sealed partial class MTMJogo
    {
        private readonly Dictionary<string,MTMNoMapaGuia> locaisMapaGuia=new Dictionary<string,MTMNoMapaGuia>();
        private bool mapaGuiaAbertoUI;
        private static readonly string[] setasMapaGuia={"→","↗","↑","↖","←","↙","↓","↘"};
        private Color CorJogadorMapa { get { return contrasteUI?Color.black:CorTecido("#71402B",Color.black); } }

        private void PrepararMapaGuiaUI()
        {
            locaisMapaGuia.Clear();
            var recurso=Resources.Load<TextAsset>("MemoriasTecidas/Interface/mapa_jaguaruana");
            if(recurso==null){Debug.LogWarning("Mapa: coordenadas dos locais ausentes. A lista de lugares continua disponível.",this);return;}
            try
            {
                var dados=JsonUtility.FromJson<MTMMapaGuiaDados>(recurso.text);
                if(dados==null||dados.locais==null)throw new InvalidOperationException("Lista de locais ausente.");
                foreach(var no in dados.locais)
                {
                    if(no==null||string.IsNullOrEmpty(no.id)||locaisMapaGuia.ContainsKey(no.id)||
                        float.IsNaN(no.x)||float.IsNaN(no.y)||no.x<0||no.x>1||no.y<0||no.y>1)
                        throw new InvalidOperationException("Local repetido ou coordenada inválida.");
                    locaisMapaGuia.Add(no.id,no);
                }
            }
            catch(Exception e){locaisMapaGuia.Clear();Debug.LogWarning("Mapa: "+e.Message+" Use a lista de lugares.",this);}
        }

        private string NomeMapaGuia(string id)
        {
            MTMNoMapaGuia no;
            return id!=null&&locaisMapaGuia.TryGetValue(id,out no)&&!string.IsNullOrEmpty(no.nome)?no.nome:NomeCurtoTecido(id??"");
        }
        private string ObjetivoMapaGuia()
        {
            if(GuiaPrimeiroFioAtivo())
            {var local=LocalPrimeiroFio();return local==null?"":local.dados.id;}
            return RegiaoDaEtapaTecido(EtapaAtualTecido(MissaoAtualTecido()))??"";
        }
        private bool CoordenadaMapaGuia(string id,Rect janela,out Vector2 ponto)
        {
            MTMNoMapaGuia no;ponto=Vector2.zero;
            if(id==null||mapaJaguaruanaUI==null||!locaisMapaGuia.TryGetValue(id,out no))return false;
            Vector2 tamanho=TamanhoMapaCidadeUI(janela)*zoomMapaCidadeUI;
            ponto=janela.center+deslocamentoMapaCidadeUI+Vector2.Scale(new Vector2(no.x-.5f,no.y-.5f),tamanho);
            return true;
        }
        private void CentralizarMapaGuia(string id,Rect janela)
        {
            destinoMapa=id;rolagemDetalhe=Vector2.zero;
            MTMNoMapaGuia no;
            if(mapaJaguaruanaUI==null||!locaisMapaGuia.TryGetValue(id,out no)){mapaEmCartoes=true;return;}
            mapaEmCartoes=false;zoomMapaCidadeUI=1.6f;
            deslocamentoMapaCidadeUI=Vector2.Scale(new Vector2(.5f-no.x,.5f-no.y),TamanhoMapaCidadeUI(janela)*zoomMapaCidadeUI);
            LimitarMapaCidadeUI(janela);
        }

        private void MapaGuiadoUI()
        {
            if(!mapaGuiaAbertoUI)
            {
                mapaGuiaAbertoUI=true;zoomMapaCidadeUI=1f;deslocamentoMapaCidadeUI=Vector2.zero;
                if(string.IsNullOrEmpty(destinoMapa)||RegiaoTecido(destinoMapa)==null)
                    destinoMapa=string.IsNullOrEmpty(destinoGuiadoTecido)?atual.dados.id:destinoGuiadoTecido;
            }
            if(RegiaoTecido(destinoMapa)==null)destinoMapa=atual.dados.id;
            bool temArte=mapaJaguaruanaUI!=null&&locaisMapaGuia.Count>0;
            if(!temArte)mapaEmCartoes=true;
            string objetivo=ObjetivoMapaGuia();
            Rect janela=new Rect(24,154,912,490);
            Fundo(new Rect(0,0,1280,720),profundoUI);
            TextoTecido(new Rect(24,15,720,20),"MEMÓRIAS TECIDAS / SUA JORNADA",12,acentoTextoUI,true);
            TituloEditorial(new Rect(22,40,790,42),"Redes de Memórias",28,textoSuperficieUI);
            if(BotaoTecido(new Rect(864,30,126,42),"Bolsa [I]")){SoltarArrastoMapaUI();painel="bolsa";return;}
            if(BotaoTecido(new Rect(1002,30,142,42),"Caderno [J]")){SoltarArrastoMapaUI();painel="diario";return;}
            if(BotaoTecido(new Rect(1156,30,100,42),"Voltar")){SoltarArrastoMapaUI();VoltarInterfaceUI();return;}
            Fundo(new Rect(24,90,1232,1),AlfaTecido(ouro,.35f));
            if(AbaTecido(new Rect(24,104,100,36),"Mapa",!mapaEmCartoes,true)&&temArte)mapaEmCartoes=false;
            if(AbaTecido(new Rect(136,104,100,36),"Lugares",mapaEmCartoes,true))mapaEmCartoes=true;
            if(BotaoTecido(new Rect(260,104,96,36),"Você"))CentralizarMapaGuia(atual.dados.id,janela);
            if(BotaoTecido(new Rect(368,104,102,36),"Missão",false,RegiaoTecido(objetivo)!=null))CentralizarMapaGuia(objetivo,janela);
            if(BotaoTecido(new Rect(496,104,48,36),"−",false,temArte&&!mapaEmCartoes&&zoomMapaCidadeUI>1.001f))AmpliarMapaCidadeUI(janela,zoomMapaCidadeUI-.25f,janela.center);
            TextoTecido(new Rect(550,110,64,24),Mathf.RoundToInt(zoomMapaCidadeUI*100)+"%",14,textoSuperficieUI,true,TextAnchor.MiddleCenter);
            if(BotaoTecido(new Rect(620,104,48,36),"+",false,temArte&&!mapaEmCartoes&&zoomMapaCidadeUI<2.999f))AmpliarMapaCidadeUI(janela,zoomMapaCidadeUI+.25f,janela.center);
            if(BotaoTecido(new Rect(682,104,150,36),"Ver inteiro",false,temArte))
            {mapaEmCartoes=false;zoomMapaCidadeUI=1f;deslocamentoMapaCidadeUI=Vector2.zero;}
            var rota=CaminhoTecido(destinoMapa);
            if(mapaEmCartoes)
            {SoltarArrastoMapaUI();ListaMapaGuia(janela,objetivo);}
            else
            {
                DesenharMapaGuia(janela,objetivo,rota);
                // Os marcadores recebem o clique antes de o fundo iniciar um arraste.
                EntradaMapaCidadeUI(janela);
            }
            DetalheMapaGuia(objetivo,rota);
            if(painel!="mapa")return;
            string[] filtros={"Todos","Visitados","Objetivo"};
            for(int i=0;i<3;i++)if(AbaTecido(new Rect(24+i*128,654,116,32),filtros[i],filtroMapa==i,true))
            {filtroMapa=i;rolagemMapa=Vector2.zero;}
            TextoTecido(new Rect(420,660,516,22),"● Você   ★ Missão   1–2–3 Rota   × Bloqueado",13,textoSuperficieUI);
            TextoTecido(new Rect(24,695,1232,20),"M / Esc · fechar    Clique · escolher local    Roda / + / − · zoom    Arrastar / WASD · explorar    Tab · botões",12,textoSuperficieUI);
        }

        private bool FiltroMapaGuia(string id,string objetivo)
        {return filtroMapa==0||(filtroMapa==1&&estado.Tem("visitou_"+id))||(filtroMapa==2&&id==objetivo);}
        private void DesenharMapaGuia(Rect janela,string objetivo,List<string> rota)
        {
            Vector2 tamanho=TamanhoMapaCidadeUI(janela)*zoomMapaCidadeUI;
            Vector2 posicao=(janela.size-tamanho)*.5f+deslocamentoMapaCidadeUI;
            Fundo(janela,tinta);
            GUI.BeginGroup(janela);
            try {GUI.DrawTexture(new Rect(posicao,tamanho),mapaJaguaruanaUI,ScaleMode.StretchToFill,false);}
            finally {GUI.EndGroup();}
            // Um fio pontilhado por ligação real entre regiões; não cria passagens novas.
            for(int i=1;i<rota.Count;i++)
            {
                Vector2 a,b;
                if(CoordenadaMapaGuia(rota[i-1],janela,out a)&&CoordenadaMapaGuia(rota[i],janela,out b))FioRotaMapaGuia(a,b,janela);
            }
            string legenda="";Vector2 pontoLegenda=Vector2.zero;
            foreach(var r in regioes)
            {
                if(r==null||r.dados==null)continue;
                string id=r.dados.id;Vector2 c;
                if(!CoordenadaMapaGuia(id,janela,out c)||!new Rect(janela.x+22,janela.y+22,janela.width-44,janela.height-44).Contains(c))continue;
                bool voce=r==atual,missao=id==objetivo,selecionado=id==destinoMapa,livre=RegiaoLiberadaRoteiro(id);
                int passo=rota.IndexOf(id);
                bool visivel=FiltroMapaGuia(id,objetivo)||voce||selecionado||passo>=0;
                Color cor=voce?CorJogadorMapa:selecionado||passo>=0?terracotaUI:missao?acentoTextoUI:textoPapelUI;
                if(!livre||!visivel)cor=Color.Lerp(cor,tinta,.55f);
                CirculoTecido(new Rect(c.x-21,c.y-21,42,42),textoPapelUI);
                CirculoTecido(new Rect(c.x-18,c.y-18,36,36),cor);
                CirculoTecido(new Rect(c.x-14,c.y-14,28,28),profundoUI);
                string simbolo=!livre?"×":passo>=0?(passo+1).ToString():missao?"★":estado.Tem("visitou_"+id)?"✓":"•";
                TextoTecido(new Rect(c.x-14,c.y-14,28,28),simbolo,16,cor,true,TextAnchor.MiddleCenter);
                if(missao)
                {
                    Fundo(new Rect(c.x+12,c.y-32,22,22),profundoUI);
                    TextoTecido(new Rect(c.x+12,c.y-32,22,22),"★",16,acentoTextoUI,true,TextAnchor.MiddleCenter);
                }
                Rect hit=new Rect(c.x-21,c.y-21,42,42);
                if(AcaoTecido(hit,"marco:"+id)){destinoMapa=id;rolagemDetalhe=Vector2.zero;}
                if(voce)
                {
                    Fundo(new Rect(c.x-36,c.y+22,72,19),profundoUI);
                    TextoTecido(new Rect(c.x-36,c.y+22,72,19),"VOCÊ",12,CorJogadorMapa,true,TextAnchor.MiddleCenter);
                }
                if(hit.Contains(Event.current.mousePosition)||(tecladoUI&&focoUI=="marco:"+id))
                {legenda=NomeMapaGuia(id);pontoLegenda=c;}
            }
            if(legenda.Length>0)
            {
                Rect label=new Rect(Mathf.Clamp(pontoLegenda.x-126,janela.x+4,janela.xMax-256),Mathf.Clamp(pontoLegenda.y-55,janela.y+4,janela.yMax-34),252,30);
                Fundo(label,profundoUI);BordaTecido(label,ouro);
                TextoTecido(label,legenda,13,textoSuperficieUI,true,TextAnchor.MiddleCenter);
            }
            BordaTecido(janela,AlfaTecido(ouro,.4f));
        }
        private void FioRotaMapaGuia(Vector2 a,Vector2 b,Rect janela)
        {
            if(Event.current.type!=EventType.Repaint)return;
            int passos=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/10f));
            Rect dentro=new Rect(janela.x+4,janela.y+4,janela.width-8,janela.height-8);
            for(int i=0;i<=passos;i++)
            {
                Vector2 p=Vector2.Lerp(a,b,i/(float)passos);if(!dentro.Contains(p))continue;
                CirculoTecido(new Rect(p.x-4,p.y-4,8,8),profundoUI);
                CirculoTecido(new Rect(p.x-2,p.y-2,4,4),terracotaUI);
            }
        }
        private void ListaMapaGuia(Rect janela,string objetivo)
        {
            var lista=new List<MTMRegiao>();
            foreach(var r in regioes)if(r!=null&&r.dados!=null&&FiltroMapaGuia(r.dados.id,objetivo))lista.Add(r);
            Fundo(janela,tinta);
            rolagemMapa=IniciarRolagemUI(janela,rolagemMapa,new Rect(0,0,886,Mathf.Max(janela.height,lista.Count*72)));
            for(int i=0;i<lista.Count;i++)
            {
                var r=lista[i];string id=r.dados.id;Rect card=new Rect(8,i*72+4,870,64);
                Fundo(card,id==destinoMapa?papelUI:algodaoUI);
                TextoTecido(new Rect(24,card.y+8,822,25),NomeMapaGuia(id),19,textoPapelUI,true);
                TextoTecido(new Rect(24,card.y+37,822,20),r==atual?"VOCÊ ESTÁ AQUI":!RegiaoLiberadaRoteiro(id)?"A DESBLOQUEAR":id==objetivo?"OBJETIVO DA MISSÃO":estado.Tem("visitou_"+id)?"VISITADO":"A EXPLORAR",12,textoPapelUI);
                if(AcaoTecido(card,"lugarGuia:"+id)){destinoMapa=id;rolagemDetalhe=Vector2.zero;}
            }
            if(lista.Count==0)TextoTecido(new Rect(24,60,838,80),"Nenhum lugar neste filtro. Selecione Todos.",18,textoSuperficieUI,false,TextAnchor.MiddleCenter);
            TerminarRolagemUI();
        }
        private void DetalheMapaGuia(string objetivo,List<string> rota)
        {
            PainelTecido(new Rect(954,104,302,574),tinta,false);
            TextoTecido(new Rect(974,122,262,20),"DESTINO SELECIONADO",12,acentoTextoUI,true);
            TextoTecido(new Rect(974,151,262,64),NomeMapaGuia(destinoMapa),23,textoSuperficieUI,true);
            bool livre=RegiaoLiberadaRoteiro(destinoMapa);
            TextoTecido(new Rect(974,221,262,36),destinoMapa==atual.dados.id?"VOCÊ ESTÁ NESTE LUGAR":!livre?"CONTINUE AS MISSÕES PARA ABRIR":destinoMapa==objetivo?"OBJETIVO DA MISSÃO":"LOCAL DISPONÍVEL",12,acentoTextoUI,true);
            var ficha=MTMAutoriaRoteiro.Lugar(tramaTecida,destinoMapa);
            string texto=(ficha==null?"":ficha.descricao+"\n\n");
            if(!livre)
            {
                var bloqueio=tramaTecida==null||tramaTecida.bloqueios==null?null:Array.Find(tramaTecida.bloqueios,b=>b.regiao==destinoMapa);
                texto+=bloqueio==null?"Este lugar será liberado pelo progresso da jornada.":bloqueio.mensagem;
            }
            else if(rota.Count==0)texto+="Não há uma passagem disponível até este lugar na cena atual.";
            else if(rota.Count==1)texto+="Você chegou. Explore os pontos deste lugar.";
            else
            {
                texto+="PASSAGENS ATÉ O DESTINO\n";
                for(int i=0;i<rota.Count;i++)texto+=(i+1)+". "+NomeMapaGuia(rota[i])+"\n";
            }
            float altura=Mathf.Max(196,AlturaTecido(texto,238,15));
            rolagemDetalhe=IniciarRolagemUI(new Rect(974,268,262,196),rolagemDetalhe,new Rect(0,0,238,altura));
            TextoTecido(new Rect(0,0,238,altura),texto,15,textoSuperficieUI);TerminarRolagemUI();
            if(BotaoTecido(new Rect(974,482,262,46),"Guiar até aqui  →",true,livre&&rota.Count>0))
            {
                destinoGuiadoTecido=destinoMapa==atual.dados.id?"":destinoMapa;
                SoltarArrastoMapaUI();painel="";retornoColecaoUI="";LimparFocoUI();Parar();quadroDaAcao=Time.frameCount;
                Avisar(destinoMapa==atual.dados.id?"Você já está neste lugar. Explore o cenário.":"Siga a seta até a próxima saída. Use E para atravessar.");return;
            }
            if(BotaoTecido(new Rect(974,540,262,38),"Seguir missão",false,!string.IsNullOrEmpty(destinoGuiadoTecido)))
            {destinoGuiadoTecido="";if(RegiaoTecido(objetivo)!=null)destinoMapa=objetivo;rolagemDetalhe=Vector2.zero;}
            if(BotaoTecido(new Rect(974,587,262,32),"Ver este lugar",false,ImagemTecido(destinoMapa)!=null))
            {SoltarArrastoMapaUI();painel="arteLugar";LimparFocoUI();Parar();return;}
            TextoTecido(new Rect(974,630,262,38),"SUA REGIÃO\n"+NomeMapaGuia(atual.dados.id),13,CorJogadorMapa,true);
        }

        private void OrientacaoMapaGuiaHUD(MTMPonto alvo,string orientacao)
        {
            Rect faixa=new Rect(422,562,436,52);PainelTecido(faixa,FundoHUDClaroUI,false);
            bool disponivel=alvo!=null&&Disponivel(alvo);
            string seta="•",acao="M · abrir o mapa e escolher um destino";
            if(disponivel)
            {
                Vector3 origem=cameraDoMapa.WorldToScreenPoint(transform.position),destino=cameraDoMapa.WorldToScreenPoint(alvo.transform.position);
                Vector2 direcao=new Vector2(destino.x-origem.x,destino.y-origem.y);
                int setor=((Mathf.RoundToInt(Mathf.Atan2(direcao.y,direcao.x)*4f/Mathf.PI)%8)+8)%8;
                seta=setasMapaGuia[setor];
                acao=perto==alvo?(alvo.dados.tipo=="saida"?"E · atravessar para o próximo lugar":"E · interagir com o objetivo"):
                    alvo.dados.tipo=="saida"?"Siga a seta até a saída · E para atravessar":"Siga a seta até o objetivo";
                Vector3 gui=GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(destino.x,Screen.height-destino.y,0));
                // Realça o alvo apenas no espaço livre do HUD; a seta orienta fora da tela.
                if(destino.z>0&&new Rect(432,144,636,390).Contains(new Vector2(gui.x,gui.y)))
                {
                    BordaTecido(new Rect(gui.x-18,gui.y-18,36,36),ouro,2);
                    BordaTecido(new Rect(gui.x-21,gui.y-21,42,42),profundoUI,1);
                }
            }
            TextoTecido(new Rect(430,570,30,30),seta,28,acentoTextoUI,true,TextAnchor.MiddleCenter);
            TextoTecido(new Rect(470,568,376,22),ResumoHUDClaroUI(orientacao,376,22,13,true),13,textoSuperficieUI,true);
            TextoTecido(new Rect(470,592,376,18),acao,12,acentoTextoUI);
        }
    }
}
