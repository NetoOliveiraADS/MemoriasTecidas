# Substituir o conteúdo mantendo o mesmo projeto

Este pacote contém o projeto organizado completo na pasta **`Projeto_Limpo`**. Você vai colocar essas pastas dentro da pasta original que já usa no Unity e no GitHub.

**Base: o RAR enviado em 14/10/2026. Alterações feitas no seu computador depois desse envio não estão automaticamente nesta versão.** Guarde o conteúdo atual no backup abaixo para poder recuperar essas alterações.

## 1. Fechar o Unity

Na janela **Enter Safe Mode?** do print, clique em **Quit**. Feche também qualquer outra janela do Unity aberta nesse projeto.

O aviso informa que existem erros de compilação. Ele não identifica a causa. A substituição completa permite retirar os scripts antigos misturados sem depender dos painéis de limpeza.

## 2. Separar a origem e o destino

1. Extraia o ZIP desta entrega em uma pasta temporária **fora do projeto atual**.
2. Abra `Projeto_Limpo` dentro da pasta extraída. Ela contém `Assets`, `Packages`, `ProjectSettings`, `README.md` e `docs`.
3. Em outra janela do Explorador, abra a **pasta original do seu projeto** — aquela que você já usa no GitHub. Ela também contém `Assets`, `Packages` e `ProjectSettings`.
4. Crie uma terceira pasta, **fora da pasta do repositório**, chamada `MemoriasTecidas_Backup_Antes_Da_Troca`.

Você terá três locais diferentes: a origem limpa, o projeto atual e o backup. O destino é sempre a pasta original do projeto.

## 3. Retirar o conteúdo antigo do projeto atual

Com o Unity fechado, **recorte** os itens abaixo da pasta original e **cole no backup**, se existirem:

| Item na raiz do projeto | Motivo |
|---|---|
| `Assets` | Será substituída integralmente pelas cenas, scripts, artes e metadados organizados. |
| `Packages` | Receberá o manifesto e o lock compatíveis com a versão entregue. |
| `ProjectSettings` | Receberá as configurações e a cena correta na lista de build. |
| `README.md` e `docs` | Os guias novos entram no lugar dos anteriores; suas anotações ficam recuperáveis no backup. |
| `Library` | Cache antigo; o Unity criará um novo ao abrir. |
| `Temp`, `obj`/`Obj`, `Logs` e `UserSettings` | Arquivos temporários e estado local anterior, quando presentes. |

Aguarde cada movimentação terminar antes de continuar. **Não renomeie a pasta antiga de Assets para `Assets/Backup` ou outra pasta dentro do projeto.** Scripts e recursos antigos devem ficar fora da árvore que o Unity importa.

Mantenha na pasta original:

- **`.git`**, seja uma pasta ou um arquivo. Se o repositório estiver numa pasta acima, mantenha também esse local intacto.
- **`.gitignore`, `.gitattributes` e `.github`**, quando existirem.
- Licenças e outros arquivos de configuração do seu repositório.

No Windows, `.git` pode estar oculta. Use **Exibir → Mostrar → Itens ocultos** para localizá-la. A troca proposta não exige selecionar nem mover esse item.

Se existirem `.sln`, `.slnx`, `.csproj` ou `.vs` gerados pelo Unity na raiz, você pode movê-los para o backup também; a integração com o editor de código poderá recriá-los. Não é necessário fazer isso para a troca dos assets.

## 4. Colocar o conteúdo limpo

Copie **o conteúdo de `Projeto_Limpo`** para dentro da pasta original:

- `Assets`
- `Packages`
- `ProjectSettings`
- `README.md`
- `docs`

Ao terminar, esses itens devem estar diretamente na raiz do projeto, ao lado do vínculo Git preservado. **Não coloque a pasta `Projeto_Limpo` dentro de `Assets` e não copie uma pasta Assets para dentro de outra Assets.**

