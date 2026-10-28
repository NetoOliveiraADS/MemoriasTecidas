# Conferir a atualização e atualizar o GitHub

## Revisão atual: artes originais de Jaguaruana

Execute **Memorias Tecidas → Visual HD2D → Validar cenário original** e confira `RESTAURAR_VISUAL_ORIGINAL.md`: fachadas, vegetação, oficina, texturas, água, qualidade e mapa. A correção ocorre ao iniciar o Play e preserva as cenas. Os testes de narrativa abaixo continuam úteis para regressão.

## Conferir a abertura nova

Siga o percurso e os testes de `PRIMEIRO_FIO.md`. Execute **Memorias Tecidas → Primeiro fio → Validar abertura e rede**. Teste primeiro seu save existente usando **J → Rede → Acompanhar o primeiro fio**; não precisa recomeçar a campanha.

## Conferência na Unity

1. Abra o projeto e resolva qualquer erro vermelho no Console antes do Play.
2. Execute **Memorias Tecidas → Organizacao → Validar marcos e interface**. Esse teste usa objetos temporários; verifica texturas, capítulos, marcos, escolta, duplicatas e limites.
3. Execute **Personagens → Validar transferencia de dialogos** e **Roteiro do PDF → Validar regras das 24 missoes**.
4. Abra **Organizar projeto → Visão geral → Conferir regiões…** para validar a cena real.
5. Salve a cena e entre no Play. Aproxime-se de Feliciana na região em que você a colocou e pressione E. Confirme missão, ficha e indicação do mapa.
6. Abra I. Digite `fio`, `madeira` e `chave` na busca quando os objetos estiverem disponíveis. As teclas I/M/J/Q não devem abrir painéis nem alterar efeitos enquanto você digita. Clique fora do campo e confirme que os atalhos voltam.
7. Confira todas as categorias, o filtro de itens disponíveis, a rolagem e a receita. O filtro não deve revelar pistas ainda não obtidas.
8. Abra J: quatro capítulos, detalhes bloqueados para missões futuras, fixar missão e localizar o próximo passo no mapa. Complete uma etapa e confira o nó de progresso.
9. Abra M: lugares, descrição e rota. Confirme que destinos bloqueados não podem ser guiados antes da liberação.
10. Fora do Play, mova um marco de encontro e teste novamente. Para patrulhas/escoltas, confira obstáculos e a chegada. Ctrl+Z deve restaurar a posição/grupo.
11. Confira 1280×720, 1920×1080 e 3840×2160 no Game View; inspecione diálogo, bolsa, mapa e atividade. Verifique também uma janela estreita, que deve manter a composição centralizada.
12. Teste o save existente e um save novo separado. Faça backup antes de escolher Nova jornada. Não apague o progresso usado para jogar apenas para testar.

Execute também **Visual e cenarios → Validar camera materiais e movimento** e os testes de cena, iluminação, contorno, qualidade e câmera descritos em `EVOLUCAO_VISUAL.md`.

## Enviar ao mesmo GitHub

Depois de conferir, no terminal **dentro do projeto existente**:

```bash
git status
git add -- Assets/MemoriasTecidas/MundoV2 Assets/MemoriasTecidas/Cenas/Jaguaruana.unity Docs/MemoriasTecidas
git diff --cached --stat
git diff --cached --check
git commit -m "Adiciona abertura com Feliciana e rede de memorias no caderno"
git push
```

Revise os arquivos preparados antes do commit, inclusive alterações suas que já estivessem preparadas. Não faça `git init` de novo e não use push forçado para esta atualização.

O instalador copia os guias para `Docs/MemoriasTecidas`. Você pode acrescentar um link para `Docs/MemoriasTecidas/COMECE_AQUI.md` no README principal do repositório.

Não envie saves pessoais nem `Library`, `Temp`, `Logs`, `obj` ou backups. Confira o `.gitignore` existente. Mantenha arquivos `.meta` junto dos assets. Se alterar as artes, mantenha também a configuração de Git LFS do projeto.

## Voltar à versão anterior

Feche a Unity. O instalador registra as cópias anteriores e `ARQUIVOS_NOVOS.txt` no backup ao lado do projeto. Restaure as cópias nos mesmos caminhos e remova somente os arquivos novos listados para esta instalação. Não misture scripts de versões diferentes.

Mudanças na cena feitas pelo painel podem ser desfeitas com Ctrl+Z antes de fechar o Editor ou recuperadas por uma versão da cena no Git. O instalador não substitui essa cena.

Edições do JSON feitas pelo painel têm backups próprios em `NOME_DO_PROJETO_Backup_Conteudo`. Preserve o arquivo atual antes de restaurar um anterior.


## Atualização de menus e UI/UX

Depois de seguir os testes de `UI_UX.md`, confira os arquivos da atualização no mesmo repositório:

```bash
git status --short
git diff --stat
git diff
```

As novas fontes, a arte e seus `.meta` devem entrar no Git. Não adicione pastas temporárias da Unity, ZIPs de entrega nem a pasta de backup do instalador. Para selecionar apenas os arquivos deste pacote, dentro da raiz do projeto:

```bash
git add Assets/MemoriasTecidas/MundoV2 Docs/MemoriasTecidas
git diff --cached --stat
git diff --cached
git commit -m "Restaura artes de Jaguaruana e refina acabamento das texturas"
git push
```

O comando `git add` acima também inclui outras mudanças suas dentro dessas duas pastas; revise antes do commit. Esta etapa não publica automaticamente no GitHub.
