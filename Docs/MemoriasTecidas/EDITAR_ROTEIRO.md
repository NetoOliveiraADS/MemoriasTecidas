# Como editar a campanha

Edite `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/trama_01.json` com a Unity fora do Play. Preserve a sintaxe JSON e a codificação UTF-8. Os dados são lidos ao iniciar a cena.

## Organização

| Campo | Função |
| --- | --- |
| `missoes` | 24 missões na ordem de apresentação; número, descrição, recompensa textual e pré-requisitos. |
| `etapas` | Ações de cada missão, executadas em sequência. Cada etapa tem um único evento persistente. |
| `pontosNovos` | Objetos e moradores gerados dentro das regiões existentes. |
| `atividades` | Regras, alternativas, respostas e dicas dos minijogos. |
| `pistas` | Texto do registro e evento que comprova a descoberta. |
| `itens` | Nome, categoria, origem, descrição e ocultação até obter o item. |
| `personagens` / `lugares` | Fichas e nomes usados na interface. |
| `bloqueios` | Eventos necessários para entrar na região. |
| `proximoCapitulo` | Nome, região, ponto e evento de revelação do próximo destino. |
| `paleta` | Cores da interface em hexadecimal. |

IDs de missões e eventos já publicados são parte do save. Renomeá-los faz o jogo deixar de reconhecer o progresso anterior. Preserve os eventos `r24_mNN_SS` ao editar apenas textos.

## Etapas

`regiao` e `ponto` precisam existir. Os horários aceitos são `dia`, `noite`, `entardecer` e `livre`. Dia significa 6h–18h, noite 18h–6h; entardecer aceita 17h em diante e a madrugada. Os diálogos pausam o relógio.

| Ação | Comportamento |
| --- | --- |
| `conversa` | Completa ao confirmar a última fala. |
| `coleta` | Confirma a observação/coleta antes de conceder o objeto. |
| `aproximar` | Completa ao chegar perto do ponto. |
| `aparicao` | Mostra uma figura por alguns segundos e apresenta o registro. |
| `escolta` | Inicia o percurso e exige acompanhar o grupo até o fim. |
| `atividade` | Abre a atividade indicada e só completa com a resposta correta. |

Use `falas` para a introdução e `falasConclusao` para mensagens depois de acertar uma atividade. `registro` guarda a observação no caderno.

`conceder` acrescenta itens; com `minimo: true`, completa somente até a quantidade informada. `consumir` exige e remove o objeto. `exigir` exige sem removê-lo. `completarMateriais` preenche apenas o que faltar da receita. `exigeReceita` bloqueia a montagem sem materiais; `fabricarRede` consome a receita e entrega a rede. Deixe `fabricarRede` apenas na última etapa da montagem.

## Minijogos

As respostas usam índices começando em zero. `sequencia` exige a ordem exata e permite repetir cores; `conjunto` e `grade` aceitam os índices corretos sem duplicação; `escolha` tem uma única alternativa. `requerPistas` impede resolver comparações antes de descobrir os registros necessários.

O painel atual comporta até 8 opções e 8 posições nas sequências, até 9 opções nas seleções e uma grade fixa 3×3. Para atividades maiores, ajuste o desenho do painel em `MTMJogoAtividades.cs` antes de aumentar esses limites. O modelo explícito aparece com `mostrarModelo: true`; as referências oferecem a dica e as pistas já descobertas.

## Pontos e artes

`x` e `y` são coordenadas locais da região. `modeloRegiao` e `modeloPonto` copiam a aparência de um personagem existente. `modeloObjeto` procura um SpriteRenderer pelo nome; quando não encontra, usa uma placa desenhada pelo jogo. `andar` com `patrulha` habilita o morador que caminha. A escolta usa o `percurso` definido na etapa.

Os objetos gerados no Play não devem ser copiados manualmente para a cena. Edite o JSON, reinicie o Play e confira alcance, colisões e ordenação visual.

## Definir o próximo capítulo

O destino provisório é `serra / pdf_proximo_capitulo`, em `(-20, -9)`. Para mudar o ponto dentro de uma região existente, atualize `proximoCapitulo` e a entrada com o mesmo ID em `pontosNovos`, incluindo coordenadas e nome. O ponto fica acessível pelo evento final da missão 23 e é revelado no mapa pelo final da missão 24.

Para um cenário realmente novo, crie a região, as conexões de saída e as artes no projeto antes de referenciá-lo. Esta atualização não constrói um capítulo posterior ao PDF nem determina a solução do mistério.

## Onde está a lógica

| Arquivo | Responsabilidade |
| --- | --- |
| `Runtime/MTMMotorMissoes.cs` | Pré-requisitos, horários, respostas, consumo e recompensas. |
| `Runtime/MTMJogoNarrativa.cs` | Diálogos, objetivos, rotas e interação com o roteiro. |
| `Runtime/MTMJogoEventos.cs` | Criação de pontos, moradores, aparições, escoltas, decoração e som. |
| `Runtime/MTMJogoAtividades.cs` | Painéis de atividades, pistas e memórias. |
| `Runtime/MTMEstado.cs` | Inventário e progresso serializáveis. |
| `Runtime/MTMTrama.cs` | Estrutura dos dados JSON. |
| `Runtime/MTMJogo.cs` | Movimento, carregamento, salvamento e interação geral. |
| `Runtime/MTMJogoInterface.cs` | HUD, mapa, bolsa, caderno e paleta. |
| `Editor/MTMValidarRoteiroPDF.cs` | Verificação das regras dentro da Unity, sem alterar o save. |

Depois de editar, execute o validador da Unity e teste o percurso afetado no Play. Se alterar a quantidade de etapas ou de missões, ajuste também as contagens esperadas do validador.
