# Mapa de fios para orientar a jornada

Revisão visual de 23/10/2026: mapa em tecido areia e controles claros. A configuração dos locais e as rotas permanecem. Consulte `TEMA_AREIA.md`.

O mapa da tecla **M** agora usa a ilustração de Jaguaruana. Os marcadores se conectam às regiões carregadas no jogo, e a rota considera as passagens da cena e os bloqueios da campanha.

## Instalar no projeto atual

1. Feche a Unity. Baixe o ZIP mais recente e extraia em uma pasta nova fora do projeto.
2. Execute `Instalar_Atualizacao.cmd` e informe a pasta que contém Assets, Packages e ProjectSettings.
3. Abra o mesmo projeto e a cena Jaguaruana. Aguarde a importação.
4. Execute **Memorias Tecidas → Interface → Validar recursos dos menus**.
5. Entre no Play, continue a jornada e pressione **M**.

O instalador padrão atualiza a interface e seus recursos. A correção que preserva `MTMJogoNarrativa.cs` continua incluída. Os dados locais de missões, os diálogos, as cenas e os NPCs são preservados. A instalação mantém backup e interrompe a cópia se um arquivo de interface tiver uma edição local desconhecida.

## Escolher um caminho

1. Clique em um marcador na imagem ou escolha um item na aba **Lugares**.
2. Confira o nome do destino e a lista **Passagens até o destino**, na lateral.
3. Clique em **Guiar até aqui** para voltar à partida.
4. Siga a seta do HUD até a próxima saída e pressione **E** para atravessar.
5. A próxima saída é calculada a partir da região em que você está. Ao alcançar o destino marcado, aparece uma mensagem de chegada.

O botão **Missão** seleciona a região da etapa acompanhada. Uma estrela identifica esse objetivo, inclusive quando ele faz parte da rota numerada. **Você** centraliza sua região. **Seguir missão** encerra um destino manual e devolve a orientação ao objetivo acompanhado.

## Controles

| Ação | Controle |
| --- | --- |
| Abrir e fechar o mapa na partida | M |
| Retornar à tela anterior | Esc ou Voltar |
| Selecionar um local | Clique no marcador ou na lista Lugares |
| Ampliar e reduzir | Roda sobre o mapa, +/− ou botões da barra |
| Explorar a imagem ampliada | Arrastar com o botão esquerdo, WASD ou setas |
| Ver tudo novamente | Ver inteiro ou Home |
| Percorrer os controles | Tab / Shift+Tab |
| Ativar o controle selecionado | Enter / espaço |
| Atravessar uma saída ou conversar | E, durante a partida e perto da interação |

Os filtros **Todos**, **Visitados** e **Objetivo** ajudam a consultar os lugares. No mapa, a rota selecionada e sua região permanecem visíveis mesmo com um filtro ativo. A lista permite consultar regiões cujo marcador ainda não foi cadastrado na imagem.

## O que a orientação representa

O marcador **Você** identifica a região atual. O jogo carrega áreas separadas; a ilustração reúne essas áreas em um atlas. Dentro do cenário, a seta usa a posição real da saída ou do alvo da missão. Quando o alvo está visível numa área livre do HUD, recebe um contorno dourado. Caminhe pelos espaços livres e contorne obstáculos normalmente.

Os fios pontilhados e os números mostram a ordem das passagens entre regiões. Essa ordem é lida das saídas existentes da cena. Por exemplo, na base recebida, a ligação da Entrada para a Fábrica passa pela Praça Matriz e pelo Mercado. A proximidade de dois desenhos na imagem não cria uma passagem nova.

O mapa permite escolher destinos disponíveis e informa quando um lugar depende do progresso ou não possui rota. Para alterar os trajetos, edite as saídas das regiões na cena, conferindo destino e ponto de chegada. A atualização não cria teletransporte nem muda o trajeto físico por conta própria.

## Arquivos para personalizar

- `Assets/MemoriasTecidas/MundoV2/Runtime/MTMJogoMapaGuiado.cs`: painel de navegação, marcadores, rota e orientação no HUD.
- `Assets/MemoriasTecidas/MundoV2/Runtime/MTMJogoMapaMenu.cs`: consulta da imagem no menu inicial e controles compartilhados de zoom/arraste.
- `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Interface/MapaJaguaruana.png`: imagem usada como base.
- `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Interface/mapa_jaguaruana.json`: posições e nomes dos marcadores.

Cada marcador tem `id`, `nome`, `x` e `y`. O ID precisa coincidir com `dados.id` da região. As coordenadas vão de 0 a 1: x cresce da esquerda para a direita e y de cima para baixo. Trocar uma coordenada reposiciona apenas o marcador da interface. Mantenha o `.meta` ao substituir um recurso.

Há coordenadas para os 16 locais planejados. Só recebem marcador selecionável as regiões realmente cadastradas na cena. O arquivo de mundo da base recebida contém 15 regiões e ainda não tem uma região separada com ID `prefeitura`; a configuração já reserva esse marcador para cenas que a tenham. O validador avisa quando uma região do projeto não tem coordenadas. Não é necessário acrescentar uma região apenas para testar esta atualização.

## Testar no Play

| Verificação | Resultado esperado |
| --- | --- |
| M durante a partida | Abre a ilustração com marcadores; jogador e relógio ficam parados enquanto o painel está aberto. |
| Clicar em local e arrastar o fundo | O clique seleciona o local; o arraste move a imagem sem selecionar outro destino. |
| Você e Missão | Centralizam a região correta; a missão respeita a localização dos pontos registrada pelo jogo. |
| Destino liberado em outra região | Mostra a sequência real de saídas e permite ativar o guia. |
| Destino bloqueado ou sem rota | Mostra o motivo e mantém o botão de guiar desativado. |
| Seguir a rota | A seta indica a saída da região atual. E atravessa; ao mudar de região, a orientação é recalculada. |
| Chegar ao destino manual | Mostra a mensagem de chegada e encerra esse destino. |
| Missão no lugar atual | A seta aponta para a interação existente, incluindo a posição atual do NPC quando o vínculo está atualizado. |
| Pausa → mapa → Voltar | Retorna à pausa. Guiar até aqui retorna diretamente à jornada. |
| Zoom, teclado, 16:9 e 4:3 | Controles legíveis, zoom sem alterar a câmera do jogador e proporção da imagem preservada. |

`Previa_Mapa_Guiado.png` é um estudo de composição com um percurso de exemplo da base recebida. Não é uma captura da Unity nem um teste da interação do jogo.

As verificações locais cobrem sintaxe C#, IDs e coordenadas, integridade do pacote, preservação dos dados anteriores, fontes, metadados e conexões usadas na prévia. Unity, compilação C#, Play e instalador Windows não foram executados aqui. O relatório desta etapa está em `VERIFICACAO_MAPA_GUIADO.json`.
