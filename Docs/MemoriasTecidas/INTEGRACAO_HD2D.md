# Integração HD2D — histórico da revisão 12

**Revisão atual: 13. As substituições de cenário descritas abaixo foram desativadas a pedido do usuário. Use `RESTAURAR_VISUAL_ORIGINAL.md`. Neto, retrato e interface continuam ativos; os locais usam as artes anteriores.**

Revisão 12 do pacote, 23/10/2026. Para o mesmo projeto Unity 6000.3.25f1, pipeline Built-in.

O kit agora possui código de aplicação no jogo: não é preciso arrastar cada imagem para a cena. A integração acontece no início do Play, depois da preparação da jornada. Os sprites e materiais originais continuam no projeto e são restaurados quando a integração é encerrada.

## O que foi integrado

| Elemento | Aplicação |
| --- | --- |
| Neto | Duas folhas transparentes, 32 quadros, oito direções. Uma pose parada e três variações de passada por direção; o controlador existente acompanha o deslocamento real e acelera a animação ao correr. |
| Identidade do Neto | Cabelo escuro de lado, sobrancelhas marcadas, barba cheia arredondada e bigode, usando o retrato e os sprites originais como referência. Corpo adulto, camisa azul-esverdeada e mochila terracota. |
| Feliciana | Retrato no ponto `npc_feliciana`, identificado pelo ID. Funciona também quando o ponto está na entrada da cidade. O diálogo e a posição são os que já estão na cena. |
| Árvores | Carnaúbas, árvores podadas e ribeirinhas recebem sprites do novo atlas. Sombras e deformação continuam ligadas aos componentes existentes. |
| Oficina | Novas artes para tear, mesa de trabalho, prateleira, algodão, vaso e rede. A banca e as caixas só são substituídas pelas regras específicas da região `oficina`. |
| Superfícies | Pedra clara recebe calçamento, terra recebe chão do sertão e água recebe textura azul-esverdeada. A água mantém o shader animado, contorno e colisão originais. |
| Pisos desenhados | Acabamento discreto sobre os pisos existentes, conservando ruas, cruzamentos e jardins. Não se troca uma planta inteira por uma textura genérica. |
| Mapa | `M → escolher lugar → Ver este lugar` abre uma ilustração. Praça, oficina, rio e beira do rio recebem as artes do kit; demais locais usam a imagem existente, se houver. Voltar/Esc retorna ao mapa. |
| Interface | Mantém a revisão areia: abertura, HUD, mapa, bolsa, pausa e caderno em areia, creme, marrom e terracota. Retratos são ajustados à área sem esticar o rosto. |

As ilustrações de praça, oficina e rio são vistas de referência dentro do caderno de lugares. O cenário jogável recebe texturas e objetos separados; as fachadas, monumentos e ligações da Jaguaruana já construída permanecem. Não há reconstrução tridimensional de edifícios nesta entrega. Os ícones da bolsa continuam sendo desenhados pelo sistema existente, não por uma imagem estática da interface.

## Instalar no mesmo projeto

1. Feche a Unity. Extraia `MemoriasTecidas_Atualizacao_24_Missoes.zip` fora da pasta do projeto.
2. Execute `Instalar_Atualizacao.cmd` e indique a pasta com `Assets`, `Packages` e `ProjectSettings`.
3. O instalador confere versões e cria um backup ao lado do projeto. O escopo padrão inclui agora interface, mapa e integração HD2D. O nome interno `MenuMapa` foi mantido para compatibilidade com o comando anterior.
4. Reabra `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity` e aguarde a importação.
5. Execute **Memorias Tecidas → Interface → Validar recursos dos menus** e **Memorias Tecidas → Visual HD2D → Validar integração**.
6. Entre no Play. O Console informa a quantidade de personagens, objetos, superfícies, pisos e retratos que receberam as novas artes.
7. Confira a tabela de testes abaixo antes de atualizar o Git.

Não é necessário copiar o ZIP do kit artístico para dentro de `Assets`: os recursos utilizados já acompanham o ZIP de atualização. O kit complementar organiza as imagens, recortes e prompts para edição.

O instalador padrão não copia cenas, prefabs, dados narrativos ou `MTMJogoNarrativa.cs`. Também preserva `.meta` existentes. Se encontrar alterações locais desconhecidas em um arquivo que precisa atualizar, interrompe antes de copiar e mostra o caminho. Guarde esse arquivo para mesclar sua versão; não contorne a proteção sobrescrevendo toda a pasta.

## Revisar antes de abrir a Unity

Abra `Previa_Integracao_HD2D.html` no navegador, após extrair o ZIP inteiro. Escolha Parado, Caminhando ou Correndo para ver os mesmos recortes e pivôs usados pela integração. É uma composição em Canvas, sem o motor, luzes, câmera ou física da Unity. A pasta deve permanecer completa para carregar as imagens relativas.

## Testes no Play

