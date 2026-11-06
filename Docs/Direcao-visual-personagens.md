# Personagens e cidade: direção visual

## Decisão de arte

Pintura 2D em alta resolução, com volumes suaves, materiais foscos e contornos discretos. A paleta e os materiais de Jaguaruana orientam os personagens. Preservar rostos, idades, vestimentas reconhecíveis, referências históricas, estátuas, letreiro, arquitetura e mosaico autorais.

O conceito visual apresentado nesta conversa é um estudo de acabamento. Ainda não é uma folha de animação, nem uma captura da build. As proporções e a câmera dos sprites finais precisam ser conferidas no enquadramento real do jogo.

## Referências selecionadas

| Referência oficial | O que estudar | Aplicação proposta |
| --- | --- | --- |
| [Eastward — imagens](https://eastwardgame.com/media/) | Personagens legíveis em cidades detalhadas; relação entre cor, luz e cenário | Concentrar contraste no personagem e diminuir microdetalhe nos pisos. A referência de pixel art serve à composição; o projeto permanece pintado em alta resolução. |
| [Spiritfarer — press kit](https://thunderlotusgames.com/press-kits/spiritfarer-press-kit/) | Desenho de personagens, expressão e materiais em arte desenhada à mão | Unificar o tratamento do rosto, das roupas e dos retratos. Manter a vista elevada usada por Memórias Tecidas. |
| [Octopath Traveler II — Square Enix](https://www.square-enix.com/asia/newsportal/en/sg/octopathii/) | Integração visual entre personagens 2D, iluminação e ambiente | Estudar sombra de contato e hierarquia luminosa, respeitando o mapa e a câmera existentes. O uso de cenários 3D nessa referência não exige migrar este projeto para 3D. |

Esses jogos são referências de direção de arte. As imagens deles não foram copiadas para os assets do jogo.

## Materiais e texturas

| Material | Referência de consulta | Tratamento no projeto |
| --- | --- | --- |
| Tecidos, algodão e couro | [Poly Haven — Textiles & Leather](https://polyhaven.com/textures/textiles-leather) | Dobras e costuras primeiro; trama fina apenas na aproximação. Não aplicar grão intenso sobre rostos. |
| Trama tecida | [ambientCG — Fabric 010](https://ambientcg.com/view?id=Fabric010) | Referência de entrelaçamento, a confrontar com as fotos das redes de Jaguaruana. O material de biblioteca não comprova a técnica local. |
| Asfalto | [Poly Haven — Asphalt Floor](https://polyhaven.com/a/asphalt_floor) | Usar como comparação de escala e granulação; preservar as referências autorais da rua. |
| Meio-fio e calçamento | [ambientCG — Paving Edge 001](https://ambientcg.com/view?id=PavingEdge001) | Estudar desgaste suave nas bordas; conservar o desenho e a cor dos pisos locais. |

Poly Haven e ambientCG disponibilizam esses materiais sob CC0: [licença Poly Haven](https://polyhaven.com/license) e [licença ambientCG](https://docs.ambientcg.com/license/). Os sites oferecem materiais PBR; os shaders deste projeto não passam a usar mapas normal/roughness simplesmente por colocá-los no Unity. Nesta etapa foram selecionadas referências, sem substituir texturas da cidade.

## Padrão para produção

- **Identidade:** usar as imagens autorais como referência de rosto, idade e roupas. O protagonista conserva cabelo escuro, barba, roupa verde-petróleo, mochila e calça marrom. A cor de pele não deve ser igualada entre pessoas.
- **Proporções:** desenhar corpos adultos compatíveis entre si. Para Neto, começar com um estudo próximo de 5–6 cabeças de altura e conferir a leitura na câmera real; variações de corpo e idade permanecem. Evitar obter proporções novas esticando o sprite antigo.
- **Contornos:** bordas pintadas em cores escuras do próprio material. Evitar alternar um morador com linha preta espessa e outro com borda fotográfica.
- **Volume:** poucos planos de luz legíveis. Tecidos e pele foscos; couro com brilho discreto. Pintar iluminação neutra que aceite o ciclo de luz do jogo.
- **Paleta:** creme, terracota, verde-petróleo, marrons e cinzas locais. Preservar cores históricas e roupas identificadoras. Avaliar saturação em conjunto com o cenário.
- **Textura:** detalhes grandes continuam visíveis no tamanho normal de jogo; fibras e poros ficam para o retrato. Não usar ruído de tecido como filtro uniforme sobre toda a figura.
- **Pés:** mesma linha de chão, pivôs consistentes e sombra de contato. Nunca incluir a sombra do chão dentro do PNG transparente do personagem.
- **Retratos:** mesma pessoa, roupa, proporção do rosto e acabamento dos sprites; enquadramento de busto. Um recorte maior não cria detalhe que não existe na imagem.
- **Resolução:** produzir o desenho mestre acima do tamanho de exibição e conferir o resultado a 100% da câmera. Aumentar PNGs antigos, isoladamente, não cria detalhe. Não há motivo para usar texturas 4K em todos os objetos.

O protagonista atual utiliza **32 quadros: oito direções e quatro passos**. Ao substituir arte, preservar a ordem, as dimensões de cada folha, os retângulos e os pivôs declarados em `Resources/MemoriasTecidas/HD2D/kit_hd2d.json`, ou atualizar todas essas informações em conjunto. O kit ativo usa `NetoCardinais` e `NetoDiagonais`; editar apenas as folhas antigas de `Arte/Protagonista` pode não mudar o personagem que aparece no Play.

Os importadores consultados já utilizam sRGB, filtro bilinear, alfa para transparência e ausência de compressão no perfil padrão. Não foram alterados indiscriminadamente. Guia de consulta: [Sprite Import Settings — Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/texture-type-sprite.html).

## Base técnica implementada

O novo perfil em `Resources/MemoriasTecidas/acabamento_original.json` é compartilhado pelo protagonista e pelos moradores com `MTMAtorV22`:

| Parâmetro | Valor inicial | Função |
| --- | --- | --- |
| saturacao | 0,94 | Redução discreta da saturação das cores existentes |
| suavizacao | 0,12 | Suavização limitada do microdetalhe, em qualidades com efeitos |
| luzPreenchimento | 0,03 | Mesmo ganho multiplicativo após a luz ambiente para Neto e moradores |

As amostragens ficam dentro do retângulo do quadro atual, preservam o alfa e evitam suavizar contornos escuros. O perfil acompanha a opção **Acabamento natural**. Desligá-la recupera o tratamento anterior, incluindo o preenchimento específico de Neto.

`MTMNetoVisual` passa a copiar o bloco de propriedades do SpriteRenderer, como a malha dos moradores já faz. Não são criados materiais individuais nem modificados quadros, animações, escalas, colisões ou os desenhos originais.

Essa camada aproxima cor, microtextura e luz. A mudança de anatomia, traço, roupas e retratos depende das novas artes; um shader não substitui esse trabalho.

## Validação no Unity

1. Comparar Neto e Feliciana lado a lado, na praça, com Acabamento natural ligado/desligado.
2. Conferir as oito direções e todos os passos: não deve aparecer cor de quadros vizinhos, halo no alfa ou perda de olhos e barba.
3. Comparar dia, entardecer, noite e interiores. A luz de preenchimento não deve tornar ninguém autoiluminado.
4. Repetir em qualidade baixa, média e alta. Na baixa, a suavização por amostragem é desligada.
5. Conferir NPCs ativados por eventos e entrada/saída das regiões. Desabilitar/reabilitar o componente de acabamento deve restaurar suas propriedades.
6. Conferir no Profiler e executar build. A filtragem acrescenta quatro amostras de textura por pixel opaco de personagem nas qualidades com efeitos.

Os testes portáteis validam os parâmetros e a sintaxe C#. Play, shaders compilados pelo Unity, aspecto visual, desempenho e build continuam dependendo do Editor.

## Próxima produção de arte

Concluir um modelo de Neto e um de Feliciana na câmera real; derivar deles retratos e animações; depois aplicar a mesma direção aos demais moradores. Conferir cada lote na cidade antes de substituir o seguinte. As pessoas históricas devem continuar reconhecíveis nas referências originais.
