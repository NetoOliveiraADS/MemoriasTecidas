using UnityEngine;

namespace MemoriasTecidas.MundoV2
{
    public sealed partial class MTMJogo
    {
        private Texture2D losangoCompacta;
        private Vector2 CoordenadaHUDCompacta(Vector3 p)
        {
            Rect px = cameraDoMapa.pixelRect;
            float s = Mathf.Min(px.width / 1280f, px.height / 720f);
            return new Vector2((p.x - px.x - (px.width - 1280 * s) * .5f) / s, (px.yMax - p.y - (px.height - 720 * s) * .5f) / s);
        }

        private void LosangoCompacta(Rect r, Color cor)
        {
            if (losangoCompacta == null)
            {
                const int n = 64;
                losangoCompacta = new Texture2D(n, n, TextureFormat.RGBA32, false)
                {
                    name = "No_de_memoria",
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear
                };
                var pixels = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        pixels[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - Mathf.Abs(x - 31.5f) - Mathf.Abs(y - 31.5f)));
                losangoCompacta.SetPixels(pixels);
                losangoCompacta.Apply(false, true);
            }

            Color antiga = GUI.color;
            GUI.color = cor;
            GUI.DrawTexture(r, losangoCompacta);
            GUI.color = antiga;
        }

        private string AcaoProximaHUD()
        {
            if (perto == null || perto.dados == null) return "";
            var d = perto.dados;
            var bau = perto.GetComponent<MTMBau>();
            if (bau != null) return bau.Aberto(estado) ? "Baú aberto" : "Abrir tesouro";
            if (d.tipo == "saida") return "Atravessar";
            if (d.tipo == "descanso") return "Descansar";
            if (d.id == IdAmostraPrimeiroFio)
                return estado.Tem(MTMPrimeiroFio.Teceu) ? "Rever amostra" : "Observar tecido";
            bool pessoa = tramaTecida != null && tramaTecida.personagens != null &&
                System.Array.Exists(tramaTecida.personagens, x => x != null && x.id == d.id);
            return pessoa ? "Conversar" : d.tipo == "coleta" ? "Recolher" : "Examinar";
        }

        private void HUDCompacta()
        {
            Color fundo = new Color(.14f, .13f, .11f, contrasteUI ? 1f : .92f);
            Color claro = new Color(.97f, .92f, .8f), dourado = new Color(.91f, .70f, .43f);
            bool primeiro = GuiaPrimeiroFioAtivo();
            var missao = MissaoAtualTecido();
            if (mostrarObjetivoTecido)
            {
                Fundo(new Rect(24, 22, 398, 92), fundo);
                Fundo(new Rect(24, 22, 3, 92), dourado);
                string titulo = primeiro ? aberturaMemorias.titulo : missao == null ? "Sua jornada" : missao.titulo;
                TextoTecido(new Rect(40, 34, 366, 25), ResumoHUDClaroUI(titulo, 366, 25, 18, true), 18, claro, true);
                TextoTecido(new Rect(40, 66, 366, 40),
                    ResumoHUDClaroUI(primeiro ? ObjetivoPrimeiroFio() : ObjetivoTecido(), 366, 40, 15), 15, claro);
                if (AcaoTecido(new Rect(24, 22, 398, 92), "hud:objetivo"))
                {
                    if (primeiro) AbrirRedeMemorias();
                    else
                    {
                        abaCaderno = "missoes";
                        missaoSelecionadaTecido = missao == null ? "" : missao.id;
                        AlternarPainel("diario");
                    }
                }
            }
            Fundo(new Rect(1130, 22, 126, 48), fundo);
            IconeTecido(Noite ? "noite" : "sol", new Rect(1142, 34, 22, 22), dourado);
            int minutos = Mathf.FloorToInt(estado.relogio * 1440f);
            TextoTecido(new Rect(1174, 31, 74, 28),
                (minutos / 60).ToString("00") + ":" + (minutos % 60).ToString("00"), 20, claro, true);

            Fundo(new Rect(24, 654, 352, 42), fundo);
            TextoTecido(new Rect(38, 663, 324, 26),
                ResumoHUDClaroUI(atual.dados.nome, 324, 26, 18, true), 18, claro, true);
            string[] nomes = { "B · Bolsa", "M · Mapa", "J · Caderno" };
            string[] ids = { "bolsa", "mapa", "diario" };
            for (int i = 0; i < ids.Length; i++)
            {
                Rect r = new Rect(782 + i * 160, 654, 154, 42);
                Fundo(r, fundo);
                TextoTecido(r, nomes[i], 15, claro, true, TextAnchor.MiddleCenter);
                if (AcaoTecido(r, "hud:atalho:" + ids[i])) AlternarPainel(ids[i]);
            }
            // A receita só aparece depois de ser apresentada na campanha.
            if (!primeiro && estado.Tem("r24_m09_01") && !estado.rede)
            {
                Rect receita = new Rect(24, 610, 224, 32);
                Fundo(receita, fundo);
                TextoTecido(receita, "Conferir materiais da rede", 13, claro, false, TextAnchor.MiddleCenter);
                if (AcaoTecido(receita, "hud:receita")) AlternarPainel("receita");
            }
            if (perto != null && Disponivel(perto))
            {
                Vector3 proj = cameraDoMapa.WorldToScreenPoint(perto.transform.position + Vector3.up * 1.2f);
                if (proj.z > 0)
                {
                    Vector2 pos = CoordenadaHUDCompacta(proj);
                    Rect r = new Rect(Mathf.Clamp(pos.x - 100, 24, 1056),
                        Mathf.Clamp(pos.y - 44, 128, avisoAte > Time.unscaledTime ? 492 : 560), 200, 36);
                    Fundo(r, fundo);
                    TextoTecido(r, "E · " + AcaoProximaHUD(), 15, claro, true, TextAnchor.MiddleCenter);
                    if (AcaoTecido(r, "hud:interagir")) Interagir(perto);
                }
            }
            else
            {
                var local = primeiro ? LocalPrimeiroFio() : null;
                string destino = !string.IsNullOrEmpty(destinoGuiadoTecido) ? destinoGuiadoTecido :
                    local != null ? local.dados.id : RegiaoDaEtapaTecido(EtapaAtualTecido(missao));
                MTMPonto alvo = ProximaSaidaTecido(destino);
                if (alvo == null && destino == atual.dados.id)
                    alvo = primeiro && string.IsNullOrEmpty(destinoGuiadoTecido) ? AlvoPrimeiroFio() : AlvoDaEtapaTecido(EtapaAtualTecido(missao));
                if (alvo != null && alvo.gameObject.activeInHierarchy)
                {
                    Rect rota = new Rect(434, 608, 412, 32);
                    Fundo(rota, fundo);
                    string texto = alvo.dados.tipo == "saida" ? "Caminho para " + NomeMapaGuia(alvo.dados.destino) : alvo.dados.nome;
                    TextoTecido(new Rect(446, 613, 388, 22), ResumoHUDClaroUI(texto, 388, 22, 13), 13, claro, false, TextAnchor.MiddleCenter);
                    if (AcaoTecido(rota, "hud:rota")) AlternarPainel("mapa");
                }
            }
            if (avisoAte > Time.unscaledTime)
            {
                Fundo(new Rect(390, 536, 500, 54), fundo);
                TextoTecido(new Rect(406, 544, 468, 38), ResumoHUDClaroUI(aviso, 468, 38, 14), 14, claro, false, TextAnchor.MiddleCenter);
            }
        }
    }
}
