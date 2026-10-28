# Organizar e editar o projeto atual

Abra **Memorias Tecidas → Organizar projeto** com a cena Jaguaruana aberta e fora do Play.

## Primeira organização

Na aba **Visão geral**:

1. **Conferir regiões, personagens e alvos das missões** verifica IDs, referências, marcos e bloqueios que impediriam uma partida nova.
2. **Organizar interações em grupos na Hierarchy** agrupa raízes individuais em `01 · Personagens`, `02 · Interações` e `03 · Saídas`, dentro de cada região. Preserva coordenadas e os prefabs. Objetos ambíguos não são movidos automaticamente.
3. **Criar marcos de edição** adiciona `04 · Marcos do roteiro`, sem duplicar marcos existentes. São pontos de autoria para os 40 encontros criados pelo roteiro.
4. Salve a cena com Ctrl+S. Ctrl+Z desfaz os comandos de organização.

Cenário e decoração ficam onde já estão. O painel reúne as referências de conteúdo sem desmontar o mapa. Não use os montadores antigos de cidade para “atualizar” esta cena: eles podem reconstruir posições.

## Lugares

A aba **Lugares** permite editar o nome, o nome curto do mapa, o grupo, a descrição e a direção visual. Mostra os personagens e as missões que passam pelo local. **Mostrar somente este lugar** ativa a região escolhida para edição; o Play continua usando a região da partida salva.

Os grupos são: Portas da cidade, Centro e convivência, Saberes e ofícios, Paisagem e matéria-prima, Memória e investigação. A direção visual é uma ficha de trabalho; ela não recolore automaticamente o cenário existente.

## Personagens existentes — incluindo Dona Feliciana

1. Abra **Personagens**, pesquise o nome e selecione a ficha.
2. Clique **Selecionar personagem / marco completo**.
3. Para mover dentro do mesmo lugar, arraste o objeto selecionado com a ferramenta Mover (W). Não selecione somente o sprite/Desenho.
4. Para mudar de região, escolha **Levar para** e clique **Mover com a interação para este lugar**.
5. Ajuste a posição de chegada e salve a cena com Ctrl+S.
6. Entre no Play, visite a região e pressione E perto do personagem.

O comando move arte e interação, reindexa os pontos da região e mantém os IDs. As etapas e a ficha acompanham o local ao iniciar o Play. Regiões bloqueadas por uma etapa posterior são recusadas como destino de um personagem necessário antes dela.

Para Feliciana continua disponível o atalho **Memorias Tecidas → Personagens → Levar Dona Feliciana para a Entrada**. Sua participação na reunião final permanece na Praça, como convidada.

O painel não muda isoladamente o nome da conversa: esse nome também liga a arte ao diálogo. Edite papel, descrição e falas pelos campos disponíveis.

## Encontros criados pelo roteiro

Artesão, Visitante da Serra e os pontos de coleta, observação ou investigação são criados durante o Play. Para posicioná-los fora do Play:

1. Na Visão geral, crie os marcos de edição.
2. Abra **Encontros do roteiro**, escolha o encontro e clique em **Selecionar personagem / marco completo**.
3. Arraste o marco no Scene ou transfira pelo seletor de região.
4. Salve a cena. O círculo do gizmo só ajuda a editar; não vira uma bola no jogo.
5. Teste no Play. O objeto real aparece conforme as regras da missão e do horário.

O marco é um objeto sem arte/colisão. Não o duplique nem mude seu ID; não use a tag `EditorOnly`, pois a posição precisa ir no build. Sem um marco, o jogo continua usando `x`, `y` e `regiao` do JSON.

Ao deslocar o marco, patrulhas e escoltas recebem o mesmo deslocamento. O validador verifica seus limites; confira caminhos e obstáculos no Play. Ao mudar um encontro para um lugar narrativamente diferente, revise suas falas, pistas e descrições — os textos não são reescritos automaticamente.

## Missões e diálogos

Na aba **Missões**, edite título, capítulo e descrição. Abra uma etapa para editar seu objetivo, registro e falas de início/conclusão. Falas de conclusão só aparecem depois da atividade realizada.

O painel protege IDs, eventos, condições, pré-requisitos e recompensas. Essas regras continuam no JSON para edição avançada. Não reutilize nem renomeie IDs do roteiro publicado; eles estão ligados ao progresso salvo.

As 24 missões foram organizadas em quatro capítulos. As missões 11 e 15 continuam opcionais. A alteração do campo Capítulo organiza o caderno; ela não muda a ordem ou libera a missão.

A aba **Personagens** também permite editar falas livres. Uma fala específica de missão deve ser alterada em **Missões → etapa**.

## Bolsa e materiais

Na aba **Materiais e objetos**, edite o nome, a descrição, a origem, a dica e o ícone, com prévia do desenho. Categorias e IDs permanecem protegidos.

A receita continua: 3 fios preparados, 1 corda, 1 pano, 1 conjunto de punhos e 1 enfeite de palha. Fio simples, cera, fita e pistas não entram nessa receita. A origem no mapa acompanha a primeira entrega de missão quando ela é movida.

## Salvar conteúdo e salvar a cena

**Salvar conteúdo** escreve `trama_01.json`; cria antes uma cópia em uma pasta `NOME_DO_PROJETO_Backup_Conteudo` ao lado do projeto. O save do jogador não é editado pelo painel.

**Ctrl+S** salva posições e hierarquia da cena. São duas operações diferentes: faça ambas quando alterar conteúdo e posicionamento.

Rascunhos são recuperáveis durante a mesma sessão da Unity. Use **Exportar rascunho** se quiser guardá-los sem aplicar. Se o JSON mudou fora do painel, o salvamento é recusado para preservar a edição externa; exporte o rascunho, recarregue e compare os conteúdos. Salve ou exporte antes de fechar a Unity.