A troca é de pastas inteiras. Colar por cima e escolher “Substituir arquivos” mantém arquivos antigos que não têm equivalente no pacote, o que pode conservar scripts duplicados e erros.

Este pacote não contém `.git`, `.gitignore`, `.gitattributes` nem `.github`; portanto, a cópia dele não altera suas configurações do repositório.

## 5. Reabrir o mesmo projeto

1. No Unity Hub, abra **a entrada original do projeto**, com o mesmo caminho de antes.
2. Use o Editor **6000.3.25f1**. A versão também está em `ProjectSettings/ProjectVersion.txt`.
3. Mantenha acesso à internet para resolver os pacotes e aguarde a importação inicial. A recriação de `Library` pode demorar.
4. No painel Project, abra **Assets → MemoriasTecidas → Cenas → Jaguaruana**.
5. Pressione **Play** e clique na aba Game para dar foco ao teclado.

A cena principal agora está em **`Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`**. Ela já contém os módulos e o protagonista. Não é necessário executar menus de montagem ou instalar novamente os pacotes antigos da conversa.

As ferramentas `MTOrganizarArquivosAtuais` e `MTMOrganizadorSeguro` não fazem parte desta entrega. Por isso, seus menus de organização anteriores deixam de aparecer depois da troca.

## 6. Conferir o jogo antes do Git

Teste:

1. Caminhar e correr nas oito direções, com a câmera acompanhando o personagem.
2. Passagens entre lugares, inclusive ida e volta para a Prefeitura e a Praça.
3. Conversas, mapa, diário e bolsa.
4. Coleta de materiais e fabricação no tear.
5. Cenário, monumentos e recursos de texto, sem objetos ou materiais ausentes.

Confira também a lista de cenas em **File → Build Profiles**. `Cenas/Jaguaruana.unity` deve estar habilitada e ser a primeira. Se um perfil tiver sua própria lista, revise essa lista também.

O Git continuará vinculado ao mesmo repositório. Depois de testar, revise as adições, remoções e alterações antes de fazer commit e push. Não é necessário criar outro repositório ou executar `git init` novamente.

## Se o Safe Mode continuar

Escolha **Enter Safe Mode**, abra **Window → General → Console**, selecione a **primeira mensagem vermelha** e copie o texto completo, incluindo o arquivo e a linha. Essa informação permite identificar a causa; o print da janela de entrada não basta.

Confirme também que abriu o caminho original com Unity 6000.3.25f1 e que esperou a resolução dos pacotes. Esta entrega foi verificada estaticamente, mas **não foi compilada nem executada no Unity neste ambiente**.

## Recuperar a versão anterior

1. Feche o Unity.
2. Mova as pastas da versão recém-instalada para outra pasta externa, caso queira guardá-las.
3. Devolva `Assets`, `Packages`, `ProjectSettings`, `README.md` e `docs` do backup aos mesmos caminhos originais. Mova também os demais itens que desejar restaurar.
4. Mantenha todos os `.meta` junto dos respectivos arquivos e pastas.
5. Reabra o projeto com a versão correta do Editor.

O progresso salvo pelo jogo fica fora do projeto, em `Application.persistentDataPath`. Esta troca de pastas não apaga esse progresso.

## O que foi conferido nesta entrega

- Arquivos de código, arte, pacotes e configurações comparados com o RAR original.
- Metadados, GUIDs e referências locais da cena principal.
- Cena principal na lista geral de build.
- Sintaxe C# por parser e declarações de tipos duplicadas.
- Conteúdo completo das imagens, sem ponteiros LFS no lugar dos arquivos.

Essas verificações não substituem a compilação, o Play ou a build no Editor. O backup é a forma de preservar ajustes mais recentes que o RAR de referência.

Referências: [Safe Mode](https://docs.unity3d.com/6000.3/Documentation/Manual/SafeMode.html), [metadados dos assets](https://docs.unity3d.com/6000.3/Documentation/Manual/AssetMetadata.html).
