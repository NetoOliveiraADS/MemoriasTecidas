# Memórias Tecidas — plano de melhorias futuras

Proposta de evolução para Jaguaruana Rede de Memórias, organizada em 28/10/2026 após revisar o código e os recursos da atualização 18. É um conjunto amplo de possibilidades, não uma lista fechada nem uma promessa de que todas serão implementadas.

**Direção:** preservar a câmera, a organização de Jaguaruana, as construções reconhecíveis, as referências locais e o rosto de Neto. Melhorar a experiência existente por etapas, comparando sempre com uma versão aprovada.

## Primeiro marco recomendado

Terminar uma experiência curta e completa: entrada da cidade → conversa com Feliciana → Praça Matriz → oficina → primeira lembrança tecida. Um jogador novo deve compreender a missão, encontrar o caminho, conversar, executar a atividade, receber a recompensa e retomar o save sem ajuda externa.

Esse percurso deve funcionar antes de acrescentar regiões ou uma reformulação gráfica maior. A revisão 18 já prepara diálogos mais claros e continuidade por posição; os itens abaixo ampliam esse trabalho.

## Prioridade 1 — confiança e orientação

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Autosave periódico configurável | Menos perda de progresso em uma interrupção | Encerrar uma sessão de teste abruptamente e medir a perda máxima |
| Salvamento com troca atômica e cópia anterior | Recuperação após escrita incompleta | Simular falha de escrita e recuperar a última cópia íntegra |
| Slots separados de save manual e automático | Repetir capítulos e preservar jornadas | Carregar cada slot sem misturar inventário ou missões |
| Tela de recuperação de save inválido | Erro compreensível com escolha segura | Abrir uma cópia corrompida sem substituir o arquivo original |
| Teste completo das 24 missões | Eliminar bloqueios e recompensas duplicadas | Percorrer cada requisito, horário e alternativa |
| Indicador discreto de salvamento | Deixar claro quando é seguro sair | Mostrar sucesso somente depois de concluir a gravação |
| Destino e próximo passo consistentes | Evitar dúvida entre mapa, HUD e caderno | Selecionar um objetivo e comparar os três locais |
| Pontos seguros validados no Editor | Evitar retorno dentro de paredes ou NPCs | Mover obstáculos e executar o validador de todas as regiões |
| Testes em um projeto clonado do Git | Instalação reproduzível em outro computador | Abrir um clone sem caches nem arquivos locais ocultos |

## Prioridade 2 — interface e acessibilidade

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Escala de toda a interface | Boa leitura em monitores e distâncias diferentes | Testar fontes, caixas e botões sem cortes em vários tamanhos |
| Controle/gamepad e remapeamento | Alternativas ao teclado e mouse | Completar o percurso inicial usando apenas cada dispositivo |
| Histórico das conversas | Rever informações sem repetir encontros inteiros | Consultar falas sem conceder recompensas novamente |
| Retomar conversas e atividades interrompidas | Continuidade mais completa do save | Reabrir cada estado intermediário sem perder ou duplicar ações |
| Contraste e símbolos além da cor | Objetivos legíveis para diferentes visões | Identificar estados da missão mesmo em tons de cinza |
| Volumes separados | Ajustar música, ambiente, efeitos e vozes | Salvar e recuperar cada ajuste individualmente |
| Ajuda contextual curta | Aprender um comando quando ele se torna necessário | Jogador novo realizar uma ação sem abrir um manual |
| Área segura para HUD em outras proporções | Evitar controles cortados ou cobrindo a cena | Testar 4:3, 16:9, ultrawide e janelas pequenas |
| Navegação e foco consistentes | Menus previsíveis | Abrir, fechar e voltar mantendo seleção útil e foco visível |

