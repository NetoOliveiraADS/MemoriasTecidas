using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Resolve o piso sob os pés somente quando necessário. Não acrescenta colisores.
    public sealed class MTMDetectorSolo
    {
        private MTMRegiao regiao;
        private MTMSuperficieV23[] superficies;
        private float[] pesosSuperficies, pesosPisos;
        private readonly List<SpriteRenderer> pontes = new List<SpriteRenderer>();
        private float pesoBase;
        private void Preparar(MTMRegiao atual)
        {
            regiao = atual;
            pontes.Clear();
            superficies = atual.GetComponentsInChildren<MTMSuperficieV23>(true);
            pesosSuperficies = new float[superficies.Length];
            for (int i = 0; i < superficies.Length; i++)
            {
                var s = superficies[i];
                pesosSuperficies[i] = s.agua ? 0 : MTMCalculosMovimento.PoeiraDoPiso(s.textura != null ? s.textura.name : "");
            }

            var dados = atual.dados;
            var pisos = dados.pisos;
            pesoBase = MTMCalculosMovimento.PoeiraDoPiso(dados.piso);
            pesosPisos = new float[pisos == null ? 0 : pisos.Length];
            for (int i = 0; i < pesosPisos.Length; i++)
                pesosPisos[i] = pisos[i] != null ? MTMCalculosMovimento.PoeiraDoPiso(pisos[i].arte) : 0;
            foreach (var sr in atual.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.sprite != null && sr.gameObject.name != "Sombra" && sr.sprite.name.ToLowerInvariant().Contains("ponte"))
                    pontes.Add(sr);
        }

        public float Peso(MTMRegiao atual, Vector3 mundo)
        {
            if (atual == null || atual.dados == null || atual.dados.interior)
                return 0;
            if (regiao != atual || superficies == null)
                Preparar(atual);
            foreach (var ponte in pontes)
            {
                if (ponte == null || !ponte.gameObject.activeInHierarchy)
                    continue;
                var b = ponte.bounds;
                if (mundo.x >= b.min.x && mundo.x <= b.max.x && mundo.y >= b.min.y && mundo.y <= b.max.y)
                    return 0;
            }

            Vector2 local = atual.transform.InverseTransformPoint(mundo);
            float peso = pesoBase;
            int ordem = int.MinValue;
            var pisos = atual.dados.pisos;
            for (int i = 0; i < pesosPisos.Length && pisos != null && i < pisos.Length; i++)
            {
                var p = pisos[i];
                if (p == null || p.ordem < ordem)
                    continue;
                if (Mathf.Abs(local.x - p.x) <= Mathf.Abs(p.largura) * .5f && Mathf.Abs(local.y - p.y) <= Mathf.Abs(p.altura) * .5f)
                {
                    peso = pesosPisos[i];
                    ordem = p.ordem;
                }
            }

            for (int i = 0; i < superficies.Length; i++)
            {
                var s = superficies[i];
                if (s == null || !s.isActiveAndEnabled || s.ordem < ordem)
                    continue;
                Vector2 p = s.transform.InverseTransformPoint(mundo);
                bool dentro = s.contorno != null && s.contorno.Length >= 3 ? MTMCalculosMovimento.DentroPoligono(p, s.contorno) : Mathf.Abs(p.x) <= Mathf.Abs(s.tamanho.x) * .5f && Mathf.Abs(p.y) <= Mathf.Abs(s.tamanho.y) * .5f;
                if (dentro)
                {
                    peso = pesosSuperficies[i];
                    ordem = s.ordem;
                }
            }

            return peso;
        }
    }
}
