# Interface de Memórias Tecidas

Atualização de 23/10/2026 para o mesmo projeto Unity 6000.3.25f1, com o pipeline Built-in e a campanha existente.

## O que mudou

- **Cenário original restaurado:** vegetação, oficina, pisos e água usam as artes anteriores com realce discreto de detalhes. Neto, retrato proporcional de Feliciana e tema areia permanecem. O mapa consulta as ilustrações anteriores dos lugares. Veja `RESTAURAR_VISUAL_ORIGINAL.md`.

- **Tema areia:** novas artes do mapa e da abertura, superfícies creme, texto marrom e ações terracota. HUD com objetivo e relógio menores. Veja `TEMA_AREIA.md`.

- **Mapa de Jaguaruana na jornada:** a tecla M e o menu de pausa abrem o mapa de fios com regiões selecionáveis, rota numerada, objetivo e região atual. Guiar até aqui ativa a indicação da próxima saída no HUD. Há zoom, arraste e lista acessível. A consulta da arte permanece na tela inicial. Consulte `MAPA_GUIADO.md` e `MAPA_MENU.md`.
- **Tela inicial ilustrada:** a jornada começa em um menu próprio. Continuar aparece quando há progresso; Começar aparece na primeira sessão. Opções, Como jogar, Sobre e Sair ficam acessíveis antes de entrar no cenário.
- **Pausa:** ação principal para continuar, atalhos de bolsa/caderno e acesso separado às opções. Reiniciar fica na tela inicial, com confirmação e cópia do save antes da substituição. Se a cópia falhar, o reinício é interrompido.
- **Mapa, bolsa e caderno:** cabeçalho e ordem de navegação comuns, botão Voltar no mesmo lugar, títulos em serifada e textos de leitura em sans-serif. Texturas foram retiradas de superfícies usadas para leitura; nós e tramas permanecem nos conteúdos ligados à rede.
- **Bolsa:** começa mostrando os objetos obtidos; “Mostrar todos os itens” também permite consultar os faltantes. Busca com botão para limpar. Quantidade, origem e próxima ação ficam separadas. O mapa abre sobre a ilustração de fios e mantém a aba Lugares como alternativa.
- **Teclado:** Tab/Shift+Tab percorrem botões e cartões, Enter/espaço acionam o foco. Setas cima/baixo também percorrem as telas inicial, pausa, opções e ajuda. A lista rola para revelar um cartão focado. O campo de busca preserva a digitação; M/I/J não mudam de tela enquanto ele está em foco. As ações da conversa e da amostra continuam disponíveis por E.
- **Conforto:** contraste reforçado, conversa em três tamanhos (20, 24 e 28 unidades na referência de 720p), movimento reduzido, objetivo do HUD, brilho, zoom e efeitos. Preferências ficam no PlayerPrefs, separadas da campanha. O tamanho maior se aplica às falas, com rolagem; não é um escalonamento de toda a interface.
- **Salvamento:** abrir a tela inicial pela primeira vez não cria nem sobrescreve o save. O salvamento da jornada é habilitado ao entrar. Retornar à tela inicial durante uma sessão preserva o estado carregado.

A imagem de abertura é uma interpretação artística do tema e da paisagem. Não pretende reproduzir fielmente uma vista ou monumento de Jaguaruana.

## Referências pesquisadas e decisões aplicadas

