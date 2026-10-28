# Visual, movimento e cenários de Memórias Tecidas

## Direção visual

A cidade mantém a arte pintada do projeto: areia, cerâmica, verde de vegetação, paredes claras e detalhes de madeira. A sensação de volume vem da composição, da ordem de desenho, das sombras projetadas existentes, das novas sombras de contato e do acabamento dos materiais. Os NPCs e o protagonista mantêm suas artes e proporções.

A luz do entardecer ganha calor e a noite conserva leitura suficiente dos caminhos. Os brilhos de água são discretos. O acabamento na base das construções ajuda a conectá-las ao chão. Na qualidade Alta há relevo visual a partir da textura, sem mudar a geometria ou inventar detalhes arquitetônicos dos monumentos.

## O que entra automaticamente

Após a instalação, abra a cena original e inicie o Play. Movimento, câmera, shaders, iluminação, animação e geração das sombras de contato entram pelo código existente. A preparação não substitui a cena nem reposiciona NPCs.

O roteiro é idêntico ao da última atualização corrigida. Os 24 objetivos, as recompensas, os IDs e o save permanecem compatíveis. As missões de Feliciana continuam seguindo a região real da interação.

As ferramentas de composição só acrescentam objetos quando você usa seus botões. Os objetos criados durante a edição ficam em `Cenario_Editavel`, dentro da região escolhida. As sombras de contato do Play são temporárias e reconstruídas ao ativar a região.

## Controles durante o jogo

| Comando | Ação |
| --- | --- |
| WASD / setas | Caminhar. |
| Shift | Correr. |
| Roda do mouse / + / − | Aproximar ou afastar a câmera durante a exploração. |
| E / Enter | Conversar e interagir. |
| I / J / M | Bolsa, caderno e mapa. |
| Esc | Pausa; entre em **Imagem e câmera**. |
| Q | Alternar qualidade dos efeitos. |
| F1 | Ocultar ou mostrar a HUD. |
| R | Voltar ao caminho seguro da região. |

O zoom não reage à rolagem dentro da bolsa, do caderno ou dos diálogos. A posição da câmera respeita o tamanho da região; em um interior pequeno, o limite pode impedir afastar mais.

**Movimento reduzido** desativa a antecipação da câmera, o zoom automático ao correr/conversar, o balanço de cenário e as partículas ambientais/poeira. O acompanhamento continua suave e o zoom manual permanece disponível. Não há tremor de câmera acrescentado.

Qualidade, brilho, zoom e movimento reduzido ficam em preferências locais, separadas do save da campanha. Para voltar aos valores da cena, saia do Play e use **Visual e cenarios → Redefinir preferencias locais**. Entre novamente no Play.

## Qualidade dos efeitos

| Recurso | Leve | Equilibrada | Alta |
| --- | --- | --- | --- |
| Limite de luzes locais selecionadas | 4 | 8 | 16 |
| Paleta, brilho, ciclo de luz | Sim | Sim | Sim |
| Variação do piso e água animada | Desativadas | Sim | Sim |
| Copas e redes ao vento | Desativadas | Sim | Sim |
| Sombras de contato extras | Desativadas | Sim | Sim |
| Relevo adicional a partir da textura | Desativado | Desativado | Sim |
| Partículas ambientais | Desativadas | Reduzidas | Mais densas |

A sombra básica do protagonista permanece no modo Leve. O modo reduzido prevalece sobre os efeitos de movimento de qualquer nível. O desempenho real depende da resolução, da placa gráfica e da densidade de cada região; nenhum FPS foi medido neste ambiente.

## Câmera e movimentação no Inspector

Selecione o objeto com `MTMJogo` pela janela **Abrir direcao de arte → Selecionar câmera, movimento e paleta**. Edite fora do Play para salvar na cena.

| Campo | Efeito |
| --- | --- |
| `velocidade` | Velocidade de caminhada já existente; a corrida usa seu multiplicador anterior. |
| `apresentacao.aceleracao` | Rapidez para atingir a velocidade desejada; padrão 36. |
| `apresentacao.frenagem` | Rapidez para parar/inverter; padrão 48. |
| `tamanhoCamera` | Distância base já definida no projeto; o zoom do jogador atua sobre ela. |
| `suavidadeCamera` | Tempo de acompanhamento; padrão 0,16 s. |
| `antecipacao` | Quanto a câmera acompanha a direção da velocidade; padrão 0,22. |
| `zonaMorta` | Pequena tolerância para evitar que cada micromovimento desloque o enquadramento. |
| `aberturaCorrida` | Afastamento discreto durante a corrida. |
| `aproximarConversas` | Ajuste de foco entre jogador e NPC durante um diálogo. |
| `exposicao`, `saturacao`, `detalhe` | Brilho e tratamento dos materiais. |
| `luzDia`, `luzEntardecer`, `luzNoite`, `luzInterior` | Paleta dos períodos e interiores. |
| `vento`, `sombrasContato` | Intensidade do movimento do cenário e das sombras extras. |

