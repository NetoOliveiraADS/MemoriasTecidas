using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Marca no chão das passagens entre lugares. Com a arte do guia (Resources/MemoriasTecidas/Interface/Guia),
    // é o mesmo anel costurado do objetivo, com duas setas bordadas apontando para o lado da passagem.
    // Sem a arte, volta ao anel de linha antigo. Entrar no anel já leva ao outro lugar (MTMJogo.SaidaAutomatica).
    [DisallowMultipleComponent]
    public sealed class MTMMarcadorSaidaV221 : MonoBehaviour
    {
        private const string PastaArte = "MemoriasTecidas/Interface/Guia/";
        // Largura do anel no chão; a altura é a metade (o chão visto de cima). É também a área que troca de lugar.
        public const float DiametroAnel = 2.8f;
        private const float Diametro = DiametroAnel;

        public Material material;
        public MTMVidaV22 vida;
        private LineRenderer linha;
        private GameObject marca;
        private Transform giro;
        private SpriteRenderer anel, seta;

        private void Start()
        {
            var spriteAnel = Resources.Load<Sprite>(PastaArte + "anel_objetivo");
            if (spriteAnel != null)
                CriarMarcaCosturada(spriteAnel, Resources.Load<Sprite>(PastaArte + "seta_saida"));
            else
                CriarLinha();
        }

        private void CriarMarcaCosturada(Sprite spriteAnel, Sprite spriteSeta)
        {
            marca = new GameObject("Passagem_No_Chao_V221") { layer = gameObject.layer };
            marca.transform.SetParent(transform, false);
            // Achatado como o chão visto de cima; a escala do objeto da saída não muda o tamanho do anel.
            float s = Diametro / Mathf.Max(.01f, spriteAnel.bounds.size.x);
            Vector3 escala = transform.lossyScale;
            marca.transform.localScale = new Vector3(s / Mathf.Max(.001f, Mathf.Abs(escala.x)), s * .5f / Mathf.Max(.001f, Mathf.Abs(escala.y)), 1f);

            var objetoAnel = new GameObject("Anel") { layer = gameObject.layer };
            objetoAnel.transform.SetParent(marca.transform, false);
            anel = objetoAnel.AddComponent<SpriteRenderer>();
            anel.sprite = spriteAnel;
            // Como o anel do objetivo: pintado no chão, abaixo das sombras e de tudo o que fica em pé.
            anel.sortingOrder = -22490;
            giro = objetoAnel.transform;

            if (spriteSeta == null)
                return;
            var objetoSeta = new GameObject("Setas") { layer = gameObject.layer };
            objetoSeta.transform.SetParent(marca.transform, false);
            seta = objetoSeta.AddComponent<SpriteRenderer>();
            seta.sprite = spriteSeta;
            seta.sortingOrder = -22489;
            Vector2 d = DirecaoSaida();
            objetoSeta.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            // As setas ocupam pouco menos da metade do anel.
            objetoSeta.transform.localScale = Vector3.one * (.42f * spriteAnel.bounds.size.x / Mathf.Max(.01f, spriteSeta.bounds.size.x));
        }

        private void CriarLinha()
        {
            var go = new GameObject("Passagem_No_Chao_V221");
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;
            linha = go.AddComponent<LineRenderer>();
            linha.sharedMaterial = material;
            linha.useWorldSpace = false;
            linha.loop = true;
            linha.positionCount = 32;
            linha.widthMultiplier = .035f;
            linha.sortingOrder = -5000;
            linha.startColor = linha.endColor = new Color(1f, .76f, .30f, .9f);
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32;
                linha.SetPosition(i, new Vector3(Mathf.Cos(a) * DiametroAnel * .5f, Mathf.Sin(a) * DiametroAnel * .25f, 0));
            }
        }

        // Para onde a passagem leva: para fora, pela borda mais próxima do lugar; longe das bordas (uma porta), para cima.
        private Vector2 DirecaoSaida()
        {
            var regiao = MTMVinculosRegiao.RegiaoAncestral(transform);
            if (regiao == null || regiao.dados == null)
                return Vector2.up;
            Vector3 local = regiao.transform.InverseTransformPoint(transform.position);
            float folgaX = regiao.dados.largura * .5f - Mathf.Abs(local.x);
            float folgaY = regiao.dados.altura * .5f - Mathf.Abs(local.y);
            if (Mathf.Min(folgaX, folgaY) > 5f)
                return Vector2.up;
            return folgaX < folgaY ? new Vector2(Mathf.Sign(local.x), 0f) : new Vector2(0f, Mathf.Sign(local.y));
        }

        private void LateUpdate()
        {
            if (linha == null && marca == null)
                return;
            // Só de perto: a seta e o losango já guiam de longe; o anel diz "a passagem é aqui".
            var jogo = vida != null ? vida.jogo : null;
            bool ligado = vida != null && vida.mostrarInterface && (jogo == null || !jogo.V22Bloqueado);
            float distancia = jogo != null ? Vector2.Distance(jogo.transform.position, transform.position) : 0f;
            float perto = Mathf.Clamp01((6f - distancia) / 2.5f);
            // Quando o anel do objetivo já está nesta passagem, ele basta.
            if (jogo != null && jogo.V22AnelGuiaEm(transform.position))
                perto = 0f;
            bool mostrar = ligado && perto > 0f;
            float pulso = .55f + .45f * Mathf.Sin(Time.unscaledTime * 4f);

            if (linha != null)
            {
                linha.enabled = mostrar;
                if (mostrar)
                    linha.startColor = linha.endColor = new Color(1f, .76f, .30f, .9f * perto * pulso);
                return;
            }

            if (marca.activeSelf != mostrar)
                marca.SetActive(mostrar);
            if (!mostrar)
                return;
            var cor = new Color(1f, 1f, 1f, perto * (.75f + .25f * pulso));
            anel.color = cor;
            if (seta != null)
                seta.color = cor;
            if (jogo == null || !jogo.V22ReduzirMovimento)
                giro.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 18f);
        }
    }
}