| Referência primária | O que orientou o projeto |
| --- | --- |
| [Thunder Lotus — Spiritfarer Digital Art Book](https://thunderlotusgames.com/blog/buy-the-spiritfarer-digital-art-book-on-itch-io/) | Coerência entre pequenos ícones e ambientes, desenho acolhedor e direção artesanal. Aplicado como princípio de unidade, sem reutilizar arte do jogo. |
| [Lettermatic — tipografia de Pentiment](https://lettermatic.com/custom/pentiment) | Tipografia ligada à narrativa, sem perder legibilidade. Aplicado com uma serifada nos títulos e uma sans-serif no conteúdo. |
| [Xbox Accessibility Guideline 101 — Text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101) | Hierarquia, legibilidade e possibilidade de ampliar texto. Aplicado principalmente às conversas e às superfícies de leitura. |
| [XAG 102 — Contrast](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102) | Texto com fundo previsível e opção de contraste. Aplicado com áreas opacas e tinta mais escura para texto terracota sobre algodão. |
| [XAG 112 — UI navigation](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112) | Ações e posições consistentes, retorno previsível e foco visível. Aplicado à navegação compartilhada dos menus. |
| [Unity — GUIUtility](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GUIUtility.html) | API de interface imediata usada pelo projeto existente. A navegação adicional continua dentro do IMGUI. |

Essas referências orientam o desenho. Esta entrega não implementa leitura de tela, navegação por controle ou ampliação de 200% de toda a UI.

## Sistema visual

| Uso | Cor |
| --- | --- |
| Painéis | Areia `#E7D3B0` |
| Fundo | `#F7EFDE` |
| Superfície de leitura | Creme `#FFF9EE` |
| Cartões | `#EFE1C8` |
| Texto | Marrom `#403128` |
| Texto de destaque | `#7B432B` |
| Ação principal | Terracota `#8A472E`, com texto creme |
| Fios e bordas decorativos | `#BA8446` |

A paleta compartilhada fica em `MTMJogoTemaAreia.cs`; os dados narrativos não são alterados para aplicar o tema.

**Títulos:** DejaVu Serif. **Leitura e botões:** DejaVu Sans e Sans Bold. Os arquivos acompanham o pacote, com licença em `LICENCA_FONTES.txt`; o computador de destino não precisa instalar as fontes no sistema.

Os painéis mantêm a referência de 1280×720, com escala uniforme e centralização. Em proporções diferentes aparecem margens. Para conferir a leitura, teste em 1280×720, 1920×1080 e uma janela 4:3. Resoluções pequenas podem exigir janela maior para manter o conforto.

## Instalar e conferir

1. Feche a Unity. Extraia o ZIP fora do projeto.
2. Execute `Instalar_Atualizacao.cmd` e informe a pasta que contém `Assets`, `Packages` e `ProjectSettings`.
3. O instalador padrão confere as versões conhecidas dos arquivos de interface, cria backup ao lado do projeto e acrescenta a arte e as fontes. Narrativa, dados da campanha, cenas, prefabs e `.meta` existentes são preservados. Se detectar edição local desconhecida em um arquivo selecionado, para antes de copiar. Consulte `CORRECAO_INSTALACAO_MENU.md`.
4. Abra o mesmo projeto e aguarde a importação. Abra `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
5. Execute **Memorias Tecidas → Interface → Validar recursos dos menus**. O validador verifica arte, fontes e referências básicas; ele não executa o jogo.
6. Pressione Play. A tela inicial deve aparecer antes da exploração.

`Previa_Menu_UIUX.png` e o SVG da raiz são um estudo de composição com dados de exemplo. Não são capturas do jogo em execução. A implementação usa os mesmos recursos e medidas principais; os ícones reais vêm de `MTMIconesTecido`.

## Testes no Play antes de publicar

| Fluxo | Resultado esperado |
| --- | --- |
| Explorar o mapa na tela inicial | Mostra a imagem completa, sem rodapé descritivo; não inicia a jornada. Zoom, arraste, Ver inteiro e Esc funcionam. |
| Pausa → Mapa de Jaguaruana → Voltar | Retorna à pausa; personagem e relógio permanecem parados. |
| M → selecionar local → Guiar até aqui | Volta à jornada; a seta aponta para a saída correta e a rota acompanha a mudança de região. |
| Você / Missão / Lugares | Centraliza a região correspondente ou abre a lista quando não há coordenadas. Objetivos e bloqueios usam o progresso da jornada. |
| Sem save, abrir e sair do menu | Nenhum save de jornada é criado só por abrir o menu. Começar entra na região de Feliciana. Use uma cópia do projeto com Product Name de teste para um diretório de progresso separado. |
| Com progresso existente, Continuar | Itens, marcos, missões e rede preservados. O relógio não avança enquanto o menu está aberto. |
| Esc em opções da tela inicial | Volta à tela inicial, sem iniciar a jornada. |
| Esc em opções da pausa | Volta à pausa; outro Esc continua a exploração. |
| Pausa → bolsa/caderno → Voltar | Retorna à pausa. Atalhos M/I/J durante a exploração voltam à jornada ao fechar. |
| Nova jornada → Manter jornada | Não altera o progresso. Esc também cancela a confirmação. |
| Nova jornada → Recomeçar | Cria arquivo `.antes-nova-jornada-...json` junto ao save e começa uma jornada vazia. Testar só no perfil de teste. |
| Tab/Shift+Tab e Enter/espaço | Foco visível, ordem repetível, uma ação por confirmação. Em listas longas o foco permanece visível. |
| Bolsa → buscar “fio”, “memória”, “pano” | Texto digitado não abre mapa/caderno. Limpar e trocar filtros atualiza a lista; lista vazia apresenta orientação. |
| Enter em botão de diálogo | Avança apenas uma página. “Anterior” relê; Esc sai sem concluir a etapa pendente. |
| Primeiro fio, teclado e mouse | Escolher, desfazer e conferir continuam funcionando, sem conceder o resultado duas vezes. |
| Conversa com texto maior | A fala completa pode ser lida pela rolagem. Testar acentos e frases longas. |
| Contraste, brilho e movimento reduzido | Preferências permanecem após fechar e abrir. Alterar contraste não acumula texturas de círculo. |
| 16:9, 4:3 e ultrawide | Sem botões cortados, fuga de linhas, distorção de proporção ou texto sobre ilustração sem fundo. |
| Caderno → Rede e mapa de lugares | Progresso antigo aparece; selecionar um local e guiar preserva os vínculos atuais dos NPCs. |

## Arquivos para editar depois

- `Runtime/MTMJogoTemaAreia.cs`: paleta areia, contraste e resumo de textos da HUD.
- `Runtime/MTMJogoMenus.cs`: tela inicial, pausa, ajuda, sobre, retorno e reinício protegido.
- `Runtime/MTMJogoMapaMenu.cs`: prévia, consulta ampliada, zoom e arraste do mapa ilustrado.
- `Runtime/MTMJogoMapaGuiado.cs`: mapa da jornada, seleção de locais, rota e orientação no HUD.
- `Resources/MemoriasTecidas/Interface/mapa_jaguaruana.json`: IDs das regiões, nomes usados no mapa e coordenadas dos marcadores na imagem.
- `Runtime/MTMJogoNavegacaoUI.cs`: foco de teclado e revelação de itens nas listas.
- `Runtime/MTMJogoInterface.cs`: estilos compartilhados, HUD, janela, mapa, caderno e diálogo.
- `Runtime/MTMJogoRedeUI.cs`: bolsa, receita e missões.
- `Runtime/MTMJogoApresentacao.cs`: opções de imagem e conforto.
- `Resources/MemoriasTecidas/Interface/InicioTecida.png`: arte de abertura. Carregada por nome, sem precisar configurar a cena.
- `Resources/MemoriasTecidas/Interface/MapaJaguaruana.png`: mapa com os lugares definidos pelo autor, sem a descrição de rodapé; mantém os nomes incorporados à ilustração.
- `Resources/MemoriasTecidas/Interface/*.ttf`: fontes incluídas no jogo.
- `Editor/MTMInterfaceEditor.cs`: verificação dos recursos importados.

Os caminhos acima são relativos a `Assets/MemoriasTecidas/MundoV2`. Para mudar a arte, mantenha o nome do PNG e seu `.meta`. Os nomes dos locais fazem parte do mapa fornecido; os botões, controles e cabeçalhos do jogo são desenhados separadamente.

## Proveniência da arte

`InicioTecida.png` foi criada com a ferramenta integrada de geração de imagens nesta etapa, para uso no projeto. O prompt completo está em `PROMPT_ARTE_MENU.txt`. Nenhum recurso visual de Spiritfarer ou Pentiment foi distribuído. As fontes DejaVu foram copiadas sem modificação, com o aviso de licença.

## O que foi verificado aqui

Conferência de sintaxe C#, dados narrativos preservados, GUIDs existentes, recursos, hashes de texto/binários e integridade do ZIP. Conferência visual de uma prévia de composição e estimativa de encaixe de rótulos com as fontes distribuídas. O relatório desta revisão está em `VERIFICACAO_TEMA_AREIA.json`; os relatórios anteriores permanecem como histórico.

Unity, compilação nativa de C#/shaders, Play Mode, build e execução do instalador PowerShell não foram executados neste ambiente. O roteiro de teste acima é necessário na cena atual, especialmente para foco, rolagem, diálogos e reinício.
