# Abertura — Praça Matriz de Jaguaruana

Revisão 15, 23/10/2026. Atualização cumulativa para o mesmo projeto Unity.

A imagem da abertura foi substituída por uma ilustração da Praça Matriz em vista elevada, baseada nas fotografias enviadas. A referência principal foi `igrejaMatriz.jpg`; `igrejaMatriz2.jpg` orientou os detalhes do piso e da estátua. A composição preserva os elementos reconhecíveis: igreja de torre central, praça alongada com desenhos no calçamento, ruas laterais, canteiro da avenida e telhados de barro.

A arte usa areia, creme e terracota, com vegetação em verde natural. É uma interpretação ilustrada da fotografia, com enquadramento adaptado ao menu; não é uma reconstrução métrica da praça.

## Instalar

1. Feche a Unity e extraia o ZIP principal fora da pasta do projeto.
2. Execute `Instalar_Atualizacao.cmd` e selecione a raiz do projeto existente, onde estão `Assets`, `Packages` e `ProjectSettings`.
3. Reabra `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` e confira a abertura no Play.

O instalador mantém backup e verifica conflitos antes de copiar. O pacote principal já contém a imagem; não é necessário importar o kit artístico separadamente.

## Integração

- Imagem ativa: `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Interface/InicioTecida.png`.
- Dimensões: **1672 × 940 pixels**, RGB, proporção aproximada de 16:9.
- Nome do recurso e arquivo `.meta` mantidos; GUID `a745af6ac54a55ba902685c862557c40`.
- O menu existente carrega o mesmo recurso, sem precisar reconectar a cena.
- Título e botões continuam sendo texto e controles do jogo, não estão pintados na imagem.
- O kit mantém a mesma imagem em `Tema_Areia/InicioTecida.png`.
- `Previa_Menu_UIUX.png` mostra a composição com o layout existente, usando dados de exemplo. **Não é captura da Unity.**

Esta revisão altera somente a imagem de abertura entre os recursos de `Assets`. O pacote continua incluindo as melhorias da revisão 14: nomes do menu/mapa, contraste, iluminação local de Neto e correção do início da caminhada. O mapa, os scripts, os sprites, os materiais e os dados de jogo permanecem idênticos aos daquela revisão.

## Conferência visual no Play

Confira em 1280 × 720 e 1920 × 1080: torre inteira visível, praça reconhecível, menu legível e atalho do mapa funcionando. Confira também uma janela mais estreita e o menu com e sem save. A abertura usa o enquadramento existente da interface.

Nesta revisão foram conferidos os PNGs, a composição da prévia, a preservação dos demais recursos, os hashes de instalação e a integridade dos ZIPs. **Compilação e Play na Unity não foram executados.** O relatório desta etapa está em `VERIFICACAO_ABERTURA_PRACA_MATRIZ.json`; os relatórios das etapas anteriores são histórico.

## Referência e criação

Fotografias previamente enviadas pelo usuário: `igrejaMatriz.jpg` (800 × 568) e `igrejaMatriz2.jpg` (600 × 450). A ilustração foi criada com a ferramenta integrada de imagens, usando essas referências. O prompt completo está em `PROMPT_ABERTURA_PRACA_MATRIZ.md`.

