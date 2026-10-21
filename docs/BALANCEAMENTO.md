# Balanceamento

> Dono: **Nicolas**. Rubrica Balanceamento (peso 2).
> Planilha: _colar o link do Google Sheets aqui (Sprint 1)_

## Metas

| Marco | Tempo alvo de jogo ativo |
|---|---|
| Fim do tutorial (1ª Lotérica) | ~3 min |
| Joalheria liberada | ~3–5 min |
| Câmbio liberado | ~10 min |
| Cassino liberado | ~20 min |
| Banco Central concluído (1º prestígio) | **40–60 min** |
| Ciclos seguintes | começo mais rápido (Fama), topo mais difícil (Procurado) |

Regras de bolso:
- Cada sala leva de 5 a 20 s.
- Sem especialista a sala dói (mais lenta e o dobro de alerta), mas é possível.
- Lotérica com a crew inicial **não** toca sirene se o jogador sair na hora certa.
- Nenhum upgrade inútil e nenhum que resolva o jogo sozinho.

## Onde ficam os números

- `Data/Config/GameConfig.asset`: números globais (chance de evento, contagem da polícia, fugas, prisão, XP, recrutas, offline, prestígio, autosave).
- `Data/Heists/*`: dificuldade, loot e alerta de cada sala.
- `Data/Economy/Upgrades/*`: custo base, crescimento, efeito por nível.
- `Thieves/*/ThiefType_*`: atributos base e ganho por nível de cada tipo.
- `Data/Progression/*`: modificadores e bônus de Fama.

**Nunca** coloque número de balanceamento direto no código.

## Fórmulas atuais

| Conta | Fórmula | Onde |
|---|---|---|
| Tempo pra limpar sala | `dificuldade × multModificador / poder` | Motor do Golpe |
| Poder | Perícia do melhor especialista; sem especialista: `0,35 × maior Perícia` | Motor do Golpe |
| Alerta por segundo | `alertaSala × (1 − 0,5 × Discrição média/100) × (2 sem especialista) × modificador × (Troca de turno nas primeiras salas) × manhas × (1 − Fama Sangue-frio%)` | `HeistRules.AlertPerSecond` |
| Contagem da polícia | `10 s + modificador (Blitz −3) + Rádio da polícia + Pé-na-Tábua`, mínimo 3 s | `HeistRules.PoliceCountdown` |
| Sacola na saída | antes da sirene 100% · fuga apressada 80% · fuga forçada 50% · Cavalo de pau 100% (Pé-na-Tábua +10% nas fugas com sirene) | `HeistRules.LootKeep` |
| Chance de prisão (só fuga forçada) | `(0,4 − 0,15 com Pé-na-Tábua) × (1 − 0,5 × Discrição do ladrão/100)` | `HeistRules.ArrestChance` |
| Pagamento do golpe | `sacola × saída × (1 + Lavanderia%) × (1 + Fama Esquema%)` | `HeistRules.Payment` |
| XP por sala limpa | `xpPerRoom × manha` (Cria da rua ×1,3; a fração vai somando sala a sala) | `HeistRules.XpPerRoom` |
| Recruta | nível sorteado de 1 a 1 + Contatos (+ Fama Lenda de graça); custo `50 × 1,8^(nível sorteado − 1) × (1 − Fama Reputação%)` | `CrewRules.RecruitCost` |
| Trocar candidatos na hora | `Cr$ 25 × (1 − 10% × nível de Contatos)` | `CrewRules.PaidRefreshCost` |
| Custo de upgrade | `custoBase × crescimento^nível` | `Formulas.UpgradeCost` |
| XP pro próximo nível | `5 × nível` | `Formulas.XpToNextLevel` |
| Fama no prestígio | `floor(√(loot do ciclo / 10.000))` | `Formulas.FameForLoot` |
| Procurado | dificuldade ×1,25, loot ×1,5, alerta ×1,1 por nível | Progressão |
| Renda de bico | `renda × (1 + 0,1 × (nível−1)) × (2 se tipo certo) × (1 + PontoDeBico%)` | Economia |

## Tabela inicial dos golpes (ponto de partida)

| Golpe | Salas | Loot total | Dificuldade por sala | Alerta/s |
|---|---|---|---|---|
| Lotérica do Seu Zé | 3 | Cr$ 500 | 250–350 | 1,5–2,5 |
| Joalheria Brilho Eterno | 4 | Cr$ 3.000 | 450–700 | 2–4 |
| Casa de Câmbio Dólar Fácil | 5 | Cr$ 18.000 | 800–1.200 | 3–5 |
| Cassino Clandestino | 6 | Cr$ 100.000 | 1.400–2.100 | 4–6 |
| Banco Central | 7 | Cr$ 600.000 | 2.300–3.800 | 1–8 |

## Registro de ajustes

| Data | O que mudou | Por quê (dado do playtest) |
|---|---|---|
| 20/10 | Valores iniciais | Setup da fundação |

## Playtest cronometrado (modelo)

| Data | Jogador | Tempo até Joalheria | Até Câmbio | Até Cassino | Até Banco Central | Travou em | Observações |
|---|---|---|---|---|---|---|---|
| | | | | | | | |
