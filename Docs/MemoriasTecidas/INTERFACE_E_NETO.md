# Interface, mapa e Neto — revisão 14

> A abertura deste documento é o histórico da revisão 14. A imagem atual foi substituída na revisão 15 pela ilustração da Praça Matriz baseada em fotos. Consulte `ABERTURA_PRACA_MATRIZ.md`. As demais alterações descritas aqui permanecem.

23/10/2026. Esta atualização se aplica ao projeto existente e inclui a restauração das artes originais do cenário feita na revisão 13.

## Resultado

| Pedido | Implementação |
| --- | --- |
| Título do mapa | **Redes de Memórias** na arte, nos cabeçalhos do mapa de consulta e do mapa guiado, e na prévia da abertura. |
| Marca da tela inicial | **Jaguaruana Rede de Memórias • Entre Memórias e Lendas**, distribuída em linhas para leitura. O nome do projeto, Memórias Tecidas, aparece menor. |
| Hover legível | Os oito estados de texto do IMGUI recebem a cor correta. Botões e abas têm fundos próprios para normal, hover/foco e pressionado. Botões desativados têm paleta legível. A busca da bolsa também recebe a correção. |
| Neto mais claro | Ganho de luz de até 10% aplicado apenas ao protagonista, respeitando os tons, o período do dia e o limite dos canais claros. Não altera o retrato nem redesenha o rosto. |
| Pescoço | A parte superior é transladada em até 1,8% da altura do sprite. A transição fica entre a gola e a barba; os pés e o pivô permanecem fixos. |
| Toque curto sem deslizar parado | O primeiro deslocamento visível já exibe a passada 1. A animação acompanha o deslocamento efetivo, sem a suavização adicional que atrasava o início. Soltar a tecla zera a velocidade comandada. |
| Abertura organizada | Igreja baseada no sprite original do projeto, rede e carnaúba; título em texto real, ação principal destacada e atalho compacto do mapa no canto inferior. |

A arte de abertura é uma ilustração para o menu. Não modifica o cenário jogável nem estabelece uma reconstrução geográfica da cidade. O desenho mantém a linguagem visual do projeto.

## Instalação no projeto atual

