# Registro da versão para substituição completa

Preparado em 15/10/2026 para aplicar na pasta do projeto que já está vinculada ao GitHub.

## Origem e mudanças

Base: `MemoriasTecidas.rar` enviado em 14/10/2026 e a revisão organizada desse mesmo projeto. Não contém automaticamente alterações posteriores feitas no computador do usuário.

- Retirados 15 grupos de protótipos, cenas antigas e backups: 348 arquivos, incluindo `.meta`, aproximadamente 160,7 MiB.
- Cena principal em `Assets/MemoriasTecidas/Cenas/Jaguaruana.unity`, com o GUID original preservado.
- Lista geral de cenas de build apontando para essa cena.
- Nove contêineres de layout histórico foram retirados da cena na revisão organizada. As regiões funcionais foram mantidas.
- Retirado o painel adicional `MTMOrganizadorSeguro.cs`. A ferramenta `MTOrganizarArquivosAtuais` também não acompanha o pacote.
- Guias de substituição, instalação e edição reunidos em `docs`.

Os módulos `MelhoriasV22`, `LugaresV23`, `ExpansaoUrbanaV24` e `NetoProtagonista` continuam juntos em `MundoV2`. Eles se complementam e não são cópias descartáveis.

## Conferência contra o RAR original

| Verificação | Resultado |
|---|---|
| Arquivos mantidos idênticos, considerando a mudança de caminho da cena | 1.206 |
| Arquivos com conteúdo alterado | Cena principal e `EditorBuildSettings.asset` |
| Metadado novo de pasta | `Assets/MemoriasTecidas/Cenas.meta` |
| Scripts C# do jogo e seus editores | 56, idênticos aos originais |
| Sintaxe C# pelo parser | Sem erros identificados |
| Tipos duplicados não parciais | Nenhum identificado |
| Regiões na cena | 16 antes e 16 depois |
| Pontos de interação na cena | 90 antes e 90 depois |
| Componente principal `MTMJogo` | 1 antes e 1 depois |
| Metadados com GUID na versão limpa | 618 |
| Referências locais da cena e lista de build | Conferidas, sem erros identificados |
| Imagens completas, sem ponteiros LFS | Conferidas |

Os registros serializados mantidos da cena foram comparados com os originais, admitindo apenas a retirada das referências aos layouts históricos e das respectivas listas de filhos. Código de execução, arte, pacotes e demais configurações permanecem iguais à origem.

## Limite da verificação

Não há Unity instalado no ambiente de preparação. A análise estática não compila C#, não resolve pacotes e não executa Play ou build. Foram encontradas sete referências por GUID externas à pasta `Assets`, já presentes nos arquivos originais; a resolução dessas referências deve ser conferida pelo Unity com os pacotes instalados.

A janela genérica de Safe Mode enviada pelo usuário não permite identificar qual arquivo ou pacote causou o erro no computador dele. Esta entrega elimina a mistura de arquivos antigos por substituição completa, mas não comprova a causa daquele erro. Se ele persistir, é necessário o texto da primeira mensagem vermelha do Console.

## Git e recuperação

O pacote não contém a pasta `.git` nem substitui configurações Git. Mantenha o repositório atual e mova os arquivos antigos para um backup externo antes de copiar os novos. Consulte [o guia de substituição](SUBSTITUIR_NO_PROJETO_ATUAL.md) para aplicar ou reverter a troca.
