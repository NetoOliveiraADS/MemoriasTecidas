# Abrir em outro computador

Este guia usa Windows como exemplo. O projeto-fonte é compartilhável entre computadores, mas cada plataforma de build precisa do módulo correspondente instalado no Unity Hub. Esta entrega ainda precisa de teste em Play e build no Editor.

## 1. Instalar os programas

| Programa | Quando é necessário |
|---|---|
| Unity Hub e Unity **6000.3.25f1** | Para abrir e editar o projeto |
| Git | Para clonar e enviar alterações ao repositório |
| Git LFS | Para obter e versionar as imagens e demais binários configurados |
| Editor de C# | Somente para editar scripts; use o de sua preferência |

No Hub, instale exatamente a versão indicada. Se ela não aparecer na lista padrão, use o [arquivo de versões do Unity](https://unity.com/releases/editor/archive). Entre com sua conta e ative uma licença compatível com seu uso. Instale também o suporte da plataforma para a qual pretende gerar uma build. Não atualize o Editor apenas para abrir esta entrega.

Na primeira abertura, mantenha acesso à internet para que o Package Manager resolva os pacotes. A pasta `Library` não vem no repositório: o Unity criará o cache neste computador.

## 2. Obter os arquivos

### Pelo ZIP desta entrega

Extraia o ZIP. Para uma instalação em outro computador, use a pasta `Projeto_Limpo`, que contém `Assets`, `Packages`, `ProjectSettings` e `README.md`. Você pode renomeá-la para `MemoriasTecidas` antes de adicioná-la ao Hub. Para substituir o projeto atual, siga [o guia de substituição](SUBSTITUIR_NO_PROJETO_ATUAL.md).

Se você já fez alterações locais depois da versão enviada para organização, elas não estão automaticamente neste ZIP. Mantenha sua pasta atual e compare as mudanças antes de adotar a cópia preparada.

### Pelo GitHub

No GitHub, copie a URL HTTPS em **Code**. Abra Git Bash na pasta onde quer guardar seus projetos. Substitua `URL_DO_REPOSITORIO` pela URL copiada:

```bash
git lfs install
git clone "URL_DO_REPOSITORIO" MemoriasTecidas
cd MemoriasTecidas
git lfs pull
git status
```

Se o repositório for privado, use uma conta que tenha acesso. O Git pode abrir o navegador para autenticar. Não coloque senhas ou tokens no código.

O GitHub Desktop também permite **File > Clone repository > URL**. Depois da clonagem, confira se as imagens são arquivos de imagem reais. Caso estejam ausentes, execute `git lfs pull` dentro da pasta clonada com o Git LFS instalado.

## 3. Adicionar ao Unity Hub

1. No Hub, abra **Projects** e escolha **Add / Add project from disk**.
2. Selecione a pasta que contém as três pastas do projeto. Não selecione `Assets` nem o arquivo `.unity`.
3. Selecione Unity **6000.3.25f1** para esse projeto.
4. Aguarde a importação de imagens, pacotes e scripts. A primeira abertura pode demorar mais.
5. No Editor, abra **Window > General > Console** e confira se existem erros vermelhos.

O projeto usa **Built-in Render Pipeline**. Não faça conversão para URP durante a instalação.

## 4. Abrir e jogar

No painel Project, siga **Assets > MemoriasTecidas > Cenas** e abra **Jaguaruana**. O caminho completo é:

```text
Assets/MemoriasTecidas/Cenas/Jaguaruana.unity
```

Pressione **Play**, selecione a aba **Game** e clique nela para dar foco ao teclado. Use WASD/setas para andar, Shift para correr e E/Enter para interagir. M abre o mapa, J o diário e I a bolsa. Esc fecha painéis. R volta ao ponto seguro da região; Q altera os efeitos e F1 alterna a interface.

**A cena já contém as expansões.** Os menus antigos de montagem servem para reconstrução e manutenção. Rodá-los novamente pode substituir edições feitas no cenário.

É normal a Hierarchy ter regiões inativas: o jogo ativa apenas a região atual. Não habilite todas ao mesmo tempo.

## 5. Teste de aceitação

Use uma sessão de teste e anote os resultados:

| Verificação | Resultado esperado |
|---|---|
| Abrir a cena e entrar em Play | Sem erros vermelhos; câmera acompanha o protagonista |
| Andar e correr nas oito direções | Pernas animadas; orientação coerente |
| Cruzar saídas e voltar | Chegada em posição acessível, sem bloqueio |
| Mercado > Prefeitura e Praça > Mercado | Caminho de ida e volta funcional |
| Praça da Prefeitura e entrada | Monumentos apoiados e cenário sem arte ausente |
| Conversas, mapa, diário e bolsa | Painéis abrem, recebem entrada e fecham |
| Coleta, encomendas e tear | Progresso e fabricação funcionam no fluxo da missão |
| Encerrar e reabrir | Progresso carregado quando essa opção está habilitada |
| Build independente | Mesmo fluxo principal sem depender do Editor |

## 6. Criar a versão executável

1. Pare o Play e salve a cena.
2. Abra **File > Build Profiles**.
3. Selecione a plataforma desejada; instale o módulo pelo Hub se necessário.
4. Confira a lista de cenas do perfil. `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` deve estar habilitada e ser a primeira. Um perfil com lista própria pode substituir a lista geral do projeto.
5. Gere a build em `Builds/Windows` ou na pasta da plataforma escolhida.
6. Abra o executável fora do Unity e repita os testes principais.

Para alguém apenas jogar, distribua a **pasta inteira da build**, compactada. No Windows, o `.exe` precisa dos arquivos e pastas produzidos ao lado dele, incluindo a pasta `_Data`. O jogador não precisa instalar Unity. O ZIP do projeto-fonte desta entrega não é uma build.

## 7. Progresso salvo

O jogo usa `Application.persistentDataPath`, fora da pasta do repositório. Com os nomes atuais do projeto, no Windows o local esperado é:

```text
%USERPROFILE%\AppData\LocalLow\DefaultCompany\MemoriasTecidas\
```

O arquivo é `memorias-tecidas-mundo-v2.json`. Ele não acompanha um clone. Para levar o progresso, feche o jogo nos dois computadores e copie esse arquivo separadamente. O código pode importar `memorias-tecidas-mundo-v1.json` se não encontrar o arquivo V2. Para testar sem carregar/gravar progresso, desmarque **Carregar Progresso** no componente MTMJogo do Explorador e evite salvar essa configuração de teste por engano.

## 8. Problemas comuns

| Sintoma | O que conferir |
|---|---|
| Hub não reconhece a pasta | Selecione a raiz que contém `Assets`, `Packages` e `ProjectSettings` |
| Editor sugere outra versão | Instale 6000.3.25f1; confira `ProjectVersion.txt` |
| Safe Mode | Abra o primeiro erro da Console; aguarde os pacotes e confirme a versão. Não apague scripts para esconder o erro |
| Imagens ausentes ou erro ao importar PNG | Execute `git lfs pull` e confirme acesso ao repositório/LFS |
| Cenário vazio ou antigo | Abra `Cenas/Jaguaruana.unity`; não recrie o mundo pelo montador |
| Personagem não se move | Clique na aba Game; feche diálogos e painéis com Esc |
| Prefeitura não aparece de início | Use a passagem do Mercado ou o painel de edição da Expansão Urbana |
| Material rosa | Confirme arquivos de shader/material e Built-in; não converta o pipeline durante a instalação |
| Começa em um lugar inesperado | Pode ser o progresso local salvo; consulte a seção anterior |
| Módulo de build indisponível | Instale o suporte da plataforma no Hub para a mesma versão do Editor |

Se precisar relatar um erro, informe versão do Unity, cena aberta, passos para reproduzir e a primeira mensagem vermelha completa. Preserve o projeto antes de qualquer tentativa de limpeza adicional.

## Referências oficiais

- [Unity Hub](https://docs.unity.com/en-us/hub)
- [Versões do Unity](https://unity.com/releases/editor/archive)
- [Clonar repositório](https://docs.github.com/articles/cloning-a-repository)
- [Instalar Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/installing-git-large-file-storage)
- [Build Profiles](https://docs.unity3d.com/6000.3/Documentation/Manual/BuildSettings.html)
- [Dados persistentes](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-persistentDataPath.html)