A física continua no `FixedUpdate`; o enquadramento é atualizado no `LateUpdate`. A colisão contínua e a interpolação são configuradas no corpo do protagonista ao iniciar. O código para imediatamente em diálogos, menus, reposicionamento ou perda de foco da janela.

A animação mede deslocamento real. Se uma parede impede a passagem, a tecla pressionada não gera uma caminhada fictícia. A velocidade das passadas acompanha a caminhada e a corrida nas oito direções já existentes.

## Montar pisos, água e caminhos

1. Fora do Play, abra **Visual e cenarios → Abrir direcao de arte**.
2. Escolha a região. Caso esteja inativa, ative-a temporariamente na Hierarchy para enxergá-la na Scene.
3. Informe a posição local desejada. Mova os objetos dentro da região; mantenha os objetos raiz das regiões nas posições usadas pelo projeto.
4. Escolha Calçamento, Areia, Vegetação, Cerâmica ou Água.
5. Clique em **Criar trecho editável de piso / água**.
6. Na Scene, arraste os pontos P1, P2 etc. para definir o contorno.
7. No Inspector, ajuste a textura, cor, repetição e ordem de desenho.
8. Para detalhar uma curva, use **Adicionar vértice na maior borda** e mova o ponto novo.
9. Salve a cena e teste a circulação no Play.

A geometria rejeita contornos cruzados, repetidos, sem área ou com coordenadas inválidas. O contorno não repete o primeiro ponto no final. A água recém-criada bloqueia a passagem e usa sua borda para gerar o `PolygonCollider2D`.

Prefira trechos maiores com poucas variações de textura. Deixe corredores legíveis até saídas e NPCs. O piso recém-criado não detecta automaticamente se você o colocou sobre uma porta ou objetivo; confira o resultado no Play.

## Construções, muros e árvores

A janela reutiliza prefabs de casa, muro, árvores, banco, vasos e poste. Eles conservam a arte, o pivô e a colisão definidos no projeto. Você pode compor novas ruas, jardins e bordas com esses módulos, usando escala e rotação com moderação.

Depois de selecionar um objeto, **Adicionar acabamento ao objeto selecionado** acrescenta `MTMVolumeCenario`. No Inspector:

- `fachada`: sprite que receberá o acabamento.
- `relevo`: intensidade do relevo de textura na qualidade Alta.
- `desgasteBase`: escurecimento sutil na base.
- `sombraContato`: habilita o contato extra desse objeto.
- `raioContato`: ajuste opcional da largura/altura do contato; zero calcula pela arte.
- `deslocamentoContato`: desloca a posição dessa sombra, em unidades do mundo.

Os objetos estáticos que já usam `MTMVisualV2` recebem acabamento e contato automaticamente ao iniciar a região. Personagens e animais são excluídos dessa geração estática. As sombras usam aproximações suaves; não são sombras 3D calculadas por raios ou por geometria de paredes.

As copas e redes com `MTMDeformacaoV22` movem seus vértices no shader. O tronco e os suportes permanecem ancorados. Os personagens continuam com a deformação própria e o vento limitado ao cabelo. Os limites da malha são ampliados para acomodar a movimentação das copas.

## Iluminação dos lugares

Use **Criar luz local quente** para acrescentar `MTMLuzLocalV23` à região. Posicione a luz perto da fonte visível: poste, janela ou luminária. Ajuste `raio`, `intensidade` e `cor` no Inspector.

- Em exteriores, deixe `sempre` desmarcado para a luz crescer ao anoitecer.
- Em interiores, use `sempre` para manter a fonte ativa.
- Evite muitas fontes sobrepostas com intensidade alta; elas podem apagar detalhes claros da arte.
- Confira caminhos e rostos às 8h, ao entardecer e à noite.

