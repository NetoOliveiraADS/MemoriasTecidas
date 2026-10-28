# Correção da instalação do menu e do mapa

## O que apareceu

O instalador anterior informou:

`Nada foi alterado. Estes arquivos diferem das versões recebidas: Assets/MemoriasTecidas/MundoV2/Runtime/MTMJogoNarrativa.cs`

Isso significa que o arquivo no computador tem conteúdo diferente das versões conhecidas pelo pacote. A mensagem não indica um erro de compilação da Unity. A instalação foi interrompida antes da cópia para não substituir essa versão sem comparação.

As versões de narrativa incluídas nos três pacotes disponíveis aqui — antes da interface, depois da interface e depois do mapa — são iguais. A atualização do menu não precisa modificar esse arquivo. Não foi possível determinar o conteúdo da edição local apenas pela captura de tela.

## Como aplicar esta versão

1. Feche a Unity.
2. Baixe novamente este ZIP corrigido e extraia em uma pasta nova fora do projeto. Use o instalador dessa nova extração.
3. Execute **Instalar_Atualizacao.cmd**.
4. Quando solicitado, informe a raiz do projeto. No computador da captura, é `C:\Users\netol\MemoriasTecidas`.
5. Aguarde a mensagem de sucesso. Abra o mesmo projeto e a cena Jaguaruana.
6. Execute **Memorias Tecidas → Interface → Validar recursos dos menus**, entre no Play e clique em **Explorar o mapa**.

O instalador agora seleciona `manifesto_menu_mapa.json`, que contém os arquivos de menu, interface, mapa, fontes e documentação correspondentes. Inclui as mudanças necessárias da etapa anterior de UI para quem ainda não aplicou essa etapa.

## O que é preservado

- `MTMJogoNarrativa.cs`, inclusive a edição local que gerou o conflito.
- Os JSONs de missões e diálogos, incluindo `trama_01.json` e `abertura_memorias.json`.
- Cenas, prefabs, posições de NPCs e progresso salvo.
- Os `.meta` já presentes no projeto.

Os arquivos selecionados continuam passando pela comparação de conteúdo. Se algum deles tiver uma edição local desconhecida, a instalação para antes de copiar; não há opção para forçar substituições. Os arquivos que mudam recebem backup antes da cópia, e o instalador mantém a reversão se houver falha durante a instalação.

## Compatibilidade

O projeto deve já conter a campanha de 24 missões, a abertura Primeiro Fio e os módulos de apresentação usados pela interface. O instalador verifica a presença desses módulos. Essa conferência não compila o projeto nem garante compatibilidade com alterações locais desconhecidas; valide no Play após a instalação.

Se aparecer outro conflito, envie o arquivo `.cs` indicado na mensagem para permitir uma mesclagem. Se aparecer uma lista de dependências ausentes, envie a lista para adaptar a atualização à sua base.

## Modo cumulativo completo

Os arquivos anteriores permanecem no ZIP. Para uma atualização completa deliberada, o script PowerShell aceita `-Escopo Completo`. Esse modo preserva a conferência original de todos os arquivos e continua bloqueando a narrativa desconhecida. Ele não é necessário para adicionar o mapa.

## Verificação desta correção

Foram conferidos o escopo de instalação, a inclusão de todos os arquivos alterados pela interface e pelo mapa, a exclusão da narrativa e dos dados de campanha do escopo padrão, as versões aceitas e a integridade do ZIP. Não foram alterados scripts C# nesta correção. PowerShell e Unity não estavam disponíveis aqui; o instalador e o Play precisam ser executados no computador do projeto.
