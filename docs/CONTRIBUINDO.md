# Como contribuir

## Fluxo de uma task

1. Pegue uma task sua no [board](https://github.com/users/souzxxx/projects/2) e mova pra **🔨 Fazendo**.
2. Crie a branch a partir da `main` atualizada: `git switch main && git pull && git switch -c <nome>/<issue>-<resumo>`.
   - Exemplo: `miguel/42-salas-modulares`
3. Trabalhe só nas **suas pastas** (ver [ARQUITETURA.md](ARQUITETURA.md)). Precisa de algo de outra frente? Use o mock e siga.
4. Commits pequenos, mensagem em português no imperativo: `Adiciona prefab da sala de câmeras`.
5. Antes do PR:
   - Rode os testes: **Test Runner > EditMode > Run All**.
   - Dê Play na `Boot` e veja se o fluxo não quebrou.
   - Assets novos? Crédito no README e linha no `docs/ASSETS.md`.
6. Abra o PR pra `main` com `Closes #<issue>` na descrição e mova o card pra **👀 Em revisão**.
7. **Alguém do grupo revisa** (qualquer um; o CODEOWNERS já marca o dono quando a pasta é dele). Aprovado, faça o merge (*Squash and merge*). O card vai pra **✅ Feito** sozinho.

> Mudança pequena e urgente (ex.: corrigir um typo) pode ir direto na `main`, mas avise no grupo.

## Ritual semanal (quinta)

- Build WebGL de teste (Leonardo) e todo mundo joga 5 minutos.
- Cada um atualiza a sua seção do GDD no Milanote.
- Revisar o board: o que fechou, o que atrasou, o que entra na próxima sprint.

## Regras do Unity (pra não ter conflito)

- **Nunca** edite cena ou prefab de outra pessoa sem combinar. Monte o seu trabalho em **prefabs na sua pasta**; as cenas principais só recebem prefabs prontos.
- Sempre commite o arquivo `.meta` junto com o asset. Asset sem `.meta` quebra referência pros outros.
- Não commite `Library/`, `Temp/`, `Logs/`, `UserSettings/` nem `Builds/` (o `.gitignore` já cuida).
- Todos usam **Unity 6000.6.4f1**. Se o Unity Hub oferecer atualizar a versão do projeto, recuse.
- Pacote novo (Package Manager)? Combine antes: ele muda o `Packages/manifest.json` de todo mundo.
- Asset novo no `GameDatabase`? Lembre que o `GameDatabase.asset` é compartilhado: faça o PR rápido pra evitar conflito.

### Configurar o Smart Merge (uma vez por máquina)

O `.gitattributes` já manda o git usar o `unityyamlmerge` em cenas, prefabs e assets. Falta só dizer pro git onde ele está:

**macOS**
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Helpers/UnityYAMLMerge' merge -p %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

**Windows** (PowerShell)
```powershell
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

Não achou o arquivo? Ele muda de pasta entre versões do Unity. Procure por `UnityYAMLMerge` dentro da pasta da versão no Unity Hub e use esse caminho.

## Definition of Done (o que é "pronto")

- [ ] Faz o que a issue pede (os critérios de "Pronto quando" estão todos ok).
- [ ] Funciona na `Boot` → fluxo normal, e não só na sua sandbox.
- [ ] Testes EditMode passando.
- [ ] Sem erro no Console ao dar Play.
- [ ] Textos do jogo em PT-BR, no tom da firma.
- [ ] Assets creditados.
- [ ] Se mudou regra de jogo: GDD/ESCOPO atualizados.

## Travou?

1. A outra parte ainda não existe? Use o mock (`Services.X` já funciona) ou o painel F1.
2. Falta algo no contrato (um evento, um campo, um método)? Abra issue com a label `frente:core` e combine no grupo. Não crie atalho direto pra classe de outra pessoa.
3. Bug de outra frente? Abra issue com o template **Bug** e marque o dono.

## Labels

- `frente:*`: de qual frente é a task.
- `ladrao:*`: tasks de um tipo de ladrão.
- `tipo:*`: código, arte, áudio, conteúdo, doc, integração, entrega.
- `rubrica:*`: qual rubrica a task ajuda. Toda task tem pelo menos uma.

## Campos do board

- **Status:**
  - 📥 Backlog
  - 📌 A fazer: está na sprint
  - 🔨 Fazendo
  - 👀 Em revisão
  - ✅ Feito
- **Sprint:** S0 a S3. As views "Sprint atual" filtram sozinhas.
- **Prioridade:**
  - 🔴 P0: essencial pra nota
  - 🟠 P1: importante
  - ⚪ P2: corta primeiro se faltar tempo
- **Tamanho:**
  - P: até 2 h
  - M: meio dia
  - G: 1–2 dias
