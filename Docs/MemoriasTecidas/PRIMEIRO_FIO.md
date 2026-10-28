# O primeiro fio — abertura e rede de memórias

Esta atualização acrescenta uma experiência jogável ligada a Dona Feliciana e uma rede visual que acompanha os encontros da campanha. Trabalha no mesmo projeto Unity 6000.3.25f1 e mantém a campanha de 24 missões.

## O que o jogador faz

| Momento | Ação | Resultado |
| --- | --- | --- |
| Chegada | Conversar com Dona Feliciana e aceitar o convite na última fala | Orientação para observar a amostra |
| Descoberta | Aproximar-se da amostra e pressionar E | Observar quatro passagens coloridas |
| Experiência | Escolher Creme, Dourado, Creme e Terracota | Ver o tecido aparecer a cada escolha |
| Retorno | Conversar novamente com Dona Feliciana | Guardar o primeiro motivo e abrir a rede no caderno |
| Continuidade | Encontrar Simão e continuar a campanha | Novos encontros passam a preencher outros motivos |

O texto original da primeira missão continua presente. O convite para a amostra aparece após essa conversa. A atividade é adicional: o jogador pode seguir diretamente para Simão e retomá-la depois.

A amostra ensina a observar e repetir um padrão. Não representa uma reconstrução técnica de um tear real nem atribui um padrão histórico a Jaguaruana. Os diálogos novos são ficcionais e podem ser revistos pela equipe de pesquisa.

## Instalar e experimentar

