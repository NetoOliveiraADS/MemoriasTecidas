# Correção da instalação — revisão 19

Data: 28/10/2026. Esta revisão corrige a instalação da revisão 18; todos os seus recursos de interface e continuidade continuam incluídos.

## O que causou a interrupção

O instalador reconheceu versões locais diferentes de `trama_01.json` e `MTMJogoNarrativa.cs`. A revisão 18 colocou esses arquivos no conjunto de substituições, embora a instalação anterior de interface preservasse a narrativa local. O controle de conflitos interrompeu o processo antes de copiar. A captura recebida não indica erro de compilação da Unity nem perda de arquivos.

## Como esta revisão resolve

Na instalação padrão, por `Instalar_Atualizacao.cmd`:

- `MTMJogoNarrativa.cs` existente permanece inteiro, byte por byte. O rodapé pedido já é aplicado pelo sistema de apresentação dos diálogos, independentemente do texto de contexto recebido desse script.
- `trama_01.json` e `abertura_memorias.json` são lidos do seu projeto. Somente valores de texto que coincidam integralmente com os nove trechos de `REVISAO_TEXTOS_18.json` recebem sua revisão correspondente. Campos novos, textos personalizados diferentes, IDs, eventos, recompensas, requisitos e ordem são preservados.
- O JSON local não é reformatado nem substituído pelo arquivo do pacote. A revisão mantém espaços, quebras de linha e a presença de BOM UTF-8. Textos com escapes JSON são reconhecidos pelo valor da frase.
- Trechos diferentes ou inexistentes ficam como estavam. Isso não bloqueia a instalação. Portanto, não é possível garantir uma revisão editorial de falas personalizadas que não foram recebidas; a assinatura padrão na caixa de diálogo continua aplicada.
- Outros arquivos continuam protegidos pela verificação de versões. Um conflito em outro script interrompe tudo antes da primeira cópia.
- Todos os arquivos que serão alterados recebem backup ao lado do projeto. O instalador verifica o resultado e tenta restaurar o backup se uma cópia falhar.

O modo manual `-Escopo Completo` mantém o controle estrito do pacote cumulativo. Para esta correção, use o instalador padrão, sem acrescentar esse argumento.

## Aplicar

1. Feche a Unity.
2. Extraia o ZIP atualizado em uma pasta nova, fora do projeto. Use o instalador dessa nova pasta.
3. Execute `Instalar_Atualizacao.cmd` e indique a raiz do projeto, que contém `Assets`, `Packages` e `ProjectSettings`.
4. Confirme que a janela mostra **Revisao 19**. A mensagem final informa o backup e quantos trechos foram revisados.
5. Reabra o mesmo projeto, execute **Memorias Tecidas → Interface → Validar interface e caminhada** e teste no Play.

Não é necessário apagar os dois arquivos citados, criar um projeto novo ou instalar primeiro a revisão 18. O pacote é cumulativo para a interface.

Teste especialmente: rodapé dos diálogos, nome de quem fala, missão local personalizada, posição ao continuar e tela inicial sem o painel de mapa. Cenas, prefabs, posições dos NPCs e saves não são apagados por esta correção.

## Verificação e limites

O relatório `VERIFICACAO_INSTALADOR_19.json` separa os testes executados dos pendentes. Os arquivos locais do seu computador não foram recebidos; a reprodução usa projetos temporários com alterações simuladas nos mesmos arquivos.

A compilação C#/shaders, os validadores no Editor e o Play/build ainda precisam ser executados na Unity. Testes com PowerShell no Linux não equivalem a executar o instalador no Windows PowerShell 5.1.
