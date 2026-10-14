using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private GUIStyle tituloUI, textoUI, pequenoUI, centroUI, botaoUI;
        private Vector2 rolagem;
        private bool interfaceVisivel = true;
        private readonly Color tinta = new Color(.07f,.13f,.14f,.97f);
        private readonly Color ouro = new Color(.98f,.79f,.47f);

        private void Estilos()
        {
            if (tituloUI != null) return;
            tituloUI = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, wordWrap = true };
            tituloUI.normal.textColor = ouro;
            textoUI = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true };
            textoUI.normal.textColor = new Color(.98f,.97f,.90f);
            pequenoUI = new GUIStyle(textoUI) { fontSize = 16 };
            centroUI = new GUIStyle(pequenoUI) { alignment = TextAnchor.MiddleCenter };
            botaoUI = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true, padding = new RectOffset(8,8,5,5) };
        }
        private static void Fundo(Rect r, Color c)
        {
            Color anterior = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = anterior;
        }
        private void OnGUI()
        {
            // MTM_VIDA_V22: antes do teste de F1 da versão antiga.
            if (DesenharVidaV22()) return;
            if (!pronto || atual == null || cameraDoMapa == null) return;
            if (!interfaceVisivel) return;
            Estilos(); Matrix4x4 anterior = GUI.matrix; Color corAnterior = GUI.color;
            Rect px = cameraDoMapa.pixelRect;
            GUI.matrix = Matrix4x4.TRS(new Vector3(px.x, Screen.height-px.yMax, 0f), Quaternion.identity, Vector3.one * (px.width/1280f));
            GUI.color = Color.white;
            Fundo(new Rect(18,14,1244,78), tinta);
            int hora = Mathf.FloorToInt(estado.relogio * 24f);
            GUI.Label(new Rect(34,18,920,32), atual.dados.nome, tituloUI);
            GUI.Label(new Rect(1050,24,192,25), "Dia " + estado.dia + "  |  " + hora.ToString("00") + "h", pequenoUI);
            GUI.Label(new Rect(34,54,1190,30), estado.Objetivo(), pequenoUI);

            if (!Bloqueado)
            {
                if (atual.pontos != null) foreach (MTMPonto p in atual.pontos)
                {
                    if (!Disponivel(p)) continue;
                    Vector3 v = cameraDoMapa.WorldToViewportPoint(p.transform.position + Vector3.up * .35f);
                    if(v.z<0f || v.x<0f || v.x>1f || v.y<.08f || v.y>.85f)continue;
                    if (p.dados.tipo == "saida")
                    {
                        float x = Mathf.Clamp(v.x*1280f-95f,20f,1070f), y = (1f-v.y)*720f;
                        Fundo(new Rect(x,y,190,32), new Color(.06f,.16f,.17f,.90f));
                        GUI.Label(new Rect(x,y,190,32), "> " + p.dados.nome, centroUI);
                    }
                    else if (!estado.Tem(p.dados.evento))
                    {
                        GUI.Label(new Rect(v.x*1280f-12f,(1f-v.y)*720f-58f,30,36), p.dados.tipo == "coleta" ? "+" : "?", tituloUI);
                    }
                }
                if (perto != null)
                {
                    Fundo(new Rect(320,600,640,42), tinta);
                    GUI.Label(new Rect(330,604,620,34), "E  ·  " + perto.dados.nome, centroUI);
                }
                if (avisoAte > Time.unscaledTime)
                {
                    Fundo(new Rect(100,104,1080,58), tinta);
                    GUI.Label(new Rect(116,112,1048,45), aviso, pequenoUI);
                }
            }
            Fundo(new Rect(18,677,1244,29), tinta);
            GUI.Label(new Rect(30,680,1220,26), "WASD/setas: andar   Shift: correr   E: interagir   M: mapa   I: bolsa   J: diário   Esc: fechar   R: caminho   Q: efeitos", centroUI);
            if (falas != null) DialogoUI();
            else if (painel.Length > 0) PainelUI();
            GUI.matrix = anterior; GUI.color = corAnterior;
        }

        private void DialogoUI()
        {
            Fundo(new Rect(44,421,1192,243), tinta);
            Fundo(new Rect(44,421,6,243), ouro);
            float inicioTexto=retratoAtual!=null?246f:68f;
            if(retratoAtual!=null)
            {
                Fundo(new Rect(64,443,158,171),new Color(.26f,.34f,.30f));
                GUI.DrawTextureWithTexCoords(new Rect(70,446,146,165),retratoAtual,recorteAtual,true);
            }
            GUI.Label(new Rect(inicioTexto,436,1188-inicioTexto,40), locutor, tituloUI);
            GUI.Label(new Rect(inicioTexto,479,1188-inicioTexto,96), falas[pagina], textoUI);
            GUI.Label(new Rect(inicioTexto,576,1188-inicioTexto,36), contexto ?? "", pequenoUI);
            GUI.Label(new Rect(68,621,770,28), "E/Enter: " + (pagina+1 == falas.Length ? "concluir" : "continuar") + "   |   Esc: sair   |   " + (pagina+1) + "/" + falas.Length, pequenoUI);
            if (GUI.Button(new Rect(975,612,232,36), pagina+1 == falas.Length ? "Concluir" : "Continuar", botaoUI)) Avancar();
        }
        private void PainelUI()
        {
            Fundo(new Rect(44,108,1192,556), tinta);
            string titulo = painel == "mapa" ? "Caminhos de Jaguaruana" : painel == "bolsa" ? "Sua bolsa" : "Diário de memórias";
            GUI.Label(new Rect(70,126,1040,42), titulo, tituloUI);
            if (GUI.Button(new Rect(1104,126,104,36), "Fechar", botaoUI)) { painel = ""; return; }
            if (painel == "mapa") MapaUI();
            else if (painel == "bolsa") BolsaUI();
            else DiarioUI();
        }
        private Vector2 CentroDoMapa(MTMRegiao r) { return new Vector2(180+r.dados.mapaX*285, 248+r.dados.mapaY*86); }
        private static void Linha(Vector2 a, Vector2 b, Color cor)
        {
            Matrix4x4 matriz = GUI.matrix; Color velha = GUI.color;
            Vector2 delta = b-a; GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            GUI.color = cor; GUI.DrawTexture(new Rect(a.x,a.y-1.5f,delta.magnitude,3f),Texture2D.whiteTexture);
            GUI.matrix = matriz; GUI.color = velha;
        }
        private void MapaUI()
        {
            GUI.Label(new Rect(70,174,1110,32), "Clique em um lugar para ver o caminho. A viagem acontece pelas saídas do cenário.", pequenoUI);
            foreach (MTMRegiao r in regioes) foreach (MTMPonto p in r.pontos)
            {
                if (p.dados.tipo != "saida") continue;
                MTMRegiao d = Array.Find(regioes,x=>x.dados.id == p.dados.destino);
                if (d != null && string.CompareOrdinal(r.dados.id,d.dados.id)<0) Linha(CentroDoMapa(r),CentroDoMapa(d),new Color(.38f,.49f,.46f));
            }
            foreach (MTMRegiao r in regioes)
            {
                Vector2 c = CentroDoMapa(r);
                Color velha = GUI.backgroundColor;
                GUI.backgroundColor = r == atual ? ouro : estado.Tem("visitou_"+r.dados.id) ? new Color(.65f,.84f,.73f) : Color.white;
                if (GUI.Button(new Rect(c.x-108,c.y-26,216,52), r.dados.nome + (r==atual ? "\nVocê está aqui" : ""), botaoUI)) destinoMapa = r.dados.id;
                GUI.backgroundColor = velha;
            }
            GUI.Label(new Rect(72,571,1120,73), string.IsNullOrEmpty(destinoMapa) ? "Mapa de jogo: posições e distâncias adaptadas. Visite lugares e registre encontros no diário." : Rota(destinoMapa), textoUI);
        }
        private string Rota(string destino)
        {
            var anteriores = new Dictionary<string,string>(); var fila = new Queue<string>();
            fila.Enqueue(atual.dados.id); anteriores[atual.dados.id] = null;
            while (fila.Count > 0)
            {
                string id = fila.Dequeue(); if (id == destino) break;
                MTMRegiao r = Array.Find(regioes,x=>x.dados.id==id);
                foreach (MTMPonto p in r.pontos)
                    if (p.dados.tipo == "saida" && !anteriores.ContainsKey(p.dados.destino)) { anteriores[p.dados.destino]=id; fila.Enqueue(p.dados.destino); }
            }
            if (!anteriores.ContainsKey(destino)) return "Ainda não existe uma ligação para este lugar.";
            var nomes = new List<string>();
            for (string id=destino; id!=null; id=anteriores[id]) nomes.Add(Array.Find(regioes,x=>x.dados.id==id).dados.nome);
            nomes.Reverse(); return "Caminho: " + string.Join(" > ",nomes);
        }
        private void BolsaUI()
        {
            string[] nomes = { "Algodão", "Fios", "Corda", "Pano", "Punhos", "Enfeite", "Cera", "Rede" };
            int[] valores = { estado.algodao, estado.fio, estado.corda, estado.pano, estado.punho, estado.enfeite, estado.cera, estado.rede ? 1 : 0 };
            for (int i=0;i<nomes.Length;i++)
            {
                Rect r = new Rect(76+(i%4)*281,208+(i/4)*98,260,77);
                Fundo(r,new Color(.16f,.24f,.24f)); GUI.Label(new Rect(r.x+15,r.y+10,225,56),nomes[i]+"\n"+valores[i],textoUI);
            }
            GUI.Label(new Rect(76,441,1100,91), "Na oficina: 6 algodões viram 3 fios.\nReceita da rede: 3 fios + 1 corda + 1 pano + 1 conjunto de punhos + 1 enfeite.", textoUI);
            GUI.Label(new Rect(76,551,1100,75), "A cera é uma lembrança da visita ao memorial. Esta receita é uma adaptação didática; a fabricação real tem outras etapas e saberes.", pequenoUI);
        }
        private void DiarioUI()
        {
            GUI.Label(new Rect(76,179,1090,57), estado.Objetivo(), textoUI);
            var entradas = new List<string>();
            foreach (MTMRegiao r in regioes) foreach (MTMPonto p in r.pontos)
                if (p.dados.tipo != "saida" && !string.IsNullOrEmpty(p.dados.evento) && estado.Tem(p.dados.evento)) entradas.Add(p.dados.nome + "  ·  " + r.dados.nome);
            Rect tela = new Rect(76,254,1120,275);
            rolagem = GUI.BeginScrollView(tela,rolagem,new Rect(0,0,1080,Mathf.Max(275,entradas.Count*33)));
            for(int i=0;i<entradas.Count;i++) GUI.Label(new Rect(8,i*33,1060,30),entradas[i],pequenoUI);
            if (entradas.Count==0) GUI.Label(new Rect(8,0,1060,55),"Suas primeiras memórias aparecem aqui após concluir as conversas.",textoUI);
            GUI.EndScrollView();
            GUI.Label(new Rect(76,543,900,67), "Encontros de memória aproximam épocas diferentes. Falas e aparências são adaptações artísticas, com fontes registradas na documentação.", pequenoUI);
            if (!confirmarNovo)
            {
                if (GUI.Button(new Rect(930,613,266,33),"Iniciar nova jornada...",botaoUI)) confirmarNovo = true;
            }
            else
            {
                GUI.Label(new Rect(76,614,800,32),"Substituir apenas o progresso deste Mundo V2?",pequenoUI);
                if (GUI.Button(new Rect(930,613,266,33),"Sim, recomeçar",botaoUI)) NovaJornada();
            }
        }
    }
}
