using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private string linhaPreparadaDialogo,locutorPreparadoDialogo,nomeExibidoDialogo,textoExibidoDialogo;
        private Texture2D retratoExibidoDialogo;
        private Rect recorteExibidoDialogo;

        private void PrepararFalaLegivelUI()
        {
            string linha=falas[pagina]??"";
            if(linha==linhaPreparadaDialogo&&locutor==locutorPreparadoDialogo)return;
            linhaPreparadaDialogo=linha;locutorPreparadoDialogo=locutor;
            var nomes=new List<string>();
            if(tramaTecida!=null&&tramaTecida.personagens!=null)
                foreach(var pessoa in tramaTecida.personagens)if(pessoa!=null)nomes.Add(pessoa.nome);
            MTMTextoDialogo.SepararFala(linha,locutor,nomes,out nomeExibidoDialogo,out textoExibidoDialogo);
            rolagemDialogo=Vector2.zero;retratoExibidoDialogo=null;recorteExibidoDialogo=new Rect(0,0,1,1);
            if(string.Equals(nomeExibidoDialogo,MTMTextoDialogo.NomeLimpo(locutor),StringComparison.OrdinalIgnoreCase))
            {retratoExibidoDialogo=retratoAtual;recorteExibidoDialogo=recorteAtual;return;}
            if(nomeExibidoDialogo=="Neto")
            {
                var ator=GetComponentInChildren<MTMAtorV22>();
                if(ator!=null&&ator.quadros!=null&&ator.quadros.Length>0&&ator.quadros[0]!=null)
                {
                    var sprite=ator.quadros[0];retratoExibidoDialogo=sprite.texture;Rect r=sprite.rect;
                    recorteExibidoDialogo=new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height);
                }
                return;
            }
            if(regioes==null)return;
            foreach(var regiao in regioes)
            {
                if(regiao==null||regiao.pontos==null)continue;
                foreach(var ponto in regiao.pontos)
                    if(ponto!=null&&ponto.dados!=null&&ponto.retrato!=null&&
                        string.Equals(MTMTextoDialogo.NomeLimpo(ponto.dados.nome),nomeExibidoDialogo,StringComparison.OrdinalIgnoreCase))
                    {retratoExibidoDialogo=ponto.retrato;recorteExibidoDialogo=ponto.recorteRosto;return;}
            }
        }
    }
}
