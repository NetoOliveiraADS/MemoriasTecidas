# Primeiro fio: interação, leitura e apresentação

A abertura passa a alternar conversa, observação, tecelagem e reencontro com Dona Feliciana. As 24 missões, os nove motivos da rede e o roteiro histórico existente permanecem na campanha.

## O que mudou

- HUD com um objetivo, uma ação próxima ao personagem e um conjunto de atalhos. O contador de missões deixa de aparecer como contagem de memórias. A receita entra no HUD depois de ser apresentada na campanha.
- Diálogo com altura adaptada ao texto, sem a assinatura repetida do projeto. Falas longas continuam roláveis; o tamanho de letra escolhido permanece respeitado.
- Retratos enquadrados em busto, usando as imagens existentes.
- Primeiro encontro com duas falas antes do convite à atividade. As sete falas completas permanecem no roteiro e no caderno.
- Bastidor com lançadeira, trama visível e sentido alternado. Selecionar a cor prepara o fio; passar até a outra borda completa a linha.
- Correção de uma linha individual. A conferência indica a primeira diferença sem apagar o restante.
- Salvamento de passagens completas, correções, desfazer e recomeçar. Um gesto interrompido não grava uma linha parcial. Saves sem o novo campo continuam aceitos.
- A pequena amostra gerada no cenário acompanha as linhas tecidas. O reencontro registra a memória pela progressão existente.
- Câmera aproxima a conversa e mantém os interlocutores acima da caixa de texto. Respeita os limites do mapa, o zoom escolhido e reduzir movimento.
- Protagonista olha para o interlocutor. O reconhecimento de fala dos moradores aceita os sufixos que identificam memórias.
- Contato suave nos pés e suavização localizada do asfalto/mosaico nas qualidades com efeitos, controlada pelo acabamento natural.

## Controles da tecelagem

| Ação | Mouse | Teclado |
| --- | --- | --- |
| Escolher fio | Clicar na cor | 1, 2 ou 3 |
| Tecer uma linha | Arrastar a lançadeira até a outra borda | E; ou Tab até o botão e Enter/espaço |
| Corrigir | Clicar numa linha pronta, escolher cor e passar | Tab até a linha, Enter/espaço, escolher cor e E |
| Desfazer última linha | Botão Desfazer | U |
| Conferir | Botão Conferir | E ao completar quatro linhas |
| Sair e conservar linhas completas | Voltar | Esc |

Não há limite de tempo. Reduzir movimento elimina a animação da passagem automática. Soltar o arrasto antes da borda preserva a amostra anterior àquele gesto.

## Preservação do projeto

Nenhuma cena, imagem, sprite, colisão ou posição de monumento foi substituída. As estátuas, o letreiro e o desenho do mosaico continuam na cena autoral. A abertura não gasta materiais da bolsa, não fabrica a rede da campanha nem repete a recompensa da memória.

As regras estão em `MTMSessaoTecido`, sem dependência de Unity. Os componentes visuais novos são criados durante o Play; não precisam ser inseridos manualmente na cena.

## Verificação automatizada

Na raiz do repositório, com .NET SDK 8:

```sh
dotnet run --project Tests/PrimeiroFio/PrimeiroFio.Tests.csproj --configuration Release -- .
```

São 12 verificações, compilando as regras reais: seleção, gesto incompleto, conclusão única, sentido alternado, correção, desfazer, retomada, dados inválidos, referências do roteiro, progressão sem consumo de materiais, modelo de save e sintaxe dos scripts em quatro variantes de entrada/Editor.

A serialização portátil usa System.Text.Json; o carregamento por JsonUtility precisa ser conferido no Play. A análise de sintaxe não resolve APIs Unity, shaders, assets ou referências de cena. O workflow de validação executa o comando em push e pull request.

## Conferência no Unity

Abrir `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` na versão de `ProjectSettings/ProjectVersion.txt`.

1. Nova jornada: falar com Feliciana, observar a amostra e iniciar a atividade. Conferir leitura, retratos, atalhos e enquadramento.
2. Fazer duas linhas com o mouse. Soltar a terceira antes da borda. Sair com Esc e retomar: somente as duas completas devem permanecer.
3. Concluir com uma cor diferente do modelo. Conferir, selecionar essa linha e refazê-la. As outras três permanecem. Voltar a Feliciana e conferir o motivo da chegada.
4. Repetir apenas com teclado. Testar reduzir movimento, fonte grande, perda de foco durante o gesto e fechamento da janela.
5. Reabrir o jogo após salvar duas linhas. Testar cópia de save antigo sem `amostraPrimeiroFio` e uma campanha avançada: o progresso deve permanecer, sem obrigar a repetir a abertura.
6. Conferir 16:9, 4:3 e janela menor. Observar personagens/monumentos na caminhada e conversa, pés e piso de dia/noite, acabamento natural ligado/desligado e qualidades disponíveis.
7. Executar a build da plataforma usada no projeto e conferir o Console.

## Pendências de integração

- O cartão “Postais da cidade · 0/3” das capturas não foi localizado nos scripts da main usada como base. Seus scripts locais são necessários para integrar o ajuste de quando o cartão aparece.
- Os retratos receberam enquadramento comum. Uniformizar o estilo da ilustração de Feliciana e do sprite de Neto ainda exige a produção/substituição das imagens e comparação no jogo.
- Esta entrega trabalha o primeiro encontro e os sistemas compartilhados de interface/apresentação. Novas atividades de outras histórias devem ser desenvolvidas a partir de cada missão, preservando as fontes históricas.
- Play, aparência final, desempenho, JsonUtility e build Unity não foram executados neste ambiente.
