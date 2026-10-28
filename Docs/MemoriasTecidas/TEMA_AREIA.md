# Tema areia e identidade do Neto

Atualização de 23/10/2026 para o mesmo projeto. O instalador padrão aplica interface, arte de abertura, mapa e integração do kit HD2D. A revisão atual preserva o cenário anterior; veja `RESTAURAR_VISUAL_ORIGINAL.md`.

## O que aparece no jogo

- Abertura com varanda e rede em luz quente, painel creme para leitura e acesso compacto ao mapa.
- Mapa de fios sobre tecido areia, rotas em terracota e marcadores com contorno marrom. Os locais, coordenadas, saídas e bloqueios mantêm a configuração anterior.
- HUD compacto: local e objetivo no alto à esquerda; horário e rede à direita; orientação, interação e atalhos na parte inferior. Objetivos longos são resumidos de acordo com a largura real da fonte; a versão completa continua no caderno.
- Bolsa, caderno, pausa, diálogos e atividades compartilham superfícies claras, texto marrom e botões terracota. Os fios permanecem como acabamento.
- Contraste reforçado mantém superfícies claras opacas e texto preto. As preferências e o progresso continuam separados.

O azul do rio e o verde da vegetação continuam presentes na arte. Areia e creme são as cores dominantes da interface.

## Paleta da interface

| Papel | Cor |
| --- | --- |
| Painel areia | `#E7D3B0` |
| Fundo | `#F7EFDE` |
| Superfície creme | `#FFF9EE` |
| Cartões | `#EFE1C8` |
| Texto | `#403128` |
| Texto de destaque | `#7B432B` |
| Ação principal | `#8A472E` |
| Fios e bordas decorativos | `#BA8446` |

Edite `Assets/MemoriasTecidas/MundoV2/Runtime/MTMJogoTemaAreia.cs` para personalizar essas cores. Os campos antigos de paleta em `trama_01.json` permanecem preservados; os menus passam a usar este tema. Cores de desafios, motivos da rede e cenário continuam vindo de seus próprios dados.

## Neto no kit visual

`Personagens/09_Protagonista_Folha_Poses.png`, no kit, foi revisado usando o retrato e a folha cardinal do Neto encontrados no projeto organizado. Foram mantidos cabelo escuro penteado de lado, sobrancelhas marcadas, barba cheia arredondada e bigode. O novo corpo e o figurino permanecem.

A integração agora usa duas folhas, com 32 quadros em oito direções: S, SW, W, NW, N, NE, E e SE. Recortes e pivôs são lidos de `HD2D/kit_hd2d.json` e aplicados ao controlador existente ao iniciar o Play. O kit complementar inclui as mesmas folhas para edição. Veja o guia de integração para personalizar e testar.

## Aplicar e testar

1. Feche a Unity e extraia o ZIP de atualização fora do projeto.
2. Execute `Instalar_Atualizacao.cmd` e indique a raiz do projeto atual.
3. Reabra `Jaguaruana.unity`, aguarde a importação e execute **Memorias Tecidas → Interface → Validar recursos dos menus** e **Memorias Tecidas → Visual HD2D → Validar integração**.
4. No Play, confira abertura, M, zoom, seleção de destino, seta de saída, conversa com Feliciana, I, J e Esc.
5. Teste objetivo longo, horário, contraste reforçado, Tab/Enter e janelas de 1280×720, 1920×1080 e 4:3.
6. Depois dos testes, siga `TESTAR_E_ATUALIZAR_GIT.md` para atualizar o repositório existente.

O instalador compara versões conhecidas, guarda backup e preserva cenas, prefabs, `.meta` existentes, NPCs e dados narrativos. Edições locais desconhecidas nos arquivos selecionados interrompem a cópia para permitir a mesclagem.

## Prévias e conferência

`Previa_Menu_UIUX.png`, `Previa_Mapa_Guiado.png`, `Previa_HUD_Areia.png` e `Previa_Bolsa_Areia.png` são composições de referência. Usam a paleta e as medidas principais da interface, com conteúdo de exemplo. O cenário pintado da prévia da HUD não substitui o cenário da Unity. Os ícones da bolsa no jogo continuam vindo de `MTMIconesTecido`.

Foram conferidos sintaxe C#, hashes, GUIDs, preservação da campanha, coordenadas do mapa, contraste das combinações principais e integridade dos arquivos. A conferência da etapa areia está em `VERIFICACAO_TEMA_AREIA.json`; a integração atual está em `VERIFICACAO_INTEGRACAO_HD2D.json`.

**Não foi executada compilação, Play ou build na Unity, nem o instalador PowerShell.** As prévias não são capturas do jogo em execução.
