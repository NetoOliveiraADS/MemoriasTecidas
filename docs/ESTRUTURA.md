# Onde editar cada parte

## Organização

| Caminho a partir da raiz | Função |
|---|---|
| `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` | Cena de trabalho e entrada da build |
| `Assets/MemoriasTecidas/MundoV2/Runtime` | Movimento, regras, interações, estado e interface base |
| `Assets/MemoriasTecidas/MundoV2/Editor` | Montagem original e reparos dos módulos atuais |
| `Assets/MemoriasTecidas/MundoV2/Data` | Dados usados pelo montador base |
| `Assets/MemoriasTecidas/MundoV2/Arte` | Imagens do mundo base |
| `Assets/MemoriasTecidas/MundoV2/Gerados` | Materiais, shaders e prefabs do mundo base |
| `Assets/MemoriasTecidas/MundoV2/MelhoriasV22` | Vida, animações, travessias e interface atualizada |
| `Assets/MemoriasTecidas/MundoV2/LugaresV23` | Praça Matriz, comércio, rio e beira-rio |
| `Assets/MemoriasTecidas/MundoV2/ExpansaoUrbanaV24` | Prefeitura, mercado, largo, entrada e monumentos |
| `Assets/MemoriasTecidas/MundoV2/NetoProtagonista` | Artes, recortes e visual do protagonista |
| `Assets/InputSystem_Actions.inputactions` | Asset de ações referenciado nas configurações |
| `Assets/TextMesh Pro` | Recursos e shaders do sistema de texto |
| `Packages` | Manifesto e lock de dependências |
| `ProjectSettings` | Configurações compartilhadas, inclusive build e versão do Unity |

Os nomes V22, V23 e V24 identificam módulos que se complementam. Não são três cópias completas do jogo. V24 usa componentes e materiais de V23 e V22. Os instaladores também usam caminhos fixos; renomear essas pastas exigiria uma migração de código e uma nova validação no Unity.

## Arquivos de código mais úteis

Os caminhos abaixo são relativos a `Assets/MemoriasTecidas/MundoV2/`.

| Arquivo | Responsabilidade |
|---|---|
| `Runtime/MTMJogo.cs` | Movimento, câmera, mudança de região, interações e salvamento |
| `Runtime/MTMEntrada.cs` | Teclado e atalhos |
| `Runtime/MTMEstado.cs` | Progresso, recursos, receita e objetivos |
| `Runtime/MTMRegiao.cs` | Dados e objetos de uma região |
| `Runtime/MTMPonto.cs` | Pontos de interação |
| `Runtime/MTMJogoInterface.cs` | Parte da interface base |
| `MelhoriasV22/Runtime/MTMJogoVidaV22.cs` | HUD e painéis da versão com melhorias |
| `NetoProtagonista/Runtime/MTMNetoVisual.cs` | Ajustes visuais do protagonista |
| `LugaresV23/Runtime/MTMSuperficieV23.cs` | Superfícies editáveis dos cenários |

`MTMJogo` é uma classe parcial: seu comportamento está distribuído entre arquivos. Excluir uma dessas partes quebra a classe, mesmo que pareça repetir o nome.

## Editar lugares e HUD

Pare o Play. Use **Memorias Tecidas > Expansao Urbana V2.4 > 4 - Abrir painel de edicao** para selecionar os lugares dessa expansão. Para os quatro lugares de V23, use o painel correspondente em **Lugares V2.3 > 5 - Abrir painel de edicao**.

Selecione a raiz da região e altere **MTMRegiao > Dados > Nome**. O campo `Id` identifica as conexões e deve manter seu valor. Para mudar o texto de uma saída, altere **MTMPonto > Dados > Nome** na interação correspondente. Salve a cena.

Editar `Data/locais.json` altera a fonte do montador; não atualiza automaticamente uma cena já construída. Se reconstruir a região, os dados do JSON podem substituir ajustes manuais.

## Editar câmera

Selecione `Explorador_MundoV2` e ajuste **MTMJogo > Tamanho Camera**. O código desta base já permite de 5 a 30. Um valor maior mostra mais cenário. Alterações diretamente no Size da câmera são sobrescritas durante o jogo.

## Conteúdo gerado e cache

**Gerados em Assets** é conteúdo de projeto: sprites recortados, materiais, shaders e prefabs usados pela cena. Preserve e envie para o Git.

**Library na raiz** é o cache local de importação. Deve ficar ignorado pelo Git e é reconstruído na outra máquina. Não confunda as duas pastas.

Os pacotes e seus locks foram mantidos como na versão recebida. Uma futura remoção de pacotes opcionais deve ser feita no Package Manager, seguida de Play e build, para o próprio Unity atualizar o lock de dependências.
