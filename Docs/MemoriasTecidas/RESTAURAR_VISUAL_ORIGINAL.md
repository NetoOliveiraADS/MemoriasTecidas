# Jaguaruana — artes originais com acabamento refinado

> O comportamento de cenário descrito aqui continua ativo. A revisão 14 acrescenta abertura, mapa, contraste e ajustes do Neto; consulte `INTERFACE_E_NETO.md`.
Revisão 13, 23/10/2026. Atualização para o mesmo projeto Unity 6000.3.25f1, pipeline Built-in.

O cenário volta a usar as imagens que já estavam ligadas às cenas e aos prefabs antes da integração do kit alternativo. A revisão 12 substituía sprites e texturas somente durante o Play; os arquivos originais não foram sobrescritos. Esta correção desativa essas trocas no perfil padrão.

## Resultado

| Área | Comportamento nesta revisão |
| --- | --- |
| Construções, igreja, mercado, prefeitura e monumentos | Artes e composição anteriores preservadas, incluindo os detalhes ligados às referências de Jaguaruana. |
| Vegetação, oficina e objetos | Voltam os sprites originais, com os pivôs, tamanhos e posições existentes. |
| Chão, mosaicos, pedra e água | Voltam as texturas originais. A camada do novo calçamento e as texturas alternativas não são aplicadas. |
| Definição | Realce discreto dos detalhes já presentes na imagem, calculado sobre seus próprios pixels. |
| Relevo | Pequena variação de luz nos detalhes da textura na qualidade Alta; respeita o horário do jogo e não é aplicada à água. |
| Cor e transparência | O realce usa um único fator de luminosidade por pixel. Mantém a proporção entre as cores e não altera o alfa. |
| Distância da câmera | O realce diminui na redução da imagem para evitar cintilação em folhas e telhas. |
| Atlas | Amostras limitadas ao recorte de cada sprite para não puxar pedaços da arte vizinha. |
| Mapa | Mantém o mapa guiado em areia; a consulta de lugares volta às imagens que já estavam em `MTMVidaV22.lugares`. |
| Neto e interface | Mantém o Neto em oito direções e sua referência facial, o retrato de Feliciana e a interface areia/creme/terracota. |

As texturas mantêm a resolução nativa. Esta revisão melhora o acabamento da renderização sobre as artes anteriores; não redesenha prédios, fachadas, objetos ou padrões do chão. Também retira a variação aleatória de cor que o shader de superfícies acrescentava sobre os pisos. A iluminação, as sombras de contato e a água animada já existentes continuam em uso.

## Aplicar sobre a última atualização

1. Saia do Play e feche a Unity.
2. Extraia a versão atual de `MemoriasTecidas_Atualizacao_24_Missoes.zip` fora do projeto.
3. Execute `Instalar_Atualizacao.cmd` e informe a pasta com `Assets`, `Packages` e `ProjectSettings`.
4. Reabra o mesmo projeto e a cena `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`.
5. Execute **Memorias Tecidas → Visual HD2D → Validar cenário original**.
6. Entre no Play. O Console deve informar **Cenário original preservado** e **Acabamento original**.

O instalador aceita a revisão anterior e mantém a comparação de versões, backup e reversão de falhas. Não reinstale uma versão antiga do projeto inteiro e não apague a pasta `Assets`: isso retiraria também as melhorias de interface e programação.

O pacote não grava cenas, prefabs, diálogos, missões, posições, conexões do mapa, colisões ou save. O repositório GitHub continua sendo o mesmo; esta entrega não faz push automaticamente. Arquivos locais desconhecidos continuam protegidos: se o instalador listar um conflito, guarde o arquivo para mesclar sua alteração.

## Ajustar a intensidade

Arquivo: `Assets/MemoriasTecidas/MundoV2/Resources/MemoriasTecidas/acabamento_original.json`.

```json
{
  "ativo": true,
  "nitidez": 0.32,
  "relevo": 0.22
}
```

`nitidez` e `relevo` aceitam valores de 0 a 1. Os valores padrão são conservadores. Com `ativo: false`, o novo realce fica desligado na próxima entrada no Play; o cenário continua usando as artes originais. Não é necessário trocar imagens ou recriar materiais para ajustar esses valores.

No jogo, **Esc → Opções → Efeitos** alterna:

- **Leve:** sem as amostras extras do novo realce.
- **Equilibrada:** nitidez discreta sobre a textura original.
- **Alta:** nitidez e relevo discreto.

O modo de movimento reduzido continua controlando o vento e a água como antes.

Em `HD2D/kit_hd2d.json`, `preservarCenarioOriginal` está `true` e as listas de substituição do cenário estão vazias. As imagens alternativas permanecem como histórico no pacote cumulativo, mas não são carregadas pela integração do cenário. A revisão atual não exige reinstalar o kit artístico complementar.

## Conferir no Play antes do Git

| Teste | Conferência |
| --- | --- |
| Praça, igreja, mercado e prefeitura | Mesmas fachadas e referências visuais anteriores. |
| Carnaúbas, árvores e oficina | Formas originais; mesmos lugares, tamanhos e pontos de interação. |
| Mosaico e ruas | Mesmos padrões e cores; nenhum calçamento alternativo por cima. |
| Rio e ponte | Textura anterior, margens e passagem corretas, movimento suave da água. |
| Qualidade Leve/Equilibrada/Alta | Realce discreto, sem contornos claros excessivos, borrões ou cintilação ao afastar a câmera. |
| Dia/noite, vegetação e redes | Luz, sombra e vento continuam acompanhando os objetos. |
| Novo Neto e Feliciana | Animação, rosto, diálogo e missão continuam funcionando. |
| M, I, J e Esc | Mapa, bolsa, caderno e menus preservados; ilustrações anteriores dos lugares disponíveis. |
| Encerrar e reabrir o Play | Sem duplicar efeitos ou acumular parâmetros. |

Depois dos testes, siga `TESTAR_E_ATUALIZAR_GIT.md`. Sugestão de mensagem do commit: `Restaura artes de Jaguaruana e refina acabamento das texturas`.

## Verificação realizada

Foram conferidos preservação dos PNGs e metadados anteriores, ausência de trocas de cenário no perfil, dependências, sintaxe C#, referências dos shaders, integridade e proteção de versões do pacote. O relatório está em `VERIFICACAO_VISUAL_ORIGINAL.json`.

**Compilação C#/shaders, execução do instalador PowerShell, Play, build e medição de desempenho dependem da Unity no seu computador e não foram executados aqui.** O validador incluído verifica os recursos e os erros de shader disponíveis no Editor. Confira o aspecto final no Play, principalmente quando estiver usando materiais personalizados.