As orientações da Microsoft sobre texto, identificação do interlocutor, contraste, navegação e salvamento servem como critérios de avaliação. Isso não significa que o jogo já cumpra integralmente essas orientações. [Texto](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101) · [Acessibilidade e salvamento](https://learn.microsoft.com/en-us/gaming/game-publishing/concepts/metadata-accessibility).

## Prioridade 3 — mapa, mundo e exploração

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Rotas realmente percorríveis | Orientação que respeita saídas e bloqueios | Comparar rota sugerida com a travessia real |
| Marcadores pessoais | Anotar um lugar para voltar depois | Salvar e recuperar marcações sem interferir nas missões |
| Filtros para lugares, pessoas e objetivos | Mapa menos carregado | Encontrar uma informação em poucos passos |
| Lugares descobertos gradualmente | Recompensa pela exploração | Revelar somente o que foi visitado ou apresentado na história |
| Transições mais claras entre regiões | Evitar sensação de teletransporte confuso | Conferir placa, direção de entrada e posição de chegada |
| Placas e marcos reconhecíveis | Orientação pelo próprio cenário | Percorrer praça, comércio e rio com a HUD oculta |
| Pequenas interações ambientais | Dar vida a bancos, redes, portas e objetos | Cada interação ter resposta curta, útil e coerente |
| Percursos alternativos e atalhos | Valorizar a familiaridade com a cidade | Atalho reduzir retorno sem pular requisitos narrativos |

## Prioridade 4 — arte e atmosfera de Jaguaruana

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Guia visual com referências locais | Coerência de fachadas, vegetação e cores | Aprovar uma prancha antes de alterar cada conjunto |
| Detalhar texturas existentes | Mais qualidade sem mudar a identidade | Comparar antes/depois no mesmo zoom e horário |
| Ajustar escala de objetos | Relação convincente entre Neto, portas e mobiliário | Conferir um conjunto de medidas de referência |
| Sombras de contato por material | Objetos menos soltos sobre o chão | Avaliar paredes, árvores e bancos em cenas claras e escuras |
| Luz suave por horário | Diferenciar manhã, tarde e noite com legibilidade | Conferir sombras, tons e NPCs ao longo de um dia completo |
| Água, folhas e redes com movimento sutil | Ambiente vivo sem ruído visual | Comparar com movimento reduzido ligado/desligado |
| Contornos e ordenação das camadas | Evitar personagens atravessando visualmente árvores | Andar em torno de cada objeto alto |
| Retratos coerentes para os interlocutores | Mais personalidade nas conversas | Preservar os rostos aprovados e conferir troca de falante |

Eu começaria pelo piso, pelas paredes e pelas sombras da Praça Matriz. Aprovaria esse trecho em movimento antes de repetir o tratamento no resto do mapa.

## Prioridade 5 — personagens, narrativa e missões

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Voz própria de cada personagem | Falas menos genéricas | Ler diálogos sem o nome e reconhecer o interlocutor |
| Rotinas por horário | Encontros mais naturais | NPC nunca ficar inacessível para uma missão obrigatória |
| Falas que reagem ao progresso | Continuidade entre encontros | Verificar versões antes/depois de cada marco importante |
| Escolhas com consequências pequenas | Participação do jogador sem explodir o escopo | Consequência aparecer em fala, registro ou ambiente |
| Missões com ações variadas | Reduzir repetição de buscar e entregar | Alternar observação, conversa, exploração e tecelagem |
| Caderno com relações entre pistas | Tornar a investigação compreensível | Jogador explicar uma conclusão usando registros acessíveis |
| Revisão cultural com referências e moradores | Melhor representação de Jaguaruana | Registrar o que é referência, adaptação ou criação do roteiro |
| Encerramento e gancho de capítulo claros | Sensação de conclusão e expectativa | Testar o entendimento do objetivo alcançado e da próxima etapa |

## Prioridade 6 — bolsa, materiais e tecelagem

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Ícones próprios e descrição curta dos materiais | Reconhecer usos sem decorar nomes | Encontrar um item e entender sua função |
| Separar pistas, materiais e lembranças | Bolsa mais fácil de consultar | Buscar cada categoria com poucos passos |
| Comparar material disponível e necessário | Planejar a próxima ação no tear | Ver faltantes, origem e quantidade na mesma tela |
| Feedback de coleta e consumo | Evitar dúvida sobre o inventário | Conferir aumento/redução com notificação discreta |
| Padrões de tecelagem com dificuldade gradual | Atividade central mais interessante | Ensinar uma regra por vez e permitir repetir sem punição excessiva |
| Redes que reflitam escolhas e encontros | Personalização ligada à história | Relacionar um motivo visual a uma memória vivida |

## Prioridade 7 — som e movimento

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Passos por superfície | Reforçar chão, ponte e interiores | Ouvir transições coerentes sem som repetitivo excessivo |
| Ambientes próprios por região | Diferenciar praça, oficina e margem do rio | Reconhecer o lugar pelo som em uma comparação controlada |
| Transições musicais suaves | Evitar cortes bruscos | Entrar/sair de diálogo, noite e interior sem saltos de volume |
| Feedback sonoro de botões e tecelagem | Dar resposta clara às ações | Testar com música baixa e efeitos ajustáveis |
| Refinar início, parada e giro da caminhada | Movimento mais natural | Comparar toques curtos, diagonais e mudança de direção |
| Câmera previsível perto de paredes e conversas | Manter o foco sem deslocamentos incômodos | Testar bordas de regiões e opção de movimento reduzido |

## Prioridade 8 — programação, desempenho e entrega

| Melhoria | Resultado esperado | Como verificar |
| --- | --- | --- |
| Medir gargalos com o Profiler | Investir esforço onde há custo real | Comparar CPU, renderização, memória e áudio em uma build |
| Reduzir alocações e buscas repetidas na UI | Menos pausas por coleta de lixo | Medir uma sessão de exploração e abertura de menus |
| Perfis gráficos em computadores diferentes | Qualidade previsível | Testar ao menos uma máquina modesta e outra de referência |
| Carregamento e uso de memória das artes | Menos espera e menor consumo | Medir entrada de região, texturas residentes e pico de memória |
| Separar apresentação, narrativa e persistência | Código mais simples de manter | Alterar um texto sem tocar na lógica de recompensa |
| Validador de referências e missões | Encontrar IDs e ligações quebradas antes do Play | Introduzir uma referência inválida em cópia de teste e detectá-la |
| Builds versionadas e changelog | Saber qual versão foi testada | Reproduzir um bug usando versão e save de teste |
| Automatizar checagens e build no Git | Reduzir erros nas entregas | Compilar a partir de um clone limpo |
| Testes com novos jogadores | Encontrar confusões que a equipe não percebe | Observar o primeiro percurso sem instruções adicionais |

O Unity Profiler permite observar custos de scripts, renderização, memória e áudio e comparar mudanças. As otimizações devem partir dessas medições; não proponho uma troca geral de renderizador ou de arte como primeiro passo. [Documentação oficial](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler/introduction).

## Ordem de execução sugerida

1. Validar a revisão 18 na Unity, principalmente save antigo, posição, diálogos e recompensas.
2. Fechar o percurso inicial completo com orientação clara e teste de jogadores novos.
3. Fortalecer salvamento, recuperação de erros e testes das 24 missões.
4. Aprovar o refinamento de um pequeno trecho da Praça Matriz com as artes atuais.
5. Aplicar o padrão aprovado às outras regiões e acrescentar som ambiente.
6. Expandir rotinas, escolhas, tecelagem e novas áreas somente depois da base estar estável.

Nenhum item desta lista está marcado como implementado apenas por constar no plano. O guia da revisão 18 descreve o que foi entregue agora.
