using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Depois da troca de quadro (150), antes das malhas de moradores (200) e Neto (220).
    [DefaultExecutionOrder(175), DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class MTMAcabamentoPersonagem : MonoBehaviour
    {
        private static readonly int PessoaId = Shader.PropertyToID("_MTM_Pessoa");
        private static readonly int RecorteId = Shader.PropertyToID("_MTM_RecortePessoa");
        private SpriteRenderer fonte;
        private Sprite ultimoQuadro;
        private MaterialPropertyBlock bloco;
        private float pessoaAnterior;
        private Vector4 recorteAnterior;

        private void OnEnable()
        {
            fonte = GetComponent<SpriteRenderer>();
            if (bloco == null) bloco = new MaterialPropertyBlock();
            fonte.GetPropertyBlock(bloco);
            pessoaAnterior = bloco.GetFloat(PessoaId);
            recorteAnterior = bloco.GetVector(RecorteId);
            ultimoQuadro = null;
        }

        private void LateUpdate()
        {
            if (fonte == null || fonte.sprite == null || fonte.sprite == ultimoQuadro) return;
            var uv = fonte.sprite.uv;
            if (uv == null || uv.Length == 0) return;
            Vector2 minimo = Vector2.one, maximo = Vector2.zero;
            foreach (var ponto in uv)
            {
                minimo = Vector2.Min(minimo, ponto);
                maximo = Vector2.Max(maximo, ponto);
            }
            fonte.GetPropertyBlock(bloco);
            bloco.SetFloat(PessoaId, 1f);
            bloco.SetVector(RecorteId, new Vector4(minimo.x, minimo.y, maximo.x, maximo.y));
            fonte.SetPropertyBlock(bloco);
            ultimoQuadro = fonte.sprite;
        }

        private void OnDisable()
        {
            if (fonte == null || bloco == null) return;
            // Restaura apenas os próprios campos, preservando alterações de outros componentes.
            fonte.GetPropertyBlock(bloco);
            bloco.SetFloat(PessoaId, pessoaAnterior);
            bloco.SetVector(RecorteId, recorteAnterior);
            fonte.SetPropertyBlock(bloco);
            ultimoQuadro = null;
        }
    }
}