1. Feche a Unity e extraia o ZIP em uma pasta nova, fora do projeto.
2. Execute `Instalar_Atualizacao.cmd` e informe a raiz do projeto existente, onde estão Assets, Packages e ProjectSettings.
3. Abra o mesmo projeto e aguarde a compilação. Abra `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
4. Execute **Memorias Tecidas → Primeiro fio → Validar abertura e rede**.
5. Entre no Play. Abra **J → Rede → Acompanhar o primeiro fio**. Encontre Feliciana na região em que ela está na sua cena.
6. Complete a sequência da tabela. Se houver um diálogo de missão pendente com Feliciana, ele será apresentado primeiro; converse novamente para a atividade adicional.

Em uma jornada realmente nova, o protagonista começa no ponto seguro da região de Feliciana. Se ela estiver na Entrada da Cidade, a abertura começa na Entrada. Um save que já contém progresso conserva sua região e sua missão atual.

**Não é necessário iniciar Nova jornada para experimentar.** J → Rede permite acompanhar a abertura em um save existente. Essa escolha muda a orientação do HUD; as missões e os materiais anteriores permanecem.

## Controles da amostra

- Clique nas cores ou use **1, 2 e 3**; as cores também aparecem pelo nome.
- **U** ou **Backspace** desfaz a última passagem.
- **Enter** ou **E** confere a sequência.
- **Esc** ou **Voltar** fecha a atividade sem concluí-la.
- **Recomeçar** limpa somente as quatro escolhas em andamento.

Não há tempo limite, custo de materiais ou penalidade por erro. A conferência indica a primeira passagem diferente do modelo. Ao sair antes de concluir, as escolhas parciais são descartadas; a observação já confirmada continua salva. Após acertar, basta retornar a Feliciana, inclusive depois de fechar e reabrir o jogo.

## A rede no caderno

Abra **J → Rede** ou clique em **Rede de memórias** no canto superior direito do HUD. Cada motivo revelado pode ser selecionado para ler um resumo e abrir o registro correspondente.

| Motivo | Quando aparece |
| --- | --- |
| Um encontro para começar | Ao retornar da amostra a Feliciana ou concluir a missão 1 com Simão |
| Caminhos da Serra | Ao concluir a missão 3 |
| Vestígios e materiais | Ao concluir a missão 5 |
| Um caminho compartilhado | Ao concluir a missão 8 |
| As mãos que ensinam | Ao concluir a missão 13 |
| Preparar o encontro | Ao concluir a missão 14 |
| Escutar a margem | Ao concluir a missão 18 |
| Uma imagem, novas perguntas | Ao concluir a missão 19 |
| Histórias que se encontram | Ao concluir a missão 24 |

Os motivos futuros têm títulos e resumos ocultos. Saves anteriores recuperam os motivos a partir dos eventos já concluídos; não é preciso repetir missões. A rede do caderno é o registro da jornada. A rede fabricada na oficina continua sendo o objeto da campanha, com sua receita original.

**Continuar pela campanha**, dentro da aba Rede, suspende a orientação da abertura. **Acompanhar o primeiro fio** a retoma. Escolher uma missão para acompanhar no caderno também dá prioridade a essa missão.

## Colocar a amostra no cenário

A amostra padrão é um pequeno expositor com fios, criado durante o Play. Depois que o jogador acerta o padrão, suas passagens ganham cor no cenário.

Sem uma posição autoral, o jogo procura espaço próximo a Feliciana usando consultas de colisão. Caso não encontre um candidato livre, a atividade fica disponível conversando com a própria personagem. Isso mantém a abertura acessível; a posição final deve ser conferida na cena real.

Para escolher o lugar exato:

1. Saia do Play.
2. Use **Memorias Tecidas → Primeiro fio → Posicionar amostra junto a Feliciana**.
3. O ponto `Amostra_PrimeiroFio` será criado ou selecionado. Ele é filho da interação de Feliciana e acompanha esse objeto quando movido.
4. Com Gizmos ligados, observe o contorno do expositor e a área de interação. Arraste-o para um trecho visível e acessível, a poucos passos de Feliciana.
5. Salve a cena com Ctrl+S e confira no Play.

O menu não muda sozinho uma posição que você já escolheu. Ctrl+Z desfaz a criação. Preserve um único ponto com ID `abertura_amostra`, na mesma região da personagem.

Você pode adicionar sua própria arte como filha desse ponto. Quando já existe um Renderer no ponto, o expositor padrão não é criado. A mudança de cor após tecer pertence ao expositor padrão; uma arte própria precisa de sua própria resposta visual.

Se mover Feliciana, mova o objeto completo com a interação `npc_feliciana`, conforme `MOVER_PERSONAGENS.md`. A região real da cena continua definindo os diálogos e a orientação.

## Ajustar o conteúdo

Arquivo: `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/abertura_memorias.json`.

- `convite`, `observacao`, `conclusao` e `reencontro`: falas novas da abertura.
- `nomes` e `cores`: três nomes e três cores hexadecimais correspondentes.
- `padrao`: quatro índices de 0 a 2, na ordem das passagens.
- `motivos`: nove registros visuais, com título, resumo, cor e ícone.
- `eventos`: um dos eventos listados precisa estar concluído para revelar o motivo.
- `registro`: etapa do caderno aberta pelo botão de leitura.

Mantenha os IDs dos eventos para preservar os saves. O JSON da campanha, `trama_01.json`, permanece igual ao da atualização anterior. Depois de editar, execute o validador da abertura novamente.

Scripts acrescentados:

| Arquivo | Responsabilidade |
| --- | --- |
| `MTMPrimeiroFio.cs` | Dados e regras de progresso, resposta e revelação dos motivos |
| `MTMJogoPrimeiroFio.cs` | Integração com Feliciana, interação, save e orientação |
| `MTMJogoRedeMemorias.cs` | Amostra interativa, rede no caderno e indicador no HUD |
| `MTMAmostraTecida.cs` | Geometria do expositor e mudança visual após a atividade |
| `MTMPrimeiroFioEditor.cs` | Posicionamento autoral, Gizmo e validação dentro da Unity |

O progresso adicional usa a lista `marcos` do save existente. Não muda a versão do save, não concede inventário e não marca etapas da campanha como concluídas. O jogo salva cada etapa confirmada se `carregarProgresso` estiver habilitado.

## Teste antes de atualizar o Git

| Teste | Resultado esperado |
| --- | --- |
| Abrir um save avançado | Mesma região/progresso; motivos antigos já visíveis |
| Fechar um diálogo com Esc | A etapa adicional não avança sem confirmar a última fala |
| Errar uma das quatro cores | Mensagem aponta a passagem; materiais permanecem iguais |
| Sair no meio da amostra | Ao retornar, a observação está salva e as escolhas parciais recomeçam |
| Acertar e sair antes de voltar a Feliciana | Ao reabrir, o objetivo continua sendo retornar a ela |
| Conversar após concluir | Nenhuma recompensa ou motivo é duplicado |
| Seguir direto para Simão | Campanha continua; abertura fica disponível para depois |
| Concluir uma missão associada a motivo | Rede ganha o motivo; leitura abre o registro correto |
| Mover Feliciana antes do Play | Orientação e amostra permanecem vinculadas à região dela |
| Testar dia, noite e três qualidades | Amostra e jogador legíveis, shader sem magenta |
| 1280×720, 1920×1080 e janela estreita | Botões acessíveis, textos legíveis e painéis centralizados |

Para ensaiar uma jornada nova sem gravar por cima do save, saia do Play, selecione o objeto com MTMJogo e desmarque `carregarProgresso` no Inspector. Entre no Play e só então use Nova jornada: com essa opção desligada, o módulo não lê nem grava o progresso em disco. Ao terminar, saia do Play e restaure a opção antes de jogar normalmente. Com `carregarProgresso` habilitado, Nova jornada substitui o progresso do módulo após confirmação.

As verificações locais deste pacote incluem análise sintática C#, ligações dos dados, preservação da campanha e conferência dos arquivos de instalação. **Unity, compilação C#/shaders, Play, build e o instalador Windows não foram executados aqui.** O validador incluído executa as regras C# quando você o aciona na Unity, usando estados em memória, sem escrever no save.

O tempo de 10–15 minutos é uma meta para a experiência inicial completa, incluindo deslocamento, leitura e continuidade até Simão. A duração desta abertura ainda precisa ser medida com jogadores; não é uma duração comprovada.

## Próxima revisão com jogadores

Peça a três pessoas que experimentem sem explicar o caminho. Anote tempo, hesitações, erros e pedidos de ajuda. Depois pergunte: “O que você queria descobrir?”, “Como soube onde ir?” e “O que a rede representa?”. Use essas observações para ajustar distância entre pontos, ritmo dos diálogos e clareza do objetivo.

As próximas melhorias propostas estão em `PROXIMAS_MELHORIAS.md`. As instruções para publicar no mesmo repositório estão em `TESTAR_E_ATUALIZAR_GIT.md`.

## Referências técnicas usadas

- [Unity 6.3 — Physics2D.OverlapCircleAll](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics2D.OverlapCircleAll.html)
- [Unity 6.3 — Physics2D.LinecastAll](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics2D.LinecastAll.html)
- [Unity Input System — Keyboard](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.Keyboard.html)
