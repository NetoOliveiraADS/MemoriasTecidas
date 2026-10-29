using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    // Marco de autoria: não possui arte nem colisão. Não use a tag EditorOnly;
    // a posição também precisa existir no build para orientar a criação do roteiro.
    [DisallowMultipleComponent]
    [AddComponentMenu("Memorias Tecidas/Marco do roteiro")]
    public sealed class MTMMarcoRoteiro : MonoBehaviour
    {
        [Tooltip("ID de pontosNovos em trama_01.json. Crie pelo painel Organizar projeto.")]
        public string idPonto;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.85f, .69f, .41f, .9f);
            Gizmos.DrawWireSphere(transform.position, 1.8f);
            Gizmos.DrawLine(transform.position + Vector3.left * .4f, transform.position + Vector3.right * .4f);
            Gizmos.DrawLine(transform.position + Vector3.up * .4f, transform.position + Vector3.down * .4f);
        }
    }
}
