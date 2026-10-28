# Diálogos e continuidade — revisão 18; instalação corrigida na revisão 19

Atualização de 28/10/2026 para o mesmo projeto. Inclui a HUD e o acabamento discreto da revisão 17.

## Diálogos

O rodapé de todos os encontros usa exatamente **Rede de Memorias - Memorias Tecidas**. A frase sobre cenas e diálogos ficcionais deixa de aparecer nas conversas, inclusive em pontos antigos da cena que ainda possuam essa informação cadastrada.

O cabeçalho mostra só o nome. Complementos como `· memória`, `(memória)` e `Memória de` são removidos na apresentação. Quando a fala começa com um personagem reconhecido, por exemplo `Neto:`, o nome aparece no cabeçalho e o corpo contém somente a fala. Textos como `A orientação comunitária diz:` continuam inteiros, pois são pistas, não nomes de personagens.

A caixa ganhou mais espaço de leitura, separador discreto, paginação e indicação de rolagem para textos extensos. O tamanho de texto configurado nas opções continua sendo respeitado. O retrato acompanha o interlocutor quando existe um recurso correspondente; Neto usa sua própria arte existente. Não foram gerados novos rostos.

Também foram revisados nove campos de texto: identificação, apresentação da primeira missão, orientações de Miguel, trechos da Serra e a descrição da montagem da rede. IDs, requisitos, recompensas, eventos e ordem das 24 missões permanecem iguais. O arquivo `REVISAO_TEXTOS_18.json` mostra cada mudança.

## Tela inicial e save

Com um save existente, o botão principal passa a ser **Continuar de onde parei**. Abaixo aparecem dia, região e data/hora do último salvamento, quando disponíveis. Sem save, aparece **Começar a jornada**. Nova jornada mantém a confirmação e o backup anterior.

O save agora inclui posição relativa à região, missão acompanhada e destino selecionado. Ao continuar, o jogo retoma esses dados junto do inventário, horário e progresso. Se a posição ficou inválida ou ocupada depois de editar o cenário, usa o ponto seguro configurado na região.

**Saves antigos continuam válidos**, mas não contêm coordenadas: na primeira retomada usam o ponto seguro da região salva. A posição exata passa a ser registrada ao salvar com esta versão. Conversas e atividades ainda não concluídas podem precisar ser retomadas desde seu início; callbacks e a fala aberta não são persistidos.

O salvamento ocorre nos encontros, nas mudanças de região, ao voltar à tela inicial, ao sair e quando o aplicativo é pausado pelo sistema. Esta revisão não acrescenta autosave periódico nem múltiplos slots. Fechar o processo à força pode perder o que ocorreu desde o último salvamento.

O painel de mapa do canto inferior direito foi removido da abertura. A ilustração original está intacta. O mapa continua disponível durante a exploração pela tecla M e pelo menu de pausa.

## Instalar

1. Feche a Unity e extraia o ZIP fora da pasta do projeto.
2. Execute `Instalar_Atualizacao.cmd` e indique a raiz que contém `Assets`, `Packages` e `ProjectSettings`.
3. Abra o mesmo projeto e `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
4. Execute **Memorias Tecidas → Interface → Validar interface e caminhada**. Essa ação também chama o novo validador de diálogos e continuidade.
5. Teste no Play antes de atualizar o Git.

A instalação padrão da revisão 19 preserva o script local de narrativa e altera somente valores JSON que coincidam integralmente com os trechos conhecidos da revisão editorial. Textos personalizados diferentes e os demais dados ficam intactos. Os `.meta` são preservados e há backup. Um conflito em outros arquivos interrompe a cópia. Não substitui cenas nem prefabs e não apaga saves. Consulte `CORRECAO_INSTALADOR_19.md`.

## Verificação no Play

- Sem save: confira **Começar a jornada**, tela inicial sem painel do mapa e funcionamento de M depois de entrar.
- Com save antigo: confira região, materiais e missões; a primeira retomada usa o ponto seguro.
- Caminhe para longe da entrada, selecione missão/destino, volte à tela inicial, saia do aplicativo e abra novamente. **Continuar** deve recuperar a posição, missão e destino salvos.
- Repita em um interior e em outra região. Teste uma posição que ficou bloqueada após mover um objeto e confira o retorno ao ponto seguro.
- Abra diálogos de Feliciana, Simão, Miguel e uma placa. Confira nomes, retratos, ausência de nome duplicado no corpo e rodapé exato.
- Use textos padrão/grande/maior, role uma fala extensa, volte à fala anterior e conclua uma missão. A recompensa deve entrar uma única vez, somente ao concluir.
- Teste mouse, Tab/Enter, E e Esc, em 1280×720 e 1920×1080.

`Previa_Menu_UIUX.png` e `Previa_Dialogo_18.png` são composições dos componentes com dados demonstrativos, não capturas da Unity.

**Limites da verificação disponível:** análise sintática, comparação dos dados de missão e recursos, inspeção das prévias e integridade dos arquivos. A compilação C#/shaders, o instalador Windows, o validador dentro da Unity e o Play/build ainda precisam ser executados.

As possibilidades de evolução estão em `PLANO_MELHORIAS_FUTURAS.md`. Elas são propostas e não fazem parte da implementação desta revisão.
