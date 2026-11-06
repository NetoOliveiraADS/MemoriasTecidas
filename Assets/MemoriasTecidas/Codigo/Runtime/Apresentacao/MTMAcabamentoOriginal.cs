using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    [Serializable]
    public sealed class MTMAcabamentoOriginalDados
    {
        public bool ativo = true;
        public float nitidez = .32f, relevo = .22f;
        public bool natural = true;
        public float luzTextura = .35f;
        public MTMPerfilPersonagens personagens = new MTMPerfilPersonagens();
    }

    // Apenas parâmetros de desenho: não troca imagens, sprites, materiais ou geometria.
    public sealed class MTMAcabamentoOriginal : IDisposable
    {
        public const string Recurso = "MemoriasTecidas/acabamento_original";
        private readonly List<Action> restaurar = new List<Action>();
        private Vector4 globalAnterior;
        private Vector4 refinoAnterior;
        private Vector4 pessoasAnterior;
        private MTMAcabamentoOriginalDados configuracao;
        public bool Natural { get; private set; }

        private bool aplicado;
        public int Objetos { get; private set; }
        public int Superficies { get; private set; }

        public static MTMAcabamentoOriginalDados LerConfiguracao()
        {
            var texto = Resources.Load<TextAsset>(Recurso);
            if (texto == null)
                throw new InvalidOperationException("acabamento_original.json ausente.");
            var d = JsonUtility.FromJson<MTMAcabamentoOriginalDados>(texto.text);
            if (d == null || !Valido(d.nitidez) || !Valido(d.relevo) || !Valido(d.luzTextura))
                throw new InvalidOperationException("Nitidez, relevo e luzTextura devem ser números entre 0 e 1.");
            if (d.personagens == null) d.personagens = new MTMPerfilPersonagens();
            d.personagens.Validar();
            return d;
        }

        private static bool Valido(float v)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0f && v <= 1f;
        }

        public void Aplicar(MTMJogo jogo)
        {
            if (aplicado)
                return;
            var dados = LerConfiguracao();
            configuracao = dados;
            refinoAnterior = Shader.GetGlobalVector("_MTM_RefinoOriginal");
            pessoasAnterior = Shader.GetGlobalVector("_MTM_PerfilPersonagens");
            globalAnterior = Shader.GetGlobalVector("_MTM_DetalheOriginal");
            aplicado = true;
            DefinirNatural(PlayerPrefs.GetInt("MemoriasTecidas.Visual.AcabamentoNatural", dados.natural ? 1 : 0) == 1, false);
            Shader.SetGlobalVector("_MTM_DetalheOriginal", dados.ativo ? new Vector4(dados.nitidez, dados.relevo, 1f, 0f) : Vector4.zero);
            if (!dados.ativo)
                return;
            var vistos = new HashSet<int>();
            try
            {
                if (jogo.regioes != null)
                    foreach (var regiao in jogo.regioes)
                    {
                        if (regiao == null)
                            continue;
                        foreach (var sr in regiao.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            if (!vistos.Add(sr.GetInstanceID()) || sr.sprite == null || sr.sharedMaterial == null || !sr.sharedMaterial.HasProperty("_MTM_ArteOriginal") || sr.name.IndexOf("sombra", StringComparison.OrdinalIgnoreCase) >= 0)
                                continue;
                            var visual = sr.GetComponentInParent<MTMVisualV2>();
                            if (visual != null && (visual.jogador || visual.sombra == sr))
                                continue;
                            if (sr.GetComponentInParent<MTMAtorV22>() != null || sr.GetComponentInParent<MTMCaminhanteV22>() != null)
                                continue;
                            var uv = sr.sprite.uv;
                            if (uv == null || uv.Length == 0)
                                continue;
                            Vector2 minimo = Vector2.one, maximo = Vector2.zero;
                            foreach (var v in uv)
                            {
                                minimo = Vector2.Min(minimo, v);
                                maximo = Vector2.Max(maximo, v);
                            }

                            var antes = new MaterialPropertyBlock();
                            sr.GetPropertyBlock(antes);
                            restaurar.Add(() =>
                            {
                                if (sr != null)
                                    sr.SetPropertyBlock(antes);
                            });
                            var bloco = new MaterialPropertyBlock();
                            sr.GetPropertyBlock(bloco);
                            bloco.SetFloat("_MTM_ArteOriginal", 1f);
                            bloco.SetVector("_MTM_RecorteOriginal", new Vector4(minimo.x, minimo.y, maximo.x, maximo.y));
                            sr.SetPropertyBlock(bloco);
                            Objetos++;
                        }

                        foreach (var superficie in regiao.GetComponentsInChildren<MTMSuperficieV23>(true))
                            if (superficie.textura != null)
                                Superficies++;
                    }

                Debug.Log("Acabamento original: " + Objetos + " objetos e " + Superficies + " superfícies com textura. Artes, câmera e geometria originais mantidas.", jogo);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void DefinirNatural(bool valor, bool salvar = true)
        {
            Natural = valor;
            Shader.SetGlobalVector("_MTM_RefinoOriginal", configuracao != null && configuracao.ativo && valor ? new Vector4(1f, configuracao.luzTextura, 0f, 0f) : Vector4.zero);
            var pessoas = configuracao != null ? configuracao.personagens : null;
            Shader.SetGlobalVector("_MTM_PerfilPersonagens",
                configuracao != null && configuracao.ativo && valor && pessoas != null && pessoas.ativo ?
                new Vector4(1f, pessoas.saturacao, pessoas.suavizacao, pessoas.luzPreenchimento) : Vector4.zero);
            if (salvar)
            {
                PlayerPrefs.SetInt("MemoriasTecidas.Visual.AcabamentoNatural", valor ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void Dispose()
        {
            for (int i = restaurar.Count - 1; i >= 0; i--)
                restaurar[i]();
            restaurar.Clear();
            if (aplicado)
                Shader.SetGlobalVector("_MTM_DetalheOriginal", globalAnterior);
            if (aplicado)
                Shader.SetGlobalVector("_MTM_RefinoOriginal", refinoAnterior);
            if (aplicado)
                Shader.SetGlobalVector("_MTM_PerfilPersonagens", pessoasAnterior);
            aplicado = false;
            Objetos = Superficies = 0;
        }
    }
}
