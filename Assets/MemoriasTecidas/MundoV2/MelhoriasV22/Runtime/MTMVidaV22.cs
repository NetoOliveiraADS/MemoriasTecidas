using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MemoriasTecidas.MundoV2
{
    [DisallowMultipleComponent]
    public sealed class MTMVidaV22 : MonoBehaviour
    {
        [Serializable] public sealed class Cartao { public string regiao; public Texture2D imagem; }
        [Header("Interface — F1 oculta e mostra")]
        public bool mostrarInterface = true;
        public bool mostrarObjetivo;
        public bool mostrarAjuda = true;
        public Texture2D painelTecido;
        public Cartao[] lugares;
        [Header("Vida e movimento")]
        public bool moradoresAndando = true;
        public bool animaisAndando = true;
        [Range(0f, 2f)] public float intensidadeVento = 1f;
        [Range(.15f, 1.5f)] public float velocidadeMoradores = .48f;
        [Header("Registro da instalação — não alterar")]
        public int versaoInstalada;
        public float ampliacaoAplicada = 1f;
        [HideInInspector] public MTMJogo jogo;
        private MTMPassosV2 passos;
        private bool passosHabilitados;

        private void Awake()
        {
            jogo = GetComponent<MTMJogo>();
            passos = GetComponent<MTMPassosV2>();
            if (passos != null) passosHabilitados = passos.enabled;
        }
        private void Update()
        {
            bool apertou = false;
#if ENABLE_INPUT_SYSTEM
            apertou = Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            apertou = Input.GetKeyDown(KeyCode.F1);
#endif
            if (apertou) mostrarInterface = !mostrarInterface;
            // A poeira continua sendo a do projeto. Só a suprime em interiores.
            if (passos != null && jogo != null)
                passos.enabled = passosHabilitados && !jogo.V22Interior;
        }
        private void OnDisable() { if (passos != null) passos.enabled = passosHabilitados; }
        public Texture2D Imagem(string id)
        {
            if (lugares != null) foreach (var l in lugares)
                if (l != null && l.regiao == id) return l.imagem;
            return null;
        }
    }
}
