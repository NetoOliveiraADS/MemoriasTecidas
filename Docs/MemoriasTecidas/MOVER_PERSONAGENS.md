# Correção — Dona Feliciana na Entrada da Cidade

Esta versão da atualização das **24 missões** inclui a correção de transferência. Corrige a ligação entre a hierarquia da cena, a lista de interações de cada região e os objetivos do roteiro. Não precisa criar outro projeto nem reiniciar o save.

A correção já está incluída neste pacote completo. Instale pela orientação de `LEIA_PRIMEIRO.md`, na raiz do ZIP, e use o comando abaixo na Unity.

## Transferir a personagem e o diálogo

1. Abra o projeto na Unity e aguarde a compilação.
2. Abra a cena **Jaguaruana**, fora do Play.
3. Use **Memorias Tecidas → Personagens → Levar Dona Feliciana para a Entrada**.
4. Salve a cena com **Ctrl + S**.
5. Entre no Play, vá à **Entrada da Cidade**, aproxime-se da Feliciana e pressione **E**.

O comando move a personagem existente com sua interação, aproxima o ponto do corpo, ajusta o alcance e atualiza as listas das regiões. Se ela já estiver dentro da região Entrada, mantém sua posição e corrige as ligações. Se estiver em outra região, coloca-a perto do ponto seguro da Entrada; você pode ajustar a posição do objeto selecionado depois.

A Entrada fica visível no Editor para você conferir a posição. Ao começar o Play, o jogo continua na área registrada no save; o comando não muda o ponto de início do jogador. **Ctrl + Z** desfaz a transferência no Editor.

As etapas com Feliciana nas missões **1 e 20**, a ficha da personagem e a indicação no mapa passam a usar a região real. Simão continua na Praça. Na missão 24, uma representação de Feliciana participa da reunião na Praça sem alterar seu lugar habitual na Entrada.

## Se o diálogo ainda não aparecer

- Use o objeto principal **Dona Feliciana · memória**, que contém a arte e `Interacao_npc_feliciana`. O comando faz essa ligação pelo ID existente.
- O ID do componente **MTMPonto** deve continuar sendo `npc_feliciana` e deve existir uma única interação com esse ID. Se você duplicou a personagem, o comando informa a duplicação e não escolhe uma cópia arbitrariamente.
- Pressione **F1** se a interface estiver oculta. Aproximar-se mostra o comando de interação; **E/Enter** abre a conversa.
- A conversa inicial exige o dia. À noite, a interação orienta a voltar no horário correto.
- Se já concluiu o encontro inicial, a personagem pode repetir um registro anterior; o pacote preserva as missões concluídas.
- Confira primeiro qualquer erro vermelho de compilação no Console. Não é necessário apagar saves ou criar outra cena para corrigir referências.

## Próximas mudanças de posição

Para mover dentro da mesma região, mova o objeto principal na Hierarchy e salve a cena. Para transferir um personagem que já existe na cena, mova o objeto principal inteiro para dentro do objeto da região de destino, que possui o componente **MTMRegiao**, e salve.

Ao iniciar o jogo, a correção refaz as listas e atualiza a região das etapas pelo ID do ponto. Preserve os IDs. Textos personalizados que citam um lugar por extenso podem precisar de revisão. Personagens gerados pelo roteiro durante o Play continuam sendo posicionados em `pontosNovos`, no JSON; não salve cópias desses personagens na cena.

## Validação

Execute, fora do Play:

**Memorias Tecidas → Personagens → Validar transferencia de dialogos**

A verificação usa objetos temporários para conferir uma transferência entre regiões, inclusive desativadas, referências antigas, missões 1/20, ficha, reunião final, duplicatas e retorno à região anterior. Não modifica saves. Depois confira a interação na cena real, pois alcance e colisões dependem de onde você posicionou o personagem.

Neste ambiente passaram a análise sintática dos 38 scripts combinados e a simulação das referências da cena. O Editor, a compilação C#, o Play e o instalador PowerShell não foram executados aqui.

## Git e backup

Depois de salvar a cena e testar, confira `git status`. A atualização inclui scripts e `.meta` novos, além da alteração em `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` feita pelo comando na Unity. Inclua esses arquivos no commit; o save pessoal e a pasta de backup ficam fora dele.

Para desfazer a instalação, siga a orientação de backup em `LEIA_PRIMEIRO.md`, na raiz do pacote completo. Para desfazer somente a transferência antes de salvar/fechar a cena, use Ctrl + Z.
