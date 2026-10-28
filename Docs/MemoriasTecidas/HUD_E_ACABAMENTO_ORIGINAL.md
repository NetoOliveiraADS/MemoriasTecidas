# HUD e acabamento do cenário original — revisão 17

Esta atualização mantém a identidade anterior de Jaguaruana e aproveita somente a HUD aprovada da proposta seguinte. Como você não instalou a revisão 16, aplique este pacote diretamente ao projeto que já usa. Não há nada para restaurar no seu computador.

## O que muda

A HUD de exploração reúne objetivo, memórias concluídas, materiais da rede, relógio, local e indicação de interação. Os quatro nós superiores abrem Rede, Caderno, Mapa e Bolsa. Os contadores usam o progresso real. A paleta combina marrom escuro, creme e dourado, com contraste no foco do mouse e do teclado. A bolsa também abre com B; o atalho I continua funcionando.

O acabamento natural acrescenta relevo de luz discreto às próprias texturas e suaviza as sombras de contato, com bordas difusas e tom terroso. O realce é limitado e diminui quando o objeto está distante, para reduzir halos e cintilação. É um refinamento da arte 2D existente: não aumenta a resolução das imagens, acrescenta modelos 3D ou produz fotorealismo.

As construções, os pisos, as árvores, as imagens, os recortes, a organização das regiões, as colisões e a câmera original permanecem. O rosto, a roupa e as animações de Neto também permanecem. A abertura ilustrada da Praça Matriz, a imagem do mapa Redes de Memórias e os outros menus continuam iguais. A mudança de interface desta revisão se concentra na HUD de exploração e na opção de acabamento.

## Aplicar no mesmo projeto

1. Saia do Play, feche a Unity e extraia este ZIP fora da pasta do projeto.
2. Execute `Instalar_Atualizacao.cmd` e indique a pasta que contém `Assets`, `Packages` e `ProjectSettings`. Use o escopo padrão.
3. Reabra o projeto e `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
4. Execute **Memorias Tecidas → Interface → Validar interface e caminhada** e **Memorias Tecidas → Visual HD2D → Validar cenário original**.
5. Teste no Play antes de atualizar o Git.

O instalador confere versões conhecidas antes de copiar, preserva os `.meta` existentes e guarda backup dos arquivos substituídos. Uma edição local diferente interrompe a instalação e aparece na lista de conflitos. Não force a cópia sobre esses arquivos. O escopo padrão não copia cenas, prefabs, roteiro das missões nem saves.

O ZIP principal já contém os recursos em uso. O kit artístico separado serve como referência e não precisa ser importado inteiro em Assets. Este pacote foi preparado para quem não instalou a proposta isométrica.

## Comparar o acabamento

No Play, abra **Esc → Imagem e câmera → Acabamento natural**. Desligar essa opção reproduz o acabamento anterior das texturas e sombras de contato, mantendo a HUD nova. A preferência fica salva neste computador. O relevo da textura aparece nos níveis de qualidade intermediário e alto; o nível baixo prioriza desempenho. A configuração inicial está em `Resources/MemoriasTecidas/acabamento_original.json`.

Compare a mesma parede, árvore, telhado e piso, no mesmo horário, posição e zoom. A diferença deve ser sutil. O objetivo é valorizar os materiais existentes.

## Testes no Play

- Explore praça, entrada da cidade, oficina e rio. Compare o acabamento ligado/desligado; observe bordas dos sprites, piso distante, árvores e água.
- Confira a abertura e o mapa. As imagens devem continuar as mesmas.
- Teste B/I, M, J e F1, os botões da HUD com mouse e Tab/Enter e o objetivo oculto/visível.
- Aproxime-se de Dona Feliciana e de outros NPCs; confira a indicação E e as conversas.
- Selecione destino no mapa e missão no caderno. Confira a próxima saída, o nome e a distância na HUD.
- Colete materiais e conclua uma missão: os indicadores devem acompanhar o save. Confira avisos temporários junto da interação.
- Confira toques curtos de movimento, corrida, colisões, limites da câmera e retomada do save.
- Teste 1280×720, 1920×1080 e uma janela estreita, com contraste normal e reforçado.

O projeto de referência usa Unity **6000.3.25f1** e o renderizador Built-in. As verificações locais cobrem sintaxe C#, preservação de arquivos, contraste da HUD e integridade do pacote. **Compilação C#/shaders, execução do instalador no Windows, Play e build permanecem pendentes.** Veja `VERIFICACAO_HUD_ORIGINAL.json`.

## Prévias e Git

As prévias de menu, bolsa e mapa incluídas são composições anteriores, não capturas da Unity. `Previa_HUD_Areia.png` registra a HUD antiga e não representa a revisão 17. As prévias isométricas rejeitadas não acompanham esta atualização. Avalie o resultado definitivo no Play.

Depois dos testes, siga `TESTAR_E_ATUALIZAR_GIT.md`. Para abrir o projeto em outro computador, siga `COMECE_AQUI.md`. Não é necessário criar outro projeto nem outro repositório.