O sistema usa luzes estilizadas nos shaders do projeto. Não exige URP, pacotes de pós-processamento ou normal maps. A seleção de fontes considera alcance, distância e intensidade; o limite depende da qualidade escolhida.

## Teste antes de atualizar o Git

1. Aguarde a Unity terminar a importação. O Console deve ficar sem erros de C# e shader.
2. Execute **Visual e cenarios → Validar camera materiais e movimento**.
3. Execute os validadores anteriores de organização, transferência de diálogos e 24 missões.
4. No Play, caminhe/corra em oito direções. Solte as teclas e encoste em paredes; confira se o personagem para e se as passadas correspondem ao deslocamento.
5. Faça uma conversa com Dona Feliciana na Entrada. Abra bolsa, caderno e mapa; confira que a câmera não muda quando você rola os textos.
6. Percorra bordas e saídas. Teste 16:9, 4:3 e uma janela larga na Game View; confira os limites do enquadramento.
7. Entre em interiores, vá ao rio e percorra as margens. A água deve bloquear nos mesmos limites do contorno.
8. Observe árvores, redes, construções e o protagonista de dia e de noite. Ative as três qualidades e o modo de movimento reduzido.
9. Use o Profiler e o painel Stats da Game View para comparar a mesma área/câmera nas três qualidades. Anote resolução, hardware, FPS e tempo de quadro.
10. Saia e entre no Play novamente. Confira as preferências locais e a continuidade da campanha.
11. Salve as alterações intencionais na cena. Depois use o guia de Git.

O validador incluído executa cálculos de movimento, limites de câmera e ciclo solar, consulta erros dos shaders e verifica referências da cena. Ele não substitui inspeção visual, testes de percurso ou medição de desempenho.

## Organização do código

Os caminhos abaixo começam em `Assets/MemoriasTecidas/MundoV2/`.

| Arquivo | Responsabilidade |
| --- | --- |
| `Runtime/MTMConfiguracaoVisual.cs` | Parâmetros da apresentação e cálculos de movimento/câmera/sol. |
| `Runtime/MTMJogoApresentacao.cs` | Integração de física, câmera, zoom, preferências e opções na pausa. |
| `Runtime/MTMAmbienteV2.cs` | Ciclo de luz, paleta e seleção das fontes locais. |
| `Qualidade/Shaders/MTMIluminacao.cginc` | Iluminação compartilhada e deformação de vegetação/tecido. |
| `LugaresV23/Gerados/Shaders/SuperficieV23.shader` | Acabamento do chão e da água. |
| `Runtime/MTMContatoCenario.cs` | Contatos estáticos reunidos em uma malha por região. |
| `Runtime/MTMVolumeCenario.cs` | Acabamento ajustável dos objetos. |
| `MelhoriasV22/Runtime/MTMAtorV22.cs` | Animação pela velocidade real. |
| `Editor/MTMDirecaoArte.cs` | Composição dos cenários e criação de luzes. |
| `Editor/MTMSuperficieEditor.cs` | Edição dos contornos pela Scene. |
| `Editor/MTMValidarExperiencia.cs` | Verificações para executar na Unity. |

Não crie outra cópia desses scripts em uma pasta diferente. As classes complementares de `MTMJogo` usam `partial`; duplicar uma versão antiga causa membros ou classes repetidos.

## Limites desta entrega

O pacote usa a versão do projeto e as artes disponíveis nesta conversa. Edições posteriores feitas apenas no seu computador são protegidas pelo instalador, mas não foram inspecionadas aqui.

Não foram executados Unity, compilação C#/shaders, Play, build ou o instalador PowerShell neste ambiente. A análise local cobre sintaxe, dependências, geometria e simulações de dados. A renderização nativa, a interação física e o desempenho precisam da conferência acima.

Para evoluir os monumentos para malhas 3D, trocar o renderizador ou produzir normal maps artísticos específicos, será necessária uma etapa adicional de produção e validação visual. Esta entrega aprimora o cenário 2D existente e sua edição.

## Referências de implementação

Documentação oficial da Unity consultada para os contratos das APIs da família Unity 6:

- [Camera.orthographicSize](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-orthographicSize.html)
- [Rigidbody2D.interpolation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody2D-interpolation.html)
- [ShaderUtil.ShaderHasError](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ShaderUtil.ShaderHasError.html)
- [Handles.FreeMoveHandle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Handles.FreeMoveHandle.html)
- [Undo.RegisterCreatedObjectUndo](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Undo.RegisterCreatedObjectUndo.html)
