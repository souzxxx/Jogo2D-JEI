# Balanceamento

> Dono: **Nicolas**. Rubrica Balanceamento (peso 2).
> Planilha: [`balanceamento/Balanceamento_v1.xlsx`](balanceamento/Balanceamento_v1.xlsx) · [Google Sheets](https://docs.google.com/spreadsheets/d/1pYudXkkM3sUWgJ2zF4S0KOWfcyLhDklo2aWt3NAhmdg/edit?usp=sharing)
> Simulador: `python docs/balanceamento/simulador.py` (lê os assets e mostra em que minuto cada golpe é concluído; `--extras` mostra quanto cada golpe extra rende perto da escada)

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

- `Data/Config/GameConfig.asset`: números globais (chance de evento, contagem da polícia, fugas, prisão, XP, recrutas, bicos, offline, prestígio, autosave).
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
| Tempo de prisão | `jailSeconds do golpe × (1 − Advogado%)`, arredondado pra cima | `CrewRules.JailSeconds` |
| Fiança | `segundos restantes × Cr$ 2 × manha (Pão-duro ×0,6) × (1 − Advogado%)`, arredondada pra cima | `CrewRules.BailCost` |
| Custo de upgrade | `custoBase × crescimento^nível` | `Formulas.UpgradeCost` |
| XP pro próximo nível | `5 × nível` | `Formulas.XpToNextLevel` |
| Fama no prestígio | `floor(√(loot do ciclo / 10.000))` | `Formulas.FameForLoot` |
| Custo de bônus de Fama | `custoBase × crescimento^nível`, arredondado pra cima | `ProgressionService.GetFamePerkCost` |
| Ciclo novo (prestígio) | Cr$ inicial + Herança; os novatos da Dona Cida nascem no nível 1 + Lenda; Procurado +1 | `ProgressionService.Prestige` |
| Procurado | dificuldade ×1,25, loot ×1,5, alerta ×1,1 por nível | Progressão |
| Modificador no mapa | cada golpe liberado: 50% de chance de 1 modificador, escolhido pelo `weight`; muda só depois de um golpe (abrir e fechar o mapa não troca); a Lotérica não sorteia antes do tutorial | `ProgressionService.RollModifiers` |
| Renda de bico | `renda × (1 + 0,1 × (nível−1)) × (2 se tipo certo) × (1 + PontoDeBico%) × (1 + Fama Rede%)`, mais o Catar latinha fixo | `EconomyService.GetSideJobIncome` |
| Ganho offline | `renda dos bicos × min(tempo fora, 4 h) × 50%`, em Cr$ inteiros; menos de 60 s fora ou relógio voltando não paga | `EconomyService.ApplyOfflineEarnings` |

## Tabela dos golpes (v1)

| Golpe | Salas | Loot total | Dificuldade por sala | Alerta/s |
|---|---|---|---|---|
| Lotérica do Seu Zé | 3 | Cr$ 500 | 250–350 | 1,5–2,5 |
| Joalheria Brilho Eterno | 4 | Cr$ 3.000 | 720–1.120 | 2–4 |
| Casa de Câmbio Dólar Fácil | 5 | Cr$ 18.000 | 1.120–1.680 | 3–5 |
| Cassino Clandestino | 6 | Cr$ 100.000 | 1.260–1.890 | 3,2–4,8 |
| Banco Central | 7 | Cr$ 600.000 | 1.380–2.280 | 0,7–5,6 |

**Golpes extras** (ramo ao lado da escada, `extra` no `HeistDef`; não contam pro prestígio):

| Golpe | Libera com | Salas | Loot total | Dificuldade por sala | Alerta/s | Rende na faixa (÷ melhor da escada) |
|---|---|---|---|---|---|---|
| Banca do Bicho do Tonhão | Lotérica | 3 (Pessoa) | Cr$ 900 | 260–340 | 1–1,5 | 81% até a Joalheria |
| Carro-forte da Avenida do Porto | Câmbio | 3 (Pessoa, 2 Fechadura) | Cr$ 20.000 | 1.300–2.200 | 6–10 | 81% até o Cassino (máx 91%) |
| Museu de Nova Guanabara | Cassino | 6 (Pessoa, 5 Sistema) | Cr$ 375.000 | 1.300–1.900 | 3,5–5,2 | 79% até o Banco Central |

Regra dos extras: render entre 75% e 90% do melhor golpe da escada na faixa deles. Vale a pena (e no automático é farm), mas nunca passa a escada, então a curva do Banco Central não muda.

## Curva simulada (v1)

Jogador perfeito do simulador: é o piso. Jogador de verdade demora mais, e o playtest (#43) dá o número real.

| Marco | Meta | v0 | v1 | v1 sem habilidades |
|---|---|---|---|---|
| Lotérica concluída | ~3 min com tutorial | 0,8 | 0,8 | 1,0 |
| Joalheria concluída (Câmbio liberado) | ~10 | 2,2 | 3,8 | 11,4 |
| Câmbio concluído (Cassino liberado) | ~20 | 5,6 | 12,1 | 36,5 |
| Cassino concluído | — | 55,6 | 26,1 | 74,4 |
| Banco Central concluído | **40–60** | nunca | **48,8** | 88,4 |

Pra ficar de olho no playtest:
- O começo ainda está rápido no papel (Câmbio liberado em ~4 min). Se o playtest confirmar, sobe a dificuldade da Joalheria.
- No fim do ciclo todos os upgrades chegam no máximo e sobra muito Cr$: custos do topo baratos demais pro loot do Cassino e do Banco. Ver na #44.
- Sem as habilidades o Banco Central leva ~90 min: no topo elas são obrigatórias, como o ESCOPO pede.

## Registro de ajustes

| Data | O que mudou | Por quê (dado do playtest) |
|---|---|---|
| 20/10 | Valores iniciais | Setup da fundação |
| 25/10 | Dificuldade: Joalheria ×1,6, Câmbio ×1,4, Cassino ×0,9, Banco ×0,6. Alerta/s: Cassino ×0,8, Banco ×0,7 | Simulador: o Cassino virava parede (nem com tudo no máximo fechava) e o Banco Central nunca era concluído, com salas de 22–36 s (regra: 5–20 s). Câmbio liberava em 2 min |
| 30/10 | Golpes extras: Bicho Cr$ 900; Carro-forte de Cr$ 30 mil pra 20 mil e alerta 4,5–7 pra 6–10; Museu de Cr$ 150 mil pra 375 mil | Simulador (`--extras`): o Carro-forte rendia 133% do Cassino e o jogador perfeito ficava 16 min nele, atrasando o Cassino em 5 min; o Museu rendia só 32% do Banco Central e ninguém ia |

## Playtest cronometrado (modelo)

| Data | Jogador | Tempo até Joalheria | Até Câmbio | Até Cassino | Até Banco Central | Travou em | Observações |
|---|---|---|---|---|---|---|---|
| | | | | | | | |
