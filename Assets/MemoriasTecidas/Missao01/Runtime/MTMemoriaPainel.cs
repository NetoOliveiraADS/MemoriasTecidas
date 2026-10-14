using UnityEngine;
using UnityEngine.UI;

namespace MemoriasTecidas.Missao01
{
    [DisallowMultipleComponent]
    public sealed class MTMemoriaPainel : MonoBehaviour
    {
        public Camera cameraDoMapa;
        public RectTransform areaDaCamera, conteudo, marcador;
        public GameObject caixaConversa, caixaAviso, caixaDica;
        public Text objetivo, etapa, dica, nome, contexto, fala, fonte, contador, aviso, nomeMarcador;
        private MTMemoriaDialogo atual;
        private int indice;
        private MTMemoriaNPC alvo;
        private float avisoAte;

        public bool Aberto => atual != null;

        public bool Abrir(MTMemoriaDialogo dialogo)
        {
            if (dialogo == null || !dialogo.Valido) return false;
            atual = dialogo;
            indice = 0;
            nome.text = dialogo.personagem;
            contexto.text = dialogo.contexto;
            fonte.text = dialogo.fonte;
            caixaConversa.SetActive(true);
            MostrarDica(null);
            AtualizarFala();
            return true;
        }

        // true somente após confirmar a última página. Abrir e fechar não concluem.
        public bool Avancar()
        {
            if (!Aberto) return false;
            indice++;
            if (indice >= atual.falas.Length) { Fechar(); return true; }
            AtualizarFala();
            return false;
        }

        public void Fechar()
        {
            atual = null;
            indice = 0;
            if (caixaConversa != null) caixaConversa.SetActive(false);
        }

        private void AtualizarFala()
        {
            fala.text = atual.falas[indice];
            contador.text = (indice + 1) + " / " + atual.falas.Length
                + (indice == atual.falas.Length - 1 ? "   E / Enter: concluir" : "   E / Enter: continuar")
                + "   |   Esc: sair";
        }

        public void MostrarDica(string texto)
        {
            caixaDica.SetActive(!string.IsNullOrEmpty(texto));
            dica.text = texto ?? "";
        }

        public void MostrarObjetivo(string texto, string progresso, MTMemoriaNPC destino)
        {
            objetivo.text = texto;
            etapa.text = progresso;
            alvo = destino;
        }

        public void Notificar(string texto)
        {
            aviso.text = texto;
            avisoAte = Time.unscaledTime + 4f;
            caixaAviso.SetActive(true);
        }

        public void ReiniciarVisual()
        {
            Fechar();
            MostrarDica(null);
            caixaAviso.SetActive(false);
            marcador.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (cameraDoMapa == null) return;
            // Interface acompanha a área 16:9 da câmera, inclusive com barras laterais.
            Rect r = cameraDoMapa.rect;
            areaDaCamera.anchorMin = r.min;
            areaDaCamera.anchorMax = r.max;
            areaDaCamera.offsetMin = areaDaCamera.offsetMax = Vector2.zero;
            Vector2 tamanho = areaDaCamera.rect.size;
            float escala = Mathf.Min(tamanho.x / 1280f, tamanho.y / 720f);
            conteudo.localScale = Vector3.one * escala;
            if (caixaAviso.activeSelf && Time.unscaledTime > avisoAte) caixaAviso.SetActive(false);
            bool mostrar = alvo != null && alvo.isActiveAndEnabled && !Aberto;
            if (mostrar)
            {
                Vector3 p = cameraDoMapa.WorldToViewportPoint(alvo.transform.position + alvo.deslocamentoMarcador);
                mostrar = p.z > 0f && p.x > .04f && p.x < .96f && p.y > .06f && p.y < .92f;
                marcador.anchoredPosition = new Vector2((p.x - .5f) * 1280f, (p.y - .5f) * 720f);
                nomeMarcador.text = alvo.nome + "  |  Objetivo";
            }
            marcador.gameObject.SetActive(mostrar);
        }
    }
}
