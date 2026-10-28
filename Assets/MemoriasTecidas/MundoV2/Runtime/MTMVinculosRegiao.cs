using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // A hierarquia da cena define a região atual de cada interação.
    // As listas serializadas podem ficar antigas quando um NPC é movido no Editor.
    public static class MTMVinculosRegiao
    {
        public static MTMRegiao RegiaoAncestral(Transform objeto)
        {
            for(var t=objeto;t!=null;t=t.parent)
            {
                var regiao=t.GetComponent<MTMRegiao>();
                if(regiao!=null)return regiao;
            }
            return null;
        }

        public static Dictionary<string,MTMRegiao> Reindexar(MTMRegiao[] regioes)
        {
            var locais=new Dictionary<string,MTMRegiao>(StringComparer.Ordinal);
            var listas=new Dictionary<MTMRegiao,MTMPonto[]>();
            if(regioes==null)return locais;
            foreach(var regiao in regioes)
            {
                if(regiao==null||regiao.dados==null)continue;
                if(listas.ContainsKey(regiao))throw new InvalidOperationException("A mesma região está cadastrada duas vezes no jogo.");
                var pontos=new List<MTMPonto>();
                // Inclui regiões desativadas: elas serão ativadas quando o jogador entrar.
                foreach(var ponto in regiao.GetComponentsInChildren<MTMPonto>(true))
                {
                    if(RegiaoAncestral(ponto.transform)!=regiao||ponto.dados==null||string.IsNullOrEmpty(ponto.dados.id))continue;
                    if(locais.ContainsKey(ponto.dados.id))
                        throw new InvalidOperationException("ID de interação repetido: "+ponto.dados.id+". Mova o personagem existente, preservando uma única interação com esse ID.");
                    locais.Add(ponto.dados.id,regiao);pontos.Add(ponto);
                }
                listas.Add(regiao,pontos.ToArray());
            }
            // Atualiza só depois de verificar todas as regiões e os IDs.
            foreach(var par in listas)par.Key.pontos=par.Value;
            return locais;
        }

        public static MTMRegiao RegiaoDoPonto(MTMRegiao[] regioes,string id)
        {
            if(regioes!=null)foreach(var regiao in regioes)
                if(regiao!=null&&regiao.pontos!=null)foreach(var ponto in regiao.pontos)
                    if(ponto!=null&&ponto.dados!=null&&ponto.dados.id==id)return regiao;
            return null;
        }

        public static void SincronizarRoteiro(MTMTrama trama,Dictionary<string,MTMRegiao> locais)
        {
            if(trama==null||locais==null)return;
            MTMRegiao regiao;
            if(trama.missoes!=null)foreach(var missao in trama.missoes)
                if(missao!=null&&missao.etapas!=null)foreach(var etapa in missao.etapas)
                    if(etapa!=null&&!string.IsNullOrEmpty(etapa.ponto)&&locais.TryGetValue(etapa.ponto,out regiao))
                        etapa.regiao=regiao.dados.id;
            if(trama.personagens!=null)foreach(var pessoa in trama.personagens)
                if(pessoa!=null&&!string.IsNullOrEmpty(pessoa.id)&&locais.TryGetValue(pessoa.id,out regiao))
                    pessoa.regiao=regiao.dados.id;
            if(trama.pontosNovos!=null)foreach(var ponto in trama.pontosNovos)
                if(ponto!=null&&!string.IsNullOrEmpty(ponto.modeloPonto)&&locais.TryGetValue(ponto.modeloPonto,out regiao))
                    ponto.modeloRegiao=regiao.dados.id;

            if(locais.TryGetValue("npc_feliciana",out regiao)&&regiao.dados.id=="entrada")
            {
                var inicio=trama.missoes==null?null:Array.Find(trama.missoes,m=>m!=null&&m.id=="m01");
                if(inicio!=null)inicio.descricao="Dona Feliciana recebe você na Entrada da Cidade e apresenta a jornada. Depois, procure Simão de Góis na Praça Matriz.";
                var pessoa=trama.personagens==null?null:Array.Find(trama.personagens,p=>p!=null&&p.id=="npc_feliciana");
                if(pessoa!=null)pessoa.descricao="Recebe o jogador na Entrada da Cidade e convida a conhecer as memórias de Jaguaruana. Representação artística com falas dramatizadas.";
                if(trama.dialogos!=null)foreach(var dialogo in trama.dialogos)
                    if(dialogo!=null&&dialogo.id=="npc_neto"&&dialogo.falas!=null)
                        for(int i=0;i<dialogo.falas.Length;i++)
                            if(dialogo.falas[i]!=null&&dialogo.falas[i].Contains("Feliciana"))
                                dialogo.falas[i]=dialogo.falas[i].Replace("Praça Matriz","Entrada da Cidade");
            }
        }
    }
}
