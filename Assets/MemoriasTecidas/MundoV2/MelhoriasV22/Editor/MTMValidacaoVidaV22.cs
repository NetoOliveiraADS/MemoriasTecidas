using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoriasTecidas.MundoV2.Editor
{
    public static class MTMValidacaoVidaV22
    {
        [MenuItem("Memorias Tecidas/Vida V2.2/4 - Verificar cena (sem alterar)")]
        public static void Verificar()
        {
            var erros=new List<string>();var avisos=new List<string>();MTMJogo jogo=null;
            foreach(var j in UnityEngine.Object.FindObjectsByType<MTMJogo>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(j.gameObject.scene==SceneManager.GetActiveScene())jogo=j;
            if(jogo==null){Debug.LogError("Vida V2.2: abra a cena do jogo antes de verificar.");return;}
            var vida=jogo.GetComponent<MTMVidaV22>();
            if(vida==null||vida.versaoInstalada<22)erros.Add("Instalação da cena ausente ou incompleta.");
            if(jogo.cameraDoMapa==null)erros.Add("Câmera não atribuída ao MTMJogo.");
            string path="Assets/MemoriasTecidas/MundoV2/Runtime/MTMJogoInterface.cs";
            if(!File.Exists(path)||!File.ReadAllText(path).Contains("if (DesenharVidaV22()) return;"))erros.Add("Falta a conexão da interface: execute o passo 1.");
            var player=jogo.GetComponentInChildren<MTMAtorV22>(true);
            if(player==null||player.quadros==null||player.quadros.Length!=32)erros.Add("Jogador precisa de 32 quadros / oito direções.");
            var antigo=jogo.GetComponentInChildren<MTMAnimacao>(true);
            if(antigo!=null&&antigo.enabled)erros.Add("MTMAnimacao antiga ainda habilitada: duas animações disputam o jogador.");
            bool reparado=vida!=null&&vida.versaoInstalada>=221;
            if(!reparado)avisos.Add("A correção V2.2.1 ainda não foi aplicada: execute o passo 5.");
            else
            {
                if(player!=null&&(!player.enabled||!player.cicloCompleto))erros.Add("MTMAtorV22 precisa estar ativo com Ciclo Completo marcado.");
                if(player!=null&&player.quadros!=null)foreach(var q in player.quadros)
                    if(q==null||!q.name.StartsWith("Jogador_221_",StringComparison.Ordinal)){erros.Add("Jogador ainda usa quadros anteriores ou vazios; reaplique o passo 5.");break;}
                var vento=jogo.GetComponent<MTMVisualV2>();if(vento!=null&&Mathf.Abs(vento.vento)>.0001f)avisos.Add("Vento do corpo do jogador deve ficar em zero. Regule Vento No Cabelo no Desenho.");
                var deformacao=jogo.GetComponentInChildren<MTMDeformacaoV22>(true);
                if(deformacao==null||!deformacao.fixarCorpo)erros.Add("Falta a proteção do corpo contra a rotação de vento.");
                var travessia=jogo.GetComponent<MTMTravessiaV221>();
                if(travessia==null||travessia.jogo!=jogo)erros.Add("Componente de travessia ausente ou sem referência ao jogo.");
            }
            int moradores=0,animais=0,andantes=0,saidas=0;
            if(jogo.regioes==null)erros.Add("Lista de regiões ausente.");
            else foreach(var r in jogo.regioes)
            {
                if(r==null||r.dados==null){erros.Add("Referência de região ausente.");continue;}
                if(vida!=null&&vida.Imagem(r.dados.id)==null)avisos.Add("Cartão sem imagem: "+r.dados.nome);
                foreach(var a in r.GetComponentsInChildren<MTMAtorV22>(true))
                {
                    if(a.tipo==MTMAtorV22.Tipo.Morador)moradores++;if(a.tipo==MTMAtorV22.Tipo.Animal)animais++;
                    if(a.quadros==null||a.quadros.Length==0)erros.Add("Ator sem quadros: "+a.transform.parent.name);
                    else foreach(var q in a.quadros)if(q==null)erros.Add("Quadro vazio: "+a.transform.parent.name);
                }
                foreach(var c in r.GetComponentsInChildren<MTMCaminhanteV22>(true))
                {andantes++;if(c.pontos==null||c.pontos.Length<2)erros.Add("Rota incompleta: "+c.name);}
                if(r.pontos==null){erros.Add("Pontos ausentes: "+r.dados.id);continue;}
                foreach(var p in r.pontos)
                {
                    if(p==null||p.dados==null){erros.Add("Ponto ausente: "+r.dados.id);continue;}
                    if(p.dados.tipo!="saida")continue;saidas++;
                    if(reparado&&(p.GetComponent<MTMMarcadorSaidaV221>()==null||p.alcance<2.2f))avisos.Add("Saída sem marcador ou com alcance curto: "+p.dados.id);
                    var alvo=Array.Find(jogo.regioes,x=>x!=null&&x.dados.id==p.dados.destino);
                    if(alvo==null)erros.Add("Destino inválido: "+p.dados.id);
                    else if(Mathf.Abs(p.dados.chegadaX)>alvo.dados.largura*.5f-.4f||Mathf.Abs(p.dados.chegadaY)>alvo.dados.altura*.5f-.4f)
                        erros.Add("Chegada fora dos limites: "+p.dados.id);
                }
            }
            foreach(string e in erros)Debug.LogError("Vida V2.2: "+e,jogo);
            foreach(string a in avisos)Debug.LogWarning("Vida V2.2: "+a,jogo);
            Debug.Log("Verificação de referências: "+erros.Count+" erros, "+avisos.Count+" avisos; "+moradores+" moradores, "+animais+" animais, "+andantes+" rotas, "+saidas+" saídas. Esta verificação NÃO substitui Play, teste das colisões, desempenho e build.",jogo);
        }
    }
}