1. Faça a instalação pelo `Instalar_Atualizacao.cmd` com a Unity fechada.
2. Informe a raiz do projeto; não a pasta `Assets` isolada.
3. O escopo padrão chama-se internamente `MenuMapa`. Ele inclui as dependências de interface, movimento, visual de Neto e acabamento original; mantém os dados narrativos e as cenas.
4. Abra `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
5. Use **Memorias Tecidas → Interface → Validar interface e caminhada** e **Memorias Tecidas → Visual HD2D → Validar cenário original**.
6. Teste no Play antes de criar um commit.

Se o instalador detectar arquivos com modificações locais desconhecidas, ele para antes de copiar. Os caminhos exibidos precisam ser comparados com as alterações locais. Não contorne essa proteção copiando toda a pasta `Assets` por cima.

Os `.meta` existentes mantêm os mesmos GUIDs. As imagens de abertura e mapa são substituídas no mesmo caminho; não é preciso reconectar referências da cena. A posição dos 16 marcadores continua no mesmo JSON. A nova imagem do mapa tem 1711 × 919 px; as coordenadas são normalizadas e a comparação dos pontos indicou diferenças de até 1 pixel.

## Testar no Play

| Onde | Ação | Resultado esperado |
| --- | --- | --- |
| Abertura com e sem save | Passe o mouse em todas as opções; percorra com Tab/Shift+Tab e confirme com Enter | Textos legíveis, foco visível, título correto e atalho do mapa funcionando. |
| Menu e pausa | Pressione, segure, arraste o cursor para fora e solte | Estado pressionado acompanha o botão; soltar fora não confirma a ação. |
| Opções | Ligue/desligue o contraste reforçado | Texto não fica branco em superfícies claras. |
| Bolsa, caderno e diálogos | Passe o mouse nos textos, selecione abas e digite na busca | Cor de leitura estável; o campo de busca recebe foco sem movimentar Neto. |
| Mapas | Abra pelo menu e com M; amplie e arraste | Redes de Memórias aparece no cabeçalho e na imagem; destinos seguem nos locais corretos. |
| Movimento | Toque brevemente cada direção; repita depois de parar | O primeiro deslocamento já mostra uma passada, sem aguardar o quadro de repouso. |
| Movimento | Mantenha a tecla, use Shift, solte e inverta a direção | Passadas acompanham caminhada/corrida; a velocidade não continua desacelerando após soltar. |
| Colisão | Segure uma direção contra uma parede e depois deslize pela lateral | Sem caminhada persistente quando não há deslocamento; movimento lateral real pode animar. |
| Bloqueio | Abra conversa ou menu em movimento; saia e volte ao jogo | O bloqueio interrompe o comando e a animação. |
| Neto | Confira frente, costas e diagonais, de dia e à noite | Rosto preservado, ganho de luz discreto, pescoço um pouco maior, pés e sombra alinhados. |
| Cenário | Confira praça, oficina, vegetação e rio | Artes originais continuam ativas. |

Confira pelo menos 1280 × 720 e 1920 × 1080. O jogo adapta a mesma composição de 1280 × 720 à área da câmera. Toques que ocorram inteiramente entre duas leituras de entrada continuam dependendo do sistema de input; esta revisão não adiciona um buffer de teclas.

## Ajustes opcionais do Neto

No objeto `Explorador_MundoV2/Desenho`, o componente `MTMNetoVisual` possui:

- **Alongamento Pescoco:** padrão `0.018`; `0` volta à proporção anterior. O valor representa uma fração da altura, não unidades do mundo.
- **Luz Preenchimento:** padrão `0.10`; `0` volta à iluminação anterior do personagem. O ganho máximo configurável é 18%.
- A escala visual, a inclinação da corrida e o vento do cabelo continuam disponíveis.

Não precisa aumentar a exposição global para clarear Neto. O shader recebe o ganho no `MaterialPropertyBlock` da malha do protagonista; a sombra não recebe esse ganho.

## Implementação

- `MTMJogoInterface.cs`, `MTMJogoTemaAreia.cs`, `MTMJogoNavegacaoUI.cs` e `MTMJogoRedeUI.cs`: estados e contraste compartilhados.
- `MTMJogoMenus.cs`, `MTMJogoMapaMenu.cs`, `MTMJogoMapaGuiado.cs`: títulos e organização da abertura/mapa.
- `MTMAtorV22.cs`: início imediato na passada 1 e velocidade medida pelo deslocamento visível. Mantém as oito direções e os ciclos completo/reduzido.
- `MTMConfiguracaoVisual.cs` e `MTMJogo.cs`: parada do comando ao soltar a tecla; aceleração e reversão continuam suaves.
- `MTMNetoVisual.cs` e `NetoPintado.shader`: postura e luz local, sem modificar os PNGs de Neto.
- `MTMInterfaceEditor.cs`: verificação no Editor dos estados de texto, primeiro passo, parada, ciclos, proporção e shader.
- `Resources/MemoriasTecidas/Interface/InicioTecida.png` e `MapaJaguaruana.png`: artes revisadas, com GUIDs preservados.

O perfil `kit_hd2d.json` continua com `preservarCenarioOriginal: true` e sem regras para substituir pisos, objetos ou ilustrações de lugares. O acabamento original da revisão 13 permanece.

## O que foi conferido e os limites

Foi feita análise de sintaxe C#, conferência dos recursos e referências, contraste sRGB das combinações principais, encaixe de textos, comparação de alinhamento do mapa e testes numéricos do ciclo de caminhada e parada em 30/60/120 Hz. Os métodos matemáticos foram adaptados mecanicamente de C# para JavaScript; isso não compila C# nem executa a física da Unity.

A prévia HTML executa a animação em Canvas e permite ligar/desligar uma aproximação do acabamento de Neto. A verificação local usou seu script com um adaptador DOM mínimo e Canvas nativo; não foi um teste completo de navegador. As imagens de menu, mapa e contraste são composições com as medidas e fontes da implementação, não capturas da Unity.

**Não houve compilação C#/shaders, execução do instalador PowerShell, Play, build ou medição de FPS.** O validador entregue deve ser executado no Editor e o roteiro acima precisa ser conferido no projeto real. Consulte `VERIFICACAO_INTERFACE_E_NETO.json` para os resultados disponíveis.
