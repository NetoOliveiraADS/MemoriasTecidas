# Artes da interface — revisão 14

> A abertura deste documento é o histórico da revisão 14. A imagem atual foi substituída na revisão 15 pela ilustração da Praça Matriz baseada em fotos. Consulte `ABERTURA_PRACA_MATRIZ.md`. As demais alterações descritas aqui permanecem.

Modo: ferramenta integrada de geração/edição de imagens (`imagegen`), com referências locais inspecionadas. Não foi usado o fallback CLI. A tipografia, botões, mapa interativo e demais controles são desenhados por código.

## Abertura

Destino: `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Interface/InicioTecida.png`. Dimensões: 1672 × 941 px.

Referências: abertura areia anterior e `igreja_matriz.png` do projeto organizado. A igreja foi usada como referência visual, sem alegar fidelidade fotográfica ou cartográfica.

Prompt enviado:

```text
Use case: compositing.
Asset type: final background illustration for the opening menu of the Brazilian narrative game Memórias Tecidas, landscape 16:9, ideally 2048 x 1152.
Input image 1 is the existing opening illustration and the edit target: keep its warm sand/cream palette, hammock, cotton threads, carnauba palm, clay tile porch, hand-painted atmosphere and overall left-text/right-art arrangement. Input image 2 is the exact project reference for the Igreja Matriz: use this recognizable church architecture in the town, including its single slender central pointed tower with cross, warm pale cream facade, white narrow gothic ornaments, clock, rose window and terracotta tile roof. Do not use a generic colonial double-tower church.
Primary request: refine and reorganize this opening art so Jaguaruana and hammock weaving feel like the central identity. Show the referenced church and part of the town square more clearly in the middle-right distance, with low terracotta-roof houses, sparse carnauba palms, the river further behind the town and a low distant ridge. Maintain the inviting cotton hammock in the lower-right foreground, with a small spool of cotton thread on the wooden stool. A thin cream thread meanders subtly along the lower edge linking porch to town. Keep the illustrated world grounded in the project's existing art, not a photorealistic vista.
Composition: keep the left 40% calm pale sand parchment with only extremely fine paper texture; this is reserved for live menu typography, NO painted text. The right 60% is a carefully composed inviting scene, refined painted detail but clean silhouettes, not overly busy. Make the church facade visible and recognizable near x=65%, y=43%, above the hammock curve. Carnauba and porch frame the outer right, do not obscure the church. Leave a quieter patch at the extreme lower-right for a small live map shortcut card. No UI, no buttons, no letters, no logo, no watermarks, no character portraits. Keep 16:9 ratio.
Lighting: soft luminous morning, balanced cream and honey sunlight rather than strongly orange sunset, natural soft painted shadows. Palette is mainly sand #E7D3B0, warm cream #FFF9EE, clay terracotta #8A472E with olive vegetation and restrained teal river; do not wash the whole image in green.
Style: polished illustrative game key art, coherent with the supplied hand-painted church and original porch, subtle linen grain, crisp detailed cotton weave and architecture, no cinematic blur or lens effects.
```

## Título do mapa

Destino: `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Interface/MapaJaguaruana.png`. Dimensões: 1711 × 919 px.

Referência: o mapa areia existente, 1710 × 919 px. Edição localizada do título. Foram conferidos os 16 pontos contra as coordenadas existentes; diferenças de até 1 pixel nos recortes comparados. O restante da imagem não é byte a byte idêntico após a edição generativa. Os dados de coordenadas não foram alterados.

Prompt enviado:

```text
Use case: text-localization.
Edit the supplied finished illustrated fabric-thread map with one strictly local change only: replace the large upper-left title "CAMINHOS DE JAGUARUANA" with exactly "REDES DE MEMÓRIAS". Set the new title in two lines, "REDES DE" on the first line and "MEMÓRIAS" on the second line, matching the original dark terracotta embroidered serif lettering. Keep the exact accent in MEMÓRIAS. The new title must fit entirely within the old title's upper-left area above the decorative braided thread.
Preserve EVERYTHING ELSE without changes: original dimensions and aspect ratio (1710 x 919), exact positions and sizes of every place medallion, every street and path, river and bridge, original words and place labels, route legend, woven linen background, colors, the decorative braided line below the large title, and the small "MEMÓRIAS TECIDAS" subtitle. Do not move the map or crop, do not create new streets or places, do not restyle the illustration or change the church drawing. Preserve the pixel alignment of all landmarks outside the title. This is a localized title correction to an existing game asset whose places already have interactive markers. No new UI or labels.
```

## Neto

Nenhuma nova folha de sprites foi gerada nesta revisão. Os 32 quadros e o retrato foram mantidos; o ganho de luz e o alongamento discreto do pescoço são aplicados pela malha e pelo shader.
