using System;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private void PrimeiroFioUI()
        {
            if(aberturaMemorias==null){painel="";return;}
            Fundo(new Rect(0,0,1280,720),new Color(.16f,.12f,.09f,.60f));
            PainelTecido(new Rect(28,32,1224,656),algodaoUI);
            Fundo(new Rect(28,32,1224,138),tinta);TramaFaixa(new Rect(28,170,1224,8));
            TextoTecido(new Rect(58,54,1000,22),"PRIMEIRO ENCONTRO  /  OBSERVAR · EXPERIMENTAR · COMPARTILHAR",12,acentoTextoUI,true);
            TextoTecido(new Rect(58,90,1000,46),aberturaMemorias.titulo,32,textoSuperficieUI,true);
            if(BotaoTecido(new Rect(1080,80,138,40),"Voltar")){LimparPrimeiroFio();painel="";return;}
            TextoTecido(new Rect(58,192,1150,38),"Escolha uma cor por passagem. A amostra vai ganhando forma conforme você tece.",19,textoPapelUI);
            Fundo(new Rect(58,244,692,278),CorTecido("#6A4E38",textoPapelUI));
            TextoTecido(new Rect(82,258,640,24),"SUA AMOSTRA  ·  "+passagensPrimeiroFio.Count+" / 4 PASSAGENS",12,algodaoUI,true);
            DesenharAmostraUI(new Rect(128,312,550,162));
            TextoTecido(new Rect(82,490,640,20),"Uma pequena experiência com as cores do jogo",12,algodaoUI,false,TextAnchor.MiddleCenter);
            Fundo(new Rect(774,244,444,278),papelUI);CosturaTecido(new Rect(774,244,444,278),AlfaTecido(terracotaUI,.5f));
            TextoTecido(new Rect(796,262,398,28),"Observe o modelo",22,textoPapelUI,true);
            for(int i=0;i<4;i++)
            {
                int indice=aberturaMemorias.padrao[i];float y=304+i*41;
                TextoTecido(new Rect(800,y,36,28),(i+1).ToString("00"),15,terracotaUI,true);
                Rect amostra=new Rect(846,y+2,84,22);Fundo(amostra,CorTecido(aberturaMemorias.cores[indice],ouro));BordaTecido(amostra,AlfaTecido(tinta,.4f));
                TextoTecido(new Rect(950,y,240,28),aberturaMemorias.nomes[indice],18,textoPapelUI,true);
            }
            TextoTecido(new Rect(796,478,398,28),"Leia de cima para baixo.",14,textoPapelUI);
            for(int i=0;i<3;i++)
            {
                Rect botao=new Rect(58+i*232,536,216,44);
                if(BotaoTecido(botao,(i+1)+"  ·  "+aberturaMemorias.nomes[i],false,passagensPrimeiroFio.Count<4))EscolherPassagemPrimeiroFio(i);
                Fundo(new Rect(botao.x+1,botao.yMax-5,botao.width-2,4),CorTecido(aberturaMemorias.cores[i],ouro));
            }
            TextoTecido(new Rect(774,540,444,40),"Sem tempo limite. Você pode desfazer e experimentar de novo.",14,textoPapelUI);
            TextoTecido(new Rect(58,590,1160,32),retornoPrimeiroFio,15,terracotaUI,true);
            if(BotaoTecido(new Rect(58,626,216,36),"Desfazer  [U]",false,passagensPrimeiroFio.Count>0))DesfazerPrimeiroFio();
            if(BotaoTecido(new Rect(290,626,216,36),"Recomeçar"))LimparPrimeiroFio();
            if(BotaoTecido(new Rect(774,622,444,40),"Conferir a amostra  [Enter]",true))ConferirPrimeiroFio();
            TextoTecido(new Rect(526,628,226,32),"1 / 2 / 3 · escolher cor",12,textoPapelUI,false,TextAnchor.MiddleCenter);
            TextoTecido(new Rect(58,670,1150,14),"Esc sai da atividade. A observação já registrada permanece salva. Esta amostra não usa os materiais da bolsa.",10,AlfaTecido(textoPapelUI,.8f));
        }

        private void DesenharAmostraUI(Rect r)
        {
            Fundo(new Rect(r.x-12,r.y-12,r.width+24,8),terracotaUI);
            Fundo(new Rect(r.x-12,r.yMax+6,r.width+24,8),terracotaUI);
            for(int coluna=0;coluna<18;coluna++)
                Fundo(new Rect(r.x+coluna*(r.width/17f),r.y-5,2,r.height+16),AlfaTecido(algodaoUI,.72f));
            for(int linha=0;linha<4;linha++)
            {
                float y=r.y+linha*40;bool inserida=linha<passagensPrimeiroFio.Count;
                Color cor=inserida?CorTecido(aberturaMemorias.cores[passagensPrimeiroFio[linha]],ouro):AlfaTecido(algodaoUI,.10f);
                Fundo(new Rect(r.x-4,y,r.width+10,27),cor);
                for(int coluna=0;coluna<18;coluna++)if((linha+coluna)%2==0)
                    Fundo(new Rect(r.x+coluna*(r.width/17f),y,2,27),AlfaTecido(algodaoUI,.9f));
                TextoTecido(new Rect(r.x-46,y,30,25),(linha+1).ToString(),16,algodaoUI,true,TextAnchor.MiddleCenter);
            }
        }

        private void RedeMemoriasUI()
        {
            if(aberturaMemorias==null)
            {TextoTecido(new Rect(80,300,1120,120),"A rede de memórias não foi carregada. Confira abertura_memorias.json. As missões e o caderno continuam disponíveis.",22,textoPapelUI);return;}
            var motivos=aberturaMemorias.motivos;
            if(!Array.Exists(motivos,m=>m.id==motivoSelecionadoRede))motivoSelecionadoRede=motivos[0].id;
            PainelTecido(new Rect(58,252,736,378),tinta);
            TextoTecido(new Rect(84,270,682,24),"SUA REDE  ·  "+MTMPrimeiroFio.Quantidade(estado,aberturaMemorias)+" / "+motivos.Length+" MOTIVOS REVELADOS",13,acentoTextoUI,true);
            // Linhas de urdidura fixas; as cores dos motivos registram a progressão.
            for(int i=0;i<30;i++)Fundo(new Rect(88+i*23,302,1,272),AlfaTecido(algodaoUI,.13f));
            Fundo(new Rect(80,300,692,4),terracotaUI);Fundo(new Rect(80,574,692,4),terracotaUI);
            for(int i=0;i<motivos.Length;i++)
            {
                var motivo=motivos[i];bool revelado=MTMPrimeiroFio.Revelado(estado,motivo),selecionado=motivo.id==motivoSelecionadoRede;
                Rect r=new Rect(88+i%3*230,312+i/3*88,214,76);
                Color cor=CorTecido(motivo.cor,ouro);
                Fundo(r,revelado?Color.Lerp(tinta,cor,.16f):profundoUI);
                if(revelado)
                {
                    PanoTecido(r,AlfaTecido(cor,.48f));
                    for(int fio=0;fio<5;fio++)Fundo(new Rect(r.x+7,r.yMax-14+fio*2,r.width-14,1),AlfaTecido(cor,.6f));
                    IconeTecido(motivo.icone,new Rect(r.x+12,r.y+15,38,38),cor);
                }
                else
                {
                    NoTecido(new Vector2(r.x+30,r.y+34),AlfaTecido(algodaoUI,.35f),6);
                    NoTecido(new Vector2(r.x+30,r.y+34),profundoUI,3);
                }
                TextoTecido(new Rect(r.x+60,r.y+12,142,44),revelado?motivo.titulo:"Ainda por descobrir",14,revelado?textoSuperficieUI:AlfaTecido(textoSuperficieUI,.60f),revelado);
                BordaTecido(r,selecionado?ouro:AlfaTecido(algodaoUI,.16f),selecionado?2:1);
                if(AcaoTecido(r,"motivo:"+motivo.id)){motivoSelecionadoRede=motivo.id;rolagemDetalhe=Vector2.zero;}
            }
            bool terminou=estado.Tem(MTMPrimeiroFio.Memoria);
            if(BotaoTecido(new Rect(84,590,332,28),terminou?"Revisitar Feliciana no mapa":"Acompanhar o primeiro fio"))
            {
                if(terminou){var local=LocalPrimeiroFio();if(local!=null)AbrirMapaTecido(local.dados.id);}
                else AcompanharPrimeiroFio();
            }
            if(BotaoTecido(new Rect(432,590,336,28),"Continuar pela campanha",true))
            {AdiarPrimeiroFio();painel="";Avisar(ObjetivoTecido());}

            var escolhido=Array.Find(motivos,m=>m.id==motivoSelecionadoRede);bool aberto=MTMPrimeiroFio.Revelado(estado,escolhido);
            Fundo(new Rect(818,252,400,378),papelUI);
            TextoTecido(new Rect(844,272,348,20),aberto?"UMA HISTÓRIA NA TRAMA":"UM ESPAÇO PARA NOVOS ENCONTROS",11,terracotaUI,true);
            TextoTecido(new Rect(844,310,348,72),aberto?escolhido.titulo:"A rede cresce com a jornada",25,textoPapelUI,true);
            string resumo=aberto?escolhido.resumo:"Converse, observe e participe das atividades da cidade. Os encontros importantes vão preencher estes espaços, sem precisar trocar materiais por pontos.";
            float altura=AlturaTecido(resumo,320,18);
            rolagemDetalhe=IniciarRolagemUI(new Rect(844,394,348,132),rolagemDetalhe,new Rect(0,0,320,Mathf.Max(132,altura)));
            TextoTecido(new Rect(0,0,320,altura),resumo,18,textoPapelUI);TerminarRolagemUI();
            if(BotaoTecido(new Rect(844,574,348,36),aberto?"Ler o registro desse encontro":"Ver minhas missões",true))
            {
                if(aberto){abaCaderno="memorias";registroSelecionadoRoteiro=escolhido.registro;}
                else abaCaderno="missoes";
                rolagemLista=rolagemDetalhe=Vector2.zero;
            }
        }

        private void RedeMemoriasHUD()
        {
            if(aberturaMemorias==null)return;
            Rect r=new Rect(1090,90,166,32);PainelTecido(r,FundoHUDClaroUI,false);
            TextoTecido(new Rect(1098,94,150,24),"Rede · "+MTMPrimeiroFio.Quantidade(estado,aberturaMemorias)+" / "+aberturaMemorias.motivos.Length,13,textoSuperficieUI,true,TextAnchor.MiddleCenter);
            if(AcaoTecido(r,"hud:rede"))AbrirRedeMemorias();
        }

    }
}