| Ação | Resultado esperado |
| --- | --- |
| Começar e continuar uma jornada existente | Menu areia, save preservado e novo Neto visível. Não reinicie seu save principal para testar. |
| Caminhar e correr em oito direções | Pose correspondente, pés ancorados, escala consistente, passadas mais rápidas na corrida. Conferir visualmente as transições e o balanço dos quadros. |
| Soltar teclas, encostar numa parede, pausar | Neto para; não continua caminhando parado. |
| Passar atrás de árvores e redes | Ordenação correta, base estável, sombras acompanhando a nova arte. |
| Conversar com Feliciana onde ela está na cena | Retrato sem distorção; mesmas falas e missão. Mover apenas o desenho sem o ponto de interação continua exigindo o procedimento de `MOVER_PERSONAGENS.md`. |
| Usar o tear e sair da oficina | Mesmo ponto de interação e mesmas passagens, com objetos renovados. |
| Percorrer margens e ponte | Água animada sem deslocar margens, travessia e bloqueios corretos. |
| M → Ver este lugar → Esc | Ilustração proporcional e retorno ao destino selecionado no mapa. |
| Seguir missão no mapa | Mesmos marcadores, rotas reais e indicação da próxima saída. |
| Alternar qualidade e movimento reduzido | Respeita as opções existentes; conferir vento, reflexos e leitura. |
| I/J/Esc, Tab/Enter, contraste e fala ampliada | Mesma navegação, textos legíveis, sem retratos esticados. |
| Sair e entrar novamente no Play | Sem duplicar materiais nem perder referências anteriores. |

## Personalizar sem remontar a cena

Configuração: `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/HD2D/kit_hd2d.json`.

- `ativo`: `false` desativa a aplicação do kit na próxima entrada no Play. A interface areia continua ativa.
- `neto`: ordem **S, SW, W, NW, N, NE, E, SE**, quatro quadros por direção. `x` e `y` são coordenadas em pixels a partir do canto inferior esquerdo. `pivotX` e `pivotY` são proporcionais ao recorte. `alturaReferencia` mantém a escala da direção com base na pose parada.
- `objetos`: recortes do atlas, pivôs próximos à base. Os objetos mantêm a largura anterior; árvores mantêm a altura. Não se altera o Transform para redimensionar a arte.
- `trocas`: nomes exatos de sprites ou texturas que serão substituídos. `regioes` vazio aplica a qualquer região. `lado` vale -1 à esquerda do centro local, 1 à direita e 0 em ambos os lados. As duas regras de caixas da oficina distinguem prateleira e algodão.
- `superficies`: substitui só a textura no componente `MTMSuperficieV23`, preservando contornos e parâmetros.
- `pisos`: `mistura` controla o acabamento (0 a 0,5); `repeticao` controla o tamanho da textura em unidades do mundo. O shader modula a cor da arte original, preservando sua composição.
- `cartoes`: associação entre região e ilustração usada no mapa.

Para deixar um objeto específico como antes, retire seu nome da regra, ou restrinja `regioes`. Objetos novos só entram na substituição se corresponderem a uma regra. As regras não atingem bustos, igreja, prefeitura, mercado, pontes ou fachadas específicas.

Importação: `Texture Type = Default`, `Non Power of 2 = None`, `Max Size = 2048`, compressão desativada. Folhas e retrato usam Clamp; chão, água e calçamento usam Repeat. Os `.meta` novos já trazem esses valores. Se a plataforma redimensionar as folhas, o validador aponta a diferença; a integração mantém a aparência anterior em caso de falha.

Recursos instanciados: `MTMKitHD2D` cria sprites FullRect e um material compartilhado de piso durante a sessão, conserva as referências antigas e libera suas instâncias ao encerrar. Não modifica rigidbody, collider, dados das missões, conexões do mapa ou arquivo de save.

## Referências e limites

Continuam as referências de Spiritfarer para unidade visual acolhedora, Pentiment para identidade artesanal/legibilidade, fios de redes como linguagem de interface e Jaguaruana como cenário. Fontes e links anteriores permanecem em `UI_UX.md`. Nenhuma arte desses jogos foi copiada para o pacote.

APIs usadas: [Sprite.Create](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Sprite.Create.html), [Renderer.SetPropertyBlock](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer.SetPropertyBlock.html).

Foram verificados arquivos, recortes/alfa, regras, GUIDs, sintaxe C#, dependências, preservação dos dados anteriores e integridade do ZIP. Consulte `VERIFICACAO_INTEGRACAO_HD2D.json`. **Não houve compilação C#/shaders, Play, build, medição de FPS ou execução de PowerShell neste ambiente.** As texturas têm intenção de repetição; confira emendas e escala no cenário. A animação foi montada e possui prévia, mas o ajuste visual final deve ser conferido no Play.

Depois de testar, use `git status`, `git add -A`, `git diff --cached --stat`, `git commit -m "Integra kit HD2D e tema areia"` e `git push` na raiz do repositório atual. Não inclua ZIPs, backups nem `Library`, `Temp`, `Logs` ou `Obj` no commit. Mantenha os `.meta` dos recursos em `Assets`.
