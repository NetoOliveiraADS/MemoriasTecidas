# Mapa de Jaguaruana no menu

Revisão visual de 23/10/2026: mapa em tecido areia e controles claros. A configuração dos locais e as rotas permanecem. Consulte `TEMA_AREIA.md`.

Atualização para o projeto existente de Memórias Tecidas, Unity 6000.3.25f1.

## Instalação

1. Feche a Unity e extraia o ZIP fora da pasta do projeto.
2. Execute `Instalar_Atualizacao.cmd` e informe a pasta que contém `Assets`, `Packages` e `ProjectSettings`.
3. Aguarde a confirmação. O instalador faz backup e interrompe a cópia se encontrar alterações locais desconhecidas.
4. Abra o mesmo projeto e a cena `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
5. Execute **Memorias Tecidas → Interface → Validar recursos dos menus**.
6. Pressione Play e use **Explorar o mapa**, no lado direito da tela inicial.

Não é preciso criar outro projeto, cena ou componente. O script existente carrega o novo recurso automaticamente. O instalador padrão aplica apenas menu, interface e mapa à campanha já presente. Ele também inclui os arquivos necessários das melhorias anteriores de interface, sem exigir que você instale o menu antigo primeiro. `MTMJogoNarrativa.cs` e os JSONs da campanha não são substituídos. Se houver conflito em outro arquivo da interface, mantenha sua cópia e envie esse arquivo para mesclar as alterações. Consulte `CORRECAO_INSTALACAO_MENU.md`.

## Onde aparece

- **Tela inicial:** mapa inteiro no painel direito, com botão para ampliar.
- **Menu de pausa:** `Esc → Mapa de Jaguaruana` abre a versão navegável, também acessível por M durante a partida. Veja `MAPA_GUIADO.md`.
- **Voltar ou Esc:** retorna ao menu de origem. Abrir a imagem não começa nem reinicia uma jornada.

O mapa ilustra os locais com fios e nós, seguindo a referência e as posições indicadas pelo autor: Serra Dantas depois do rio, Fábrica de redes antes da entrada, Museu Carnaubal acima da fábrica e os locais centrais junto à Avenida Simão de Góis e Rua do Comércio. A descrição inferior foi retirada; título, nomes e legenda permanecem.

A consulta da tela inicial permite ampliar a imagem. Durante a partida, o atalho **M** abre a versão com marcadores selecionáveis e rotas. O deslocamento entre lugares acontece pelas saídas da cena, usando E. As coordenadas dos marcadores na ilustração ficam no arquivo separado `mapa_jaguaruana.json`; construções e personagens mantêm suas posições na cena.

## Controles da imagem ampliada

| Ação | Controle |
| --- | --- |
| Ampliar e reduzir | Roda do mouse sobre a imagem, teclas `+`/`−` ou botões da barra |
| Explorar os detalhes | Arrastar com o botão esquerdo ou usar WASD/setas |
| Reenquadrar tudo | **Ver inteiro** ou **Home** |
| Percorrer os botões | Tab e Shift+Tab |
| Confirmar um botão | Enter ou espaço |
| Retornar ao menu de origem | **Voltar** ou **Esc** |

O zoom varia de 100% a 300% em relação ao enquadramento inicial. A imagem mantém a proporção. Em 100%, todo o mapa fica visível; ao ampliar, o recorte fica limitado à área da imagem e não cobre os controles. O zoom desta tela não modifica o zoom da câmera do personagem.

## Trocar a imagem depois

Arquivo dentro do projeto:

`Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/Interface/MapaJaguaruana.png`

Substitua esse PNG mantendo o nome e o arquivo `.meta` ao lado. A versão entregue tem **1711 × 919 pixels**. A importação incluída usa textura 2D, filtro bilinear, sem mipmaps, sem compressão e limite 2048 para preservar os rótulos. Se futuramente usar imagem maior, ajuste o limite de importação para sua resolução.

Código da tela:

`Assets/MemoriasTecidas/MundoV2/Runtime/MTMJogoMapaMenu.cs`

As chamadas ficam em `MTMJogoMenus.cs` e `MTMJogoInterface.cs`. A captura do mouse é liberada ao fechar a tela ou perder o foco; a navegação geral deixa as setas disponíveis para explorar o mapa.

## Conferir no Play

1. Abra o mapa pela tela inicial antes de começar a jornada. Veja se todos os locais aparecem no enquadramento inicial.
2. Amplie até 300%, arraste até as quatro bordas e solte também fora da área do mapa. Os botões devem continuar acessíveis.
3. Pressione Home ou Ver inteiro; confirme o retorno ao mapa completo.
4. Use Tab, Shift+Tab, Enter e Esc. Teste as setas no mapa ampliado.
5. Entre na jornada, pause e abra o mapa novamente. O relógio e o personagem devem permanecer parados; Esc deve voltar à pausa.
6. Teste em 1280×720, 1920×1080 e numa janela 4:3. Confira a proporção e a leitura com zoom.

Foram conferidos os arquivos, a sintaxe C#, a imagem e os metadados de importação, os hashes de atualização e a integridade do ZIP. O relatório está em `VERIFICACAO_MAPA_MENU.json`. A Unity não estava disponível neste ambiente; compilação, Play e execução do instalador PowerShell precisam ser conferidos no computador do projeto.

## Origem e referências técnicas

A imagem foi gerada com assistência de IA a partir da referência de Jaguaruana enviada e das correções do autor. Nesta revisão, retirou-se a descrição do rodapé. A imagem corrigida foi incluída no pacote sem recortar ou comprimir seu arquivo original.

Referências de implementação consultadas na documentação oficial da Unity 6.3:

- [GUI.BeginGroup](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GUI.BeginGroup.html): delimitação da área de desenho da imagem ampliada.
- [GUIUtility.hotControl](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/GUIUtility-hotControl.html): captura do mouse durante o arraste.
