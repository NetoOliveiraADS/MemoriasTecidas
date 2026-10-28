# Direção visual — rede de memórias

A interface usa a rede como ligação entre objetos, pessoas e etapas. Petróleo forma o fundo, algodão e papel sustentam a leitura, ouro marca fios e nós, terracota orienta ações e verde-folha sinaliza conclusão.

| Cor | Hexadecimal | Aplicação |
| --- | --- | --- |
| petroleo | `#163C3B` | Painéis escuros |
| profundo | `#102B2B` | Rebaixo dos ícones |
| algodao | `#F4E9D5` | Texto claro e fundo principal |
| papel | `#EADCC0` | Cartões e detalhes |
| ouro | `#D9AF68` | Fios e nós |
| terracota | `#B96C4D` | Ações e ênfase |
| folha | `#718A70` | Conclusão e tarefas |
| texto | `#193D3A` | Texto sobre fundo claro |

## Componentes

- A HUD recebe costuras, cordões e nós de progresso. A indicação de interação permanece textual para deixar o cenário legível.
- A bolsa separa materiais, lembranças, pistas e tarefas. Objetos ainda não descobertos continuam ocultos conforme o roteiro. Materiais conhecidos podem aparecer com quantidade zero para ajudar a planejar a receita.
- Os ícones são desenhos geométricos produzidos por código e armazenados em cache. Os 37 objetos usam uma família de 40 desenhos, também empregada na navegação. Objetos da mesma família podem compartilhar um desenho.
- O caderno organiza os quatro capítulos e mostra a sequência de etapas. O detalhe tem rolagem para títulos e textos maiores.
- O mapa mantém os trajetos e bloqueios; cada lugar ganha uma ficha com sua função na jornada.

## Arquivos

| Arquivo | Responsabilidade |
| --- | --- |
| `MTMJogoInterface.cs` | Estrutura comum, diálogo, mapa, pausa e HUD |
| `MTMJogoRedeUI.cs` | Nós, rede decorativa, bolsa, receita e missões |
| `MTMIconesTecido.cs` | Desenhos dos ícones e textura de tecido |
| `trama_01.json → paleta` | Cores da interface |
| `trama_01.json → itens[].icone` | Desenho de cada objeto |

Os painéis usam coordenadas de projeto 1280×720, escaladas para a área da câmera. As linhas são desenhadas em texturas locais, sem rotação da matriz da GUI. A textura de tecido é discreta e não exige uma imagem externa. A coleta de texturas ocorre ao encerrar o componente.

A prévia PNG é ilustrativa. A fonte nativa depende da GUI da Unity; confirme legibilidade em 720p, 1080p e 4K. O cenário e os sprites originais não foram redesenhados nesta atualização; as fichas de ambiente documentam a direção para a próxima etapa de arte.


## Cenários, shaders e câmera

A revisão gráfica do mundo, suas opções e seus limites estão documentados em `EVOLUCAO_VISUAL.md`. A prévia de interface deste guia pertence à etapa anterior; não representa uma captura dos novos cenários na Unity.
