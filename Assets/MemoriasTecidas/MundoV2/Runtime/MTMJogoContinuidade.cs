using System;
using System.Globalization;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private bool retomadaNoPontoSeguro;

        private void RegistrarPontoDaJornada()
        {
            if(atual==null)return;
            Vector3 local=atual.transform.InverseTransformPoint(transform.position);
            estado.temPosicao=true;estado.posicaoX=local.x;estado.posicaoY=local.y;
            estado.missaoAcompanhada=missaoFixadaTecido??"";
            estado.destinoAcompanhado=destinoGuiadoTecido??"";
            estado.salvoEmUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);
        }

        private bool RetomarJornadaSalva()
        {
            if(!possuiJornadaUI)return false;
            var regiao=RegiaoTecido(estado.area);
            if(regiao==null||!RegiaoLiberadaRoteiro(estado.area)){retomadaNoPontoSeguro=true;return false;}
            bool posicaoValida=estado.temPosicao&&CoordenadaSalvaValida(estado.posicaoX,estado.posicaoY,regiao.dados.largura,regiao.dados.altura);
            Vector2 chegada=posicaoValida?(Vector2)regiao.transform.TransformPoint(new Vector3(estado.posicaoX,estado.posicaoY,0)):
                new Vector2(regiao.dados.seguroX,regiao.dados.seguroY);
            if(!IrPara(regiao.dados.id,chegada))return false;
            retomadaNoPontoSeguro=!posicaoValida;
            if(posicaoValida&&!PosicaoRetomadaLivre())
            {Reposicionar(new Vector2(regiao.dados.seguroX,regiao.dados.seguroY));retomadaNoPontoSeguro=true;}
            if(!string.IsNullOrEmpty(estado.missaoAcompanhada)&&MissaoTecido(estado.missaoAcompanhada)!=null)
                missaoFixadaTecido=estado.missaoAcompanhada;
            if(!string.IsNullOrEmpty(estado.destinoAcompanhado)&&estado.destinoAcompanhado!=estado.area&&RegiaoTecido(estado.destinoAcompanhado)!=null)
                destinoGuiadoTecido=estado.destinoAcompanhado;
            return true;
        }

        public static bool CoordenadaSalvaValida(float x,float y,float largura,float altura)
        {
            return !float.IsNaN(x)&&!float.IsNaN(y)&&!float.IsInfinity(x)&&!float.IsInfinity(y)&&
                largura>1f&&altura>1f&&Mathf.Abs(x)<=largura*.5f-.25f&&Mathf.Abs(y)<=altura*.5f-.25f;
        }

        private bool PosicaoRetomadaLivre()
        {
            Physics2D.SyncTransforms();
            foreach(var col in GetComponentsInChildren<Collider2D>())
            {
                if(!col.enabled||col.isTrigger||col.attachedRigidbody!=corpo)continue;
                Bounds limites=col.bounds;
                foreach(var outro in Physics2D.OverlapBoxAll(limites.center,limites.size,0f,Physics2D.GetLayerCollisionMask(col.gameObject.layer)))
                {
                    if(outro==null||outro==col||outro.isTrigger||outro.attachedRigidbody==corpo||Physics2D.GetIgnoreCollision(col,outro))continue;
                    var distancia=col.Distance(outro);
                    if(distancia.isValid&&distancia.isOverlapped&&distancia.distance<-.01f)return false;
                }
            }
            return true;
        }

        private string ResumoJornadaSalvaUI()
        {
            string resumo="Dia "+estado.dia+" · "+atual.dados.nome;
            DateTime data;
            if(DateTime.TryParse(estado.salvoEmUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out data))
                resumo+="\nSalvo em "+data.ToLocalTime().ToString("dd/MM 'às' HH:mm");
            else resumo+="\nSeu progresso está pronto para continuar.";
            return resumo;
        }
    }
}
