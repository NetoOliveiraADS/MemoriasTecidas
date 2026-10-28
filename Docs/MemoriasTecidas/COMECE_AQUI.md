# Memórias Tecidas — começar em outro computador

Jogo 2D de exploração, encontros e tecelagem em uma representação artística de Jaguaruana. O projeto usa Unity **6000.3.25f1**. Falas, pistas e encontros são dramatizações do roteiro; não devem ser apresentados como citações históricas autênticas.

## Baixar o projeto

Instale Git, Git LFS e Unity Hub. Pelo terminal:

```bash
git lfs install
git clone https://github.com/NetoOliveiraADS/MemoriasTecidas.git
cd MemoriasTecidas
git lfs pull
```

O Git LFS baixa as imagens reais quando elas são versionadas por LFS. Um arquivo `.png` que começa com `version https://git-lfs.github.com/spec/v1` ainda é um ponteiro, não a imagem. Baixar apenas o ZIP do GitHub pode deixar esses ponteiros no lugar das artes.

Na Unity Hub, instale a versão indicada em `ProjectSettings/ProjectVersion.txt`, escolha **Add / Add project from disk** e selecione a pasta que contém `Assets`, `Packages` e `ProjectSettings`. Não crie um novo projeto e não copie a pasta inteira dentro de `Assets`.

Aguarde a importação, abra `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` e clique em Play. Se esta atualização ainda não estiver no repositório, aplique-a antes de abrir o projeto.

## Controles

| Tecla | Ação |
| --- | --- |
| WASD / setas | Andar |
| Shift | Correr |
| E / Enter | Interagir ou continuar conversa |
| B / I | Bolsa |
| M | Mapa |
| J | Caderno de missões, pessoas, memórias e pistas |
| Esc | Fechar painel / pausar / sair da atividade |
| F1 | Mostrar ou ocultar HUD |
| R | Retornar ao caminho seguro |
| Q | Alternar qualidade dos efeitos |
| Roda do mouse / + / − | Ajustar a distância da câmera durante a exploração |

Na bolsa, clique no campo de busca e digite o nome do objeto. Enquanto o campo tem foco, as letras não acionam atalhos. Clique fora do campo para usar os atalhos novamente; Esc fecha o painel.

Aproxime-se do NPC e pressione E quando aparecer a indicação. Arrastar somente a arte sem a interação não transfere uma conversa; veja `ORGANIZAR_E_EDITAR.md`.

Para configurar brilho, qualidade e conforto de movimento, use **Esc → Imagem e câmera**. Para editar materiais, pisos, luzes e composição, consulte `EVOLUCAO_VISUAL.md`.

## Estrutura do código atual

| Caminho | Conteúdo |
| --- | --- |
| `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` | Cidade, posições dos personagens e marcos de autoria |
| `Assets/MemoriasTecidas/MundoV2/Runtime` | Execução do jogo, interface e regras |
| `Assets/MemoriasTecidas/MundoV2/Editor` | Painel de organização e validadores da Unity |
| `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/trama_01.json` | Missões, falas, itens, lugares, encontros, paleta e atividades |
| `Docs/MemoriasTecidas` | Guias de edição e verificação |

As pastas anteriores de arte continuam como dependências da cena e dos prefabs. Não exclua pastas por conterem números de versão no nome. Reorganizar fisicamente assets exige preservar seus `.meta` e verificar todas as referências na Unity.

## Progresso

O save é individual de cada computador. Ele não deve ser enviado ao Git. Use **Memorias Tecidas → Roteiro do PDF → Abrir pasta do progresso** para localizá-lo.

Atualizar de uma versão que já tem as 24 missões mantém seu progresso. A migração de versões mais antigas mantém os itens/rede, faz backup do save e inicia as etapas novas que não existiam. Uma nova jornada pelo menu de pausa substitui o progresso atual depois da confirmação.

## Primeira experiência

Abra J → Rede para acompanhar O primeiro fio. O guia `PRIMEIRO_FIO.md` explica o encontro com Feliciana e os novos controles. Em um save já avançado, você pode experimentar sem reiniciar.


## Interface atual

Ao dar Play em Jaguaruana, use a tela inicial para começar ou continuar. Opções e Como jogar ficam disponíveis antes da exploração. As fontes e a arte acompanham o projeto. Veja `UI_UX.md` para controles e testes.

## Continuar a jornada — revisão 18

Use **Continuar de onde parei** na tela inicial. A posição e o objetivo passam a acompanhar os novos salvamentos. Saves antigos mantêm seu progresso, com a primeira retomada no ponto seguro da região. Veja `DIALOGOS_E_CONTINUAR.md`.
