using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public static class MTMAutoriaRoteiro
    {
        public static string Capitulo(MTMMissaoTecido m)
        {
            if(m==null)return "Jornada concluída";
            if(!string.IsNullOrEmpty(m.capitulo))return m.capitulo;
            return m.numero<=8?"Chegadas e encontros":m.numero<=13?"Saberes do tear":m.numero<=15?"Uma cidade que celebra":"Os fios do mistério";
        }
        public static MTMLugarTecido Lugar(MTMTrama t,string id)
        {return t==null||t.lugares==null?null:Array.Find(t.lugares,l=>l!=null&&l.id==id);}
        public static int Receita(string id)
        {return id=="fio"?3:id=="corda"||id=="pano"||id=="punho"||id=="enfeite"?1:0;}

        private static void VerificarPercurso(MTMPosicaoTecida[] percurso,float dx,float dy,MTMRegiao regiao,string id)
        {
            if(percurso==null)return;
            foreach(var pos in percurso)
                if(Mathf.Abs(pos.x+dx)>=regiao.dados.largura*.5f||Mathf.Abs(pos.y+dy)>=regiao.dados.altura*.5f)
                    throw new InvalidOperationException("O deslocamento deixa o percurso fora da região: "+id+". Aproxime o marco do centro.");
        }
        public static void SincronizarOrigens(MTMTrama trama)
        {
            if(trama==null||trama.itens==null||trama.missoes==null)return;
            // A primeira entrega define a origem. Reposição posterior não muda a dica da bolsa.
            foreach(var item in trama.itens)
            {
                bool encontrou=false;
                foreach(var missao in trama.missoes)
                {
                    foreach(var etapa in missao.etapas)
                        if(etapa.conceder!=null&&Array.Exists(etapa.conceder,q=>q.id==item.id))
                        {item.regiao=etapa.regiao;encontrou=true;break;}
                    if(encontrou)break;
                }
            }
        }
        public static void AplicarMarcos(MTMTrama trama,MTMRegiao[] regioes)
        {
            if(trama==null||trama.pontosNovos==null||regioes==null)return;
            var alteracoes=new Dictionary<string,MTMMarcoRoteiro>(StringComparer.Ordinal);
            var montados=new HashSet<string>();
            foreach(var regiao in regioes)if(regiao!=null)foreach(var p in regiao.GetComponentsInChildren<MTMPonto>(true))
                if(p.dados!=null)montados.Add(p.dados.id);
            foreach(var regiao in regioes)
            {
                if(regiao==null||regiao.dados==null)continue;
                foreach(var marco in regiao.GetComponentsInChildren<MTMMarcoRoteiro>(true))
                {
                    if(MTMVinculosRegiao.RegiaoAncestral(marco.transform)!=regiao)continue;
                    if(string.IsNullOrEmpty(marco.idPonto)||!Array.Exists(trama.pontosNovos,p=>p.id==marco.idPonto))
                        throw new InvalidOperationException("Marco sem ponto conhecido: "+marco.name);
                    if(alteracoes.ContainsKey(marco.idPonto))throw new InvalidOperationException("Marco duplicado: "+marco.idPonto);
                    Vector3 local=regiao.transform.InverseTransformPoint(marco.transform.position);
                    if(Mathf.Abs(local.x)>=regiao.dados.largura*.5f||Mathf.Abs(local.y)>=regiao.dados.altura*.5f)
                        throw new InvalidOperationException("Marco fora dos limites de "+regiao.dados.nome+": "+marco.idPonto);
                    if(montados.Contains(marco.idPonto))throw new InvalidOperationException("O ponto "+marco.idPonto+" já foi montado na cena. Use apenas o marco ou o ponto completo.");
                    var definicao=Array.Find(trama.pontosNovos,p=>p.id==marco.idPonto);
                    float deslocX=local.x-definicao.x,deslocY=local.y-definicao.y;
                    VerificarPercurso(definicao.patrulha,deslocX,deslocY,regiao,marco.idPonto);
                    foreach(var missao in trama.missoes)foreach(var etapa in missao.etapas)
                        if(etapa.ponto==marco.idPonto)VerificarPercurso(etapa.percurso,deslocX,deslocY,regiao,marco.idPonto);
                    alteracoes.Add(marco.idPonto,marco);
                }
            }
            // Nenhum dado é alterado antes de verificar todos os marcos.
            foreach(var par in alteracoes)
            {
                var marco=par.Value;var regiao=MTMVinculosRegiao.RegiaoAncestral(marco.transform);
                var ponto=Array.Find(trama.pontosNovos,p=>p.id==par.Key);
                Vector3 local=regiao.transform.InverseTransformPoint(marco.transform.position);
                float dx=local.x-ponto.x,dy=local.y-ponto.y;
                ponto.regiao=regiao.dados.id;ponto.x=local.x;ponto.y=local.y;
                if(ponto.patrulha!=null)foreach(var pos in ponto.patrulha){pos.x+=dx;pos.y+=dy;}
                foreach(var missao in trama.missoes)foreach(var etapa in missao.etapas)
                    if(etapa.ponto==ponto.id)
                    {
                        etapa.regiao=ponto.regiao;
                        if(etapa.percurso!=null)foreach(var pos in etapa.percurso){pos.x+=dx;pos.y+=dy;}
                    }
                if(trama.personagens!=null)foreach(var pessoa in trama.personagens)
                    if(pessoa.id==ponto.id)pessoa.regiao=ponto.regiao;
                if(trama.pontosNovos!=null)foreach(var modelo in trama.pontosNovos)
                    if(modelo.modeloPonto==ponto.id)modelo.modeloRegiao=ponto.regiao;
                if(trama.proximoCapitulo!=null&&trama.proximoCapitulo.ponto==ponto.id)
                {trama.proximoCapitulo.regiao=ponto.regiao;trama.proximoCapitulo.x=ponto.x;trama.proximoCapitulo.y=ponto.y;}
            }
        }
    }
}
